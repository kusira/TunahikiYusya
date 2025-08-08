using UnityEngine;

[RequireComponent(typeof(PlacedCharacter))]
public class HeroSkillManager : MonoBehaviour
{
    [Header("スキル設定：HP依存ATKバフ")]
    [Tooltip("スキルが発動するHPの割合(%)の閾値。この値以上で発動。")]
    [SerializeField] [Range(0, 100)] private float hpThresholdPercent = 80f;

    [Tooltip("HPが閾値以上の時にATKに掛かる倍率")]
    [SerializeField] private float attackMultiplier = 1.5f;

    [Header("バフUI設定")]
    // ▼▼▼ 修正点 1: Spriteの参照を削除し、buffIdをInspectorから設定できるように変更 ▼▼▼
    [Tooltip("このスキルで表示するバフのID（BuffIconDatabaseで設定したもの）")]
    [SerializeField] private string buffId = "hero";

    // --- 内部で使う変数 ---
    private PlacedCharacter placedCharacter;
    private bool isBuffActive = false;
    private BuffManager buffManager;

    void Awake()
    {
        placedCharacter = GetComponent<PlacedCharacter>();
        buffManager = GetComponent<BuffManager>();
    }

    void Update()
    {
        if (placedCharacter == null || placedCharacter.maxHp == 0) return;

        float currentHpPercent = (float)placedCharacter.hp / placedCharacter.maxHp * 100f;
        bool conditionMet = (currentHpPercent >= hpThresholdPercent);

        if (conditionMet != isBuffActive)
        {
            isBuffActive = conditionMet;

            if (isBuffActive)
            {
                // バフを発動
                placedCharacter.SetAttackMultiplier(attackMultiplier);
                Debug.Log(gameObject.name + " のスキル効果発動！");

                // ▼▼▼ 修正点 2: AddBuffの呼び出し方を変更 ▼▼▼
                if (buffManager != null)
                {
                    buffManager.AddBuff(buffId);
                }
            }
            else
            {
                // バフを解除
                placedCharacter.SetAttackMultiplier(1f);
                Debug.Log(gameObject.name + " のスキル効果終了。");

                if (buffManager != null)
                {
                    buffManager.RemoveBuff(buffId);
                }
            }
        }
    }
    
    void OnDisable()
    {
        if(isBuffActive)
        {
            if (placedCharacter != null)
            {
                // 念のため倍率を元に戻す
                placedCharacter.SetAttackMultiplier(1f);
            }
            if (buffManager != null)
            {
                buffManager.RemoveBuff(buffId);
            }
            isBuffActive = false;
        }
    }
}