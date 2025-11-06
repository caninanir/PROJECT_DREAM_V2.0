using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class LevelData
{
    public int level_number;
    public int grid_width;
    public int grid_height;
    public int move_count;
    public string[] grid;

    public ItemType ParseGridItem(string item)
    {
        switch (item.ToLower())
        {
            case "r": return ItemType.RedCube;
            case "g": return ItemType.GreenCube;
            case "b": return ItemType.BlueCube;
            case "y": return ItemType.YellowCube;
            case "rand": return ItemType.RandomCube;
            case "vro": return ItemType.VerticalRocket;
            case "hro": return ItemType.HorizontalRocket;
            case "bo": return ItemType.Box;
            case "s": return ItemType.Stone;
            case "v": return ItemType.Vase;
            default: return ItemType.Empty;
        }
    }

    public ItemType GetItemAt(int x, int y)
    {
        if (!IsValidPosition(x, y)) return ItemType.Empty;
        
        int index = y * grid_width + x;
        if (index >= 0 && index < grid.Length)
        {
            return ParseGridItem(grid[index]);
        }
        return ItemType.Empty;
    }

    public bool IsValidPosition(int x, int y)
    {
        return x >= 0 && x < grid_width && y >= 0 && y < grid_height;
    }

    public Dictionary<ItemType, int> GetObstacleGoals()
    {
        Dictionary<ItemType, int> goals = new Dictionary<ItemType, int>();
        
        foreach (string item in grid)
        {
            ItemType itemType = ParseGridItem(item);
            if (IsObstacle(itemType))
            {
                if (goals.ContainsKey(itemType))
                    goals[itemType]++;
                else
                    goals[itemType] = 1;
            }
        }
        
        return goals;
    }

    private ItemType GetRandomCubeType()
    {
        ItemType[] cubeTypes = { ItemType.RedCube, ItemType.GreenCube, ItemType.BlueCube, ItemType.YellowCube };
        return cubeTypes[UnityEngine.Random.Range(0, cubeTypes.Length)];
    }

    private bool IsObstacle(ItemType itemType)
    {
        return itemType == ItemType.Box || itemType == ItemType.Stone || itemType == ItemType.Vase;
    }
} 