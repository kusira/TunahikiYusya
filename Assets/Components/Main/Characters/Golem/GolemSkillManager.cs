using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// ゴーレムのスキル（生存中の敵がいる最前列の左右にダメージ）を管理します。
/// PlacedCharacterのUseSkill()から呼び出されることを想定しています。
/// </summary>
public class GolemSkillManager : MonoBehaviour
{
    [Header("スキル設定")]
    [Tooltip("ダメージ量 = ATK × この値。5なら5倍。")]
    [SerializeField] private float damageMultiplier = 5f;

    private PlacedCharacter _placedCharacter;
    private RopeManager _ropeManager;

    void Awake()
    {
        _placedCharacter = GetComponent<PlacedCharacter>();
        if (_placedCharacter == null)
        {
            Debug.LogError("GolemSkillManagerはPlacedCharacterと同じGameObjectにいる必要があります！", this);
        }

        // ▼▼▼ このブロックを修正 ▼▼▼
        // 自分自身の親の親（Ropeオブジェクト）からRopeManagerを取得
        if (transform.parent != null && transform.parent.parent != null)
        {
            _ropeManager = transform.parent.parent.GetComponent<RopeManager>();
        }
    }

    // ▼▼▼ Updateメソッドは不要になったため削除しました ▼▼▼

    /// <summary>
    /// PlacedCharacterから呼び出される、ゴーレム専用のスキル処理
    /// </summary>
    public void ActivateGolemSkill()
    {
        if (_placedCharacter == null || _ropeManager == null) return;
        
        int damage = (int)(_placedCharacter.atk * damageMultiplier);
        if (damage <= 0) return;

        Debug.Log($"<color=brown>GOLEM SKILL: {_placedCharacter.name}が最前列攻撃スキルを発動！ (ダメージ: {damage})</color>");

        // ▼▼▼ ターゲットの決定方法を修正 ▼▼▼

        // 1. 生存している敵がいる最初の行を探す
        EnemyRow targetRow = null;
        foreach (var row in _ropeManager.enemyRows)
        {
            // .?はnull安全演算子。もしleftEnemyがnullでもエラーにならない
            PlacedEnemy left = row.leftEnemy?.GetComponent<PlacedEnemy>();
            PlacedEnemy right = row.rightEnemy?.GetComponent<PlacedEnemy>();

            bool isLeftAlive = left != null && _ropeManager.AliveEnemies.Contains(left);
            bool isRightAlive = right != null && _ropeManager.AliveEnemies.Contains(right);

            // 左右どちらかに生存している敵がいたら、その行をターゲットとしてループを抜ける
            if (isLeftAlive || isRightAlive)
            {
                targetRow = row;
                break;
            }
        }

        // ターゲットとなる行が見つからなかった場合は処理を終了
        if (targetRow == null)
        {
            Debug.Log("攻撃対象の敵が見つかりませんでした。");
            return;
        }

        // 2. 攻撃対象（見つかった行の左と右）をリストにまとめる
        var targets = new List<GameObject> { targetRow.leftEnemy, targetRow.rightEnemy };

        // 3. 各ターゲットにダメージを与える
        foreach (var targetGO in targets)
        {
            if (targetGO == null) continue;

            PlacedEnemy placedEnemy = targetGO.GetComponent<PlacedEnemy>();
            
            // ターゲットが生存している場合のみダメージを与える
            if (placedEnemy != null && _ropeManager.AliveEnemies.Contains(placedEnemy))
            {
                Debug.Log($"  -> {placedEnemy.name} に {damage} ダメージ！");
                placedEnemy.TakeDamage(damage);
            }
        }
    }
}