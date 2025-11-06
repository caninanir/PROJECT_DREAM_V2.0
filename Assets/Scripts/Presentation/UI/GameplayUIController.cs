using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class GameplayUIController : MonoBehaviour
{
    [Header("Top UI Elements")]
    [SerializeField] private TextMeshProUGUI movesText;
    [SerializeField] private TextMeshProUGUI levelText;
    [SerializeField] private Transform goalsContainer;
    [SerializeField] private GameObject goalItemPrefab;
    
    [Header("Goal Display")]
    [SerializeField] private TextMeshProUGUI goalHeaderText;

    private GoalDisplayController goalDisplayController;
    private Dictionary<ItemType, GoalItem> goalItems = new Dictionary<ItemType, GoalItem>();

    public IReadOnlyDictionary<ItemType, GoalItem> GetGoalItems()
    {
        return goalItems;
    }

    private void Awake()
    {
        goalDisplayController = GetComponent<GoalDisplayController>() ?? GetComponentInChildren<GoalDisplayController>() ?? gameObject.AddComponent<GoalDisplayController>();
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
        EventBus.Subscribe<MovesChangedEvent>(HandleMovesChanged);
        EventBus.Subscribe<GoalUpdatedEvent>(HandleGoalUpdated);
    }

    private void UnsubscribeFromEvents()
    {
        EventBus.Unsubscribe<LevelStartedEvent>(HandleLevelStarted);
        EventBus.Unsubscribe<MovesChangedEvent>(HandleMovesChanged);
        EventBus.Unsubscribe<GoalUpdatedEvent>(HandleGoalUpdated);
    }

    private void HandleLevelStarted(LevelStartedEvent evt)
    {
        StartCoroutine(SetupGoalsDelayed());
        UpdateMovesDisplay(GameStateController.Instance.MovesRemaining);
        UpdateLevelDisplay(evt.LevelNumber);
    }
    
    private void UpdateLevelDisplay(int levelNumber)
    {
        if (levelText != null)
        {
            levelText.text = $"Level {levelNumber}";
        }
    }

    private System.Collections.IEnumerator SetupGoalsDelayed()
    {
        yield return new WaitForEndOfFrame();
        SetupGoals();
    }

    private void HandleMovesChanged(MovesChangedEvent evt)
    {
        UpdateMovesDisplay(evt.MovesRemaining);
    }

    private void HandleGoalUpdated(GoalUpdatedEvent evt)
    {
        UpdateGoalProgress(evt.ObstacleType, evt.RemainingCount);
    }

    private void SetupGoals()
    {
        LevelData levelData = LevelManager.Instance.GetCurrentLevelData();
        
        ClearGoals();
        
        Dictionary<ItemType, int> obstacleGoals = levelData.GetObstacleGoals();
        goalDisplayController.SetupGoalLayout(goalsContainer, obstacleGoals, goalItemPrefab, goalItems);
        
        goalHeaderText.text = "Goal";
    }

    private void ClearGoals()
    {
        goalDisplayController.ClearGoals(goalItems);
        goalItems.Clear();
    }

    public void UpdateGoalProgress(ItemType itemType, int remaining)
    {
        if (goalItems.ContainsKey(itemType))
        {
            goalItems[itemType].UpdateCount(remaining);
        }
    }

    private void UpdateMovesDisplay(int moves)
    {
        movesText.text = moves.ToString();
    }
}

