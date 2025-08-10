using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// モンクのスキル（同じ綱の味方を回復）を管理します。
/// PlacedCharacterのUseSkill()から呼び出されることを想定しています。
/// </summary>
public class MonkSkillManager : MonoBehaviour
{
    [Header("スキル設定")]
    [Tooltip("回復量 = ATK × この値。10なら10倍。")]
    [SerializeField] private float healMultiplier = 10f;

    private PlacedCharacter _placedCharacter;
    private RopeManager _ropeManager;

    void Awake()
    {
        // 自身が持つコンポーネントへの参照を取得
        _placedCharacter = GetComponent<PlacedCharacter>();
        if (_placedCharacter == null)
        {
            Debug.LogError("MonkSkillManagerはPlacedCharacterと同じGameObjectにいる必要があります！", this);
        }

        // 親階層を遡って、所属するRopeManagerを取得
        _ropeManager = GetComponentInParent<RopeManager>();
    }

    /// <summary>
    /// PlacedCharacterから呼び出される、モンク専用のスキル処理
    /// </summary>
    public void ActivateMonkSkill()
    {
        // 必要なコンポーネントがなければ処理を中断
        if (_placedCharacter == null || _ropeManager == null) return;

        // 回復量を計算 (ATK * 倍率)
        int healAmount = (int)(_placedCharacter.atk * healMultiplier);
        if (healAmount <= 0) return; // 回復量が0以下なら何もしない

        Debug.Log($"<color=green>MONK SKILL: {_placedCharacter.name}が回復スキルを発動！ (回復量: {healAmount})</color>");

        // RopeManagerが管理している、同じ綱の味方キャラクター全員を取得
        ICollection<GameObject> alliesOnSameRope = _ropeManager.OccupiedHolderInfo.Values;

        _placedCharacter.Heal(healAmount);
        
        foreach (GameObject allyGO in alliesOnSameRope)
        {
            if (allyGO == null) continue; // すでに破壊された味方はスキップ

            PlacedCharacter allyCharacter = allyGO.GetComponent<PlacedCharacter>();

            // 自分自身は回復しないようにする（お好みで allyCharacter != _placedCharacter の条件は外してもOKです）
            if (allyCharacter != null && allyCharacter != _placedCharacter)
            {
                Debug.Log($"  -> {allyCharacter.name} を {healAmount} 回復させます。");
                allyCharacter.Heal(healAmount);
            }
        }
    }
}