using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;

/// <summary>
/// 最終的なEnd画面の管理を行う
/// </summary>
public class EndManager : MonoBehaviour
{
    [System.Serializable]
    public class DisplayItem
    {
        [Tooltip("表示するGameObject")]
        public GameObject displayObject;
        
        [Tooltip("アニメーションタイプ")]
        public AnimationType animationType;
    }

    [Header("表示設定")]
    [Tooltip("表示するオブジェクトとアニメーションタイプのリスト")]
    [SerializeField] private List<DisplayItem> displayItems = new List<DisplayItem>();

    [Tooltip("各オブジェクトの表示間隔（秒）")]
    [SerializeField] private float delayBetweenObjects = 0.5f;

    [Header("アニメーション設定")]
    [Tooltip("サイズ0から大きくなるアニメーション時間")]
    [SerializeField] private float scaleAnimDuration = 0.8f;

    [Tooltip("下からフェードインするアニメーション時間")]
    [SerializeField] private float fadeInAnimDuration = 0.8f;

    [Tooltip("下からフェードインする移動距離")]
    [SerializeField] private float fadeInMoveDistance = 50f;

    [Header("イージング設定")]
    [Tooltip("サイズアニメーションのイージング")]
    [SerializeField] private Ease scaleEase = Ease.OutBack;

    [Tooltip("フェードインアニメーションのイージング")]
    [SerializeField] private Ease fadeInEase = Ease.OutCubic;

    // アニメーションタイプの列挙
    public enum AnimationType
    {
        Scale,      // サイズ0から大きくなる
        FadeIn      // 下からフェードイン
    }

    private void Start()
    {
        // 最初にTime.timeScaleを1に戻す
        Time.timeScale = 1f;
        
        // 初期状態では全てのオブジェクトを非表示にする
        InitializeObjects();
        
        // 表示シーケンスを開始
        StartCoroutine(DisplaySequence());
    }

    /// <summary>
    /// 初期化処理：全てのオブジェクトを非表示にする
    /// </summary>
    private void InitializeObjects()
    {
        foreach (var item in displayItems)
        {
            if (item != null && item.displayObject != null)
            {
                var obj = item.displayObject;
                obj.SetActive(false);
                
                // アニメーションタイプに応じて初期状態を設定
                if (item.animationType == AnimationType.Scale)
                {
                    obj.transform.localScale = Vector3.zero;
                }
                else if (item.animationType == AnimationType.FadeIn)
                {
                    var canvasGroup = obj.GetComponent<CanvasGroup>();
                    if (canvasGroup == null) canvasGroup = obj.AddComponent<CanvasGroup>();
                    canvasGroup.alpha = 0f;
                    
                    var rectTransform = obj.GetComponent<RectTransform>();
                    if (rectTransform != null)
                    {
                        // 初期位置を保存（後でアニメーション時に使用）
                        Vector2 originalPos = rectTransform.anchoredPosition;
                        // 開始位置を設定（下から）
                        rectTransform.anchoredPosition = new Vector2(originalPos.x, originalPos.y - fadeInMoveDistance);
                        
                        Debug.Log($"Initialize FadeIn: {obj.name} - Original: {originalPos}, Start: {rectTransform.anchoredPosition}");
                    }
                }
            }
        }
    }

    /// <summary>
    /// 表示シーケンスのコルーチン
    /// </summary>
    private IEnumerator DisplaySequence()
    {
        for (int i = 0; i < displayItems.Count; i++)
        {
            var item = displayItems[i];
            if (item != null && item.displayObject != null)
            {
                yield return new WaitForSeconds(delayBetweenObjects);
                DisplayObject(item);
            }
        }
    }

    /// <summary>
    /// 個別オブジェクトの表示処理
    /// </summary>
    private void DisplayObject(DisplayItem item)
    {
        if (item == null || item.displayObject == null) return;

        var obj = item.displayObject;
        obj.SetActive(true);

        if (item.animationType == AnimationType.Scale)
        {
            // サイズ0から大きくなるアニメーション
            obj.transform.localScale = Vector3.zero;
            obj.transform.DOScale(Vector3.one, scaleAnimDuration)
                .SetEase(scaleEase);
        }
        else if (item.animationType == AnimationType.FadeIn)
        {
            // 下からフェードインするアニメーション
            var canvasGroup = obj.GetComponent<CanvasGroup>();
            if (canvasGroup == null) canvasGroup = obj.AddComponent<CanvasGroup>();
            
            var rectTransform = obj.GetComponent<RectTransform>();
            if (rectTransform != null)
            {
                // 現在の位置を保存
                Vector2 currentPos = rectTransform.anchoredPosition;
                
                // 開始位置を設定（下から）
                Vector2 startPos = new Vector2(currentPos.x, currentPos.y - fadeInMoveDistance);
                rectTransform.anchoredPosition = startPos;
                
                // アルファを0に設定
                canvasGroup.alpha = 0f;

                // 移動とフェードインを同時実行
                Sequence seq = DOTween.Sequence();
                seq.Append(rectTransform.DOAnchorPos(currentPos, fadeInAnimDuration).SetEase(fadeInEase));
                seq.Join(canvasGroup.DOFade(1f, fadeInAnimDuration).SetEase(fadeInEase));
                
                Debug.Log($"FadeIn Animation: {obj.name} - Start: {startPos}, End: {currentPos}, Distance: {fadeInMoveDistance}");
            }
        }
    }

    /// <summary>
    /// 外部から表示シーケンスを再開する
    /// </summary>
    public void RestartDisplaySequence()
    {
        StopAllCoroutines();
        InitializeObjects();
        StartCoroutine(DisplaySequence());
    }

    /// <summary>
    /// 特定のオブジェクトを即座に表示する
    /// </summary>
    public void ShowObjectImmediately(int index)
    {
        if (index >= 0 && index < displayItems.Count)
        {
            var item = displayItems[index];
            if (item != null && item.displayObject != null)
            {
                var obj = item.displayObject;
                obj.SetActive(true);
                obj.transform.localScale = Vector3.one;
                
                var canvasGroup = obj.GetComponent<CanvasGroup>();
                if (canvasGroup != null) canvasGroup.alpha = 1f;
            }
        }
    }
}