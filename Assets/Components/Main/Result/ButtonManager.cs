using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using DG.Tweening;

public class ButtonManager : MonoBehaviour
{
    [Header("ボタン (UI Image)")]
    [SerializeField] private Image titleButton;   // TitleScene へ
    [SerializeField] private Image replayButton;  // MainScene へ (リセット付き)
    [SerializeField] private Image xButton;       // X (Twitter) 共有

    [Header("クリックアニメーション")]
    [SerializeField] private float pressScale = 0.92f;
    [SerializeField] private float tweenDuration = 0.12f; // 非スケール秒
    [SerializeField] private Ease pressEase = Ease.OutQuad;
    [SerializeField] private Ease releaseEase = Ease.InQuad;

    private StageManager stageManager;
    private CardDatabase cardDatabase;
    private TransitionManager transitionManager;

    private void Awake()
    {
        // 探すコンポーネント
        stageManager = StageManager.Instance != null
            ? StageManager.Instance
            : FindAnyObjectByType<StageManager>(FindObjectsInactive.Include);

        cardDatabase = CardDatabase.Instance != null
            ? CardDatabase.Instance
            : FindAnyObjectByType<CardDatabase>(FindObjectsInactive.Include);

        transitionManager = FindAnyObjectByType<TransitionManager>(FindObjectsInactive.Include);
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
        clickEntry.callback.AddListener(_ => onClick?.Invoke());
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
        LoadSceneWithTransitionOrDirect("TitleScene");
    }

    private void OnReplayClicked()
    {
        // MainScene に移動する前に初期化
        if (stageManager != null) stageManager.ResetStage();
        if (cardDatabase != null) cardDatabase.ResetToDefaults();

        LoadSceneWithTransitionOrDirect("MainScene");
    }

    private void OnXClicked()
    {
        int stage = stageManager != null ? stageManager.CurrentStage : 1;
        string text = $"CurrentStage: {stage}";
        string url = "https://twitter.com/intent/tweet?text=" + Uri.EscapeDataString(text);
        Application.OpenURL(url);
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
}

