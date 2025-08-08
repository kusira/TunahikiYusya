using UnityEngine;

/// <summary>
/// 戦士のスキル（味方の数に応じた自己強化）を管理します。
/// </summary>
public class WarriorSkillManager : MonoBehaviour
{
    [Header("スキル設定")]
    [Tooltip("味方が3人以上の時に、自身のATKに乗算される倍率。1.3なら1.3倍。")]
    [SerializeField] private float attackMultiplier = 1.3f;

    // ▼▼▼ 以下を追加 ▼▼▼
    [Header("バフUI設定")]
    [Tooltip("このバフのID（BuffIconDatabaseで設定したもの）")]
    [SerializeField] private string buffId = "warrior";

    private PlacedCharacter _placedCharacter;
    private RopeManager _ropeManager;
    private BuffManager _buffManager; // BuffManagerへの参照
    
    private bool _isBuffActive = false;

    void Awake()
    {
        _placedCharacter = GetComponent<PlacedCharacter>();
        if (_placedCharacter == null)
        {
            Debug.LogError("WarriorSkillManagerにはPlacedCharacterが必要です！", this);
        }

        _ropeManager = GetComponentInParent<RopeManager>();
        // RopeManagerが見つからない場合のエラーログは任意で追加してください

        // ▼▼▼ 以下を追加 ▼▼▼
        _buffManager = GetComponent<BuffManager>();
        if (_buffManager == null)
        {
            Debug.LogError("WarriorSkillManagerにはBuffManagerが必要です！", this);
        }
    }

    void Update()
    {
        // 必要なコンポーネントがなければ処理を中断
        if (_placedCharacter == null || _ropeManager == null || _buffManager == null) return;

        int alliesCount = _ropeManager.OccupiedHolderInfo.Count;
        bool conditionMet = alliesCount >= 3;

        if (conditionMet != _isBuffActive)
        {
            if (conditionMet)
            {
                // バフを発動する
                Debug.Log($"<color=red>WARRIOR SKILL: {name} のATKが{attackMultiplier}倍になります！(味方: {alliesCount}人)</color>");
                _placedCharacter.SetAttackMultiplier(attackMultiplier);
                // ▼▼▼ 以下を追加 ▼▼▼
                _buffManager.AddBuff(buffId); // バフアイコンを表示
            }
            else
            {
                // バフを解除する
                Debug.Log($"<color=grey>WARRIOR SKILL: {name} のATKが元に戻ります。(味方: {alliesCount}人)</color>");
                _placedCharacter.SetAttackMultiplier(1.0f);
                // ▼▼▼ 以下を追加 ▼▼▼
                _buffManager.RemoveBuff(buffId); // バフアイコンを削除
            }
            
            _isBuffActive = conditionMet;
        }
    }
}