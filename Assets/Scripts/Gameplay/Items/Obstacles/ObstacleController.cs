using System.Collections.Generic;
using UnityEngine;

public class ObstacleController : MonoBehaviour
{
    public static ObstacleController Instance { get; private set; }
    
    private Dictionary<ItemType, int> obstacleGoals = new Dictionary<ItemType, int>();
    private Dictionary<ItemType, int> obstaclesRemaining = new Dictionary<ItemType, int>();

    public IReadOnlyDictionary<ItemType, int> GetRemainingGoals()
    {
        return obstaclesRemaining;
    }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        SubscribeToEvents();
    }

    private void OnDestroy()
    {
        UnsubscribeFromEvents();
    }

    private void SubscribeToEvents()
    {
        EventBus.Subscribe<LevelStartedEvent>(HandleLevelStarted);
        EventBus.Subscribe<ObstacleDestroyedEvent>(HandleObstacleDestroyed);
    }

    private void UnsubscribeFromEvents()
    {
        EventBus.Unsubscribe<LevelStartedEvent>(HandleLevelStarted);
        EventBus.Unsubscribe<ObstacleDestroyedEvent>(HandleObstacleDestroyed);
    }

    private void HandleLevelStarted(LevelStartedEvent evt)
    {
        LevelData levelData = LevelManager.Instance.GetLevelData(evt.LevelNumber);
        if (levelData != null)
        {
            InitializeGoals(levelData);
        }
    }

    public void InitializeGoals(LevelData levelData)
    {
        obstacleGoals.Clear();
        obstaclesRemaining.Clear();
        
        Dictionary<ItemType, int> goalCounts = levelData.GetObstacleGoals();
        
        foreach (var goal in goalCounts)
        {
            obstacleGoals[goal.Key] = goal.Value;
            obstaclesRemaining[goal.Key] = goal.Value;
        }
        
        UpdateGoalDisplay();
    }

    private void HandleObstacleDestroyed(ObstacleDestroyedEvent evt)
    {
        ItemType obstacleType = evt.ObstacleType;
        
        if (obstaclesRemaining.ContainsKey(obstacleType))
        {
            obstaclesRemaining[obstacleType] = Mathf.Max(0, obstaclesRemaining[obstacleType] - 1);
            
            UpdateGoalDisplay();
            
            if (AreAllObstaclesCleared())
            {
                GameStateController.Instance.WinLevel();
            }
        }
    }

    private void UpdateGoalDisplay()
    {
        foreach (var obstacle in obstaclesRemaining)
        {
            EventBus.Publish(new GoalUpdatedEvent
            {
                ObstacleType = obstacle.Key,
                RemainingCount = obstacle.Value
            });
        }
    }

    public int GetRemainingObstacles(ItemType obstacleType)
    {
        return obstaclesRemaining.ContainsKey(obstacleType) ? obstaclesRemaining[obstacleType] : 0;
    }

    public bool AreAllObstaclesCleared()
    {
        foreach (var obstacle in obstaclesRemaining)
        {
            if (obstacle.Value > 0)
                return false;
        }
        return true;
    }

    public Dictionary<ItemType, int> GetObstacleGoals()
    {
        return new Dictionary<ItemType, int>(obstacleGoals);
    }

    public Dictionary<ItemType, int> GetRemainingObstacles()
    {
        return new Dictionary<ItemType, int>(obstaclesRemaining);
    }
}

