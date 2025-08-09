using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System; // Actionを使うために必要

// (CardDeckData, CardRegistryEntryクラスは変更なし)
[System.Serializable]
public class CardDeckData
{
    [Tooltip("カードの現在のレベル")]
    public int level = 1;
    [Tooltip("カードの現在の所持枚数")]
    public int count = 0;
}
[System.Serializable]
public class CardRegistryEntry
{
    [Tooltip("カードを識別するための名前")]
    public string cardName;
    public CardDeckData cardData = new CardDeckData();
}

public class CardDatabase : MonoBehaviour
{
    public static CardDatabase Instance { get; private set; }

    // ▼▼▼ 1. イベントを追加 ▼▼▼
    /// <summary>
    /// カードがアンロックされた時に呼び出されるイベント。
    /// 引数としてアンロックされたカードの名前(string)を渡します。
    /// </summary>
    public static event Action<string> OnCardUnlocked;


    [Header("カード所持状況リスト")]
    [SerializeField]
    private List<CardRegistryEntry> cardRegistry;
    
    public List<string> UnlockedCardOrder { get; private set; }
    private Dictionary<string, CardDeckData> _cardDatabase;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // シーンをまたいでも破棄しない
            InitializeDatabase();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void InitializeDatabase()
    {
        _cardDatabase = new Dictionary<string, CardDeckData>();
        UnlockedCardOrder = new List<string>();

        foreach (var entry in cardRegistry)
        {
            if (string.IsNullOrEmpty(entry.cardName) || _cardDatabase.ContainsKey(entry.cardName)) continue;
            _cardDatabase.Add(entry.cardName, entry.cardData);

            if (entry.cardData.count > 0)
            {
                UnlockedCardOrder.Add(entry.cardName);
            }
        }
    }

    public void UnlockCard(string cardName)
    {
        CardDeckData data = GetCardData(cardName);
        if (data != null && data.count == 0)
        {
            data.count = 1;
            if (!UnlockedCardOrder.Contains(cardName))
            {
                UnlockedCardOrder.Add(cardName);
                
                // ▼▼▼ 2. イベントを発火させる ▼▼▼
                // 登録しているリスナー（CharacterStoreManagerなど）に通知
                OnCardUnlocked?.Invoke(cardName);
            }
            Debug.Log($"カード '{cardName}' がアンロックされました！");
        }
    }
    
    public CardDeckData GetCardData(string cardName)
    {
        if (_cardDatabase.TryGetValue(cardName, out CardDeckData data))
        {
            return data;
        }
        Debug.LogError($"CardDatabaseに'{cardName}'という名前のカードが見つかりません。", this);
        return null;
    }
}