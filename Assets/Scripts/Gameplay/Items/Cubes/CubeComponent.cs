using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CubeComponent : MonoBehaviour
{
    private CubeItem cubeItem;
    private bool showRocketHint = false;
    private float hintBlend = 0f;
    private Image hintOverlayImage;

    public void Initialize(CubeItem item)
    {
        cubeItem = item;
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
        hintOverlayImage.sprite = cubeItem.GetRocketHintSprite();
        hintOverlayImage.raycastTarget = false;
        hintOverlayImage.preserveAspect = true;
        
        Color overlayColor = hintOverlayImage.color;
        overlayColor.a = 0f;
        hintOverlayImage.color = overlayColor;
        
        overlayObj.transform.SetSiblingIndex(transform.childCount - 1);
    }

    public void SetRocketHint(bool show)
    {
        showRocketHint = show;
        
        if (show)
        {
            if (RocketHintAnimator.Instance != null)
            {
                RocketHintAnimator.Instance.StartHintAnimation(new List<CubeItem> { cubeItem });
            }
        }
        else
        {
            SetHintBlend(0f);
            if (RocketHintAnimator.Instance != null)
            {
                RocketHintAnimator.Instance.RemoveCubeFromHints(cubeItem);
            }
        }
    }

    public void SetHintBlend(float blend)
    {
        float newBlend = Mathf.Clamp01(blend);
        
        if (Mathf.Abs(hintBlend - newBlend) > 0.01f)
        {
            hintBlend = newBlend;
            
            if (cubeItem.itemImage != null)
            {
                cubeItem.itemImage.sprite = cubeItem.GetNormalSprite();
                cubeItem.itemImage.color = Color.white;
            }
            
            if (hintOverlayImage != null)
            {
                Color overlayColor = hintOverlayImage.color;
                overlayColor.a = hintBlend;
                hintOverlayImage.color = overlayColor;
            }
        }
    }

    public void ResetForPool()
    {
        SetRocketHint(false);
        SetHintBlend(0f);
        
        if (hintOverlayImage != null)
        {
            hintOverlayImage.color = Color.clear;
        }
    }
}




