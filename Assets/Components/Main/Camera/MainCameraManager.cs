using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;

/// <summary>
/// カメラの移動（パン）、拡縮（ズーム）、移動範囲の制限、
/// そしてカメラの動きに影響されない静的オブジェクトの制御をまとめて行う管理クラス。
/// </summary>
public class MainCameraManager : MonoBehaviour
{
    [Header("必須コンポーネント")]
    [Tooltip("操作対象のメインカメラ")]
    [SerializeField] private Camera mainCamera;

    [Header("カメラ移動（パン）設定")]
    [Tooltip("このGameObjectをクリック＆ドラッグするとカメラが移動する（背景など）")]
    [SerializeField] private GameObject dragTriggerObject;

    [Header("カメラ拡縮（ズーム）設定")]
    [Tooltip("マウスホイールでのズーム速度。50～100程度の値がおすすめです")]
    [SerializeField] private float zoomSpeed = 50f;
    [Tooltip("最もズームインした時のサイズ（小さいほど近い）")]
    [SerializeField] private float minZoomSize = 3f;
    [Tooltip("最もズームアウトした時のサイズ（大きいほど遠い）")]
    [SerializeField] private float maxZoomSize = 10f;

    [Header("移動範囲設定")]
    [Tooltip("カメラが移動できる範囲の最小座標 (ワールド座標の左下)")]
    [SerializeField] private Vector2 minBounds;
    [Tooltip("カメラが移動できる範囲の最大座標 (ワールド座標の右上)")]
    [SerializeField] private Vector2 maxBounds;

    [Header("静的オブジェクト設定")]
    [Tooltip("カメラの動きに影響されず、画面上の同じ位置とサイズを保つオブジェクトのリスト")]
    [SerializeField] private List<Transform> staticObjects;
    
    // --- 内部で使う変数 ---
    // 現在カメラをドラッグ中かどうかのフラグ
    private bool isCameraDragging = false;
    // 前のフレームのマウスのワールド座標を保持
    private Vector3 lastMousePosition;
    
    // 静的オブジェクトの初期状態を保存するための変数
    // 起動時のカメラのorthographicSize
    private float initialOrthographicSize;
    // 各静的オブジェクトの、画面上の相対位置（ビューポート座標）を保持
    private Dictionary<Transform, Vector3> staticObjectViewportPositions = new Dictionary<Transform, Vector3>();
    // 各静的オブジェクトの、初期スケールを保持
    private Dictionary<Transform, Vector3> staticObjectInitialScales = new Dictionary<Transform, Vector3>();

    /// <summary>
    /// Startよりも先に一度だけ呼ばれる初期化処理
    /// </summary>
    void Awake()
    {
        // mainCameraがインスペクターから設定されていなければ、自動でシーンのメインカメラを探す
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
        }
    }

    /// <summary>
    /// ゲーム開始時に一度だけ呼ばれる処理
    /// </summary>
    void Start()
    {
        // 静的オブジェクトの拡縮を計算するために、起動時のカメラサイズを記録
        initialOrthographicSize = mainCamera.orthographicSize;

        // 起動時に、静的オブジェクトの画面上の相対位置と初期スケールを記録しておく
        foreach (Transform objTransform in staticObjects)
        {
            if (objTransform != null)
            {
                // World座標をViewport座標（画面の左下(0,0)から右上(1,1)の相対座標）に変換して記録
                staticObjectViewportPositions[objTransform] = mainCamera.WorldToViewportPoint(objTransform.position);
                // 初期スケールを記録
                staticObjectInitialScales[objTransform] = objTransform.localScale;
            }
        }
    }

    /// <summary>
    /// 毎フレーム呼ばれる更新処理
    /// </summary>
    void Update()
    {
        // マウスが利用できない場合は何もしない
        if (Mouse.current == null) return;
        
        // 各フレームでズームとパン（移動）の入力を処理
        HandleZoom();
        HandlePan();
    }

    /// <summary>
    /// 全てのUpdate処理が終わった後に呼ばれる更新処理。
    /// カメラの最終的な位置を確定させたり、追従するオブジェクトの処理に適しています。
    /// </summary>
    void LateUpdate()
    {
        // Updateで行われたパンやズームの結果を、指定した範囲内に収まるように補正する
        ClampCameraPosition();
        
        // 最終的に確定したカメラの位置とサイズに合わせて、静的オブジェクトの位置とサイズを更新する
        UpdateStaticObjects();
    }

    /// <summary>
    /// マウスホイールによるズーム処理
    /// </summary>
    private void HandleZoom()
    {
        float scroll = Mouse.current.scroll.ReadValue().y;
        if (scroll == 0) return; // スクロール入力がなければ何もしない
        
        // スクロール方向（-1 or 1）を取得
        float scrollDirection = Mathf.Sign(scroll); 
        float currentSize = mainCamera.orthographicSize;
        // ズーム速度と時間経過を考慮して、カメラの表示サイズを更新
        currentSize -= scrollDirection * zoomSpeed * Time.deltaTime;

        // 設定された最小・最大サイズを超えないように値を制限（クランプ）
        mainCamera.orthographicSize = Mathf.Clamp(currentSize, minZoomSize, maxZoomSize);
    }

    /// <summary>
    /// マウスドラッグによるカメラの移動（パン）処理
    /// </summary>
    private void HandlePan()
    {
        // 他のカードなどをドラッグ中は、カメラを移動させない
        if (DragAndDropCharacterManager.IsDragging)
        {
            isCameraDragging = false; // 念のためフラグをリセット
            return;
        }

        // マウスの左ボタンが押された瞬間
        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            // マウスカーソルがdragTriggerObjectの上にあるかチェック
            RaycastHit2D hit = Physics2D.Raycast(mainCamera.ScreenToWorldPoint(Mouse.current.position.ReadValue()), Vector2.zero);
            if (hit.collider != null && hit.collider.gameObject == dragTriggerObject)
            {
                // 条件を満たせば、カメラドラッグを開始
                isCameraDragging = true;
                // ドラッグ開始時のマウス位置を記録
                lastMousePosition = mainCamera.ScreenToWorldPoint(Mouse.current.position.ReadValue());
            }
        }

        // マウスの左ボタンが離された瞬間
        if (Mouse.current.leftButton.wasReleasedThisFrame)
        {
            // カメラドラッグを終了
            isCameraDragging = false;
        }

        // カメラをドラッグ中の場合
        if (isCameraDragging)
        {
            // 現在のマウス位置との差分を計算
            Vector3 currentMousePosition = mainCamera.ScreenToWorldPoint(Mouse.current.position.ReadValue());
            Vector3 delta = currentMousePosition - lastMousePosition;
            
            // マウスの動きと逆方向にカメラを動かすことで、掴んだ地点がマウスに追従するように見せる
            mainCamera.transform.position -= delta;
        }
    }
    
    /// <summary>
    /// カメラの位置を、インスペクターで設定された範囲内に制限（クランプ）する
    /// </summary>
    private void ClampCameraPosition()
    {
        // 現在のズームレベルから、カメラの表示範囲（高さと幅）をワールド単位で計算
        float camHeight = mainCamera.orthographicSize * 2;
        float camWidth = camHeight * mainCamera.aspect;

        // カメラの表示範囲を考慮して、カメラの「中心座標」が動ける実際の境界を計算
        float clampedMinX = minBounds.x + camWidth / 2;
        float clampedMaxX = maxBounds.x - camWidth / 2;
        float clampedMinY = minBounds.y + camHeight / 2;
        float clampedMaxY = maxBounds.y - camHeight / 2;

        Vector3 camPos = mainCamera.transform.position;
        
        // 計算した境界内に、現在のカメラのX, Y座標を制限
        float newX = Mathf.Clamp(camPos.x, clampedMinX, clampedMaxX);
        float newY = Mathf.Clamp(camPos.y, clampedMinY, clampedMaxY);

        // ズームアウトしすぎて、表示範囲が移動範囲そのものより広くなった場合の補正
        if (clampedMinX > clampedMaxX)
        {
            // X軸の移動を許可せず、範囲の中央に固定
            newX = (minBounds.x + maxBounds.x) / 2;
        }
        if (clampedMinY > clampedMaxY)
        {
            // Y軸の移動を許可せず、範囲の中央に固定
            newY = (minBounds.y + maxBounds.y) / 2;
        }
        
        // 最終的に計算された座標でカメラの位置を更新
        mainCamera.transform.position = new Vector3(newX, newY, camPos.z);
    }

    /// <summary>
    /// 静的オブジェクトの位置と大きさを、現在のカメラに合わせて更新する
    /// </summary>
    private void UpdateStaticObjects()
    {
        // 現在のカメラサイズと初期サイズの比率を計算
        float scaleRatio = mainCamera.orthographicSize / initialOrthographicSize;

        foreach (var entry in staticObjectViewportPositions)
        {
            Transform objTransform = entry.Key;
            Vector3 targetViewportPos = entry.Value;

            if (objTransform != null)
            {
                // 1. 位置の更新：記録しておいた画面上の相対位置を、現在のカメラ基準のワールド座標に再変換して適用
                Vector3 newWorldPos = mainCamera.ViewportToWorldPoint(targetViewportPos);
                objTransform.position = newWorldPos;

                // 2. 大きさの更新：記録しておいた初期スケールに、カメラサイズの比率を掛けて適用
                Vector3 initialScale = staticObjectInitialScales[objTransform];
                objTransform.localScale = initialScale * scaleRatio;
            }
        }
    }
}