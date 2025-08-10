using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;
using System.Collections.Generic;

public class TutorialManager : MonoBehaviour
{
    [Header("チュートリアル画像")]
    [SerializeField] private List<Sprite> tutorialImages = new List<Sprite>();
    
    [Header("UI要素")]
    [SerializeField] private GameObject tutorialContainer;
    [SerializeField] private Image tutorialBackground;
    [SerializeField] private RectTransform tutorialPanel;
    [SerializeField] private Button prevButton;
    [SerializeField] private Button nextButton;
    [SerializeField] private TextMeshProUGUI progressText;
    [SerializeField] private Button closeButton;
    [SerializeField] private Image tutorialImageDisplay;
    
    [Header("無効化するマネージャー")]
    [SerializeField] private MonoBehaviour dragAndDropManager;
    
    [Header("アニメーション設定")]
    [SerializeField] private float backgroundFadeDuration = 0.5f;
    [SerializeField] private float panelSlideDuration = 0.3f;
    [SerializeField] private float panelSlideDistance = 100f;
    
    private int currentImageIndex = 0;
    private bool isTutorialShown = false;
    
    private void Start()
    {
        // ボタンのイベントを設定
        SetupButtons();
    }
    
    private void SetupButtons()
    {
        if (prevButton != null)
            prevButton.onClick.AddListener(ShowPreviousImage);
        
        if (nextButton != null)
            nextButton.onClick.AddListener(ShowNextImage);
        
        if (closeButton != null)
            closeButton.onClick.AddListener(HideTutorial);
    }
    
    public void ShowTutorial()
    {
        if (tutorialContainer == null || isTutorialShown) return;
        
        isTutorialShown = true;
        currentImageIndex = 0;
        
        // コンテナを有効化
        tutorialContainer.SetActive(true);
        
        // マネージャーを無効化
        DisableManagers();
        
        // 背景をフェードイン
        if (tutorialBackground != null)
        {
            tutorialBackground.color = new Color(0, 0, 0, 0);
            tutorialBackground.DOFade(0.8f, backgroundFadeDuration);
        }
        
        // パネルを下からスライドイン
        if (tutorialPanel != null)
        {
            tutorialPanel.anchoredPosition = new Vector2(0, -panelSlideDistance);
            tutorialPanel.DOAnchorPosY(0, panelSlideDuration).SetEase(Ease.OutBack);
        }
        
        // 最初の画像を表示
        UpdateTutorialDisplay();
        UpdateButtonStates();
    }
    
    public void HideTutorial()
    {
        if (tutorialContainer == null) return;
        
        // マネージャーを再有効化
        EnableManagers();
        
        // 背景をフェードアウト
        if (tutorialBackground != null)
        {
            tutorialBackground.DOFade(0, backgroundFadeDuration);
        }
        
        // パネルを下にスライドアウト
        if (tutorialPanel != null)
        {
            tutorialPanel.DOAnchorPosY(-panelSlideDistance, panelSlideDuration).SetEase(Ease.InBack);
        }
        
        // アニメーション完了後にコンテナを無効化
        DOVirtual.DelayedCall(panelSlideDuration, () => {
            tutorialContainer.SetActive(false);
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
    
    private void ShowPreviousImage()
    {
        if (currentImageIndex > 0)
        {
            currentImageIndex--;
            UpdateTutorialDisplay();
            UpdateButtonStates();
        }
    }
    
    private void ShowNextImage()
    {
        if (currentImageIndex < tutorialImages.Count - 1)
        {
            currentImageIndex++;
            UpdateTutorialDisplay();
            UpdateButtonStates();
        }
    }
    
    private void UpdateTutorialDisplay()
    {
        if (tutorialImageDisplay != null && tutorialImages.Count > 0)
        {
            if (currentImageIndex < tutorialImages.Count)
            {
                tutorialImageDisplay.sprite = tutorialImages[currentImageIndex];
            }
        }
        
        // 進捗テキストを更新
        if (progressText != null)
        {
            progressText.text = $"{currentImageIndex + 1} / {tutorialImages.Count}";
        }
    }
    
    private void UpdateButtonStates()
    {
        // Prevボタンの表示/非表示
        if (prevButton != null)
        {
            prevButton.gameObject.SetActive(currentImageIndex > 0);
        }
        
        // Nextボタンの表示/非表示
        if (nextButton != null)
        {
            nextButton.gameObject.SetActive(currentImageIndex < tutorialImages.Count - 1);
        }
    }
    
    private void OnDestroy()
    {
        // ボタンのイベントを解除
        if (prevButton != null)
            prevButton.onClick.RemoveListener(ShowPreviousImage);
        
        if (nextButton != null)
            nextButton.onClick.RemoveListener(ShowNextImage);
        
        if (closeButton != null)
            closeButton.onClick.RemoveListener(HideTutorial);
    }
}
