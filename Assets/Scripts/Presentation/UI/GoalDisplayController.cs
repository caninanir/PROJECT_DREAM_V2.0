using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GoalDisplayController : MonoBehaviour
{
    public void SetupGoalLayout(Transform container, Dictionary<ItemType, int> obstacleGoals, GameObject goalItemPrefab, Dictionary<ItemType, GoalItem> goalItems)
    {
        if (container == null) return;
        
        int goalCount = obstacleGoals.Count;
        List<ItemType> goalTypes = new List<ItemType>(obstacleGoals.Keys);
        
        ClearLayoutComponents(container);
        ApplyLayoutForGoalCount(container, goalCount);
        
        for (int i = 0; i < goalTypes.Count; i++)
        {
            ItemType goalType = goalTypes[i];
            int count = obstacleGoals[goalType];
            CreateGoalItem(container, goalType, count, goalItemPrefab, goalItems);
        }
    }

    private void ClearLayoutComponents(Transform container)
    {
        HorizontalLayoutGroup hLayout = container.GetComponent<HorizontalLayoutGroup>();
        if (hLayout != null) DestroyImmediate(hLayout);
        
        GridLayoutGroup gLayout = container.GetComponent<GridLayoutGroup>();
        if (gLayout != null) DestroyImmediate(gLayout);
    }

    private void ApplyLayoutForGoalCount(Transform container, int goalCount)
    {
        if (goalCount == 1)
        {
            HorizontalLayoutGroup layout = container.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 0;
            layout.padding = new RectOffset(20, 20, 20, 20);
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = true;
        }
        else if (goalCount == 2)
        {
            HorizontalLayoutGroup layout = container.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 10;
            layout.padding = new RectOffset(0, 0, 35, 35);
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = true;
        }
        else if (goalCount == 3)
        {
            GridLayoutGroup layout = container.gameObject.AddComponent<GridLayoutGroup>();
            layout.spacing = new Vector2(15, 15);
            layout.padding = new RectOffset(0, 0, 0, 0);
            layout.constraint = GridLayoutGroup.Constraint.FixedRowCount;
            layout.constraintCount = 2;
            layout.startCorner = GridLayoutGroup.Corner.UpperLeft;
            layout.cellSize = new Vector2(80, 80);
            layout.childAlignment = TextAnchor.MiddleCenter;
        }
    }

    private void CreateGoalItem(Transform container, ItemType itemType, int count, GameObject goalItemPrefab, Dictionary<ItemType, GoalItem> goalItems)
    {
        GoalItem goalItem = null;
        
        if (UIElementPoolManager.Instance != null)
        {
            if (goalItemPrefab != null)
            {
                UIElementPoolManager.Instance.SetGoalItemPrefab(goalItemPrefab);
            }
            goalItem = UIElementPoolManager.Instance.GetGoalItem(container);
        }
        else
        {
            GameObject goalObj = Instantiate(goalItemPrefab, container);
            goalItem = goalObj.GetComponent<GoalItem>();
            if (goalItem == null)
            {
                goalItem = goalObj.AddComponent<GoalItem>();
            }
        }
        
        if (goalItem != null)
        {
            goalItem.Initialize(itemType, count);
            goalItems[itemType] = goalItem;
        }
    }

    public void ClearGoals(Dictionary<ItemType, GoalItem> goalItems)
    {
        if (UIElementPoolManager.Instance != null)
        {
            UIElementPoolManager.Instance.ReturnAllGoalItems(goalItems.Values);
        }
        else
        {
            foreach (var goalItem in goalItems.Values)
            {
                if (goalItem != null)
                {
                    Destroy(goalItem.gameObject);
                }
            }
        }
    }
}

