using System.Collections.Generic;
using UnityEngine;

public class RocketService
{
    private GridController gridController;

    public void Initialize(GridController controller)
    {
        gridController = controller;
    }

    public Vector2Int GetExplosionDirection(ItemType rocketType)
    {
        if (rocketType == ItemType.HorizontalRocket)
        {
            return Vector2Int.right;
        }
        else
        {
            return Vector2Int.up;
        }
    }

    public List<RocketItem> GetAdjacentRockets(Vector2Int position)
    {
        List<RocketItem> adjacentRockets = new List<RocketItem>();
        Vector2Int[] directions = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };
        
        foreach (Vector2Int dir in directions)
        {
            Vector2Int checkPos = position + dir;
            if (gridController.IsValidPosition(checkPos.x, checkPos.y))
            {
                BaseItem item = gridController.GetItem(checkPos.x, checkPos.y);
                if (item is RocketItem rocket)
                {
                    adjacentRockets.Add(rocket);
                }
            }
        }
        
        return adjacentRockets;
    }

    public void DamageItemsIn3x3Area(Vector2Int center)
    {
        for (int x = center.x - 1; x <= center.x + 1; x++)
        {
            for (int y = center.y - 1; y <= center.y + 1; y++)
            {
                if (gridController.IsValidPosition(x, y))
                {
                    BaseItem item = gridController.GetItem(x, y);
                    if (item != null)
                    {
                        DamageItem(item);
                    }
                }
            }
        }
    }

    private void DamageItem(BaseItem item)
    {
        switch (item)
        {
            case CubeItem cube:
                HandleCubeDamage(cube);
                break;
            case ObstacleItem obstacle:
                HandleObstacleDamage(obstacle);
                break;
            case RocketItem rocket:
                if (RocketController.Instance != null)
                {
                    RocketController.Instance.TriggerChainReaction(rocket);
                }
                break;
        }
    }

    private void HandleCubeDamage(CubeItem cube)
    {
        RectTransform rectTransform = cube.GetComponent<RectTransform>();
        ParticleEffectManager.Instance.GetPositionAndSize(rectTransform, out Vector2 position, out Vector2 size);
        
        ParticleEffectManager.Instance.SpawnCubeParticles(position, size, cube.itemType);
        AudioManager.Instance.PlayCubeBreakSound();
        
        PoolManager.Instance.ReturnItem(cube);
        cube.currentCell?.RemoveItem();
    }

    private void HandleObstacleDamage(ObstacleItem obstacle)
    {
        if (obstacle.CanTakeDamageFrom(DamageSource.Rocket))
        {
            obstacle.TakeDamage(1);
        }
    }

    public Vector2Int GetPerpendicularStartPosition(Vector2Int center, Vector2Int direction, int offset)
    {
        if (direction == Vector2Int.left || direction == Vector2Int.right)
        {
            return new Vector2Int(center.x, center.y + offset);
        }
        else
        {
            return new Vector2Int(center.x + offset, center.y);
        }
    }
}

