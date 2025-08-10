using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;
using DG.Tweening;
using TMPro;

/// <summary>
/// 【最終版 v4】オブジェクトを右クリックでステータスポップアップを表示します。
/// isStaticScaleで、カメラズーム時の挙動（常にサイズ一定 or ワールドサイズ一定）を切り替えられます。
/// </summary>
public class PopupManager : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
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
    [Tooltip("trueにすると、カメラの拡大率に関わらず画面上のポップアップの大きさが常に一定になります。")]
    [SerializeField] private bool isStaticScale = false;

    // --- データベース参照 ---
    private CharacterDatabase _characterDatabase;
    private EnemyDatabase _enemyDatabase;
    private CardDatabase _cardDatabase;
    
    // --- 内部変数 ---
    private GameObject _popupInstance;
    private Camera _mainCamera;
    private Transform _popupParentTransform;
    private RectTransform _popupRectTransform;
    
    private Vector3 _initialCameraPosition;
    private float _initialOrthographicSize;


    void Awake()
    {
        _mainCamera = Camera.main;
        GameObject parentGO = GameObject.Find("PopupContainer");
        if (parentGO != null)
        {
            _popupParentTransform = parentGO.transform;
        }
        else
        {
            Debug.LogWarning("ポップアップの親となる 'PopupContainer' という名前のGameObjectが見つかりません！", this);
        }
    }
    
    void Start()
    {
        InitializeDatabases();
    }

    private void InitializeDatabases()
    {
        _characterDatabase = FindAnyObjectByType<CharacterDatabase>();
        _enemyDatabase = FindAnyObjectByType<EnemyDatabase>();
        _cardDatabase = FindAnyObjectByType<CardDatabase>();
        
        // データベースのnullチェックを追加
        if (_characterDatabase == null)
            Debug.LogError("CharacterDatabaseが見つかりません！", this);
        if (_enemyDatabase == null)
            Debug.LogError("EnemyDatabaseが見つかりません！", this);
        if (_cardDatabase == null)
            Debug.LogError("CardDatabaseが見つかりません！", this);
        if (string.IsNullOrEmpty(characterName))
            Debug.LogError("characterNameが設定されていません！", this);
    }

    void Update()
    {
        if (_mainCamera == null) return;
        
        if (_popupInstance != null)
        {
            // --- ポップアップを閉じる条件 ---
            bool cameraMoved = _mainCamera.transform.position != _initialCameraPosition;
            // isStaticScaleがfalseの時だけ、ズームでも閉じる
            bool hideOnZoom = !isStaticScale && (_mainCamera.orthographicSize != _initialOrthographicSize);

            if (cameraMoved || hideOnZoom)
            {
                HidePopup();
                return; // 閉じた場合は以降の処理をしない
            }

            // isStaticScaleがtrueの時は、リアルタイムでのスケール追従は不要なため処理を削除
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Right)
        {
            ShowPopup();
        }
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Right)
        {
            HidePopup();
        }
    }
    
    private void ShowPopup()
    {
        if (_popupInstance != null) return;

        _initialCameraPosition = _mainCamera.transform.position;
        _initialOrthographicSize = _mainCamera.orthographicSize;
        
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
            if(_popupRectTransform != null) DOTween.Kill(_popupRectTransform, true);
            Destroy(_popupInstance);
            _popupInstance = null;
            _popupRectTransform = null;
        }
    }

    private void PopulatePopupData(TMP_Text nameText, TMP_Text atkValue, TMP_Text hpValue, TMP_Text skillText)
    {
        // データベースが初期化されていない場合は、再度取得を試行
        if (_cardDatabase == null || _characterDatabase == null)
        {
            Debug.LogWarning("必要なデータベースが初期化されていません。再度取得を試行します。", this);
            InitializeDatabases();
            
            // 再度チェック
            if (_cardDatabase == null || _characterDatabase == null)
            {
                Debug.LogError("必要なデータベースが初期化されていません！", this);
                return;
            }
        }
        
        if (isEnemy)
        {
            if (_enemyDatabase == null)
            {
                Debug.LogWarning("EnemyDatabaseが初期化されていません。再度取得を試行します。", this);
                _enemyDatabase = FindAnyObjectByType<EnemyDatabase>();
                
                if (_enemyDatabase == null)
                {
                    Debug.LogError("EnemyDatabaseが初期化されていません！", this);
                    return;
                }
            }
            
            var stats = _enemyDatabase.GetStats(characterName);
            if (stats == null) return;
            nameText.text = !string.IsNullOrEmpty(stats.displayName) ? stats.displayName : stats.enemyName;
            atkValue.text = stats.atk.ToString();
            hpValue.text = stats.hp.ToString();
            skillText.text = stats.skillDescription;
        }
        else 
        {
            var deckData = _cardDatabase.GetCardData(characterName);
            if (deckData == null) return;
            var characterData = _characterDatabase.GetCharacterData(characterName);
            var stats = _characterDatabase.GetStats(characterName, deckData.level);
            if (stats == null || characterData == null) return;
            nameText.text = !string.IsNullOrEmpty(characterData.displayName) ? characterData.displayName : characterData.characterName;
            atkValue.text = stats.atk.ToString();
            hpValue.text = stats.hp.ToString();
            skillText.text = stats.skillDescription;
        }
    }
    
    private float GetCurrentScaleFactor()
    {
        // ★変更点: isStaticScaleがtrueの時は、常にスケール1.0を返す
        if (isStaticScale)
        {
            return 1.0f;
        }

        // isStaticScaleがfalseの時のみ、カメラの拡大率に応じた計算を行う
        if (_mainCamera.orthographic)
        {
            // ワールド空間でのサイズを一定に保つための計算式
            return baseOrthographicSize / _mainCamera.orthographicSize;
        }
        
        return 1.0f;
    }
}