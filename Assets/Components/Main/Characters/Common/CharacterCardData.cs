using UnityEngine;
using System.Collections.Generic;
using DG.Tweening;

/// <summary>
/// キャラクターカードのデータ（プレハブ、レベル、個数）を保持します。
/// さらに、データの変更に応じて表示スプライトの更新、アニメーションの再生、
/// 在庫切れ（個数が0）の状態管理も行う、カードの心臓部となるクラスです。
/// </summary>
public class CharacterCardData : MonoBehaviour
{
    [Header("カードの基本データ")]
    [Tooltip("このカードをドラッグした時に生成されるキャラクターのプレハブ")]
    public GameObject characterPrefab;

    [Tooltip("キャラクターのレベル（1～3）")]
    [Range(1, 3)]
    [SerializeField] private int level = 1;

    [Tooltip("カードの所持数（0～8）")]
    [Range(0, 8)]
    [SerializeField] private int count = 1;


    [Header("表示設定")]
    [Tooltip("レベル画像を表示するSpriteRenderer")]
    [SerializeField] private SpriteRenderer levelDisplayRenderer;

    [Tooltip("個数画像を表示するSpriteRenderer")]
    [SerializeField] private SpriteRenderer countDisplayRenderer;


    [Header("スプライトアセット")]
    [Tooltip("レベル画像リスト (Element 0にLv1, 1にLv2, 2にLv3のSpriteを設定)")]
    [SerializeField] private List<Sprite> levelSprites;

    [Tooltip("個数画像リスト (Element 0にx0, 1にx1, ... のSpriteを設定)")]
    [SerializeField] private List<Sprite> countSprites;

    [Header("アニメーション設定")]
    [Tooltip("値が変動した時の拡大アニメーションの強さ")]
    [SerializeField] private float punchAmount = 0.3f;

    [Tooltip("パンチアニメーションの時間")]
    [SerializeField] private float punchDuration = 0.3f;
    
    // --- 内部で使うコンポーネント（パフォーマンス向上のためキャッシュ） ---
    // カード自身と子オブジェクト全てのSpriteRendererを保持。グレースケール化に使用。
    private SpriteRenderer[] allSpriteRenderers;
    // 同じオブジェクトにアタッチされたCardAnimationManager。ホバーアニメーションの有効/無効化に使用。
    private CardAnimationManager cardAnimationManager;


    /// <summary>
    /// キャラクターのレベル。外部からこのプロパティ経由で値を設定すると、表示も自動で更新されます。
    /// </summary>
    public int Level
    {
        get { return level; }
        set
        {
            level = Mathf.Clamp(value, 1, 3); // 値を1～3の範囲に収める
            UpdateLevelDisplay();                 // スプライト表示を更新
        }
    }

    /// <summary>
    /// カードの所持数。外部からこのプロパティ経由で値を設定すると、表示更新や状態変化の処理が自動で走ります。
    /// </summary>
    public int Count
    {
        get { return count; }
        set
        {
            int oldCount = count; // 変更前の値を、状態変化の判定のために一時的に保持
            count = Mathf.Clamp(value, 0, 8); // 値を0～8の範囲に収める
            UpdateCountDisplay();                 // スプライト表示を更新

            // 値が0になったか、0から変わったかをチェックして、状態を切り替える
            if (count == 0 && oldCount > 0)
            {
                // 0になった瞬間の処理
                SetExhaustedState(true);
            }
            else if (count > 0 && oldCount == 0)
            {
                // 0から増えた瞬間の処理
                SetExhaustedState(false);
            }
        }
    }
    
    /// <summary>
    /// Startよりも先に一度だけ呼ばれる初期化処理。コンポーネントの参照を取得するのに適しています。
    /// </summary>
    void Awake()
    {
        // 毎回GetComponentを呼ぶのはコストがかかるため、一度だけ取得して変数に保存（キャッシュ）しておく
        allSpriteRenderers = GetComponentsInChildren<SpriteRenderer>();
        cardAnimationManager = GetComponent<CardAnimationManager>();
    }

    /// <summary>
    /// ゲーム開始時に一度だけ呼ばれる処理
    /// </summary>
    void Start()
    {
        // インスペクターで設定された初期値に基づいて、表示を正しく更新
        UpdateLevelDisplay();
        UpdateCountDisplay();
        // 起動時に個数が0だった場合に、最初からグレースケール表示などにする
        CheckInitialState();
    }
    
    /// <summary>
    /// 【外部呼び出し用】レベルを1上げ、アニメーションさせます。
    /// </summary>
    public void IncreaseLevel()
    {
        if (Level >= 3) return; // 上限に達していたら何もしない
        Level++;
        AnimatePunch(levelDisplayRenderer.transform);
    }

    /// <summary>
    /// 【外部呼び出し用】個数を1増やし、アニメーションさせます。
    /// </summary>
    public void IncreaseCount()
    {
        if (Count >= 8) return; // 上限に達していたら何もしない
        Count++; // プロパティ経由で値を変更。これによりset内の処理（表示更新、状態変化）が自動で実行される。
        AnimatePunch(countDisplayRenderer.transform);
    }

    /// <summary>
    /// 【外部呼び出し用】個数を1減らし、アニメーションさせます。
    /// </summary>
    public void DecreaseCount()
    {
        if (Count <= 0) return; // 下限に達していたら何もしない
        Count--; // プロパティ経由で値を変更。
        AnimatePunch(countDisplayRenderer.transform);
    }

    /// <summary>
    /// 指定されたTransformにパンチ（拡大して戻る）アニメーションを適用する共通メソッド
    /// </summary>
    private void AnimatePunch(Transform targetTransform)
    {
        if (targetTransform == null) return;
        // 実行中の古いアニメーションがあれば、即座に完了させて止める
        targetTransform.DOKill(true);
        // DOTweenのDOPunchScaleを使ってアニメーションを再生
        targetTransform.DOPunchScale(new Vector3(punchAmount, punchAmount, 0), punchDuration, 1, 1);
    }

    /// <summary>
    /// カードが在庫切れ（個数0）になった、または回復した時の状態を設定します。
    /// </summary>
    /// <param name="isExhausted">在庫切れ状態にするかどうか</param>
    private void SetExhaustedState(bool isExhausted)
    {
        // isExhaustedがtrueなら灰色(Color.gray)、falseなら白色(Color.white)をセット
        Color color = isExhausted ? Color.gray : Color.white;
        if (allSpriteRenderers != null)
        {
            // キャッシュしておいた全てのスプライトの色を一括で変更
            foreach (var renderer in allSpriteRenderers)
            {
                renderer.color = color;
            }
        }

        // CardAnimationManagerの有効/無効を切り替える
        if (cardAnimationManager != null)
        {
            // isExhaustedがtrueなら無効(false)に、falseなら有効(true)にする
            cardAnimationManager.enabled = !isExhausted;
        }
    }

    /// <summary>
    /// ゲーム開始時の個数に応じて、カードの初期状態（通常 or 在庫切れ）を正しく設定します。
    /// </summary>
    private void CheckInitialState()
    {
        SetExhaustedState(count == 0);
    }

    /// <summary>
    /// Unityエディタ上でインスペクターの値が変更されたときにのみ呼ばれる特殊なメソッド
    /// </summary>
    private void OnValidate()
    {
        // ゲームを実行していなくても、インスペクターでの変更がシーンビューに即座に反映されるようにする
        if (levelDisplayRenderer != null && countDisplayRenderer != null)
        {
            UpdateLevelDisplay();
            UpdateCountDisplay();
        }
    }

    /// <summary>
    /// レベルの表示を現在のlevel値に基づいて更新します。
    /// </summary>
    private void UpdateLevelDisplay()
    {
        // 必要なコンポーネントやリストが設定されていない場合は、エラーを防ぐために処理を中断
        if (levelDisplayRenderer == null || levelSprites == null || levelSprites.Count == 0) return;

        // レベルは1, 2, 3...と1から始まるが、リストのインデックスは0, 1, 2...と0から始まるため、-1して調整
        int spriteIndex = level - 1;

        // インデックスがリストの範囲内かチェック
        if (spriteIndex >= 0 && spriteIndex < levelSprites.Count)
        {
            // 対応するスプライトをSpriteRendererに設定
            levelDisplayRenderer.sprite = levelSprites[spriteIndex];
        }
    }

    /// <summary>
    /// 個数の表示を現在のcount値に基づいて更新します。
    /// </summary>
    private void UpdateCountDisplay()
    {
        if (countDisplayRenderer == null || countSprites == null || countSprites.Count == 0) return;

        // 個数は0, 1, 2...と0から始まるので、そのままインデックスとして使える
        int spriteIndex = count;

        if (spriteIndex >= 0 && spriteIndex < countSprites.Count)
        {
            countDisplayRenderer.sprite = countSprites[spriteIndex];
        }
    }
}