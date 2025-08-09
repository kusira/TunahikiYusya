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

    /// <summary>
    /// 初期状態にリセットします。
    /// soldier / archer / monk の level=1, count=1、それ以外は level=1, count=0。
    /// UnlockedCardOrder は soldier → archer → monk の順に再構築します。
    /// </summary>
    public void ResetToDefaults()
    {
        if (_cardDatabase == null || cardRegistry == null)
        {
            InitializeDatabase();
        }

        string[] initialNames = new[] { "soldier", "archer", "monk" };
        var initialSet = new HashSet<string>(initialNames);

        // 全カードを初期化
        foreach (var entry in cardRegistry)
        {
            if (entry == null || string.IsNullOrEmpty(entry.cardName) || entry.cardData == null) continue;
            entry.cardData.level = 1;
            entry.cardData.count = initialSet.Contains(entry.cardName) ? 1 : 0;

            // 辞書にも反映（参照同一のため不要だが念のため）
            if (_cardDatabase.ContainsKey(entry.cardName))
            {
                _cardDatabase[entry.cardName] = entry.cardData;
            }
            else
            {
                _cardDatabase.Add(entry.cardName, entry.cardData);
            }
        }

        // アンロック順を soldier → archer → monk の順で再構築
        var newOrder = new List<string>();
        foreach (var name in initialNames)
        {
            if (_cardDatabase.TryGetValue(name, out var data) && data != null && data.count > 0)
            {
                newOrder.Add(name);
            }
        }
        UnlockedCardOrder = newOrder;

        Debug.Log("CardDatabase: ResetToDefaults を実行しました (soldier, archer, monk を初期アンロック)。", this);
    }
}