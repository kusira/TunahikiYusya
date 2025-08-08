using UnityEngine;
using System.Collections.Generic;
using System.Linq;

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
    }

    void OnDisable()
    {
        CardDatabase.OnCardUnlocked -= HandleCardUnlocked;
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
    }

    private void HandleCardUnlocked(string cardName)
    {
        Debug.Log($"ストアが '{cardName}' のアンロックを検知しました。");
        var newCard = SpawnAndPlaceCard(cardName, playAnimation: true);
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
        
        Vector3 spawnPosition = new Vector3(startX + (_spawnedCards.Count * spacingX), startY, 0);
        GameObject newCardInstance = Instantiate(prefabToSpawn, spawnPosition, Quaternion.identity, transform);
        _spawnedCards.Add(newCardInstance);

        if (playAnimation)
        {
            var cardData = newCardInstance.GetComponent<CharacterCardData>();
            cardData?.PlayUnlockAnimation();
        }
        
        return newCardInstance;
    }
}