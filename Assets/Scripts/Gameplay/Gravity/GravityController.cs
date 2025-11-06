using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GravityController : MonoBehaviour
{
    [Header("Performance")]
    [SerializeField] private bool enableBatching = true;
    [SerializeField] private int maxOperationsPerFrame = 30;

    private GridController gridController;
    private GravityService gravityService;
    private FallAnimator fallAnimator;

    private void Awake()
    {
        gridController = FindFirstObjectByType<GridController>();
        gravityService = new GravityService();
        fallAnimator = GetComponent<FallAnimator>() ?? GetComponentInChildren<FallAnimator>();
        
        if (fallAnimator == null)
        {
            GameObject animatorObj = new GameObject("FallAnimator");
            animatorObj.transform.SetParent(transform);
            fallAnimator = animatorObj.AddComponent<FallAnimator>();
        }
        
        gravityService.Initialize(gridController);
        SubscribeToEvents();
    }

    private void OnDestroy()
    {
        UnsubscribeFromEvents();
    }

    private void SubscribeToEvents()
    {
        EventBus.Subscribe<GravityStartedEvent>(HandleGravityStarted);
        EventBus.Subscribe<MatchProcessedEvent>(HandleMatchProcessed);
    }

    private void UnsubscribeFromEvents()
    {
        EventBus.Unsubscribe<GravityStartedEvent>(HandleGravityStarted);
        EventBus.Unsubscribe<MatchProcessedEvent>(HandleMatchProcessed);
    }

    private void HandleGravityStarted(GravityStartedEvent evt)
    {
        StartCoroutine(ProcessGravity());
    }

    private void HandleMatchProcessed(MatchProcessedEvent evt)
    {
        StartCoroutine(ProcessGravity());
    }

    public IEnumerator ProcessGravity()
    {
        yield return StartCoroutine(ProcessAllFalls());
        
        gridController.SpawnNewCubes();
        EventBus.Publish(new GridUpdatedEvent());
        EventBus.Publish(new GravityCompletedEvent());
    }

    private IEnumerator ProcessAllFalls()
    {
        bool itemsAreFalling = true;
        int fallIterations = 0;
        const int maxFallIterations = 20;
        
        while (itemsAreFalling && fallIterations < maxFallIterations)
        {
            itemsAreFalling = false;
            List<FallOperation> currentWave = new List<FallOperation>();
            int operationsThisFrame = 0;
            
            for (int x = 0; x < gridController.DataService.GridWidth; x++)
            {
                for (int y = gridController.DataService.TotalHeight - 2; y >= 0; y--)
                {
                    if (enableBatching && operationsThisFrame >= maxOperationsPerFrame)
                    {
                        yield return null;
                        operationsThisFrame = 0;
                    }
                    
                    GridCell currentCell = gridController.DataService.GetExtendedCell(x, y);
                    if (currentCell?.currentItem == null || !currentCell.currentItem.CanFall()) continue;
                    
                    int fallDistance = gravityService.CalculateFallDistance(x, y);
                    if (fallDistance > 0)
                    {
                        GridCell targetCell = gridController.DataService.GetExtendedCell(x, y + fallDistance);
                        if (targetCell != null)
                        {
                            gravityService.PrepareFallOperation(
                                currentCell, 
                                targetCell, 
                                fallDistance, 
                                currentWave,
                                fallAnimator.enableSubtleRotation,
                                fallAnimator.maxRotationAngle
                            );
                            itemsAreFalling = true;
                            operationsThisFrame++;
                        }
                    }
                }
            }
            
            if (currentWave.Count > 0)
            {
                yield return StartCoroutine(fallAnimator.AnimateFalls(currentWave));
            }
            
            fallIterations++;
        }
    }
}

