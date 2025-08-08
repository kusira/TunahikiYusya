using UnityEngine;
using DG.Tweening;
using System;
using Random = UnityEngine.Random;

/// <summary>
/// フィールドに配置された敵が持つデータと機能。
/// ステータス管理、HP/スキルゲージ、被ダメージ処理、死亡演出などを担当します。
/// </summary>
public class PlacedEnemy : MonoBehaviour
{
    [Header("敵の識別")]
    [Tooltip("EnemyDatabaseで設定した敵の名前と完全に一致させる")]
    [SerializeField] private string enemyName;

    [Header("基本ステータス")]
    public int maxHp { get; private set; }
    public int hp { get; private set; }
    public int baseAtk { get; private set; }
    public int atk { get; private set; }
    public bool hasCooldownSkill { get; private set; }
    public float skillCooldownTime { get; private set; }
    public string skill { get; private set; }

    // ★ここから追加
    [Header("綱引き設定")]
    [Tooltip("綱引きで不利な状況の時に、ATK差分に乗算されるダメージ係数")]
    [SerializeField] private float tugOfWarDamageMultiplier = 0.1f;
    // ★ここまで追加

    [Header("HPゲージ設定")]
    [SerializeField] private SpriteRenderer hpGaugeRenderer;
    [SerializeField] private Color highHpColor = Color.red;
    [SerializeField] private Color middleHpColor = new Color(1.0f, 0.5f, 0.0f); // Orange
    [SerializeField] [Range(0, 100)] private int middleHpThreshold = 50;
    [SerializeField] private Color lowHpColor = Color.yellow;
    [SerializeField] [Range(0, 100)] private int lowHpThreshold = 20;

    [Header("スキル設定")]
    [SerializeField] private SpriteRenderer skillGaugeRenderer;

    [Header("ゲージ全般設定")]
    [SerializeField] [Range(0f, 1f)] private float gaugeAlpha = 1.0f;

    [Header("エフェクト設定")]
    [SerializeField] private Color damageFlashColor = Color.white;
    [SerializeField] private Color healFlashColor = Color.cyan;
    [SerializeField] private float flashDuration = 0.2f;

    [Header("死亡演出の設定")]
    [SerializeField] private float delayBeforeFade = 1.5f;
    [SerializeField] private float fadeDuration = 0.5f;

    // --- 内部変数 ---
    private EnemyDatabase database;
    private BattleBeginsManager battleBeginsManager;
    private RopeManager ropeManager; // ★追加
    private SpriteRenderer mainSpriteRenderer;
    private SpriteRenderer[] allRenderers;
    private BoxCollider2D boxCollider;
    private bool isDead = false;
    private float currentCooldown;
    private float tugOfWarDamageTimer = 0f; // ★追加
    private float initialHpGaugeScaleX, initialHpGaugePosX;
    private float initialSkillGaugeScaleX, initialSkillGaugePosX;

    public static event Action<PlacedEnemy> OnEnemyDied;
    
    public Vector3 CurrentPosition => transform.position;

    void Awake()
    {
        mainSpriteRenderer = GetComponentInChildren<SpriteRenderer>();
        allRenderers = GetComponentsInChildren<SpriteRenderer>();
        boxCollider = GetComponent<BoxCollider2D>();
        database = FindAnyObjectByType<EnemyDatabase>();
        battleBeginsManager = FindAnyObjectByType<BattleBeginsManager>();
        ropeManager = GetComponentInParent<RopeManager>();
    }

    void Start()
    {
        if (database == null)
        {
            Debug.LogError("シーンにEnemyDatabaseが見つかりません！", this);
            return;
        }
        
        EnemyDataEntry stats = database.GetStats(enemyName);
        if (stats == null) return;
        
        this.maxHp = stats.hp;
        this.hp = stats.hp;
        this.baseAtk = stats.atk;
        this.atk = stats.atk;
        this.hasCooldownSkill = stats.hasCooldownSkill;
        this.skillCooldownTime = stats.skillCooldownTime;
        this.skill = stats.skillDescription;
        
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
        if (battleBeginsManager != null && !battleBeginsManager.IsInBattle)
        {
            return;
        }

        // スキルクールダウン処理
        if (hasCooldownSkill && !isDead)
        {
            currentCooldown += Time.deltaTime;
            UpdateSkillGauge();
            if (currentCooldown >= skillCooldownTime)
            {
                UseSkill();
            }
        }

        // ★ここから追加：綱引きによる継続ダメージ処理
        if (!isDead && ropeManager != null)
        {
            // 敵は、味方の総ATKが上回っている場合にダメージを受ける
            if (ropeManager.TotalAlliedAtk > ropeManager.TotalEnemyAtk)
            {
                tugOfWarDamageTimer += Time.deltaTime;
                if (tugOfWarDamageTimer >= 1.0f)
                {
                    tugOfWarDamageTimer -= 1.0f;
                    int diff = ropeManager.TotalAlliedAtk - ropeManager.TotalEnemyAtk;
                    int damage = Mathf.Max(1, (int)(diff * tugOfWarDamageMultiplier));
                    TakeTugOfWarDamage(damage);
                }
            }
            else
            {
                tugOfWarDamageTimer = 0f;
            }
        }
        // ★ここまで追加
    }
    
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

    // ★ここから追加
    public void TakeTugOfWarDamage(int damage)
    {
        if (isDead) return;
        hp -= damage;
        UpdateHpGauge(); // フラッシュなしでゲージだけ更新
        
        if (hp <= 0)
        {
            hp = 0;
            Die();
        }
    }
    // ★ここまで追加

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

    private void UseSkill()
    {
        if (!hasCooldownSkill || isDead) return;
        Debug.Log(gameObject.name + " がスキルを発動！ (" + skill + ")");
        currentCooldown = 0f;

        // ゴブリン
        GetComponent<GoblinSkillManager>()?.ActivateGoblinSkill();
        // ミノタウロス
        GetComponent<MinotaurSkillManager>()?.ActivateMinotaurSkill();
        // ドラゴン
        GetComponent<DragonSkillManager>()?.ActivateDragonSkill();
        // ヒーラー
        GetComponent<HealerSkillManager>()?.ActivateHealerSkill();
        // デーモン
        GetComponent<DemonSkillManager>()?.ActivateDemonSkill();
    }
    
    private void FlashColor(Color flashColor)
    {
        if (mainSpriteRenderer != null)
        {
            mainSpriteRenderer.DOKill();
            mainSpriteRenderer.DOColor(flashColor, flashDuration / 2).OnComplete(() =>
            {
                mainSpriteRenderer.DOColor(Color.white, flashDuration / 2);
            });
        }
    }
    
    public void Die()
    {
        if (isDead) return;
        isDead = true;

        OnEnemyDied?.Invoke(this);

        if (boxCollider != null) boxCollider.enabled = false;
        if (hpGaugeRenderer != null) hpGaugeRenderer.gameObject.SetActive(false);
        if (skillGaugeRenderer != null) skillGaugeRenderer.gameObject.SetActive(false);

        // 吹っ飛びパラメータ
        float randomXDirection = Random.value < 0.5f ? -1f : 1f;
        float flyDistanceX = 1.5f * randomXDirection; // X方向の距離
        float flyHeight = 2.0f;                       // Y方向の最大高さ
        float flyDuration = 0.8f;                     // 吹っ飛び時間
        float rotationAngle = 360f * randomXDirection; // 回転角度（方向も反転）

        Vector3 startPos = transform.position;
        Vector3 endPos = startPos + new Vector3(flyDistanceX, 0, 0); // 地面に戻る位置

        // 放物線を描くアニメーション
        DOTween.To(
            () => 0f,
            t =>
            {
                float x = Mathf.Lerp(startPos.x, endPos.x, t);
                float y = Mathf.Lerp(startPos.y, startPos.y + flyHeight, t) * (1 - t) + startPos.y * t;
                transform.position = new Vector3(x, y, startPos.z);
            },
            1f,
            flyDuration
        ).SetEase(Ease.Linear);

        // 回転アニメーション
        transform.DORotate(new Vector3(0, 0, rotationAngle), flyDuration, RotateMode.FastBeyond360)
            .SetEase(Ease.OutCubic);

        // フェードアウトと破棄
        Sequence deathSequence = DOTween.Sequence();
        deathSequence.AppendInterval(flyDuration + delayBeforeFade);
        if (allRenderers.Length > 0)
        {
            foreach (var renderer in allRenderers)
            {
                deathSequence.Join(renderer.DOFade(0, fadeDuration));
            }
        }
        deathSequence.OnComplete(() => Destroy(gameObject));
    }


    private void UpdateHpGauge()
    {
        if (hpGaugeRenderer == null) return;
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
        if (skillGaugeRenderer == null) return;
        
        float clampedCooldown = Mathf.Max(0, currentCooldown);
        float cooldownPercent = skillCooldownTime > 0 ? clampedCooldown / skillCooldownTime : 0;

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
}