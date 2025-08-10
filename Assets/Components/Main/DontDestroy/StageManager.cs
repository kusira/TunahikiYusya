// Assets/Components/Main/StageManager.cs
using UnityEngine;
using System.Collections;

public class StageManager : MonoBehaviour
{
    public static StageManager Instance { get; private set; }

    [Header("初期値")]
    [SerializeField] private int defaultStage = 1;

    public int CurrentStage { get; private set; } = 1;
    
    // 獲得したロープ数を保持
    public int CurrentRopeCount { get; private set; } = 0;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject); // 既存のインスタンスを維持
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        // 初期化（既に値が入っていれば維持）
        if (CurrentStage <= 0) CurrentStage = Mathf.Max(1, defaultStage);
    }

    // 外部から発火: ステージを+1
    public void IncrementStage()
    {
        CurrentStage++;
    }

    // 外部から発火: ステージをリセット（デフォルトは1）
    public void ResetStage()
    {
        CurrentStage = Mathf.Max(1, defaultStage);
        CurrentRopeCount = 0; // ロープ数もリセット
    }

    // 必要なら任意の数値へ直接セット（省略可）
    public void ResetStageTo(int stage)
    {
        CurrentStage = Mathf.Max(1, stage);
    }
    
    // ロープ数を増加させる
    public void AddRope(int count = 1)
    {
        CurrentRopeCount += count;
    }
    
    // ロープ数を設定する
    public void SetRopeCount(int count)
    {
        CurrentRopeCount = Mathf.Max(0, count);
    }
}