using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// ゴブリンのスキル（同じ綱のランダムな味方単体にダメージ）を管理します。
/// PlacedEnemyのUseSkill()から呼び出されることを想定しています。
/// </summary>
public class GoblinSkillManager : MonoBehaviour
{
    [Header("スキル設定")]
    [Tooltip("ダメージ量 = ATK × この値。2なら2倍。")]
    [SerializeField] private float damageMultiplier = 2f;

    private PlacedEnemy _placedEnemy;
    private RopeManager _ropeManager;

    // ▼▼▼ 追加：常に最新の味方リストを保持する変数 ▼▼▼
    private readonly List<GameObject> _alliesOnSameRope = new List<GameObject>();

    void Awake()
    {
        _placedEnemy = GetComponent<PlacedEnemy>();
        if (_placedEnemy == null)
        {
            Debug.LogError("GoblinSkillManagerはPlacedEnemyと同じGameObjectにいる必要があります！", this);
        }

        _ropeManager = GetComponentInParent<RopeManager>();
    }

    // ▼▼▼ 追加：Updateメソッドで味方リストを監視・更新 ▼▼▼
    void Update()
    {
        if (_ropeManager == null) return;

        // 毎フレームリストをクリアし、現在の最新の味方情報に更新する
        _alliesOnSameRope.Clear();
        foreach(var allyGO in _ropeManager.OccupiedHolderInfo.Values)
        {
            // 破壊されたオブジェクトがリストに残らないようにnullチェック
            if (allyGO != null)
            {
                _alliesOnSameRope.Add(allyGO);
            }
        }
    }

    /// <summary>
    /// PlacedEnemyから呼び出される、ゴブリン専用のスキル処理
    /// </summary>
    public void ActivateGoblinSkill()
    {
        if (_placedEnemy == null) return;

        // ▼▼▼ 修正：Updateで更新されたリストを参照するように変更 ▼▼▼
        // 攻撃対象がいなければスキルを不発にする
        if (_alliesOnSameRope.Count == 0)
        {
            Debug.Log($"<color=lime>GOBLIN SKILL: {name}がスキルを発動したが、攻撃対象がいなかった！</color>");
            return;
        }

        // 攻撃対象をランダムに1体選ぶ
        int randomIndex = Random.Range(0, _alliesOnSameRope.Count);
        GameObject targetGO = _alliesOnSameRope[randomIndex];
        if (targetGO == null) return;

        PlacedCharacter targetCharacter = targetGO.GetComponent<PlacedCharacter>();
        if (targetCharacter == null) return;

        // ダメージを計算して与える
        int damage = (int)(_placedEnemy.atk * damageMultiplier);
        if (damage <= 0) return;

        Debug.Log($"<color=lime>GOBLIN SKILL: {name}がスキル発動！ -> {targetCharacter.name} に {damage} ダメージ！</color>");
        targetCharacter.TakeDamage(damage);
    }
}