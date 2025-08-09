using UnityEngine;
using UnityEngine.UI;
using TMPro; // TextMeshProを使用するために必要
using System.Collections;
using DG.Tweening; // DOTweenを使用するために必要
using System.Collections.Generic;

/// <summary>
/// ゲームの終了を判定し、リザルトUIを表示する
/// </summary>
public class ResultManager : MonoBehaviour
{
    [Header("コンポーネント参照")]
    [Tooltip("TimeManagerがアタッチされているGameObject")]
    [SerializeField] private TimeManager timeManager;

    // RopeCountManagerの参照を削除し、テキストの参照に変更
    [Header("スコアテキスト参照")]
    [Tooltip("味方の綱の数を表示するTextMeshProテキスト")]
    [SerializeField] private TMP_Text alliedRopeCountText;

    [Tooltip("敵の綱の数を表示するTextMeshProテキスト")]
    [SerializeField] private TMP_Text enemyRopeCountText;

    [Header("UIオブジェクト参照")]
    [Tooltip("リザルト全体の親GameObject")]
    [SerializeField] private GameObject resultParent;

    [Tooltip("リザルトの背景パネル")]
    [SerializeField] private Image resultBack;

    [Tooltip("勝利時に表示するテキスト")]
    [SerializeField] private TMP_Text winText;

    [Tooltip("敗北時に表示するテキスト")]
    [SerializeField] private TMP_Text loseText;

    [Header("アニメーション設定")]
    [Tooltip("終了条件を満たしてからリザルトが表示されるまでの待機時間（秒）")]
    [SerializeField] private float resultDelay = 2.0f;

    [Tooltip("背景パネルのフェードイン時間")]
    [SerializeField] private float backFadeDuration = 0.5f;

    [Tooltip("勝敗テキストのアニメーション時間")]
    [SerializeField] private float textAnimDuration = 0.8f;

    [Tooltip("勝敗テキストが下から移動してくる距離")]
    [SerializeField] private float textMoveDistance = 50f;

    [Header("追加設定")]
    [Tooltip("リザルト表示後の分岐処理までの待機時間（非スケール秒）")]
    [SerializeField] private float postShowWait = 0.3f;

    [Tooltip("敗北時に表示するボタン群の親 GameObject")]
    [SerializeField] private GameObject buttonManager;

    [Tooltip("勝利時に表示する BenefitsManager の親 GameObject（必要なら Show() を呼び出します）")]
    [SerializeField] private GameObject benefitsManager;

    [Tooltip("ボタン群を下から出すときの移動距離")]
    [SerializeField] private float buttonMoveDistance = 30f;

    [Tooltip("ボタン群のフェードイン時間（非スケール秒）")]
    [SerializeField] private float buttonFadeDuration = 0.4f;

    // --- プライベート変数 ---
    private bool _isGameFinished = false;
    private CanvasGroup _resultBackCanvasGroup;

    void Start()
    {
        // --- 初期化処理 ---
        if (timeManager == null || alliedRopeCountText == null || enemyRopeCountText == null || resultParent == null || resultBack == null || winText == null || loseText == null)
        {
            Debug.LogError("ResultManagerの参照がInspectorで一部設定されていません！", this);
            enabled = false;
            return;
        }

        _resultBackCanvasGroup = resultBack.GetComponent<CanvasGroup>();
        if (_resultBackCanvasGroup == null) _resultBackCanvasGroup = resultBack.gameObject.AddComponent<CanvasGroup>();

        // 初期状態ではリザルトを非表示にする
        resultParent.SetActive(false);
        winText.gameObject.SetActive(false);
        loseText.gameObject.SetActive(false);
        _resultBackCanvasGroup.alpha = 0;

        // 追加UI初期化
        if (buttonManager != null) buttonManager.SetActive(false);
        if (benefitsManager != null) benefitsManager.SetActive(false); // ここを追加
        // benefitsManager は勝利時に有効化するため、ここでは触らない
    }

    void Update()
    {
        if (_isGameFinished) return;

        // --- 終了条件の判定 ---
        bool timeUp = timeManager.CurrentTime <= 0;
        bool noMoreRopes = FindAnyObjectByType<RopeManager>() == null;

        if (timeUp || noMoreRopes)
        {
            _isGameFinished = true;
            Debug.Log("ゲーム終了！ リザルトを表示します。");
            StartCoroutine(ShowResultSequence());
        }
    }

    /// <summary>
    /// リザルト表示の一連の流れを管理するコルーチン
    /// </summary>
    private IEnumerator ShowResultSequence()
    {
        // 結果表示までの待機（スケール時間）
        yield return new WaitForSeconds(resultDelay);

        // --- テキストからスコアを数字に変換 ---
        int alliedCount = 0;
        int enemyCount = 0;

        if (!int.TryParse(alliedRopeCountText.text, out alliedCount))
        {
            Debug.LogError($"味方のスコアテキスト「{alliedRopeCountText.text}」を数字に変換できませんでした。", alliedRopeCountText);
        }
        if (!int.TryParse(enemyRopeCountText.text, out enemyCount))
        {
            Debug.LogError($"敵のスコアテキスト「{enemyRopeCountText.text}」を数字に変換できませんでした。", enemyRopeCountText);
        }

        // --- 勝利判定 ---
        bool isWin = alliedCount > enemyCount;
        Debug.Log($"判定: 味方 {alliedCount} vs 敵 {enemyCount}。 結果: {(isWin ? "勝利" : "敗北")}");

        // ドラッグ&ドロップを無効化（直接参照）
        var ddm = FindAnyObjectByType<DragAndDropCharacterManager>();
        if (ddm != null) ddm.enabled = false;

        // リザルトの親オブジェクトを有効化
        resultParent.SetActive(true);

        // まず TimeScale を 0 にする（以降は非スケールで進行）
        Time.timeScale = 0f;

        // --- アニメーション開始（非スケール時間）---
        _resultBackCanvasGroup.DOFade(1, backFadeDuration).SetUpdate(true);
        TMP_Text targetText = isWin ? winText : loseText;
        AnimateResultText(targetText); // 内部で SetUpdate(true)

        // 分岐前の待機（非スケール時間）
        yield return new WaitForSecondsRealtime(postShowWait);

        if (!isWin)
        {
            // 敗北時: 敗北テキストは残したまま、ボタン群を下からフェードイン
            if (buttonManager != null)
            {
                FadeInFromBelow(buttonManager, buttonMoveDistance, buttonFadeDuration);
            }
            else
            {
                Debug.LogWarning("ResultManager: buttonManager が未アサインのため、敗北時のボタン表示をスキップします。", this);
            }
        }
        else
        {
            // 勝利時: 勝利テキストを消し、消し終わってから BenefitsManager を有効化
            if (winText != null)
            {
                var cg = winText.GetComponent<CanvasGroup>();
                if (cg == null) cg = winText.gameObject.AddComponent<CanvasGroup>();
                cg.DOFade(0f, 0.2f).SetUpdate(true).OnComplete(() =>
                {
                    winText.gameObject.SetActive(false);

                    if (benefitsManager != null)
                    {
                        benefitsManager.SetActive(true);
                        var bm = benefitsManager.GetComponent<BenefitsManager>();
                    }
                    else
                    {
                        Debug.LogWarning("ResultManager: benefitsManager が未アサインのため、勝利時の恩恵画面表示をスキップします。", this);
                    }
                });
            }
        }
    }

    /// <summary>
    /// 勝敗テキストをアニメーションさせる（非スケール時間）
    /// </summary>
    private void AnimateResultText(TMP_Text textToShow)
    {
        CanvasGroup textCanvasGroup = textToShow.GetComponent<CanvasGroup>();
        if (textCanvasGroup == null) textCanvasGroup = textToShow.gameObject.AddComponent<CanvasGroup>();

        RectTransform textRect = textToShow.GetComponent<RectTransform>();
        Vector2 originalPos = textRect.anchoredPosition;
        textRect.anchoredPosition = new Vector2(originalPos.x, originalPos.y - textMoveDistance);
        textCanvasGroup.alpha = 0;
        textToShow.gameObject.SetActive(true);

        Sequence seq = DOTween.Sequence().SetUpdate(true);
        seq.Append(textRect.DOAnchorPosY(originalPos.y, textAnimDuration).SetEase(Ease.OutCubic).SetUpdate(true));
        seq.Join(textCanvasGroup.DOFade(1, textAnimDuration).SetUpdate(true));
    }

    /// <summary>
    /// 指定の GameObject を少し下からフェードインで表示（非スケール時間）
    /// </summary>
    private void FadeInFromBelow(GameObject go, float moveDistance, float duration)
    {
        if (go == null) return;

        var cg = go.GetComponent<CanvasGroup>();
        if (cg == null) cg = go.AddComponent<CanvasGroup>();

        var rt = go.GetComponent<RectTransform>();
        Vector2 originalPos = rt != null ? rt.anchoredPosition : Vector2.zero;

        go.SetActive(true);
        cg.alpha = 0f;
        if (rt != null)
        {
            rt.anchoredPosition = new Vector2(originalPos.x, originalPos.y - Mathf.Abs(moveDistance));
            rt.DOAnchorPosY(originalPos.y, duration).SetEase(Ease.OutCubic).SetUpdate(true);
        }

        cg.DOFade(1f, duration).SetUpdate(true);
    }
}