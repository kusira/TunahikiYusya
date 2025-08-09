using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using TMPro;

public class UnlockPanelManager : MonoBehaviour
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
    [SerializeField] private Image characterImage;  // Character
    [SerializeField] private TMP_Text nameText;     // Name
    [SerializeField] private TMP_Text nextATKText;  // NextATK
    [SerializeField] private TMP_Text nextHPText;   // NextHP
    [SerializeField] private TMP_Text skillText;

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

        if (characterImage == null) Debug.LogError("characterImage (Character) が未アサインです。", this);
        if (nameText == null) Debug.LogError("nameText (Name) が未アサインです。", this);
        if (nextATKText == null) Debug.LogError("nextATKText (NextATK) が未アサインです。", this);
        if (nextHPText == null) Debug.LogError("nextHPText (NextHP) が未アサインです。", this);
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

    public bool CanShow()
    {
        if (cardDatabase == null || string.IsNullOrEmpty(characterName)) return false;
        var data = cardDatabase.GetCardData(characterName);
        if (data == null) return false;
        return data.level == 0;
    }

    public void Show()
    {
        if (string.IsNullOrEmpty(characterName))
        {
            Debug.LogError("characterName が空です。先に CharacterName を設定してください。", this);
            return;
        }
        if (!CanShow())
        {
            Debug.Log($"「{characterName}」は未解放ではないためアンロックパネルは表示されません。", this);
            return;
        }

        if (!SetPanelData(characterName)) return;

        gameObject.SetActive(true);
        DOVirtual.DelayedCall(displayDelay, AnimateShow);
    }

    public void Show(string name)
    {
        CharacterName = name;
        Show();
    }

    private bool SetPanelData(string name)
    {
        if (characterDatabase == null) return false;

        var characterData = characterDatabase.GetCharacterData(name);
        if (characterData == null)
        {
            Debug.LogError($"キャラクター「{name}」のデータが見つかりません。", this);
            return false;
        }

        var lvl1Stats = characterDatabase.GetStats(name, 1);
        if (lvl1Stats == null)
        {
            Debug.LogError($"「{name}」レベル1のデータが見つかりません。", this);
            return false;
        }

        if (characterImage != null)
        {
            if (characterData.characterSprite != null) characterImage.sprite = characterData.characterSprite;
            else Debug.LogWarning($"「{name}」にスプライトが設定されていません。", this);
        }

        if (nameText != null)
            nameText.text = string.IsNullOrEmpty(characterData.displayName) ? characterData.characterName : characterData.displayName;

        if (nextATKText != null) nextATKText.text = lvl1Stats.atk.ToString();
        if (nextHPText != null) nextHPText.text = lvl1Stats.hp.ToString();

        if (nextATKText != null) nextATKText.text = lvl1Stats.atk.ToString();
        if (nextHPText != null) nextHPText.text = lvl1Stats.hp.ToString();

        if (skillText != null) skillText.text = lvl1Stats.skillDescription;

        return true;
    }

    private void AnimateShow()
    {
        isVisible = true;
        canvasGroup.DOFade(1f, fadeDuration);
        rectTransform.DOAnchorPosY(rectTransform.anchoredPosition.y + moveYAmount, fadeDuration).SetEase(Ease.OutQuad);
    }

    public void Hide()
    {
        if (!isVisible) return;
        isVisible = false;

        ApplyUnlockToCardDatabase();

        canvasGroup.DOFade(0f, fadeDuration);
        rectTransform
            .DOAnchorPosY(rectTransform.anchoredPosition.y - moveYAmount, fadeDuration)
            .SetEase(Ease.InQuad)
            .OnComplete(() =>
            {
                gameObject.SetActive(false);
                SetInitialState();
            });
    }

    public void OnPanelClick()
    {
        var mgr = FindAnyObjectByType<BenefitsManager>();
        if (mgr != null) mgr.OnPanelSelected(this);
        Hide();

        Selected?.Invoke(this);
        Hide();
    }

    public void HideAsOther(float distance, float duration)
    {
        Debug.Log("aaa");
        if (!isVisible) return;
        isVisible = false;

        canvasGroup.DOFade(0f, duration);
        rectTransform
            .DOAnchorPosY(rectTransform.anchoredPosition.y - Mathf.Abs(distance), duration)
            .SetEase(Ease.InQuad)
            .OnComplete(() =>
            {
                gameObject.SetActive(false);
                Destroy(gameObject);
            });
    }

    private void ApplyUnlockToCardDatabase()
    {
        if (cardDatabase == null || string.IsNullOrEmpty(characterName))
        {
            Debug.LogError("CardDatabase もしくは characterName が未設定です。", this);
            return;
        }

        var data = cardDatabase.GetCardData(characterName);
        if (data == null)
        {
            Debug.LogError($"CardDatabaseに「{characterName}」が見つかりません。", this);
            return;
        }

        if (data.count == 0)
        {
            cardDatabase.UnlockCard(characterName);
        }

        data.level = 1;
        data.count = 1;
    }

}