# Batch EXT-3: Milestone 2 - Core RTS Skeleton

**Project:** Relic (Unity game - Kyle's repo fork)
**Status:** active
**Created:** 2025-12-28
**Owner:** Henry (Project Manager)
**Prerequisite:** Batch EXT-2 complete (M1: AR Tabletop Foundation delivered)

## Context

Milestone 1 is complete! Unity project, scenes, AR battlefield placement, and era configs are all working. Now we implement the Core RTS systems from Kyle's Milestone 2.

**Reference:** Kyle's requirements in `/home/k4therin2/projects/relic/docs/milestones.md`

## Milestone 2 Objectives (from Kyle's spec)

- Establish core unit and command systems
- Implement selection and movement
- Work in both AR and debug scenes

---

## Work Packages

### WP-EXT-3.1: Unit Data Architecture
**Status:** ✅ Complete (2025-12-28)
**Priority:** P0 (blocking other M2 work)
**Complexity:** S
**Agent:** Agent-Anette

**Objective:** Create the foundational unit data types and archetypes.

**Tasks:**
1. Create `UnitArchetypeSO` ScriptableObject with:
   - id (unique identifier)
   - health (max HP)
   - moveSpeed (units per second)
   - weapon reference (placeholder for M3)
   - unit prefab reference
2. Create `UnitController` MonoBehaviour:
   - Health tracking (current/max)
   - Movement state
   - Squad reference (placeholder)
   - Team/faction identifier
3. Create 4 sample archetypes (one per era):
   - Ancient: Legionnaire
   - Medieval: Knight
   - WWII: Rifleman
   - Future: Drone
4. Create base unit prefab with UnitController
5. Write unit tests for UnitArchetype/UnitController

**Implementation Progress:**
- [x] UnitArchetypeSO.cs - ScriptableObject with stats, validation, CreateStats()
- [x] UnitStats struct - Runtime stats with damage/heal methods
- [x] UnitController.cs - MonoBehaviour with health, movement, selection, squad
- [x] UnitArchetypeTests.cs - 22 unit tests for archetype and stats
- [x] UnitControllerTests.cs - 20 unit tests for controller behavior
- [x] UnitArchetypeCreator.cs - Editor tool to create 4 sample archetypes
- [x] UnitPrefabCreator.cs - Editor tool to create base unit prefab
- [x] Assembly definitions updated (Relic.CoreRTS, tests, editor)

**Acceptance Criteria:**
- [x] UnitArchetype ScriptableObject created
- [x] UnitController MonoBehaviour created
- [x] 4 sample archetypes (via Editor menu: Relic > Create Sample Archetypes)
- [x] Unit prefab (via Editor menu: Relic > Create Base Unit Prefab)
- [x] Unity compiles without errors (verified via batchmode)

---

### WP-EXT-3.2: Unit Movement System
**Status:** ✅ Complete (2025-12-28)
**Priority:** P1
**Complexity:** M
**Agent:** Agent-Anette

**Objective:** Implement NavMesh-based unit movement.

**Implementation Progress:**
- [x] Command.cs: Base Command + MoveCommand, StopCommand, AttackCommand (stub)
- [x] CommandQueue.cs: MonoBehaviour for command execution
  - Issue() replaces current (left-click), Queue() adds (shift+click)
  - Events: OnCommandStarted, OnCommandCompleted, OnQueueEmpty
- [x] NavMeshSetupUtility.cs: Editor menu for NavMesh setup
  - Mark objects as Navigation Static
  - Add NavMeshObstacle to obstacles
  - Create test NavMesh scene
- [x] CommandTests.cs: 20 unit tests
- [x] CommandQueueTests.cs: 18 unit tests
- [x] UnitController already had MoveTo()/Stop() from WP-EXT-3.1

**Acceptance Criteria:**
- [x] Units move using NavMesh pathfinding (via UnitController.MoveTo)
- [x] Units stop on command (via UnitController.Stop)
- [x] Movement works via Command pattern (MoveCommand, StopCommand)
- [x] Unity compiles without errors (verified via batchmode)

---

### WP-EXT-3.3: Selection System
**Status:** ⚪ Available
**Priority:** P1
**Complexity:** M
**For:** Developer
**Unblocked:** WP-EXT-3.1 complete

**Objective:** Implement unit selection for both AR and debug modes.

**Tasks:**
1. Create `SelectionManager` singleton:
   - Current selection list
   - Selection events (OnSelectionChanged)
   - Select/deselect methods
   - Clear selection
2. Create **Flat Debug selection** (mouse input):
   - Click to select single unit
   - Shift+click to add to selection
   - Drag rectangle for multi-select
   - Click empty to deselect all
3. Create **AR selection** (controller raycast):
   - XR controller raycast to unit
   - Trigger to select
   - Grip to add to selection
   - Raycast to battlefield for move command
4. Create visual selection indicators:
   - Selection circle/ring under unit
   - Highlight material or outline
5. Write unit tests for selection logic

**Acceptance Criteria:**
- [ ] Single unit selection works
- [ ] Multi-selection works (drag box in debug)
- [ ] AR controller selection works
- [ ] Selection visuals display correctly
- [ ] Tests pass

---

### WP-EXT-3.4: Unit Spawning System
**Status:** ✅ Complete (2025-12-28)
**Priority:** P1
**Complexity:** S
**Agent:** Agent-Dorian

**Objective:** Create unit spawning from archetypes.

**Tasks:**
1. Create `UnitFactory`:
   - SpawnUnit(archetype, position, team) method
   - Uses archetype's prefab reference
   - Applies archetype stats to UnitController
   - Assigns team/faction
2. Create `SpawnPoint` component:
   - Team assignment
   - Spawn radius
   - Spawn direction
3. Integrate with BattlefieldRoot:
   - Reference spawn points (red/blue)
   - Quick-spawn buttons for testing
4. Create spawn UI for testing:
   - Dropdown to select archetype
   - Button to spawn at spawn point
5. Write unit tests for spawning

**Implementation Progress:**
- [x] UnitFactory.cs - Factory with SpawnUnit, SpawnAtPoint, unit tracking, cleanup
- [x] SpawnPoint.cs - Component with team ID, spawn radius, gizmo visualization
- [x] SpawnTestingUI.cs - UI for testing spawns (archetype dropdown, spawn buttons)
- [x] SpawnPointSetupUtility.cs - Editor tool to add SpawnPoints to battlefield
- [x] SpawnTestingUICreator.cs - Editor tool to create testing UI in scene
- [x] UnitFactoryTests.cs - 18 unit tests for factory
- [x] SpawnPointTests.cs - 11 unit tests for spawn point
- [x] Updated UILayer.asmdef to reference CoreRTS

**Acceptance Criteria:**
- [x] Units spawn from archetypes
- [x] Units spawn at correct spawn points
- [x] Spawned units have correct stats
- [x] Unity compiles (tests need runtime verification)

---

## Testing Strategy

All WPs should have both:
1. **EditMode tests** - Logic tests that don't require Play mode
2. **PlayMode tests** - Integration tests that verify Unity behavior

Use existing test structure:
- `Assets/Tests/EditMode/` for unit tests
- `Assets/Tests/PlayMode/` for integration tests

---

## Notes

**Client Communication:** Post to #relic-game when WPs complete or when creating PRs.

**Batchmode Testing:**
```bash
# Run EditMode tests
xvfb-run /path/to/Unity -projectPath /home/k4therin2/projects/relic \
  -batchmode -runTests -testPlatform EditMode -quit

# Run PlayMode tests
xvfb-run /path/to/Unity -projectPath /home/k4therin2/projects/relic \
  -batchmode -runTests -testPlatform PlayMode -quit
```

**Dependencies:**
- WP-EXT-3.1 must complete first (defines core data types)
- WP-EXT-3.2 and 3.3 can run in parallel after 3.1
- WP-EXT-3.4 is independent

**Next Batch:** EXT-4 (Milestone 3: Combat, Elevation, Squad Upgrades) - blocked until this batch complete.

---

*Created: 2025-12-28 by Agent-Anette*
*Based on Kyle's Milestone 2 requirements in docs/milestones.md*
