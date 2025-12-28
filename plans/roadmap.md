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

**Batch EXT-2: Milestone 1 - Project and AR Tabletop Foundation** - 4/4 Complete ✅

| WP | Title | Status | Priority |
|----|-------|--------|----------|
| WP-EXT-2.1 | Unity Project Initialization | ✅ Complete | P0 (blocking) |
| WP-EXT-2.2 | Scene Architecture | ✅ Complete | P1 |
| WP-EXT-2.3 | AR Battlefield Placement | ✅ Complete | P1 |
| WP-EXT-2.4 | Era Configuration System | ✅ Complete | P1 |

**Milestone 1 Complete!** All core foundation work done - Unity project, scenes, AR placement, and era configs.

See [active/batch-ext-2.md](active/batch-ext-2.md) for full details.

---

## Future Work (4 More Milestones)

1. **M2: RTS Core** - Units, selection, commands (Batch EXT-3)
2. **M3: Combat** - Weapons, squads, per-bullet mechanics (Batch EXT-4)
3. **M4: Performance** - Optimization for 100v100 units (Batch EXT-5)
4. **M5: Polish** - Scenarios, visual polish (optional stretch) (Batch EXT-6)

See [docs/milestones.md](../docs/milestones.md) for full specification.

---

## Recent Completions

**2025-12-28:** WP-EXT-2.3 AR Battlefield Placement complete. BattlefieldPlacer with AR plane detection, placement workflow (Detecting→Previewing→Confirming→Placed), scale controller with table-size presets, and 41 unit tests.

**2025-12-28:** WP-EXT-2.1, 2.2, 2.4 complete. Full Milestone 1 foundation done!

**2025-12-27:** WP-EXT-1.3 Unity Environment Check complete. Unity 6000.3.2f1 installed with Android Build Support and Personal license activated. Batchmode verified with xvfb-run. All game development now unblocked!

---

*This file provides overview only. See index.yaml for machine-readable status.*
*Last updated: 2025-12-28 by Agent-Nadia - WP-EXT-2.3 complete, Milestone 1 done!*
