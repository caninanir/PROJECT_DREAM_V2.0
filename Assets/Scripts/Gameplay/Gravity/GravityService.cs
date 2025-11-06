using System.Collections.Generic;
using UnityEngine;

public class GravityService
{
    private GridController gridController;
    private readonly Dictionary<BaseItem, RectTransform> rectTransformCache = new Dictionary<BaseItem, RectTransform>();

    public void Initialize(GridController controller)
    {
        gridController = controller;
    }

    public int CalculateFallDistance(int x, int startY)
    {
        int distance = 0;
        
        for (int y = startY + 1; y < gridController.DataService.TotalHeight; y++)
        {
            GridCell cellBelow = gridController.DataService.GetExtendedCell(x, y);
            if (cellBelow != null && cellBelow.IsEmpty())
            {
                distance++;
            }
            else
            {
                break;
            }
        }
        
        return distance;
    }

    public void PrepareFallOperation(GridCell fromCell, GridCell toCell, int distance, List<FallOperation> wave, bool enableSubtleRotation, float maxRotationAngle)
    {
        if (toCell.currentItem?.IsPhantom() == true)
        {
            Object.Destroy(toCell.currentItem.gameObject);
            toCell.RemoveItem();
        }
        
        BaseItem item = fromCell.RemoveItem();
        RectTransform itemRect = GetCachedRectTransform(item);
        RectTransform fromRect = fromCell.GetComponent<RectTransform>();
        RectTransform toRect = toCell.GetComponent<RectTransform>();
        
        if (itemRect != null && fromRect != null && toRect != null)
        {
            Vector2 startPos = fromRect.anchoredPosition;
            Quaternion startRot = itemRect.rotation;
            
            toCell.SetItem(item);
            
            itemRect.anchoredPosition = startPos;
            
            float duration = CalculateFallDuration(distance);
            
            var operation = new FallOperation
            {
                item = item,
                itemTransform = itemRect,
                startPosition = startPos,
                targetPosition = toRect.anchoredPosition,
                fallDistance = distance,
                duration = duration,
                startRotation = startRot,
                targetRotation = enableSubtleRotation ? 
                    startRot * Quaternion.Euler(0, 0, Random.Range(-maxRotationAngle, maxRotationAngle)) : 
                    startRot
            };
            
            wave.Add(operation);
        }
        else
        {
            toCell.SetItem(item);
        }
    }

    private float CalculateFallDuration(int cellDistance)
    {
        const float fallTimePerCell = 0.08f;
        const float minimumFallTime = 0.1f;
        const float maximumFallTime = 0.6f;
        
        float duration = minimumFallTime + (cellDistance * fallTimePerCell);
        return Mathf.Clamp(duration, minimumFallTime, maximumFallTime);
    }

    private RectTransform GetCachedRectTransform(BaseItem item)
    {
        if (!rectTransformCache.TryGetValue(item, out RectTransform rect))
        {
            rect = item.GetComponent<RectTransform>();
            if (rect != null)
            {
                rectTransformCache[item] = rect;
            }
        }
        
        return rect;
    }
}

public class FallOperation
{
    public BaseItem item;
    public RectTransform itemTransform;
    public Vector2 startPosition;
    public Vector2 targetPosition;
    public int fallDistance;
    public float duration;
    public Quaternion startRotation;
    public Quaternion targetRotation;
}

