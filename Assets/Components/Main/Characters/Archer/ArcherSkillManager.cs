using UnityEngine;
using System.Linq;

/// <summary>
/// アーチャーのスキル発動を管理する。
/// Updateメソッドで自身の位置を常に把握し、UseSkill()で攻撃を実行する。
/// </summary>
public class ArcherSkillManager : MonoBehaviour
{
    [Header("スキル設定")]
    [Tooltip("スキルで与えるダメージ ATK * 倍率。0以下の場合はキャラクター自身のATKを使用します。")]
    [SerializeField] private float skillDamageMagnification = 0;

    private PlacedCharacter _placedCharacter;
    private RopeManager _ropeManager; // ターゲット検索のためにRopeManagerの参照は維持

    private enum ColumnSide { None, Left, Right }
    private ColumnSide _currentColumn = ColumnSide.None;

    void Awake()
    {
        _placedCharacter = GetComponent<PlacedCharacter>();
        if (_placedCharacter == null)
        {
            Debug.LogError("ArcherSkillManagerはPlacedCharacterと同じGameObjectにアタッチされる必要があります！", this);
        }
        // RopeManagerはGetComponentInParentではなくFindObjectOfTypeでシーン全体から探す方が確実かもしれません
        // 今回はターゲット検索で必要なので、GetComponentInParentのままにしておきます
        _ropeManager = GetComponentInParent<RopeManager>();
    }

    void Update()
    {
        // ▼▼▼ このメソッド内のロジックをBuffManagerと統一 ▼▼▼
        
        // 1. 基準となる軸（親の親＝Ropeオブジェクト）を取得
        Transform axisTransform = transform.parent?.parent;
        
        if (axisTransform == null)
        {
            _currentColumn = ColumnSide.None;
            return;
        }

        // 2. 軸のX座標と自身のX座標を比較して左右を判定
        if (transform.position.x <= axisTransform.position.x)
        {
            _currentColumn = ColumnSide.Left;
        }
        else
        {
            _currentColumn = ColumnSide.Right;
        }
    }

    public void ActivateArcherSkill()
    {
        if (_placedCharacter == null || _ropeManager == null)
        {
            Debug.LogWarning("必要なコンポーネントが見つからないため、アーチャースキルを中断しました。");
            return;
        }

        Debug.Log($"<color=orange>ARCHER SKILL: {_placedCharacter.name}がアーチャーのスキル（最後尾狙撃）を発動！</color>");

        PlacedEnemy targetEnemy = GetRearmostEnemyInColumn();
        if (targetEnemy == null)
        {
            Debug.Log("攻撃対象の敵が見つかりませんでした。", this);
            return;
        }

        int damage = (skillDamageMagnification > 0) ? (int)(_placedCharacter.atk * skillDamageMagnification) : _placedCharacter.atk;
        Debug.Log($"ターゲット「{targetEnemy.name}」に {damage} ダメージを与えます。");
        targetEnemy.TakeDamage(damage);
    }

    /// <summary>
    /// 自分と同じサイド（左右）の敵の中から、最も後ろの列にいる生存中の敵を探して返す
    /// </summary>
    private PlacedEnemy GetRearmostEnemyInColumn()
    {
        if (_currentColumn == ColumnSide.None) return null;

        for (int i = _ropeManager.enemyRows.Count - 1; i >= 0; i--)
        {
            EnemyRow enemyRow = _ropeManager.enemyRows[i];
            GameObject potentialTargetGO = null;

            if (_currentColumn == ColumnSide.Left)
            {
                potentialTargetGO = enemyRow.leftEnemy;
            }
            else // _currentColumn == ColumnSide.Right
            {
                potentialTargetGO = enemyRow.rightEnemy;
            }

            if (potentialTargetGO != null)
            {
                PlacedEnemy placedEnemy = potentialTargetGO.GetComponent<PlacedEnemy>();
                if (placedEnemy != null && _ropeManager.AliveEnemies.Contains(placedEnemy))
                {
                    return placedEnemy;
                }
            }
        }
        
        return null;
    }
}