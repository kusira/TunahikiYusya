using UnityEngine;
using UnityEngine.InputSystem;
using DG.Tweening;
using TMPro;

/// <summary>
/// 【最終版】オブジェクトを右クリックでステータスポップアップを表示します。
/// カメラの拡大率にリアルタイムで対応し、データベースに設定された表示名（DisplayName）を使用します。
/// </summary>
public class PopupManager : MonoBehaviour
{
    [Header("ポップアップ設定")]
    [Tooltip("表示するキャラクター/敵の名前（データベース上のID名）")]
    [SerializeField] private string characterName;
    [Tooltip("このオブジェクトが敵キャラクターならチェックを入れる")]
    [SerializeField] private bool isEnemy;
    [Tooltip("表示するポップアップのプレハブ（UI要素であること）")]
    [SerializeField] private GameObject popupPrefab;

    [Header("表示アニメーション設定")]
    [SerializeField] private float popupYOffset = 1.0f;
    [SerializeField] private float popupMagnification = 1.1f;
    [SerializeField] private float appearDuration = 0.2f;
    [SerializeField] private float appearMoveDistance = 50f;
    
    [Header("カメラ設定")]
    [Tooltip("ポップアップのスケールが1.0になる、カメラの基準Orthographic Size")]
    [SerializeField] private float baseOrthographicSize = 5.0f;

    // --- データベース参照 ---
    private CharacterDatabase _characterDatabase;
    private EnemyDatabase _enemyDatabase;
    private CardDatabase _cardDatabase;
    
    // --- 内部変数 ---
    private GameObject _popupInstance;
    private Camera _mainCamera;
    private Transform _popupParentTransform;
    private RectTransform _popupRectTransform;

    void Awake()
    {
        _mainCamera = Camera.main;
    }
    
    void Start()
    {
        GameObject parentGO = GameObject.Find("PopUpManager");
        if (parentGO != null)
        {
            _popupParentTransform = parentGO.transform;
        }
        else
        {
            Debug.LogWarning("ポップアップの親となる 'PopUpManager' という名前のGameObjectが見つかりません！", this);
        }

        // シーン内の各データベースを自動で取得
        _characterDatabase = FindAnyObjectByType<CharacterDatabase>();
        _enemyDatabase = FindAnyObjectByType<EnemyDatabase>();
        _cardDatabase = FindAnyObjectByType<CardDatabase>();
    }

    void Update()
    {
        if (Mouse.current == null || _mainCamera == null) return;

        // --- 右クリック押下でポップアップ表示 ---
        if (Mouse.current.rightButton.wasPressedThisFrame)
        {
            RaycastHit2D hit = Physics2D.Raycast(_mainCamera.ScreenToWorldPoint(Mouse.current.position.ReadValue()), Vector2.zero);
            if (hit.collider != null && hit.collider.gameObject == this.gameObject)
            {
                ShowPopup();
            }
        }

        // --- 右クリック解放でポップアップ非表示 ---
        if (Mouse.current.rightButton.wasReleasedThisFrame)
        {
            HidePopup();
        }
        
        // --- ポップアップ表示中にカメラの拡大率をリアルタイムで反映 ---
        if (_popupInstance != null && _popupRectTransform != null)
        {
            // 初回のアニメーション中はDOTweenに任せる
            if (!DOTween.IsTweening(_popupRectTransform))
            {
                UpdatePopupScale();
            }
        }
    }
    
    private void ShowPopup()
    {
        if (_popupInstance != null) return;
        
        _popupInstance = Instantiate(popupPrefab, _popupParentTransform);
        var canvasGroup = _popupInstance.GetComponent<CanvasGroup>();
        _popupRectTransform = _popupInstance.GetComponent<RectTransform>(); 
        
        TMP_Text nameText = _popupInstance.transform.Find("NameText")?.GetComponent<TMP_Text>();
        TMP_Text atkValue = _popupInstance.transform.Find("ATKValue")?.GetComponent<TMP_Text>();
        TMP_Text hpValue = _popupInstance.transform.Find("HPValue")?.GetComponent<TMP_Text>();
        TMP_Text skillText = _popupInstance.transform.Find("SkillText")?.GetComponent<TMP_Text>();

        if (canvasGroup == null || _popupRectTransform == null || nameText == null || atkValue == null || hpValue == null || skillText == null)
        {
            Debug.LogError("ポップアッププレハブに必要なコンポーネントや子オブジェクトが不足しています。", _popupInstance);
            Destroy(_popupInstance);
            return;
        }
        
        PopulatePopupData(nameText, atkValue, hpValue, skillText);
        
        float scaleFactor = GetCurrentScaleFactor();

        Vector3 worldPosition = transform.position + new Vector3(0, popupYOffset, 0);
        Vector2 screenPosition = _mainCamera.WorldToScreenPoint(worldPosition);
        _popupRectTransform.position = screenPosition;
        
        Vector2 startPosition = (Vector2)_popupRectTransform.localPosition - new Vector2(0, appearMoveDistance);
        _popupRectTransform.localPosition = startPosition;

        _popupRectTransform.localScale = Vector3.one * scaleFactor;
        canvasGroup.alpha = 0;

        // アニメーションを実行
        _popupRectTransform.DOAnchorPos((Vector2)_popupRectTransform.localPosition + new Vector2(0, appearMoveDistance), appearDuration).SetEase(Ease.OutCubic);
        _popupRectTransform.DOScale(popupMagnification * scaleFactor, appearDuration).SetEase(Ease.OutCubic);
        canvasGroup.DOFade(1, appearDuration);
    }

    private void HidePopup()
    {
        if (_popupInstance != null)
        {
            if(_popupRectTransform != null) DOTween.Kill(_popupRectTransform, true); // アニメーションを安全に停止

            Destroy(_popupInstance);
            _popupInstance = null;
            _popupRectTransform = null; // 参照をクリア
        }
    }

    private void PopulatePopupData(TMP_Text nameText, TMP_Text atkValue, TMP_Text hpValue, TMP_Text skillText)
    {
        if (isEnemy)
        {
            var stats = _enemyDatabase.GetStats(characterName);
            if (stats == null) return;

            // DisplayNameが設定されていればそれを、なければ内部名(enemyName)を使用
            nameText.text = !string.IsNullOrEmpty(stats.displayName) ? stats.displayName : stats.enemyName;
            
            atkValue.text = stats.atk.ToString();
            hpValue.text = stats.hp.ToString();
            skillText.text = stats.skillDescription;
        }
        else // isCharacter
        {
            var deckData = _cardDatabase.GetCardData(characterName);
            if (deckData == null) return;

            var characterData = _characterDatabase.GetCharacterData(characterName);
            var stats = _characterDatabase.GetStats(characterName, deckData.level);

            if (stats == null || characterData == null) return;

            // DisplayNameが設定されていればそれを、なければ内部名(characterName)を使用
            nameText.text = !string.IsNullOrEmpty(characterData.displayName) ? characterData.displayName : characterData.characterName;
            
            atkValue.text = stats.atk.ToString();
            hpValue.text = stats.hp.ToString();
            skillText.text = stats.skillDescription;
        }
    }

    /// <summary>
    /// 現在のカメラ拡大率に応じたポップアップのスケール係数を取得します。
    /// </summary>
    private float GetCurrentScaleFactor()
    {
        if (_mainCamera.orthographic)
        {
            // orthographicSizeが大きい(ズームアウト)ほど、スケールは小さくなる
            return baseOrthographicSize / _mainCamera.orthographicSize;
        }
        return 1.0f;
    }

    /// <summary>
    /// ポップアップのスケールを現在のカメラ拡大率に合わせて更新します。
    /// </summary>
    private void UpdatePopupScale()
    {
        float scaleFactor = GetCurrentScaleFactor();
        // アニメーション後の最終的なスケールを適用
        _popupRectTransform.localScale = Vector3.one * scaleFactor * popupMagnification;
    }
}