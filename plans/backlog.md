# Relic Backlog

Deferred items and future enhancements not yet scheduled.

---

## ✅ Resolved: Unity Setup Complete

**Unity License Activation** - RESOLVED 2025-12-27

Unity 6000.3.2f1 (6.3 LTS) is now fully installed and licensed on Colby:
- Android Build Support modules installed
- Personal license activated
- Batchmode verified with xvfb-run

**Game development is now unblocked!** See Batch EXT-2 for Milestone 1 work.

---

## Future Milestones (After M1)

### Milestone 2: Core RTS Skeleton
**Blocked by:** Milestone 1 (Batch EXT-2)

Work packages (Batch EXT-3):
- RELIC-2.1: Unit Archetype System
- RELIC-2.2: Unit Controller and Spawning
- RELIC-2.3: Selection System
- RELIC-2.4: Command System (Move/Stop)

### Milestone 3: Combat and Upgrades
**Blocked by:** Milestone 2

Work packages (Batch EXT-4):
- RELIC-3.1: Weapon Stats System
- RELIC-3.2: Squad System and Upgrades
- RELIC-3.3: Per-Bullet Combat (complex)
- RELIC-3.4: AI States and Auto-Target

### Milestone 4: Performance Optimization
**Blocked by:** Milestone 3

Work packages (Batch EXT-5):
- RELIC-4.1: AR UX Enhancements
- RELIC-4.2: Central Tick Manager
- RELIC-4.3: GPU Instancing and LOD
- RELIC-4.4: Unit Pooling

### Milestone 5: Scenarios and Polish (Optional Stretch)
**Blocked by:** Milestone 4

Work packages (Batch EXT-6):
- RELIC-5.1: Scenario System
- RELIC-5.2: Era Visual/Audio Polish

---

## Technical Backlog

### Build Automation
When Unity project is created (WP-EXT-2.1), integrate build.py tool with CI/CD.

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

*Last updated: 2025-12-28 by Agent-Henry (Unity now working, Batch EXT-2 created)*
