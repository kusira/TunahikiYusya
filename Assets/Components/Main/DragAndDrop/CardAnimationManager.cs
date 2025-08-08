using UnityEngine;
using UnityEngine.EventSystems; // イベントシステムを使うために必要
using DG.Tweening;

/// <summary>
/// 【改修版】イベントシステムを利用し、オブジェクト自身のホバーおよびドラッグ中のアニメーションを制御します。
/// Updateメソッドでのマウス監視は不要になりました。
/// </summary>
public class CardAnimationManager : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler // ← インターフェースを追加
{
    [Header("参照")]
    [Tooltip("シーンのメインカメラをここに割り当ててください")]
    [SerializeField] private Camera mainCamera; // この参照は、今後他の機能で必要になる可能性を考慮し残しておきます

    [Header("ホバーアニメーション設定")]
    [Tooltip("ホバー時にY軸方向に上昇する量")]
    [SerializeField] private float hoverMoveAmount = 0.5f;
    [Tooltip("上昇・下降アニメーションにかかる時間")]
    [SerializeField] private float moveDuration = 0.2f;

    [Header("ドラッグアニメーション設定")]
    [Tooltip("ドラッグ中に縮小するスケール（1が通常サイズ）")]
    [SerializeField] private float dragScale = 0.8f;
    [Tooltip("スケール変更アニメーションにかかる時間")]
    [SerializeField] private float scaleDuration = 0.2f;

    // --- 内部で管理する変数 ---
    private Vector3 originalLocalPosition;
    private Vector3 originalLocalScale; 
    private Tweener moveTween;
    private Tweener scaleTween;

    void Start()
    {
        originalLocalPosition = transform.localPosition;
        originalLocalScale = transform.localScale; 
        
        if (GetComponent<Collider2D>() == null)
        {
            Debug.LogWarning("CardAnimationManager: 当たり判定用のCollider2Dがこのオブジェクトにアタッチされていません。", this);
        }
    }
    
    // ▼▼▼ Updateメソッドは完全に削除しました ▼▼▼

    /// <summary>
    /// マウスカーソルがオブジェクトのColliderに入った瞬間に自動で呼ばれる
    /// </summary>
    public void OnPointerEnter(PointerEventData eventData)
    {
        // 他のキャラクターがドラッグ中の場合は、ホバーアニメーションを再生しない
        if (DragAndDropCharacterManager.IsDragging) return;
        
        AnimateUp(); // 上昇アニメーションを開始
    }

    /// <summary>
    /// マウスカーソルがオブジェクトのColliderから出た瞬間に自動で呼ばれる
    /// </summary>
    public void OnPointerExit(PointerEventData eventData)
    {
        // 他のキャラクターがドラッグ中の場合でも、カードが上がったままにならないように下降アニメーションは再生する
        AnimateDown(); // 下降アニメーションを開始
    }
    
    /// <summary>
    /// 【外部呼び出し用】ドラッグ開始時に呼ばれ、カードを縮小します。
    /// </summary>
    public void OnDragStart()
    {
        moveTween?.Kill();
        
        scaleTween?.Kill();
        scaleTween = transform.DOScale(originalLocalScale * dragScale, scaleDuration).SetEase(Ease.OutQuad);
    }

    /// <summary>
    /// 【外部呼び出し用】ドラッグ終了時に呼ばれ、カードを元のサイズと位置に戻します。
    /// </summary>
    public void OnDragEnd()
    {
        scaleTween?.Kill();
        scaleTween = transform.DOScale(originalLocalScale, scaleDuration).SetEase(Ease.OutQuad);

        // このカードの上にマウスカーソルがなければ、元の位置に戻る
        // （もしカーソルがあれば、OnPointerEnterが呼ばれて浮き上がるので自然な挙動になる）
        AnimateDown();
    }
    
    private void AnimateUp()
    {
        moveTween?.Kill();
        Vector3 targetPosition = originalLocalPosition + new Vector3(0, hoverMoveAmount, 0);
        moveTween = transform.DOLocalMove(targetPosition, moveDuration).SetEase(Ease.OutQuad);
    }

    private void AnimateDown()
    {
        moveTween?.Kill();
        moveTween = transform.DOLocalMove(originalLocalPosition, moveDuration).SetEase(Ease.OutQuad);
    }
}