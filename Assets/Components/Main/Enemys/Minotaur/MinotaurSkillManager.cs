using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// ミノタウロスのスキル（味方の最前列左右にダメージ）を管理します。
/// PlacedEnemyのUseSkill()から呼び出されることを想定しています。
/// </summary>
public class MinotaurSkillManager : MonoBehaviour
{
    [Header("スキル設定")]
    [Tooltip("ダメージ量 = ATK × この値。3なら3倍。")]
    [SerializeField] private float damageMultiplier = 3f;

    private PlacedEnemy _placedEnemy;
    private RopeManager _ropeManager;

    void Awake()
    {
        // 自身が持つコンポーネントへの参照を取得
        _placedEnemy = GetComponent<PlacedEnemy>();
        if (_placedEnemy == null)
        {
            Debug.LogError("MinotaurSkillManagerはPlacedEnemyと同じGameObjectにいる必要があります！", this);
        }

        // 自分自身の親の親（Ropeオブジェクト）からRopeManagerを取得（Golemと同様）
        if (transform.parent != null && transform.parent.parent != null)
        {
            _ropeManager = transform.parent.parent.GetComponent<RopeManager>();
        }
    }

    /// <summary>
    /// PlacedEnemyから呼び出される、ミノタウロス専用のスキル処理
    /// </summary>
    public void ActivateMinotaurSkill()
    {
        if (_placedEnemy == null || _ropeManager == null) return;

        int damage = (int)(_placedEnemy.atk * damageMultiplier);
        if (damage <= 0) return;

        Debug.Log($"<color=red>MINOTAUR SKILL: {_placedEnemy.name}が薙ぎ払いスキルを発動！ (ダメージ: {damage})</color>");

        // 1. 味方が存在する最前列（holderRowsの先頭から）を見つける
        HolderRow targetRow = null;
        foreach (var row in _ropeManager.holderRows)
        {
            bool leftOccupied  = row.leftHolder  != null && _ropeManager.OccupiedHolderInfo.ContainsKey(row.leftHolder);
            bool rightOccupied = row.rightHolder != null && _ropeManager.OccupiedHolderInfo.ContainsKey(row.rightHolder);
            if (leftOccupied || rightOccupied)
            {
                targetRow = row;
                break;
            }
        }

        if (targetRow == null)
        {
            Debug.Log("攻撃対象の味方が見つかりませんでした。");
            return;
        }

        // 2. 最前列の左右ターゲットを取得
        GameObject leftGO = null, rightGO = null;
        if (targetRow.leftHolder != null)
            _ropeManager.OccupiedHolderInfo.TryGetValue(targetRow.leftHolder, out leftGO);
        if (targetRow.rightHolder != null)
            _ropeManager.OccupiedHolderInfo.TryGetValue(targetRow.rightHolder, out rightGO);

        // 3. ダメージ適用（存在する側のみ）
        if (leftGO != null)
        {
            var leftAlly = leftGO.GetComponent<PlacedCharacter>();
            if (leftAlly != null)
            {
                Debug.Log($"  -> 最前列(左)の {leftAlly.name} に {damage} ダメージ！");
                leftAlly.TakeDamage(damage);
            }
        }

        if (rightGO != null)
        {
            var rightAlly = rightGO.GetComponent<PlacedCharacter>();
            if (rightAlly != null)
            {
                Debug.Log($"  -> 最前列(右)の {rightAlly.name} に {damage} ダメージ！");
                rightAlly.TakeDamage(damage);
            }
        }
    }
}