using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class RocketProjectileService : MonoBehaviour
{
    [Header("Rocket Projectile Prefabs")] 
    [SerializeField] private GameObject projectilePrefabUp;
    [SerializeField] private GameObject projectilePrefabDown;
    [SerializeField] private GameObject projectilePrefabLeft;
    [SerializeField] private GameObject projectilePrefabRight;

    [Header("Rocket Projectile Settings")]
    [Tooltip("How fast rockets fly across the grid (time per cell in seconds). Lower values = faster rockets.")]
    [SerializeField] [Range(0.05f, 1.0f)] private float rocketSpeed = 0.15f;

    private GridController gridController;
    private List<Vector2Int> phantomPositions = new List<Vector2Int>();
    private int activeProjectiles = 0;
    private Dictionary<GameObject, GameObject> duplicateProjectiles = new Dictionary<GameObject, GameObject>();

    public float RocketSpeed => rocketSpeed;

    private void Awake()
    {
        gridController = FindFirstObjectByType<GridController>();
    }

    public IEnumerator AnimateProjectile(Vector2Int startPos, Vector2Int direction)
    {
        activeProjectiles++;
        
        GameObject projectile = CreateRocketProjectile(startPos, direction);
        if (projectile == null)
        {
            activeProjectiles--;
            yield break;
        }

        RectTransform projectileRect = projectile.GetComponent<RectTransform>();
        GameObject duplicateProjectile = GetDuplicateProjectile(projectile);
        RectTransform duplicateRect = duplicateProjectile?.GetComponent<RectTransform>();

        yield return MoveProjectileThroughGrid(projectileRect, duplicateRect, startPos, direction);
        
        activeProjectiles--;
        if (activeProjectiles <= 0) ClearAllPhantoms();

        if (duplicateRect != null)
        {
            duplicateRect.anchoredPosition = GetUIPosition(projectileRect.transform);
        }

        yield return MoveProjectileOffScreen(projectileRect, duplicateRect, direction);
        yield return FadeOutProjectile(projectile, duplicateProjectile, direction);
        
        CleanupProjectile(projectile);
    }

    public IEnumerator WaitForAllProjectilesToComplete()
    {
        while (activeProjectiles > 0)
        {
            yield return null;
        }
        
        if (phantomPositions.Count > 0)
        {
            ClearAllPhantoms();
        }
        
        yield return new WaitForSeconds(0.1f);
    }

    public IEnumerator SpawnComboProjectiles(Vector2Int center, RocketService rocketService)
    {
        Vector2Int[] directions = { Vector2Int.left, Vector2Int.right, Vector2Int.up, Vector2Int.down };
        
        foreach (Vector2Int direction in directions)
        {
            for (int offset = -1; offset <= 1; offset++)
            {
                Vector2Int projectileStart = rocketService.GetPerpendicularStartPosition(center, direction, offset);
                if (gridController.IsValidPosition(projectileStart.x, projectileStart.y))
                {
                    StartCoroutine(AnimateProjectile(projectileStart, direction));
                }
            }
        }
        
        yield return null;
    }

    private GameObject GetDuplicateProjectile(GameObject projectile)
    {
        return duplicateProjectiles.ContainsKey(projectile) ? duplicateProjectiles[projectile] : null;
    }

    private IEnumerator MoveProjectileThroughGrid(RectTransform projectileRect, RectTransform duplicateRect, Vector2Int startPos, Vector2Int direction)
    {
        Vector2Int currentPos = startPos + direction;
        float cellSize = 60f;
        
        if (duplicateRect != null && !gridController.IsValidPosition(currentPos.x, currentPos.y))
        {
            duplicateRect.anchoredPosition = GetUIPosition(projectileRect.transform);
        }

        while (gridController.IsValidPosition(currentPos.x, currentPos.y))
        {
            GridCell targetCell = gridController.GetCell(currentPos.x, currentPos.y);
            if (targetCell != null)
            {
                RectTransform targetRect = targetCell.GetComponent<RectTransform>();
                if (targetRect != null)
                {
                    cellSize = Mathf.Max(targetRect.sizeDelta.x, targetRect.sizeDelta.y);
                    yield return MoveProjectileToPosition(projectileRect, duplicateRect, targetRect.anchoredPosition, direction, cellSize);
                }
            }

            ProcessProjectileAtPosition(currentPos);
            currentPos += direction;
        }
    }

    private IEnumerator MoveProjectileToPosition(RectTransform projectileRect, RectTransform duplicateRect, Vector2 targetPosition, Vector2Int direction, float cellSize)
    {
        Vector2 startPosition = projectileRect.anchoredPosition;
        
        yield return AnimateMovement(projectileRect, duplicateRect, startPosition, targetPosition, rocketSpeed);
    }

    private void ProcessProjectileAtPosition(Vector2Int position)
    {
        BaseItem item = gridController.GetItem(position.x, position.y);
        if (item != null)
        {
            DamageItem(item);
        }
        PlacePhantom(position);
    }

    private void DamageItem(BaseItem item)
    {
        switch (item)
        {
            case CubeItem cube:
                RectTransform rectTransform = cube.GetComponent<RectTransform>();
                ParticleEffectManager.Instance.GetPositionAndSize(rectTransform, out Vector2 position, out Vector2 size);
                
                ParticleEffectManager.Instance.SpawnCubeParticles(position, size, cube.itemType);
                AudioManager.Instance.PlayCubeBreakSound();
                PoolManager.Instance.ReturnItem(cube);
                cube.currentCell?.RemoveItem();
                break;
            case ObstacleItem obstacle:
                if (obstacle.CanTakeDamageFrom(DamageSource.Rocket))
                {
                    obstacle.TakeDamage(1);
                }
                break;
            case RocketItem rocket:
                StartCoroutine(ProcessChainReactionExplosion(rocket));
                break;
        }
    }

    private IEnumerator ProcessChainReactionExplosion(RocketItem rocket)
    {
        Vector2Int rocketPos = rocket.GetGridPosition();
        Vector2Int direction = GetRocketDirection(rocket.itemType);
        
        AudioManager.Instance.PlayRocketPopSound();
        DestroyRocket(rocket);
        
        StartCoroutine(AnimateProjectile(rocketPos, direction));
        StartCoroutine(AnimateProjectile(rocketPos, -direction));
        
        yield return new WaitForSeconds(0.1f);
    }

    private Vector2Int GetRocketDirection(ItemType rocketType)
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

    private void DestroyRocket(RocketItem rocket)
    {
        Destroy(rocket.gameObject);
        rocket.currentCell?.RemoveItem();
    }

    private void PlacePhantom(Vector2Int position)
    {
        if (phantomPositions.Contains(position)) return;
        
        phantomPositions.Add(position);
        GridCell cell = gridController.GetCell(position.x, position.y);
        
        if (cell != null && cell.IsEmpty())
        {
            GameObject phantomObj = new GameObject("Phantom");
            phantomObj.transform.SetParent(gridController.GridContainer, false);
            
            RectTransform phantomRect = phantomObj.AddComponent<RectTransform>();
            Image phantomImage = phantomObj.AddComponent<Image>();
            PhantomItem phantom = phantomObj.AddComponent<PhantomItem>();
            phantom.Initialize(ItemType.Empty);
            
            cell.SetItem(phantom);
        }
    }

    private void ClearAllPhantoms()
    {
        foreach (Vector2Int pos in phantomPositions)
        {
            GridCell cell = gridController.GetCell(pos.x, pos.y);
            if (cell?.currentItem is PhantomItem phantom)
            {
                Destroy(phantom.gameObject);
                cell.RemoveItem();
            }
        }
        phantomPositions.Clear();
    }

    private IEnumerator MoveProjectileOffScreen(RectTransform projectileRect, RectTransform duplicateRect, Vector2Int direction)
    {
        float cellSize = 80f;
        int offScreenSteps = 16;
        
        Vector2Int originalDirection = direction;
        bool isVertical = direction == Vector2Int.up || direction == Vector2Int.down;
        if (isVertical)
        {
            originalDirection = -direction;
        }

        for (int step = 0; step < offScreenSteps; step++)
        {
            Vector2 startPosition = projectileRect.anchoredPosition;
            Vector2 targetPosition = startPosition + new Vector2(originalDirection.x * cellSize, originalDirection.y * cellSize);
            
            yield return AnimateMovement(projectileRect, duplicateRect, startPosition, targetPosition, rocketSpeed);
        }
    }

    private IEnumerator FadeOutProjectile(GameObject projectile, GameObject duplicateProjectile, Vector2Int direction)
    {
        Image projectileImage = projectile.GetComponent<Image>();
        Image duplicateImage = duplicateProjectile?.GetComponent<Image>();
        
        Color originalColor = projectileImage.color;
        Color duplicateOriginalColor = duplicateImage?.color ?? Color.white;
        
        RectTransform projectileRect = projectile.GetComponent<RectTransform>();
        RectTransform duplicateRect = duplicateProjectile?.GetComponent<RectTransform>();
        
        float cellSize = 60f;
        
        Vector2Int originalDirection = direction;
        bool isVertical = direction == Vector2Int.up || direction == Vector2Int.down;
        if (isVertical)
        {
            originalDirection = -direction;
        }

        for (int fadeStep = 0; fadeStep < 3; fadeStep++)
        {
            Vector2 startPosition = projectileRect.anchoredPosition;
            Vector2 targetPosition = startPosition + new Vector2(originalDirection.x * cellSize, originalDirection.y * cellSize);
            
            float fadeAlpha = 1f - ((fadeStep + 1) / 3f);
            
            yield return AnimateMovementWithFade(projectileRect, duplicateRect, projectileImage, duplicateImage, 
                startPosition, targetPosition, originalColor, duplicateOriginalColor, fadeAlpha, rocketSpeed);
        }
    }

    private IEnumerator AnimateMovement(RectTransform projectileRect, RectTransform duplicateRect, 
        Vector2 startPos, Vector2 targetPos, float moveTime)
    {
        float elapsed = 0f;
        
        while (elapsed < moveTime)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / moveTime;
            
            projectileRect.anchoredPosition = Vector2.Lerp(startPos, targetPos, t);
            
            if (duplicateRect != null)
            {
                duplicateRect.anchoredPosition = GetUIPosition(projectileRect.transform);
            }
            
            yield return null;
        }

        projectileRect.anchoredPosition = targetPos;
        if (duplicateRect != null)
        {
            duplicateRect.anchoredPosition = GetUIPosition(projectileRect.transform);
        }
    }

    private IEnumerator AnimateMovementWithFade(RectTransform projectileRect, RectTransform duplicateRect, 
        Image projectileImage, Image duplicateImage, Vector2 startPos, Vector2 targetPos, 
        Color originalColor, Color duplicateOriginalColor, float fadeAlpha, float moveTime)
    {
        float elapsed = 0f;
        
        while (elapsed < moveTime)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / moveTime;
            
            projectileRect.anchoredPosition = Vector2.Lerp(startPos, targetPos, t);
            
            if (duplicateRect != null)
            {
                duplicateRect.anchoredPosition = GetUIPosition(projectileRect.transform);
                
                if (duplicateImage != null)
                {
                    Color duplicateFadedColor = duplicateOriginalColor;
                    duplicateFadedColor.a = fadeAlpha;
                    duplicateImage.color = duplicateFadedColor;
                }
            }
            
            yield return null;
        }

        projectileRect.anchoredPosition = targetPos;
        if (duplicateRect != null)
        {
            duplicateRect.anchoredPosition = GetUIPosition(projectileRect.transform);
        }
    }

    private void CleanupProjectile(GameObject projectile)
    {
        if (duplicateProjectiles.ContainsKey(projectile))
        {
            GameObject duplicate = duplicateProjectiles[projectile];
            if (duplicate != null) Destroy(duplicate);
            duplicateProjectiles.Remove(projectile);
        }
        
        if (projectile != null) Destroy(projectile);
    }

    private GameObject CreateRocketProjectile(Vector2Int startPos, Vector2Int direction)
    {
        Sprite projectileSprite = GetProjectileSprite(direction);
        
        GameObject projectile = new GameObject("RocketProjectile");
        projectile.transform.SetParent(gridController.GridContainer, false);
        
        SetupProjectileComponents(projectile, projectileSprite, startPos);
        CreateDuplicateProjectile(projectile, projectileSprite, direction);
        
        return projectile;
    }

    private void SetupProjectileComponents(GameObject projectile, Sprite sprite, Vector2Int startPos)
    {
        RectTransform projectileRect = projectile.AddComponent<RectTransform>();
        Image projectileImage = projectile.AddComponent<Image>();
        
        projectileImage.sprite = sprite;
        projectileImage.raycastTarget = false;
        projectileImage.preserveAspect = true;
        projectileImage.color = new Color(1f, 1f, 1f, 0f);
        
        GridCell startCell = gridController.GetCell(startPos.x, startPos.y);
        if (startCell != null)
        {
            RectTransform cellRect = startCell.GetComponent<RectTransform>();
            if (cellRect != null)
            {
                CopyRectTransformProperties(projectileRect, cellRect);
                projectile.transform.SetSiblingIndex(gridController.GridContainer.childCount - 1);
            }
        }
    }

    private void CopyRectTransformProperties(RectTransform target, RectTransform source)
    {
        target.anchorMin = source.anchorMin;
        target.anchorMax = source.anchorMax;
        target.pivot = source.pivot;
        target.sizeDelta = source.sizeDelta;
        target.anchoredPosition = source.anchoredPosition;
    }

    private void CreateDuplicateProjectile(GameObject originalProjectile, Sprite projectileSprite, Vector2Int direction)
    {
        Transform container = ParticleEffectManager.Instance.transform.Find("ParticleContainer");

        GameObject prefab = GetProjectilePrefab(direction);

        GameObject duplicate = Instantiate(prefab, container);
        duplicate.SetActive(true);

        RectTransform dupRect = duplicate.GetComponent<RectTransform>();
        dupRect.anchoredPosition = GetUIPosition(originalProjectile.transform);

        ParticleSystem[] particleSystems = duplicate.GetComponentsInChildren<ParticleSystem>(true);
        foreach (ParticleSystem ps in particleSystems)
        {
            ps.gameObject.SetActive(true);
            ps.Play();
        }

        duplicateProjectiles[originalProjectile] = duplicate;
    }

    private GameObject GetProjectilePrefab(Vector2Int dir)
    {
        if (dir == Vector2Int.right) return projectilePrefabRight;
        if (dir == Vector2Int.left) return projectilePrefabLeft;
        if (dir == Vector2Int.down) return projectilePrefabDown;
        return projectilePrefabUp;
    }

    private Vector2 GetUIPosition(Transform itemTransform)
    {
        Transform particleContainer = ParticleEffectManager.Instance.transform.Find("ParticleContainer");

        RectTransform itemRect = itemTransform.GetComponent<RectTransform>();

        Vector3[] corners = new Vector3[4];
        itemRect.GetWorldCorners(corners);
        
        Vector3 worldCenter = (corners[0] + corners[2]) * 0.5f;
        
        return particleContainer.InverseTransformPoint(worldCenter);
    }

    private Sprite GetProjectileSprite(Vector2Int direction)
    {
        string spritePath = "CaseStudyAssets2025/Rocket/";
        
        if (direction == Vector2Int.right) spritePath += "horizontal_rocket_part_right";
        else if (direction == Vector2Int.left) spritePath += "horizontal_rocket_part_left";
        else if (direction == Vector2Int.up) spritePath += "vertical_rocket_part_bottom";
        else if (direction == Vector2Int.down) spritePath += "vertical_rocket_part_top";
        else return null;
        
        return Resources.Load<Sprite>(spritePath);
    }
    
    public void CleanupAllProjectiles()
    {
        StopAllCoroutines();
        
        List<GameObject> projectilesToClean = new List<GameObject>(duplicateProjectiles.Keys);
        foreach (GameObject projectile in projectilesToClean)
        {
            CleanupProjectile(projectile);
        }
        
        for (int i = gridController.GridContainer.childCount - 1; i >= 0; i--)
        {
            Transform child = gridController.GridContainer.GetChild(i);
            if (child.name == "RocketProjectile")
            {
                Destroy(child.gameObject);
            }
        }
        
        activeProjectiles = 0;
        duplicateProjectiles.Clear();
        ClearAllPhantoms();
    }
}

