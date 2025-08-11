using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using DG.Tweening;

[RequireComponent(typeof(Image))]
[RequireComponent(typeof(CanvasGroup))]
[RequireComponent(typeof(AudioSource))] // AudioSourceを必須コンポーネントに追加
public class BattleBeginsManager : MonoBehaviour, IPointerClickHandler
{
    [Header("ボタンのアニメーション設定")]
    [Tooltip("クリック時に縮小する目標のスケール（例: 0.9で90%のサイズになる）")]
    [SerializeField] private float clickDownScale = 0.9f;
    [Tooltip("縮小にかかる時間")]
    [SerializeField] private float shrinkDuration = 0.15f;
    [Tooltip("縮小してからフェードアウトが始まるまでの待機時間（秒）")]
    [SerializeField] private float delayBeforeFadeOut = 0.1f;
    [Tooltip("フェードアウトにかかる時間")]
    [SerializeField] private float fadeOutDuration = 0.5f;

    [Header("バトル開始テキスト設定")]
    [Tooltip("アニメーションさせる「バトル開始！」テキストのGameObject")]
    [SerializeField] private GameObject battleStartTextObject;
    [Tooltip("テキストが登場する時のアニメーション時間")]
    [SerializeField] private float textAppearDuration = 0.5f;
    [Tooltip("テキストが画面に表示され続ける時間")]
    [SerializeField] private float textDisplayDuration = 1.0f;
    [Tooltip("テキストが消える時のフェードアウト時間")]
    [SerializeField] private float textFadeOutDuration = 0.3f;

    [Header("音声設定")]
    [Tooltip("バトル開始ボタンクリック時に再生するAudioSource")]
    [SerializeField] private AudioSource buttonClickAudio;

    /// <summary>
    /// 【変更点】staticを削除。このインスタンスがバトル中かどうかを示すプロパティ。
    /// </summary>
    public bool IsInBattle { get; private set; }

    private RectTransform rectTransform;
    private CanvasGroup canvasGroup;

    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();
        IsInBattle = false;

        if (battleStartTextObject != null)
        {
            battleStartTextObject.SetActive(false);
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (IsInBattle || DragAndDropCharacterManager.IsDragging)
        {
            return;
        }

        // ボタンクリック音を再生
        if (buttonClickAudio != null)
        {
            buttonClickAudio.Play();
        }

        IsInBattle = true;
        canvasGroup.interactable = false;

        AnimateButton();
        AnimateBattleStartText();

        Debug.Log("バトル開始！");
    }

    private void AnimateButton()
    {
        Sequence buttonAnimation = DOTween.Sequence();
        buttonAnimation.Append(rectTransform.DOScale(clickDownScale, shrinkDuration));
        buttonAnimation.AppendInterval(delayBeforeFadeOut);
        buttonAnimation.Append(canvasGroup.DOFade(0, fadeOutDuration).SetEase(Ease.InQuad));
        buttonAnimation.OnComplete(() =>
        {
            // 音がなくなるまで待ってから非アクティブにする
            StartCoroutine(WaitForAudioToFinish());
        });
    }

    /// <summary>
    /// 音の再生が完了するまで待ってからGameObjectを非アクティブにする
    /// </summary>
    private System.Collections.IEnumerator WaitForAudioToFinish()
    {
        // 音が再生中の場合、再生が完了するまで待つ
        if (buttonClickAudio != null && buttonClickAudio.isPlaying)
        {
            yield return new WaitWhile(() => buttonClickAudio.isPlaying);
        }
        
        // 音の再生が完了したら非アクティブにする
        gameObject.SetActive(false);
    }
    
    private void AnimateBattleStartText()
    {
        if (battleStartTextObject == null) return;
        
        CanvasGroup textCanvasGroup = battleStartTextObject.GetComponent<CanvasGroup>();
        if (textCanvasGroup == null)
        {
            textCanvasGroup = battleStartTextObject.AddComponent<CanvasGroup>();
            Debug.LogWarning("BattleStartTextObjectにCanvasGroupがなかったので自動で追加しました。フェードに必要です。", battleStartTextObject);
        }
        Transform textTransform = battleStartTextObject.transform;
        
        textTransform.localScale = Vector3.zero;
        textCanvasGroup.alpha = 0f;
        battleStartTextObject.SetActive(true);

        Sequence textAnimation = DOTween.Sequence();
        
        textAnimation.Join(textTransform.DOScale(1f, textAppearDuration).SetEase(Ease.OutBack)); 
        textAnimation.Join(textCanvasGroup.DOFade(1f, textAppearDuration / 2));
        textAnimation.AppendInterval(textDisplayDuration);
        textAnimation.Append(textCanvasGroup.DOFade(0, textFadeOutDuration));
        
        textAnimation.OnComplete(() =>
        {
            battleStartTextObject.SetActive(false);
        });
    }
    
    public void ResetButton()
    {
        IsInBattle = false;
        gameObject.SetActive(true);
        rectTransform.localScale = Vector3.one;
        canvasGroup.alpha = 1f;
        canvasGroup.interactable = true;
    }
}