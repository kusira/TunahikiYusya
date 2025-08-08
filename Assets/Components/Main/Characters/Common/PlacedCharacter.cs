using UnityEngine;
using DG.Tweening;
using System;
using Random = UnityEngine.Random;

public class PlacedCharacter : MonoBehaviour
{
    public static int HpToTransfer = -1;

    // (Inspectorで設定する変数は変更ありません)
    #region Serialized Fields
    [Header("キャラクター識別")]
    [SerializeField] private string characterName;
    [SerializeField] [Range(1, 3)] private int level = 1;

    [Header("連携データ")]
    public CharacterCardData SourceCardData { get; set; }
    public CharacterHolderManager CurrentHolder { get; set; }

    [Header("基本ステータス")]
    public int maxHp { get; private set; }
    public int hp { get; private set; }
    public int baseAtk { get; private set; }
    public int atk { get; private set; }
    public bool hasCooldownSkill { get; private set; }
    public float skillCooldownTime { get; private set; }
    public string skill { get; private set; }

    [Header("綱引き設定")]
    [SerializeField] private float tugOfWarDamageMultiplier = 0.1f;

    [Header("HPゲージ設定")]
    [SerializeField] private SpriteRenderer hpGaugeRenderer;
    [SerializeField] private Color highHpColor = Color.green;
    [SerializeField] private Color middleHpColor = Color.yellow;
    [SerializeField] [Range(0, 100)] private int middleHpThreshold = 50;
    [SerializeField] private Color lowHpColor = Color.red;
    [SerializeField] [Range(0, 100)] private int lowHpThreshold = 20;

    [Header("スキル設定")]
    [SerializeField] private SpriteRenderer skillGaugeRenderer;

    [Header("ゲージ全般設定")]
    [SerializeField] [Range(0f, 1f)] private float gaugeAlpha = 1.0f;

    [Header("エフェクト設定")]
    [SerializeField] [Range(0f, 1f)] private float dragAlpha = 0.6f;
    [SerializeField] private Color damageFlashColor = Color.red;
    [SerializeField] private Color healFlashColor = Color.green;
    [SerializeField] private float flashDuration = 0.3f;
    
    [Header("死亡演出の設定")]
    [SerializeField] private float deathForce = 250f;
    [SerializeField] private float delayBeforeFade = 1.5f;
    [SerializeField] private float fadeDuration = 0.5f;
    #endregion

    public bool IsPaused { get; private set; } = false;
    
    // (内部変数は変更ありません)
    #region Private Fields
    private CharacterDatabase database;
    private RopeManager ropeManager;
    private BattleBeginsManager battleBeginsManager;
    private SpriteRenderer mainSpriteRenderer;
    private SpriteRenderer[] allRenderers;
    private BoxCollider2D boxCollider;
    private bool isDead = false;
    private float currentCooldown;
    private float tugOfWarDamageTimer = 0f;
    private float initialHpGaugeScaleX, initialHpGaugePosX, initialSkillGaugeScaleX, initialSkillGaugePosX;
    #endregion

    public Vector3 CurrentPosition => transform.position;

    // (Awake, Start, Updateなどの前半部分は変更ありません)
    #region Unity Lifecycle Methods
    void Awake()
    {
        mainSpriteRenderer = GetComponentInChildren<SpriteRenderer>();
        allRenderers = GetComponentsInChildren<SpriteRenderer>();
        boxCollider = GetComponent<BoxCollider2D>();
        database = FindAnyObjectByType<CharacterDatabase>();
        ropeManager = GetComponentInParent<RopeManager>();
        battleBeginsManager = FindAnyObjectByType<BattleBeginsManager>();
    }

    void Start()
    {
        if (database == null) { Debug.LogError("シーンにCharacterDatabaseが見つかりません！", this); return; }
        CharacterLevelStats stats = database.GetStats(characterName, level);
        if (stats == null) return;
        
        this.maxHp = stats.hp;
        this.baseAtk = stats.atk;
        this.atk = stats.atk;
        this.hasCooldownSkill = stats.hasCooldownSkill;
        this.skillCooldownTime = stats.skillCooldownTime;
        this.skill = stats.skillDescription;
        
        if (HpToTransfer != -1)
        {
            this.hp = Mathf.Clamp(HpToTransfer, 0, this.maxHp);
            HpToTransfer = -1;
        }
        else
        {
            this.hp = stats.hp;
        }

        if (hpGaugeRenderer != null)
        {
            initialHpGaugeScaleX = hpGaugeRenderer.transform.localScale.x;
            initialHpGaugePosX = hpGaugeRenderer.transform.localPosition.x;
        }
        if (skillGaugeRenderer != null)
        {
            initialSkillGaugeScaleX = skillGaugeRenderer.transform.localScale.x;
            initialSkillGaugePosX = skillGaugeRenderer.transform.localPosition.x;
        }

        UpdateHpGauge();
        
        if (hasCooldownSkill)
        {
            currentCooldown = 0f;
            UpdateSkillGauge();
        }
        else if (skillGaugeRenderer != null)
        {
            skillGaugeRenderer.gameObject.SetActive(false);
        }
    }

    void Update()
    {
        if (battleBeginsManager != null && !battleBeginsManager.IsInBattle) return;
        
        if (hasCooldownSkill && !isDead && !IsPaused)
        {
            currentCooldown += Time.deltaTime;
            UpdateSkillGauge();
            if (currentCooldown >= skillCooldownTime)
            {
                UseSkill();
            }
        }

        if (!isDead && ropeManager != null)
        {
            if (ropeManager.TotalEnemyAtk > ropeManager.TotalAlliedAtk)
            {
                tugOfWarDamageTimer += Time.deltaTime;
                if (tugOfWarDamageTimer >= 1.0f)
                {
                    tugOfWarDamageTimer -= 1.0f;
                    int diff = ropeManager.TotalEnemyAtk - ropeManager.TotalAlliedAtk;
                    int damage = Mathf.Max(1, (int)(diff * tugOfWarDamageMultiplier));
                    TakeTugOfWarDamage(damage);
                }
            }
            else
            {
                tugOfWarDamageTimer = 0f;
            }
        }
    }
    #endregion

    // (TakeDamage, Heal, SetAttackMultiplierなどのメソッドは変更ありません)
    #region Public Gameplay Methods
    public void TakeDamage(int damage)
    {
        if (isDead) return;
        hp -= damage;
        FlashColor(damageFlashColor);
        UpdateHpGauge();
        if (hp <= 0)
        {
            hp = 0;
            Die();
        }
    }
    
    public void TakeTugOfWarDamage(int damage)
    {
        if (isDead) return;
        hp -= damage;
        UpdateHpGauge();
        if (hp <= 0)
        {
            hp = 0;
            Die();
        }
    }

    public void Heal(int amount)
    {
        if (isDead) return;
        hp += amount;
        hp = Mathf.Clamp(hp, 0, maxHp);
        FlashColor(healFlashColor);
        UpdateHpGauge();
    }

    public void SetAttackMultiplier(float multiplier)
    {
        if (isDead) return;
        atk = (int)(baseAtk * multiplier);
    }
    
    public void UseSkill()
    {
        if (!hasCooldownSkill) return;
        Debug.Log(gameObject.name + " がスキルを自動発動！ (" + skill + ")");
        currentCooldown = 0f;
        
        GetComponent<ArcherSkillManager>()?.ActivateArcherSkill();
        GetComponent<MonkSkillManager>()?.ActivateMonkSkill();
        GetComponent<GolemSkillManager>()?.ActivateGolemSkill();
    }
    
    public void OnDragStart()
    {
        IsPaused = true;
        SetRenderersAlpha(dragAlpha);
    }

    public void OnDragEnd()
    {
        IsPaused = false;
        SetRenderersAlpha(1.0f);
    }
    #endregion
    
    // ▼▼▼ ここからが今回の修正箇所です ▼▼▼

    /// <summary>
    /// 【外部API】指定した色と時間でキャラクターを点滅させます。
    /// </summary>
    /// <param name="color">点滅させる色</param>
    /// <param name="duration">点滅の合計時間（秒）</param>
    public void Flash(Color color, float duration)
    {
        if (mainSpriteRenderer == null || duration <= 0) return;

        mainSpriteRenderer.DOKill();
        // 点滅後、元の白色に戻る
        mainSpriteRenderer.DOColor(color, duration / 2).OnComplete(() =>
        {
            mainSpriteRenderer.DOColor(Color.white, duration / 2);
        });
    }

    /// <summary>
    /// 内部用の点滅処理。高速で繰り返し点滅させます。
    /// </summary>
    private void FlashColor(Color flashColor)
    {
        // 外部APIとして作成したFlash()メソッドを、
        // Inspectorで設定したデフォルト値（flashDuration）で呼び出す
        Flash(flashColor, flashDuration);
    }

    #region Private Helper Methods
    private void SetRenderersAlpha(float alpha)
    {
        if (allRenderers == null) return;
        foreach (var renderer in allRenderers)
        {
            if(renderer != mainSpriteRenderer)
            {
                Color color = renderer.color;
                color.a = alpha;
                renderer.color = color;
            }
        }
    }
    
    private void Die()
    {
        if (isDead) return;
        isDead = true;

        if (CurrentHolder != null) CurrentHolder.SetOccupied(false);
        if (boxCollider != null) boxCollider.enabled = false;
        if (mainSpriteRenderer != null) mainSpriteRenderer.DOKill();
        
        var rb = gameObject.AddComponent<Rigidbody2D>();
        rb.gravityScale = 1.0f;
        float randomXDirection = Random.value < 0.5f ? -1f : 1f;
        Vector2 forceDirection = new Vector2(randomXDirection, 1.5f).normalized;
        rb.AddForce(forceDirection * deathForce);
        rb.AddTorque(Random.Range(-180f, 180f));

        Sequence deathSequence = DOTween.Sequence();
        deathSequence.AppendInterval(delayBeforeFade);
        if (mainSpriteRenderer != null)
        {
            deathSequence.Append(mainSpriteRenderer.DOFade(0, fadeDuration));
        }
        deathSequence.OnComplete(() => Destroy(gameObject));
    }

    private void UpdateHpGauge()
    {
        if (hpGaugeRenderer == null || maxHp == 0) return;
        float hpPercent = (float)hp / maxHp;

        Vector3 newScale = hpGaugeRenderer.transform.localScale;
        newScale.x = initialHpGaugeScaleX * hpPercent;
        hpGaugeRenderer.transform.localScale = newScale;

        Vector3 newPosition = hpGaugeRenderer.transform.localPosition;
        float positionOffset = (initialHpGaugeScaleX - newScale.x) / 2f;
        newPosition.x = initialHpGaugePosX - positionOffset;
        hpGaugeRenderer.transform.localPosition = newPosition;

        float hpPercent100 = hpPercent * 100f;
        Color targetColor;
        if (hpPercent100 > middleHpThreshold) targetColor = highHpColor;
        else if (hpPercent100 > lowHpThreshold) targetColor = middleHpColor;
        else targetColor = lowHpColor;
        
        targetColor.a = gaugeAlpha;
        hpGaugeRenderer.color = targetColor;
    }

    private void UpdateSkillGauge()
    {
        if (skillGaugeRenderer == null || skillCooldownTime <= 0) return;
        float clampedCooldown = Mathf.Max(0, currentCooldown);
        float cooldownPercent = clampedCooldown / skillCooldownTime;

        Vector3 newScale = skillGaugeRenderer.transform.localScale;
        newScale.x = initialSkillGaugeScaleX * cooldownPercent;
        skillGaugeRenderer.transform.localScale = newScale;

        Vector3 newPosition = skillGaugeRenderer.transform.localPosition;
        float positionOffset = (initialSkillGaugeScaleX - newScale.x) / 2f;
        newPosition.x = initialSkillGaugePosX - positionOffset;
        skillGaugeRenderer.transform.localPosition = newPosition;

        Color currentColor = skillGaugeRenderer.color;
        currentColor.a = gaugeAlpha;
        skillGaugeRenderer.color = currentColor;
    }
    #endregion
}