using UnityEngine;
using System.Collections.Generic;
using DG.Tweening;
using System.Collections;

/// <summary>
/// キャラクターカードのデータ。CardDatabaseから情報を取得して初期化されます。
/// </summary>
public class CharacterCardData : MonoBehaviour
{
    [Header("カードの基本データ")]
    [Tooltip("CardDatabaseに登録した名前と完全に一致させること")]
    [SerializeField] private string cardName;
    public string CardName => cardName; // ← エラー解決に必要な公開プロパティ

    [Tooltip("このカードをドラッグした時に生成されるキャラクターのプレハブ")]
    public GameObject characterPrefab;

    [Tooltip("（実行時にDBから自動設定）カードのレベル")]
    [Range(1, 3)]
    [SerializeField] private int level = 1;

    [Tooltip("（実行時にDBから自動設定）カードの所持数")]
    [Range(0, 8)]
    [SerializeField] private int count = 1;

    [Header("表示設定")]
    [SerializeField] private SpriteRenderer levelDisplayRenderer;
    [SerializeField] private SpriteRenderer countDisplayRenderer;

    [Header("スプライトアセット")]
    [SerializeField] private List<Sprite> levelSprites;
    [SerializeField] private List<Sprite> countSprites;

    [Header("アニメーション設定")]
    [SerializeField] private float punchAmount = 0.3f;
    [SerializeField] private float punchDuration = 0.3f;

    [Header("アンロックアニメーション設定")]
    [Tooltip("アンロック時に拡大するスケール")]
    [SerializeField] private float unlockScaleAmount = 1.2f;
    [Tooltip("アンロック時のアニメーション時間")]
    [SerializeField] private float unlockAnimationDuration = 0.5f;
    
    [Header("リアルタイム同期")]
    [SerializeField] private bool autoSyncFromDB = true;
    [SerializeField] private float syncInterval = 0.2f;
    private Coroutine syncRoutine;
    private int _lastSyncedLevel = int.MinValue;
    private int _lastSyncedCount = int.MinValue;

    private SpriteRenderer[] allSpriteRenderers;
    private CardAnimationManager cardAnimationManager;

    public int Level
    {
        get { return level; }
        set
        {
            level = Mathf.Clamp(value, 1, 3);
            UpdateLevelDisplay();
        }
    }
    
    public int Count
    {
        get { return count; }
        set
        {
            int oldCount = count;
            count = Mathf.Clamp(value, 0, 8);
            UpdateCountDisplay();
            
            if (count == 0 && oldCount > 0) SetExhaustedState(true);
            else if (count > 0 && oldCount == 0) SetExhaustedState(false);
        }
    }
    
    void Awake()
    {
        allSpriteRenderers = GetComponentsInChildren<SpriteRenderer>();
        cardAnimationManager = GetComponent<CardAnimationManager>();
    }
    
    void Start()
    {
        if (CardDatabase.Instance == null)
        {
            Debug.LogError("シーンにCardDatabaseが存在しません！", this);
            gameObject.SetActive(false);
            return;
        }

        CardDeckData data = CardDatabase.Instance.GetCardData(cardName);

        if (data != null)
        {
            this.Level = data.level;
            this.Count = data.count;
            CheckInitialState();
            _lastSyncedLevel = data.level;
            _lastSyncedCount = data.count;
        }
        else
        {
            Debug.LogError($"'{cardName}' のデータがCardDatabaseに見つかりません。カードを非表示にします。", this);
            gameObject.SetActive(false);
        }
    }
    
    void OnEnable()
    {
        if (autoSyncFromDB && syncRoutine == null)
            syncRoutine = StartCoroutine(CoSyncFromDB());
    }

    void OnDisable()
    {
        if (syncRoutine != null)
        {
            StopCoroutine(syncRoutine);
            syncRoutine = null;
        }
    }

    public void IncreaseLevel()
    {
        if (Level >= 3) return;
        Level++;
        AnimatePunch(levelDisplayRenderer.transform);
    }
    
    public void IncreaseCount()
    {
        if (Count >= 8) return;
        Count++;
        AnimatePunch(countDisplayRenderer.transform);
    }
    
    public void DecreaseCount()
    {
        if (Count <= 0) return;
        Count--;
        AnimatePunch(countDisplayRenderer.transform);
    }

    public void PlayUnlockAnimation()
    {
        transform.localScale = Vector3.zero;
        Sequence sequence = DOTween.Sequence();
        sequence.Append(transform.DOScale(unlockScaleAmount, unlockAnimationDuration / 2).SetEase(Ease.OutBack));
        sequence.Append(transform.DOScale(1f, unlockAnimationDuration / 2).SetEase(Ease.InSine));
    }

    private void AnimatePunch(Transform targetTransform)
    {
        if (targetTransform == null) return;
        targetTransform.DOKill(true);
        targetTransform.DOPunchScale(new Vector3(punchAmount, punchAmount, 0), punchDuration, 1, 1);
    }

    private void SetExhaustedState(bool isExhausted)
    {
        Color color = isExhausted ? Color.gray : Color.white;
        if (allSpriteRenderers != null)
        {
            foreach (var renderer in allSpriteRenderers)
            {
                renderer.color = color;
            }
        }

        if (cardAnimationManager != null)
        {
            cardAnimationManager.enabled = !isExhausted;
        }
    }
    
    private void CheckInitialState()
    {
        SetExhaustedState(count == 0);
    }
    
    private void OnValidate()
    {
        if (Application.isPlaying) return;
        if (levelDisplayRenderer != null && countDisplayRenderer != null)
        {
            UpdateLevelDisplay();
            UpdateCountDisplay();
        }
    }

    private void UpdateLevelDisplay()
    {
        if (levelDisplayRenderer == null || levelSprites == null || levelSprites.Count == 0) return;
        int spriteIndex = level - 1;
        if (spriteIndex >= 0 && spriteIndex < levelSprites.Count)
        {
            levelDisplayRenderer.sprite = levelSprites[spriteIndex];
        }
    }
    
    private void UpdateCountDisplay()
    {
        if (countDisplayRenderer == null || countSprites == null || countSprites.Count == 0) return;
        int spriteIndex = count;
        if (spriteIndex >= 0 && spriteIndex < countSprites.Count)
        {
            countDisplayRenderer.sprite = countSprites[spriteIndex];
        }
    }

    public void RefreshFromDatabase()
    {
        if (CardDatabase.Instance == null || string.IsNullOrEmpty(cardName)) return;
        var data = CardDatabase.Instance.GetCardData(cardName);
        if (data == null) return;

        if (data.level != _lastSyncedLevel)
        {
            this.Level = data.level;
            _lastSyncedLevel = data.level;
        }
        if (data.count != _lastSyncedCount)
        {
            this.Count = data.count;
            _lastSyncedCount = data.count;
        }
    }

    private IEnumerator CoSyncFromDB()
    {
        var wait = new WaitForSeconds(syncInterval);
        while (isActiveAndEnabled)
        {
            RefreshFromDatabase();
            yield return wait;
        }
    }
}