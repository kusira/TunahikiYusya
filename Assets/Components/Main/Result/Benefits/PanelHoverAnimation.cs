using UnityEngine;
using UnityEngine.EventSystems;
using DG.Tweening;
using UnityEngine.InputSystem;

public class PanelHoverAnimation : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private float scaleMultiplier = 1.08f;
    [SerializeField] private float tweenDuration = 0.15f;
    [SerializeField] private Ease easeIn = Ease.OutQuad;
    [SerializeField] private Ease easeOut = Ease.InQuad;

    [Header("音響設定")]
    [SerializeField] private AudioSource audioSource;

    private RectTransform rectTransform;
    private Vector3 baseScale;
    private Tween currentTween;

    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        baseScale = rectTransform != null ? rectTransform.localScale : Vector3.one;
        
        // AudioSourceの自動取得
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
        }
    }

    void OnEnable()
    {
        KillTween();
        SetScale(baseScale);
    }

    void OnDisable()
    {
        KillTween();
        SetScale(baseScale);
    }

    void OnDestroy()
    {
        KillTween();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        // ホバー音を再生
        if (audioSource != null && audioSource.clip != null)
        {
            audioSource.Play();
        }
        
        AnimateTo(baseScale * scaleMultiplier, easeIn);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        AnimateTo(baseScale, easeOut);
    }

    private void AnimateTo(Vector3 target, Ease ease)
    {
        KillTween();
        if (rectTransform != null)
        {
            currentTween = rectTransform.DOScale(target, tweenDuration).SetEase(ease).SetUpdate(true);
        }
        else
        {
            currentTween = transform.DOScale(target, tweenDuration).SetEase(ease).SetUpdate(true);
        }
    }

    private void KillTween()
    {
        if (currentTween != null && currentTween.IsActive())
        {
            currentTween.Kill(false);
            currentTween = null;
        }
        transform.DOKill(false);
    }

    private void SetScale(Vector3 s)
    {
        if (rectTransform != null) rectTransform.localScale = s;
        else transform.localScale = s;
    }
}
