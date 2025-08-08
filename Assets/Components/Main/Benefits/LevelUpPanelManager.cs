using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using TMPro;

/// <summary>
/// ローグライクゲーム用のレベルアップパネルマネージャー
/// キャラクターのレベルアップ時に表示されるUIパネルを管理します
/// </summary>
public class LevelUpPanelManager : MonoBehaviour
{
    [Header("アニメーション設定")]
    [Tooltip("フェードする時間")]
    [SerializeField] private float fadeDuration = 0.5f;
    [Tooltip("Yの移動量")]
    [SerializeField] private float moveYAmount = 50f;
    [Tooltip("表示の遅れ")]
    [SerializeField] private float displayDelay = 0.1f;

    [Header("テスト用設定")]
    [Tooltip("テスト用のキャラクター名（インスペクタで設定可能）")]
    [SerializeField] private string testCharacterName = "Warrior";
    [Tooltip("テスト用の現在レベル")]
    [SerializeField] private int testCurrentLevel = 1;

    [Header("UI要素")]
    [SerializeField] private Image characterImage;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text currLevelText;
    [SerializeField] private TMP_Text nextLevelText;
    [SerializeField] private TMP_Text currATKText;
    [SerializeField] private TMP_Text nextATKText;
    [SerializeField] private TMP_Text currHPText;
    [SerializeField] private TMP_Text nextHPText;
    [SerializeField] private TMP_Text skillText;

    // 内部変数
    private CanvasGroup canvasGroup;
    private RectTransform rectTransform;
    private CharacterDatabase characterDatabase;
    private bool isVisible = false;

    void Awake()
    {
        // コンポーネントの取得
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }

        rectTransform = GetComponent<RectTransform>();
        
        // データベースの取得
        characterDatabase = FindAnyObjectByType<CharacterDatabase>();
        if (characterDatabase == null)
        {
            Debug.LogError("CharacterDatabaseが見つかりません！シーンにCharacterDatabaseを配置してください。");
        }
        
        // 初期状態で非表示
        SetInitialState();
    }

    void Start()
    {
        // 仮テスト用：最初からアクティブにする
        gameObject.SetActive(true);
        ShowLevelUpPanel(testCharacterName, testCurrentLevel);
    }

    /// <summary>
    /// 初期状態を設定します
    /// </summary>
    private void SetInitialState()
    {
        canvasGroup.alpha = 0f;
        Vector3 currentPos = rectTransform.anchoredPosition;
        rectTransform.anchoredPosition = new Vector3(currentPos.x, currentPos.y - moveYAmount, currentPos.z);
        gameObject.SetActive(false);
    }

    /// <summary>
    /// レベルアップパネルを表示します
    /// </summary>
    /// <param name="characterName">キャラクター名</param>
    /// <param name="currentLevel">現在のレベル</param>
    public void ShowLevelUpPanel(string characterName, int currentLevel)
    {
        if (isVisible) return;

        // データを設定
        if (!SetPanelData(characterName, currentLevel))
        {
            Debug.LogError($"レベルアップパネルの表示に失敗しました。キャラクター名: {characterName}, レベル: {currentLevel}");
            return;
        }
        
        // パネルをアクティブにする
        gameObject.SetActive(true);
        
        // 表示アニメーション
        DOVirtual.DelayedCall(displayDelay, () => {
            AnimateShow();
        });
    }

    /// <summary>
    /// テスト用のレベルアップパネルを表示します（インスペクタから呼び出し可能）")
    /// </summary>
    [ContextMenu("テスト用レベルアップパネル表示")]
    public void ShowTestLevelUpPanel()
    {
        ShowLevelUpPanel(testCharacterName, testCurrentLevel);
    }

    /// <summary>
    /// パネルのデータを設定します
    /// </summary>
    /// <returns>設定が成功したかどうか</returns>
    private bool SetPanelData(string characterName, int currentLevel)
    {
        if (characterDatabase == null)
        {
            Debug.LogError("CharacterDatabaseが見つかりません！");
            return false;
        }

        // キャラクターデータを取得
        var characterData = characterDatabase.GetCharacterData(characterName);
        if (characterData == null)
        {
            Debug.LogError($"キャラクター「{characterName}」のデータが見つかりません！");
            return false;
        }

        // 現在のレベルと次のレベルのステータスを取得
        var currentStats = characterDatabase.GetStats(characterName, currentLevel);
        var nextStats = characterDatabase.GetStats(characterName, currentLevel + 1);

        if (currentStats == null)
        {
            Debug.LogError($"キャラクター「{characterName}」のレベル{currentLevel}のデータが見つかりません！");
            return false;
        }

        // UI要素にデータを設定
        if (characterImage != null && characterData.characterSprite != null)
        {
            characterImage.sprite = characterData.characterSprite;
        }
        else if (characterImage != null && characterData.characterSprite == null)
        {
            Debug.LogWarning($"キャラクター「{characterName}」にスプライトが設定されていません。");
        }

        if (nameText != null)
        {
            nameText.text = string.IsNullOrEmpty(characterData.displayName) ? characterData.characterName : characterData.displayName;
        }

        if (currLevelText != null)
        {
            currLevelText.text = $"Lv. {currentLevel}";
        }

        if (nextLevelText != null)
        {
            nextLevelText.text = $"Lv. {currentLevel + 1}";
        }

        if (currATKText != null)
        {
            currATKText.text = currentStats.atk.ToString();
        }

        if (nextATKText != null && nextStats != null)
        {
            nextATKText.text = nextStats.atk.ToString();
        }
        else if (nextATKText != null && nextStats == null)
        {
            nextATKText.text = "MAX";
        }

        if (currHPText != null)
        {
            currHPText.text = currentStats.hp.ToString();
        }

        if (nextHPText != null && nextStats != null)
        {
            nextHPText.text = nextStats.hp.ToString();
        }
        else if (nextHPText != null && nextStats == null)
        {
            nextHPText.text = "MAX";
        }

        if (skillText != null)
        {
            skillText.text = currentStats.skillDescription;
        }

        return true;
    }

    /// <summary>
    /// 表示アニメーションを実行します
    /// </summary>
    private void AnimateShow()
    {
        isVisible = true;
        
        // フェードインと移動を同時に実行
        canvasGroup.DOFade(1f, fadeDuration);
        rectTransform.DOAnchorPosY(rectTransform.anchoredPosition.y + moveYAmount, fadeDuration)
            .SetEase(Ease.OutQuad);
    }

    /// <summary>
    /// レベルアップパネルを非表示にします
    /// </summary>
    public void HideLevelUpPanel()
    {
        if (!isVisible) return;

        isVisible = false;
        
        // フェードアウトと移動を同時に実行
        canvasGroup.DOFade(0f, fadeDuration);
        rectTransform.DOAnchorPosY(rectTransform.anchoredPosition.y - moveYAmount, fadeDuration)
            .SetEase(Ease.InQuad)
            .OnComplete(() => {
                gameObject.SetActive(false);
                SetInitialState();
            });
    }

    /// <summary>
    /// クリック時の処理（UI Buttonから呼び出し）
    /// </summary>
    public void OnPanelClick()
    {
        HideLevelUpPanel();
    }
}