using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// ヒーラー（敵）のスキル（同じ綱の敵を回復）を管理します。
/// PlacedEnemyのUseSkill()から呼び出されることを想定しています。
/// </summary>
public class HealerSkillManager : MonoBehaviour
{
    [Header("スキル設定")]
    [Tooltip("回復量 = ATK × この値。5なら5倍。")]
    [SerializeField] private float healMultiplier = 5f;

    private PlacedEnemy _placedEnemy;
    private RopeManager _ropeManager;

    void Awake()
    {
        // 自身が持つコンポーネントへの参照を取得
        _placedEnemy = GetComponent<PlacedEnemy>();
        if (_placedEnemy == null)
        {
            Debug.LogError("HealerSkillManagerはPlacedEnemyと同じGameObjectにいる必要があります！", this);
        }

        // 自分が所属するRopeManagerを取得
        _ropeManager = GetComponentInParent<RopeManager>();
    }

    /// <summary>
    /// PlacedEnemyから呼び出される、ヒーラー専用のスキル処理
    /// </summary>
    public void ActivateHealerSkill()
    {
        if (_placedEnemy == null || _ropeManager == null) return;

        // 回復量を計算
        int healAmount = (int)(_placedEnemy.atk * healMultiplier);
        if (healAmount <= 0) return;

        Debug.Log($"<color=cyan>HEALER SKILL: {_placedEnemy.name}が回復スキルを発動！ (回復量: {healAmount})</color>");

        // 1. 同じ綱にいる全ての敵を取得
        foreach (var enemyRow in _ropeManager.enemyRows)
        {
            // 左の敵と右の敵をリストにまとめる
            var targets = new List<GameObject> { enemyRow.leftEnemy, enemyRow.rightEnemy };
            foreach (var targetGO in targets)
            {
                if (targetGO == null) continue;

                PlacedEnemy targetEnemy = targetGO.GetComponent<PlacedEnemy>();

                // 2. ターゲットが生存しており、かつ自分自身ではない場合に回復
                if (targetEnemy != null && targetEnemy != _placedEnemy && _ropeManager.AliveEnemies.Contains(targetEnemy))
                {
                    Debug.Log($"  -> {targetEnemy.name} を {healAmount} 回復！");
                    targetEnemy.Heal(healAmount);
                }
            }
        }
    }
}