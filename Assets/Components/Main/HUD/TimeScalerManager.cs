using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 各ボタンに対応する個別の関数を持つ、タイムスケール管理クラス。
/// EventTriggerから値を渡す必要がありません。
/// </summary>
public class TimeScalerManager : MonoBehaviour
{
    [Header("UIイメージ設定")]
    [Tooltip("x1速度を表すUI Image")]
    [SerializeField] private Image speed1xImage;

    [Tooltip("x2速度を表すUI Image")]
    [SerializeField] private Image speed2xImage;

    [Tooltip("x4速度を表すUI Image")]
    [SerializeField] private Image speed4xImage;

    [Header("カラー設定")]
    [Tooltip("現在選択されている速度のUI色")]
    [SerializeField] private Color activeColor = Color.yellow;

    [Tooltip("選択されていない速度のUI色")]
    [SerializeField] private Color inactiveColor = Color.gray;

    //-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-

    void Start()
    {
        // 起動時にデフォルトのx1速度に設定
        ChangeTimeScale(1.0f);
    }

    // =======================================================
    // ▼▼ EventTriggerから呼び出す専用の関数 ▼▼
    // =======================================================

    /// <summary>
    /// 【x1ボタン用】タイムスケールを1倍にする
    /// </summary>
    public void SetTimeScaleTo1()
    {
        ChangeTimeScale(1.0f);
    }

    /// <summary>
    /// 【x2ボタン用】タイムスケールを2倍にする
    /// </summary>
    public void SetTimeScaleTo2()
    {
        ChangeTimeScale(2.0f);
    }

    /// <summary>
    /// 【x4ボタン用】タイムスケールを4倍にする
    /// </summary>
    public void SetTimeScaleTo4()
    {
        ChangeTimeScale(4.0f);
    }

    // =======================================================
    // ▼▼ 内部で使われる共通の処理 ▼▼
    // =======================================================

    /// <summary>
    /// 実際にタイムスケール変更とUI更新を行う処理
    /// </summary>
    private void ChangeTimeScale(float newScale)
    {
        Debug.Log($"ボタンが押されました。タイムスケールを x{newScale} に変更します。");
        Time.timeScale = newScale;
        UpdateVisuals(newScale);
    }

    /// <summary>
    /// UIの見た目を更新する
    /// </summary>
    private void UpdateVisuals(float currentScale)
    {
        if (speed1xImage != null)
            speed1xImage.color = (Mathf.Approximately(currentScale, 1.0f)) ? activeColor : inactiveColor;
        if (speed2xImage != null)
            speed2xImage.color = (Mathf.Approximately(currentScale, 2.0f)) ? activeColor : inactiveColor;
        if (speed4xImage != null)
            speed4xImage.color = (Mathf.Approximately(currentScale, 4.0f)) ? activeColor : inactiveColor;
    }

    void OnDestroy()
    {
        Time.timeScale = 1.0f;
    }
}