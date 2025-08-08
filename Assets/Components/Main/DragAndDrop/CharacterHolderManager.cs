using UnityEngine;
using UnityEngine.InputSystem;
using DG.Tweening;

[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(Collider2D))]
public class CharacterHolderManager : MonoBehaviour
{
    [Header("参照")]
    [Tooltip("シーン内のBattleBeginsManagerをアサインしてください")]
    [SerializeField] private BattleBeginsManager battleBeginsManager;

    [Header("スプライト設定")]
    [SerializeField] private Sprite characterHolderBlue;
    [SerializeField] private Sprite characterHolderGreen;
    
    [Header("アニメーション設定")]
    [SerializeField] private float blueFadeInDuration = 0.25f;
    [SerializeField] private float battleStartFadeOutDuration = 0.5f;
    [SerializeField] [Range(0f, 1f)] private float visibleAlpha = 1.0f;
    
    private SpriteRenderer spriteRenderer;
    private bool isOccupied = false;
    
    private enum State { Invisible, Blue, Green }
    private State currentState = State.Invisible;

    public bool IsOccupied => isOccupied;

    void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    void Update()
    {
        State targetState;
        
        // 【変更点】判定の優先順位を見直し
        if (isOccupied)
        {
            // 1. 最優先：使用中のホルダーは常に非表示
            targetState = State.Invisible;
        }
        else if (DragAndDropCharacterManager.IsDragging)
        {
            // 2. 次点：ドラッグ中はバトル状態を問わず、表示状態を決定
            bool isHovered = IsMouseHovering();
            targetState = isHovered ? State.Green : State.Blue;
        }
        else if (battleBeginsManager != null && battleBeginsManager.IsInBattle)
        {
            // 3. 次点：ドラッグ中でなく、バトル中なら非表示
            targetState = State.Invisible;
        }
        else
        {
            // 4. 上記以外（バトル前でドラッグ中でもない）場合は常に青色で表示
            targetState = State.Blue;
        }
        
        if (currentState == targetState)
        {
            return;
        }
        
        State oldState = currentState;
        currentState = targetState;
        
        spriteRenderer.DOKill();

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

    public void SetOccupied(bool occupied)
    {
        isOccupied = occupied;
    }

    private bool IsMouseHovering()
    {
        RaycastHit2D[] hits = Physics2D.RaycastAll(Camera.main.ScreenToWorldPoint(Mouse.current.position.ReadValue()), Vector2.zero);
        foreach (var hit in hits)
        {
            if (hit.collider != null && hit.collider.gameObject == this.gameObject)
            {
                return true;
            }
        }
        return false;
    }
}