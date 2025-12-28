using UnityEngine;
using System;
using System.Collections.Generic;

namespace Relic.CoreRTS
{
    /// <summary>
    /// AI controller for units. Implements a simple state machine with
    /// Idle, Moving, and Attacking states.
    /// </summary>
    /// <remarks>
    /// Attach to unit prefabs alongside UnitController.
    /// See Kyle's milestones.md Milestone 3 for AI requirements.
    /// </remarks>
    [RequireComponent(typeof(UnitController))]
    public class UnitAI : MonoBehaviour
    {
        #region Constants

        private const float DEFAULT_DETECTION_RADIUS = 15f;
        private const float MIN_DETECTION_RADIUS = 1f;
        private const float MAX_DETECTION_RADIUS = 100f;
        private const float SCAN_INTERVAL = 0.25f; // Scan for enemies every 0.25 seconds
        private const float FIRE_CHECK_INTERVAL = 0.1f;
        private const int MAX_OVERLAP_RESULTS = 50;

        #endregion

        #region Serialized Fields

        [Header("Detection")]
        [Tooltip("Radius within which the AI can detect enemies")]
        [Range(MIN_DETECTION_RADIUS, MAX_DETECTION_RADIUS)]
        [SerializeField] private float _detectionRadius = DEFAULT_DETECTION_RADIUS;

        [Tooltip("Layer mask for enemy detection")]
        [SerializeField] private LayerMask _detectionLayerMask = ~0; // All layers

        [Header("Combat")]
        [Tooltip("Automatically engage enemies in range")]
        [SerializeField] private bool _autoEngage = true;

        [Tooltip("Time between firing bursts")]
        [SerializeField] private float _attackCooldown = 1f;

        [Header("Debug")]
        [Tooltip("Show detection radius gizmo")]
        [SerializeField] private bool _showDetectionGizmo = true;

        #endregion

        #region Private Fields

        private UnitController _unitController;
        private AIState _currentState = AIState.Idle;
        private UnitController _currentTarget;
        private Vector3 _moveDestination;
        private bool _isAIEnabled = true;

        private float _lastScanTime;
        private float _lastFireTime;

        // Cached array for Physics.OverlapSphereNonAlloc
        private readonly Collider[] _overlapResults = new Collider[MAX_OVERLAP_RESULTS];

        #endregion

        #region Events

        /// <summary>
        /// Fired when the AI state changes.
        /// </summary>
        public event Action<AIState> OnStateChanged;

        /// <summary>
        /// Fired when the AI acquires a new target.
        /// </summary>
        public event Action<UnitController> OnTargetAcquired;

        /// <summary>
        /// Fired when the AI loses its target.
        /// </summary>
        public event Action OnTargetLost;

        #endregion

        #region Properties

        /// <summary>Current AI state.</summary>
        public AIState CurrentState => _currentState;

        /// <summary>Current combat target.</summary>
        public UnitController CurrentTarget => _currentTarget;

        /// <summary>Whether the AI has a target.</summary>
        public bool HasTarget => _currentTarget != null && _currentTarget.IsAlive;

        /// <summary>Detection radius for enemy scanning.</summary>
        public float DetectionRadius
        {
            get => _detectionRadius;
            set => _detectionRadius = Mathf.Clamp(value, MIN_DETECTION_RADIUS, MAX_DETECTION_RADIUS);
        }

        /// <summary>Whether AI processing is enabled.</summary>
        public bool IsAIEnabled => _isAIEnabled;

        /// <summary>Reference to the UnitController.</summary>
        public UnitController UnitController => _unitController;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            _unitController = GetComponent<UnitController>();
        }

        private void Update()
        {
            if (!_isAIEnabled || _unitController == null || !_unitController.IsAlive)
                return;

            // Run state-specific update
            switch (_currentState)
            {
                case AIState.Idle:
                    UpdateIdleState();
                    break;
                case AIState.Moving:
                    UpdateMovingState();
                    break;
                case AIState.Attacking:
                    UpdateAttackingState();
                    break;
            }
        }

        #endregion

        #region State Management

        /// <summary>
        /// Sets the AI state.
        /// </summary>
        /// <param name="newState">The new state.</param>
        public void SetState(AIState newState)
        {
            if (_currentState == newState)
                return;

            AIState previousState = _currentState;
            _currentState = newState;

            // Exit previous state
            OnExitState(previousState);

            // Enter new state
            OnEnterState(newState);

            OnStateChanged?.Invoke(_currentState);
        }

        private void OnEnterState(AIState state)
        {
            switch (state)
            {
                case AIState.Idle:
                    _unitController.Stop();
                    break;
                case AIState.Moving:
                    // Movement is initiated via MoveTo()
                    break;
                case AIState.Attacking:
                    _unitController.Stop();
                    break;
            }
        }

        private void OnExitState(AIState state)
        {
            // Cleanup if needed when leaving states
        }

        #endregion

        #region State Updates

        private void UpdateIdleState()
        {
            // Periodically scan for enemies
            if (_autoEngage && Time.time - _lastScanTime >= SCAN_INTERVAL)
            {
                _lastScanTime = Time.time;
                var enemy = FindNearestEnemy();
                if (enemy != null)
                {
                    SetTarget(enemy);
                }
            }
        }

        private void UpdateMovingState()
        {
            // Check if reached destination
            if (!_unitController.IsMoving)
            {
                SetState(AIState.Idle);
                return;
            }

            // Scan for enemies while moving
            if (_autoEngage && Time.time - _lastScanTime >= SCAN_INTERVAL)
            {
                _lastScanTime = Time.time;
                var enemy = FindNearestEnemy();
                if (enemy != null)
                {
                    SetTarget(enemy);
                }
            }
        }

        private void UpdateAttackingState()
        {
            // Check if target is still valid
            if (!HasTarget)
            {
                ClearTarget();
                return;
            }

            // Check if target is still in range
            if (!IsEnemyInRange(_currentTarget))
            {
                // Try to find a new target in range
                var newTarget = FindNearestEnemy();
                if (newTarget != null)
                {
                    SetTarget(newTarget);
                }
                else
                {
                    ClearTarget();
                }
                return;
            }

            // Face the target
            LookAtTarget();

            // Fire at target
            if (Time.time - _lastFireTime >= _attackCooldown)
            {
                _lastFireTime = Time.time;
                FireAtTarget();
            }
        }

        #endregion

        #region Target Management

        /// <summary>
        /// Sets the current target.
        /// </summary>
        /// <param name="target">The target to attack.</param>
        public void SetTarget(UnitController target)
        {
            if (target == null || target == _unitController || !target.IsAlive)
                return;

            // Don't target friendly units
            if (target.TeamId == _unitController.TeamId)
                return;

            _currentTarget = target;
            SetState(AIState.Attacking);
            OnTargetAcquired?.Invoke(_currentTarget);
        }

        /// <summary>
        /// Clears the current target.
        /// </summary>
        public void ClearTarget()
        {
            if (_currentTarget == null)
                return;

            _currentTarget = null;
            SetState(AIState.Idle);
            OnTargetLost?.Invoke();
        }

        private void LookAtTarget()
        {
            if (_currentTarget == null)
                return;

            Vector3 direction = _currentTarget.transform.position - transform.position;
            direction.y = 0; // Keep upright
            if (direction.sqrMagnitude > 0.001f)
            {
                transform.rotation = Quaternion.LookRotation(direction);
            }
        }

        private void FireAtTarget()
        {
            if (_currentTarget == null || _unitController.Archetype == null)
                return;

            // Get weapon from archetype
            var weapon = _unitController.Archetype.Weapon;
            if (weapon == null)
                return;

            // Resolve combat
            var result = CombatResolver.ResolveCombat(_unitController, _currentTarget, weapon);

            // Log result for debugging
            if (result.ShotsFired > 0)
            {
                Debug.Log($"[UnitAI] {gameObject.name} fired at {_currentTarget.name}: {result}");
            }

            // Check if target was destroyed
            if (result.TargetDestroyed)
            {
                var newTarget = FindNearestEnemy();
                if (newTarget != null)
                {
                    SetTarget(newTarget);
                }
                else
                {
                    ClearTarget();
                }
            }
        }

        #endregion

        #region Enemy Detection

        /// <summary>
        /// Finds the nearest enemy within detection radius.
        /// </summary>
        /// <returns>The nearest enemy, or null if none found.</returns>
        public UnitController FindNearestEnemy()
        {
            int count = Physics.OverlapSphereNonAlloc(
                transform.position,
                _detectionRadius,
                _overlapResults,
                _detectionLayerMask
            );

            UnitController nearestEnemy = null;
            float nearestDistance = float.MaxValue;

            for (int i = 0; i < count; i++)
            {
                var unit = _overlapResults[i].GetComponent<UnitController>();
                if (unit == null || unit == _unitController || !unit.IsAlive)
                    continue;

                // Check if enemy (different team)
                if (unit.TeamId == _unitController.TeamId)
                    continue;

                float distance = Vector3.Distance(transform.position, unit.transform.position);
                if (distance < nearestDistance)
                {
                    nearestDistance = distance;
                    nearestEnemy = unit;
                }
            }

            return nearestEnemy;
        }

        /// <summary>
        /// Checks if an enemy is within detection range.
        /// </summary>
        /// <param name="enemy">The enemy to check.</param>
        /// <returns>True if the enemy is in range and is actually an enemy.</returns>
        public bool IsEnemyInRange(UnitController enemy)
        {
            if (enemy == null || enemy == _unitController || !enemy.IsAlive)
                return false;

            // Must be a different team
            if (enemy.TeamId == _unitController.TeamId)
                return false;

            float distance = Vector3.Distance(transform.position, enemy.transform.position);
            return distance <= _detectionRadius;
        }

        /// <summary>
        /// Gets all enemies within detection radius.
        /// </summary>
        /// <returns>List of enemy units in range.</returns>
        public List<UnitController> GetEnemiesInRange()
        {
            var enemies = new List<UnitController>();

            int count = Physics.OverlapSphereNonAlloc(
                transform.position,
                _detectionRadius,
                _overlapResults,
                _detectionLayerMask
            );

            for (int i = 0; i < count; i++)
            {
                var unit = _overlapResults[i].GetComponent<UnitController>();
                if (unit == null || unit == _unitController || !unit.IsAlive)
                    continue;

                if (unit.TeamId != _unitController.TeamId)
                {
                    enemies.Add(unit);
                }
            }

            return enemies;
        }

        #endregion

        #region Movement

        /// <summary>
        /// Commands the AI to move to a position.
        /// </summary>
        /// <param name="destination">The destination position.</param>
        public void MoveTo(Vector3 destination)
        {
            _moveDestination = destination;

            if (_unitController.MoveTo(destination))
            {
                SetState(AIState.Moving);
            }
        }

        /// <summary>
        /// Stops all AI-controlled movement and clears target.
        /// </summary>
        public void Stop()
        {
            _unitController.Stop();
            ClearTarget();
            SetState(AIState.Idle);
        }

        #endregion

        #region AI Enable/Disable

        /// <summary>
        /// Enables or disables AI processing.
        /// </summary>
        /// <param name="enabled">True to enable AI.</param>
        public void SetAIEnabled(bool enabled)
        {
            _isAIEnabled = enabled;

            if (!enabled)
            {
                ClearTarget();
            }
        }

        #endregion

        #region Debug

        private void OnDrawGizmosSelected()
        {
            if (!_showDetectionGizmo)
                return;

            // Draw detection radius
            Gizmos.color = new Color(1f, 0.5f, 0f, 0.3f);
            Gizmos.DrawWireSphere(transform.position, _detectionRadius);

            // Draw line to current target
            if (_currentTarget != null)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawLine(transform.position, _currentTarget.transform.position);
            }
        }

        #endregion
    }

    /// <summary>
    /// AI states for units.
    /// </summary>
    public enum AIState
    {
        /// <summary>Unit is idle, scanning for enemies.</summary>
        Idle,

        /// <summary>Unit is moving to a destination.</summary>
        Moving,

        /// <summary>Unit is attacking a target.</summary>
        Attacking
    }
}
