using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GridController : MonoBehaviour
{
    public static GridController Instance { get; private set; }

    [Header("Grid Setup")]
    [SerializeField] private GridCell cellPrefab;
    [SerializeField] private Transform gridContainer;
    [SerializeField] private Transform gridBackgroundContainer;
    [SerializeField] private float cellSpacing = 10f;
    [SerializeField] private int bufferRows = 20;
    
    [Header("Grid Constraints")]
    [SerializeField] private int maxGridWidth = 14;
    [SerializeField] private int maxGridHeight = 10;
    [SerializeField] private float paddingLeft = 50f;
    [SerializeField] private float paddingRight = 50f;
    [SerializeField] private float paddingTop = 262f;
    [SerializeField] private float paddingBottom = 50f;
    [SerializeField] private float bufferRowGap = 4f;

    [Header("Item Prefabs")]
    [SerializeField] private GameObject cubePrefab;
    [SerializeField] private GameObject rocketPrefab;
    [SerializeField] private GameObject obstaclePrefab;
    [SerializeField] private float itemSizeMultiplier = 1.2f;

    private GridDataService dataService;
    private GridLayoutService layoutService;
    private RectMask2D gridMask;
    private GridBackground gridBackground;
    private Dictionary<ItemType, GameObject> itemPrefabMap;

    public Transform GridContainer => gridContainer;
    public Transform GridBackgroundContainer => gridBackgroundContainer;
    public GridDataService DataService => dataService;
    public GridLayoutService LayoutService => layoutService;
    public int BufferRows => bufferRows;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }
        
        dataService = new GridDataService();
        layoutService = new GridLayoutService();
        InitializePrefabMap();
        SubscribeToEvents();
    }

    private void OnDestroy()
    {
        UnsubscribeFromEvents();
    }

    private void SubscribeToEvents()
    {
        EventBus.Subscribe<LevelStartedEvent>(HandleLevelStarted);
        EventBus.Subscribe<GravityCompletedEvent>(HandleGravityCompleted);
    }

    private void UnsubscribeFromEvents()
    {
        EventBus.Unsubscribe<LevelStartedEvent>(HandleLevelStarted);
        EventBus.Unsubscribe<GravityCompletedEvent>(HandleGravityCompleted);
    }

    private void InitializePrefabMap()
    {
        itemPrefabMap = new Dictionary<ItemType, GameObject>
        {
            { ItemType.RedCube, cubePrefab },
            { ItemType.GreenCube, cubePrefab },
            { ItemType.BlueCube, cubePrefab },
            { ItemType.YellowCube, cubePrefab },
            { ItemType.HorizontalRocket, rocketPrefab },
            { ItemType.VerticalRocket, rocketPrefab },
            { ItemType.Box, obstaclePrefab },
            { ItemType.Stone, obstaclePrefab },
            { ItemType.Vase, obstaclePrefab }
        };
    }

    private void HandleLevelStarted(LevelStartedEvent evt)
    {
        if (TransitionController.Instance != null && TransitionController.Instance.IsFading)
        {
            StartCoroutine(WaitForFadeAndInitialize(evt.LevelNumber));
        }
        else
        {
            InitializeGridForLevel(evt.LevelNumber);
        }
    }

    private IEnumerator WaitForFadeAndInitialize(int levelNumber)
    {
        bool fadeComplete = false;
        System.Action onFadeComplete = () => fadeComplete = true;
        TransitionController.OnFadeInComplete.AddListener(onFadeComplete.Invoke);
        
        while (!fadeComplete && TransitionController.Instance.IsFading)
        {
            yield return null;
        }
        
        TransitionController.OnFadeInComplete.RemoveListener(onFadeComplete.Invoke);
        yield return new WaitForSeconds(0.1f);
        
        InitializeGridForLevel(levelNumber);
    }

    private void InitializeGridForLevel(int levelNumber)
    {
        LevelData levelData = LevelManager.Instance.GetLevelData(levelNumber);
        if (levelData != null)
        {
            InitializeGrid(levelData);
        }
    }

    public void InitializeGrid(LevelData levelData)
    {
        ClearGrid();
        
        dataService.Initialize(levelData.grid_width, levelData.grid_height, bufferRows);
        layoutService.Initialize(cellSpacing, paddingLeft, paddingRight, paddingTop, paddingBottom, bufferRowGap, maxGridWidth, maxGridHeight);
        layoutService.CalculateCellSize(gridContainer, dataService.GridWidth, dataService.GridHeight);
        
        CreateExtendedGrid();
        PopulateVisibleGrid(levelData.grid);
        PopulateBufferRows();
        SetupGridMask();
        SetupGridBackground();
        layoutService.CenterGrid(gridContainer, dataService.GridWidth, dataService.GridHeight);
        
        gridBackground.UpdateBackgroundTransform();
        
        EventBus.Publish(new GridInitializedEvent
        {
            GridWidth = dataService.GridWidth,
            GridHeight = dataService.GridHeight
        });
        
        MatchController matchController = FindFirstObjectByType<MatchController>();
        if (matchController != null)
        {
            matchController.UpdateRocketHints();
        }
    }

    private void ClearGrid()
    {
        if (dataService.Grid != null)
        {
            for (int x = 0; x < dataService.Grid.GetLength(0); x++)
            {
                for (int y = 0; y < dataService.Grid.GetLength(1); y++)
                {
                    if (dataService.Grid[x, y] != null)
                    {
                        DestroyImmediate(dataService.Grid[x, y].gameObject);
                    }
                }
            }
        }
        
        for (int i = gridContainer.childCount - 1; i >= 0; i--)
        {
            DestroyImmediate(gridContainer.GetChild(i).gameObject);
        }
        
        if (gridMask != null)
        {
            DestroyImmediate(gridMask);
            gridMask = null;
        }
        
        dataService.Clear();
    }

    private void CreateExtendedGrid()
    {
        for (int x = 0; x < dataService.GridWidth; x++)
        {
            for (int y = 0; y < dataService.TotalHeight; y++)
            {
                GameObject cellObj = Instantiate(cellPrefab.gameObject, gridContainer);
                GridCell cell = cellObj.GetComponent<GridCell>();
                
                RectTransform cellRect = cellObj.GetComponent<RectTransform>();
                layoutService.PositionCell(cellRect, x, y, bufferRows);
                
                cell.Initialize(x, y);
                dataService.SetCell(x, y, cell);
            }
        }
    }

    private void PopulateVisibleGrid(string[] gridData)
    {
        for (int i = 0; i < gridData.Length && i < dataService.GridWidth * dataService.GridHeight; i++)
        {
            int x = i % dataService.GridWidth;
            int jsonY = i / dataService.GridWidth;
            int gridY = (dataService.GridHeight - 1) - jsonY + bufferRows;
            
            string itemString = gridData[i];
            ItemType itemType = ParseItemType(itemString);
            
            if (itemType != ItemType.Empty)
            {
                SpawnItemInExtendedGrid(itemType, x, gridY);
            }
        }
    }

    private void PopulateBufferRows()
    {
        for (int x = 0; x < dataService.GridWidth; x++)
        {
            for (int y = 0; y < bufferRows; y++)
            {
                ItemType randomCube = GetRandomCubeType();
                SpawnItemInExtendedGrid(randomCube, x, y);
            }
        }
    }

    private void SetupGridMask()
    {
        layoutService.SetupGridMask(gridContainer, dataService.GridHeight);
        gridMask = gridContainer.gameObject.GetComponent<RectMask2D>();
    }

    private void SetupGridBackground()
    {
        gridBackground = gridBackgroundContainer.GetComponentInChildren<GridBackground>();
        if (gridBackground != null)
        {
            gridBackground.InitializeBackground(gridContainer);
        }
    }

    public BaseItem SpawnItem(ItemType itemType, int x, int y)
    {
        int extendedY = y + bufferRows;
        return SpawnItemInExtendedGrid(itemType, x, extendedY);
    }

    public BaseItem SpawnItemInExtendedGrid(ItemType itemType, int x, int y)
    {
        if (!GridValidator.IsValidExtendedPosition(x, y, dataService.GridWidth, dataService.TotalHeight))
        {
            return null;
        }
        
        if (!itemPrefabMap.ContainsKey(itemType))
        {
            return null;
        }
        
        GridCell cell = dataService.GetExtendedCell(x, y);
        if (!GridValidator.CanSpawnItem(cell))
        {
            return null;
        }
        
        if (cell.currentItem?.IsPhantom() == true)
        {
            Destroy(cell.currentItem.gameObject);
            cell.RemoveItem();
        }
        
        BaseItem item = PoolManager.Instance.GetItem(itemType, gridContainer);
        
        RectTransform itemRect = item.GetComponent<RectTransform>();
        RectTransform cellRect = cell.GetComponent<RectTransform>();
        
        itemRect.anchorMin = cellRect.anchorMin;
        itemRect.anchorMax = cellRect.anchorMax;
        itemRect.anchoredPosition = cellRect.anchoredPosition;
        itemRect.sizeDelta = cellRect.sizeDelta * GetScaleForItemType(itemType);
        
        item.Initialize(itemType);
        cell.SetItem(item);
        
        UpdateItemSiblingOrder(item);
        
        EventBus.Publish(new ItemSpawnedEvent
        {
            GridX = x,
            GridY = y - bufferRows,
            ItemType = itemType
        });
        
        return item;
    }

    public void SpawnNewCubes()
    {
        for (int x = 0; x < dataService.GridWidth; x++)
        {
            for (int y = 0; y < bufferRows; y++)
            {
                GridCell cell = dataService.GetExtendedCell(x, y);
                if (cell != null && cell.IsEmpty())
                {
                    ItemType randomCube = GetRandomCubeType();
                    SpawnItemInExtendedGrid(randomCube, x, y);
                }
            }
        }
    }

    private void HandleGravityCompleted(GravityCompletedEvent evt)
    {
        SpawnNewCubes();
        EventBus.Publish(new GridUpdatedEvent());
    }

    public float GetScaleForItemType(ItemType type)
    {
        switch (type)
        {
            case ItemType.RedCube:
            case ItemType.GreenCube:
            case ItemType.BlueCube:
            case ItemType.YellowCube:
            case ItemType.Box:
            case ItemType.Stone:
                return itemSizeMultiplier;
            default:
                return 1f;
        }
    }

    public void UpdateItemSiblingOrder(BaseItem item)
    {
        if (item.currentCell == null) return;
        
        int cellCount = dataService.GridWidth * dataService.TotalHeight;
        int rowBasedIndex = (dataService.TotalHeight - item.currentCell.y - 1) * dataService.GridWidth + item.currentCell.x;
        int desiredIndex = cellCount + rowBasedIndex;

        desiredIndex = Mathf.Min(desiredIndex, gridContainer.childCount - 1);
        item.transform.SetSiblingIndex(desiredIndex);
    }

    public ItemType ParseItemType(string itemString)
    {
        switch (itemString.ToLower())
        {
            case "r": return ItemType.RedCube;
            case "g": return ItemType.GreenCube;
            case "b": return ItemType.BlueCube;
            case "y": return ItemType.YellowCube;
            case "rand": return GetRandomCubeType();
            case "vro": return ItemType.VerticalRocket;
            case "hro": return ItemType.HorizontalRocket;
            case "bo": return ItemType.Box;
            case "s": return ItemType.Stone;
            case "v": return ItemType.Vase;
            default: return ItemType.Empty;
        }
    }

    public ItemType GetRandomCubeType()
    {
        ItemType[] cubeTypes = { ItemType.RedCube, ItemType.GreenCube, ItemType.BlueCube, ItemType.YellowCube };
        return cubeTypes[Random.Range(0, cubeTypes.Length)];
    }

    public GridCell GetCell(int x, int y)
    {
        return dataService.GetCell(x, y);
    }

    public BaseItem GetItem(int x, int y)
    {
        return dataService.GetItem(x, y);
    }

    public bool IsValidPosition(int x, int y)
    {
        return dataService.IsValidPosition(x, y);
    }

    public Vector2Int GetGridSize()
    {
        return dataService.GetGridSize();
    }

    public List<GridCell> GetAdjacentCells(int x, int y)
    {
        return dataService.GetAdjacentCells(x, y);
    }
}

