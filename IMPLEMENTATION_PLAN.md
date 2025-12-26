# Relic Implementation Plan

## Overview

This document captures Kyle's requirements and provides a roadmap for the agent team's implementation work on Relic.

**Project Type:** Tabletop AR Real-Time Strategy Sandbox
**Platform:** Meta Quest 3 (with non-AR debug mode for phones/editor)
**Scale:** Up to ~100 vs 100 units per battle
**Tech Stack:** Unity LTS, URP, AR Foundation, XR Interaction Toolkit

---

## Requirements Summary

Based on Kyle's milestones.md:

### Core Features Required

1. **AR Battlefield Placement**
   - Plane detection on real surfaces
   - Tap-to-place battlefield
   - Stable tracking during user movement

2. **Data-Driven Era System**
   - 4 eras: Ancient, Medieval, WWII, Future
   - Each era has distinct archetypes, weapons, visuals
   - ScriptableObject-based configuration

3. **RTS Core Mechanics**
   - Unit archetypes with health, speed, weapons
   - Selection system (mouse in debug, controller ray in AR)
   - Command system (Move, Attack, Stop)
   - NavMesh-based pathfinding

4. **Combat System**
   - Per-bullet hit chance evaluation
   - Range-based accuracy curves
   - Elevation bonuses
   - Squad upgrades that modify damage/accuracy

5. **AI Behavior**
   - Basic state machine (Idle, Moving, Attacking)
   - Auto-acquire nearest enemy
   - Squad-level coordination

6. **Performance Optimization**
   - Central tick manager (replace per-unit Update)
   - GPU instancing for large unit counts
   - LOD system for distant units
   - Unit pooling to reduce instantiation

---

## MVP Scope

Kyle's scope control rules:
- One battlefield
- Three unit archetypes
- One fully tuned era
- Everything else is data layers

**Minimum Viable Product includes:**
- Milestones 1-3 (Foundation, RTS Core, Combat)
- Milestone 4 (Performance) required for 100v100 scale
- Milestone 5 (Scenarios/Polish) is optional stretch

---

## Architecture Approach

### Directory Structure (per Kyle's spec)

```
/Assets
  /Scripts
    /CoreRTS      # Platform-independent RTS logic
    /ARLayer      # AR-specific features
    /UILayer      # UI components
  /Configs        # ScriptableObject configs
  /Art
    /Shared       # Common assets
    /Ancient      # Era-specific
    /Medieval
    /WWII
    /Future
  /Scenes
    Boot.unity        # Entry point
    AR_Battlefield.unity
    Flat_Debug.unity  # Non-AR testing

/docs
  milestones.md      # Kyle's roadmap (source of truth)
  design_rts_combat.md
```

### Development Philosophy

1. **Flat Debug First** - Build core RTS in debug scene, then port to AR
2. **Small to Large** - Start with minimal unit counts, scale after validation
3. **Data-Driven** - Era configs and archetypes via ScriptableObjects
4. **Separation of Concerns** - Clean split between RTS logic and AR features

---

## Work Package Mapping

### Batch RELIC-1: Milestone 1 - Foundation
| WP | Task | Complexity |
|----|------|------------|
| RELIC-1.1 | Unity Project Initialization | S |
| RELIC-1.2 | Scene Architecture | S |
| RELIC-1.3 | AR Battlefield Placement | M |
| RELIC-1.4 | Era Configuration System | S |

### Batch RELIC-2: Milestone 2 - RTS Core
| WP | Task | Complexity |
|----|------|------------|
| RELIC-2.1 | Unit Archetype System | M |
| RELIC-2.2 | Unit Controller and Spawning | M |
| RELIC-2.3 | Selection System | M |
| RELIC-2.4 | Command System (Move/Stop) | M |

### Batch RELIC-3: Milestone 3 - Combat
| WP | Task | Complexity |
|----|------|------------|
| RELIC-3.1 | Weapon Stats System | M |
| RELIC-3.2 | Squad System and Upgrades | M |
| RELIC-3.3 | Per-Bullet Combat | L |
| RELIC-3.4 | AI States and Auto-Target | M |

### Batch RELIC-4: Milestone 4 - Performance
| WP | Task | Complexity |
|----|------|------------|
| RELIC-4.1 | AR UX Enhancements | M |
| RELIC-4.2 | Central Tick Manager | M |
| RELIC-4.3 | GPU Instancing and LOD | M |
| RELIC-4.4 | Unit Pooling | S |

### Batch RELIC-5: Milestone 5 - Polish (Optional)
| WP | Task | Complexity |
|----|------|------------|
| RELIC-5.1 | Scenario System | M |
| RELIC-5.2 | Era Visual/Audio Polish | L |

---

## Key Technical Decisions

### Combat Math (per Kyle's spec)
```
hit_chance = base_hit_chance
           * range_curve(distance)
           * elevation_curve(height_diff)
           * squad_accuracy_multiplier
           * upgrade_modifiers

final_hit = clamp(hit_chance, 0, 1)
damage = base_damage * squad_damage_multiplier * upgrade_damage_modifier
```

### ScriptableObject Architecture
- `UnitArchetype`: id, health, speed, weapon ref, prefab
- `WeaponStats`: shots/burst, fire rate, base hit, damage, range curve, elevation curve
- `EraConfig`: archetype list, upgrade list, visual refs
- `UpgradeDefinition`: hit/damage/elevation multipliers

### Performance Targets
- 60 FPS on Quest 3 with 100v100 units
- Smooth AR tracking
- Responsive selection and commands

---

## Current State

**Repository:** https://github.com/SomewhatRogue/Relic
**Fork:** https://github.com/k4therin2/Relic

**Existing Content:**
- milestones.md - Complete project specification
- README.md - Stub
- No Unity project files yet

**Prerequisites Completed:**
- [x] Fork created (WP-EXT-1.1)
- [x] Local clone with remotes
- [x] Development workflow documented
- [ ] Unity environment setup (WP-EXT-1.3)
- [ ] Project config in agent-automation (WP-EXT-1.4)

---

## Next Steps

1. **Unity Setup (WP-EXT-1.3):**
   - Install Unity Hub and correct Unity LTS version
   - Initialize Unity project structure
   - Configure for Quest 3 (Android + XR plugins)

2. **Project Tracking (WP-EXT-1.4):**
   - Create relic.yaml config in agent-automation
   - Add to dashboard tracking

3. **Begin RELIC-1:**
   - Project initialization
   - Scene architecture
   - AR placement system

---

## Communication

- **Slack Channel:** #relic-game
- **Client:** Kyle (SomewhatRogue)
- **Workflow:** Create PRs, ping Kyle in Slack for review

---

*Generated by Agent-Dorian, 2025-12-26*
*Based on Kyle's milestones.md specification*
