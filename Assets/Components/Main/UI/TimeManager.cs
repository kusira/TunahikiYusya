using UnityEngine;
using TMPro; // TextMeshProを使用するために必要
using System.Collections;

/// <summary>
/// ゲーム内の時間を管理するクラス
/// </summary>
public class TimeManager : MonoBehaviour
{
    [Header("タイマー設定")]
    [Tooltip("時間を表示するTextMeshProのUIテキスト")]
    [SerializeField]
    private TMP_Text timeText;

    [Tooltip("タイマーの初期値（秒）")]
    [SerializeField]
    private int initialTime = 60; // Inspectorから初期値を設定できるようにする

    // --- プライベート変数 ---
    public int CurrentTime { get; private set; }
    private Coroutine _countdownCoroutine;
    private BattleBeginsManager _battleBeginsManager;
    private bool _isCountdownStarted = false;

    void Start()
    {
        // シーン内からBattleBeginsManagerのインスタンスを探す
        _battleBeginsManager = FindAnyObjectByType<BattleBeginsManager>();
        if (_battleBeginsManager == null)
        {
            Debug.LogError("シーン内に BattleBeginsManager が見つかりません。");
            // BattleBeginsManagerが見つからない場合は、このコンポーネントを無効にする
            enabled = false;
            return;
        }

        // タイマーの初期化
        ResetTimer();
    }

    void Update()
    {
        // まだカウントダウンが開始されておらず、バトルが開始されたら
        if (!_isCountdownStarted && _battleBeginsManager.IsInBattle)
        {
            // カウントダウンを開始する
            StartCountdown();
        }
    }

    /// <summary>
    /// カウントダウンを開始する
    /// </summary>
    public void StartCountdown()
    {
        // 既に開始されている場合は何もしない
        if (_isCountdownStarted) return;

        _isCountdownStarted = true;

        // 既に実行中のコルーチンがあれば念のため停止する
        if (_countdownCoroutine != null)
        {
            StopCoroutine(_countdownCoroutine);
        }

        // カウントダウンのコルーチンを開始
        _countdownCoroutine = StartCoroutine(CountdownCoroutine());
        Debug.Log("タイマー開始！");
    }

    /// <summary>
    /// 1秒ごとに時間を減らすコルーチン
    /// </summary>
    private IEnumerator CountdownCoroutine()
    {
        // 現在の時間が0より大きい間、ループを続ける
        while (CurrentTime > 0)
        {
            // 1秒待機
            yield return new WaitForSeconds(1f);

            // 時間を1減らす
            CurrentTime--;

            // UIテキストを更新
            timeText.text = CurrentTime.ToString();
        }

        Debug.Log("時間切れ！");
        // ここに時間切れになった際の処理を追加できます
    }

    /// <summary>
    /// タイマーを初期状態にリセットする
    /// </summary>
    public void ResetTimer()
    {
        // 実行中のコルーチンがあれば停止
        if (_countdownCoroutine != null)
        {
            StopCoroutine(_countdownCoroutine);
            _countdownCoroutine = null;
        }

        // 各変数を初期状態に戻す
        CurrentTime = initialTime;
        if (timeText != null)
        {
            timeText.text = CurrentTime.ToString();
        }
        _isCountdownStarted = false;
    }
}