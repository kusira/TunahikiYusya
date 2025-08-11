using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using DG.Tweening;
using unityroom.Api;

public class ButtonManager : MonoBehaviour
{
    [Header("ボタン (UI Image)")]
    [SerializeField] private Image titleButton;   // TitleScene へ
    [SerializeField] private Image replayButton;  // MainScene へ (リセット付き)
    [SerializeField] private Image xButton;       // X (Twitter) 共有

    [Header("X投稿設定")]
    [SerializeField] private string[] hashtags = { "#ゲーム", "#Unity" };
    [SerializeField] private string shareUrl = "https://example.com";
    [SerializeField] private bool isGameClear = false; // インスペクタで切り替え

    [Header("スコア送信設定")]
    [SerializeField] private bool enableScoreSubmission = true; // スコア送信するかどうか

    [Header("クリックアニメーション")]
    [SerializeField] private float pressScale = 0.92f;
    [SerializeField] private float tweenDuration = 0.12f; // 非スケール秒
    [SerializeField] private Ease pressEase = Ease.OutQuad;
    [SerializeField] private Ease releaseEase = Ease.InQuad;

    [Header("音響")]
    [SerializeField] private AudioSource audioSource; // AudioSourceコンポーネント

    private StageManager stageManager;
    private CardDatabase cardDatabase;
    private TransitionManager transitionManager;

    private void Awake()
    {
        // AudioSourceがアサインされていない場合は自動で取得
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
            }
        }

        // 探すコンポーネント
        stageManager = StageManager.Instance != null
            ? StageManager.Instance
            : FindAnyObjectByType<StageManager>(FindObjectsInactive.Include);

        cardDatabase = CardDatabase.Instance != null
            ? CardDatabase.Instance
            : FindAnyObjectByType<CardDatabase>(FindObjectsInactive.Include);

        transitionManager = FindAnyObjectByType<TransitionManager>(FindObjectsInactive.Include);

        // スコア送信（設定が有効な場合のみ）
        if (enableScoreSubmission)
        {
            UnityroomApiClient.Instance.SendScore(2, stageManager != null ? stageManager.CurrentRopeCount : 0, ScoreboardWriteMode.HighScoreDesc);
            if(isGameClear)
            {
                UnityroomApiClient.Instance.SendScore(1, stageManager != null ? stageManager.CurrentStage - 1: 1, ScoreboardWriteMode.HighScoreDesc);
            }
            else
            {
                UnityroomApiClient.Instance.SendScore(1, stageManager != null ? stageManager.CurrentStage: 1, ScoreboardWriteMode.HighScoreDesc);
            }
        }
    }

    private void Start()
    {
        // UI イベントを登録
        AttachUIEvents(titleButton, OnTitleClicked);
        AttachUIEvents(replayButton, OnReplayClicked);
        AttachUIEvents(xButton, OnXClicked);
    }

    private void AttachUIEvents(Image targetImage, Action onClick)
    {
        if (targetImage == null) return;
        var trigger = targetImage.GetComponent<EventTrigger>();
        if (trigger == null) trigger = targetImage.gameObject.AddComponent<EventTrigger>();

        // PointerDown -> 縮小
        var downEntry = new EventTrigger.Entry { eventID = EventTriggerType.PointerDown };
        downEntry.callback.AddListener(_ => AnimatePress(targetImage.transform));
        trigger.triggers.Add(downEntry);

        // PointerUp -> リリース
        var upEntry = new EventTrigger.Entry { eventID = EventTriggerType.PointerUp };
        upEntry.callback.AddListener(_ => AnimateRelease(targetImage.transform));
        trigger.triggers.Add(upEntry);

        // PointerClick -> アクション
        var clickEntry = new EventTrigger.Entry { eventID = EventTriggerType.PointerClick };
        clickEntry.callback.AddListener(_ => {
            PlayButtonSound(); // ボタンクリック音を再生
            onClick?.Invoke();
        });
        trigger.triggers.Add(clickEntry);
    }

    private void AnimatePress(Transform target)
    {
        if (target == null) return;
        target.DOKill(false);
        target.DOScale(pressScale, tweenDuration).SetEase(pressEase).SetUpdate(true);
    }

    private void AnimateRelease(Transform target)
    {
        if (target == null) return;
        target.DOKill(false);
        target.DOScale(1f, tweenDuration).SetEase(releaseEase).SetUpdate(true);
    }

    // --- Click Handlers ---
    private void OnTitleClicked()
    {
        // TitleScene に移動する前に初期化
        if (stageManager != null) stageManager.ResetStage();
        if (cardDatabase != null) cardDatabase.ResetToDefaults();
        LoadSceneWithTransitionOrDirect("TitleScene");
        Time.timeScale = 1;
    }

    private void OnReplayClicked()
    {
        // MainScene に移動する前に初期化
        if (stageManager != null) stageManager.ResetStage();
        if (cardDatabase != null) cardDatabase.ResetToDefaults();
        Time.timeScale = 1;
        LoadSceneWithTransitionOrDirect("MainScene");
    }

    private void OnXClicked()
    {
        string mainText = GenerateMainText();
        string hashtagText = string.Join(" ", hashtags);
        string fullText = $"{mainText}\n{hashtagText}\n\n{shareUrl}";
        
        string url = "https://twitter.com/intent/tweet?text=" + Uri.EscapeDataString(fullText);
        Application.OpenURL(url);
    }

    private string GenerateMainText()
    {
        if (isGameClear)
        {
            // クリア時：StageManagerからロープ数を取得
            int ropeCount = stageManager != null ? stageManager.CurrentRopeCount : 0;
            return $"ゲームクリア！ロープを{ropeCount}本獲得しました！";
        }
        else
        {
            // 通常時：ステージ数を取得
            int stage = stageManager != null ? stageManager.CurrentStage : 1;
            return $"ステージ{stage}まで到達しました！";
        }
    }

    private void LoadSceneWithTransitionOrDirect(string sceneName)
    {
        if (transitionManager != null)
        {
            transitionManager.PlayFadeOutAndLoad(sceneName);
        }
        else
        {
            // Result表示中は timeScale=0 なので、遷移直前に元に戻しておく
            Time.timeScale = 1f;
            SceneManager.LoadScene(sceneName);
        }
    }

    /// <summary>
    /// ボタンクリック音を再生
    /// </summary>
    private void PlayButtonSound()
    {
        if (audioSource != null && audioSource.clip != null)
        {
            audioSource.Play();
        }
    }
}

