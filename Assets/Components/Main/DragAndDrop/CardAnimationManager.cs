using UnityEngine;
using UnityEngine.EventSystems;
using DG.Tweening;

/// <summary>
/// 【改修版】イベントシステムを利用し、オブジェクト自身のホバーおよびドラッグ中のアニメーションを制御します。
/// PanelHoverAnimationと同じ判定ロジックを使用しています。
/// </summary>
public class CardAnimationManager : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("参照")]
    [SerializeField] private Camera mainCamera;

    [Header("ホバーアニメーション設定")]
    [SerializeField] private float hoverMoveAmount = 0.5f;
    [SerializeField] private float moveDuration = 0.2f;

    [Header("ドラッグアニメーション設定")]
    [SerializeField] private float dragScale = 0.8f;
    [SerializeField] private float scaleDuration = 0.2f;

    // --- 内部で管理する変数 ---
    private Vector3 originalLocalPosition;
    private Vector3 originalLocalScale; 
    private Tweener moveTween;
    private Tweener scaleTween;
    private bool isHovered = false;

    void Start()
    {
        originalLocalPosition = transform.localPosition;
        originalLocalScale = transform.localScale;
        
        // Collider2Dの存在確認と警告
        if (GetComponent<Collider2D>() == null)
        {
            Debug.LogWarning("CardAnimationManager: 当たり判定用のCollider2Dがこのオブジェクトにアタッチされていません。", this);
        }
    }

    void OnEnable()
    {
        KillMoveTween();
        SetPosition(originalLocalPosition);
    }

    void OnDisable()
    {
        KillMoveTween();
        SetPosition(originalLocalPosition);
    }

    void OnDestroy()
    {
        KillMoveTween();
        KillScaleTween();
    }

    /// <summary>
    /// マウスカーソルがオブジェクトのColliderに入った瞬間に自動で呼ばれる
    /// </summary>
    public void OnPointerEnter(PointerEventData eventData)
    {
        // 他のキャラクターがドラッグ中の場合は、ホバーアニメーションを再生しない
        if (DragAndDropCharacterManager.IsDragging) return;
        
        isHovered = true;
        AnimateUp();
    }

    /// <summary>
    /// マウスカーソルがオブジェクトのColliderから出た瞬間に自動で呼ばれる
    /// </summary>
    public void OnPointerExit(PointerEventData eventData)
    {
        isHovered = false;
        AnimateDown();
    }
    
    /// <summary>
    /// 【外部呼び出し用】ドラッグ開始時に呼ばれ、カードを縮小します。
    /// </summary>
    public void OnDragStart()
    {
        KillMoveTween();
        
        KillScaleTween();
        scaleTween = transform.DOScale(originalLocalScale * dragScale, scaleDuration).SetEase(Ease.OutQuad);
    }

    /// <summary>
    /// 【外部呼び出し用】ドラッグ終了時に呼ばれ、カードを元のサイズと位置に戻します。
    /// </summary>
    public void OnDragEnd()
    {
        KillScaleTween();
        scaleTween = transform.DOScale(originalLocalScale, scaleDuration).SetEase(Ease.OutQuad);

        // このカードの上にマウスカーソルがなければ、元の位置に戻る
        if (!isHovered)
        {
            AnimateDown();
        }
    }
    
    private void AnimateUp()
    {
        Vector3 targetPosition = originalLocalPosition + new Vector3(0, hoverMoveAmount, 0);
        AnimateTo(targetPosition, Ease.OutQuad);
    }

    private void AnimateDown()
    {
        AnimateTo(originalLocalPosition, Ease.OutQuad);
    }

    private void AnimateTo(Vector3 target, Ease ease)
    {
        KillMoveTween();
        moveTween = transform.DOLocalMove(target, moveDuration).SetEase(ease).SetUpdate(true);
    }

    private void KillMoveTween()
    {
        if (moveTween != null && moveTween.IsActive())
        {
            moveTween.Kill(false);
            moveTween = null;
        }
    }

    private void KillScaleTween()
    {
        if (scaleTween != null && scaleTween.IsActive())
        {
            scaleTween.Kill(false);
            scaleTween = null;
        }
    }

    private void SetPosition(Vector3 position)
    {
        transform.localPosition = position;
    }
    
    /// <summary>
    /// 現在ホバー状態かどうかを取得
    /// </summary>
    public bool IsHovered => isHovered;
}