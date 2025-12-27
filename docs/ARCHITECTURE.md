# Relic Architecture Guide

## System Overview

Relic is a tabletop augmented reality real-time strategy (RTS) sandbox built in Unity for Meta Quest 3, with a non-AR debug mode for development and testing.

```mermaid
flowchart TB
    subgraph Platform["Platform Layer"]
        AR[AR Layer<br/>Quest 3 / AR Foundation]
        Debug[Debug Layer<br/>Mouse/Keyboard]
    end

    subgraph Core["Core RTS Layer"]
        Units[Unit System]
        Combat[Combat System]
        Commands[Command System]
        AI[AI System]
        Squads[Squad System]
    end

    subgraph Data["Data Layer"]
        Era[Era Config]
        Archetype[Unit Archetypes]
        Weapons[Weapon Stats]
        Upgrades[Upgrade Definitions]
    end

    AR --> Core
    Debug --> Core
    Core --> Data
```

---

## Core Systems

### 1. Unit System

Manages individual unit instances, including spawning, health, movement, and death.

```mermaid
classDiagram
    class UnitController {
        +float Health
        +float MaxHealth
        +float MoveSpeed
        +UnitArchetype Archetype
        +Squad OwningSquad
        +NavMeshAgent Agent
        +TakeDamage(float amount)
        +MoveTo(Vector3 position)
        +Stop()
        +Die()
    }

    class UnitArchetype {
        <<ScriptableObject>>
        +string Id
        +float BaseHealth
        +float BaseMoveSpeed
        +WeaponStats Weapon
        +GameObject Prefab
    }

    UnitController --> UnitArchetype : references
    UnitController --> NavMeshAgent : uses
```

### 2. Combat System

Handles per-bullet hit chance evaluation with range, elevation, and upgrade modifiers.

```mermaid
flowchart LR
    subgraph Input["Combat Input"]
        Attacker[Attacker Unit]
        Target[Target Unit]
    end

    subgraph Calculation["Hit Calculation"]
        Base[Base Hit Chance]
        Range[Range Curve]
        Elev[Elevation Curve]
        Squad[Squad Modifiers]
        Upgrade[Upgrade Modifiers]
        Clamp[Clamp 0-1]
    end

    subgraph Output["Per-Bullet Result"]
        Roll[Random Roll]
        Hit{Hit?}
        Damage[Apply Damage]
        Miss[Miss]
    end

    Attacker --> Base
    Target --> Base
    Base --> Range --> Elev --> Squad --> Upgrade --> Clamp
    Clamp --> Roll --> Hit
    Hit -->|Yes| Damage
    Hit -->|No| Miss
```

#### Hit Chance Formula

```
hit_chance = base_hit_chance
           * range_curve(distance)
           * elevation_curve(height_diff)
           * squad_accuracy_multiplier
           * upgrade_modifiers

final_hit = clamp(hit_chance, 0, 1)
damage = base_damage * squad_damage_multiplier * upgrade_damage_modifier
```

### 3. Command System

Processes player commands for unit control.

```mermaid
stateDiagram-v2
    [*] --> Idle

    Idle --> Moving : Move Command
    Idle --> Attacking : Attack Command

    Moving --> Idle : Destination Reached
    Moving --> Attacking : Attack Command
    Moving --> Idle : Stop Command

    Attacking --> Idle : Target Dead
    Attacking --> Moving : Move Command
    Attacking --> Idle : Stop Command
```

**Command Types:**
- `Move` - Navigate unit to target position
- `Attack` - Engage target unit
- `Stop` - Halt current action

### 4. Squad System

Groups units for collective buffs and coordination.

```mermaid
classDiagram
    class Squad {
        +List~UnitController~ Members
        +List~UpgradeDefinition~ Upgrades
        +GetHitChanceMultiplier() float
        +GetDamageMultiplier() float
        +GetElevationBonus() float
        +AddMember(UnitController unit)
        +RemoveMember(UnitController unit)
        +ApplyUpgrade(UpgradeDefinition upgrade)
    }

    class UpgradeDefinition {
        <<ScriptableObject>>
        +string Name
        +float HitChanceMultiplier
        +float DamageMultiplier
        +float ElevationBonus
    }

    Squad --> UnitController : contains
    Squad --> UpgradeDefinition : applies
```

### 5. AI System

Basic state machine for autonomous unit behavior.

```mermaid
stateDiagram-v2
    [*] --> Idle

    Idle --> Searching : No Target + Enemies Nearby
    Searching --> Attacking : Target Acquired
    Attacking --> Pursuing : Target Out of Range
    Pursuing --> Attacking : Target in Range
    Attacking --> Searching : Target Dead
    Searching --> Idle : No Enemies Found

    note right of Idle : Auto-acquires nearest enemy
    note right of Attacking : Fires weapon bursts
    note right of Pursuing : NavMesh movement
```

---

## Data Architecture

All game data is defined via Unity ScriptableObjects for easy tuning and era swapping.

```mermaid
erDiagram
    EraConfig ||--|{ UnitArchetype : contains
    EraConfig ||--|{ UpgradeDefinition : contains
    EraConfig {
        string Name
        VisualStyle Visuals
    }

    UnitArchetype ||--|| WeaponStats : has
    UnitArchetype {
        string Id
        float BaseHealth
        float BaseMoveSpeed
        GameObject Prefab
    }

    WeaponStats {
        int ShotsPerBurst
        float FireRate
        float BaseHitChance
        float BaseDamage
        AnimationCurve RangeCurve
        AnimationCurve ElevationCurve
    }

    UpgradeDefinition {
        string Name
        float HitMultiplier
        float DamageMultiplier
        float ElevationBonus
    }
```

### Era Configurations

Four eras with distinct unit archetypes and visuals:

| Era | Archetype Examples | Visual Theme |
|-----|-------------------|--------------|
| Ancient | Spearmen, Archers, Cavalry | Bronze, Stone, Leather |
| Medieval | Knights, Crossbowmen, Pikemen | Steel, Chainmail, Heraldry |
| WWII | Riflemen, MG Teams, Tanks | Olive Drab, Camouflage |
| Future | Marines, Mechs, Drones | Sci-fi, Energy Weapons |

---

## Scene Architecture

```mermaid
flowchart TB
    Boot[Boot.unity] --> Choice{Platform?}
    Choice -->|Quest 3| AR[AR_Battlefield.unity]
    Choice -->|Editor/Phone| Debug[Flat_Debug.unity]

    subgraph AR Scene
        ARSession[AR Session]
        AROrigin[AR Session Origin]
        PlaneManager[AR Plane Manager]
        Battlefield[Battlefield Root]
    end

    subgraph Debug Scene
        Camera[Standard Camera]
        DebugBattlefield[Battlefield Root]
        DebugInput[Mouse/Keyboard Input]
    end
```

### Battlefield Structure

```
BattlefieldRoot (Prefab)
├── GroundPlane
├── Obstacles/
│   ├── Hill_01
│   ├── Cover_01
│   └── ...
├── SpawnPoints/
│   ├── Team1_Spawn
│   └── Team2_Spawn
├── NavMesh (baked)
└── UI/
    └── WorldSpacePanel
```

---

## Input Handling

### Debug Mode (Mouse/Keyboard)

```mermaid
flowchart LR
    subgraph Input
        LMB[Left Click]
        RMB[Right Click]
        Drag[Drag Select]
    end

    subgraph Actions
        Select[Select Unit]
        MultiSelect[Multi-Select]
        Move[Move Command]
        Attack[Attack Command]
    end

    LMB --> Select
    Drag --> MultiSelect
    RMB -->|Ground| Move
    RMB -->|Enemy| Attack
```

### AR Mode (Quest 3 Controllers)

```mermaid
flowchart LR
    subgraph Input
        Ray[Controller Raycast]
        Trigger[Trigger Button]
        Grip[Grip Button]
    end

    subgraph Actions
        Hover[Highlight Unit]
        Select[Select Unit]
        Command[Issue Command]
    end

    Ray --> Hover
    Ray --> Trigger --> Select
    Ray --> Grip --> Command
```

---

## Performance Architecture

### Central Tick Manager

Replaces per-unit `Update()` calls with a centralized tick system.

```mermaid
flowchart TB
    TickManager[Central Tick Manager] --> |Tick| UnitPool

    subgraph UnitPool["Unit Pool (100+ units)"]
        U1[Unit 1]
        U2[Unit 2]
        U3[...]
        Un[Unit N]
    end

    TickManager --> |Frame Budget| Budget{Budget OK?}
    Budget -->|Yes| Process[Process All]
    Budget -->|No| Defer[Defer to Next Frame]
```

### Optimization Strategies

| Technique | Purpose | Target |
|-----------|---------|--------|
| Object Pooling | Reduce instantiation | Units, Bullets |
| GPU Instancing | Batch draw calls | Unit Meshes |
| LOD System | Reduce distant poly count | Unit Models |
| Central Tick | Reduce Update overhead | Unit Controllers |
| Bullet Aggregation | Optional: reduce bullet count | High fire-rate weapons |

---

## Directory Structure

```
/Assets
├── /Scripts
│   ├── /CoreRTS           # Platform-independent RTS logic
│   │   ├── UnitController.cs
│   │   ├── CombatSystem.cs
│   │   ├── CommandSystem.cs
│   │   ├── SquadSystem.cs
│   │   └── AIStateMachine.cs
│   ├── /ARLayer           # AR-specific features
│   │   ├── BattlefieldPlacer.cs
│   │   ├── ARInputHandler.cs
│   │   └── ARSelectionVisuals.cs
│   └── /UILayer           # UI components
│       ├── WorldSpaceUI.cs
│       ├── UnitHealthBar.cs
│       └── SelectionIndicator.cs
├── /Configs               # ScriptableObject configs
│   ├── /Eras
│   │   ├── Ancient.asset
│   │   ├── Medieval.asset
│   │   ├── WWII.asset
│   │   └── Future.asset
│   ├── /Archetypes
│   │   └── ...
│   ├── /Weapons
│   │   └── ...
│   └── /Upgrades
│       └── ...
├── /Art
│   ├── /Shared            # Common assets
│   ├── /Ancient           # Era-specific art
│   ├── /Medieval
│   ├── /WWII
│   └── /Future
├── /Scenes
│   ├── Boot.unity         # Entry point
│   ├── AR_Battlefield.unity
│   └── Flat_Debug.unity   # Non-AR testing
└── /Prefabs
    ├── BattlefieldRoot.prefab
    ├── /Units
    └── /Effects

/docs
├── milestones.md          # Kyle's roadmap (source of truth)
├── ARCHITECTURE.md        # This file
├── CODING_STANDARDS.md    # C#/Unity conventions
└── TESTING.md             # Test strategy
```

---

## Technology Stack

| Component | Technology |
|-----------|------------|
| Engine | Unity 6.3 LTS (6000.3.0f1) |
| Rendering | Universal Render Pipeline (URP) |
| AR | AR Foundation + Meta XR SDK |
| Input | XR Interaction Toolkit |
| Platform | Meta Quest 3 (primary), Android (debug) |
| Navigation | Unity NavMesh |
| Data | ScriptableObjects |

---

## Communication Flow

```mermaid
sequenceDiagram
    participant Agent as Agent Team
    participant Fork as k4therin2/Relic
    participant Upstream as SomewhatRogue/Relic
    participant Kyle as Kyle (Slack)

    Agent->>Fork: Push feature branch
    Agent->>Upstream: Create Pull Request
    Agent->>Kyle: Notify in #relic-game
    Kyle->>Upstream: Review & Merge
    Kyle->>Agent: Feedback (if needed)
    Agent->>Fork: Sync with upstream
```

---

*Document created by Agent-Dorian, 2025-12-26*
*Based on Kyle's milestones.md specification*
