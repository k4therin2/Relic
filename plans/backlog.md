# Relic Backlog

Deferred items and future enhancements not yet scheduled.

---

## Current Blocker

### Unity License Activation (WP-EXT-1.3)
**Status:** Blocked - Requires User Action
**Priority:** P0 (Blocking all game development)

Unity 6.3 LTS is installed on Colby but license activation requires GUI interaction.
Colby is a headless server.

**Options (See DEVELOPMENT.md for details):**
1. SSH with X11 forwarding: `ssh -X k4therin2@colby` then `~/UnityHub.AppImage --no-sandbox`
2. VNC/Remote Desktop to Colby
3. Copy license file from another machine: `~/.config/unity3d/Unity/Unity_lic.ulf`

Once activated, all future milestones unblock.

---

## Future Milestones (From Kyle's Roadmap)

All milestones from `docs/milestones.md` are planned but blocked until Unity setup completes.

### Milestone 1: Project and AR Tabletop Foundation
**Blocked by:** WP-EXT-1.3 (Unity License)

Work packages:
- RELIC-1.1: Unity Project Initialization
- RELIC-1.2: Scene Architecture
- RELIC-1.3: AR Battlefield Placement
- RELIC-1.4: Era Configuration System

### Milestone 2: Core RTS Skeleton
**Blocked by:** Milestone 1

Work packages:
- RELIC-2.1: Unit Archetype System
- RELIC-2.2: Unit Controller and Spawning
- RELIC-2.3: Selection System
- RELIC-2.4: Command System (Move/Stop)

### Milestone 3: Combat and Upgrades
**Blocked by:** Milestone 2

Work packages:
- RELIC-3.1: Weapon Stats System
- RELIC-3.2: Squad System and Upgrades
- RELIC-3.3: Per-Bullet Combat (complex)
- RELIC-3.4: AI States and Auto-Target

### Milestone 4: Performance Optimization
**Blocked by:** Milestone 3

Work packages:
- RELIC-4.1: AR UX Enhancements
- RELIC-4.2: Central Tick Manager
- RELIC-4.3: GPU Instancing and LOD
- RELIC-4.4: Unit Pooling

### Milestone 5: Scenarios and Polish (Optional Stretch)
**Blocked by:** Milestone 4

Work packages:
- RELIC-5.1: Scenario System
- RELIC-5.2: Era Visual/Audio Polish

---

## Technical Backlog

### Build Automation
When Unity is available, integrate build.py tool with CI/CD.

### Config Validation
Run config_validator.py as part of CI to catch ScriptableObject issues.

### Performance Testing Framework
Implement 100v100 unit performance benchmarks as documented in TESTING.md.

---

## Client Communication

**Slack Channel:** #relic-game
**Client:** Kyle (SomewhatRogue)
**Workflow:** Create PRs to upstream, ping Kyle in Slack for review

---

*Last updated: 2025-12-27 by Agent-Anette (WP-62.3)*
