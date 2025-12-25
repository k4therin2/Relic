# AR RTS Project -- Roadmap and Milestones

## Overview

This project is a **tabletop augmented reality real-time strategy (RTS)
sandbox** built in Unity targeting **Meta Quest 3 immersive AR**, with a
**non-AR debug mode** for phones and editor testing.

Core focus areas:

-   Fun at **small scope**, but supports **large unit counts** (goal: up
    to \~100 vs 100)
-   Strong emphasis on:
    -   AR usability and readability
    -   Data-driven eras (ancient, medieval, WWII, future)
    -   RTS fundamentals (selection, pathfinding, commands, AI)
    -   Combat system with:
        -   **per-bullet hit probability**
        -   **elevation bonuses**
        -   **squad upgrades**

This document defines:

-   project structure
-   major systems
-   milestones
-   acceptance criteria
-   implementation direction

------------------------------------------------------------------------

## 0. Repository and Project Structure

### Goals

-   Clean separation between **platform-independent RTS logic** and **AR
    features**
-   Reusable systems across AR and non-AR debug modes

### Suggested directory layout

    /Assets
      /Scripts
        /CoreRTS
        /ARLayer
        /UILayer
      /Configs
      /Art
        /Shared
        /Ancient
        /Medieval
        /WWII
        /Future
      /Scenes
        Boot.unity
        AR_Battlefield.unity
        Flat_Debug.unity

    /docs
      milestones.md
      design_rts_combat.md

### Technology choices

-   Unity LTS using **URP**
-   **AR Foundation** + **XR Interaction Toolkit** for Meta Quest 3
-   Android build target first
-   Phone debug version uses:
    -   non-AR camera
    -   same RTS core systems

------------------------------------------------------------------------

# Milestone 1 -- Project and AR Tabletop Foundation

### Objectives

-   Create foundational Unity project
-   Implement AR tabletop placement on Quest 3
-   Provide flat debug scene for rapid iteration

### Tasks

1.  Unity project setup
2.  Enable Android + Quest XR plug-ins
3.  Import AR Foundation and XR Interaction Toolkit
4.  Create scenes:
    -   `Boot`
    -   `AR_Battlefield`
    -   `Flat_Debug`
5.  Implement plane detection and **tap-to-place battlefield**
6.  Create `BattlefieldRoot` prefab with:
    -   visual plane
    -   optional obstacles
    -   spawn root transforms
7.  Create base `EraConfig` ScriptableObjects for:
    -   Ancient
    -   Medieval
    -   WWII
    -   Future

### Acceptance Criteria

-   Battlefield can be **placed on a real surface** using Quest 3
-   Battlefield remains stable when user moves
-   Flat debug scene runs without AR
-   Era config assets exist and can be swapped

------------------------------------------------------------------------

# Milestone 2 -- Core RTS Skeleton (Units and Commands)

### Objectives

-   Establish core unit and command systems
-   Implement selection and movement
-   Work in both AR and debug scenes

### Tasks

#### Core data types

-   `UnitArchetype` ScriptableObject including:
    -   id
    -   health
    -   move speed
    -   weapon reference
    -   unit prefab reference

#### Unit controller

-   health and movement
-   NavMeshAgent movement
-   squad reference placeholder

#### Command system

-   Command types:
    -   Move
    -   Attack (stubbed)
    -   Stop
-   Issue commands to:
    -   single unit
    -   multiple units

#### Selection system

Flat debug:

-   mouse click to select
-   drag rectangle for multi-select

AR scene:

-   controller raycast for selection
-   battlefield raycast for move command

### Acceptance Criteria

-   Units can be spawned
-   Player can select units
-   Player can move them reliably
-   Same logic works in both AR and debug modes

------------------------------------------------------------------------

# Milestone 3 -- Combat, Elevation, and Squad Upgrades

### Objectives

-   Implement full combat system
-   Per-bullet hit chance
-   Elevation bonuses
-   Squad upgrade modifiers

### Systems Introduced

#### WeaponStats ScriptableObject

Includes:

-   shots per burst
-   fire rate
-   base hit chance
-   base damage
-   effective range
-   **range hit curve**
-   **elevation bonus curve**

#### Squad system

-   List of squad members
-   List of applied upgrades
-   Utility methods returning:
    -   hit chance multiplier
    -   damage multiplier
    -   elevation bonus flat

#### Upgrade definitions

-   Upgrade ScriptableObject with multipliers for:
    -   hit chance
    -   damage
    -   elevation

#### Combat logic

Per bullet:

-   base hit chance
-   apply range curve
-   apply elevation curve
-   apply squad multipliers
-   clamp
-   random roll per bullet
-   apply damage

#### AI behavior

-   basic state machine:
    -   Idle
    -   Moving
    -   Attacking
-   auto-acquire nearest enemy

#### Era integration

-   `EraConfig` references:
    -   archetypes
    -   upgrades
    -   visuals

### Acceptance Criteria

-   Units fire in bursts
-   Elevation visibly affects outcomes
-   Upgrades affect accuracy or damage
-   Per-bullet evaluation is implemented
-   Combat works in AR and debug

------------------------------------------------------------------------

# Milestone 4 -- AR UX, Large Battles, and Performance

### Objectives

-   Improve AR usability and feedback
-   Scale to large unit counts
-   Profile and optimize

### UX Improvements

-   clear selection circles
-   team colors
-   multi-unit selection in AR
-   move/attack indicators
-   world-space UI panel for:
    -   spawning units
    -   switching era
    -   applying upgrades
    -   resetting match

### Performance tasks

-   replace per-unit Update loops with central tick manager
-   enable GPU instancing
-   add simple LOD levels
-   pool unit prefabs
-   optional aggregation of bullets rather than explicit simulation

### Acceptance Criteria

-   Comfortable, clear AR interactions
-   Smooth performance with large unit counts within cap
-   Stable AR tracking with no forced camera motion

------------------------------------------------------------------------

# Milestone 5 -- Sandbox Scenarios and Era Polish (Optional)

### Objectives

-   Add pre-made scenarios
-   Polish visuals per era

### Tasks

-   ScenarioConfig ScriptableObject
-   Scenario loader UI
-   Preset examples:
    -   hill defense
    -   archers vs infantry
    -   tanks vs infantry
    -   mechs vs armor
-   Distinct models and SFX per era
-   Basic impact and projectile VFX

### Acceptance Criteria

-   Player can:
    -   pick scenario
    -   pick era
    -   run sandbox battle
-   System supports experimentation

------------------------------------------------------------------------

## Technical Checklists

### Unity setup checklist

-   URP enabled
-   Scenes created
-   XR configured for Quest
-   Build configuration assets created

### AR integration checklist

-   ARSession + Origin in AR scene
-   AR Plane Manager
-   AR Raycast Manager
-   Tap to place battlefield

### Interaction checklist

-   gaze / controller ray working
-   battlefield surface ray detection
-   selection indicators
-   world-space command UI

### RTS systems checklist

-   unit archetypes
-   weapon stats
-   squad + upgrades
-   command queue
-   pathfinding
-   AI states

### Build and testing checklist

-   Development build variants created:
    -   Quest dev
    -   Phone debug
-   On-device profiling flow established

------------------------------------------------------------------------

## Iteration Strategy

-   Build core RTS in **Flat_Debug first**
-   Port interaction logic into AR
-   Use extremely small unit counts early
-   Scale upward only after:
    -   performance testing
    -   readability validation

Feedback loop:

-   test interaction comfort
-   test visibility and clarity
-   test combat comprehension
-   refine curves and multipliers, not core systems

Scope control rule:

-   one battlefield
-   three unit archetypes
-   one fully tuned era
-   everything else is **data layers**

------------------------------------------------------------------------

## End of Document
