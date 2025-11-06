using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CubeItem : BaseItem
{
    [Header("Cube Sprites")]
    [SerializeField] private Sprite redSprite;
    [SerializeField] private Sprite greenSprite;
    [SerializeField] private Sprite blueSprite;
    [SerializeField] private Sprite yellowSprite;
    
    [Header("Rocket Hint Sprites")]
    [SerializeField] private Sprite redRocketHint;
    [SerializeField] private Sprite greenRocketHint;
    [SerializeField] private Sprite blueRocketHint;
    [SerializeField] private Sprite yellowRocketHint;
    
    private bool showRocketHint = false;
    private float hintBlend = 0f;
    private Image hintOverlayImage;

    public override void Initialize(ItemType type)
    {
        base.Initialize(type);
        CreateHintOverlay();
        SetRocketHint(false);
        SetHintBlend(0f);
    }

    private void CreateHintOverlay()
    {
        GameObject overlayObj = new GameObject("HintOverlay");
        overlayObj.transform.SetParent(transform, false);
        
        RectTransform overlayRect = overlayObj.AddComponent<RectTransform>();
        
        overlayRect.anchorMin = Vector2.zero;
        overlayRect.anchorMax = Vector2.one;
        overlayRect.offsetMin = Vector2.zero;
        overlayRect.offsetMax = Vector2.zero;
        overlayRect.pivot = new Vector2(0.5f, 0.5f);
        
        hintOverlayImage = overlayObj.AddComponent<Image>();
        hintOverlayImage.sprite = GetRocketHintSprite();
        hintOverlayImage.raycastTarget = false;
        hintOverlayImage.preserveAspect = true;
        
        Color overlayColor = hintOverlayImage.color;
        overlayColor.a = 0f;
        hintOverlayImage.color = overlayColor;
        
        overlayObj.transform.SetSiblingIndex(transform.childCount - 1);
    }

    public override void OnTapped()
    {
        Vector2Int gridPos = GetGridPosition();
        
        if (gridPos.y < 0)
        {
            return;
        }
        
        EventBus.Publish(new CubeTappedEvent
        {
            GridX = gridPos.x,
            GridY = gridPos.y,
            CubeType = itemType
        });
    }

    public override Sprite GetSprite()
    {
        if (hintBlend > 0.5f)
        {
            return GetRocketHintSprite();
        }
        
        return GetNormalSprite();
    }

    public Sprite GetNormalSprite()
    {
        switch (itemType)
        {
            case ItemType.RedCube: return redSprite;
            case ItemType.GreenCube: return greenSprite;
            case ItemType.BlueCube: return blueSprite;
            case ItemType.YellowCube: return yellowSprite;
            default: return redSprite;
        }
    }

    public Sprite GetRocketHintSprite()
    {
        switch (itemType)
        {
            case ItemType.RedCube: return redRocketHint;
            case ItemType.GreenCube: return greenRocketHint;
            case ItemType.BlueCube: return blueRocketHint;
            case ItemType.YellowCube: return yellowRocketHint;
            default: return GetNormalSprite();
        }
    }

    public void SetRocketHint(bool show)
    {
        showRocketHint = show;
        
        if (show)
        {
            RocketHintAnimator.Instance.StartHintAnimation(new List<CubeItem> { this });
        }
        else
        {
            SetHintBlend(0f);
            RocketHintAnimator.Instance.RemoveCubeFromHints(this);
        }
    }

    public void SetHintBlend(float blend)
    {
        float newBlend = Mathf.Clamp01(blend);
        
        if (Mathf.Abs(hintBlend - newBlend) > 0.01f)
        {
            hintBlend = newBlend;
            
            itemImage.sprite = GetNormalSprite();
            itemImage.color = Color.white;
            
            Color overlayColor = hintOverlayImage.color;
            overlayColor.a = hintBlend;
            hintOverlayImage.color = overlayColor;
        }
    }

    public ItemType GetCubeColor()
    {
        return itemType;
    }

    public bool IsMatchingColor(CubeItem other)
    {
        return other != null && itemType == other.itemType;
    }

    public override bool CanFall()
    {
        return true;
    }

    public override void OnReturnToPool()
    {
        base.OnReturnToPool();
        
        SetRocketHint(false);
        SetHintBlend(0f);
        
        hintOverlayImage.color = Color.clear;
    }
} 