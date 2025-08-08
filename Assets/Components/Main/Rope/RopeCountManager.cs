using UnityEngine;
using TMPro;
using DG.Tweening;

/// <summary>
/// 各ロープにアタッチし、陣地に入った際のカウント処理、UI更新、自身の消滅を管理します。
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class RopeCountManager : MonoBehaviour
{
    [Header("アニメーション設定")]
    [Tooltip("カウントアップ時にUIテキストが拡大する倍率")]
    [SerializeField] private float textPunchScale = 1.5f;
    [Tooltip("テキストの拡大アニメーションにかかる時間")]
    [SerializeField] private float textPunchDuration = 0.3f;
    [Tooltip("ロープがフェードアウトして消えるまでの時間")]
    [SerializeField] private float fadeOutDuration = 1.0f;

    // --- 外部から参照可能なstatic変数 ---
    /// <summary>味方が獲得したロープの数</summary>
    public static int AlliedRopeCount { get; private set; }
    /// <summary>敵が獲得したロープの数</summary>
    public static int EnemyRopeCount { get; private set; }

    // --- 内部で利用する変数 ---
    private TextMeshProUGUI alliedCountText;
    private TextMeshProUGUI enemyCountText;
    private bool isCaptured = false; // このロープが獲得済みかどうかのフラグ

    void Start()
    {
        // シーンからUIオブジェクトを名前で検索し、コンポーネントを取得
        var alliedCountObject = GameObject.Find("AlliedRopeCount");
        if (alliedCountObject != null)
        {
            alliedCountText = alliedCountObject.GetComponent<TextMeshProUGUI>();
        }

        var enemyCountObject = GameObject.Find("EnemyRopeCount");
        if (enemyCountObject != null)
        {
            enemyCountText = enemyCountObject.GetComponent<TextMeshProUGUI>();
        }

        // UIオブジェクトが見つからない場合はエラーを出す
        if (alliedCountText == null || enemyCountText == null)
        {
            Debug.LogError("UIオブジェクト 'AlliedRopeCount' または 'EnemyRopeCount' が見つかりません。");
        }
    }

    /// <summary>
    /// トリガーとなるColliderに侵入した時の処理
    /// </summary>
    private void OnTriggerEnter2D(Collider2D other)
    {
        // 既に獲得済みの場合は何もしない
        if (isCaptured) return;

        // 味方の陣地に入った場合
        if (other.CompareTag("AlliedBase"))
        {
            isCaptured = true; // 獲得済みにする
            AlliedRopeCount++; // 味方のカウントを増やす
            UpdateUICount(alliedCountText, AlliedRopeCount); // UIを更新
            FadeOutAndDestroy(); // オブジェクトを消す
        }
        // 敵の陣地に入った場合
        else if (other.CompareTag("EnemyBase"))
        {
            isCaptured = true; // 獲得済みにする
            EnemyRopeCount++; // 敵のカウントを増やす
            UpdateUICount(enemyCountText, EnemyRopeCount); // UIを更新
            FadeOutAndDestroy(); // オブジェクトを消す
        }
    }

    /// <summary>
    /// 指定されたUIテキストを更新し、アニメーションさせます。
    /// </summary>
    private void UpdateUICount(TextMeshProUGUI textElement, int count)
    {
        if (textElement == null) return;
        
        // テキストの数値を更新
        textElement.text = count.ToString();
        // テキストを拡大させてから元に戻すアニメーション
        textElement.transform.DOPunchScale(Vector3.one * textPunchScale, textPunchDuration, 1, 0.5f);
    }

    /// <summary>
    /// このロープオブジェクト（子も含む）をフェードアウトさせてから破棄します。
    /// </summary>
    private void FadeOutAndDestroy()
    {
        // このオブジェクトと子オブジェクトに含まれる全てのSpriteRendererを取得
        SpriteRenderer[] allRenderers = GetComponentsInChildren<SpriteRenderer>();
        
        if (allRenderers.Length > 0)
        {
            // 全てのSpriteRendererを同時にフェードアウトさせる
            foreach (var renderer in allRenderers)
            {
                renderer.DOFade(0, fadeOutDuration * Time.timeScale);
            }
            
            // アニメーション完了後にGameObjectを破棄する
            // （いずれか一つのDOFadeにOnCompleteを追加すればOK）
            allRenderers[0].DOFade(0, fadeOutDuration * Time.timeScale).OnComplete(() =>
            {
                Destroy(gameObject);
            });
        }
        else
        {
            // レンダラーがない場合は即座に破棄
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// ゲーム開始時などに外部から呼び出し、カウントをリセットします。
    /// </summary>
    public static void ResetCounts()
    {
        AlliedRopeCount = 0;
        EnemyRopeCount = 0;
    }
}