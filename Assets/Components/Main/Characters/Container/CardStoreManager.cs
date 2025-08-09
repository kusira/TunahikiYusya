using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;

// ▼▼▼ 1. Inspectorで編集するための専用クラスを追加 ▼▼▼
/// <summary>
/// カード名と、それに対応するカードのプレハブを紐づけるためのクラス
/// </summary>
[System.Serializable]
public class CardPrefabMapping
{
    [Tooltip("カード名（CardDatabaseやCharacterCardDataのCardNameと一致させる）")]
    public string cardName;
    [Tooltip("上記カード名に対応するカードのプレハブ")]
    public GameObject cardPrefab;
}


/// <summary>
/// キャラクターショップのカードを管理・配置します。
/// CardDatabaseを監視し、アンロックされたカードをリアルタイムで表示します。
/// </summary>
public class CardStoreManager : MonoBehaviour
{
    [Header("カードプレハブ設定")]
    // ▼▼▼ 2. Inspectorでの設定方法を新しいリストに変更 ▼▼▼
    [Tooltip("ストアに並べるカードの名前とプレハブを登録します")]
    [SerializeField] private List<CardPrefabMapping> cardPrefabMappings;

    [Header("配置設定")]
    [SerializeField] private float startX = -7.5f;
    [SerializeField] private float startY = -3.9f;
    [SerializeField] private float spacingX = 2.1f;

    [Header("レイアウト監視設定")]
    [SerializeField] private float layoutTweenDuration = 0.2f;
    [SerializeField] private float monitorInterval = 0.2f;

    [Header("監視対象")]
    [SerializeField] private string targetTag = "CharacterContainer";

    // プレハブを名前で高速に検索するための辞書
    private Dictionary<string, GameObject> _cardPrefabDict;
    // 現在ストアに表示されているカードのリスト
    private List<GameObject> _spawnedCards = new List<GameObject>();

    void Awake()
    {
        // ▼▼▼ 3. 新しいリストから辞書を生成するようにAwakeを修正 ▼▼▼
        _cardPrefabDict = new Dictionary<string, GameObject>();
        foreach (var mapping in cardPrefabMappings)
        {
            // 名前やプレハブが未設定、または名前が重複している場合はエラーを防ぐ
            if (mapping != null && !string.IsNullOrEmpty(mapping.cardName) && mapping.cardPrefab != null)
            {
                if (!_cardPrefabDict.ContainsKey(mapping.cardName))
                {
                    _cardPrefabDict.Add(mapping.cardName, mapping.cardPrefab);
                }
                else
                {
                    Debug.LogWarning($"CardStoreManager: カード名 '{mapping.cardName}' が重複して登録されています。", this);
                }
            }
        }
    }
    
    void OnEnable()
    {
        CardDatabase.OnCardUnlocked += HandleCardUnlocked;
        StartCoroutine(CoMonitorLayout());
    }

    void OnDisable()
    {
        CardDatabase.OnCardUnlocked -= HandleCardUnlocked;
        StopAllCoroutines();
    }

    void Start()
    {
        if (CardDatabase.Instance == null)
        {
            Debug.LogError("シーンにCardDatabaseが存在しません。ストアを生成できません。", this);
            return;
        }
        DisplayInitialCards();
    }
    
    private void DisplayInitialCards()
    {
        List<string> initiallyUnlocked = CardDatabase.Instance.UnlockedCardOrder;
        foreach (var cardName in initiallyUnlocked)
        {
            SpawnAndPlaceCard(cardName, playAnimation: false);
        }
        RelayoutNow();
    }

    private void HandleCardUnlocked(string cardName)
    {
        Debug.Log($"ストアが '{cardName}' のアンロックを検知しました。");
        var newCard = SpawnAndPlaceCard(cardName, playAnimation: true);
        RelayoutNow();
    }

    // ▼▼▼ 4. FindCardPrefabByNameメソッドは不要になったため削除し、このメソッドを修正 ▼▼▼
    private GameObject SpawnAndPlaceCard(string cardName, bool playAnimation)
    {
        // 辞書からプレハブを直接取得（高速）
        if (!_cardPrefabDict.TryGetValue(cardName, out GameObject prefabToSpawn))
        {
            Debug.LogWarning($"カードプレハブの登録に '{cardName}' が見つかりませんでした。");
            return null;
        }
        
        GameObject newCardInstance = Instantiate(prefabToSpawn, transform);
        newCardInstance.transform.localPosition = new Vector3(startX + (_spawnedCards.Count * spacingX), startY, 0);
        _spawnedCards.Add(newCardInstance);

        if (playAnimation)
        {
            var cardData = newCardInstance.GetComponent<CharacterCardData>();
            cardData?.PlayUnlockAnimation();
        }
        
        return newCardInstance;
    }

    private System.Collections.IEnumerator CoMonitorLayout()
    {
        var wait = new WaitForSeconds(monitorInterval);
        while (isActiveAndEnabled)
        {
            CleanupAndSyncList();
            RelayoutNow();
            yield return wait;
        }
    }

    private void CleanupAndSyncList()
    {
        // null を除去
        _spawnedCards.RemoveAll(go => go == null);
        // タグ付きの子のみを監視対象として再構築（常に正規化）
        _spawnedCards = transform.Cast<Transform>()
                                 .Select(t => t.gameObject)
                                 .Where(go => go != null && go.CompareTag(targetTag))
                                 .OrderBy(go => go.transform.localPosition.x)
                                 .ToList();
    }

    public void RelayoutNow()
    {
        for (int i = 0; i < _spawnedCards.Count; i++)
        {
            var go = _spawnedCards[i];
            if (go == null) continue;
            if (go == this.gameObject) continue;                 // 自身は対象外
            if (!go.CompareTag(targetTag)) continue;             // タグ以外は対象外
            var tr = go.transform;
            Vector3 targetLocal = new Vector3(startX + (i * spacingX), startY, 0f);
            if ((tr.localPosition - targetLocal).sqrMagnitude > 0.0001f)
            {
                tr.DOKill(false);
                tr.DOLocalMove(targetLocal, layoutTweenDuration).SetEase(Ease.OutCubic);
            }
        }
    }
}