# Match to POP - Complete Technical Analysis Report

**Generated:** 2025  
**Unity Version:** 2019+ (MonoBehaviour lifecycle)  
**Project Path:** `Assets/Scripts/`

---

## Executive Summary

"Match to POP" is a match-3 puzzle game built in Unity using C#. The architecture follows an event-driven, service-oriented pattern with clear separation between gameplay logic, presentation, and data management. The game uses a singleton-based manager system with an EventBus for decoupled communication, object pooling for performance, and coroutine-based animations for visual feedback.

---

## 1. High-Level Architecture

### 1.1 Architecture Summary

The game uses a **layered, event-driven architecture** with the following structure:
- **Core Layer**: State management (`GameStateController`), event system (`EventBus`), configuration (`ConfigurationManager`)
- **Data Layer**: Level data (`LevelData`), save system (`SaveData`, `SaveManager`), game config (`GameConfig` ScriptableObject)
- **Gameplay Layer**: Grid management (`GridController`), matching (`MatchController`), input (`InputController`), gravity (`GravityController`), items (cubes, rockets, obstacles)
- **Presentation Layer**: UI controllers (`GameplayUIController`, `MenuUIController`), effects (`ParticleEffectManager`, `TransitionController`), animations (`FallAnimator`, `RocketAnimator`)
- **Services Layer**: Pooling (`PoolManager`), audio (`AudioManager`, `MusicManager`), scene transitions (`SceneTransitionManager`)

### 1.2 Major Subsystems

#### **Game/Level Management**
- **`Assets/Scripts/Core/GameStateController.cs`** — Central state machine; manages `GameState` enum (MainMenu, Playing, GameWon, GameLost, Finished, Paused); tracks current level, moves remaining, processing flags
- **`Assets/Scripts/Managers/LevelManager.cs`** — Loads level JSON files from Resources; maintains dictionary of all levels; provides level data access
- **`Assets/Scripts/Managers/SaveManager.cs`** — Persists progress via `PlayerPrefs`; tracks completed levels; manages current level progression

#### **Board/Tile System**
- **`Assets/Scripts/Gameplay/Grid/GridController.cs`** — Main grid orchestrator; spawns cells and items; manages extended grid (visible + buffer rows); handles item positioning and sibling ordering
- **`Assets/Scripts/Grid/GridCell.cs`** — Individual cell container; stores reference to `BaseItem`; provides position data
- **`Assets/Scripts/Gameplay/Grid/Services/GridDataService.cs`** — Data layer for grid; maintains 2D array of `GridCell[,]`; handles coordinate conversion (visible ↔ extended)
- **`Assets/Scripts/Gameplay/Grid/Services/GridLayoutService.cs`** — Calculates cell sizes, positions; handles grid centering and masking

#### **Match-Detection**
- **`Assets/Scripts/Gameplay/Matching/MatchController.cs`** — Orchestrates match processing; listens to `CubeTappedEvent`; handles match validation, rocket creation, obstacle damage
- **`Assets/Scripts/Gameplay/Matching/MatchDetectorService.cs`** — Flood-fill algorithm to find connected matching cubes; recursive neighbor traversal
- **`Assets/Scripts/Gameplay/Matching/MatchValidator.cs`** — Static validation: `IsValidMatch()` (≥2 cubes), `CanCreateRocket()` (≥4 cubes)

#### **Input**
- **`Assets/Scripts/Gameplay/Input/InputController.cs`** — Singleton input manager; processes mouse/touch in `Update()`; validates input state; publishes `CubeTappedEvent` or `RocketTappedEvent`
- **`Assets/Scripts/Gameplay/Input/InputHandler.cs`** — Raycast handler using `GraphicRaycaster`; finds `BaseItem` under pointer
- **`Assets/Scripts/Gameplay/Input/InputValidator.cs`** — Static validation: checks game state, processing flags, input lock

#### **UI**
- **`Assets/Scripts/Presentation/UI/GameplayUIController.cs`** — Updates moves counter, level display, goal items; listens to `LevelStartedEvent`, `MovesChangedEvent`, `GoalUpdatedEvent`
- **`Assets/Scripts/Presentation/UI/MenuUIController.cs`** — Main menu UI management
- **`Assets/Scripts/Presentation/UI/GoalDisplayController.cs`** — Manages goal item UI layout

#### **Audio**
- **`Assets/Scripts/Audio/AudioManager.cs`** — Singleton; pools `AudioSource` components (20 default); plays cube break, rocket, obstacle sounds with random pitch variation
- **`Assets/Scripts/Audio/MusicManager.cs`** — Background music management

#### **Animation/FX**
- **`Assets/Scripts/Gameplay/Gravity/FallAnimator.cs`** — Animates item falling using `AnimationCurve`; supports landing bounce, subtle rotation
- **`Assets/Scripts/Gameplay/Items/Rockets/RocketAnimator.cs`** — Animates rocket creation (cubes gather to center, 0.4s duration)
- **`Assets/Scripts/Effects/ParticleEffectManager.cs`** — Spawns particle effects for cube/obstacle destruction; uses pooled `ParticleElement` objects
- **`Assets/Scripts/Presentation/Effects/TransitionController.cs`** — Scene fade transitions (fadeIn: 0.8s, fadeOut: 0.6s)

#### **Persistence/Save**
- **`Assets/Scripts/Data/SaveData.cs`** — Serializable class; stores `currentLevel` (int) and `levelCompleted` (bool[999]); saves to `PlayerPrefs` as JSON
- **`Assets/Scripts/Managers/SaveManager.cs`** — Wraps `SaveData`; provides `LoadSave()`, `SaveGame()`, `MarkLevelCompleted()`, `AreAllLevelsCompleted()`

#### **Asset Config (ScriptableObjects)**
- **`Assets/Scripts/Data/GameConfig.cs`** — ScriptableObject; configures cube size, grid spacing, animation durations (fallSpeed: 5f, explosionDelay: 0.2f, rocketCreationDuration: 0.5f), animation curves

---

## 2. Design Patterns & Technical Constructs

### 2.1 Singleton Pattern
**Files:** Multiple managers use singleton pattern
- **Implementation:** `public static Instance { get; private set; }` with `Awake()` check
- **Examples:**
  - `GameStateController.cs:5` — `public static GameStateController Instance`
  - `GridController.cs:8` — `public static GridController Instance`
  - `InputController.cs:8` — `public static InputController Instance`
  - `PoolManager.cs:7` — `public static PoolManager Instance`
  - `AudioManager.cs:7` — `public static AudioManager Instance`
  - `RocketController.cs:7` — `public static RocketController Instance`
  - `ObstacleController.cs:6` — `public static ObstacleController Instance`
- **Pattern:** All use `DontDestroyOnLoad()` to persist across scenes

### 2.2 Event Bus / Observer Pattern
**File:** `Assets/Scripts/Core/Events/EventBus.cs`
- **Implementation:** Static generic event bus using `Dictionary<Type, List<object>>`
- **Methods:**
  - `Subscribe<T>(Action<T> handler)` — Register handler for event type
  - `Unsubscribe<T>(Action<T> handler)` — Remove handler
  - `Publish<T>(T eventData)` — Invoke all handlers for event type
- **Event Types:** Defined in `Assets/Scripts/Core/Events/GameEvents.cs`:
  - `GameStateChangedEvent`, `LevelStartedEvent`, `LevelWonEvent`, `LevelLostEvent`
  - `CubeTappedEvent`, `RocketTappedEvent`, `MatchFoundEvent`, `MatchProcessedEvent`
  - `GravityStartedEvent`, `GravityCompletedEvent`, `GridInitializedEvent`, `GridUpdatedEvent`
  - `ItemSpawnedEvent`, `ItemDestroyedEvent`, `ItemDamagedEvent`
  - `ObstacleDestroyedEvent`, `GoalUpdatedEvent`, `MovesChangedEvent`
- **Usage Example:**
```csharp
// Publishing
EventBus.Publish(new CubeTappedEvent { GridX = x, GridY = y, CubeType = type });

// Subscribing
EventBus.Subscribe<CubeTappedEvent>(HandleCubeTapped);
```

### 2.3 Service Locator Pattern
**Files:** Multiple services accessed via static `Instance` properties
- Services are located through singleton instances rather than dependency injection
- Example: `GridController.Instance`, `GameStateController.Instance`, `PoolManager.Instance`

### 2.4 ScriptableObjects
**File:** `Assets/Scripts/Data/GameConfig.cs`
- **Usage:** Configuration data asset (`[CreateAssetMenu]`)
- **Fields:** Grid settings, animation durations, animation curves
- **Access:** Via `ConfigurationManager.Instance.GameConfig` or `GameStateController.Instance.Config`

### 2.5 State Machine
**File:** `Assets/Scripts/Core/GameStateController.cs`
- **States:** `GameState` enum (MainMenu, Playing, GameWon, GameLost, Finished, Paused)
- **Transition:** `ChangeGameState(GameState newState)` publishes `GameStateChangedEvent`
- **State checks:** `IsPlaying()`, `IsProcessingMove`, `CheckWinCondition()`, `CheckLoseCondition()`

### 2.6 Coroutines
**Extensive use throughout for async operations:**
- **`MatchController.cs:50`** — `ProcessCubeTap()` coroutine handles match processing
- **`GravityController.cs:59`** — `ProcessGravity()` coroutine handles falling items
- **`FallAnimator.cs:42`** — `AnimateFalls()` coroutine animates multiple items falling
- **`RocketAnimator.cs:12`** — `AnimateRocketCreation()` coroutine (0.4s duration)
- **`SceneTransitionManager.cs:65`** — `LoadSceneWithTransition()` coroutine handles async scene loading
- **`AudioManager.cs:171`** — `PlaySoundDelayed()` coroutine for delayed audio playback

### 2.7 Update() Loops
**Files with Update() methods:**
- **`InputController.cs:76`** — `HandleInput()` called every frame; checks input state, processes taps
- **`AudioManager.cs:118`** — Cleans up finished `AudioSource` components
- **`ParticleEffectManager.cs:53`** — Updates all active particles (`UpdateParticles()`)

### 2.8 Object Pooling
**Files:** `Assets/Scripts/Services/Pooling/`
- **`GenericPool.cs`** — Generic pool implementation using `Queue<T>`
- **`PoolManager.cs`** — Manages pools for cubes (4 types, 50 each), rockets (20), obstacles (30), particles (100)
- **`IPoolable.cs`** — Interface with `OnSpawn()`, `OnDespawn()`, `OnReturnToPool()`
- **Usage:**
```csharp
BaseItem item = PoolManager.Instance.GetItem(ItemType.RedCube, parent);
PoolManager.Instance.ReturnItem(item);
```

### 2.9 Factory Pattern
**File:** `Assets/Scripts/Gameplay/Items/Obstacles/ObstacleBehaviorFactory.cs`
- Creates obstacle behavior components based on `ItemType`
- Returns `ObstacleBehavior` implementations (BoxBehavior, StoneBehavior, VaseBehavior)

### 2.10 Strategy Pattern
**File:** `Assets/Scripts/Gameplay/Items/Obstacles/ObstacleBehavior.cs`
- Abstract base class for obstacle behaviors
- Different behaviors handle damage, sprites, fall capability differently

---

## 3. Dependency and Interaction Map

### 3.1 Central Hubs

#### **GameStateController** (`Assets/Scripts/Core/GameStateController.cs`)
**Dependencies:**
- `SaveManager.Instance` — Get/set current level
- `LevelManager.Instance` — Get level data, validate levels
- `EventBus` — Publishes `GameStateChangedEvent`, `LevelStartedEvent`, `MovesChangedEvent`, `LevelWonEvent`, `LevelLostEvent`
- `RocketProjectileService` — Cleanup on level start
- `ParticleEffectManager.Instance` — Cleanup on level start
- `MusicManager.Instance` — Play end game music
- `AudioManager.Instance` — Play win/lose sounds
- `ObstacleController.Instance` — Check win condition

**Dependents:**
- `InputController` — Checks `CurrentState`, `IsProcessingMove`
- `MatchController` — Checks `IsProcessingMove`, calls `UseMove()`, `SetProcessingMove()`
- `RocketController` — Checks `IsProcessingMove`, calls `UseMove()`, `SetProcessingMove()`
- `SceneTransitionManager` — Calls `StartLevel()`
- `UI Controllers` — Listen to state change events

#### **GridController** (`Assets/Scripts/Gameplay/Grid/GridController.cs`)
**Dependencies:**
- `PoolManager.Instance` — Get/return items
- `EventBus` — Publishes `GridInitializedEvent`, `ItemSpawnedEvent`, `GridUpdatedEvent`
- `LevelManager.Instance` — Get level data
- `TransitionController.Instance` — Check fade state
- `MatchController` — Update rocket hints

**Dependents:**
- `MatchController` — Get cells, spawn items
- `GravityController` — Access grid data service
- `RocketController` — Get items, check positions
- `InputController` — Get grid container for canvas reference
- `RocketService` — Get items, check positions
- `MatchDetectorService` — Get cells, adjacent cells

#### **EventBus** (`Assets/Scripts/Core/Events/EventBus.cs`)
**Dependencies:** None (static class)

**Dependents:** Almost all systems subscribe/publish:
- `GameStateController` — Publishes state/level events
- `GridController` — Publishes grid/item events
- `MatchController` — Subscribes to `CubeTappedEvent`, `GridUpdatedEvent`; publishes `MatchProcessedEvent`
- `GravityController` — Subscribes to `GravityStartedEvent`, `MatchProcessedEvent`; publishes `GravityCompletedEvent`
- `RocketController` — Subscribes to `RocketTappedEvent`; publishes `RocketExplodedEvent`
- `InputController` — Publishes `CubeTappedEvent`, `RocketTappedEvent`
- `ObstacleController` — Subscribes to `LevelStartedEvent`, `ObstacleDestroyedEvent`; publishes `GoalUpdatedEvent`
- `GameplayUIController` — Subscribes to `LevelStartedEvent`, `MovesChangedEvent`, `GoalUpdatedEvent`

### 3.2 Dependency Matrix

| Class | Depends On | Publishes Events | Subscribes To Events |
|-------|------------|------------------|---------------------|
| `GameStateController` | `SaveManager`, `LevelManager`, `ObstacleController` | `GameStateChangedEvent`, `LevelStartedEvent`, `MovesChangedEvent`, `LevelWonEvent`, `LevelLostEvent` | None |
| `GridController` | `PoolManager`, `LevelManager` | `GridInitializedEvent`, `ItemSpawnedEvent`, `GridUpdatedEvent` | `LevelStartedEvent`, `GravityCompletedEvent` |
| `MatchController` | `GridController`, `GameStateController` | `MatchProcessedEvent` | `CubeTappedEvent`, `GridUpdatedEvent` |
| `GravityController` | `GridController` | `GravityCompletedEvent` | `GravityStartedEvent`, `MatchProcessedEvent` |
| `RocketController` | `GridController`, `GameStateController` | `RocketExplodedEvent` | `RocketTappedEvent` |
| `InputController` | `GameStateController` | `CubeTappedEvent`, `RocketTappedEvent` | `GameStateChangedEvent` |
| `ObstacleController` | `LevelManager`, `GameStateController` | `GoalUpdatedEvent` | `LevelStartedEvent`, `ObstacleDestroyedEvent` |
| `GameplayUIController` | `LevelManager` | None | `LevelStartedEvent`, `MovesChangedEvent`, `GoalUpdatedEvent` |

---

## 4. Runtime Sequences

### 4.1 Game Startup / Application Launch

**Sequence:**
1. **Unity Awake() phase:**
   - `GameStateController.Awake()` — Sets singleton instance, marks DontDestroyOnLoad
   - `LevelManager.Awake()` — Sets singleton, calls `LoadAllLevels()` (scans `Assets/Resources/CaseStudyAssets2025/Levels/` for `level_*.json` files)
   - `SaveManager.Awake()` — Sets singleton, calls `LoadSave()` (loads from `PlayerPrefs`)
   - `PoolManager.Awake()` — Sets singleton, initializes pools (cubes, rockets, obstacles, particles)
   - `AudioManager.Awake()` — Sets singleton, initializes audio source pool (20 sources)
   - `ConfigurationManager.Awake()` — Sets singleton, loads `GameConfig` ScriptableObject

2. **Unity Start() phase:**
   - `GameStateController.Start()` — Calls `InitializeGameState()`
     - Gets current level from `SaveManager.Instance.GetCurrentLevel()`
     - Checks if all levels completed → sets state to `Finished`
     - Otherwise validates level → sets state to `MainMenu`
     - Publishes `GameStateChangedEvent`

3. **Scene Loading:**
   - Main menu scene loads
   - UI controllers initialize and subscribe to events

**Timing:** Synchronous initialization, no delays

### 4.2 Loading and Starting a Level

**Sequence:**
1. **User triggers level start** (e.g., button click)
   - `SceneTransitionManager.LoadLevelScene(levelNumber)` called

2. **Scene transition:**
   - `TransitionController.FadeOut()` — 0.6s fade
   - `SceneManager.LoadSceneAsync("LevelScene")` — Async scene load
   - `TransitionController.FadeIn()` — 0.8s fade
   - `GameStateController.StartLevel(levelNumber)` called

3. **Level initialization:**
   - `GameStateController.StartLevel()`:
     - Cleans up visual effects (`RocketProjectileService.CleanupAllProjectiles()`, `ParticleEffectManager.CleanupAllParticles()`)
     - Gets `LevelData` from `LevelManager.Instance.GetLevelData(levelNumber)`
     - Sets `currentLevel`, `movesRemaining = levelData.move_count`
     - Sets `isProcessingMove = false`
     - Calls `LevelManager.Instance.SetCurrentLevel(levelNumber)`
     - Changes state to `GameState.Playing`
     - Publishes `LevelStartedEvent { LevelNumber }`
     - Publishes `MovesChangedEvent { MovesRemaining }`

4. **Grid initialization** (triggered by `LevelStartedEvent`):
   - `GridController.HandleLevelStarted()`:
     - If fade active, waits for fade completion (`WaitForFadeAndInitialize()` coroutine)
     - Otherwise calls `InitializeGridForLevel()`

5. **Grid setup:**
   - `GridController.InitializeGrid()`:
     - `ClearGrid()` — Destroys existing cells/items
     - `dataService.Initialize(width, height, bufferRows)` — Creates `GridCell[width, height + bufferRows]`
     - `layoutService.Initialize()` — Calculates cell sizes based on screen bounds
     - `CreateExtendedGrid()` — Instantiates `GridCell` prefabs, positions them
     - `PopulateVisibleGrid(levelData.grid)` — Parses JSON grid array, spawns items
     - `PopulateBufferRows()` — Fills top buffer rows with random cubes
     - `SetupGridMask()` — Adds `RectMask2D` to hide buffer rows
     - `SetupGridBackground()` — Initializes background
     - `layoutService.CenterGrid()` — Centers grid on screen
     - Publishes `GridInitializedEvent { GridWidth, GridHeight }`

6. **Obstacle goals initialization:**
   - `ObstacleController.HandleLevelStarted()`:
     - Calls `InitializeGoals(levelData)`
     - Calculates obstacle counts from grid
     - Publishes `GoalUpdatedEvent` for each obstacle type

7. **UI initialization:**
   - `GameplayUIController.HandleLevelStarted()`:
     - Updates level display text
     - Updates moves display
     - Sets up goal items UI (delayed by `WaitForEndOfFrame`)

**Timing:**
- Fade out: 0.6s
- Scene load: Async (varies)
- Fade in: 0.8s
- Grid initialization: Synchronous (fast)
- UI setup: One frame delay

### 4.3 Typical Player Action — Tap Cube That Produces Match

**Sequence:**
1. **Input detection:**
   - `InputController.Update()` — Checks input state via `InputValidator.CanProcessInput()`
   - `Input.GetMouseButtonDown(0)` or `TouchPhase.Began` detected
   - `InputController.ProcessTap(screenPosition)` called

2. **Raycast:**
   - `InputHandler.GetTappedItem(screenPosition)` — Uses `GraphicRaycaster.Raycast()`
   - Returns `BaseItem` (assumed `CubeItem`)

3. **Event publishing:**
   - `InputController.HandleItemTapped(cube)`:
     - Gets grid position via `cube.GetGridPosition()`
     - Publishes `CubeTappedEvent { GridX, GridY, CubeType }`

4. **Match processing:**
   - `MatchController.HandleCubeTapped(CubeTappedEvent)`:
     - Checks `GameStateController.Instance.IsProcessingMove` → returns if true
     - Starts `ProcessCubeTap()` coroutine

5. **Match detection:**
   - `GameStateController.Instance.SetProcessingMove(true)` — Locks input
   - `MatchController.ProcessCubeTap()`:
     - Gets `GridCell` at tapped position: `gridController.GetCell(evt.GridX, evt.GridY)`
     - Calls `matchDetector.FindMatchingGroup(tappedCell)`:
       - Flood-fill algorithm: `FindMatchingNeighbors()` recursively finds connected cubes of same color
       - Returns `List<GridCell>` of matching group

6. **Match validation:**
   - `MatchValidator.IsValidMatch(matchingGroup)` — Checks `group.Count >= 2`
   - If valid:
     - `GameStateController.Instance.UseMove()` — Decrements `movesRemaining`, publishes `MovesChangedEvent`

7. **Rocket creation check:**
   - `MatchValidator.CanCreateRocket(matchingGroup)` — Checks `group.Count >= 4`
   - If true:
     - Randomly selects `ItemType.HorizontalRocket` or `ItemType.VerticalRocket`
     - Starts `RocketController.Instance.AnimateRocketCreation()` coroutine:
       - `RocketAnimator.AnimateRocketCreation()` — Animates cubes gathering to center (0.4s)
       - `AudioManager.Instance.PlayRocketCreationSound()`
       - Waits 0.1s
     - Spawns rocket at tapped cell: `gridController.SpawnItem(rocketType, x, y)`
   - If false:
     - Destroys matching cubes:
       - For each `GridCell` in matching group:
         - Gets `CubeItem`
         - Spawns particles: `ParticleEffectManager.Instance.SpawnCubeParticles()`
         - Plays sound: `AudioManager.Instance.PlayCubeBreakSound()`
         - Returns to pool: `PoolManager.Instance.ReturnItem(cube)`
         - Removes from cell: `cell.RemoveItem()`

8. **Obstacle damage:**
   - `MatchController.DamageAdjacentObstacles(matchingGroup)`:
     - For each cell in matching group:
       - Gets adjacent cells: `gridController.GetAdjacentCells(x, y)`
       - For each adjacent cell:
         - If `ObstacleItem` and not already damaged:
           - Calls `obstacle.TakeDamage(1)` — Updates health, sprite
           - If destroyed: Publishes `ObstacleDestroyedEvent`

9. **Event publishing:**
   - Publishes `MatchProcessedEvent { MatchCount, RocketCreated }`
   - Waits 0.2s

10. **Gravity trigger:**
    - Publishes `GravityStartedEvent`

11. **Win/lose check:**
    - `GameStateController.Instance.CheckWinCondition()` — Checks `ObstacleController.Instance.AreAllObstaclesCleared()`
    - `GameStateController.Instance.CheckLoseCondition()` — Checks `movesRemaining <= 0 && !win`
    - If win: `GameStateController.Instance.WinLevel()`
    - If lose: `GameStateController.Instance.LoseLevel()`

12. **Unlock input:**
    - `GameStateController.Instance.SetProcessingMove(false)`

**Timing:**
- Input processing: Synchronous (<1ms)
- Match detection: Synchronous (<5ms for typical grid)
- Rocket creation animation: 0.4s + 0.1s = 0.5s
- Cube destruction: Synchronous (instant)
- Wait before gravity: 0.2s
- Total before gravity: ~0.7s (with rocket) or ~0.2s (without rocket)

### 4.4 Creation of Special Tiles (Rockets)

**Location:** `Assets/Scripts/Gameplay/Matching/MatchController.cs:63-97`

**Sequence:**
1. **Detection:**
   - `MatchValidator.CanCreateRocket(matchingGroup)` — Returns `group.Count >= 4`
   - Called in `MatchController.ProcessCubeTap()` after match validation

2. **Animation:**
   - `RocketController.Instance.AnimateRocketCreation(matchingGroup, rocketCell, rocketType)`:
     - `RocketAnimator.AnimateRocketCreation()`:
       - Creates temporary animating cube objects for each matched cube
       - Animates them gathering to target cell (0.4s duration, `SmoothStep` interpolation)
       - Scales cubes during animation (1.0 → 1.2 → 1.0)
       - Destroys animating cubes and original cubes
     - `AudioManager.Instance.PlayRocketCreationSound()`
     - Waits 0.1s

3. **Spawn:**
   - `gridController.SpawnItem(rocketType, rocketCell.x, visibleY)`:
     - Gets item from pool: `PoolManager.Instance.GetItem(rocketType, parent)`
     - Positions item at cell location
     - Calls `item.Initialize(rocketType)` — Sets sprite (horizontal/vertical)
     - Sets cell item: `cell.SetItem(item)`
     - Updates sibling order for rendering
     - Publishes `ItemSpawnedEvent`

4. **Event:**
   - Publishes `RocketCreatedEvent { GridX, GridY, RocketType }` (if needed)

**Timing:**
- Animation: 0.4s
- Audio delay: 0.1s
- Total: ~0.5s

### 4.5 Activation of Special Tiles (Rocket Explosion)

**Location:** `Assets/Scripts/Gameplay/Items/Rockets/RocketController.cs:66-114`

**Sequence:**
1. **Input:**
   - Player taps rocket → `InputController` publishes `RocketTappedEvent`

2. **Processing:**
   - `RocketController.HandleRocketTapped()`:
     - Checks `GameStateController.Instance.IsProcessingMove` → returns if true
     - Starts `ProcessRocketExplosion()` coroutine

3. **Lock:**
   - `GameStateController.Instance.SetProcessingMove(true)`
   - `GameStateController.Instance.UseMove()` — Decrements moves

4. **Combo check:**
   - `rocketService.GetAdjacentRockets(rocketPos)` — Checks 4 directions for other rockets
   - If combo found: `ProcessRocketCombo()` coroutine
   - Otherwise: `ProcessSingleRocketExplosion()` coroutine

5. **Single rocket explosion:**
   - `AudioManager.Instance.PlayRocketPopSound()`
   - `DestroyRocket(rocket)` — Destroys GameObject, removes from cell
   - Publishes `RocketExplodedEvent { GridX, GridY, RocketType, IsCombo: false }`
   - Gets direction: `rocketService.GetExplosionDirection(rocketType)` — `Vector2Int.right` (horizontal) or `Vector2Int.up` (vertical)
   - Starts projectile animations:
     - `projectileService.AnimateProjectile(rocketPos, direction)` — Animates projectile in +direction
     - `projectileService.AnimateProjectile(rocketPos, -direction)` — Animates projectile in -direction
   - Waits for projectiles to complete

6. **Damage application:**
   - Projectiles damage items in their path:
     - For each cell in path:
       - Gets item: `gridController.GetItem(x, y)`
       - If `CubeItem`: Destroys (particles, sound, return to pool)
       - If `ObstacleItem`: Calls `obstacle.TakeDamage(1)` (if can take rocket damage)
       - If `RocketItem`: Triggers chain reaction (`RocketController.Instance.TriggerChainReaction()`)

7. **Combo explosion (3x3):**
   - Destroys all rockets
   - `AudioManager.Instance.PlayComboRocketPopSound()`
   - `rocketService.DamageItemsIn3x3Area(center)` — Damages all items in 3x3 area
   - Waits 0.1s
   - Spawns combo projectiles (8 directions)

8. **Gravity trigger:**
   - Publishes `GravityStartedEvent`

9. **Win/lose check:**
   - Same as match processing

10. **Unlock:**
    - `GameStateController.Instance.SetProcessingMove(false)`

**Timing:**
- Projectile animation: ~0.5-1.0s (depends on grid size)
- Combo delay: 0.1s
- Total: ~1.0-1.5s

### 4.6 Level Completion / Game Over

**Win Sequence:**
1. **Win condition check:**
   - `GameStateController.CheckWinCondition()` — Returns `ObstacleController.Instance.AreAllObstaclesCleared()`
   - Called after match/rocket processing

2. **Win processing:**
   - `GameStateController.WinLevel()`:
     - Checks state is `Playing` → returns if not
     - `MusicManager.Instance.PlayEndGameMusic(true)` — Plays win music
     - `AudioManager.Instance.PlayGameWonSoundDelayed()` — Plays win sound (0.5s delay)
     - `SaveManager.Instance.MarkLevelCompleted(currentLevel)` — Marks level complete, advances current level
     - Checks `SaveManager.Instance.AreAllLevelsCompleted()`:
       - If true: Changes state to `GameState.Finished`
       - Otherwise: Changes state to `GameState.GameWon`
     - Publishes `LevelWonEvent { LevelNumber }`

3. **UI response:**
   - UI controllers listen to `LevelWonEvent` → Show win popup
   - Player can click "Next Level" or "Main Menu"

**Lose Sequence:**
1. **Lose condition check:**
   - `GameStateController.CheckLoseCondition()` — Returns `movesRemaining <= 0 && state == Playing && !win`
   - Called after match processing

2. **Lose processing:**
   - `GameStateController.LoseLevel()`:
     - Checks state is `Playing` → returns if not
     - `MusicManager.Instance.PlayEndGameMusic(false)` — Plays lose music
     - `AudioManager.Instance.PlayGameLostSoundDelayed()` — Plays lose sound (0.5s delay)
     - Changes state to `GameState.GameLost`
     - Publishes `LevelLostEvent { LevelNumber }`

3. **UI response:**
   - UI controllers listen to `LevelLostEvent` → Show lose popup
   - Player can click "Retry" or "Main Menu"

**Next Level:**
- `GameStateController.NextLevel()`:
  - Gets next level: `LevelManager.Instance.GetNextLevelAfter(currentLevel)`
  - If found: Calls `StartLevel(nextLevel)`
  - Otherwise: Returns to main menu

**Return to Main Menu:**
- `GameStateController.ReturnToMainMenu()`:
  - Changes state to `MainMenu`
  - `SceneTransitionManager.Instance.LoadMainScene()` — Loads main scene with fade

**Timing:**
- Win/lose sound delay: 0.5s
- State change: Synchronous
- Scene transition: ~1.4s (fade out 0.6s + load + fade in 0.8s)

---

## 5. Concrete Example Playthrough

**Scenario:** Player taps a cube, creates 4-match, rocket spawns, player taps rocket, rocket explodes, gravity refills, chain reaction occurs.

**Timeline (0-20 seconds):**

| Time | Action | Method/Event | Script | Sync/Async |
|------|--------|--------------|--------|-----------|
| 0.0s | Game running, level loaded | - | - | - |
| 0.1s | Player taps cube at (3,4) | `InputController.Update()` → `ProcessTap()` | `InputController.cs:96` | Sync |
| 0.1s | Raycast finds CubeItem | `InputHandler.GetTappedItem()` | `InputHandler.cs:74` | Sync |
| 0.1s | Event published | `EventBus.Publish(CubeTappedEvent)` | `InputController.cs:144` | Sync |
| 0.1s | Match processing starts | `MatchController.HandleCubeTapped()` → `ProcessCubeTap()` | `MatchController.cs:40` | Async (Coroutine) |
| 0.1s | Input locked | `GameStateController.SetProcessingMove(true)` | `MatchController.cs:52` | Sync |
| 0.1s | Match detection | `MatchDetectorService.FindMatchingGroup()` | `MatchDetectorService.cs:13` | Sync |
| 0.1s | Found 4 matching cubes | Returns `List<GridCell>` (4 cells) | `MatchDetectorService.cs:20` | Sync |
| 0.1s | Match validated | `MatchValidator.IsValidMatch()` → true | `MatchValidator.cs:5` | Sync |
| 0.1s | Rocket check | `MatchValidator.CanCreateRocket()` → true (≥4) | `MatchValidator.cs:10` | Sync |
| 0.1s | Move used | `GameStateController.UseMove()` | `MatchController.cs:61` | Sync |
| 0.1s | Rocket creation animation | `RocketAnimator.AnimateRocketCreation()` | `RocketAnimator.cs:12` | Async (Coroutine, 0.4s) |
| 0.5s | Rocket creation sound | `AudioManager.PlayRocketCreationSound()` | `RocketAnimator.cs:21` | Async (Coroutine) |
| 0.6s | Rocket spawned | `GridController.SpawnItem(HorizontalRocket, 3, 4)` | `MatchController.cs:95` | Sync |
| 0.6s | Event published | `EventBus.Publish(MatchProcessedEvent)` | `MatchController.cs:101` | Sync |
| 0.8s | Wait complete | `yield return new WaitForSeconds(0.2f)` | `MatchController.cs:107` | Async |
| 0.8s | Gravity started | `EventBus.Publish(GravityStartedEvent)` | `MatchController.cs:109` | Sync |
| 0.8s | Gravity processing | `GravityController.HandleGravityStarted()` → `ProcessGravity()` | `GravityController.cs:49` | Async (Coroutine) |
| 0.8s | Fall calculation | `GravityService.CalculateFallDistance()` for each column | `GravityService.cs:14` | Sync |
| 0.8s | Fall animation | `FallAnimator.AnimateFalls()` | `FallAnimator.cs:42` | Async (Coroutine, ~0.3s) |
| 1.1s | Gravity complete | `EventBus.Publish(GravityCompletedEvent)` | `GravityController.cs:65` | Sync |
| 1.1s | New cubes spawned | `GridController.SpawnNewCubes()` | `GravityController.cs:63` | Sync |
| 1.1s | Input unlocked | `GameStateController.SetProcessingMove(false)` | `MatchController.cs:122` | Sync |
| 2.0s | Player taps rocket at (3,4) | `InputController.Update()` → `ProcessTap()` | `InputController.cs:96` | Sync |
| 2.0s | Event published | `EventBus.Publish(RocketTappedEvent)` | `InputController.cs:156` | Sync |
| 2.0s | Rocket processing | `RocketController.HandleRocketTapped()` → `ProcessRocketExplosion()` | `RocketController.cs:66` | Async (Coroutine) |
| 2.0s | Input locked | `GameStateController.SetProcessingMove(true)` | `RocketController.cs:87` | Sync |
| 2.0s | Move used | `GameStateController.UseMove()` | `RocketController.cs:88` | Sync |
| 2.0s | Rocket destroyed | `DestroyRocket(rocket)` | `RocketController.cs:123` | Sync |
| 2.0s | Sound played | `AudioManager.PlayRocketPopSound()` | `RocketController.cs:121` | Async (Coroutine) |
| 2.0s | Event published | `EventBus.Publish(RocketExplodedEvent)` | `RocketController.cs:125` | Sync |
| 2.0s | Projectiles start | `RocketProjectileService.AnimateProjectile()` (2 directions) | `RocketController.cs:133-134` | Async (Coroutine, ~0.8s) |
| 2.0-2.8s | Projectiles damage items | For each cell in path: destroy cubes, damage obstacles | `RocketService.cs:64-81` | Sync (per cell) |
| 2.8s | Projectiles complete | `projectileService.WaitForAllProjectilesToComplete()` | `RocketController.cs:136` | Async |
| 2.8s | Gravity started | `EventBus.Publish(GravityStartedEvent)` | `RocketController.cs:102` | Sync |
| 2.8s | Gravity processing | `GravityController.ProcessGravity()` | `GravityController.cs:59` | Async (Coroutine) |
| 3.1s | Gravity complete | `EventBus.Publish(GravityCompletedEvent)` | `GravityController.cs:65` | Sync |
| 3.1s | New cubes spawned | `GridController.SpawnNewCubes()` | `GravityController.cs:63` | Sync |
| 3.1s | Input unlocked | `GameStateController.SetProcessingMove(false)` | `RocketController.cs:113` | Sync |
| 3.2s | Win check | `GameStateController.CheckWinCondition()` | `RocketController.cs:104` | Sync |
| 3.2s | (Assume not won) | - | - | - |
| 20.0s | (Continue gameplay) | - | - | - |

**Notes:**
- Most operations are synchronous except animations (coroutines)
- Total time for match + rocket creation: ~0.8s
- Total time for rocket explosion: ~1.1s
- Gravity typically takes 0.2-0.5s depending on fall distances

---

## 6. Call Stacks and Event Flow for Critical Operations

### 6.1 Match Detection Flow

```
InputController.Update()
  └─ InputController.ProcessTap()
      └─ InputHandler.GetTappedItem()
          └─ GraphicRaycaster.Raycast()
              └─ Returns BaseItem
      └─ InputController.HandleItemTapped()
          └─ EventBus.Publish(CubeTappedEvent)

MatchController.HandleCubeTapped(CubeTappedEvent)
  └─ MatchController.ProcessCubeTap() [Coroutine]
      └─ GameStateController.SetProcessingMove(true)
      └─ GridController.GetCell(x, y)
          └─ GridDataService.GetCell(x, y)
              └─ GridDataService.GetExtendedCell(x, y + bufferRows)
      └─ MatchDetectorService.FindMatchingGroup(cell)
          └─ MatchDetectorService.FindMatchingNeighbors() [Recursive]
              └─ GridController.GetAdjacentCells(x, y)
                  └─ GridDataService.GetAdjacentCells(x, y)
                      └─ Returns List<GridCell>
              └─ Recurses for each adjacent matching cube
      └─ MatchValidator.IsValidMatch(group)
      └─ MatchValidator.CanCreateRocket(group)
      └─ GameStateController.UseMove()
          └─ EventBus.Publish(MovesChangedEvent)
      └─ (If rocket) RocketController.AnimateRocketCreation()
      └─ (If no rocket) Destroy matching cubes
      └─ MatchController.DamageAdjacentObstacles()
      └─ EventBus.Publish(MatchProcessedEvent)
      └─ EventBus.Publish(GravityStartedEvent)
      └─ GameStateController.SetProcessingMove(false)
```

### 6.2 Gravity/Refill Flow

```
EventBus.Publish(GravityStartedEvent)

GravityController.HandleGravityStarted(GravityStartedEvent)
  └─ GravityController.ProcessGravity() [Coroutine]
      └─ GravityController.ProcessAllFalls() [Coroutine]
          └─ Loop: for each column, bottom to top
              └─ GravityService.CalculateFallDistance(x, y)
                  └─ Checks cells below until non-empty
              └─ GravityService.PrepareFallOperation()
                  └─ Creates FallOperation struct
                  └─ Adds to currentWave list
          └─ FallAnimator.AnimateFalls(currentWave) [Coroutine]
              └─ Animates all items in wave simultaneously
              └─ Uses AnimationCurve for easing
              └─ Updates positions every frame
          └─ Repeat until no more falls (max 20 iterations)
      └─ GridController.SpawnNewCubes()
          └─ For each empty cell in buffer rows:
              └─ GridController.SpawnItemInExtendedGrid(randomCube, x, y)
                  └─ PoolManager.Instance.GetItem(ItemType, parent)
                  └─ BaseItem.Initialize(itemType)
                  └─ GridCell.SetItem(item)
                  └─ EventBus.Publish(ItemSpawnedEvent)
      └─ EventBus.Publish(GridUpdatedEvent)
      └─ EventBus.Publish(GravityCompletedEvent)
```

### 6.3 Special Tile Activation (Rocket) Flow

```
InputController.Update()
  └─ InputController.ProcessTap()
      └─ InputHandler.GetTappedItem()
      └─ InputController.HandleItemTapped()
          └─ (If RocketItem) EventBus.Publish(RocketTappedEvent)

RocketController.HandleRocketTapped(RocketTappedEvent)
  └─ RocketController.ProcessRocketExplosion() [Coroutine]
      └─ GameStateController.SetProcessingMove(true)
      └─ GameStateController.UseMove()
      └─ RocketService.GetAdjacentRockets(position)
          └─ Checks 4 directions for RocketItem
      └─ (If combo) RocketController.ProcessRocketCombo()
          └─ DestroyRocket() for all rockets
          └─ RocketService.DamageItemsIn3x3Area(center)
              └─ For each cell in 3x3:
                  └─ RocketService.DamageItem(item)
                      └─ (If CubeItem) HandleCubeDamage()
                      └─ (If ObstacleItem) HandleObstacleDamage()
                      └─ (If RocketItem) TriggerChainReaction()
          └─ RocketProjectileService.SpawnComboProjectiles()
      └─ (If single) RocketController.ProcessSingleRocketExplosion()
          └─ DestroyRocket(rocket)
          └─ RocketService.GetExplosionDirection(rocketType)
          └─ RocketProjectileService.AnimateProjectile() [2x Coroutine]
              └─ Animates projectile along path
              └─ Damages items in path
      └─ RocketProjectileService.WaitForAllProjectilesToComplete()
      └─ EventBus.Publish(GravityStartedEvent)
      └─ GameStateController.SetProcessingMove(false)
```

### 6.4 Level Complete Flow

```
MatchController.ProcessCubeTap() [or RocketController.ProcessRocketExplosion()]
  └─ GameStateController.CheckWinCondition()
      └─ ObstacleController.Instance.AreAllObstaclesCleared()
          └─ Checks obstaclesRemaining dictionary
              └─ Returns true if all values == 0

GameStateController.WinLevel()
  └─ MusicManager.Instance.PlayEndGameMusic(true)
  └─ AudioManager.Instance.PlayGameWonSoundDelayed() [Coroutine, 0.5s delay]
  └─ SaveManager.Instance.MarkLevelCompleted(currentLevel)
      └─ SaveData.MarkLevelCompleted(level)
      └─ SaveData.Save()
          └─ JsonUtility.ToJson(this)
          └─ PlayerPrefs.SetString("SaveData", json)
          └─ PlayerPrefs.Save()
      └─ Advances currentLevel if needed
  └─ SaveManager.Instance.AreAllLevelsCompleted()
      └─ Checks all levels in LevelManager
  └─ GameStateController.ChangeGameState(finished ? Finished : GameWon)
      └─ EventBus.Publish(GameStateChangedEvent)
  └─ EventBus.Publish(LevelWonEvent)

GameplayUIController (or other UI) listens to LevelWonEvent
  └─ Shows win popup
```

### 6.5 Chain Reaction Flow

```
RocketService.DamageItem(RocketItem rocket)
  └─ RocketController.Instance.TriggerChainReaction(rocket)
      └─ RocketController.ProcessChainReactionExplosion() [Coroutine]
          └─ AudioManager.Instance.PlayRocketPopSound()
          └─ DestroyRocket(rocket)
          └─ RocketProjectileService.AnimateProjectile() [2x Coroutine]
              └─ Damages items in path
                  └─ (If another rocket) Recurses via DamageItem()
          └─ Waits 0.1s
```

**Note:** Chain reactions are recursive but limited by grid size. No explicit depth limit, but physics limits prevent infinite loops.

---

## 7. Data Ownership & Persistence

### 7.1 Level Data

**Storage:** JSON files in `Assets/Resources/CaseStudyAssets2025/Levels/level_*.json`

**Format:**
```json
{
  "level_number": 1,
  "grid_width": 8,
  "grid_height": 8,
  "move_count": 20,
  "grid": ["r", "g", "b", "y", "bo", "s", "v", ...]
}
```

**Loading:**
- **`LevelManager.LoadAllLevels()`** — Scans directory at startup, loads all JSON files
- **`LevelManager.LoadLevelFromJSON(levelNumber)`** — Uses `Resources.Load<TextAsset>()`, parses with `JsonUtility.FromJson<LevelData>()`
- **Caching:** All levels stored in `Dictionary<int, LevelData>` in memory

**File:** `Assets/Scripts/Data/LevelData.cs`

### 7.2 Player Progress

**Storage:** `PlayerPrefs` key `"SaveData"` (JSON string)

**Format:**
```json
{
  "currentLevel": 5,
  "levelCompleted": [true, true, true, true, false, false, ...]
}
```

**Class:** `Assets/Scripts/Data/SaveData.cs`

**Methods:**
- **`SaveData.Save()`** — `JsonUtility.ToJson(this)` → `PlayerPrefs.SetString("SaveData", json)` → `PlayerPrefs.Save()`
- **`SaveData.Load()`** — `PlayerPrefs.GetString("SaveData")` → `JsonUtility.FromJson<SaveData>(json)`
- **`SaveManager.SaveGame()`** — Wrapper that calls `currentSave.Save()`
- **`SaveManager.MarkLevelCompleted(level)`** — Updates array, calls `SaveGame()`

**Timing:** Synchronous (blocking I/O, but fast on modern devices)

### 7.3 Game Configuration

**Storage:** ScriptableObject asset (`GameConfig.asset`)

**File:** `Assets/Scripts/Data/GameConfig.cs`

**Fields:**
- Grid settings (cubeSize, gridSpacing)
- Animation settings (fallSpeed, explosionDelay, rocketCreationDuration, cubeDestroyDuration)
- Animation curves (fallCurve, explosionCurve, scaleCurve)

**Access:** `ConfigurationManager.Instance.GameConfig` or `GameStateController.Instance.Config`

**Editing:** Via Unity Inspector on ScriptableObject asset

### 7.4 High Scores

**Not implemented** — No high score system found in codebase

---

## 8. Performance & Concurrency Considerations

### 8.1 Hot Paths

#### **Per-Frame Update() Loops:**
1. **`InputController.Update()`** — `Assets/Scripts/Gameplay/Input/InputController.cs:76`
   - Called every frame
   - Checks input state, processes taps
   - **Optimization:** Early return if input locked or invalid state

2. **`AudioManager.Update()`** — `Assets/Scripts/Audio/AudioManager.cs:118`
   - Cleans up finished `AudioSource` components
   - Iterates `activeAudioSources` list
   - **Optimization:** Only checks if list has items

3. **`ParticleEffectManager.Update()`** — `Assets/Scripts/Effects/ParticleEffectManager.cs:53`
   - Updates all active particles
   - Iterates `activeParticles` list, removes expired
   - **Optimization:** Uses reverse iteration for safe removal

#### **Heavy Loops:**
1. **Match Detection** — `Assets/Scripts/Gameplay/Matching/MatchDetectorService.cs:13`
   - Flood-fill algorithm: O(n) where n = connected matching cubes
   - Recursive calls: `FindMatchingNeighbors()`
   - **Bottleneck:** Deep recursion on large matching groups
   - **Fix:** Consider iterative BFS instead of recursion

2. **Gravity Calculation** — `Assets/Scripts/Gameplay/Gravity/GravityController.cs:68`
   - Nested loops: `for x in width, for y in height`
   - Called multiple times per gravity cycle (max 20 iterations)
   - **Bottleneck:** O(width × height × iterations)
   - **Optimization:** Already batches operations (`maxOperationsPerFrame = 30`)

3. **Grid Initialization** — `Assets/Scripts/Gameplay/Grid/GridController.cs:133`
   - Creates all cells: `width × (height + bufferRows)`
   - Spawns all items
   - **Bottleneck:** Instantiation of many GameObjects
   - **Optimization:** Uses object pooling for items, but cells are instantiated fresh

### 8.2 Potential Race Conditions

#### **1. Multiple Coroutines Modifying Grid State**
**Location:** `MatchController.ProcessCubeTap()` and `GravityController.ProcessGravity()` running simultaneously

**Issue:** If gravity starts before match processing completes, items might be moved while being destroyed.

**Current Protection:** `GameStateController.IsProcessingMove` flag prevents input, but doesn't prevent gravity from starting.

**Fix:** Add `isProcessingGravity` flag, check in match processing before starting gravity.

#### **2. Event Bus Handler Execution Order**
**Location:** `EventBus.Publish()` iterates handlers in reverse order (`for (int i = handlers.Count - 1; i >= 0; i--)`)

**Issue:** Handler execution order is not guaranteed if multiple handlers subscribe to same event.

**Fix:** Use priority system or explicit ordering if needed.

#### **3. Pool Return During Animation**
**Location:** Items returned to pool while animations still running

**Issue:** `PoolManager.ReturnItem()` might be called while `FallAnimator` is still animating the item.

**Current Protection:** Items are disabled (`SetActive(false)`) but transform might still be animated.

**Fix:** Ensure animations complete before returning to pool, or check `gameObject.activeInHierarchy` in animator.

### 8.3 Performance Bottlenecks

1. **Grid Cell Instantiation** — Creates all cells on level start (could be 100+ GameObjects)
   - **Suggestion:** Pool `GridCell` objects or use object pooling

2. **Match Detection Recursion** — Deep recursion on large groups
   - **Suggestion:** Use iterative BFS with `Queue<GridCell>`

3. **Particle Updates** — Updates all particles every frame
   - **Suggestion:** Use object pooling (already implemented) and limit max particles

4. **Audio Source Pool** — Creates new sources if pool exhausted
   - **Suggestion:** Increase pool size or limit concurrent sounds

---

## 9. Testing & Instrumentation Suggestions

### 9.1 Unit Test Targets

#### **Pure Logic Classes (No Unity Dependencies):**
1. **`MatchValidator.cs`** — Static validation methods
   - Test `IsValidMatch()` with various group sizes
   - Test `CanCreateRocket()` with 3, 4, 5+ cubes

2. **`MatchDetectorService.cs`** — Match detection algorithm
   - Mock `GridController`, test flood-fill on known grids
   - Test edge cases: single cube, large group, disconnected groups

3. **`GravityService.cs`** — Fall distance calculation
   - Mock `GridController`, test fall distances for various scenarios
   - Test with obstacles, empty columns, full columns

4. **`SaveData.cs`** — Save/load logic
   - Test JSON serialization/deserialization
   - Test level completion tracking
   - Test edge cases: invalid level numbers, corrupted data

#### **Integration Test Targets:**
1. **Match Processing Flow** — Test full flow from input → match → destruction → gravity
2. **Rocket Creation** — Test match → rocket creation → spawn
3. **Rocket Explosion** — Test rocket tap → explosion → damage → gravity
4. **Level Progression** — Test level complete → save → next level load

### 9.2 Logging Suggestions

#### **Match Detection:**
```csharp
// In MatchDetectorService.FindMatchingGroup()
Debug.Log($"[MatchDetection] Starting at ({cell.x}, {cell.y}), color: {targetType}");
Debug.Log($"[MatchDetection] Found {group.Count} matching cubes");
```

#### **Gravity Processing:**
```csharp
// In GravityController.ProcessAllFalls()
Debug.Log($"[Gravity] Iteration {fallIterations}, {currentWave.Count} items falling");
```

#### **Event Publishing:**
```csharp
// In EventBus.Publish() (add logging flag)
if (enableEventLogging)
    Debug.Log($"[EventBus] Published {eventType.Name}, {handlers.Count} handlers");
```

#### **Performance Profiling:**
```csharp
// Wrap heavy operations
var stopwatch = System.Diagnostics.Stopwatch.StartNew();
// ... operation ...
Debug.Log($"[Performance] {operationName} took {stopwatch.ElapsedMilliseconds}ms");
```

### 9.3 Analytics Hooks

**Suggested locations:**
1. **Level Start** — `GameStateController.StartLevel()`
   - Track: level number, moves available, obstacle counts

2. **Match Made** — `MatchController.ProcessCubeTap()`
   - Track: match size, rocket created, time since level start

3. **Rocket Used** — `RocketController.ProcessRocketExplosion()`
   - Track: rocket type, combo (yes/no), items destroyed

4. **Level Complete** — `GameStateController.WinLevel()` / `LoseLevel()`
   - Track: level number, moves remaining, time taken, win/lose

5. **Obstacle Destroyed** — `ObstacleController.HandleObstacleDestroyed()`
   - Track: obstacle type, level number, destruction method (match/rocket)

**Sample Implementation:**
```csharp
public static class Analytics
{
    public static void TrackEvent(string eventName, Dictionary<string, object> parameters)
    {
        // Integrate with analytics SDK (e.g., Unity Analytics, Firebase)
        Debug.Log($"[Analytics] {eventName}: {JsonUtility.ToJson(parameters)}");
    }
}

// Usage:
Analytics.TrackEvent("match_made", new Dictionary<string, object>
{
    { "level", currentLevel },
    { "match_size", matchingGroup.Count },
    { "rocket_created", shouldCreateRocket }
});
```

---

## 10. Improvement Opportunities and Risks

### 10.1 Prioritized Improvements

#### **1. Add Input Lock During Gravity (HIGH PRIORITY)**
**Location:** `MatchController.cs:109`, `GravityController.cs:49`

**Issue:** Gravity can start while match processing is still destroying items.

**Fix:**
```csharp
// In GameStateController.cs
private bool isProcessingGravity = false;

public bool IsProcessingGravity => isProcessingGravity;

public void SetProcessingGravity(bool processing)
{
    isProcessingGravity = processing;
}

// In MatchController.cs, before publishing GravityStartedEvent:
if (!GameStateController.Instance.IsProcessingGravity)
{
    EventBus.Publish(new GravityStartedEvent());
}

// In GravityController.cs:
public IEnumerator ProcessGravity()
{
    GameStateController.Instance.SetProcessingGravity(true);
    // ... existing code ...
    GameStateController.Instance.SetProcessingGravity(false);
}
```

**Risk:** Low — Simple flag addition

#### **2. Extract Interfaces for Testing (MEDIUM PRIORITY)**
**Location:** Multiple managers

**Issue:** Hard to unit test due to static singleton dependencies.

**Fix:** Create interfaces:
```csharp
public interface IGridController
{
    GridCell GetCell(int x, int y);
    BaseItem GetItem(int x, int y);
    void SpawnItem(ItemType type, int x, int y);
}

// Inject via constructor or property
public class MatchDetectorService
{
    private IGridController gridController;
    public MatchDetectorService(IGridController grid) { this.gridController = grid; }
}
```

**Risk:** Medium — Requires refactoring many classes

#### **3. Replace Recursive Match Detection with Iterative BFS (MEDIUM PRIORITY)**
**Location:** `MatchDetectorService.cs:26`

**Issue:** Stack overflow risk on very large matching groups.

**Fix:**
```csharp
public List<GridCell> FindMatchingGroup(GridCell startCell)
{
    List<GridCell> group = new List<GridCell>();
    Queue<GridCell> queue = new Queue<GridCell>();
    HashSet<GridCell> visited = new HashSet<GridCell>();
    
    if (startCell.currentItem is CubeItem startCube)
    {
        queue.Enqueue(startCell);
        visited.Add(startCell);
        
        while (queue.Count > 0)
        {
            GridCell current = queue.Dequeue();
            group.Add(current);
            
            List<GridCell> adjacent = gridController.GetAdjacentCells(current.x, visibleY);
            foreach (GridCell adj in adjacent)
            {
                if (!visited.Contains(adj) && adj.currentItem is CubeItem cube && 
                    cube.GetCubeColor() == startCube.GetCubeColor())
                {
                    queue.Enqueue(adj);
                    visited.Add(adj);
                }
            }
        }
    }
    
    return group;
}
```

**Risk:** Low — Algorithmic improvement, no behavior change

#### **4. Add Null Checks in Event Handlers (MEDIUM PRIORITY)**
**Location:** All event handlers

**Issue:** Handlers assume instances exist (e.g., `GridController.Instance`).

**Fix:** Add null checks:
```csharp
private void HandleLevelStarted(LevelStartedEvent evt)
{
    if (GridController.Instance == null) return;
    // ... rest of code
}
```

**Risk:** Low — Defensive programming

#### **5. Pool GridCell Objects (LOW PRIORITY)**
**Location:** `GridController.cs:199`

**Issue:** Creates new `GridCell` objects on every level load.

**Fix:** Create `GridCellPool`, reuse cells across levels.

**Risk:** Medium — Requires careful cleanup

#### **6. Reduce Coupling Between MatchController and RocketController (MEDIUM PRIORITY)**
**Location:** `MatchController.cs:70`

**Issue:** Direct dependency on `RocketController.Instance`.

**Fix:** Use event: `EventBus.Publish(new RocketCreationRequestEvent { ... })`

**Risk:** Medium — Requires event system extension

#### **7. Add Validation for Grid Bounds (LOW PRIORITY)**
**Location:** `GridDataService.cs`, `GridController.cs`

**Issue:** Some methods don't validate bounds before array access.

**Fix:** Add bounds checks in `GetExtendedCell()`, `GetCell()`, etc.

**Risk:** Low — Defensive programming

#### **8. Extract Constants for Magic Numbers (LOW PRIORITY)**
**Location:** Multiple files

**Issue:** Magic numbers scattered (e.g., `0.2f`, `0.4f`, `20`).

**Fix:** Create `GameplayConstants` class:
```csharp
public static class GameplayConstants
{
    public const float MATCH_PROCESSING_DELAY = 0.2f;
    public const float ROCKET_CREATION_DURATION = 0.4f;
    public const int MAX_GRAVITY_ITERATIONS = 20;
}
```

**Risk:** Low — Code organization

#### **9. Add Error Handling for JSON Parsing (MEDIUM PRIORITY)**
**Location:** `LevelManager.cs:79`, `SaveData.cs:31`

**Issue:** JSON parsing can fail silently or crash.

**Fix:** Add try-catch with logging:
```csharp
try
{
    levelData = JsonUtility.FromJson<LevelData>(jsonFile.text);
}
catch (Exception e)
{
    Debug.LogError($"Failed to parse level {levelNumber}: {e.Message}");
    return null;
}
```

**Risk:** Low — Error handling

#### **10. Add Unit Tests for Core Logic (HIGH PRIORITY)**
**Location:** Create `Tests/` folder

**Issue:** No unit tests found in codebase.

**Fix:** Create test assembly, add tests for `MatchValidator`, `MatchDetectorService`, `SaveData`.

**Risk:** Low — New code addition

---

## 11. Mapping of Gameplay → Code Responsibilities

| Gameplay Concept | Code Classes/Methods Responsible |
|-----------------|----------------------------------|
| **Player Input** | `InputController.cs:HandleInput()` → `InputHandler.GetTappedItem()` → Publishes `CubeTappedEvent` or `RocketTappedEvent` |
| **Tile Spawn** | `GridController.cs:SpawnItem()` → `PoolManager.Instance.GetItem()` → `BaseItem.Initialize()` → `GridCell.SetItem()` |
| **Match Detection** | `MatchController.cs:HandleCubeTapped()` → `MatchDetectorService.FindMatchingGroup()` → `MatchValidator.IsValidMatch()` |
| **Match Processing** | `MatchController.cs:ProcessCubeTap()` → Destroys cubes → `DamageAdjacentObstacles()` → Publishes `MatchProcessedEvent` |
| **Rocket Creation** | `MatchController.cs:ProcessCubeTap()` → `MatchValidator.CanCreateRocket()` → `RocketAnimator.AnimateRocketCreation()` → `GridController.SpawnItem()` |
| **Rocket Activation** | `RocketController.cs:HandleRocketTapped()` → `ProcessRocketExplosion()` → `RocketProjectileService.AnimateProjectile()` → `RocketService.DamageItemsIn3x3Area()` |
| **Gravity** | `GravityController.cs:ProcessGravity()` → `GravityService.CalculateFallDistance()` → `FallAnimator.AnimateFalls()` → `GridController.SpawnNewCubes()` |
| **Obstacle Damage** | `MatchController.DamageAdjacentObstacles()` / `RocketService.DamageItem()` → `ObstacleItem.TakeDamage()` → `ObstacleBehavior.TakeDamage()` → Publishes `ObstacleDestroyedEvent` |
| **Obstacle Destruction** | `ObstacleItem.TakeDamage()` → `ObstacleBehavior.IsDestroyed` → `ObstacleController.HandleObstacleDestroyed()` → Publishes `GoalUpdatedEvent` |
| **Level Win** | `GameStateController.CheckWinCondition()` → `ObstacleController.AreAllObstaclesCleared()` → `GameStateController.WinLevel()` → `SaveManager.MarkLevelCompleted()` |
| **Level Lose** | `GameStateController.CheckLoseCondition()` → `GameStateController.LoseLevel()` → Publishes `LevelLostEvent` |
| **Level Load** | `SceneTransitionManager.LoadLevelScene()` → `GameStateController.StartLevel()` → `GridController.InitializeGrid()` → `ObstacleController.InitializeGoals()` |
| **Save Progress** | `SaveManager.MarkLevelCompleted()` → `SaveData.MarkLevelCompleted()` → `SaveData.Save()` → `PlayerPrefs.SetString()` |
| **Audio (Cube Break)** | `MatchController.ProcessCubeTap()` → `AudioManager.PlayCubeBreakSound()` → `AudioSource.Play()` |
| **Audio (Rocket)** | `RocketController.ProcessRocketExplosion()` → `AudioManager.PlayRocketPopSound()` / `PlayComboRocketPopSound()` |
| **Particles** | `MatchController.ProcessCubeTap()` → `ParticleEffectManager.SpawnCubeParticles()` → `PoolManager.GetParticle()` → `ParticleElement.Setup()` |
| **Scene Transition** | `SceneTransitionManager.LoadMainScene()` / `LoadLevelScene()` → `TransitionController.FadeOut()` → `SceneManager.LoadSceneAsync()` → `TransitionController.FadeIn()` |
| **UI Updates (Moves)** | `GameStateController.UseMove()` → Publishes `MovesChangedEvent` → `GameplayUIController.HandleMovesChanged()` → Updates `movesText` |
| **UI Updates (Goals)** | `ObstacleController.HandleObstacleDestroyed()` → Publishes `GoalUpdatedEvent` → `GameplayUIController.HandleGoalUpdated()` → Updates `GoalItem` count |
| **Object Pooling** | `PoolManager.GetItem()` → `GenericPool<T>.Get()` → `IPoolable.OnSpawn()` → Returns pooled object |
| **Object Return** | `PoolManager.ReturnItem()` → `IPoolable.OnReturnToPool()` → `GenericPool<T>.Return()` → Object disabled and queued |

---

## 12. Top 10 Files for New Developers

**Priority Order:**

1. **`Assets/Scripts/Core/GameStateController.cs`** — Central state machine, orchestrates game flow
2. **`Assets/Scripts/Core/Events/EventBus.cs`** — Event system used throughout codebase
3. **`Assets/Scripts/Core/Events/GameEvents.cs`** — All event type definitions
4. **`Assets/Scripts/Gameplay/Grid/GridController.cs`** — Grid management, item spawning
5. **`Assets/Scripts/Gameplay/Matching/MatchController.cs`** — Match processing logic
6. **`Assets/Scripts/Gameplay/Input/InputController.cs`** — Input handling
7. **`Assets/Scripts/Gameplay/Gravity/GravityController.cs`** — Gravity and refill logic
8. **`Assets/Scripts/Gameplay/Items/Rockets/RocketController.cs`** — Rocket explosion logic
9. **`Assets/Scripts/Data/LevelData.cs`** — Level data structure
10. **`Assets/Scripts/Enums/GameEnums.cs`** — All enums (ItemType, GameState, etc.)

---

## Appendix A: File Structure Reference

### Core Systems
- `Core/GameStateController.cs` — State machine
- `Core/Events/EventBus.cs` — Event system
- `Core/Events/GameEvents.cs` — Event definitions
- `Core/ConfigurationManager.cs` — Config access

### Managers
- `Managers/LevelManager.cs` — Level loading
- `Managers/SaveManager.cs` — Save system
- `Managers/SceneTransitionManager.cs` — Scene transitions

### Gameplay
- `Gameplay/Grid/GridController.cs` — Grid management
- `Gameplay/Grid/Services/GridDataService.cs` — Grid data
- `Gameplay/Grid/Services/GridLayoutService.cs` — Layout calculations
- `Gameplay/Matching/MatchController.cs` — Match processing
- `Gameplay/Matching/MatchDetectorService.cs` — Match detection
- `Gameplay/Matching/MatchValidator.cs` — Match validation
- `Gameplay/Input/InputController.cs` — Input handling
- `Gameplay/Gravity/GravityController.cs` — Gravity system
- `Gameplay/Items/Rockets/RocketController.cs` — Rocket logic
- `Gameplay/Items/Obstacles/ObstacleController.cs` — Obstacle tracking

### Data
- `Data/LevelData.cs` — Level structure
- `Data/SaveData.cs` — Save structure
- `Data/GameConfig.cs` — Config ScriptableObject

### Items
- `Items/BaseItem.cs` — Base item class
- `Items/CubeItem.cs` — Cube implementation
- `Items/RocketItem.cs` — Rocket implementation
- `Gameplay/Items/Obstacles/ObstacleItem.cs` — Obstacle implementation

### Services
- `Services/Pooling/PoolManager.cs` — Object pooling
- `Services/Pooling/GenericPool.cs` — Generic pool implementation
- `Audio/AudioManager.cs` — Audio system
- `Effects/ParticleEffectManager.cs` — Particle effects

---

## Appendix B: Key Constants and Timing Values

| Constant | Value | Location |
|----------|-------|----------|
| Rocket creation animation duration | 0.4s | `RocketAnimator.cs:10` |
| Match processing delay | 0.2s | `MatchController.cs:107` |
| Fade in duration | 0.8s | `TransitionController.cs:11` |
| Fade out duration | 0.6s | `TransitionController.cs:12` |
| Fall time per cell | 0.08s | `GravityService.cs:82` |
| Minimum fall time | 0.1s | `GravityService.cs:83` |
| Maximum fall time | 0.6s | `GravityService.cs:84` |
| Max gravity iterations | 20 | `GravityController.cs:72` |
| Max operations per frame (gravity) | 30 | `GravityController.cs:9` |
| Buffer rows | 20 | `GridController.cs:15` |
| Cube pool size | 50 | `PoolManager.cs:15` |
| Rocket pool size | 20 | `PoolManager.cs:16` |
| Obstacle pool size | 30 | `PoolManager.cs:17` |
| Particle pool size | 100 | `PoolManager.cs:18` |
| Audio source pool size | 20 | `AudioManager.cs:35` |

---

**End of Technical Report**

