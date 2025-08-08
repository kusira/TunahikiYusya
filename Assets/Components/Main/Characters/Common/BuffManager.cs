using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// キャラクターのワールド空間にバフアイコンを表示・管理する。
/// </summary>
public class BuffManager : MonoBehaviour
{
    [Header("バフ設定")]
    [Tooltip("アイコンとして生成するプレハブ")]
    [SerializeField] private GameObject buffIconPrefab;
    [Tooltip("アイコンを整列させる起点となるTransform")]
    [SerializeField] private Transform iconAnchor;
    [Tooltip("アイコン同士の間隔")]
    [SerializeField] private float iconSpacing = 0.5f;
    
    [Header("表示設定")]
    [Tooltip("アイコンがフェードイン／アウトする速度")]
    [SerializeField] private float alphaFadeSpeed = 10f;

    private class Buff
    {
        public string Id { get; }
        public GameObject IconInstance { get; }
        public SpriteRenderer Renderer { get; }

        public Buff(string id, GameObject iconInstance)
        {
            this.Id = id;
            this.IconInstance = iconInstance;
            this.Renderer = iconInstance.GetComponent<SpriteRenderer>();
        }
    }

    private readonly List<Buff> activeBuffs = new List<Buff>();
    private PlacedCharacter placedCharacter;
    private Transform currentComparisonAxis;
    private float initialAnchorLocalX;

    void Awake()
    {
        placedCharacter = GetComponent<PlacedCharacter>();
        if (placedCharacter == null)
        {
            Debug.LogError("同じGameObjectにPlacedCharacterが見つかりません。", this);
        }
        if (iconAnchor != null)
        {
            initialAnchorLocalX = iconAnchor.localPosition.x;
        }
    }

    void Update()
    {
        // ... (Updateメソッドは変更なし) ...
        Transform potentialNewAxis = transform.parent?.parent;
        if (potentialNewAxis != null)
        {
            currentComparisonAxis = potentialNewAxis;
            UpdateAnchorOrientation();
        }

        if (placedCharacter == null) return;
        
        float targetAlpha = placedCharacter.IsPaused ? 0f : 1f;

        foreach (var buff in activeBuffs)
        {
            if (buff.Renderer == null) continue;

            Color currentColor = buff.Renderer.color;
            if (!Mathf.Approximately(currentColor.a, targetAlpha))
            {
                float newAlpha = Mathf.MoveTowards(currentColor.a, targetAlpha, alphaFadeSpeed * Time.deltaTime);
                buff.Renderer.color = new Color(currentColor.r, currentColor.g, currentColor.b, newAlpha);
            }
        }
    }

    public void AddBuff(string buffId)
    {
        if (buffIconPrefab == null || iconAnchor == null) return;
        if (activeBuffs.Any(b => b.Id == buffId)) return;
        
        Sprite iconSprite = BuffIconDatabase.Instance.GetBuffIcon(buffId);
        if (iconSprite == null) return;

        GameObject newIcon = Instantiate(buffIconPrefab, iconAnchor);
        var newBuff = new Buff(buffId, newIcon);

        if (newBuff.Renderer == null)
        {
            Debug.LogError($"プレハブ '{buffIconPrefab.name}' にSpriteRendererがありません。", buffIconPrefab);
            Destroy(newIcon);
            return;
        }
        
        newBuff.Renderer.sprite = iconSprite;
        newBuff.Renderer.color = Color.white;

        activeBuffs.Add(newBuff);
        RepositionIcons();
    }

    // (RemoveBuffや他のメソッドは変更なし)
    #region Unchanged Methods
    public void RemoveBuff(string buffId)
    {
        Buff buffToRemove = activeBuffs.FirstOrDefault(b => b.Id == buffId);
        if (buffToRemove != null)
        {
            Destroy(buffToRemove.IconInstance);
            activeBuffs.Remove(buffToRemove);
            RepositionIcons();
        }
    }
    
    private void UpdateAnchorOrientation()
    {
        if (iconAnchor == null || currentComparisonAxis == null) return;

        float axisXPosition = currentComparisonAxis.position.x;
        float characterXPosition = transform.position.x;
        Vector3 anchorPos = iconAnchor.localPosition;

        anchorPos.x = (characterXPosition <= axisXPosition) ? -initialAnchorLocalX : initialAnchorLocalX;
        iconAnchor.localPosition = anchorPos;
    }
    
    private void RepositionIcons()
    {
        for (int i = 0; i < activeBuffs.Count; i++)
        {
            Vector3 newPosition = new Vector3(i * iconSpacing, 0, 0);
            activeBuffs[i].IconInstance.transform.localPosition = newPosition;
        }
    }
    #endregion
}