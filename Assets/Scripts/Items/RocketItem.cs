using UnityEngine;

public class RocketItem : BaseItem
{
    [Header("Rocket Sprites")]
    [SerializeField] private Sprite horizontalRocketSprite;
    [SerializeField] private Sprite verticalRocketSprite;

    public override void Initialize(ItemType type)
    {
        base.Initialize(type);
        
        itemImage.preserveAspect = true;
    }

    public override void OnTapped()
    {
        if (GameStateController.Instance.IsProcessingMove)
        {
            return;
        }
        
        Vector2Int gridPos = GetGridPosition();
        
        EventBus.Publish(new RocketTappedEvent
        {
            GridX = gridPos.x,
            GridY = gridPos.y,
            RocketType = itemType
        });
    }

    public override Sprite GetSprite()
    {
        switch (itemType)
        {
            case ItemType.HorizontalRocket:
                return horizontalRocketSprite;
            case ItemType.VerticalRocket:
                return verticalRocketSprite;
            default:
                return horizontalRocketSprite;
        }
    }

    public override bool CanFall()
    {
        return true;
    }

    public bool IsHorizontal()
    {
        return itemType == ItemType.HorizontalRocket;
    }

    public bool IsVertical()
    {
        return itemType == ItemType.VerticalRocket;
    }

    public Vector2Int GetExplosionDirection()
    {
        if (IsHorizontal())
            return Vector2Int.right;
        else
            return Vector2Int.up;
    }
} 