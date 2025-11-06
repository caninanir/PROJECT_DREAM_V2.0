using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MatchController : MonoBehaviour
{
    private GridController gridController;
    private MatchDetectorService matchDetector;
    private RocketHintService rocketHintService;

    private void Awake()
    {
        gridController = FindFirstObjectByType<GridController>();
        matchDetector = new MatchDetectorService();
        rocketHintService = new RocketHintService();
        
        matchDetector.Initialize(gridController);
        rocketHintService.Initialize(gridController, matchDetector);
        
        SubscribeToEvents();
    }

    private void OnDestroy()
    {
        UnsubscribeFromEvents();
    }

    private void SubscribeToEvents()
    {
        EventBus.Subscribe<CubeTappedEvent>(HandleCubeTapped);
        EventBus.Subscribe<GridUpdatedEvent>(HandleGridUpdated);
    }

    private void UnsubscribeFromEvents()
    {
        EventBus.Unsubscribe<CubeTappedEvent>(HandleCubeTapped);
        EventBus.Unsubscribe<GridUpdatedEvent>(HandleGridUpdated);
    }

    private void HandleCubeTapped(CubeTappedEvent evt)
    {
        if (GameStateController.Instance.IsProcessingMove)
        {
            return;
        }
        
        StartCoroutine(ProcessCubeTap(evt));
    }

    private IEnumerator ProcessCubeTap(CubeTappedEvent evt)
    {
        GameStateController.Instance.SetProcessingMove(true);
        
        GridCell tappedCell = gridController.GetCell(evt.GridX, evt.GridY);
        if (tappedCell?.currentItem is CubeItem tappedCube)
        {
            List<GridCell> matchingGroup = matchDetector.FindMatchingGroup(tappedCell);
            
            if (MatchValidator.IsValidMatch(matchingGroup))
            {
                GameStateController.Instance.UseMove();
                
                bool shouldCreateRocket = MatchValidator.CanCreateRocket(matchingGroup);
                GridCell rocketCell = shouldCreateRocket ? tappedCell : null;
                ItemType rocketType = ItemType.Empty;
                
                if (shouldCreateRocket && rocketCell != null)
                {
                    rocketType = Random.Range(0, 2) == 0 ? ItemType.HorizontalRocket : ItemType.VerticalRocket;
                    yield return StartCoroutine(RocketController.Instance.AnimateRocketCreation(matchingGroup, rocketCell, rocketType));
                }
                else
                {
                    foreach (GridCell cell in matchingGroup)
                    {
                        if (cell.currentItem is CubeItem cube)
                        {
                            RectTransform rectTransform = cube.GetComponent<RectTransform>();
                            ParticleEffectManager.Instance.GetPositionAndSize(rectTransform, out Vector2 position, out Vector2 size);
                            
                            ParticleEffectManager.Instance.SpawnCubeParticles(position, size, cube.itemType);
                            AudioManager.Instance.PlayCubeBreakSound();
                            
                            PoolManager.Instance.ReturnItem(cell.currentItem);
                            cell.RemoveItem();
                        }
                    }
                }
                
                if (shouldCreateRocket && rocketCell != null)
                {
                    int visibleY = rocketCell.y - gridController.BufferRows;
                    if (visibleY >= 0 && visibleY < gridController.DataService.GridHeight)
                    {
                        gridController.SpawnItem(rocketType, rocketCell.x, visibleY);
                    }
                }
                
                DamageAdjacentObstacles(matchingGroup);
                
                EventBus.Publish(new MatchProcessedEvent
                {
                    MatchCount = matchingGroup.Count,
                    RocketCreated = shouldCreateRocket
                });
                
                yield return new WaitForSeconds(0.2f);
                
                EventBus.Publish(new GravityStartedEvent());
                
                if (GameStateController.Instance.CheckWinCondition())
                {
                    GameStateController.Instance.WinLevel();
                }
                else if (GameStateController.Instance.CheckLoseCondition())
                {
                    GameStateController.Instance.LoseLevel();
                }
            }
        }
        
        GameStateController.Instance.SetProcessingMove(false);
    }

    private void DamageAdjacentObstacles(List<GridCell> blastCells)
    {
        HashSet<ObstacleItem> damagedObstacles = new HashSet<ObstacleItem>();
        
        foreach (GridCell blastCell in blastCells)
        {
            int visibleY = blastCell.y - gridController.BufferRows;
            
            if (visibleY >= 0 && visibleY < gridController.DataService.GridHeight)
            {
                List<GridCell> adjacentCells = gridController.GetAdjacentCells(blastCell.x, visibleY);
                
                foreach (GridCell adjacent in adjacentCells)
                {
                    if (adjacent.currentItem is ObstacleItem obstacle && !damagedObstacles.Contains(obstacle))
                    {
                        if (obstacle.CanTakeDamageFrom(DamageSource.AdjacentBlast))
                        {
                            obstacle.TakeDamage(1);
                            damagedObstacles.Add(obstacle);
                        }
                    }
                }
            }
        }
    }

    private void HandleGridUpdated(GridUpdatedEvent evt)
    {
        rocketHintService.UpdateRocketHints();
    }

    public List<GridCell> FindMatchingGroup(GridCell startCell)
    {
        return matchDetector.FindMatchingGroup(startCell);
    }

    public bool IsValidMatch(List<GridCell> group)
    {
        return MatchValidator.IsValidMatch(group);
    }

    public bool CanCreateRocket(List<GridCell> group)
    {
        return MatchValidator.CanCreateRocket(group);
    }

    public void UpdateRocketHints()
    {
        rocketHintService.UpdateRocketHints();
    }
}

