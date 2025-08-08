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

    private RopeManager ropeManager;

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

        // RopeManagerの取得
        ropeManager = GetComponent<RopeManager>();
        if (ropeManager == null)
        {
#if UNITY_2023_1_OR_NEWER
            ropeManager = FindFirstObjectByType<RopeManager>();
#else
            ropeManager = FindObjectOfType<RopeManager>();
#endif
        }
    }

    /// <summary>
    /// トリガーとなるColliderに侵入した時の処理
    /// </summary>
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (isCaptured) return;

        if (other.CompareTag("AlliedBase"))
        {
            isCaptured = true;
            AlliedRopeCount++;
            UpdateUICount(alliedCountText, AlliedRopeCount);
            FadeOutAndDestroy();
        }
        else if (other.CompareTag("EnemyBase"))
        {
            isCaptured = true;
            EnemyRopeCount++;
            UpdateUICount(enemyCountText, EnemyRopeCount);
            FadeOutAndDestroy();
        }
    }

    /// <summary>
    /// 指定されたUIテキストを更新し、アニメーションさせます。
    /// </summary>
    private void UpdateUICount(TextMeshProUGUI textElement, int count)
    {
        if (textElement == null) return;

        textElement.text = count.ToString();
        textElement.transform.DOPunchScale(Vector3.one * textPunchScale, textPunchDuration, 1, 0.5f);
    }

    /// <summary>
    /// フェードアウトアニメーション後にオブジェクトを削除します。勝敗処理も含みます。
    /// </summary>
    private void FadeOutAndDestroy()
    {
        // 勝敗判定とDie処理
        if (ropeManager != null)
        {
            int alliedAtk = ropeManager.TotalAlliedAtk;
            int enemyAtk = ropeManager.TotalEnemyAtk;

            if (alliedAtk != enemyAtk)
            {
                bool isAlliedLoser = alliedAtk < enemyAtk;

                if (isAlliedLoser)
                {
                    foreach (var kvp in ropeManager.OccupiedHolderInfo)
                    {
                        GameObject character = kvp.Value;
                        if (character != null)
                        {
                            var characterManager = character.GetComponent<PlacedCharacter>();
                            if (characterManager != null)
                            {
                                characterManager.Die();
                            }
                        }
                    }
                }
                else
                {
                    foreach (var enemy in ropeManager.AliveEnemies)
                    {
                        if (enemy != null)
                        {
                            var enemyManager = enemy.GetComponent<PlacedEnemy>();
                            if (enemyManager != null)
                            {
                                enemyManager.Die();
                            }
                        }
                    }
                }
            }
        }

        // フェードアウト処理
        SpriteRenderer[] allRenderers = GetComponentsInChildren<SpriteRenderer>();

        if (allRenderers.Length > 0)
        {
            foreach (var renderer in allRenderers)
            {
                renderer.DOFade(0, fadeOutDuration * Time.timeScale);
            }

            allRenderers[0].DOFade(0, fadeOutDuration * Time.timeScale).OnComplete(() =>
            {
                Destroy(gameObject);
            });
        }
        else
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// ゲーム開始時などに外部から呼び出し、ロープのカウントをリセットします。
    /// </summary>
    public static void ResetCounts()
    {
        AlliedRopeCount = 0;
        EnemyRopeCount = 0;
    }
}
