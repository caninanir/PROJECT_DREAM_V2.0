using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RocketController : MonoBehaviour
{
    public static RocketController Instance { get; private set; }
    
    private GridController gridController;
    private RocketService rocketService;
    private RocketAnimator rocketAnimator;
    private RocketProjectileService projectileService;

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
        gridController = FindFirstObjectByType<GridController>();
        rocketService = new RocketService();
        rocketAnimator = GetComponent<RocketAnimator>() ?? GetComponentInChildren<RocketAnimator>();
        projectileService = GetComponent<RocketProjectileService>() ?? GetComponentInChildren<RocketProjectileService>();
        
        if (rocketAnimator == null)
        {
            GameObject animatorObj = new GameObject("RocketAnimator");
            animatorObj.transform.SetParent(transform);
            rocketAnimator = animatorObj.AddComponent<RocketAnimator>();
        }
        
        if (projectileService == null)
        {
            GameObject projectileObj = new GameObject("RocketProjectileService");
            projectileObj.transform.SetParent(transform);
            projectileService = projectileObj.AddComponent<RocketProjectileService>();
        }
        
        rocketService.Initialize(gridController);
        SubscribeToEvents();
    }

    private void OnDestroy()
    {
        UnsubscribeFromEvents();
    }

    private void SubscribeToEvents()
    {
        EventBus.Subscribe<RocketTappedEvent>(HandleRocketTapped);
    }

    private void UnsubscribeFromEvents()
    {
        EventBus.Unsubscribe<RocketTappedEvent>(HandleRocketTapped);
    }

    private void HandleRocketTapped(RocketTappedEvent evt)
    {
        if (GameStateController.Instance.IsProcessingMove)
        {
            return;
        }
        
        BaseItem item = gridController.GetItem(evt.GridX, evt.GridY);
        if (item is RocketItem rocket)
        {
            StartCoroutine(ProcessRocketExplosion(rocket));
        }
    }

    public void ExplodeRocket(RocketItem rocket)
    {
        StartCoroutine(ProcessRocketExplosion(rocket));
    }

    private IEnumerator ProcessRocketExplosion(RocketItem rocket)
    {
        GameStateController.Instance.SetProcessingMove(true);
        GameStateController.Instance.UseMove();
        
        Vector2Int rocketPos = rocket.GetGridPosition();
        List<RocketItem> adjacentRockets = rocketService.GetAdjacentRockets(rocketPos);
        
        if (adjacentRockets.Count > 0)
        {
            yield return ProcessRocketCombo(rocket, adjacentRockets);
        }
        else
        {
            yield return ProcessSingleRocketExplosion(rocket);
        }
        
        EventBus.Publish(new GravityStartedEvent());
        
        if (GameStateController.Instance.CheckWinCondition())
        {
            GameStateController.Instance.WinLevel();
        }
        else if (GameStateController.Instance.CheckLoseCondition())
        {
            GameStateController.Instance.LoseLevel();
        }
        
        GameStateController.Instance.SetProcessingMove(false);
    }

    private IEnumerator ProcessSingleRocketExplosion(RocketItem rocket)
    {
        Vector2Int rocketPos = rocket.GetGridPosition();
        Vector2Int direction = rocketService.GetExplosionDirection(rocket.itemType);
        
        AudioManager.Instance.PlayRocketPopSound();
        
        DestroyRocket(rocket);
        
        EventBus.Publish(new RocketExplodedEvent
        {
            GridX = rocketPos.x,
            GridY = rocketPos.y,
            RocketType = rocket.itemType,
            IsCombo = false
        });
        
        StartCoroutine(projectileService.AnimateProjectile(rocketPos, direction));
        StartCoroutine(projectileService.AnimateProjectile(rocketPos, -direction));
        
        yield return projectileService.WaitForAllProjectilesToComplete();
    }

    private IEnumerator ProcessRocketCombo(RocketItem mainRocket, List<RocketItem> adjacentRockets)
    {
        Vector2Int comboCenter = mainRocket.GetGridPosition();
        
        DestroyRocket(mainRocket);
        foreach (RocketItem r in adjacentRockets)
        {
            DestroyRocket(r);
        }

        AudioManager.Instance.PlayComboRocketPopSound();

        yield return Create3x3ExplosionWithProjectiles(comboCenter);
        yield return projectileService.WaitForAllProjectilesToComplete();
    }

    private IEnumerator Create3x3ExplosionWithProjectiles(Vector2Int center)
    {
        rocketService.DamageItemsIn3x3Area(center);
        yield return new WaitForSeconds(0.1f);
        yield return projectileService.SpawnComboProjectiles(center, rocketService);
    }

    private void DestroyRocket(RocketItem rocket)
    {
        Destroy(rocket.gameObject);
        rocket.currentCell?.RemoveItem();
    }

    public IEnumerator AnimateRocketCreation(List<GridCell> sourceGroup, GridCell targetCell, ItemType rocketType)
    {
        yield return rocketAnimator.AnimateRocketCreation(sourceGroup, targetCell, rocketType);
    }

    public void TriggerChainReaction(RocketItem rocket)
    {
        StartCoroutine(ProcessChainReactionExplosion(rocket));
    }

    private IEnumerator ProcessChainReactionExplosion(RocketItem rocket)
    {
        Vector2Int rocketPos = rocket.GetGridPosition();
        Vector2Int direction = rocketService.GetExplosionDirection(rocket.itemType);
        
        AudioManager.Instance.PlayRocketPopSound();
        DestroyRocket(rocket);
        
        StartCoroutine(projectileService.AnimateProjectile(rocketPos, direction));
        StartCoroutine(projectileService.AnimateProjectile(rocketPos, -direction));
        
        yield return new WaitForSeconds(0.1f);
    }
}

