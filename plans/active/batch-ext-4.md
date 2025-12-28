# Batch EXT-4: Milestone 3 - Combat, Elevation, and Squad Upgrades

**Project:** Relic (Unity game - Kyle's repo fork)
**Status:** active
**Created:** 2025-12-28
**Owner:** Henry (Project Manager)
**Prerequisite:** Batch EXT-3 complete (M2: Core RTS Skeleton delivered)

## Context

Milestone 2 is complete! Unit data architecture, movement system, selection system, and unit spawning are all working. Now we implement the Combat systems from Kyle's Milestone 3.

**Reference:** Kyle's requirements in `/home/k4therin2/projects/relic/docs/milestones.md`

## Milestone 3 Objectives (from Kyle's spec)

- Implement full combat system
- Per-bullet hit chance
- Elevation bonuses
- Squad upgrade modifiers

---

## Work Packages

### WP-EXT-4.1: Weapon Stats System
**Status:** ✅ Complete (2025-12-28)
**Priority:** P0 (blocking combat logic)
**Complexity:** S
**Agent:** Agent-Nadia

**Objective:** Create WeaponStats ScriptableObject for weapon configuration.

**Tasks:**
1. [x] Create `WeaponStatsSO` ScriptableObject with:
   - shotsPerBurst (int)
   - fireRate (shots per second)
   - baseHitChance (0-1 float)
   - baseDamage (float)
   - effectiveRange (units)
   - rangeHitCurve (AnimationCurve)
   - elevationBonusCurve (AnimationCurve)
2. [x] Create 4 sample weapons (one per era):
   - Ancient: Bow (slow, high damage, medium range)
   - Medieval: Crossbow (very slow, very high damage, long range)
   - WWII: Rifle (medium speed, medium damage, long range)
   - Future: Laser (fast, low damage per hit, excellent accuracy)
3. [x] Create Editor tool to generate sample weapons
4. [x] Write unit tests for WeaponStats validation (21 tests, 100% pass)
5. [x] Update UnitArchetypeSO to reference WeaponStatsSO

**Acceptance Criteria:**
- [x] WeaponStatsSO ScriptableObject created with all fields
- [x] 4 sample weapons created (via Editor menu: Relic > Create Sample Weapons)
- [x] WeaponStats validated (positive values, valid curves)
- [x] Unity compiles without errors

**Completion Notes:**
- WeaponStatsSO with full combat stats, range curves, elevation curves
- Editor tool creates 4 era-specific weapons with balanced stats
- 21 unit tests covering all functionality
- Fixed pre-existing XR namespace issues (XRI 3.0 migration)
- Fixed Editor asmdef missing UILayer reference

---

### WP-EXT-4.2: Squad System and Upgrades
**Status:** ✅ Complete (2025-12-28)
**Priority:** P1
**Complexity:** M
**Agent:** Agent-Anette, Agent-Dorian
**Blocked by:** ~~WP-EXT-4.1~~ (complete)

**Objective:** Implement squad grouping and upgrade modifiers.

**Tasks:**
1. [x] Create `Squad` class:
   - List of UnitController members
   - List of applied UpgradeSO
   - Methods: AddMember, RemoveMember, ApplyUpgrade
   - Computed properties: HitChanceMultiplier, DamageMultiplier, ElevationBonusFlat
2. [x] Create `UpgradeSO` ScriptableObject with:
   - id (unique identifier)
   - displayName
   - hitChanceMultiplier (float, default 1.0)
   - damageMultiplier (float, default 1.0)
   - elevationBonus (float, default 0)
   - era (EraType enum for filtering)
3. [x] Create 8 sample upgrades (2 per era):
   - Ancient: Veterans (+10% hit), Shield Wall (+20% damage)
   - Medieval: Heavy Armor (+15% damage), Marksmen (+15% hit)
   - WWII: Elite Training (+20% hit), Heavy Weapons (+25% damage)
   - Future: Targeting System (+30% hit), Overcharge (+40% damage, -10% hit)
4. [x] Update UnitController to reference its Squad
5. [x] Write unit tests for Squad operations and upgrade stacking (43 tests, 100% pass)
6. [x] Create Editor tool to generate sample upgrades

**Acceptance Criteria:**
- [x] Squad class manages unit groups
- [x] UpgradeSO defines stat modifiers
- [x] Upgrades stack multiplicatively for hit/damage
- [x] 8 sample upgrades created (via Editor menu: Relic > Create Sample Upgrades)
- [x] Unity compiles without errors

**Completion Notes:**
- Squad class with full member management, upgrade stacking, and event system
- UpgradeSO with era filtering, max stacks, mutual exclusivity
- EraType enum for era-based filtering
- UnitController extended with JoinSquad, LeaveSquad, GetSquadHitChanceMultiplier, etc.
- 27 SquadTests + 16 UpgradeTests = 43 total new tests
- Editor tool creates 8 balanced upgrades (2 per era)
- Started by Anette, fixes and completion by Dorian

---

### WP-EXT-4.3: Combat Logic with Per-Bullet Evaluation
**Status:** 🟡 In Progress
**Priority:** P1
**Complexity:** L (complex math and integration)
**Agent:** Agent-Anette
**Blocked by:** ~~WP-EXT-4.1, WP-EXT-4.2~~ (complete)

**Objective:** Implement per-bullet hit chance with elevation and squad modifiers.

**Tasks:**
1. Create `CombatResolver` class with static method:
   ```csharp
   public static CombatResult ResolveCombat(
       UnitController attacker,
       UnitController target,
       WeaponStatsSO weapon)
   ```
2. Combat resolution per bullet:
   - Start with baseHitChance from weapon
   - Apply range curve based on distance
   - Apply elevation curve based on height difference
   - Apply squad hit chance multiplier (if in squad)
   - Clamp to 0.05-0.95 (always 5% miss/hit chance)
   - Roll random for each bullet
   - Apply damage with squad damage multiplier
3. Create `CombatResult` struct:
   - shotsFirered (int)
   - shotsHit (int)
   - totalDamage (float)
   - targetDestroyed (bool)
4. Integrate with UnitController:
   - AttackCommand uses CombatResolver
   - Unit tracks current target
   - Fires at fire rate intervals
   - Stops when target destroyed or out of range
5. Handle elevation calculation:
   - Use transform.position.y for elevation
   - Positive elevation = bonus, negative = penalty
6. Write extensive unit tests for combat math
7. Create debug visualization for hit/miss feedback

**Acceptance Criteria:**
- [ ] Per-bullet hit evaluation implemented
- [ ] Range affects hit chance via curve
- [ ] Elevation affects hit chance via curve
- [ ] Squad upgrades apply correctly
- [ ] Combat integrates with command system
- [ ] Debug feedback shows hits/misses

---

### WP-EXT-4.4: AI Behavior State Machine
**Status:** Not Started
**Priority:** P1
**Complexity:** M
**Agent:** Unassigned
**Blocked by:** WP-EXT-4.3 (needs combat system)

**Objective:** Implement basic AI for enemy units.

**Tasks:**
1. Create `UnitAI` component with state machine:
   - States: Idle, Moving, Attacking
   - Transitions handled in Update
2. Implement state behaviors:
   - **Idle:** Scan for enemies in detection radius
   - **Moving:** Move to destination, scan while moving
   - **Attacking:** Fire at target, re-acquire if target dies
3. Add enemy detection:
   - DetectionRadius (float) on UnitController
   - FindNearestEnemy() method using Physics.OverlapSphere
   - Filter by team (attackable = different team)
4. Auto-acquire behavior:
   - When enemy enters detection radius, switch to Attacking
   - When current target dies, find new target
   - When no enemies in range, return to Idle (or continue Move)
5. Team configuration:
   - Use existing team field from UnitController
   - Team 0 vs Team 1 for basic opposition
6. Write unit tests for state transitions
7. Create debug visualization (detection radius gizmo)

**Acceptance Criteria:**
- [ ] AI state machine with Idle/Moving/Attacking
- [ ] Auto-acquire nearest enemy
- [ ] Re-target when target destroyed
- [ ] Detection radius configurable per unit
- [ ] AI works in both AR and debug scenes

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
- WP-EXT-4.1 must complete first (defines weapon data types)
- WP-EXT-4.2 depends on 4.1 (needs weapon pattern)
- WP-EXT-4.3 depends on 4.1 and 4.2 (uses both systems)
- WP-EXT-4.4 depends on 4.3 (needs combat for attacking state)

**Next Batch:** EXT-5 (Milestone 4: AR UX, Large Battles, Performance) - blocked until this batch complete.

---

*Created: 2025-12-28 by Agent-Nadia*
*Based on Kyle's Milestone 3 requirements in docs/milestones.md*
