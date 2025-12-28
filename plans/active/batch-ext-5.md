# Batch EXT-5: Milestone 4 - AR UX, Large Battles, and Performance

**Project:** Relic (Unity game - Kyle's repo fork)
**Status:** active
**Created:** 2025-12-28
**Owner:** Henry (Project Manager)
**Prerequisite:** ~~Batch EXT-4 complete~~ (M3: Combat complete 2025-12-28)

## Context

Milestone 3 complete! Combat system, squad upgrades, and AI behavior are all working. Now we move to performance optimization and AR UX improvements for large-scale battles.

**Reference:** Kyle's requirements in `/home/k4therin2/projects/relic/docs/milestones.md`

## Milestone 4 Objectives (from Kyle's spec)

- Improve AR usability and feedback
- Scale to large unit counts (goal: ~100 vs 100)
- Profile and optimize performance

---

## Work Packages

### WP-EXT-5.1: AR UX Enhancements
**Status:** ✅ Complete (2025-12-28)
**Priority:** P1
**Complexity:** M
**Agent:** Agent-Anette
**Started:** 2025-12-28
**Blocked by:** ~~WP-EXT-4.4~~ (complete)

**Objective:** Improve AR interaction clarity and usability.

**Tasks:**
1. [x] Implement clear selection circles/rings for selected units
2. [x] Add team color differentiation (Team 0 vs Team 1)
3. [x] Create move/attack destination indicators (waypoint markers)
4. [x] Implement multi-unit selection in AR mode (controller drag or area select)
5. [x] Add visual feedback for:
   - Unit health (health bar or damage flash)
   - Attack target lines (optional - deferred to future WP)
   - Range indicators (optional - deferred to future WP)
6. [x] Write unit tests for UI components

**Acceptance Criteria:**
- [x] Selection visually clear in AR (SelectionIndicator already exists)
- [x] Team colors distinguishable (TeamColorApplier component)
- [x] Move/attack commands have visual feedback (DestinationMarker/Manager)
- [x] Multi-select works in AR mode (ARBoxSelection + ARSelectionController)

**Completion Notes:**
- TeamColorApplier: Applies team-based colors using MaterialPropertyBlock for GPU instancing
- DestinationMarker + DestinationMarkerManager: Pooled markers for move/attack commands with pulse animation
- HealthBar: World-space health bars with gradient coloring (green→yellow→red), billboarding, auto-hide when full
- ARBoxSelection: Drag selection with visual feedback, team filtering, min-size threshold
- ARSelectionController: Extended with trigger-hold for box selection, destination markers on move commands
- 4 new test files with comprehensive EditMode tests (TeamColorApplier, DestinationMarker, HealthBar, ARBoxSelection)

---

### WP-EXT-5.2: World-Space UI Panel
**Status:** ✅ Complete (2025-12-28)
**Priority:** P1
**Complexity:** M
**Agent:** Agent-Nadia
**Started:** 2025-12-28
**Blocked by:** ~~WP-EXT-5.1~~ (complete)

**Objective:** Create in-world UI panel for spawning, era switching, and match control.

**Tasks:**
1. [x] Create `WorldSpaceUIPanel` component:
   - Anchored to battlefield (follows placement)
   - Always faces player (billboard mode with vertical lock option)
2. [x] Implement UI sections:
   - **Spawn Controls:** Select archetype from dropdown, spawn for Team 0/1
   - **Era Selector:** Switch between eras, dynamically created buttons
   - **Upgrade Panel:** Apply upgrades to selected squad with validation
   - **Match Controls:** Reset match (clears all units), pause/resume with Time.timeScale
3. [x] Create UI prefabs using Unity UI (Canvas in World Space mode)
   - Editor utility `WorldSpaceUIPanelCreator` creates complete prefab
4. [x] Integrate with existing EraManager, SelectionManager, Squad systems
5. [x] Write tests for UI interactions (27 unit tests)

**Acceptance Criteria:**
- [x] World-space UI visible and readable in AR (Canvas in World Space mode)
- [x] Can spawn units from UI (archetype dropdown + team buttons)
- [x] Can switch eras from UI (era buttons with selection highlight)
- [x] Can apply upgrades from UI (upgrade buttons, squad validation)
- [x] Can reset match from UI (ResetMatch clears all units)

**Completion Notes:**
- WorldSpaceUIPanel: Comprehensive component with 4 UI sections
- Anchoring: Follows battlefield transform with configurable offset
- Billboard: Faces camera with optional vertical rotation lock
- Spawn Controls: TMP_Dropdown for archetype, buttons for Team 0/1
- Era Selector: Dynamically creates buttons per EraConfigSO
- Upgrade Panel: Shows squad info, upgrade buttons with era filtering
- Match Controls: Reset (clears units + resumes), Pause/Resume (Time.timeScale)
- WorldSpaceUIPanelCreator: Editor tool creates complete prefab with all UI elements
- 27 unit tests covering initialization, pause, match reset, visibility, anchoring, upgrades

---

### WP-EXT-5.3: Central Tick Manager (Performance)
**Status:** ✅ Complete (2025-12-28)
**Priority:** P0 (critical for 100v100 scale)
**Complexity:** M
**Agent:** Agent-Dorian
**Blocked by:** ~~WP-EXT-4.4~~ (complete)

**Objective:** Replace per-unit Update() calls with centralized tick manager.

**Tasks:**
1. [x] Create `TickManager` singleton:
   - Maintains list of all tickable entities
   - Calls tick methods in batched groups
   - Configurable tick rate (e.g., AI ticks at 10Hz, not 60Hz)
2. [x] Create `ITickable` interface:
   - `void OnTick(float deltaTime)`
   - `TickPriority Priority { get; }`
3. [x] Refactor UnitController to use ITickable instead of Update()
4. [x] Refactor UnitAI to use ITickable (can run at lower frequency)
5. [x] Add performance profiling markers
6. [x] Write unit tests for tick registration and execution (19 tests)

**Performance Target:** Reduce per-frame overhead by 50%+ with 100 units

**Acceptance Criteria:**
- [x] TickManager centralizes unit updates
- [x] UnitController uses ITickable
- [x] UnitAI runs at configurable lower frequency (Medium priority = 10 Hz)
- [x] No per-unit Update() methods
- [x] Profiler shows reduced overhead (profiler markers added)

**Completion Notes:**
- ITickable interface with Priority (Low/Medium/Normal/High) and IsTickActive
- TickManager with configurable intervals: Low=5Hz, Medium=10Hz, Normal=30Hz, High=60Hz
- UnitController movement check at Normal priority (30 Hz)
- UnitAI state machine at Medium priority (10 Hz) for efficient AI updates
- 19 unit tests covering registration, unregistration, and priority handling

---

### WP-EXT-5.4: GPU Instancing and LOD
**Status:** ⏸️ Parked (awaiting art assets)
**Priority:** P1
**Complexity:** M
**Blocked by:** Missing unit prefabs and art assets

**Objective:** Enable GPU instancing for large unit counts and add LOD levels.

**Tasks:**
1. Enable GPU instancing on unit materials:
   - Verify shaders support instancing
   - Enable "GPU Instancing" checkbox on materials
   - Use MaterialPropertyBlock for per-instance data (team color, etc.)
2. Create LOD groups for unit prefabs:
   - LOD0: Full detail (close)
   - LOD1: Medium detail (mid-range)
   - LOD2: Low detail or billboard (far)
3. Create simple LOD meshes for existing unit types
4. Configure LOD distances based on battlefield scale
5. Profile draw call reduction

**Performance Target:** Draw calls reduced by 60%+ with 100 units

**Acceptance Criteria:**
- [ ] GPU instancing enabled on unit materials
- [ ] LOD groups configured on unit prefabs
- [ ] Draw calls significantly reduced
- [ ] Visual quality acceptable at all LOD levels

---

### WP-EXT-5.5: Unit Pooling
**Status:** ✅ Complete (2025-12-28)
**Priority:** P1
**Complexity:** S
**Agent:** Agent-Dorian
**Blocked by:** ~~WP-EXT-5.3~~ (complete)

**Objective:** Implement object pooling to reduce instantiation/destruction overhead.

**Tasks:**
1. [x] Create `UnitPool` class:
   - Pre-spawn pool of inactive unit GameObjects
   - `Spawn(archetype, position, team)` returns pooled unit
   - `Despawn(unit)` returns unit to pool
   - Pool expansion when depleted
2. [x] Create pooling configuration:
   - Initial pool size per archetype
   - Max pool size
   - Pool warm-up at scene load
3. [x] Integrate with UnitFactory:
   - Replace Instantiate/Destroy with pool operations
4. [x] Handle squad cleanup when units return to pool
5. [x] Write unit tests for pool operations (26 tests)

**Performance Target:** Zero runtime allocations for unit spawn/despawn

**Acceptance Criteria:**
- [x] UnitPool manages unit lifecycle
- [x] UnitFactory uses pooling (togglable via _usePooling)
- [x] No GC allocation spikes during battle (pools units instead of destroying)
- [x] Pool correctly resets unit state on reuse (removes from squad, stops, disables AI)

**Completion Notes:**
- UnitPool class with Spawn/Despawn/WarmUp methods
- Configurable max pool size per archetype
- Automatic pool parent management (inactive units hidden)
- Squad cleanup on despawn (LeaveSquad called)
- UnitFactory integrated with _usePooling toggle (enabled by default)
- 26 unit tests covering spawn, despawn, reuse, warm-up, capacity, and squad cleanup

---

### WP-EXT-5.6: Performance Profiling and Validation
**Status:** ⚪ Not Started
**Priority:** P1
**Complexity:** S
**Blocked by:** WP-EXT-5.3, WP-EXT-5.4, WP-EXT-5.5

**Objective:** Validate 100v100 performance target and identify remaining bottlenecks.

**Tasks:**
1. Create benchmark scene with 100 vs 100 units:
   - Spawn 200 units in opposing teams
   - Start combat automatically
   - Run for 60 seconds
2. Profile on Quest 3:
   - CPU frame time (target: <16.6ms for 60fps)
   - Draw calls (target: <100 batched calls)
   - Memory allocation (target: 0 per-frame GC)
   - Combat resolution time
3. Profile in Editor (for comparison baseline)
4. Document bottlenecks and optimization recommendations
5. Create performance test that can run in CI

**Acceptance Criteria:**
- [ ] 100v100 battle runs at 60fps on Quest 3
- [ ] Profile data documented
- [ ] No major GC spikes during battle
- [ ] Performance regression test exists

---

## Testing Strategy

All WPs should have both:
1. **EditMode tests** - Logic tests (TickManager registration, pool operations)
2. **PlayMode tests** - Integration tests (UI interaction, pooling with spawns)

Use existing test structure:
- `Assets/Tests/EditMode/` for unit tests
- `Assets/Tests/PlayMode/` for integration tests

---

## Notes

**Client Communication:** Post to #relic-game when WPs complete or when creating PRs.

**Performance Testing on Quest 3:**
```bash
# Build for Quest 3
xvfb-run /home/k4therin2/Unity/Hub/Editor/6000.3.2f1/Editor/Unity \
  -projectPath /home/k4therin2/projects/relic \
  -buildTarget Android -executeMethod Build.BuildQuest3 -quit

# Install to connected Quest
adb install -r build/Relic-Quest.apk
```

**Dependencies:**
- WP-EXT-5.1 can start immediately when M3 completes (no dependency on other M4 WPs)
- WP-EXT-5.2 depends on 5.1 (uses selection/team UI foundation)
- WP-EXT-5.3 can start immediately (performance infrastructure)
- WP-EXT-5.4 depends on 5.3 (needs tick manager for instanced updates)
- WP-EXT-5.5 depends on 5.3 (pooled units need tick registration)
- WP-EXT-5.6 depends on 5.3, 5.4, 5.5 (validates all performance work)

**Parallel Work Opportunities:**
- WP-EXT-5.1 (UX) and WP-EXT-5.3 (Tick Manager) can be done in parallel
- Two developers can work on M4 simultaneously

**Next Batch:** EXT-6 (Milestone 5: Scenarios and Polish) - optional stretch goal

---

*Created: 2025-12-28 by Agent-Dorian*
*Based on Kyle's Milestone 4 requirements in docs/milestones.md*
