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

### Milestone 4: Performance Optimization - ✅ BATCH CREATED
**Status:** Batch EXT-5 created and ready (blocked by M3)
**See:** [active/batch-ext-5.md](active/batch-ext-5.md) for work packages

Work packages:
- WP-EXT-5.1: AR UX Enhancements (P1/M)
- WP-EXT-5.2: World-Space UI Panel (P1/M)
- WP-EXT-5.3: Central Tick Manager (P0/M) - critical for 100v100
- WP-EXT-5.4: GPU Instancing and LOD (P1/M)
- WP-EXT-5.5: Unit Pooling (P1/S)
- WP-EXT-5.6: Performance Profiling and Validation (P1/S)

### Milestone 5: Scenarios and Polish (Optional Stretch)
**Blocked by:** Milestone 4

Work packages (Batch EXT-6):
- RELIC-5.1: Scenario System
- RELIC-5.2: Era Visual/Audio Polish

---

## Feature Backlog

### Quick Setup Utility (One-Click Playable Demo)
**Status:** ⚪ Backlog
**Priority:** P2 (suggested)
**Source:** Slack #relic-game (2025-12-29, routed by Grace)
**Original request:** "Quick Setup Utility (one-click playable demo) - New menu command that auto-generates demo scene"

**Description:**
Create a menu command in Unity Editor that automatically sets up a playable demo scene with:
- Pre-configured units for both sides
- Reasonable starting positions
- Working camera and controls
- One-click to enter play mode and see action

**Tasks:**
- [ ] Create Editor menu item (Tools > Relic > Quick Demo Setup)
- [ ] Auto-instantiate demo scene with units from multiple eras
- [ ] Position units in combat range for immediate action
- [ ] Document usage in README or dev docs

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

*Last updated: 2025-12-28 by Agent-Dorian (Batch EXT-5 created for Milestone 4 Performance)*
