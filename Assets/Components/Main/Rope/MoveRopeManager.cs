using UnityEngine;

/// <summary>
/// 綱（Rope）の移動を管理するクラス。
/// </summary>
public class MoveRopeManager : MonoBehaviour
{
    [Header("参照")]
    [Tooltip("シーン内のRopeManagerをアサインしてください")]
    [SerializeField] private RopeManager ropeManager;

    // ★変更: Inspectorでのアサインが不要になったため、SerializeField属性を削除
    private BattleBeginsManager battleBeginsManager;

    [Tooltip("実際に移動させるロープのTransformをアサインしてください")]
    [SerializeField] private Transform ropeTransform;

    [Header("移動設定")]
    [Tooltip("力の差に掛ける移動速度の係数")]
    [SerializeField] private float moveSpeedMultiplier = 0.1f;

    // ★追加: Awakeメソッドでコンポーネントを自動検索
    void Awake()
    {
        // シーン内からBattleBeginsManagerを検索して取得する
        battleBeginsManager = FindAnyObjectByType<BattleBeginsManager>();
        
        // もし見つからなかった場合は、エラーログを出力して知らせる
        if (battleBeginsManager == null)
        {
            Debug.LogError("シーン内に BattleBeginsManager が見つかりませんでした！");
        }
    }

    void Update()
    {
        // 必要なコンポーネントが設定されていなければ、処理を中断
        if (ropeManager == null || battleBeginsManager == null || ropeTransform == null)
        {
            return;
        }

        // 戦闘が開始されていない場合は、ロープは移動しない
        if (!battleBeginsManager.IsInBattle)
        {
            return;
        }

        // --- ここからがロープの移動計算 ---

        // RopeManagerから最新のATK合計値を取得
        int alliedAtk = ropeManager.TotalAlliedAtk;
        int enemyAtk = ropeManager.TotalEnemyAtk;

        // ATKの差を計算（敵が優勢だとプラス、味方が優勢だとマイナスになる）
        int diff = enemyAtk - alliedAtk;
        
        // 差がなければ移動しない
        if (diff == 0)
        {
            return;
        }

        // 移動方向を決定（diffがプラスなら1、マイナスなら-1）
        float direction = Mathf.Sign(diff);

        // 移動速度を計算（差の絶対値の平方根に係数を掛ける）
        float speed = Mathf.Sqrt(Mathf.Abs(diff)) * moveSpeedMultiplier;

        // このフレームでの移動量を計算
        float moveAmount = direction * speed * Time.deltaTime;

        // ロープのY軸を移動させる
        ropeTransform.Translate(0, moveAmount, 0);
    }
}
