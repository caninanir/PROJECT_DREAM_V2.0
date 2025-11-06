using UnityEngine;

public class GameStateController : MonoBehaviour
{
    public static GameStateController Instance { get; private set; }
    
    [SerializeField] private GameConfig config;
    
    private GameState currentState = GameState.MainMenu;
    private int currentLevel = 1;
    private int movesRemaining = 0;
    private bool isProcessingMove = false;

    public GameState CurrentState => currentState;
    public int CurrentLevel => currentLevel;
    public int MovesRemaining => movesRemaining;
    public bool IsProcessingMove => isProcessingMove;
    public GameConfig Config => config;

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
        InitializeGameState();
    }

    private void InitializeGameState()
    {
        currentLevel = SaveManager.Instance.GetCurrentLevel();
        
        bool allLevelsCompleted = SaveManager.Instance.AreAllLevelsCompleted();
        
        if (allLevelsCompleted)
        {
            ChangeGameState(GameState.Finished);
        }
        else
        {
            if (!LevelManager.Instance.IsValidLevel(currentLevel))
            {
                int firstLevel = LevelManager.Instance.GetFirstLevel();
                SaveManager.Instance.SetCurrentLevel(firstLevel);
                currentLevel = firstLevel;
            }
            
            ChangeGameState(GameState.MainMenu);
        }
    }

    public void ChangeGameState(GameState newState)
    {
        if (currentState != newState)
        {
            GameState previousState = currentState;
            currentState = newState;
            
            EventBus.Publish(new GameStateChangedEvent
            {
                PreviousState = previousState,
                NewState = newState
            });
        }
    }

    public void StartLevel(int levelNumber)
    {
        CleanupVisualEffects();
        
        LevelData levelData = LevelManager.Instance.GetLevelData(levelNumber);
        
        currentLevel = levelNumber;
        movesRemaining = levelData.move_count;
        isProcessingMove = false;
        
        LevelManager.Instance.SetCurrentLevel(levelNumber);
        
        ChangeGameState(GameState.Playing);
        
        EventBus.Publish(new LevelStartedEvent { LevelNumber = currentLevel });
        EventBus.Publish(new MovesChangedEvent { MovesRemaining = movesRemaining });
    }
    
    private void CleanupVisualEffects()
    {
        RocketProjectileService projectileService = FindFirstObjectByType<RocketProjectileService>();
        if (projectileService != null)
        {
            projectileService.CleanupAllProjectiles();
        }
        
        ParticleEffectManager.Instance.CleanupAllParticles();
    }

    public void UseMove()
    {
        if (currentState != GameState.Playing)
        {
            return;
        }
        
        movesRemaining = Mathf.Max(0, movesRemaining - 1);
        
        EventBus.Publish(new MovesChangedEvent { MovesRemaining = movesRemaining });
        
        if (movesRemaining <= 0 && !CheckWinCondition())
        {
        }
    }

    public bool CheckWinCondition()
    {
        return ObstacleController.Instance.AreAllObstaclesCleared();
    }

    public bool CheckLoseCondition()
    {
        return movesRemaining <= 0 && currentState == GameState.Playing && !CheckWinCondition();
    }

    public void WinLevel()
    {
        if (currentState != GameState.Playing)
        {
            return;
        }

        MusicManager.Instance.PlayEndGameMusic(true);
        AudioManager.Instance.PlayGameWonSoundDelayed();

        SaveManager.Instance.MarkLevelCompleted(currentLevel);
        bool finished = SaveManager.Instance.AreAllLevelsCompleted();

        ChangeGameState(finished ? GameState.Finished : GameState.GameWon);

        EventBus.Publish(new LevelWonEvent { LevelNumber = currentLevel });
    }

    public void LoseLevel()
    {
        if (currentState != GameState.Playing)
        {
            return;
        }

        MusicManager.Instance.PlayEndGameMusic(false);
        AudioManager.Instance.PlayGameLostSoundDelayed();
        
        ChangeGameState(GameState.GameLost);
        EventBus.Publish(new LevelLostEvent { LevelNumber = currentLevel });
    }

    public void RestartLevel()
    {
        StartLevel(currentLevel);
    }

    public void NextLevel()
    {
        int nextLevel = LevelManager.Instance.GetNextLevelAfter(currentLevel);
        if (nextLevel != -1)
        {
            StartLevel(nextLevel);
        }
        else
        {
            if (SaveManager.Instance.AreAllLevelsCompleted())
            {
                ChangeGameState(GameState.Finished);
            }
            
            ReturnToMainMenu();
        }
    }

    public void ReturnToMainMenu()
    {
        if (currentState != GameState.Finished)
        {
            ChangeGameState(GameState.MainMenu);
        }
        
        SceneTransitionManager.Instance.LoadMainScene();
    }

    public void SetProcessingMove(bool processing)
    {
        isProcessingMove = processing;
    }

    public bool IsPlaying()
    {
        return currentState == GameState.Playing;
    }
}

