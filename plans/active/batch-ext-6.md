# Batch EXT-6: Playable Flat Debug Demo

**Project:** Relic (Unity game - Kyle's repo fork)
**Status:** active
**Created:** 2025-12-28
**Owner:** Henry (Project Manager)
**Prerequisite:** Batch EXT-3 complete (Core RTS Skeleton exists)

## Context

Create a flat (non-AR) debug scene for rapid testing and development. This provides a traditional RTS camera view for easier debugging without needing VR hardware, and serves as a testbed for game mechanics before AR integration.

**Reference:** Kyle's request via Slack (colby-agent-work, 2025-12-28)

## Batch Objectives

- Create playable debug scene with traditional RTS camera
- Implement minimal viable unit controls (select, move, spawn)
- Enable rapid iteration without AR headset
- Provide testbed for new features before AR integration

---

## Work Packages

### WP-EXT-6.1: Flat Debug Scene Setup
**Status:** Complete (2025-12-29)
**Priority:** P2
**Complexity:** S
**Agent:** Nadia
**Blocked by:** None

**Objective:** Create basic scene infrastructure for flat debug mode.

**Tasks:**
1. [x] Analyze existing scripts in `/Assets/Relic/Scripts/`:
   - Reviewed Core, CoreRTS, ARLayer, Data, UILayer directories
   - Identified reusable: UnitController, SelectionManager, DebugSelectionController, UnitFactory, TickManager
   - Document dependencies and integration points
2. [x] Create `Flat_Debug.unity` scene in `/Assets/Scenes/`:
   - Created editor menu: Relic > Debug > Create Flat Debug Scene
   - Adds ground plane (100x100, NavMesh-ready)
   - Configures lighting (directional light, ambient)
   - Sets up RTS-style camera (perspective, 45° top-down view)
   - NavMesh baking via Relic > Debug > Bake NavMesh
3. [x] Create `DebugCameraController` component:
   - WASD + arrow key panning
   - Mouse wheel + Q/E for zoom
   - Middle-mouse drag for pan
   - Camera bounds to prevent flying off battlefield
   - Shift for fast pan
4. [x] Write unit tests for camera controller (23 tests)

**Acceptance Criteria:**
- [x] Flat_Debug.unity scene creator exists (via menu)
- [x] Ground plane has NavMesh baking support
- [x] Camera responds to keyboard/mouse controls
- [x] Scene has proper lighting for visibility

**Implementation Notes:**
- Use `Relic > Debug > Create Flat Debug Scene` menu to create scene
- Use `Relic > Debug > Bake NavMesh for Current Scene` to bake NavMesh
- Use `Relic > Debug > Validate Scene Setup` to verify configuration
- 23 unit tests passing for DebugCameraController

**Dependencies:** None (uses existing Core/CoreRTS scripts)

---

### WP-EXT-6.2: Minimal Viable Units
**Status:** Complete (2025-12-29)
**Priority:** P2
**Complexity:** S
**Agent:** Nadia
**Blocked by:** WP-EXT-6.1

**Objective:** Create basic unit prefabs for debug scene testing.

**Tasks:**
1. [x] Create unit prefab for debug mode:
   - Capsule-based visual with UnitController, NavMeshAgent, TeamColorApplier
   - Editor menu: Relic > Debug > Create Debug Unit Prefab
   - Creates prefab at Assets/Prefabs/Debug/DebugUnit.prefab
   - Creates archetype at Assets/Data/Archetypes/DebugUnitArchetype.asset
2. [x] Implement team color differentiation:
   - Reuses TeamColorApplier from WP-EXT-5.1
   - Team 0 = Red, Team 1 = Blue (configurable)
   - MaterialPropertyBlock for GPU instancing compatibility
3. [x] Verify unit behavior in flat scene:
   - Editor menu: Relic > Debug > Spawn Test Units in Scene
   - Spawns 5 units per team (10 total)
   - Units positioned on left/right sides of battlefield
4. [x] Write unit tests for prefab instantiation (17 tests)

**Acceptance Criteria:**
- [x] Unit prefab created via menu (Assets/Prefabs/Debug/)
- [x] Units can be instantiated in Flat_Debug scene
- [x] Team colors clearly distinguish Team 0 (red) vs Team 1 (blue)
- [x] NavMeshAgent configured for pathfinding

**Implementation Notes:**
- Menu: Relic > Debug > Create Debug Unit Prefab
- Menu: Relic > Debug > Create Debug Unit Archetype
- Menu: Relic > Debug > Spawn Test Units in Scene
- 17 unit tests passing for prefab components and team colors

**Dependencies:** WP-EXT-6.1 (needs NavMesh scene)

---

### WP-EXT-6.3: Selection and Movement System
**Status:** Complete (2025-12-29)
**Priority:** P2
**Complexity:** M
**Agent:** Nadia
**Blocked by:** WP-EXT-6.2

**Objective:** Enable basic RTS controls for unit selection and movement.

**Tasks:**
1. [x] Integrate `SelectionManager` (from WP-EXT-3.3):
   - Reuses existing SelectionManager singleton
   - DebugSelectionController already exists with full functionality
   - Left-click to select, shift+click to add, box-drag for multi-select
2. [x] Implement visual selection feedback:
   - Added SelectionIndicator to debug unit prefab
   - Shows ring/circle under selected units
   - Colors: green for friendly, red for enemy, yellow for hover
3. [x] Integrate movement commands:
   - Right-click to issue move command via DebugSelectionController
   - Units use NavMeshAgent for pathfinding
   - Added DestinationMarkerManager to scene for move command feedback
4. [x] Write unit tests (18 tests total for debug unit prefab)

**Acceptance Criteria:**
- [x] Left-click selects unit (raycast hit detection)
- [x] Selected units show visual feedback (SelectionIndicator)
- [x] Right-click moves selected units to clicked position
- [x] Units pathfind using NavMesh to destination

**Implementation Notes:**
- All selection/movement code was already implemented in WP-EXT-3.2/3.3
- WP-EXT-6.3 integrated existing systems into the debug scene
- SelectionIndicator added to unit prefab
- DestinationMarkerManager added to scene managers

**Dependencies:** WP-EXT-6.2 (needs unit prefabs)

---

### WP-EXT-6.4: Unit Spawner UI
**Status:** Complete (2025-12-29)
**Priority:** P2
**Complexity:** S
**Agent:** Nadia
**Blocked by:** WP-EXT-6.3

**Objective:** Create simple UI for spawning units in debug scene.

**Tasks:**
1. [x] Create spawner UI (Screen Space Canvas):
   - "Spawn Team 0" (red) button spawns 5 units on left side
   - "Spawn Team 1" (blue) button spawns 5 units on right side
   - "Clear All" button destroys all units
   - Unit count display shows current totals
2. [x] Integrate with `UnitFactory`:
   - Uses existing UnitFactory for instantiation
   - Falls back to manual instantiation if factory unavailable
   - Grid-based spawn with randomization
3. [x] Created `DebugSpawnerUI` component:
   - Handles button clicks
   - Spawns units at configurable positions
   - Max units per team (default 20)
   - Clears all units on "Clear All"
4. [x] Position spawned units:
   - Team 0: Left side (X = -15)
   - Team 1: Right side (X = 15)
   - Grid layout with small random offset
5. [x] Write unit tests (20 tests)

**Acceptance Criteria:**
- [x] UI buttons visible and responsive
- [x] "Spawn Team 0" creates 5 units on left side (configurable)
- [x] "Spawn Team 1" creates 5 units on right side (configurable)
- [x] "Clear All" removes all units from scene
- [x] Units spawn without overlapping (grid + random offset)

**Implementation Notes:**
- Scene setup automatically creates Spawner Canvas with buttons
- DebugSpawnerUI component configurable via Inspector
- 20 unit tests passing for spawner logic

**Dependencies:** WP-EXT-6.3 (needs selection/movement working)

---

### WP-EXT-6.5: Quick Setup Utility (One-Click Playable Demo)
**Status:** Complete (2025-12-29)
**Priority:** P2
**Complexity:** S
**Agent:** Nadia
**Blocked by:** WP-EXT-6.4
**Source:** Backlog (Slack #relic-game, routed by Grace 2025-12-29)

**Objective:** Create a menu command that auto-generates a playable demo scene with pre-positioned units ready for combat.

**Tasks:**
1. [x] Create Editor menu item `Relic > Demo > Quick Combat Demo Setup`
2. [x] Auto-create or load Flat_Debug scene
3. [x] Spawn 15 units per team in combat positions
4. [x] Position teams facing each other for immediate action
5. [x] Verify all managers initialized (SelectionManager, CommandManager, etc.)
6. [x] Auto-enter Play Mode after setup
7. [x] Write unit tests for setup utility (19 tests)

**Acceptance Criteria:**
- [x] Single menu click creates complete playable demo
- [x] Units from both teams positioned for combat
- [x] Play Mode entered automatically
- [x] User can immediately select and command units
- [x] Works even if no scene was open

**Implementation Notes:**
- `Relic > Demo > Quick Combat Demo Setup` - Full setup with Play Mode
- `Relic > Demo > Quick Combat Demo (No Play Mode)` - Setup without Play Mode
- `Relic > Demo > Validate Demo Setup` - Validates scene components
- Configurable via `DemoConfig` struct: UnitsPerTeam, spawn positions, spacing
- 19 unit tests passing for configuration and formation logic

**Files Created:**
- `Assets/Relic/Scripts/CoreRTS/Editor/QuickDemoSetup.cs`
- `Assets/Tests/EditMode/QuickDemoSetupTests.cs`
- Updated `Assets/Tests/EditMode/Relic.Tests.EditMode.asmdef` (added Relic.CoreRTS.Editor reference)

**Dependencies:** WP-EXT-6.4 (needs complete debug scene infrastructure)

---

## Testing Strategy

All WPs should have both:
1. **EditMode tests** - Logic tests (camera controller, spawner logic)
2. **PlayMode tests** - Integration tests (scene loading, unit behavior, UI interaction)

Use existing test structure:
- `Assets/Tests/EditMode/Debug/` for unit tests
- `Assets/Tests/PlayMode/Debug/` for integration tests

---

## Integration Notes

**Reuse Existing Systems:**
- `UnitController` (CoreRTS) - Unit behavior and stats
- `SelectionManager` (WP-EXT-3.3) - Selection logic
- `CommandManager` (WP-EXT-3.2) - Movement commands
- `UnitFactory` (WP-EXT-3.4) - Unit instantiation
- `TeamColorApplier` (WP-EXT-5.1) - Team color visualization
- `DestinationMarker` (WP-EXT-5.1) - Move command feedback
- `UnitPool` (WP-EXT-5.5) - Object pooling (if available)

**New Components:**
- `DebugCameraController` - RTS camera controls
- `DebugSelectionController` - Mouse-based selection (may already exist)
- `DebugSpawnerController` - UI-based unit spawning

**Scene Structure:**
```
Flat_Debug.unity
├── Lighting
│   └── Directional Light
├── Ground
│   └── Plane (with NavMesh)
├── Camera
│   └── Main Camera (DebugCameraController)
├── UI
│   └── Canvas (DebugSpawnerController)
└── Managers (from DontDestroyOnLoad)
    ├── SelectionManager
    ├── CommandManager
    └── UnitFactory
```

---

## Dependencies

**Batch Dependencies:**
- Batch EXT-3 complete (Core RTS Skeleton) - provides UnitController, SelectionManager, CommandManager
- Batch EXT-5 (partial) - optional enhancements (TeamColorApplier, DestinationMarker, UnitPool)

**WP Dependencies:**
- WP-EXT-6.1: None (can start immediately)
- WP-EXT-6.2: WP-EXT-6.1 (needs scene + NavMesh)
- WP-EXT-6.3: WP-EXT-6.2 (needs unit prefabs)
- WP-EXT-6.4: WP-EXT-6.3 (needs working selection/movement)

**Parallel Work Opportunities:**
- All WPs are sequential (each depends on previous)
- Single developer can complete batch in 1-2 days

---

## Acceptance Criteria (Batch Complete)

- [ ] Flat_Debug.unity scene playable without VR headset
- [ ] Can spawn units for both teams via UI
- [ ] Can select units with mouse clicks
- [ ] Can move units with right-click commands
- [ ] Units navigate using NavMesh pathfinding
- [ ] Team colors clearly distinguish units
- [ ] Camera controls responsive and intuitive
- [ ] All unit tests passing

---

## Client Communication

Post to #relic-game when batch complete with demo video/GIF showing:
1. Camera controls (pan, zoom)
2. Unit spawning (both teams)
3. Unit selection (click to select)
4. Unit movement (right-click to move)

---

*Created: 2025-12-28 by Agent-Henry*
*Based on Kyle's Slack request for "Playable Flat Debug Demo"*
