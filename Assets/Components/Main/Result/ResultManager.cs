using UnityEngine;
using UnityEngine.UI;
using TMPro; // TextMeshProを使用するために必要
using System.Collections;
using DG.Tweening; // DOTweenを使用するために必要

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

    // --- プライベート変数 ---
    private bool _isGameFinished = false;
    private CanvasGroup _resultBackCanvasGroup;

    void Start()
    {
        // --- 初期化処理 ---
        // 参照が設定されているか確認 (RopeCountManagerのチェックを削除し、テキストのチェックに変更)
        if (timeManager == null || alliedRopeCountText == null || enemyRopeCountText == null || resultParent == null || resultBack == null || winText == null || loseText == null)
        {
            Debug.LogError("ResultManagerの参照がInspectorで一部設定されていません！", this);
            enabled = false; // コンポーネントを無効化
            return;
        }

        // フェード用にCanvasGroupを取得（なければ追加）
        _resultBackCanvasGroup = resultBack.GetComponent<CanvasGroup>();
        if (_resultBackCanvasGroup == null) _resultBackCanvasGroup = resultBack.gameObject.AddComponent<CanvasGroup>();

        // 初期状態ではリザルトを非表示にする
        resultParent.SetActive(false);
        winText.gameObject.SetActive(false);
        loseText.gameObject.SetActive(false);
        _resultBackCanvasGroup.alpha = 0;
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
        yield return new WaitForSeconds(resultDelay);

        // --- テキストからスコアを数字に変換 ---
        int alliedCount = 0;
        int enemyCount = 0;

        // int.TryParseで安全に文字列を整数に変換
        if (!int.TryParse(alliedRopeCountText.text, out alliedCount))
        {
            Debug.LogError($"味方のスコアテキスト「{alliedRopeCountText.text}」を数字に変換できませんでした。", alliedRopeCountText);
        }
        if (!int.TryParse(enemyRopeCountText.text, out enemyCount))
        {
            Debug.LogError($"敵のスコアテキスト「{enemyRopeCountText.text}」を数字に変換できませんでした。", enemyRopeCountText);
        }

        // --- 勝利判定 ---
        // AlliedRopeCountがEnemyRopeCountより大きい場合のみ勝利
        bool isWin = alliedCount > enemyCount;
        Debug.Log($"判定: 味方 {alliedCount} vs 敵 {enemyCount}。 結果: {(isWin ? "勝利" : "敗北")}");

        // リザルトの親オブジェクトを有効化
        resultParent.SetActive(true);

        // --- アニメーション開始 ---
        _resultBackCanvasGroup.DOFade(1, backFadeDuration);
        TMP_Text targetText = isWin ? winText : loseText;
        AnimateResultText(targetText);
    }

    /// <summary>
    /// 勝敗テキストをアニメーションさせる
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

        Sequence seq = DOTween.Sequence();
        seq.Append(textRect.DOAnchorPosY(originalPos.y, textAnimDuration).SetEase(Ease.OutCubic));
        seq.Join(textCanvasGroup.DOFade(1, textAnimDuration));
    }
}