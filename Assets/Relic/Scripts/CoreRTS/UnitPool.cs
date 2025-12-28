using UnityEngine;
using System;
using System.Collections.Generic;

namespace Relic.CoreRTS
{
    /// <summary>
    /// Object pool for units to reduce instantiation/destruction overhead.
    /// Pre-spawns units and reuses them instead of creating new ones.
    /// </summary>
    /// <remarks>
    /// Performance optimization for large unit counts (100v100 target).
    /// Target: Zero runtime allocations for unit spawn/despawn.
    /// See Kyle's milestones.md Milestone 4 for requirements.
    /// </remarks>
    public class UnitPool : MonoBehaviour
    {
        #region Constants

        private const int DEFAULT_MAX_POOL_SIZE = 100;
        private const int DEFAULT_INITIAL_POOL_SIZE = 10;

        #endregion

        #region Serialized Fields

        [Header("Pool Configuration")]
        [Tooltip("Maximum number of units to keep in the pool per archetype")]
        [SerializeField] private int _maxPoolSize = DEFAULT_MAX_POOL_SIZE;

        [Tooltip("Parent transform for pooled units (optional)")]
        [SerializeField] private Transform _poolParent;

        [Header("Debug")]
        [Tooltip("Log pool operations for debugging")]
        [SerializeField] private bool _enableDebugLog = false;

        #endregion

        #region Private Fields

        // Pools organized by archetype ID
        private readonly Dictionary<string, Queue<UnitController>> _pools = new();
        private readonly Dictionary<string, List<UnitController>> _activeUnits = new();
        private readonly Dictionary<UnitController, string> _unitArchetypeMap = new();

        #endregion

        #region Events

        /// <summary>
        /// Fired when a unit is spawned from the pool.
        /// </summary>
        public event Action<UnitController> OnUnitSpawned;

        /// <summary>
        /// Fired when a unit is returned to the pool.
        /// </summary>
        public event Action<UnitController> OnUnitDespawned;

        #endregion

        #region Properties

        /// <summary>
        /// Maximum number of units to keep pooled per archetype.
        /// </summary>
        public int MaxPoolSize
        {
            get => _maxPoolSize;
            set => _maxPoolSize = Mathf.Max(0, value);
        }

        /// <summary>
        /// Parent transform for pooled units.
        /// </summary>
        public Transform PoolParent
        {
            get => _poolParent;
            set => _poolParent = value;
        }

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            // Create pool parent if not set
            if (_poolParent == null)
            {
                var parentGO = new GameObject("UnitPool_Inactive");
                parentGO.SetActive(false);
                _poolParent = parentGO.transform;
                _poolParent.SetParent(transform);
            }
        }

        private void OnDestroy()
        {
            ClearAllPools();
        }

        #endregion

        #region Spawn Methods

        /// <summary>
        /// Spawns a unit from the pool, or creates a new one if pool is empty.
        /// </summary>
        /// <param name="archetype">The archetype to spawn.</param>
        /// <param name="position">World position to spawn at.</param>
        /// <param name="teamId">Team to assign the unit to.</param>
        /// <param name="rotation">Optional rotation.</param>
        /// <returns>The spawned unit controller, or null if spawn failed.</returns>
        public UnitController Spawn(UnitArchetypeSO archetype, Vector3 position, int teamId, Quaternion? rotation = null)
        {
            if (archetype == null)
            {
                LogDebug("Spawn failed: archetype is null");
                return null;
            }

            if (archetype.UnitPrefab == null)
            {
                LogDebug($"Spawn failed: archetype '{archetype.Id}' has no prefab");
                return null;
            }

            string archetypeId = archetype.Id ?? archetype.name;
            UnitController unit;

            // Try to get from pool
            if (TryGetFromPool(archetypeId, out unit))
            {
                LogDebug($"Reusing pooled unit for archetype '{archetypeId}'");
            }
            else
            {
                // Create new unit
                unit = CreateNewUnit(archetype);
                if (unit == null)
                    return null;

                LogDebug($"Created new unit for archetype '{archetypeId}'");
            }

            // Initialize and activate
            ActivateUnit(unit, archetype, position, teamId, rotation ?? Quaternion.identity);

            // Track as active
            TrackActiveUnit(unit, archetypeId);

            OnUnitSpawned?.Invoke(unit);
            return unit;
        }

        #endregion

        #region Despawn Methods

        /// <summary>
        /// Returns a unit to the pool for reuse.
        /// </summary>
        /// <param name="unit">The unit to despawn.</param>
        public void Despawn(UnitController unit)
        {
            if (unit == null)
                return;

            // Get archetype ID
            if (!_unitArchetypeMap.TryGetValue(unit, out string archetypeId))
            {
                LogDebug($"Despawn failed: unit '{unit.name}' not tracked");
                SafeDestroy(unit.gameObject);
                return;
            }

            // Clean up unit state
            ResetUnitState(unit);

            // Remove from active tracking
            UntrackActiveUnit(unit, archetypeId);

            // Return to pool or destroy if over capacity
            if (ShouldReturnToPool(archetypeId))
            {
                ReturnToPool(unit, archetypeId);
                LogDebug($"Returned unit to pool for archetype '{archetypeId}'");
            }
            else
            {
                _unitArchetypeMap.Remove(unit);
                SafeDestroy(unit.gameObject);
                LogDebug($"Pool full, destroyed unit for archetype '{archetypeId}'");
            }

            OnUnitDespawned?.Invoke(unit);
        }

        #endregion

        #region Warm-Up Methods

        /// <summary>
        /// Pre-spawns units into the pool for faster runtime spawning.
        /// </summary>
        /// <param name="archetype">The archetype to warm up.</param>
        /// <param name="count">Number of units to pre-spawn.</param>
        public void WarmUp(UnitArchetypeSO archetype, int count)
        {
            if (archetype == null || count <= 0)
                return;

            string archetypeId = archetype.Id ?? archetype.name;

            for (int i = 0; i < count; i++)
            {
                // Check if we're at max capacity
                if (!ShouldReturnToPool(archetypeId))
                    break;

                var unit = CreateNewUnit(archetype);
                if (unit == null)
                    break;

                // Deactivate and add to pool
                unit.gameObject.SetActive(false);
                unit.transform.SetParent(_poolParent);
                ReturnToPool(unit, archetypeId);
            }

            LogDebug($"Warmed up {GetPooledCount(archetype)} units for archetype '{archetypeId}'");
        }

        #endregion

        #region Pool Statistics

        /// <summary>
        /// Gets the number of pooled (inactive) units for an archetype.
        /// </summary>
        /// <param name="archetype">The archetype to check.</param>
        /// <returns>Number of pooled units.</returns>
        public int GetPooledCount(UnitArchetypeSO archetype)
        {
            if (archetype == null)
                return 0;

            string archetypeId = archetype.Id ?? archetype.name;
            return _pools.TryGetValue(archetypeId, out var pool) ? pool.Count : 0;
        }

        /// <summary>
        /// Gets the number of active (spawned) units for an archetype.
        /// </summary>
        /// <param name="archetype">The archetype to check.</param>
        /// <returns>Number of active units.</returns>
        public int GetActiveCount(UnitArchetypeSO archetype)
        {
            if (archetype == null)
                return 0;

            string archetypeId = archetype.Id ?? archetype.name;
            return _activeUnits.TryGetValue(archetypeId, out var active) ? active.Count : 0;
        }

        /// <summary>
        /// Gets the total number of pooled units across all archetypes.
        /// </summary>
        /// <returns>Total pooled unit count.</returns>
        public int GetTotalPooledCount()
        {
            int total = 0;
            foreach (var pool in _pools.Values)
            {
                total += pool.Count;
            }
            return total;
        }

        /// <summary>
        /// Gets the total number of active units across all archetypes.
        /// </summary>
        /// <returns>Total active unit count.</returns>
        public int GetTotalActiveCount()
        {
            int total = 0;
            foreach (var active in _activeUnits.Values)
            {
                total += active.Count;
            }
            return total;
        }

        #endregion

        #region Pool Management

        /// <summary>
        /// Clears the pool for a specific archetype.
        /// </summary>
        /// <param name="archetype">The archetype to clear.</param>
        public void ClearPool(UnitArchetypeSO archetype)
        {
            if (archetype == null)
                return;

            string archetypeId = archetype.Id ?? archetype.name;

            if (_pools.TryGetValue(archetypeId, out var pool))
            {
                while (pool.Count > 0)
                {
                    var unit = pool.Dequeue();
                    if (unit != null)
                    {
                        _unitArchetypeMap.Remove(unit);
                        SafeDestroy(unit.gameObject);
                    }
                }
                _pools.Remove(archetypeId);
            }

            LogDebug($"Cleared pool for archetype '{archetypeId}'");
        }

        /// <summary>
        /// Clears all pools and destroys all pooled units.
        /// </summary>
        public void ClearAllPools()
        {
            foreach (var pool in _pools.Values)
            {
                while (pool.Count > 0)
                {
                    var unit = pool.Dequeue();
                    if (unit != null)
                    {
                        _unitArchetypeMap.Remove(unit);
                        SafeDestroy(unit.gameObject);
                    }
                }
            }

            _pools.Clear();
            LogDebug("Cleared all pools");
        }

        #endregion

        #region Private Methods

        private bool TryGetFromPool(string archetypeId, out UnitController unit)
        {
            unit = null;

            if (!_pools.TryGetValue(archetypeId, out var pool))
                return false;

            while (pool.Count > 0)
            {
                unit = pool.Dequeue();
                if (unit != null && unit.gameObject != null)
                    return true;
            }

            return false;
        }

        private UnitController CreateNewUnit(UnitArchetypeSO archetype)
        {
            if (archetype.UnitPrefab == null)
                return null;

            GameObject unitGO = Instantiate(archetype.UnitPrefab);

            UnitController controller = unitGO.GetComponent<UnitController>();
            if (controller == null)
            {
                controller = unitGO.AddComponent<UnitController>();
            }

            // Ensure UnitAI is present
            if (unitGO.GetComponent<UnitAI>() == null)
            {
                unitGO.AddComponent<UnitAI>();
            }

            string archetypeId = archetype.Id ?? archetype.name;
            _unitArchetypeMap[controller] = archetypeId;

            return controller;
        }

        private void ActivateUnit(UnitController unit, UnitArchetypeSO archetype, Vector3 position, int teamId, Quaternion rotation)
        {
            // Set position and rotation
            unit.transform.position = position;
            unit.transform.rotation = rotation;

            // Unparent from pool
            unit.transform.SetParent(null);

            // Initialize the unit
            unit.Initialize(archetype, teamId);

            // Activate
            unit.gameObject.SetActive(true);

            // Name for debugging
            unit.gameObject.name = $"{archetype.DisplayName ?? archetype.Id}_Team{teamId}_{GetTotalActiveCount()}";
        }

        private void ResetUnitState(UnitController unit)
        {
            // Remove from squad
            if (unit.IsInSquad)
            {
                unit.LeaveSquad();
            }

            // Stop any movement
            unit.Stop();

            // Disable AI if present
            var ai = unit.GetComponent<UnitAI>();
            if (ai != null)
            {
                ai.SetAIEnabled(false);
                ai.ClearTarget();
            }

            // Deselect
            unit.SetSelected(false);

            // Deactivate
            unit.gameObject.SetActive(false);

            // Parent to pool
            unit.transform.SetParent(_poolParent);
        }

        private void TrackActiveUnit(UnitController unit, string archetypeId)
        {
            if (!_activeUnits.TryGetValue(archetypeId, out var active))
            {
                active = new List<UnitController>();
                _activeUnits[archetypeId] = active;
            }

            if (!active.Contains(unit))
            {
                active.Add(unit);
            }
        }

        private void UntrackActiveUnit(UnitController unit, string archetypeId)
        {
            if (_activeUnits.TryGetValue(archetypeId, out var active))
            {
                active.Remove(unit);
            }
        }

        private bool ShouldReturnToPool(string archetypeId)
        {
            if (!_pools.TryGetValue(archetypeId, out var pool))
                return true;

            return pool.Count < _maxPoolSize;
        }

        private void ReturnToPool(UnitController unit, string archetypeId)
        {
            if (!_pools.TryGetValue(archetypeId, out var pool))
            {
                pool = new Queue<UnitController>();
                _pools[archetypeId] = pool;
            }

            pool.Enqueue(unit);
        }

        private void SafeDestroy(GameObject gameObjectToDestroy)
        {
            if (gameObjectToDestroy == null)
                return;

#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                DestroyImmediate(gameObjectToDestroy);
                return;
            }
#endif
            Destroy(gameObjectToDestroy);
        }

        private void LogDebug(string message)
        {
            if (_enableDebugLog)
            {
                Debug.Log($"[UnitPool] {message}");
            }
        }

        #endregion
    }
}
