using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UIElementPoolManager : MonoBehaviour
{
    public static UIElementPoolManager Instance { get; private set; }

    [Header("UI Prefabs")]
    [SerializeField] private GameObject goalItemPrefab;

    [Header("Pool Settings")]
    [SerializeField] private int goalItemPoolSize = 10;

    private GenericPool<GoalItem> goalItemPool;
    private Transform poolContainer;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            InitializePools();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void InitializePools()
    {
        poolContainer = new GameObject("UIElementPoolContainer").transform;
        poolContainer.SetParent(transform);

        GoalItem goalPrefab = goalItemPrefab.GetComponent<GoalItem>();
        if (goalPrefab == null)
        {
            goalPrefab = goalItemPrefab.AddComponent<GoalItem>();
        }
        goalItemPool = new GenericPool<GoalItem>(goalPrefab, poolContainer, goalItemPoolSize);
    }

    public GoalItem GetGoalItem(Transform parent = null)
    {
        GoalItem goalItem = goalItemPool.Get();
        if (parent != null)
        {
            goalItem.transform.SetParent(parent, false);
            goalItem.transform.localScale = Vector3.one;
        }

        return goalItem;
    }

    public void ReturnGoalItem(GoalItem goalItem)
    {
        if (goalItem == null || goalItemPool == null) return;

        goalItem.transform.SetParent(poolContainer, false);
        goalItemPool.Return(goalItem);
    }

    public void ReturnAllGoalItems(IEnumerable<GoalItem> goalItems)
    {
        foreach (var goalItem in goalItems)
        {
            ReturnGoalItem(goalItem);
        }
    }

    public void ClearAllPools()
    {
        goalItemPool?.Clear();
    }

    public void LogPoolStats()
    {
        Debug.Log($"UIElementPool Stats - GoalItems: {goalItemPool?.TotalCreated}/{goalItemPool?.PoolSize}");
    }

    public void SetGoalItemPrefab(GameObject prefab)
    {
        if (goalItemPrefab == null && prefab != null)
        {
            goalItemPrefab = prefab;
            
            if (goalItemPool == null)
            {
                GoalItem goalPrefab = goalItemPrefab.GetComponent<GoalItem>();
                if (goalPrefab == null)
                {
                    goalPrefab = goalItemPrefab.AddComponent<GoalItem>();
                }
                goalItemPool = new GenericPool<GoalItem>(goalPrefab, poolContainer, goalItemPoolSize);
            }
        }
    }
} 