using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;
using DG.Tweening;
using System.Collections.Generic;

public class DragAndDropCharacterManager : MonoBehaviour
{
    [Header("参照")]
    [Tooltip("シーンのメインカメラをここに割り当ててください")]
    [SerializeField] private Camera mainCamera;

    [Header("設定")]
    [SerializeField] private string draggableCardTag = "CharacterCard";
    [SerializeField] private string placedCharacterTag = "PlacedCharacter";
    [SerializeField] private string characterHolderTag = "CharacterHolder";
    [SerializeField] private float placementRadius = 1.0f;
    [SerializeField] private float returnToCardRadius = 1.5f;

    [Header("アニメーション設定")]
    [SerializeField] private float returnDuration = 0.3f;

    [Header("ドラッグ中の描画設定")]
    [SerializeField] private int dragSortingOrder = 1000;

    [Header("音声設定")]
    [Tooltip("ドラッグ開始時とドロップ時に再生するAudioSource")]
    [SerializeField] private AudioSource dragAndDropAudio;
    
    private GameObject draggedObject;
    private GameObject characterToInstantiate;
    private Vector3 dragStartPosition;
    private CharacterCardData sourceCardData;
    private CharacterHolderManager sourceHolderManager;
    private RopeManager ropeManager;
    private BattleBeginsManager battleBeginsManager;
    private Dictionary<SpriteRenderer, int> originalSortingOrders;
    private enum DragMode { FromCard, FromField }
    private DragMode currentDragMode;
    public static bool IsDragging { get; private set; }

    void Awake()
    {
        originalSortingOrders = new Dictionary<SpriteRenderer, int>();
    }

    void Start()
    {
        ropeManager = FindAnyObjectByType<RopeManager>();
        if (ropeManager == null) Debug.LogError("シーンにRopeManagerが見つかりません！", this);

        battleBeginsManager = FindAnyObjectByType<BattleBeginsManager>();
        if (battleBeginsManager == null) Debug.LogError("シーンにBattleBeginsManagerが見つかりません！", this);
    }

    void Update()
    {
        if (Mouse.current == null) return;
        if (Mouse.current.leftButton.wasPressedThisFrame && draggedObject == null)
        {
            TryStartDrag();
        }
        if (draggedObject != null)
        {
            draggedObject.transform.position = GetMouseWorldPosition();
        }
        if (Mouse.current.leftButton.wasReleasedThisFrame)
        {
            if (draggedObject != null)
            {
                EndDrag();
            }
        }
    }

    private void TryStartDrag()
    {
        PointerEventData pointerData = new PointerEventData(EventSystem.current)
        {
            position = Mouse.current.position.ReadValue()
        };

        List<RaycastResult> results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(pointerData, results);

        if (results.Count == 0) return;

        foreach (var result in results)
        {
            GameObject hitObject = result.gameObject;
            
            if (hitObject.CompareTag(draggableCardTag))
            {
                var cardData = hitObject.GetComponent<CharacterCardData>();
                if (cardData != null && cardData.characterPrefab != null && cardData.Count > 0)
                {
                    // ドラッグ開始音を再生
                    PlayDragAndDropSound();
                    
                    currentDragMode = DragMode.FromCard;
                    sourceCardData = cardData;
                    characterToInstantiate = cardData.characterPrefab;
                    sourceCardData.DecreaseCount();
                    var animManager = sourceCardData.GetComponent<CardAnimationManager>();
                    if (animManager != null) animManager.OnDragStart();
                    dragStartPosition = hitObject.transform.position;
                    StartDrag_CreateGhost(characterToInstantiate, dragStartPosition);
                    return;
                }
            }
            else if (hitObject.CompareTag(placedCharacterTag))
            {
                var placedChar = hitObject.GetComponent<PlacedCharacter>();
                if (placedChar == null || placedChar.IsPaused) return;
                
                // ドラッグ開始音を再生
                PlayDragAndDropSound();
                
                placedChar.OnDragStart();
                
                currentDragMode = DragMode.FromField;
                draggedObject = placedChar.gameObject;
                sourceCardData = placedChar.SourceCardData;
                sourceHolderManager = placedChar.CurrentHolder;

                if (sourceCardData != null)
                {
                    characterToInstantiate = sourceCardData.characterPrefab;
                }
                
                if(sourceHolderManager != null)
                    dragStartPosition = sourceHolderManager.transform.position;

                if (sourceHolderManager != null) sourceHolderManager.SetOccupied(false);
                
                draggedObject.transform.SetParent(null, true);

                IsDragging = true;
                StoreAndSetSortingOrder(draggedObject, dragSortingOrder);
                return;
            }
        }
    }
    
    private void EndDrag()
    {
        IsDragging = false;
        
        if (draggedObject != null && currentDragMode == DragMode.FromField)
        {
            var placedChar = draggedObject.GetComponent<PlacedCharacter>();
            if (placedChar != null)
            {
                placedChar.OnDragEnd();
            }
        }
        
        RestoreSortingOrder();
        
        if (currentDragMode == DragMode.FromCard && sourceCardData != null)
        {
            var animManager = sourceCardData.GetComponent<CardAnimationManager>();
            if (animManager != null) animManager.OnDragEnd();
        }

        Vector2 dropPosition = draggedObject.transform.position;

        if (battleBeginsManager != null && !battleBeginsManager.IsInBattle &&
            currentDragMode == DragMode.FromField && sourceCardData != null && 
            Vector2.Distance(dropPosition, sourceCardData.transform.position) < returnToCardRadius)
        {
            // カードに戻る場合のドロップ音を再生
            PlayDragAndDropSound();
            
            sourceCardData.IncreaseCount();
            Destroy(draggedObject);
            ResetDragState();
            return;
        }

        Collider2D[] collidersInRadius = Physics2D.OverlapCircleAll(dropPosition, placementRadius);
        Transform closestHolder = null;
        float minDistance = float.MaxValue;
        foreach (var col in collidersInRadius)
        {
            if (col.CompareTag(characterHolderTag))
            {
                var holderManager = col.GetComponent<CharacterHolderManager>();
                if (holderManager != null && !holderManager.IsOccupied)
                {
                    float distance = Vector2.Distance(dropPosition, col.transform.position);
                    if (distance < minDistance)
                    {
                        minDistance = distance;
                        closestHolder = col.transform;
                    }
                }
            }
        }
        
        if (closestHolder != null)
        {
            // 配置成功時のドロップ音を再生
            PlayDragAndDropSound();
            
            var targetHolderManager = closestHolder.GetComponent<CharacterHolderManager>();
            targetHolderManager.SetOccupied(true);
            Transform alliedParent = closestHolder.parent;
            
            if (currentDragMode == DragMode.FromField && draggedObject != null)
            {
                var oldCharData = draggedObject.GetComponent<PlacedCharacter>();
                if (oldCharData != null)
                {
                    PlacedCharacter.HpToTransfer = oldCharData.hp;
                }
            }
            
            GameObject newChar = Instantiate(characterToInstantiate, closestHolder.position, Quaternion.identity, alliedParent);
            var placedCharData = newChar.GetComponent<PlacedCharacter>();
            if (placedCharData != null)
            {
                placedCharData.SourceCardData = sourceCardData;
                placedCharData.CurrentHolder = targetHolderManager;
            }
            
            Destroy(draggedObject);
        }
        else 
        {
            if (currentDragMode == DragMode.FromCard)
            {
                CharacterCardData cardToRestore = sourceCardData;
                GameObject ghostToDestroy = draggedObject;
                draggedObject.transform.DOMove(dragStartPosition, returnDuration)
                    .SetEase(Ease.OutQuad)
                    .OnComplete(() =>
                    {
                        if (cardToRestore != null) cardToRestore.IncreaseCount();
                        if (ghostToDestroy != null) Destroy(ghostToDestroy);
                    });
            }
            else // DragMode.FromField
            {
                CharacterHolderManager holderToRestore = sourceHolderManager;
                GameObject characterToRestore = draggedObject;
                
                Vector3 returnPosition = holderToRestore != null ? holderToRestore.transform.position : dragStartPosition;

                characterToRestore.transform.DOMove(returnPosition, returnDuration)
                    .SetEase(Ease.OutQuad)
                    .OnComplete(() =>
                    {
                        if (holderToRestore != null)
                        {
                            holderToRestore.SetOccupied(true);
                            if (characterToRestore != null)
                            {
                                characterToRestore.transform.SetParent(holderToRestore.transform.parent, true);
                            }
                        }

                        var placedCharData = characterToRestore.GetComponent<PlacedCharacter>();
                        if(placedCharData != null) placedCharData.CurrentHolder = holderToRestore;
                        
                        if (ropeManager != null && holderToRestore != null && characterToRestore != null)
                        {
                            ropeManager.OccupiedHolderInfo[holderToRestore.gameObject] = characterToRestore;
                        }
                    });
            }
        }

        ResetDragState();
    }
    
    private void StartDrag_CreateGhost(GameObject characterPrefab, Vector3 startPosition)
    {
        IsDragging = true;
        characterToInstantiate = characterPrefab;
        draggedObject = Instantiate(characterToInstantiate, startPosition, Quaternion.identity);

        var spriteRenderer = draggedObject.GetComponentInChildren<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            Color color = spriteRenderer.color;
            color.a = 0.6f;
            spriteRenderer.color = color;
        }

        var collider = draggedObject.GetComponent<Collider2D>();
        if (collider != null) collider.enabled = false;
        
        StoreAndSetSortingOrder(draggedObject, dragSortingOrder);
    }
    
    private void StoreAndSetSortingOrder(GameObject target, int order)
    {
        if (target == null) return;
        
        originalSortingOrders.Clear();
        SpriteRenderer[] renderers = target.GetComponentsInChildren<SpriteRenderer>();
        
        foreach (var renderer in renderers)
        {
            originalSortingOrders[renderer] = renderer.sortingOrder;
            renderer.sortingOrder = order;
        }
    }

    private void RestoreSortingOrder()
    {
        if (originalSortingOrders == null) return;

        foreach (var pair in originalSortingOrders)
        {
            if (pair.Key != null) 
            {
                pair.Key.sortingOrder = pair.Value;
            }
        }
        originalSortingOrders.Clear();
    }
    
    private void ResetDragState()
    {
        characterToInstantiate = null;
        sourceCardData = null;
        draggedObject = null;
        sourceHolderManager = null;
    }

    /// <summary>
    /// ドラッグ開始時とドロップ時に再生する音声
    /// </summary>
    private void PlayDragAndDropSound()
    {
        if (dragAndDropAudio != null)
        {
            dragAndDropAudio.Play();
        }
    }

    private Vector3 GetMouseWorldPosition()
    {
        Vector3 mouseScreenPos = Mouse.current.position.ReadValue();
        mouseScreenPos.z = mainCamera.nearClipPlane + 10f;
        return mainCamera.ScreenToWorldPoint(mouseScreenPos);
    }
}