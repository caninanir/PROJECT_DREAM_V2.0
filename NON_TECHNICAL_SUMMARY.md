# Match to POP - High-Level Flow Summary (Non-Technical)

**For:** Non-developer stakeholders  
**Purpose:** Understand game flow without code details

---

## Game Overview

"Match to POP" is a match-3 puzzle game where players tap groups of matching colored cubes to clear them and destroy obstacles. The game features special power-ups (rockets) that can be created by matching 4+ cubes, and these rockets can clear entire rows or columns when activated.

---

## Game Flow (High Level)

### 1. Starting the Game

When the game launches:
- The game loads all available levels from files
- It checks what level the player last reached
- It shows the main menu where players can select levels

### 2. Starting a Level

When a player selects a level:
- The screen fades out
- The level scene loads (showing the game board)
- The game reads the level configuration (board size, number of moves, obstacle layout)
- The board is created with cubes and obstacles placed according to the level design
- The game sets up the UI showing moves remaining and goals (which obstacles to destroy)
- The screen fades in and gameplay begins

### 3. Player Makes a Move

When a player taps a cube:
- The game detects which cube was tapped
- It finds all connected cubes of the same color (matching group)
- If 2+ cubes match:
  - The game uses one move
  - If 4+ cubes match, a rocket is created (cubes animate together, then a rocket appears)
  - Otherwise, the matching cubes are destroyed (with particles and sound effects)
  - Any obstacles adjacent to the match take damage
  - The game checks if all obstacles are cleared (win condition)
  - The game checks if moves are exhausted (lose condition)
- Gravity causes remaining cubes to fall down
- New cubes spawn from the top to fill empty spaces
- The game unlocks input for the next move

### 4. Rocket Activation

When a player taps a rocket:
- The rocket explodes, clearing an entire row (horizontal rocket) or column (vertical rocket)
- If two rockets are adjacent, they create a combo explosion (3x3 area)
- All cubes and obstacles in the explosion path are destroyed/damaged
- If another rocket is hit by the explosion, it triggers a chain reaction
- Gravity refills the board
- Win/lose conditions are checked

### 5. Obstacle Destruction

Obstacles (boxes, stones, vases) are destroyed by:
- Being adjacent to a cube match (takes 1 damage)
- Being hit by a rocket explosion (takes 1 damage)
- Different obstacles have different health (boxes: 1 hit, stones: 2 hits, vases: 2 hits)
- When an obstacle is destroyed, the goal counter decreases
- When all obstacles are cleared, the player wins

### 6. Level Completion

**Winning:**
- When all obstacles are destroyed, the game:
  - Plays victory music and sound
  - Saves progress (marks level as completed)
  - Shows win screen
  - Player can proceed to next level or return to menu

**Losing:**
- When moves run out and obstacles remain, the game:
  - Plays defeat music and sound
  - Shows lose screen
  - Player can retry the level or return to menu

### 7. Progress Saving

The game automatically saves:
- Current level progress
- Which levels have been completed
- This data persists between game sessions

---

## Key Game Systems

### Board Management
- The game board is a grid of cells
- Each cell can contain a cube, rocket, obstacle, or be empty
- The board has a "buffer zone" above the visible area where new cubes spawn
- When cubes fall, they animate smoothly to their new positions

### Matching System
- Players tap a cube to find matching groups
- Matching means connected cubes of the same color
- Groups of 2-3 cubes are destroyed normally
- Groups of 4+ cubes create a rocket

### Special Features
- **Rockets:** Clear entire rows or columns
- **Combos:** Two adjacent rockets create a 3x3 explosion
- **Chain Reactions:** Rockets can trigger other rockets
- **Visual Effects:** Particles, animations, and sounds provide feedback

### User Interface
- Top bar shows: current level number, moves remaining, goal items (obstacles to destroy)
- Goal items show how many of each obstacle type remain
- Popups appear for win/lose states

---

## Technical Highlights (Simplified)

- **Performance:** Uses object pooling to reuse game objects (cubes, particles) instead of creating/destroying them constantly
- **Smooth Animations:** All movements (falling, explosions) are animated smoothly using curves
- **Event System:** Game systems communicate through events, keeping code organized
- **Modular Design:** Different systems (matching, gravity, rockets) are separate and can be modified independently

---

## Player Experience Flow

1. **Main Menu** → Player selects level
2. **Level Load** → Board appears, goals shown
3. **Gameplay Loop:**
   - Player taps cubes
   - Matches are found and processed
   - Board refills with gravity
   - Repeat until win or lose
4. **Result Screen** → Win or lose popup
5. **Next Action** → Continue to next level, retry, or return to menu

---

**Note:** This summary describes the high-level flow. For detailed technical implementation, see the full Technical Report.

