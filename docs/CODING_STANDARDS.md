# Relic C# and Unity Coding Standards

This document defines the coding conventions for the Relic project to ensure consistency, maintainability, and team collaboration.

---

## Table of Contents

1. [General Principles](#general-principles)
2. [Naming Conventions](#naming-conventions)
3. [Code Organization](#code-organization)
4. [Unity-Specific Guidelines](#unity-specific-guidelines)
5. [ScriptableObject Patterns](#scriptableobject-patterns)
6. [Performance Guidelines](#performance-guidelines)
7. [Documentation Standards](#documentation-standards)
8. [Git Workflow](#git-workflow)

---

## General Principles

### Core Values

1. **Readability over cleverness** - Code is read more than written
2. **Explicit over implicit** - Make intentions clear
3. **Composition over inheritance** - Prefer component-based design
4. **Fail fast** - Validate inputs, throw early
5. **Test-driven** - Write tests alongside features

### Language Version

- Use C# 9.0+ features where supported by Unity 6.x
- Target .NET Standard 2.1

---

## Naming Conventions

### General Rules

| Element | Convention | Example |
|---------|------------|---------|
| Classes | PascalCase | `UnitController` |
| Interfaces | IPascalCase | `IDamageable` |
| Methods | PascalCase | `TakeDamage()` |
| Properties | PascalCase | `MaxHealth` |
| Private fields | _camelCase | `_currentHealth` |
| Local variables | camelCase | `targetUnit` |
| Constants | SCREAMING_CASE | `MAX_UNITS` |
| Enums | PascalCase | `UnitState.Idle` |
| Parameters | camelCase | `damageAmount` |

### Unity-Specific Naming

| Element | Convention | Example |
|---------|------------|---------|
| MonoBehaviours | PascalCase | `UnitController.cs` |
| ScriptableObjects | PascalCase + SO | `UnitArchetypeSO.cs` |
| Editor scripts | PascalCase + Editor | `UnitControllerEditor.cs` |
| Prefabs | PascalCase_Variant | `Infantry_Ancient.prefab` |
| Scenes | PascalCase | `AR_Battlefield.unity` |
| Layers | PascalCase | `Units`, `Terrain` |
| Tags | PascalCase | `PlayerUnit`, `EnemyUnit` |

### File Organization

```csharp
// File: UnitController.cs
// One class per file, file name matches class name

namespace Relic.CoreRTS
{
    public class UnitController : MonoBehaviour
    {
        // ...
    }
}
```

---

## Code Organization

### Namespace Structure

```
Relic
├── Relic.CoreRTS      # Platform-independent RTS logic
├── Relic.ARLayer      # AR-specific features
├── Relic.UILayer      # UI components
├── Relic.Data         # ScriptableObjects and configs
└── Relic.Utilities    # Shared utilities
```

### Class Member Ordering

Follow this order within each class:

```csharp
public class UnitController : MonoBehaviour, IDamageable
{
    // 1. Constants
    private const float MAX_HEALTH = 100f;

    // 2. Static fields
    private static int _unitCount;

    // 3. Serialized fields (Inspector-visible)
    [Header("Configuration")]
    [SerializeField] private UnitArchetypeSO _archetype;
    [SerializeField] private float _moveSpeed = 5f;

    // 4. Private fields
    private float _currentHealth;
    private NavMeshAgent _agent;
    private Squad _squad;

    // 5. Properties
    public float Health => _currentHealth;
    public bool IsAlive => _currentHealth > 0;

    // 6. Unity Messages (lifecycle order)
    private void Awake() { }
    private void OnEnable() { }
    private void Start() { }
    private void Update() { }
    private void OnDisable() { }
    private void OnDestroy() { }

    // 7. Public methods
    public void Initialize(UnitArchetypeSO archetype) { }
    public void TakeDamage(float amount) { }

    // 8. Private methods
    private void Die() { }

    // 9. Interface implementations
    void IDamageable.OnDamage(float amount) => TakeDamage(amount);
}
```

### Region Usage

Avoid `#region` for code organization. Use proper class structure and extract classes instead.

**Exception:** Large generated code or Unity Editor scripts may use regions sparingly.

---

## Unity-Specific Guidelines

### MonoBehaviour Best Practices

```csharp
public class UnitController : MonoBehaviour
{
    // DO: Cache component references
    private NavMeshAgent _agent;

    private void Awake()
    {
        _agent = GetComponent<NavMeshAgent>();
    }

    // DON'T: Call GetComponent in Update
    private void Update()
    {
        // Bad: GetComponent<NavMeshAgent>().SetDestination(...);
        // Good: _agent.SetDestination(...);
    }
}
```

### SerializeField Guidelines

```csharp
// DO: Use SerializeField for private fields you need in Inspector
[SerializeField] private float _moveSpeed = 5f;

// DO: Use Header and Tooltip for clarity
[Header("Combat")]
[Tooltip("Damage dealt per hit")]
[SerializeField] private float _damage = 10f;

// DO: Use Range for constrained values
[SerializeField, Range(0f, 100f)] private float _health = 100f;

// DON'T: Make fields public just for Inspector access
// Bad: public float moveSpeed;
```

### Coroutine Guidelines

```csharp
// DO: Store coroutine reference for cancellation
private Coroutine _moveCoroutine;

public void MoveTo(Vector3 position)
{
    if (_moveCoroutine != null)
        StopCoroutine(_moveCoroutine);

    _moveCoroutine = StartCoroutine(MoveToPosition(position));
}

// DO: Use yield break for early exit
private IEnumerator MoveToPosition(Vector3 target)
{
    if (!IsAlive)
        yield break;

    while (Vector3.Distance(transform.position, target) > 0.1f)
    {
        // ...
        yield return null;
    }
}
```

### Event Pattern

```csharp
// DO: Use C# events with null-conditional invoke
public event Action<float> OnDamageTaken;
public event Action OnDeath;

private void TakeDamage(float amount)
{
    _currentHealth -= amount;
    OnDamageTaken?.Invoke(amount);

    if (_currentHealth <= 0)
        OnDeath?.Invoke();
}
```

---

## ScriptableObject Patterns

### Configuration Objects

```csharp
[CreateAssetMenu(fileName = "NewUnitArchetype", menuName = "Relic/Unit Archetype")]
public class UnitArchetypeSO : ScriptableObject
{
    [Header("Identity")]
    [SerializeField] private string _id;
    [SerializeField] private string _displayName;

    [Header("Stats")]
    [SerializeField] private float _baseHealth = 100f;
    [SerializeField] private float _moveSpeed = 5f;

    [Header("References")]
    [SerializeField] private WeaponStatsSO _weapon;
    [SerializeField] private GameObject _prefab;

    // Read-only properties
    public string Id => _id;
    public float BaseHealth => _baseHealth;
    public float MoveSpeed => _moveSpeed;
    public WeaponStatsSO Weapon => _weapon;
    public GameObject Prefab => _prefab;
}
```

### Curve-Based Stats

```csharp
[CreateAssetMenu(fileName = "NewWeaponStats", menuName = "Relic/Weapon Stats")]
public class WeaponStatsSO : ScriptableObject
{
    [Header("Fire Rate")]
    [SerializeField] private int _shotsPerBurst = 3;
    [SerializeField] private float _fireRate = 2f;

    [Header("Accuracy")]
    [SerializeField] private float _baseHitChance = 0.7f;
    [SerializeField] private AnimationCurve _rangeCurve = AnimationCurve.Linear(0, 1, 50, 0.2f);
    [SerializeField] private AnimationCurve _elevationCurve = AnimationCurve.Linear(-10, 0.5f, 10, 1.5f);

    [Header("Damage")]
    [SerializeField] private float _baseDamage = 25f;

    public float EvaluateHitChance(float distance, float elevationDiff)
    {
        float rangeModifier = _rangeCurve.Evaluate(distance);
        float elevationModifier = _elevationCurve.Evaluate(elevationDiff);
        return Mathf.Clamp01(_baseHitChance * rangeModifier * elevationModifier);
    }
}
```

---

## Performance Guidelines

### Avoid Allocations in Hot Paths

```csharp
// BAD: Allocates new list every frame
private void Update()
{
    var nearbyUnits = new List<UnitController>(); // GC pressure!
}

// GOOD: Reuse collections
private readonly List<UnitController> _nearbyUnits = new();

private void Update()
{
    _nearbyUnits.Clear();
    // Populate and use _nearbyUnits
}
```

### Object Pooling

```csharp
// Use Unity's built-in ObjectPool or custom pooling
private ObjectPool<Bullet> _bulletPool;

private void Awake()
{
    _bulletPool = new ObjectPool<Bullet>(
        createFunc: () => Instantiate(_bulletPrefab),
        actionOnGet: bullet => bullet.gameObject.SetActive(true),
        actionOnRelease: bullet => bullet.gameObject.SetActive(false),
        defaultCapacity: 100
    );
}
```

### Central Tick Manager Pattern

```csharp
// Instead of per-unit Update(), register with central manager
public class UnitController : MonoBehaviour, ITickable
{
    private void OnEnable() => TickManager.Register(this);
    private void OnDisable() => TickManager.Unregister(this);

    public void Tick(float deltaTime)
    {
        // Called by TickManager instead of Update()
    }
}
```

### String Operations

```csharp
// BAD: String concatenation in hot paths
Debug.Log("Unit " + _id + " took " + damage + " damage");

// GOOD: Use string interpolation or StringBuilder
Debug.Log($"Unit {_id} took {damage} damage");

// BEST: Use logging conditionals
#if UNITY_EDITOR
Debug.Log($"Unit {_id} took {damage} damage");
#endif
```

---

## Documentation Standards

### XML Documentation

```csharp
/// <summary>
/// Controls individual unit behavior including movement, combat, and death.
/// </summary>
/// <remarks>
/// Units must be initialized with an archetype before use.
/// See <see cref="Initialize(UnitArchetypeSO)"/>.
/// </remarks>
public class UnitController : MonoBehaviour
{
    /// <summary>
    /// Initializes the unit with the specified archetype configuration.
    /// </summary>
    /// <param name="archetype">The unit archetype defining stats and visuals.</param>
    /// <exception cref="ArgumentNullException">Thrown if archetype is null.</exception>
    public void Initialize(UnitArchetypeSO archetype)
    {
        if (archetype == null)
            throw new ArgumentNullException(nameof(archetype));
        // ...
    }

    /// <summary>
    /// Applies damage to the unit, potentially killing it.
    /// </summary>
    /// <param name="amount">The amount of damage to apply. Must be positive.</param>
    /// <returns>True if the unit died from this damage.</returns>
    public bool TakeDamage(float amount)
    {
        // ...
    }
}
```

### Comment Guidelines

```csharp
// DO: Explain "why", not "what"
// The AI needs a small delay before acquiring new targets to prevent
// rapid target switching when multiple enemies enter range simultaneously.
private const float TARGET_ACQUISITION_COOLDOWN = 0.5f;

// DON'T: State the obvious
// This sets the health (don't do this)
_health = 100f;
```

---

## Git Workflow

### Branch Naming

```
feature/WP-RELIC-1.1-unit-archetype-system
bugfix/combat-damage-calculation
hotfix/ar-tracking-crash
```

### Commit Messages

```
WP-RELIC-1.1: Implement UnitArchetype ScriptableObject

- Create UnitArchetypeSO with health, speed, weapon refs
- Add CreateAssetMenu for easy asset creation
- Add unit tests for archetype validation

Co-Authored-By: Claude Opus 4.5 <noreply@anthropic.com>
```

### Pull Request Template

```markdown
## Summary
Brief description of changes.

## Work Package
WP-RELIC-X.Y: Title

## Changes
- [ ] Feature 1
- [ ] Feature 2

## Testing
- [ ] Unit tests pass
- [ ] Playmode tests pass
- [ ] Tested in Flat_Debug scene
- [ ] Tested on Quest 3 (if AR changes)

## Screenshots/Videos
(If applicable)
```

---

## Code Review Checklist

Before submitting a PR, verify:

- [ ] Naming conventions followed
- [ ] No `GetComponent` in Update/FixedUpdate
- [ ] SerializeField used instead of public fields
- [ ] XML documentation on public members
- [ ] No allocations in hot paths
- [ ] Events properly unsubscribed in OnDisable/OnDestroy
- [ ] ScriptableObjects are immutable at runtime
- [ ] Tests written for new functionality
- [ ] No compiler warnings

---

## Tools and Extensions

### Recommended VS Code Extensions

- C# Dev Kit (Microsoft)
- Unity (Microsoft)
- Unity Code Snippets

### Recommended Unity Packages

- Unity Test Framework
- Unity Profiler
- Memory Profiler

---

*Document created by Agent-Dorian, 2025-12-26*
*Based on Unity and C# best practices for game development*
