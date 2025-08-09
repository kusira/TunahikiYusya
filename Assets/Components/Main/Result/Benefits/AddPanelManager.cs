using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

public class AddPanelManager : MonoBehaviour
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
    [SerializeField] private Image Characterr;   // キャラ画像
    [SerializeField] private TMP_Text Name;      // 表示名
    [SerializeField] private TMP_Text CurrCount; // 現在のCount
    [SerializeField] private TMP_Text NextCount; // 次のCount (=現在+2)
    [SerializeField] private TMP_Text SkillText; // スキル説明

    public event System.Action<MonoBehaviour> Selected;

    private CanvasGroup canvasGroup;
    private RectTransform rectTransform;
    private CharacterDatabase characterDatabase;
    private CardDatabase cardDatabase;
    private bool isVisible = false;

    void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null) canvasGroup = gameObject.AddComponent<CanvasGroup>();
        rectTransform = GetComponent<RectTransform>();

        characterDatabase = FindAnyObjectByType<CharacterDatabase>();
        cardDatabase = CardDatabase.Instance != null ? CardDatabase.Instance : FindAnyObjectByType<CardDatabase>();

        if (characterDatabase == null) Debug.LogError("CharacterDatabaseが見つかりません。シーンに配置してください。", this);
        if (cardDatabase == null) Debug.LogError("CardDatabaseが見つかりません。シーンに配置してください。", this);

        if (Characterr == null) Debug.LogError("Characterr が未アサインです。", this);
        if (Name == null) Debug.LogError("Name が未アサインです。", this);
        if (CurrCount == null) Debug.LogError("CurrCount が未アサインです。", this);
        if (NextCount == null) Debug.LogError("NextCount が未アサインです。", this);
        if (SkillText == null) Debug.LogError("SkillText が未アサインです。", this);

        SetInitialState();
    }

    private void SetInitialState()
    {
        canvasGroup.alpha = 0f;
        var p = rectTransform.anchoredPosition;
        rectTransform.anchoredPosition = new Vector2(p.x, p.y - moveYAmount);
        gameObject.SetActive(false);
    }

    public bool CanShow()
    {
        if (cardDatabase == null || string.IsNullOrEmpty(characterName)) return false;
        var deck = cardDatabase.GetCardData(characterName);
        if (deck == null) return false;
        return deck.count > 0;
    }

    public void Show()
    {
        if (string.IsNullOrEmpty(characterName))
        {
            Debug.LogError("characterName が空です。先に CharacterName を設定してください。", this);
            return;
        }
        if (cardDatabase == null || characterDatabase == null) return;

        var deck = cardDatabase.GetCardData(characterName);
        if (deck == null) return;

        if (deck.count == 0) return;

        var cdata = characterDatabase.GetCharacterData(characterName);
        if (cdata == null)
        {
            Debug.LogError($"キャラクター「{characterName}」のデータが見つかりません。", this);
            return;
        }

        if (Characterr != null)
        {
            if (cdata.characterSprite != null) Characterr.sprite = cdata.characterSprite;
            else Debug.LogWarning($"「{characterName}」にスプライトが設定されていません。", this);
        }

        if (Name != null)
            Name.text = string.IsNullOrEmpty(cdata.displayName) ? cdata.characterName : cdata.displayName;

        if (CurrCount != null) CurrCount.text = deck.count.ToString();
        if (NextCount != null) NextCount.text = (deck.count + 1).ToString();

        int lvl = Mathf.Max(1, deck.level);
        var stats = characterDatabase.GetStats(characterName, lvl);
        if (SkillText != null) SkillText.text = stats != null ? stats.skillDescription : string.Empty;

        gameObject.SetActive(true);
        DOVirtual.DelayedCall(displayDelay, AnimateShow).SetUpdate(true);
    }

    public void Show(string name)
    {
        CharacterName = name;
        Show();
    }

    private void AnimateShow()
    {
        isVisible = true;
        canvasGroup.DOFade(1f, fadeDuration).SetUpdate(true);
        rectTransform.DOAnchorPosY(rectTransform.anchoredPosition.y + moveYAmount, fadeDuration)
                     .SetEase(Ease.OutQuad)
                     .SetUpdate(true);
    }

    public void OnPanelClick()
    {
        var mgr = FindAnyObjectByType<BenefitsManager>();
        if (mgr != null) mgr.OnPanelSelected(this);
        Selected?.Invoke(this);
        HideSelectedUp();
    }

    public void Hide()
    {
        if (!isVisible) return;
        isVisible = false;

        ApplyCountIncrease();

        canvasGroup.DOFade(0f, fadeDuration).SetUpdate(true);
        rectTransform
            .DOAnchorPosY(rectTransform.anchoredPosition.y - moveYAmount, fadeDuration)
            .SetEase(Ease.InQuad)
            .SetUpdate(true)
            .OnComplete(() =>
            {
                gameObject.SetActive(false);
                SetInitialState();
            });
    }

    public void HideAsOther(float distance, float duration)
    {
        if (!isVisible) return;
        isVisible = false;

        canvasGroup.DOFade(0f, duration).SetUpdate(true);
        rectTransform
            .DOAnchorPosY(rectTransform.anchoredPosition.y - Mathf.Abs(distance), duration)
            .SetEase(Ease.InQuad)
            .SetUpdate(true)
            .OnComplete(() =>
            {
                gameObject.SetActive(false);
                Destroy(gameObject);
            });
    }

    private void ApplyCountIncrease()
    {
        if (cardDatabase == null || string.IsNullOrEmpty(characterName))
        {
            Debug.LogError("CardDatabase もしくは characterName が未設定です。", this);
            return;
        }
        var deck = cardDatabase.GetCardData(characterName);
        if (deck == null) return;

        deck.count = Mathf.Max(0, deck.count) + 1;
    }

    public void HideSelectedUp()
    {
        if (!isVisible) return;
        isVisible = false;
        ApplyCountIncrease();
        canvasGroup.DOFade(0f, fadeDuration).SetUpdate(true);
        rectTransform
            .DOAnchorPosY(rectTransform.anchoredPosition.y + moveYAmount, fadeDuration)
            .SetEase(Ease.OutQuad)
            .SetUpdate(true)
            .OnComplete(() => Destroy(gameObject));
    }
}