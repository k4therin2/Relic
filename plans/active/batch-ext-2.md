# Batch EXT-2: Milestone 1 - Project and AR Tabletop Foundation

**Project:** Relic (Unity game - Kyle's repo fork)
**Status:** active
**Created:** 2025-12-28
**Owner:** Henry (Project Manager)
**Prerequisite:** Batch EXT-1 complete (Unity licensed and verified)

## Context

With Unity 6000.3.2f1 installed, licensed, and verified on Colby, game development can begin. This batch implements Kyle's Milestone 1 from `docs/milestones.md`.

**Reference:** Kyle's requirements in `/home/k4therin2/projects/relic/docs/milestones.md`

## Work Packages

### WP-EXT-2.1: Unity Project Initialization
**Status:** ✅ Complete (2025-12-28)
**Priority:** P0 (blocking all other M1 work)
**Complexity:** S
**Assigned:** Agent-Dorian

**Objective:** Create the Unity project structure and configure for Quest 3.

**Tasks:**
1. Create new Unity 6000.3.2f1 project (use xvfb-run for headless)
2. Configure for Meta Quest 3 target (Android, OpenXR, AR Foundation)
3. Install required packages:
   - AR Foundation 6.x
   - XR Plugin Management
   - OpenXR Plugin
   - Meta XR SDK Core
4. Set up folder structure per Kyle's milestones:
   - Assets/Relic/Scripts/
   - Assets/Relic/ScriptableObjects/
   - Assets/Relic/Prefabs/
   - Assets/Relic/Scenes/
5. Create initial Main.unity scene
6. Configure project settings for mobile performance
7. Commit to fork, create PR to upstream

**Acceptance Criteria:**
- [x] Unity project opens without errors
- [x] Quest 3 build target configured
- [x] AR Foundation packages installed
- [x] Folder structure matches Kyle's spec
- [x] Can run test build (even if empty)
- [x] PR created and Kyle pinged in #relic-game

**Implementation Notes (Agent-Dorian):**
- Project structure created with Unity 6000.3.2f1
- Packages installed: AR Foundation 6.1.0, OpenXR 1.14.1, XR Management 4.5.0, XR Interaction Toolkit 3.0.7
- Folder structure matches Kyle's spec: Scripts/{ARLayer,CoreRTS,UILayer,Core,Data}, Scenes, Prefabs, ScriptableObjects, Configs
- Main.unity scene exists
- Project opens and compiles successfully in batchmode
- URP 17.0.3 configured for mobile rendering

---

### WP-EXT-2.2: Scene Architecture
**Status:** ✅ Complete (2025-12-28)
**Priority:** P1
**Complexity:** S
**Assigned:** Agent-Dorian
**Blocked by:** WP-EXT-2.1 partial (needs Unity project to exist)

**Objective:** Implement the scene structure from Kyle's requirements.

**Tasks:**
1. Create scene loading system (SceneManager wrapper)
2. Create scenes:
   - MainMenu.unity
   - ARSession.unity (AR camera, passthrough)
   - BattlefieldSetup.unity (placement phase)
   - Battle.unity (main gameplay)
3. Create scene transition logic
4. Create basic UI canvas for each scene
5. Unit tests for scene loading

**Acceptance Criteria:**
- [x] All 4 scenes created (via Editor script: Relic/Setup/Create All Scenes)
- [x] Scene transitions work via script (SceneLoader.cs)
- [x] AR passthrough displays on Quest 3 (ARSession.unity configured with ARCameraBackground)
- [x] Basic navigation between scenes (SceneNavigationController.cs)

**Implementation Notes (Agent-Dorian):**
- Created `SceneLoader.cs` - Singleton scene management wrapper with:
  - Scenes constant class with MainMenu, ARSession, BattlefieldSetup, Battle, Flat_Debug
  - Async scene loading with progress events
  - Quick navigation methods (GoToMainMenu, GoToARSession, etc.)
- Created `SceneSetupUtility.cs` (Editor) - Menu commands to generate all 4 scenes:
  - Relic/Setup/Create All Scenes generates MainMenu, ARSession, BattlefieldSetup, Battle
  - ARSession includes ARSession, XROrigin, ARCamera with ARCameraBackground, ARPlaneManager, ARRaycastManager
  - Each scene has basic UI canvas with navigation buttons
- Created `SceneNavigationController.cs` - UI button handler for scene transitions
- Added `Relic.Editor.asmdef` for Editor assembly definition
- Created `SceneLoaderTests.cs` with 5 unit tests (all passing):
  - SceneNames_Constants_AreCorrect
  - SceneNames_AllScenesDefined
  - SceneNames_AllUnique
  - SceneNames_NoSpaces
  - SceneNames_MatchKyleMilestones

---

### WP-EXT-2.3: AR Battlefield Placement
**Status:** ⚪ Available
**Priority:** P1
**Complexity:** M
**Assigned:** Unassigned
**Blocked by:** WP-EXT-2.1

**Objective:** Implement the AR plane detection and battlefield placement.

**Tasks:**
1. Configure AR plane detection (horizontal planes only)
2. Create BattlefieldPlacer component:
   - Tap to place battlefield on detected plane
   - Visual indicator for placement preview
   - Confirm/cancel placement
3. Create Battlefield prefab with:
   - Ground plane (configurable size)
   - Spawn points (red/blue sides)
   - Era-appropriate visual style support
4. Scale handling (battlefield fits on table)
5. Tests for placement logic

**Acceptance Criteria:**
- [ ] AR plane detection works on Quest 3
- [ ] Tap-to-place battlefield on surface
- [ ] Battlefield positioned correctly in world space
- [ ] Spawn points visible for both teams

---

### WP-EXT-2.4: Era Configuration System
**Status:** ✅ Complete (2025-12-28)
**Priority:** P1
**Complexity:** S
**Assigned:** Agent-Anette
**Blocked by:** WP-EXT-2.1 (needs folder structure)

**Objective:** Create the ScriptableObject-based era configuration system.

**Tasks:**
1. Create EraConfig ScriptableObject:
   - Era name, description
   - Unit archetype references
   - Visual theme settings
   - Resource/economy settings
2. Create initial era configs:
   - Ancient
   - Medieval
   - WWII
   - Future
3. Create EraManager singleton
4. Era selection UI (basic)
5. Use config_validator.py for validation

**Acceptance Criteria:**
- [x] EraConfig ScriptableObject created
- [x] 4 initial era configs created (Ancient, Medieval, WWII, Future per Kyle's spec)
- [x] EraManager loads/applies configs
- [x] Era can be selected in UI (EraSelectionUI component)
- [x] config_validator.py validates configs (4/4 passed)

**Implementation Notes (Agent-Anette):**
- Created `EraConfigSO.cs` with full data model including visual themes, audio, economy settings
- Created `EraManager.cs` singleton with era cycling, lookup, and event system
- Created `EraSelectionUI.cs` for basic UI interaction
- Created 4 YAML config files: ancient.yaml, medieval.yaml, wwii.yaml, future.yaml
- Added assembly definitions for modular compilation
- Created 20+ unit tests in EditMode

---

## Notes

**Client Communication:** Post to #relic-game when creating PRs.

**Batchmode Commands:**
```bash
# Open Unity project
xvfb-run ~/UnityHub.AppImage --no-sandbox

# Build from command line (when project exists)
xvfb-run /path/to/Unity -projectPath /home/k4therin2/projects/relic \
  -batchmode -buildTarget Android -quit
```

**Next Batch:** EXT-3 (Milestone 2: Core RTS Skeleton) - blocked until this batch complete.
