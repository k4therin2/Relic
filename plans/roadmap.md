# Relic Roadmap

## Overview

Relic is a tabletop AR real-time strategy sandbox for Meta Quest 3.

**Client:** Kyle (SomewhatRogue)
**Slack:** #relic-game
**Status:** Setup complete, game development starting!

---

## Quick Links

| Document | Purpose |
|----------|---------|
| [index.yaml](index.yaml) | Machine-readable status for agent tools |
| [backlog.md](backlog.md) | Deferred items and future work |
| [docs/milestones.md](../docs/milestones.md) | Kyle's detailed requirements |
| [IMPLEMENTATION_PLAN.md](../IMPLEMENTATION_PLAN.md) | Work package mapping |

---

## Current State

**Batch EXT-1: Quick Start Setup** - **7/7 Complete** ✅

| WP | Title | Status |
|----|-------|--------|
| WP-EXT-1.1 | Fork & Clone Setup | Complete |
| WP-EXT-1.2 | Read Kyle's Requirements | Complete |
| WP-EXT-1.3 | Unity Environment Check | **Complete** (2025-12-27) |
| WP-EXT-1.4 | Create Relic Project Config | Complete |
| WP-EXT-1.5 | Documentation Improvements | Complete |
| WP-EXT-1.6 | Test Planning | Complete |
| WP-EXT-1.7 | CLI Tools & Scripts | Complete |

**Unity Status:** ✅ Unity 6000.3.2f1 (6.3 LTS) installed and licensed on Colby. Batchmode verified with xvfb-run.

---

## Active Work

**Batch EXT-4: Milestone 3 - Combat, Elevation, and Squad Upgrades** - 1/4 In Progress

| WP | Title | Status | Agent |
|----|-------|--------|-------|
| WP-EXT-4.1 | Weapon Stats System | ✅ Complete | Nadia |
| WP-EXT-4.2 | Squad System and Upgrades | ⚪ Not Started (unblocked) | Unassigned |
| WP-EXT-4.3 | Combat Logic with Per-Bullet Evaluation | ⚪ Not Started (blocked by 4.2) | Unassigned |
| WP-EXT-4.4 | AI Behavior State Machine | ⚪ Not Started (blocked by 4.3) | Unassigned |

**Milestone 3 Objectives:** Implement full combat system with per-bullet hit chance, elevation bonuses, and squad upgrade modifiers.

See [active/batch-ext-4.md](active/batch-ext-4.md) for full details.

---

## Completed

**Batch EXT-3: Milestone 2 - Core RTS Skeleton** - ✅ 4/4 Complete

| WP | Title | Status | Agent |
|----|-------|--------|-------|
| WP-EXT-3.1 | Unit Data Architecture | ✅ Complete | Anette |
| WP-EXT-3.2 | Unit Movement System | ✅ Complete | Anette |
| WP-EXT-3.3 | Selection System | ✅ Complete | Dorian |
| WP-EXT-3.4 | Unit Spawning System | ✅ Complete | Dorian |

See [active/batch-ext-3.md](active/batch-ext-3.md) for implementation details.

**Batch EXT-2: Milestone 1 - Project and AR Tabletop Foundation** - ✅ 4/4 Complete

| WP | Title | Status | Agent |
|----|-------|--------|-------|
| WP-EXT-2.1 | Unity Project Initialization | ✅ Complete | Dorian |
| WP-EXT-2.2 | Scene Architecture | ✅ Complete | Dorian |
| WP-EXT-2.3 | AR Battlefield Placement | ✅ Complete | Nadia |
| WP-EXT-2.4 | Era Configuration System | ✅ Complete | Anette |

See [active/batch-ext-2.md](active/batch-ext-2.md) for implementation details.

---

## Future Work (4 More Milestones)

1. **M2: RTS Core** - Units, selection, commands (Batch EXT-3)
2. **M3: Combat** - Weapons, squads, per-bullet mechanics (Batch EXT-4)
3. **M4: Performance** - Optimization for 100v100 units (Batch EXT-5)
4. **M5: Polish** - Scenarios, visual polish (optional stretch) (Batch EXT-6)

See [docs/milestones.md](../docs/milestones.md) for full specification.

---

## Recent Completions

**2025-12-28:** WP-EXT-4.1 Weapon Stats System complete (Nadia). WeaponStatsSO ScriptableObject with fire rate, hit chance, damage, range/elevation curves. 21 unit tests. Editor tool creates 4 era-specific weapons.

**2025-12-28:** Batch EXT-4 created for Milestone 3 (Combat, Elevation, Squad Upgrades). 4 work packages: Weapon Stats, Squad System, Combat Logic, AI Behavior. Created by Nadia.

**2025-12-28:** WP-EXT-3.3 Selection System complete (Dorian). SelectionManager singleton, DebugSelectionController, ARSelectionController, SelectionIndicator with 27 unit tests. **Milestone 2 Complete!**

**2025-12-28:** WP-EXT-3.4 Unit Spawning System complete (Dorian). UnitFactory, SpawnPoint, SpawnTestingUI with 29 unit tests.

**2025-12-28:** WP-EXT-3.2 Unit Movement System complete (Anette). Command pattern with MoveCommand, StopCommand, CommandQueue, 38 tests.

**2025-12-28:** WP-EXT-3.1 Unit Data Architecture complete! UnitArchetypeSO, UnitController, UnitStats, 42 unit tests, editor tools. All M2 WPs now unblocked.

**2025-12-28:** Batch EXT-3 created for Milestone 2 (Core RTS Skeleton). 4 work packages: Unit Data Architecture, Movement System, Selection System, Unit Spawning.

**2025-12-28:** Milestone 1 Complete! All 4 WPs done: Unity project (Dorian), scene architecture (Dorian), AR battlefield placement (Nadia), era config system (Anette). Full AR tabletop foundation delivered!

**2025-12-27:** WP-EXT-1.3 Unity Environment Check complete. Unity 6000.3.2f1 installed with Android Build Support and Personal license activated. Batchmode verified with xvfb-run. All game development now unblocked!

---

*This file provides overview only. See index.yaml for machine-readable status.*
*Last updated: 2025-12-28 by Agent-Nadia - WP-EXT-4.1 Weapon Stats System complete*
