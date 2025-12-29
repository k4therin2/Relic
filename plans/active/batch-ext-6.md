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
**Status:** Not Started
**Priority:** P2
**Complexity:** M
**Agent:** (unassigned)
**Blocked by:** WP-EXT-6.2

**Objective:** Enable basic RTS controls for unit selection and movement.

**Tasks:**
1. [ ] Integrate `SelectionManager` (from WP-EXT-3.3):
   - Reuse existing SelectionManager singleton
   - Create `DebugSelectionController` (if not already exists)
   - Implement mouse-based selection:
     - Left-click to select single unit (raycast)
     - Click-drag for box selection (optional: defer to future WP)
2. [ ] Implement visual selection feedback:
   - Reuse `SelectionIndicator` from WP-EXT-3.3
   - Add selection ring/circle under selected units
   - Highlight selected units (outline or glow effect - optional)
3. [ ] Integrate `CommandManager` (from WP-EXT-3.2):
   - Right-click to issue move command
   - Create `MoveCommand` using NavMesh pathfinding
   - Add destination marker (reuse DestinationMarker from WP-EXT-5.1 if available)
4. [ ] Write unit tests for selection and commands (10-15 tests)

**Acceptance Criteria:**
- [ ] Left-click selects unit (raycast hit detection)
- [ ] Selected units show visual feedback (SelectionIndicator)
- [ ] Right-click moves selected units to clicked position
- [ ] Units pathfind using NavMesh to destination

**Dependencies:** WP-EXT-6.2 (needs unit prefabs)

---

### WP-EXT-6.4: Unit Spawner UI
**Status:** Not Started
**Priority:** P2
**Complexity:** S
**Agent:** (unassigned)
**Blocked by:** WP-EXT-6.3

**Objective:** Create simple UI for spawning units in debug scene.

**Tasks:**
1. [ ] Create spawner UI (Screen Space or World Space Canvas):
   - "Spawn Team 0" button (spawns 5-10 units on left side)
   - "Spawn Team 1" button (spawns 5-10 units on right side)
   - "Clear All" button (destroys all units)
   - Optional: Slider for unit count
2. [ ] Integrate with `UnitFactory` (from WP-EXT-3.4):
   - Reuse existing UnitFactory for instantiation
   - Use UnitPool if available (WP-EXT-5.5)
   - Define spawn positions (grid or random within bounds)
3. [ ] Create `DebugSpawnerController`:
   - Handles button clicks
   - Spawns units at opposite sides of battlefield
   - Clears all units on "Clear All"
4. [ ] Position spawned units:
   - Team 0: Left side of scene (X < 0)
   - Team 1: Right side of scene (X > 0)
   - Slight randomization to prevent overlap
5. [ ] Write unit tests for spawner logic (5-10 tests)

**Acceptance Criteria:**
- [ ] UI buttons visible and responsive
- [ ] "Spawn Team 0" creates 5-10 units on left side
- [ ] "Spawn Team 1" creates 5-10 units on right side
- [ ] "Clear All" removes all units from scene
- [ ] Units spawn without overlapping

**Dependencies:** WP-EXT-6.3 (needs selection/movement working)

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
