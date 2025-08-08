using UnityEngine;
using System.Collections.Generic;
using DG.Tweening;
using TMPro;

/// <summary>
/// 【味方用】左右のホルダーを1つの「行」として管理するためのクラス
/// </summary>
[System.Serializable]
public class HolderRow
{
    public GameObject leftHolder;
    public GameObject rightHolder;
}

/// <summary>
/// 【敵用】左右の敵を1つの「行」として管理するためのクラス
/// </summary>
[System.Serializable]
public class EnemyRow
{
    public GameObject leftEnemy;
    public GameObject rightEnemy;
}

/// <summary>
/// 味方と敵のキャラクター配置をリアルタイムで追跡・管理し、戦況（合計ATK）を表示する統合クラス。
/// </summary>
public class RopeManager : MonoBehaviour
{
    [Header("味方ホルダー設定")]
    [Tooltip("味方キャラクターを配置するホルダーのリスト。リストの上が最前列になります。")]
    public List<HolderRow> holderRows;

    [Header("味方キャラクター索敵設定")]
    [Tooltip("配置された味方キャラクターに付けるタグ")]
    [SerializeField] private string placedCharacterTag = "PlacedCharacter";
    
    [Header("エネミー設定")]
    [Tooltip("シーンに配置されている全ての敵オブジェクトを行ごとにアサインします。リストの上が最前列（奥側）になります。")]
    public List<EnemyRow> enemyRows;

    [Header("合計ATK表示設定")]
    [SerializeField] private TMP_Text alliedAtkText;
    [SerializeField] private TMP_Text enemyAtkText;
    [SerializeField] private TMP_Text diffAtkText;

    [Header("表示色設定")]
    [SerializeField] private Color alliedAdvantageColor = Color.blue;
    [SerializeField] private Color enemyAdvantageColor = Color.red;
    
    [Header("テキストアニメーション設定")]
    [SerializeField] private float textPunchAmount = 0.2f;
    [SerializeField] private float textPunchDuration = 0.2f;
    [SerializeField] private float diffTextMagnification = 1.2f;

    public Dictionary<GameObject, GameObject> OccupiedHolderInfo { get; private set; }
    public List<PlacedEnemy> AliveEnemies { get; private set; }
    public int TotalAlliedAtk { get; private set; }
    public int TotalEnemyAtk { get; private set; }

    private int lastAlliedAtk = -1;
    private int lastEnemyAtk = -1;
    private Vector3 initialDiffTextScale;

    void Awake()
    {
        OccupiedHolderInfo = new Dictionary<GameObject, GameObject>();
        AliveEnemies = new List<PlacedEnemy>();
        foreach (var row in enemyRows)
        {
            if (row.leftEnemy != null)
            {
                var placedEnemy = row.leftEnemy.GetComponent<PlacedEnemy>();
                if (placedEnemy != null) AliveEnemies.Add(placedEnemy);
            }
            if (row.rightEnemy != null)
            {
                var placedEnemy = row.rightEnemy.GetComponent<PlacedEnemy>();
                if (placedEnemy != null) AliveEnemies.Add(placedEnemy);
            }
        }
    }

    void Start()
    {
        if (diffAtkText != null)
        {
            initialDiffTextScale = diffAtkText.transform.localScale;
        }
        CalculateAndDisplayTotalAtk();
    }

    void OnEnable()
    {
        PlacedEnemy.OnEnemyDied += HandleEnemyDied;
    }

    void OnDisable()
    {
        PlacedEnemy.OnEnemyDied -= HandleEnemyDied;
    }

    void Update()
    {
        ScanHoldersForChanges();
        CalculateAndDisplayTotalAtk();
    }
    
    private void CalculateAndDisplayTotalAtk()
    {
        int totalAlliedAtk = 0;
        int totalEnemyAtk = 0;
        
        // --- 味方ATKの合計を計算 ---
        foreach (var characterGO in OccupiedHolderInfo.Values)
        {
            // ▼▼▼ 修正点 ▼▼▼
            // characterGOがnull（破壊済み）でないことを確認してからGetComponentを呼ぶ
            if (characterGO != null)
            {
                var placedCharacter = characterGO.GetComponent<PlacedCharacter>();
                if (placedCharacter != null)
                {
                    totalAlliedAtk += placedCharacter.atk;
                }
            }
        }

        // --- 敵ATKの合計を計算 ---
        foreach (var enemy in AliveEnemies)
        {
            // 念のため、敵リストのnullチェックも追加
            if (enemy != null)
            {
                totalEnemyAtk += enemy.atk;
            }
        }
        
        this.TotalAlliedAtk = totalAlliedAtk;
        this.TotalEnemyAtk = totalEnemyAtk;
        
        // --- UIテキストの更新処理 ---
        if (totalAlliedAtk != lastAlliedAtk)
        {
            if (alliedAtkText != null)
            {
                alliedAtkText.text = totalAlliedAtk.ToString();
                AnimateTextUpdate(alliedAtkText);
            }
        }
        
        if (totalEnemyAtk != lastEnemyAtk)
        {
            if (enemyAtkText != null)
            {
                enemyAtkText.text = totalEnemyAtk.ToString();
                AnimateTextUpdate(enemyAtkText);
            }
        }

        if (totalAlliedAtk != lastAlliedAtk || totalEnemyAtk != lastEnemyAtk)
        {
            if (diffAtkText == null) return;

            int diff = Mathf.Abs(totalAlliedAtk - totalEnemyAtk);
            diffAtkText.text = diff.ToString();
            
            if (diff == 0)
            {
                diffAtkText.color = Color.white;
                diffAtkText.transform.DOScale(initialDiffTextScale, textPunchDuration);
            }
            else
            {
                diffAtkText.color = (totalAlliedAtk > totalEnemyAtk) ? alliedAdvantageColor : enemyAdvantageColor;
                diffAtkText.transform.DOScale(initialDiffTextScale * diffTextMagnification, textPunchDuration);
            }
            
            AnimateTextUpdate(diffAtkText);
        }
        
        lastAlliedAtk = totalAlliedAtk;
        lastEnemyAtk = totalEnemyAtk;
    }

    private void AnimateTextUpdate(TMP_Text textElement)
    {
        if (textElement == null) return;
        textElement.transform.DOKill(true);
        textElement.transform.DOPunchScale(Vector3.one * textPunchAmount, textPunchDuration, 1, 0);
    }

    private void HandleEnemyDied(PlacedEnemy deadEnemy)
    {
        if (AliveEnemies.Contains(deadEnemy))
        {
            AliveEnemies.Remove(deadEnemy);
        }
    }

    private void ScanHoldersForChanges()
    {
        foreach (var row in holderRows)
        {
            CheckHolder(row.leftHolder);
            CheckHolder(row.rightHolder);
        }
    }
    
    private void CheckHolder(GameObject holderGO)
    {
        if (holderGO == null) return;
        var holderManager = holderGO.GetComponent<CharacterHolderManager>();
        if (holderManager == null) return;

        if (holderManager.IsOccupied)
        {
            if (!OccupiedHolderInfo.ContainsKey(holderGO))
            {
                Collider2D characterCollider = Physics2D.OverlapPoint(holderGO.transform.position);
                if (characterCollider != null && characterCollider.CompareTag(placedCharacterTag))
                {
                    OccupiedHolderInfo[holderGO] = characterCollider.gameObject;
                }
            }
        }
        else
        {
            if (OccupiedHolderInfo.ContainsKey(holderGO))
            {
                OccupiedHolderInfo.Remove(holderGO);
            }
        }
    }
}