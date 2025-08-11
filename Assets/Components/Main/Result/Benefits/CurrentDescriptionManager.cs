using UnityEngine;
using TMPro;

/// <summary>
/// 現在のカード情報を表示するマネージャー
/// CardDatabaseから情報を取得し、指定された形式でTMPに表示します
/// </summary>
public class CurrentDescriptionManager : MonoBehaviour
{
    [Header("表示設定")]
    [SerializeField] private TMP_Text descriptionText;
    [SerializeField] private string cardName = "hero"; // 表示するカード名
    
    [Header("表示形式設定")]
    [SerializeField] private string levelPrefix = "Lv.";
    [SerializeField] private string countPrefix = "×";
    
    private CardDatabase cardDatabase;
    
    void Start()
    {
        // CardDatabaseの参照を取得
        cardDatabase = CardDatabase.Instance;
        if (cardDatabase == null)
        {
            Debug.LogError("CardDatabaseが見つかりません！", this);
            return;
        }
        
        // 初期表示を更新
        UpdateDescription();
    }
    
    void Update()
    {
        // 毎フレーム更新（必要に応じて変更可能）
        UpdateDescription();
    }
    
    /// <summary>
    /// カード情報の説明文を更新します
    /// </summary>
    private void UpdateDescription()
    {
        if (descriptionText == null || cardDatabase == null)
        {
            return;
        }
        
        // CardDatabaseからカード情報を取得
        var cardData = cardDatabase.GetCardData(cardName);
        if (cardData == null)
        {
            descriptionText.text = "カード情報が見つかりません";
            return;
        }
        
        // LevelとCountが両方とも0の場合は何も表示しない
        if (cardData.level == 0 && cardData.count == 0)
        {
            descriptionText.text = "";
            return;
        }
        
        // 指定された形式でテキストを構築
        string description = $"{levelPrefix} {cardData.level}\n{countPrefix} {cardData.count}";
        descriptionText.text = description;
    }
    
    /// <summary>
    /// 表示するカード名を変更します
    /// </summary>
    /// <param name="newCardName">新しいカード名</param>
    public void SetCardName(string newCardName)
    {
        cardName = newCardName;
        UpdateDescription();
    }
    
    /// <summary>
    /// 手動で説明文を更新します
    /// </summary>
    public void RefreshDescription()
    {
        UpdateDescription();
    }
}
