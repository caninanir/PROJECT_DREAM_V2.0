using UnityEngine;
using UnityEngine.SceneManagement;

public class UIController : MonoBehaviour
{
    public static UIController Instance { get; private set; }

    private GameplayUIController gameplayUI;
    private MenuUIController menuUI;
    private PopupController popupController;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            MarkParentAsDontDestroy();
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    private void MarkParentAsDontDestroy()
    {
        if (transform.parent != null)
        {
            DontDestroyOnLoad(transform.parent.gameObject);
        }
        else
        {
            DontDestroyOnLoad(gameObject);
        }
    }

    private void Start()
    {
        InitializeControllers();
        SubscribeToEvents();
    }

    private void OnDestroy()
    {
        UnsubscribeFromEvents();
    }

    private void InitializeControllers()
    {
        gameplayUI = FindFirstObjectByType<GameplayUIController>();
        menuUI = FindFirstObjectByType<MenuUIController>();
        popupController = FindFirstObjectByType<PopupController>();
    }

    private void SubscribeToEvents()
    {
        EventBus.Subscribe<GameStateChangedEvent>(HandleGameStateChanged);
    }

    private void UnsubscribeFromEvents()
    {
        EventBus.Unsubscribe<GameStateChangedEvent>(HandleGameStateChanged);
    }

    private void HandleGameStateChanged(GameStateChangedEvent evt)
    {
        StartCoroutine(HandleGameStateChangedDelayed(evt));
    }

    private System.Collections.IEnumerator HandleGameStateChangedDelayed(GameStateChangedEvent evt)
    {
        yield return new WaitForEndOfFrame();
        
        string currentScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        bool isMainScene = currentScene == "MainScene";
        bool isLevelScene = currentScene == "LevelScene";
        
        switch (evt.NewState)
        {
            case GameState.MainMenu:
                if (isMainScene)
                {
                    menuUI = FindFirstObjectByType<MenuUIController>();
                    menuUI.gameObject.SetActive(true);
                }
                if (isLevelScene)
                {
                    gameplayUI = FindFirstObjectByType<GameplayUIController>();
                    if (gameplayUI != null)
                    {
                        gameplayUI.gameObject.SetActive(false);
                    }
                }
                break;
            case GameState.Playing:
                if (isLevelScene)
                {
                    gameplayUI = FindFirstObjectByType<GameplayUIController>();
                    if (gameplayUI != null)
                    {
                        gameplayUI.gameObject.SetActive(true);
                    }
                }
                if (isMainScene)
                {
                    menuUI = FindFirstObjectByType<MenuUIController>();
                    if (menuUI != null)
                    {
                        menuUI.gameObject.SetActive(false);
                    }
                }
                break;
            case GameState.GameWon:
            case GameState.GameLost:
                if (isLevelScene)
                {
                    gameplayUI = FindFirstObjectByType<GameplayUIController>();
                    if (gameplayUI != null)
                    {
                        gameplayUI.gameObject.SetActive(false);
                    }
                }
                break;
        }
    }

    private void ShowGameplayUI()
    {
        gameplayUI = FindFirstObjectByType<GameplayUIController>();
        gameplayUI.gameObject.SetActive(true);
    }

    private void HideGameplayUI()
    {
        gameplayUI = FindFirstObjectByType<GameplayUIController>();
        gameplayUI.gameObject.SetActive(false);
    }

    private void ShowMenuUI()
    {
        menuUI = FindFirstObjectByType<MenuUIController>();
        menuUI.gameObject.SetActive(true);
    }

    private void HideMenuUI()
    {
        menuUI = FindFirstObjectByType<MenuUIController>();
        menuUI.gameObject.SetActive(false);
    }
}

