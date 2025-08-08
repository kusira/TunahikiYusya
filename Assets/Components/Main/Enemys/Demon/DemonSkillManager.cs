using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// デーモンのスキル（3種類のランダムな効果）を管理します。
/// PlacedEnemyのUseSkill()から呼び出されることを想定しています。
/// </summary>
public class DemonSkillManager : MonoBehaviour
{
    [Header("スキル設定")]
    [Tooltip("全体回復時の回復量倍率（ATK × この値）")]
    [SerializeField] private float healAllEnemiesMultiplier = 3f;

    [Tooltip("全体攻撃時のダメージ量倍率（ATK × この値）")]
    [SerializeField] private float damageAllAlliesMultiplier = 3f;
    
    [Tooltip("単体攻撃時のダメージ量倍率（ATK × この値）")]
    [SerializeField] private float damageSingleAllyMultiplier = 6f;

    private PlacedEnemy _placedEnemy;
    private RopeManager _ropeManager;

    void Awake()
    {
        _placedEnemy = GetComponent<PlacedEnemy>();
        if (_placedEnemy == null)
        {
            Debug.LogError("DemonSkillManagerはPlacedEnemyと同じGameObjectにいる必要があります！", this);
        }

        _ropeManager = GetComponentInParent<RopeManager>();
        if (_ropeManager == null)
        {
            Debug.LogError("親階層にRopeManagerが見つかりません！", this);
        }
    }

    /// <summary>
    /// PlacedEnemyから呼び出される、デーモン専用のスキル処理
    /// </summary>
    public void ActivateDemonSkill()
    {
        if (_placedEnemy == null || _ropeManager == null) return;

        // 0, 1, 2の3つのうち、ランダムな数字を1つ選ぶ
        int choice = Random.Range(0, 3);

        switch (choice)
        {
            case 0:
                HealAllEnemiesOnRope();
                break;
            case 1:
                DamageAllAlliesOnRope();
                break;
            case 2:
                DamageSingleAllyOnRope();
                break;
        }
    }

    /// <summary>
    /// 効果1: 同じ綱の敵全体を回復
    /// </summary>
    private void HealAllEnemiesOnRope()
    {
        int healAmount = (int)(_placedEnemy.atk * healAllEnemiesMultiplier);
        if (healAmount <= 0) return;

        Debug.Log($"<color=fuchsia>DEMON SKILL: {_placedEnemy.name}が【全体回復】を発動！ (回復量: {healAmount})</color>");
        
        // 同じ綱にいる全ての敵を取得
        foreach (var enemyRow in _ropeManager.enemyRows)
        {
            var targets = new List<GameObject> { enemyRow.leftEnemy, enemyRow.rightEnemy };
            foreach (var targetGO in targets)
            {
                if (targetGO == null) continue;

                PlacedEnemy targetEnemy = targetGO.GetComponent<PlacedEnemy>();
                // 生存しており、自分自身ではない敵を回復
                if (targetEnemy != null && targetEnemy != _placedEnemy && _ropeManager.AliveEnemies.Contains(targetEnemy))
                {
                    Debug.Log($"  -> {targetEnemy.name} を {healAmount} 回復！");
                    targetEnemy.Heal(healAmount);
                }
            }
        }
    }

    /// <summary>
    /// 効果2: 同じ綱の味方全体を攻撃
    /// </summary>
    private void DamageAllAlliesOnRope()
    {
        int damage = (int)(_placedEnemy.atk * damageAllAlliesMultiplier);
        if (damage <= 0) return;

        Debug.Log($"<color=fuchsia>DEMON SKILL: {_placedEnemy.name}が【全体攻撃】を発動！ (ダメージ: {damage})</color>");

        List<GameObject> allies = _ropeManager.OccupiedHolderInfo.Values.ToList();
        if (allies.Count == 0) return;

        foreach (var allyGO in allies)
        {
            if (allyGO == null) continue;
            PlacedCharacter targetCharacter = allyGO.GetComponent<PlacedCharacter>();
            if (targetCharacter != null)
            {
                Debug.Log($"  -> {targetCharacter.name} に {damage} ダメージ！");
                targetCharacter.TakeDamage(damage);
            }
        }
    }

    /// <summary>
    /// 効果3: 同じ綱の味方単体をランダムに攻撃
    /// </summary>
    private void DamageSingleAllyOnRope()
    {
        int damage = (int)(_placedEnemy.atk * damageSingleAllyMultiplier);
        if (damage <= 0) return;
        
        List<GameObject> allies = _ropeManager.OccupiedHolderInfo.Values.ToList();
        if (allies.Count == 0)
        {
            Debug.Log($"<color=fuchsia>DEMON SKILL: {_placedEnemy.name}が【単体攻撃】を発動したが、対象がいなかった！</color>");
            return;
        }

        int randomIndex = Random.Range(0, allies.Count);
        GameObject targetGO = allies[randomIndex];
        if (targetGO == null) return;
        
        PlacedCharacter targetCharacter = targetGO.GetComponent<PlacedCharacter>();
        if (targetCharacter != null)
        {
            Debug.Log($"<color=fuchsia>DEMON SKILL: {_placedEnemy.name}が【単体攻撃】を発動！ -> {targetCharacter.name} に {damage} ダメージ！</color>");
            targetCharacter.TakeDamage(damage);
        }
    }
}