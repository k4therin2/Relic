using UnityEngine;
using UnityEngine.AI;
using System;

namespace Relic.CoreRTS
{
    /// <summary>
    /// Core component that controls a unit instance. Handles health, movement,
    /// team assignment, and combat state (combat logic in M3).
    /// </summary>
    /// <remarks>
    /// Attached to unit prefabs. Initialized from UnitArchetypeSO via UnitFactory.
    /// Uses ITickable for centralized tick updates (performance optimization).
    /// See Kyle's milestones.md Milestone 2 for requirements.
    /// </remarks>
    [RequireComponent(typeof(Collider))]
    public class UnitController : MonoBehaviour, ITickable
    {
        #region Serialized Fields

        [Header("Configuration")]
        [Tooltip("Reference to the archetype this unit was spawned from")]
        [SerializeField] private UnitArchetypeSO _archetype;

        [Header("Team Assignment")]
        [Tooltip("Team/faction this unit belongs to (0 = Red/Player1, 1 = Blue/Player2)")]
        [SerializeField] private int _teamId;

        [Header("Visual Feedback")]
        [Tooltip("Transform for selection indicator (defaults to this transform)")]
        [SerializeField] private Transform _selectionIndicatorPoint;

        [Tooltip("Transform for health bar display (defaults to this transform)")]
        [SerializeField] private Transform _healthBarPoint;

        #endregion

        #region Runtime State

        private UnitStats _stats;
        private NavMeshAgent _navAgent;
        private bool _isSelected;
        private UnitState _currentState = UnitState.Idle;

        // Squad reference for M3 combat modifiers
        private Squad _squad;
        private int _squadId = -1;

        #endregion

        #region Events

        /// <summary>
        /// Fired when the unit's health changes.
        /// Parameters: (currentHealth, maxHealth, damageOrHealAmount)
        /// </summary>
        public event Action<int, int, int> OnHealthChanged;

        /// <summary>
        /// Fired when the unit dies.
        /// </summary>
        public event Action OnDeath;

        /// <summary>
        /// Fired when the unit's state changes.
        /// </summary>
        public event Action<UnitState> OnStateChanged;

        /// <summary>
        /// Fired when the unit's selection status changes.
        /// </summary>
        public event Action<bool> OnSelectionChanged;

        #endregion

        #region Properties

        public UnitArchetypeSO Archetype => _archetype;
        public UnitStats Stats => _stats;
        public int TeamId => _teamId;
        public int SquadId => _squadId;
        public Squad Squad => _squad;
        public bool IsInSquad => _squad != null;
        public bool IsSelected => _isSelected;
        public UnitState CurrentState => _currentState;
        public bool IsAlive => _stats.IsAlive;
        public float HealthPercent => _stats.HealthPercent;
        public Transform SelectionIndicatorPoint => _selectionIndicatorPoint != null ? _selectionIndicatorPoint : transform;
        public Transform HealthBarPoint => _healthBarPoint != null ? _healthBarPoint : transform;

        /// <summary>
        /// Gets whether the unit is currently moving.
        /// </summary>
        public bool IsMoving => _currentState == UnitState.Moving &&
                                _navAgent != null &&
                                _navAgent.enabled &&
                                !_navAgent.isStopped &&
                                _navAgent.remainingDistance > _navAgent.stoppingDistance;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            // Get or add NavMeshAgent
            _navAgent = GetComponent<NavMeshAgent>();
            if (_navAgent == null)
            {
                _navAgent = gameObject.AddComponent<NavMeshAgent>();
            }

            // Setup defaults for selection indicator
            if (_selectionIndicatorPoint == null)
                _selectionIndicatorPoint = transform;
            if (_healthBarPoint == null)
                _healthBarPoint = transform;
        }

        private void Start()
        {
            // Initialize if archetype is set in inspector
            if (_archetype != null && string.IsNullOrEmpty(_stats.ArchetypeId))
            {
                Initialize(_archetype, _teamId);
            }

            // Register with TickManager for centralized updates
            if (TickManager.HasInstance || Application.isPlaying)
            {
                TickManager.Instance?.Register(this);
            }
        }

        private void OnDestroy()
        {
            // Unregister from TickManager
            if (TickManager.HasInstance)
            {
                TickManager.Instance?.Unregister(this);
            }
        }

        #endregion

        #region ITickable Implementation

        /// <summary>
        /// Movement state checks run at Normal priority (~30 Hz).
        /// </summary>
        public TickPriority Priority => TickPriority.Normal;

        /// <summary>
        /// Tick is active when the unit is alive and moving.
        /// </summary>
        public bool IsTickActive => IsAlive && _currentState == UnitState.Moving;

        /// <summary>
        /// Called by TickManager to update movement state.
        /// </summary>
        /// <param name="deltaTime">Time since last tick.</param>
        public void OnTick(float deltaTime)
        {
            // Check if movement has completed
            if (_currentState == UnitState.Moving && !IsMoving)
            {
                SetState(UnitState.Idle);
            }
        }

        #endregion

        #region Initialization

        /// <summary>
        /// Initializes the unit from an archetype.
        /// Called by UnitFactory when spawning.
        /// </summary>
        /// <param name="archetype">The archetype to initialize from.</param>
        /// <param name="teamId">The team this unit belongs to.</param>
        public void Initialize(UnitArchetypeSO archetype, int teamId)
        {
            if (archetype == null)
            {
                Debug.LogError($"[UnitController] Cannot initialize with null archetype on {gameObject.name}");
                return;
            }

            _archetype = archetype;
            _teamId = teamId;
            _stats = archetype.CreateStats();

            // Configure NavMeshAgent from archetype
            if (_navAgent != null)
            {
                _navAgent.speed = _stats.MoveSpeed;
                _navAgent.stoppingDistance = 0.1f;
                _navAgent.angularSpeed = 360f;
                _navAgent.acceleration = 8f;
            }

            // Apply scale from archetype
            if (archetype.Scale != 1f)
            {
                transform.localScale = Vector3.one * archetype.Scale;
            }

            SetState(UnitState.Idle);
        }

        /// <summary>
        /// Assigns the unit to a squad.
        /// </summary>
        /// <param name="squad">The squad to join.</param>
        /// <returns>True if successfully joined the squad.</returns>
        public bool JoinSquad(Squad squad)
        {
            if (squad == null)
                return false;

            // Leave current squad if in one
            if (_squad != null)
                LeaveSquad();

            if (squad.AddMember(this))
            {
                _squad = squad;
                _squadId = _squad.Id.GetHashCode();
                return true;
            }

            return false;
        }

        /// <summary>
        /// Removes the unit from its current squad.
        /// </summary>
        /// <returns>True if successfully left the squad.</returns>
        public bool LeaveSquad()
        {
            if (_squad == null)
                return false;

            _squad.RemoveMember(this);
            _squad = null;
            _squadId = -1;
            return true;
        }

        /// <summary>
        /// Legacy method for squad assignment by ID (for backwards compatibility).
        /// </summary>
        /// <param name="squadId">The squad ID to assign.</param>
        [System.Obsolete("Use JoinSquad(Squad) instead.")]
        public void AssignToSquad(int squadId)
        {
            _squadId = squadId;
        }

        /// <summary>
        /// Gets the squad's hit chance multiplier, or 1.0 if not in a squad.
        /// </summary>
        public float GetSquadHitChanceMultiplier()
        {
            return _squad?.HitChanceMultiplier ?? 1f;
        }

        /// <summary>
        /// Gets the squad's damage multiplier, or 1.0 if not in a squad.
        /// </summary>
        public float GetSquadDamageMultiplier()
        {
            return _squad?.DamageMultiplier ?? 1f;
        }

        /// <summary>
        /// Gets the squad's elevation bonus, or 0 if not in a squad.
        /// </summary>
        public float GetSquadElevationBonus()
        {
            return _squad?.ElevationBonusFlat ?? 0f;
        }

        #endregion

        #region Movement

        /// <summary>
        /// Commands the unit to move to a position.
        /// </summary>
        /// <param name="destination">World position to move to.</param>
        /// <returns>True if movement command was accepted.</returns>
        public bool MoveTo(Vector3 destination)
        {
            if (!IsAlive || _navAgent == null || !_navAgent.isOnNavMesh)
            {
                return false;
            }

            if (_navAgent.SetDestination(destination))
            {
                _navAgent.isStopped = false;
                SetState(UnitState.Moving);
                return true;
            }

            return false;
        }

        /// <summary>
        /// Commands the unit to stop moving.
        /// </summary>
        public void Stop()
        {
            if (_navAgent != null && _navAgent.enabled && _navAgent.isOnNavMesh)
            {
                _navAgent.isStopped = true;
                _navAgent.ResetPath();
            }

            if (_currentState == UnitState.Moving)
            {
                SetState(UnitState.Idle);
            }
        }

        #endregion

        #region Health/Combat

        /// <summary>
        /// Applies damage to the unit.
        /// </summary>
        /// <param name="damage">Raw damage to apply.</param>
        /// <returns>Actual damage applied after armor.</returns>
        public int TakeDamage(int damage)
        {
            if (!IsAlive) return 0;

            int actualDamage = _stats.ApplyDamage(damage);
            OnHealthChanged?.Invoke(_stats.CurrentHealth, _stats.MaxHealth, -actualDamage);

            if (!_stats.IsAlive)
            {
                Die();
            }

            return actualDamage;
        }

        /// <summary>
        /// Heals the unit.
        /// </summary>
        /// <param name="amount">Amount to heal.</param>
        /// <returns>Actual amount healed.</returns>
        public int Heal(int amount)
        {
            if (!IsAlive) return 0;

            int actualHeal = _stats.Heal(amount);
            if (actualHeal > 0)
            {
                OnHealthChanged?.Invoke(_stats.CurrentHealth, _stats.MaxHealth, actualHeal);
            }

            return actualHeal;
        }

        /// <summary>
        /// Called when the unit dies.
        /// </summary>
        private void Die()
        {
            SetState(UnitState.Dead);
            Stop();

            // Play death sound if available
            if (_archetype != null && _archetype.DeathSound != null)
            {
                AudioSource.PlayClipAtPoint(_archetype.DeathSound, transform.position);
            }

            OnDeath?.Invoke();

            // Disable components
            if (_navAgent != null)
                _navAgent.enabled = false;

            // Disable collider to prevent further interactions
            var collider = GetComponent<Collider>();
            if (collider != null)
                collider.enabled = false;
        }

        #endregion

        #region Selection

        /// <summary>
        /// Sets the selection state of this unit.
        /// </summary>
        /// <param name="selected">True if selected, false otherwise.</param>
        public void SetSelected(bool selected)
        {
            if (_isSelected == selected) return;

            _isSelected = selected;
            OnSelectionChanged?.Invoke(_isSelected);

            // Play selection sound
            if (selected && _archetype != null && _archetype.SelectSound != null)
            {
                AudioSource.PlayClipAtPoint(_archetype.SelectSound, transform.position, 0.5f);
            }
        }

        #endregion

        #region State Management

        /// <summary>
        /// Sets the unit's current state.
        /// </summary>
        /// <param name="newState">The new state.</param>
        private void SetState(UnitState newState)
        {
            if (_currentState == newState) return;

            _currentState = newState;
            OnStateChanged?.Invoke(_currentState);
        }

        #endregion

        #region Debug

        private void OnDrawGizmosSelected()
        {
            if (!Application.isPlaying) return;

            // Draw detection range
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, _stats.DetectionRange);

            // Draw movement path if moving
            if (_navAgent != null && _navAgent.hasPath)
            {
                Gizmos.color = Color.green;
                var corners = _navAgent.path.corners;
                for (int i = 0; i < corners.Length - 1; i++)
                {
                    Gizmos.DrawLine(corners[i], corners[i + 1]);
                }
            }
        }

        #endregion
    }

    /// <summary>
    /// Possible states for a unit.
    /// </summary>
    public enum UnitState
    {
        /// <summary>Unit is idle and not performing any action.</summary>
        Idle,

        /// <summary>Unit is moving to a destination.</summary>
        Moving,

        /// <summary>Unit is attacking a target (M3).</summary>
        Attacking,

        /// <summary>Unit is dead and awaiting cleanup.</summary>
        Dead
    }
}
