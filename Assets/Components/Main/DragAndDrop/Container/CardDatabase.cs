using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// 【データ定義用】カードのレベルと所持枚数を格納するクラス
/// </summary>
[System.Serializable]
public class CardDeckData
{
    [Tooltip("カードの現在のレベル")]
    public int level = 1;
    [Tooltip("カードの現在の所持枚数")]
    public int count = 0;
}

/// <summary>
/// 【Inspector表示用】カード名とデータを紐づけるためのクラス
/// </summary>
[System.Serializable]
public class CardRegistryEntry
{
    [Tooltip("カードを識別するための名前（CharacterCardDataのcharacterNameと一致させる）")]
    public string cardName;
    public CardDeckData cardData = new CardDeckData();
}

/// <summary>
/// 全てのカードのレベルと所持枚数を管理するデータベース。
/// シーンに一つだけ配置してシングルトンとして機能します。
/// </summary>
public class CardDatabase : MonoBehaviour
{
    // シーン内のどこからでもアクセスできる静的インスタンス
    public static CardDatabase Instance { get; private set; }

    [Header("カード所持状況リスト")]
    [Tooltip("ここに所持しているカードの種類、レベル、枚数を登録します")]
    [SerializeField]
    private List<CardRegistryEntry> cardRegistry;
    
    // ゲーム実行中に高速アクセスするための辞書（Dictionary）
    private Dictionary<string, CardDeckData> _cardDatabase;

    void Awake()
    {
        // シングルトンパターンの実装
        if (Instance == null)
        {
            Instance = this;
            InitializeDatabase();
            // DontDestroyOnLoad(gameObject); // シーンをまたいでデータを保持する場合
        }
        else
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// Inspectorで設定されたリストを、実行時用の辞書に変換する
    /// </summary>
    private void InitializeDatabase()
    {
        _cardDatabase = new Dictionary<string, CardDeckData>();
        foreach (var entry in cardRegistry)
        {
            if (string.IsNullOrEmpty(entry.cardName))
            {
                Debug.LogWarning("CardDatabaseに名前が設定されていない項目があります。", this);
                continue;
            }
            if (_cardDatabase.ContainsKey(entry.cardName))
            {
                Debug.LogWarning($"CardDatabaseに'{entry.cardName}'が重複して登録されています。", this);
                continue;
            }
            _cardDatabase.Add(entry.cardName, entry.cardData);
        }
    }

    /// <summary>
    /// カード名を指定して、そのカードのレベルと枚数のデータを取得します。
    /// </summary>
    /// <param name="cardName">取得したいカードの名前</param>
    /// <returns>カードのデータ。見つからなければnull。</returns>
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