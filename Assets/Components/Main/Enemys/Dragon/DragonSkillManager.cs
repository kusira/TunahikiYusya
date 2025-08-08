using UnityEngine;

/// <summary>
/// ドラゴンのスキル（味方全体にダメージ）を管理します。
/// PlacedEnemyのUseSkill()から呼び出されることを想定しています。
/// </summary>
public class DragonSkillManager : MonoBehaviour
{
    [Header("スキル設定")]
    [Tooltip("ダメージ量 = ATK × この値。2なら2倍。")]
    [SerializeField] private float damageMultiplier = 2f;

    private PlacedEnemy _placedEnemy;

    void Awake()
    {
        // 自身が持つコンポーネントへの参照を取得
        _placedEnemy = GetComponent<PlacedEnemy>();
        if (_placedEnemy == null)
        {
            Debug.LogError("DragonSkillManagerはPlacedEnemyと同じGameObjectにいる必要があります！", this);
        }
    }

    /// <summary>
    /// PlacedEnemyから呼び出される、ドラゴン専用のスキル処理
    /// </summary>
    public void ActivateDragonSkill()
    {
        if (_placedEnemy == null) return;

        // ダメージ量を計算
        int damage = (int)(_placedEnemy.atk * damageMultiplier);
        if (damage <= 0) return;

        Debug.Log($"<color=purple>DRAGON SKILL: {_placedEnemy.name}がブレスで全体攻撃！ (ダメージ: {damage})</color>");

        // 1. シーン上に存在する全ての味方キャラクターを取得
        PlacedCharacter[] allAllies = FindObjectsByType<PlacedCharacter>(FindObjectsSortMode.None);
        if (allAllies.Length == 0)
        {
            Debug.Log("攻撃対象の味方が一人もいませんでした。");
            return;
        }

        // 2. 見つけた全ての味方にダメージを与える
        foreach (var ally in allAllies)
        {
            Debug.Log($"  -> {ally.name} に {damage} ダメージ！");
            ally.TakeDamage(damage);
        }
    }
}

