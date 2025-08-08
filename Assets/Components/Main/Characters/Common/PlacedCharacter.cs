using UnityEngine;
using DG.Tweening;
using System;
using Random = UnityEngine.Random;

public class PlacedCharacter : MonoBehaviour
{
    public static int HpToTransfer = -1;

    #region Serialized Fields
    [Header("キャラクター識別")]
    [Tooltip("データベースで設定したキャラクター名と完全に一致させる")]
    [SerializeField] private string characterName;

    // レベルはCardDatabaseから自動取得するため、Inspectorでの設定は不要になります
    // public int Level { get; private set; }

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
    public int Level { get; private set; } // レベルを保持するプロパティ

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
    [SerializeField] private float delayBeforeFade = 1.5f;
    [SerializeField] private float fadeDuration = 0.5f;
    #endregion

    public bool IsPaused { get; private set; } = false;
    
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
        // ▼▼▼ Startメソッドを正しいロジックに修正 ▼▼▼
        
        // --- 1. CardDatabaseからレベルを取得 ---
        if (CardDatabase.Instance == null)
        {
            Debug.LogError("シーンにCardDatabaseが存在しません！", this);
            gameObject.SetActive(false);
            return;
        }
        // Inspectorで設定されたcharacterNameを使って、カードのデッキ情報を取得
        CardDeckData deckData = CardDatabase.Instance.GetCardData(characterName);
        if (deckData == null)
        {
            Debug.LogError($"CardDatabaseに '{characterName}' のデータが見つかりません。オブジェクトを非アクティブ化します。", this);
            gameObject.SetActive(false);
            return;
        }
        // 取得したレベルをこのキャラクターのレベルとして設定
        this.Level = deckData.level;

        // --- 2. CharacterDatabaseからステータスを取得 ---
        if (database == null) { Debug.LogError("シーンにCharacterDatabaseが見つかりません！", this); return; }

        // characterNameと、上記で取得したLevelを使ってステータスを取得
        CharacterLevelStats stats = database.GetStats(characterName, this.Level);
        if (stats == null)
        {
            Debug.LogError($"CharacterDatabaseに '{characterName}' (Level {this.Level}) のデータが見つかりません。", this);
            gameObject.SetActive(false);
            return;
        }

        // --- 3. ステータスを初期化 ---
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

        // --- 4. UIゲージを初期化 ---
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
    
    public void Flash(Color color, float duration)
    {
        if (mainSpriteRenderer == null || duration <= 0) return;

        mainSpriteRenderer.DOKill();
        mainSpriteRenderer.DOColor(color, duration / 2).OnComplete(() =>
        {
            mainSpriteRenderer.DOColor(Color.white, duration / 2);
        });
    }
    #endregion
    
    #region Private Helper Methods
    private void FlashColor(Color flashColor)
    {
        Flash(flashColor, flashDuration);
    }
    
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
    
    public void Die()
    {
        if (isDead) return;
        isDead = true;

        if (CurrentHolder != null)
        {
            CurrentHolder.SetOccupied(false);
            CurrentHolder = null; // オプション：参照を切る
        }
        
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