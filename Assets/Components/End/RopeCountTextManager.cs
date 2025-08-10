using UnityEngine;
using TMPro;

/// <summary>
/// 現在のロープ数をTMPテキストに表示する
/// </summary>
public class RopeCountText : MonoBehaviour
{
    [Header("テキスト設定")]
    [Tooltip("表示するTMPテキストコンポーネント")]
    [SerializeField] private TMP_Text ropeCountText;
    
    [Tooltip("ロープ数の前に表示するテキスト")]
    [SerializeField] private string prefixText = "獲得ロープ数: ";
    
    [Tooltip("ロープ数の後に表示するテキスト")]
    [SerializeField] private string suffixText = "本";

    private void Start()
    {
        // TMPテキストが設定されていない場合は、自身のコンポーネントを取得
        if (ropeCountText == null)
        {
            ropeCountText = GetComponent<TMP_Text>();
        }
        
        // ロープ数を更新
        UpdateRopeCount();
    }

    /// <summary>
    /// ロープ数を更新する
    /// </summary>
    public void UpdateRopeCount()
    {
        if (ropeCountText == null) return;

        // StageManagerから現在のロープ数を取得
        var stageManager = FindAnyObjectByType<StageManager>();
        if (stageManager != null)
        {
            int currentRopeCount = stageManager.CurrentRopeCount;
            ropeCountText.text = $"{prefixText}{currentRopeCount}{suffixText}";
            
            Debug.Log($"ロープ数テキストを更新: {currentRopeCount}本");
        }
        else
        {
            ropeCountText.text = $"{prefixText}0{suffixText}";
            Debug.LogWarning("StageManagerが見つかりません。ロープ数を0として表示します。", this);
        }
    }

    /// <summary>
    /// 外部からロープ数を更新する（必要に応じて）
    /// </summary>
    public void RefreshRopeCount()
    {
        UpdateRopeCount();
    }

    /// <summary>
    /// プレフィックスとサフィックスを設定する
    /// </summary>
    public void SetTextFormat(string prefix, string suffix)
    {
        prefixText = prefix;
        suffixText = suffix;
        UpdateRopeCount();
    }
}