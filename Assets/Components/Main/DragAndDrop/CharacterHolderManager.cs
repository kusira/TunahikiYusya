using UnityEngine;
using UnityEngine.InputSystem;
using DG.Tweening;

[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(Collider2D))]
public class CharacterHolderManager : MonoBehaviour
{
    [Header("スプライト設定")]
    [SerializeField] private Sprite characterHolderBlue;
    [SerializeField] private Sprite characterHolderGreen;

    [Header("アニメーション設定")]
    [SerializeField] private float blueFadeInDuration = 0.25f;
    [SerializeField] private float battleStartFadeOutDuration = 0.5f;
    [SerializeField, Range(0f, 1f)] private float visibleAlpha = 1.0f;

    private SpriteRenderer spriteRenderer;
    private bool isOccupied = false;
    private BattleBeginsManager battleBeginsManager;

    public bool IsOccupied => isOccupied;

    private enum State { Invisible, Blue, Green }
    private State currentState = State.Invisible;

    void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();

        // シーンから BattleBeginsManager を自動取得
        battleBeginsManager = FindAnyObjectByType<BattleBeginsManager>();
        if (battleBeginsManager == null)
        {
            Debug.LogError("BattleBeginsManager がシーンに見つかりません。");
        }
    }

    void Update()
    {
        State targetState;

        if (isOccupied)
        {
            // 使用中のホルダーは常に非表示
            targetState = State.Invisible;
        }
        else if (DragAndDropCharacterManager.IsDragging)
        {
            // ドラッグ中：マウスが上にあれば緑、なければ青
            bool isHovered = IsMouseHovering();
            targetState = isHovered ? State.Green : State.Blue;
        }
        else if (battleBeginsManager != null && battleBeginsManager.IsInBattle)
        {
            // 戦闘中は非表示
            targetState = State.Invisible;
        }
        else
        {
            // それ以外は青
            targetState = State.Blue;
        }

        if (currentState == targetState) return;

        State oldState = currentState;
        currentState = targetState;

        spriteRenderer.DOKill(); // 既存のアニメーションを停止

        switch (currentState)
        {
            case State.Invisible:
                if (battleBeginsManager != null && battleBeginsManager.IsInBattle && oldState != State.Invisible)
                {
                    spriteRenderer.DOFade(0, battleStartFadeOutDuration);
                }
                else
                {
                    spriteRenderer.color = new Color(1, 1, 1, 0);
                }
                break;

            case State.Blue:
                spriteRenderer.sprite = characterHolderBlue;
                if (oldState == State.Invisible)
                {
                    spriteRenderer.color = new Color(1, 1, 1, 0);
                    spriteRenderer.DOFade(visibleAlpha, blueFadeInDuration);
                }
                else
                {
                    spriteRenderer.color = new Color(1, 1, 1, visibleAlpha);
                }
                break;

            case State.Green:
                spriteRenderer.sprite = characterHolderGreen;
                spriteRenderer.color = new Color(1, 1, 1, visibleAlpha);
                break;
        }
    }

    /// <summary>
    /// 外部からホルダーの使用状態を設定できるようにします。
    /// キャラが死んだときなどに SetOccupied(false) を呼び出せば再利用可能になります。
    /// </summary>
    public void SetOccupied(bool occupied)
    {
        isOccupied = occupied;
    }

    /// <summary>
    /// マウスがこのホルダーの上にあるかどうかを判定します。
    /// </summary>
    private bool IsMouseHovering()
    {
        Vector2 mouseWorldPos = Camera.main.ScreenToWorldPoint(Mouse.current.position.ReadValue());
        RaycastHit2D[] hits = Physics2D.RaycastAll(mouseWorldPos, Vector2.zero);
        foreach (var hit in hits)
        {
            if (hit.collider != null && hit.collider.gameObject == gameObject)
            {
                return true;
            }
        }
        return false;
    }
}
