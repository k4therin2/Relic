# Relic Testing Strategy

This document outlines the testing approach for the Relic project, ensuring quality and reliability across all game systems.

---

## Table of Contents

1. [Testing Philosophy](#testing-philosophy)
2. [Test Categories](#test-categories)
3. [Unity Test Framework](#unity-test-framework)
4. [Test Structure](#test-structure)
5. [Critical Path Testing](#critical-path-testing)
6. [Performance Testing](#performance-testing)
7. [AR-Specific Testing](#ar-specific-testing)
8. [CI/CD Integration](#cicd-integration)
9. [Test Checklist](#test-checklist)

---

## Testing Philosophy

### Core Principles

1. **Test-Driven Development (TDD)** - Write tests before or alongside implementation
2. **Fast Feedback** - Tests should run quickly to support rapid iteration
3. **Isolation** - Tests should not depend on each other or external state
4. **Reliability** - Tests should produce consistent results
5. **Coverage of Critical Paths** - Focus on game-breaking functionality first

### What to Test

| Priority | Category | Examples |
|----------|----------|----------|
| P0 | Core Logic | Combat calculations, pathfinding, state machines |
| P1 | Data Validation | ScriptableObject integrity, config loading |
| P2 | Integration | System interactions, event handling |
| P3 | Visual/UX | AR placement, selection visuals (manual) |

### What NOT to Unit Test

- Unity's built-in systems (NavMesh, Physics, etc.)
- Third-party packages (AR Foundation)
- Visual polish (colors, animations)
- Platform-specific hardware behavior

---

## Test Categories

### 1. Edit Mode Tests (Unit Tests)

Fast tests that run without entering Play mode. Best for pure logic.

**Use for:**
- Combat math calculations
- Data structure operations
- ScriptableObject validation
- Utility functions
- State machine transitions

```csharp
[TestFixture]
public class CombatCalculatorTests
{
    [Test]
    public void HitChance_AtMaxRange_ReturnsMinimumChance()
    {
        var calculator = new CombatCalculator();
        var weaponStats = CreateTestWeaponStats(baseHitChance: 0.7f);

        float hitChance = calculator.CalculateHitChance(
            weapon: weaponStats,
            distance: 100f,  // Max range
            elevationDiff: 0f
        );

        Assert.That(hitChance, Is.LessThan(0.3f));
    }
}
```

### 2. Play Mode Tests (Integration Tests)

Tests that run in Play mode with full Unity lifecycle.

**Use for:**
- MonoBehaviour interactions
- Coroutine behavior
- Event systems
- Multi-frame logic
- Scene loading

```csharp
[UnityTest]
public IEnumerator Unit_TakesDamage_FiresEvent()
{
    var unit = new GameObject().AddComponent<UnitController>();
    unit.Initialize(CreateTestArchetype(health: 100f));

    bool eventFired = false;
    unit.OnDamageTaken += _ => eventFired = true;

    unit.TakeDamage(25f);
    yield return null;

    Assert.That(eventFired, Is.True);
    Assert.That(unit.Health, Is.EqualTo(75f));
}
```

### 3. Manual Tests

Tests requiring human observation or interaction.

**Use for:**
- AR placement and tracking
- Visual feedback
- Controller input
- Performance "feel"
- Multi-player scenarios

---

## Unity Test Framework

### Project Setup

1. **Install Test Framework:**
   - Package Manager > Unity Registry > Test Framework
   - Already included in Unity 6.x

2. **Create Test Assemblies:**

```
Assets/
├── Scripts/
│   └── Relic.asmdef
└── Tests/
    ├── EditMode/
    │   └── Relic.Tests.EditMode.asmdef
    └── PlayMode/
        └── Relic.Tests.PlayMode.asmdef
```

3. **Edit Mode Assembly Definition:**

```json
{
    "name": "Relic.Tests.EditMode",
    "rootNamespace": "Relic.Tests",
    "references": [
        "Relic"
    ],
    "includePlatforms": [
        "Editor"
    ],
    "optionalUnityReferences": [
        "TestAssemblies"
    ]
}
```

4. **Play Mode Assembly Definition:**

```json
{
    "name": "Relic.Tests.PlayMode",
    "rootNamespace": "Relic.Tests",
    "references": [
        "Relic"
    ],
    "optionalUnityReferences": [
        "TestAssemblies"
    ]
}
```

### Running Tests

1. Open Test Runner: `Window > General > Test Runner`
2. Select Edit Mode or Play Mode tab
3. Click "Run All" or select specific tests

### Command Line (CI)

```bash
# Edit Mode Tests
Unity -runTests -testPlatform EditMode -projectPath . -testResults results.xml

# Play Mode Tests
Unity -runTests -testPlatform PlayMode -projectPath . -testResults results.xml
```

---

## Test Structure

### File Organization

```
Assets/Tests/
├── EditMode/
│   ├── CoreRTS/
│   │   ├── CombatCalculatorTests.cs
│   │   ├── CommandSystemTests.cs
│   │   ├── SquadSystemTests.cs
│   │   └── AIStateMachineTests.cs
│   ├── Data/
│   │   ├── UnitArchetypeSOTests.cs
│   │   ├── WeaponStatsSOTests.cs
│   │   └── EraConfigSOTests.cs
│   └── Utilities/
│       └── MathHelpersTests.cs
└── PlayMode/
    ├── CoreRTS/
    │   ├── UnitControllerTests.cs
    │   ├── SelectionSystemTests.cs
    │   └── PathfindingTests.cs
    ├── Integration/
    │   ├── CombatIntegrationTests.cs
    │   └── SquadCombatTests.cs
    └── Scenarios/
        └── BasicBattleTests.cs
```

### Test Naming Convention

```csharp
[Test]
public void MethodName_Condition_ExpectedResult()
{
}

// Examples:
public void TakeDamage_WhenHealthReachesZero_TriggersDeath()
public void CalculateHitChance_WithElevationAdvantage_IncreasesChance()
public void SelectUnit_WithMultipleUnits_AddsToSelection()
```

### Test Class Template

```csharp
using NUnit.Framework;
using UnityEngine;
using Relic.CoreRTS;

namespace Relic.Tests.EditMode.CoreRTS
{
    [TestFixture]
    public class CombatCalculatorTests
    {
        private CombatCalculator _calculator;

        [SetUp]
        public void SetUp()
        {
            _calculator = new CombatCalculator();
        }

        [TearDown]
        public void TearDown()
        {
            // Cleanup if needed
        }

        [Test]
        public void CalculateHitChance_BaseCase_ReturnsExpectedValue()
        {
            // Arrange
            var weapon = CreateTestWeaponStats();

            // Act
            float result = _calculator.CalculateHitChance(weapon, 10f, 0f);

            // Assert
            Assert.That(result, Is.EqualTo(0.7f).Within(0.01f));
        }

        // Test helper methods
        private WeaponStatsSO CreateTestWeaponStats()
        {
            var stats = ScriptableObject.CreateInstance<WeaponStatsSO>();
            // Configure test data
            return stats;
        }
    }
}
```

---

## Critical Path Testing

### P0: Must Test (Game-Breaking if Broken)

#### Combat System

```csharp
[TestFixture]
public class CombatSystemCriticalTests
{
    [Test]
    public void PerBulletEvaluation_EachBulletRollsIndependently()
    {
        // Verify each bullet in a burst gets its own random roll
    }

    [Test]
    public void Damage_NeverNegative_ClampsToZero()
    {
        // Edge case: damage modifiers could theoretically go negative
    }

    [Test]
    public void HitChance_AlwaysWithinBounds_Clamps0To1()
    {
        // Verify hit chance is always 0-100%
    }

    [Test]
    public void UnitDeath_OnZeroHealth_TriggersCorrectly()
    {
        // Ensure units die at 0 health, not negative
    }
}
```

#### Command System

```csharp
[TestFixture]
public class CommandSystemCriticalTests
{
    [UnityTest]
    public IEnumerator MoveCommand_ReachesDestination_WithinTimeout()
    {
        // Units must actually move to commanded positions
    }

    [Test]
    public void StopCommand_InterruptsMovement_Immediately()
    {
        // Stop must work even mid-movement
    }

    [Test]
    public void AttackCommand_TargetsEnemy_NotFriendly()
    {
        // Must not allow friendly fire without explicit permission
    }
}
```

#### State Machine

```csharp
[TestFixture]
public class AIStateMachineCriticalTests
{
    [Test]
    public void StateTransition_FromIdleToAttacking_WhenEnemyInRange()
    {
        // AI must respond to enemies
    }

    [Test]
    public void NoInfiniteLoop_BetweenStates_WithTimeout()
    {
        // Prevent state machine oscillation
    }
}
```

### P1: Should Test (Noticeable if Broken)

- ScriptableObject loading and validation
- Selection system (multi-select, deselect)
- Squad upgrade application
- Era switching

### P2: Nice to Test (Polish)

- Event firing order
- Edge cases in curves
- Numerical precision

---

## Performance Testing

### Profiling Approach

1. **Baseline Metrics:**
   - Target: 60 FPS on Quest 3 with 100v100 units
   - Budget: 16.67ms per frame

2. **Key Metrics to Track:**
   - Frame time (ms)
   - Draw calls
   - Memory allocations (GC)
   - NavMesh query time

### Performance Test Template

```csharp
[TestFixture]
public class PerformanceTests
{
    [Test]
    [Performance]
    public void Update100Units_StaysWithinBudget()
    {
        // Arrange
        var units = SpawnUnits(100);

        // Act & Measure
        Measure.Method(() =>
        {
            foreach (var unit in units)
                unit.Tick(Time.deltaTime);
        })
        .WarmupCount(5)
        .MeasurementCount(20)
        .Run();

        // Assert - check profiler results
    }

    [Test]
    public void CombatCalculation_NoGCAllocation()
    {
        // Arrange
        var calculator = new CombatCalculator();
        var weapon = CreateTestWeaponStats();

        // Pre-warm
        calculator.CalculateHitChance(weapon, 10f, 0f);

        // Act
        long allocBefore = GC.GetTotalMemory(false);
        for (int i = 0; i < 1000; i++)
        {
            calculator.CalculateHitChance(weapon, 10f, 0f);
        }
        long allocAfter = GC.GetTotalMemory(false);

        // Assert
        Assert.That(allocAfter - allocBefore, Is.LessThan(1024)); // < 1KB
    }
}
```

---

## AR-Specific Testing

### Manual AR Test Checklist

AR features require human observation. Create a checklist:

#### Battlefield Placement

- [ ] Plane detection works on various surfaces (table, floor, counter)
- [ ] Tap-to-place responds within 200ms
- [ ] Battlefield remains stable when user moves
- [ ] Battlefield scale is appropriate for surface
- [ ] Can reposition battlefield after initial placement

#### Selection (AR)

- [ ] Controller ray is visible and responsive
- [ ] Selection highlight is visible in AR
- [ ] Multi-select works with controller
- [ ] Deselect works when tapping empty space

#### Commands (AR)

- [ ] Move command indicator visible on battlefield
- [ ] Attack command shows target
- [ ] Commands work at various viewing angles
- [ ] Commands work at arm's length and close range

#### Performance (AR)

- [ ] Stable 60 FPS with 20 units
- [ ] Stable 60 FPS with 50 units
- [ ] No visible jitter at 100 units
- [ ] No tracking loss during combat

### Automated AR Validation

What CAN be automated for AR:

```csharp
[TestFixture]
public class ARValidationTests
{
    [Test]
    public void ARSession_HasRequiredComponents()
    {
        var arScene = SceneManager.LoadScene("AR_Battlefield");
        yield return null;

        Assert.That(FindObjectOfType<ARSession>(), Is.Not.Null);
        Assert.That(FindObjectOfType<ARSessionOrigin>(), Is.Not.Null);
        Assert.That(FindObjectOfType<ARPlaneManager>(), Is.Not.Null);
        Assert.That(FindObjectOfType<ARRaycastManager>(), Is.Not.Null);
    }

    [Test]
    public void Battlefield_HasCorrectLayerMask()
    {
        var battlefield = Resources.Load<GameObject>("Prefabs/BattlefieldRoot");
        Assert.That(battlefield.layer, Is.EqualTo(LayerMask.NameToLayer("Battlefield")));
    }
}
```

---

## CI/CD Integration

### GitHub Actions Workflow

```yaml
# .github/workflows/unity-tests.yml
name: Unity Tests

on:
  push:
    branches: [main, develop]
  pull_request:
    branches: [main]

jobs:
  test:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v3

      - uses: game-ci/unity-test-runner@v2
        with:
          projectPath: .
          testMode: EditMode
          artifactsPath: test-results
          githubToken: ${{ secrets.GITHUB_TOKEN }}

      - uses: game-ci/unity-test-runner@v2
        with:
          projectPath: .
          testMode: PlayMode
          artifactsPath: test-results
          githubToken: ${{ secrets.GITHUB_TOKEN }}

      - uses: actions/upload-artifact@v3
        if: always()
        with:
          name: test-results
          path: test-results
```

### Build Verification

```yaml
  build:
    needs: test
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v3

      - uses: game-ci/unity-builder@v2
        with:
          targetPlatform: Android
          projectPath: .
```

---

## Test Checklist

### Before Each PR

- [ ] All Edit Mode tests pass
- [ ] All Play Mode tests pass
- [ ] No new compiler warnings
- [ ] Coverage maintained or improved
- [ ] New features have corresponding tests

### Before Each Milestone

- [ ] Full test suite passes
- [ ] Performance benchmarks within budget
- [ ] Manual AR testing completed
- [ ] Build succeeds for Android
- [ ] Quest 3 deployment tested

### Test Coverage Goals

| System | Target Coverage |
|--------|-----------------|
| Combat Calculator | 90%+ |
| Command System | 80%+ |
| State Machine | 85%+ |
| ScriptableObjects | 75%+ |
| Overall | 70%+ |

---

## Appendix: Test Utilities

### Common Test Helpers

```csharp
public static class TestHelpers
{
    public static UnitController CreateTestUnit(float health = 100f)
    {
        var go = new GameObject("TestUnit");
        var unit = go.AddComponent<UnitController>();
        var archetype = ScriptableObject.CreateInstance<UnitArchetypeSO>();
        // Configure...
        unit.Initialize(archetype);
        return unit;
    }

    public static WeaponStatsSO CreateTestWeaponStats(
        float baseHitChance = 0.7f,
        float baseDamage = 25f)
    {
        var stats = ScriptableObject.CreateInstance<WeaponStatsSO>();
        // Configure via reflection or public setters for testing
        return stats;
    }

    public static void CleanupTestObjects()
    {
        foreach (var go in GameObject.FindObjectsOfType<GameObject>())
        {
            if (go.name.StartsWith("Test"))
                Object.DestroyImmediate(go);
        }
    }
}
```

---

*Document created by Agent-Dorian, 2025-12-26*
*Based on Unity testing best practices and TDD principles*
