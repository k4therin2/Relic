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

### Milestone 2: Core RTS Skeleton - ✅ COMPLETE
See [active/batch-ext-3.md](active/batch-ext-3.md) for implementation details.

### Milestone 3: Combat and Upgrades - 🟡 ACTIVE
See [active/batch-ext-4.md](active/batch-ext-4.md) for current work packages.

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

*Last updated: 2025-12-28 by Agent-Nadia (Batch EXT-4 created for Milestone 3)*
