using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using TMPro;

public class LevelUpPanelManager : MonoBehaviour
{
    [Header("アニメーション設定")]
    [Tooltip("フェードする時間")]
    [SerializeField] private float fadeDuration = 0.5f;
    [Tooltip("Yの移動量")]
    [SerializeField] private float moveYAmount = 50f;
    [Tooltip("表示の遅れ (秒)")]
    [SerializeField] private float displayDelay = 0.1f;

    [Header("対象キャラクター")]
    [Tooltip("対象のキャラクター名（CardDatabase/CharacterDatabaseのキーと一致）")]
    [SerializeField] private string characterName;
    public string CharacterName
    {
        get => characterName;
        set => characterName = value;
    }

    [Header("UI要素 (インスペクタでアサイン)")]
    [SerializeField] private Image characterImage;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text currLevelText;
    [SerializeField] private TMP_Text nextLevelText;
    [SerializeField] private TMP_Text currATKText;
    [SerializeField] private TMP_Text nextATKText;
    [SerializeField] private TMP_Text currHPText;
    [SerializeField] private TMP_Text nextHPText;
    [SerializeField] private TMP_Text skillText;

    public event System.Action<MonoBehaviour> Selected;

    private CanvasGroup canvasGroup;
    private RectTransform rectTransform;
    private CharacterDatabase characterDatabase;
    private CardDatabase cardDatabase;
    private bool isVisible = false;

    private const int MaxLevel = 3;

    void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null) canvasGroup = gameObject.AddComponent<CanvasGroup>();
        rectTransform = GetComponent<RectTransform>();

        characterDatabase = FindAnyObjectByType<CharacterDatabase>();
        cardDatabase = CardDatabase.Instance != null ? CardDatabase.Instance : FindAnyObjectByType<CardDatabase>();

        if (characterDatabase == null) Debug.LogError("CharacterDatabaseが見つかりません。シーンに配置してください。", this);
        if (cardDatabase == null) Debug.LogError("CardDatabaseが見つかりません。シーンに配置してください。", this);

        if (characterImage == null) Debug.LogError("characterImage が未アサインです。", this);
        if (nameText == null) Debug.LogError("nameText が未アサインです。", this);
        if (currLevelText == null) Debug.LogError("currLevelText が未アサインです。", this);
        if (nextLevelText == null) Debug.LogError("nextLevelText が未アサインです。", this);
        if (currATKText == null) Debug.LogError("currATKText が未アサインです。", this);
        if (nextATKText == null) Debug.LogError("nextATKText が未アサインです。", this);
        if (currHPText == null) Debug.LogError("currHPText が未アサインです。", this);
        if (nextHPText == null) Debug.LogError("nextHPText が未アサインです。", this);
        if (skillText == null) Debug.LogError("skillText が未アサインです。", this);

        SetInitialState();
    }

    private void SetInitialState()
    {
        canvasGroup.alpha = 0f;
        var p = rectTransform.anchoredPosition;
        rectTransform.anchoredPosition = new Vector2(p.x, p.y - moveYAmount);
        gameObject.SetActive(false);
    }

    public void Show()
    {
        if (string.IsNullOrEmpty(characterName))
        {
            Debug.LogError("characterName が空です。先に CharacterName を設定してください。", this);
            return;
        }

        int currentLevel = GetCurrentLevelFromCardDatabase(characterName);
        if (currentLevel <= 0)
        {
            Debug.LogError($"CardDatabaseからレベルを取得できませんでした: {characterName}", this);
            return;
        }

        if (currentLevel >= MaxLevel)
        {
            Debug.Log($"「{characterName}」は既に最大レベル({MaxLevel})のため、レベルアップパネルは表示しません。", this);
            return;
        }

        if (!SetPanelData(characterName, currentLevel)) return;

        gameObject.SetActive(true);
        DOVirtual.DelayedCall(displayDelay, AnimateShow);
    }

    public void Show(string name)
    {
        CharacterName = name;
        Show();
    }

    private int GetCurrentLevelFromCardDatabase(string name)
    {
        if (cardDatabase == null) return -1;
        var data = cardDatabase.GetCardData(name);
        if (data == null) return -1;
        return Mathf.Max(1, data.level);
    }

    private bool SetPanelData(string name, int currentLevel)
    {
        if (characterDatabase == null) return false;

        var characterData = characterDatabase.GetCharacterData(name);
        if (characterData == null)
        {
            Debug.LogError($"キャラクター「{name}」のデータが見つかりません。", this);
            return false;
        }

        var currentStats = characterDatabase.GetStats(name, currentLevel);
        var nextStats = characterDatabase.GetStats(name, currentLevel + 1);
        if (currentStats == null)
        {
            Debug.LogError($"「{name}」レベル{currentLevel}のデータが見つかりません。", this);
            return false;
        }

        if (characterImage != null)
        {
            if (characterData.characterSprite != null) characterImage.sprite = characterData.characterSprite;
            else Debug.LogWarning($"「{name}」にスプライトが設定されていません。", this);
        }

        if (nameText != null)
            nameText.text = string.IsNullOrEmpty(characterData.displayName) ? characterData.characterName : characterData.displayName;

        if (currLevelText != null) currLevelText.text = $"Lv. {currentLevel}";
        if (nextLevelText != null) nextLevelText.text = $"Lv. {currentLevel + 1}";

        if (currATKText != null) currATKText.text = currentStats.atk.ToString();
        if (nextATKText != null) nextATKText.text = nextStats != null ? nextStats.atk.ToString() : "MAX";

        if (currHPText != null) currHPText.text = currentStats.hp.ToString();
        if (nextHPText != null) nextHPText.text = nextStats != null ? nextStats.hp.ToString() : "MAX";

        if (skillText != null) skillText.text = currentStats.skillDescription;

        return true;
    }

    private void AnimateShow()
    {
        isVisible = true;
        canvasGroup.DOFade(1f, fadeDuration);
        rectTransform.DOAnchorPosY(rectTransform.anchoredPosition.y + moveYAmount, fadeDuration).SetEase(Ease.OutQuad);
    }

    public void OnPanelClick()
    {
        var mgr = FindAnyObjectByType<BenefitsManager>();
        if (mgr != null) mgr.OnPanelSelected(this);
        Hide();
        Selected?.Invoke(this);
        Hide();
    }

    public void Hide()
    {
        if (!isVisible) return;
        isVisible = false;

        ApplyLevelUpToCardDatabase();

        canvasGroup.DOFade(0f, fadeDuration);
        rectTransform
            .DOAnchorPosY(rectTransform.anchoredPosition.y - moveYAmount, fadeDuration)
            .SetEase(Ease.InQuad)
            .OnComplete(() =>
            {
                Destroy(gameObject);
            });
    }

    public void HideAsOther(float distance, float duration)
    {
        if (!isVisible) return;
        isVisible = false;

        canvasGroup.DOFade(0f, duration);
        rectTransform
            .DOAnchorPosY(rectTransform.anchoredPosition.y - Mathf.Abs(distance), duration)
            .SetEase(Ease.InQuad)
            .OnComplete(() => Destroy(gameObject));
    }

    private void ApplyLevelUpToCardDatabase()
    {
        if (cardDatabase == null || string.IsNullOrEmpty(characterName))
        {
            Debug.LogError("CardDatabase もしくは characterName が未設定です。", this);
            return;
        }

        var deckData = cardDatabase.GetCardData(characterName);
        if (deckData == null)
        {
            Debug.LogError($"CardDatabaseに「{characterName}」が見つかりません。", this);
            return;
        }

        int current = Mathf.Max(0, deckData.level);
        if (current >= MaxLevel)
        {
            Debug.Log($"「{characterName}」は既に最大レベル({MaxLevel})です。", this);
            return;
        }

        deckData.level = current + 1;
    }
}