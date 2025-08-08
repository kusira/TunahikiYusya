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

    void Awake()
    {
        // 自身が持つコンポーネントへの参照を取得
        _placedEnemy = GetComponent<PlacedEnemy>();
        if (_placedEnemy == null)
        {
            Debug.LogError("MinotaurSkillManagerはPlacedEnemyと同じGameObjectにいる必要があります！", this);
        }
    }

    /// <summary>
    /// PlacedEnemyから呼び出される、ミノタウロス専用のスキル処理
    /// </summary>
    public void ActivateMinotaurSkill()
    {
        if (_placedEnemy == null) return;

        // ダメージ量を計算
        int damage = (int)(_placedEnemy.atk * damageMultiplier);
        if (damage <= 0) return;

        Debug.Log($"<color=red>MINOTAUR SKILL: {_placedEnemy.name}が薙ぎ払いスキルを発動！ (ダメージ: {damage})</color>");

        // 1. シーンに存在する全ての味方キャラクターを取得
        PlacedCharacter[] allAllies = FindObjectsByType<PlacedCharacter>(FindObjectsSortMode.None);
        if (allAllies.Length == 0)
        {
            Debug.Log("攻撃対象の味方が一人もいませんでした。");
            return;
        }

        // 2. 左右それぞれのサイドで最もY座標が低い（＝最前列）の味方を探す
        PlacedCharacter leftFrontAlly = null;
        PlacedCharacter rightFrontAlly = null;
        float minLeftY = float.MaxValue;
        float minRightY = float.MaxValue;

        foreach (var ally in allAllies)
        {
            // キャラクターの親の親（Ropeオブジェクト）を基準に左右を判定
            Transform axis = ally.transform.parent?.parent;
            if (axis == null) continue;

            bool isOnLeft = ally.transform.position.x <= axis.position.x;
            float currentY = ally.transform.position.y;

            if (isOnLeft)
            {
                if (currentY < minLeftY)
                {
                    minLeftY = currentY;
                    leftFrontAlly = ally;
                }
            }
            else // On Right
            {
                if (currentY < minRightY)
                {
                    minRightY = currentY;
                    rightFrontAlly = ally;
                }
            }
        }

        // 3. 見つけたターゲットにダメージを与える
        if (leftFrontAlly != null)
        {
            Debug.Log($"  -> 左最前列の {leftFrontAlly.name} に {damage} ダメージ！");
            leftFrontAlly.TakeDamage(damage);
        }
        if (rightFrontAlly != null)
        {
            Debug.Log($"  -> 右最前列の {rightFrontAlly.name} に {damage} ダメージ！");
            rightFrontAlly.TakeDamage(damage);
        }
    }
}