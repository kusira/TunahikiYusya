using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class PauseManager : MonoBehaviour
{
    [Header("UI要素")]
    [SerializeField] private GameObject pauseContainer;
    [SerializeField] private Image pauseBlackGround;
    [SerializeField] private RectTransform pausePanel;
    
    [Header("ボタン")]
    [SerializeField] private Button pauseButton;
    [SerializeField] private Button closeButton;
    
    [Header("音声設定")]
    [Tooltip("ポーズボタンを押すときに再生するAudioSource")]
    [SerializeField] private AudioSource pauseButtonSE;
    [Tooltip("ポーズを閉じるときに再生するAudioSource")]
    [SerializeField] private AudioSource closeButtonSE;
    
    [Header("アニメーション設定")]
    [SerializeField] private float backgroundFadeDuration = 0.5f;
    [SerializeField] private float panelSlideDuration = 0.3f;
    [SerializeField] private float panelSlideDistance = 100f;
    
    [Header("無効化するマネージャー")]
    [SerializeField] private MonoBehaviour dragAndDropManager;
    
    private bool isPaused = false;
    
    private void Start()
    {
        // ボタンのイベントを設定
        SetupButtons();
        
        // 初期状態ではポーズウィンドウを非表示
        if (pauseContainer != null)
        {
            pauseContainer.SetActive(false);
        }
    }
    
    private void SetupButtons()
    {
        if (pauseButton != null)
            pauseButton.onClick.AddListener(ShowPause);
        
        if (closeButton != null)
            closeButton.onClick.AddListener(HidePause);
    }
    
    public void ShowPause()
    {
        if (pauseContainer == null || isPaused) return;
        
        // ポーズボタンの音を再生
        PlayPauseButtonSound();
        
        isPaused = true;
        
        // コンテナを有効化
        pauseContainer.SetActive(true);
        
        // マネージャーを無効化
        DisableManagers();
        
        // 背景をフェードイン
        if (pauseBlackGround != null)
        {
            pauseBlackGround.color = new Color(0, 0, 0, 0);
            pauseBlackGround.DOFade(0.8f, backgroundFadeDuration);
        }
        
        // パネルを下からスライドイン
        if (pausePanel != null)
        {
            pausePanel.anchoredPosition = new Vector2(0, -panelSlideDistance);
            pausePanel.DOAnchorPosY(0, panelSlideDuration).SetEase(Ease.OutBack);
        }
    }
    
    public void HidePause()
    {
        if (pauseContainer == null) return;
        
        // 閉じるボタンの音を再生
        PlayCloseButtonSound();
        
        // マネージャーを再有効化
        EnableManagers();
        
        // MainCameraManagerのホバー状態をfalseにリセット
        MainCameraManager mainCameraManager = FindAnyObjectByType<MainCameraManager>();
        if (mainCameraManager != null)
        {
            // ホバー状態をリセットするメソッドを呼び出し
            mainCameraManager.ResetHoverState();
        }
        
        // 背景をフェードアウト
        if (pauseBlackGround != null)
        {
            pauseBlackGround.DOFade(0, backgroundFadeDuration);
        }
        
        // パネルを下にスライドアウト
        if (pausePanel != null)
        {
            pausePanel.DOAnchorPosY(-panelSlideDistance, panelSlideDuration).SetEase(Ease.InBack);
        }
        
        // アニメーション完了後にコンテナを無効化
        DOVirtual.DelayedCall(panelSlideDuration, () => {
            pauseContainer.SetActive(false);
            isPaused = false;
        });
    }
    
    private void DisableManagers()
    {
        // DragAndDropManagerを無効化
        if (dragAndDropManager != null)
        {
            dragAndDropManager.enabled = false;
        }
        
        // MainCameraManagerをFindObjectOfTypeで探して無効化
        MainCameraManager mainCameraManager = FindAnyObjectByType<MainCameraManager>();
        if (mainCameraManager != null)
        {
            mainCameraManager.enabled = false;
        }
    }
    
    private void EnableManagers()
    {
        // DragAndDropManagerを再有効化
        if (dragAndDropManager != null)
        {
            dragAndDropManager.enabled = true;
        }
        
        // MainCameraManagerをFindObjectOfTypeで探して再有効化
        MainCameraManager mainCameraManager = FindAnyObjectByType<MainCameraManager>();
        if (mainCameraManager != null)
        {
            mainCameraManager.enabled = true;
        }
    }
    
    /// <summary>
    /// ポーズボタンの音を再生する
    /// </summary>
    private void PlayPauseButtonSound()
    {
        if (pauseButtonSE != null)
        {
            pauseButtonSE.Play();
        }
    }
    
    /// <summary>
    /// ポーズを閉じる音を再生する
    /// </summary>
    private void PlayCloseButtonSound()
    {
        if (closeButtonSE != null)
        {
            closeButtonSE.Play();
        }
    }
    
    private void OnDestroy()
    {
        // ボタンのイベントを解除
        if (pauseButton != null)
            pauseButton.onClick.RemoveListener(ShowPause);
        
        if (closeButton != null)
            closeButton.onClick.RemoveListener(HidePause);
    }
}