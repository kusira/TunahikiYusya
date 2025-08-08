using UnityEngine;
using System.Collections.Generic;
using DG.Tweening;

/// <summary>
/// キャラクターカードのデータ。CardDatabaseから情報を取得して初期化されます。
/// </summary>
public class CharacterCardData : MonoBehaviour
{
    [Header("カードの基本データ")]
    // ▼▼▼ 1. CardDatabaseと連携するための名前を追加 ▼▼▼
    [Tooltip("CardDatabaseに登録した名前と完全に一致させること")]
    [SerializeField] private string cardName;

    [Tooltip("このカードをドラッグした時に生成されるキャラクターのプレハブ")]
    public GameObject characterPrefab;

    // ▼▼▼ Inspectorでの直接編集は不要になりますが、デバッグ用に残します ▼▼▼
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
            
            if (count == 0 && oldCount > 0)
            {
                SetExhaustedState(true);
            }
            else if (count > 0 && oldCount == 0)
            {
                SetExhaustedState(false);
            }
        }
    }
    
    void Awake()
    {
        allSpriteRenderers = GetComponentsInChildren<SpriteRenderer>();
        cardAnimationManager = GetComponent<CardAnimationManager>();
    }
    
    // ▼▼▼ 2. Startメソッドをデータベースから読み込む処理に書き換え ▼▼▼
    void Start()
    {
        if (CardDatabase.Instance == null)
        {
            Debug.LogError("シーンにCardDatabaseが存在しません！", this);
            gameObject.SetActive(false);
            return;
        }

        // 自身のcardNameを使って、データベースに情報を問い合わせる
        CardDeckData data = CardDatabase.Instance.GetCardData(cardName);

        if (data != null)
        {
            // プロパティ経由で値を設定（これで表示更新なども自動で行われる）
            this.Level = data.level;
            this.Count = data.count;

            // 起動時に個数が0だった場合に備えて初期状態をチェック
            CheckInitialState();
            Debug.Log($"カード '{cardName}' のデータをロードしました。Level: {Level}, Count: {Count}");
        }
        else
        {
            Debug.LogError($"'{cardName}' のデータがCardDatabaseに見つかりません。カードを非表示にします。", this);
            gameObject.SetActive(false);
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
        if (Application.isPlaying) return; // 実行中はOnValidateでの更新を止める

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
}