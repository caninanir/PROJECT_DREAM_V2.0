using UnityEngine;

public static class ObstacleBehaviorFactory
{
    public static ObstacleBehavior CreateBehavior(ItemType obstacleType, GameObject obstacleObject)
    {
        ObstacleBehavior behavior = null;
        
        switch (obstacleType)
        {
            case ItemType.Box:
                behavior = obstacleObject.AddComponent<BoxBehavior>();
                behavior.Initialize(obstacleObject.GetComponent<ObstacleItem>(), 1);
                break;
                
            case ItemType.Stone:
                behavior = obstacleObject.AddComponent<StoneBehavior>();
                behavior.Initialize(obstacleObject.GetComponent<ObstacleItem>(), 1);
                break;
                
            case ItemType.Vase:
                behavior = obstacleObject.AddComponent<VaseBehavior>();
                behavior.Initialize(obstacleObject.GetComponent<ObstacleItem>(), 2);
                break;
        }
        
        return behavior;
    }

    public static int GetMaxHealth(ItemType obstacleType)
    {
        switch (obstacleType)
        {
            case ItemType.Box:
            case ItemType.Stone:
                return 1;
            case ItemType.Vase:
                return 2;
            default:
                return 1;
        }
    }
}




