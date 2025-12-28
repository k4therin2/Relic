using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Relic.CoreRTS
{
    /// <summary>
    /// Factory for spawning and tracking units on the battlefield.
    /// Responsible for instantiating units from archetypes and maintaining
    /// a registry of all active units.
    /// </summary>
    /// <remarks>
    /// UnitFactory is typically attached to the BattlefieldRoot or a game manager.
    /// See Kyle's milestones.md Milestone 2 for requirements.
    /// </remarks>
    public class UnitFactory : MonoBehaviour
    {
        #region Serialized Fields

        [Header("Configuration")]
        [Tooltip("Parent transform for spawned units (optional)")]
        [SerializeField] private Transform _unitsParent;

        [Header("Spawn Settings")]
        [Tooltip("Apply height offset from archetype on spawn")]
        [SerializeField] private bool _applyHeightOffset = true;

        #endregion

        #region Runtime State

        private readonly List<UnitController> _allUnits = new List<UnitController>();
        private readonly Dictionary<int, List<UnitController>> _unitsByTeam = new Dictionary<int, List<UnitController>>();

        #endregion

        #region Events

        /// <summary>
        /// Fired when a unit is spawned.
        /// </summary>
        public event Action<UnitController> OnUnitSpawned;

        /// <summary>
        /// Fired when a unit is destroyed.
        /// </summary>
        public event Action<UnitController> OnUnitDestroyed;

        #endregion

        #region Properties

        /// <summary>
        /// Total number of active units.
        /// </summary>
        public int UnitCount => _allUnits.Count;

        /// <summary>
        /// Parent transform for spawned units.
        /// </summary>
        public Transform UnitsParent
        {
            get => _unitsParent;
            set => _unitsParent = value;
        }

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            // Initialize team dictionaries for common teams
            _unitsByTeam[SpawnPoint.TEAM_RED] = new List<UnitController>();
            _unitsByTeam[SpawnPoint.TEAM_BLUE] = new List<UnitController>();
        }

        #endregion

        #region Spawning Methods

        /// <summary>
        /// Spawns a unit from an archetype at a position.
        /// </summary>
        /// <param name="archetype">The archetype to spawn from.</param>
        /// <param name="position">World position to spawn at.</param>
        /// <param name="teamId">Team to assign the unit to.</param>
        /// <param name="rotation">Optional rotation (defaults to identity).</param>
        /// <returns>The spawned unit's GameObject, or null if spawn failed.</returns>
        public GameObject SpawnUnit(UnitArchetypeSO archetype, Vector3 position, int teamId, Quaternion? rotation = null)
        {
            if (archetype == null)
            {
                Debug.LogWarning("[UnitFactory] Cannot spawn unit: archetype is null");
                return null;
            }

            if (archetype.UnitPrefab == null)
            {
                Debug.LogWarning($"[UnitFactory] Cannot spawn unit '{archetype.Id}': prefab is null");
                return null;
            }

            // Apply height offset if configured
            Vector3 spawnPosition = position;
            if (_applyHeightOffset)
            {
                spawnPosition.y += archetype.HeightOffset;
            }

            // Instantiate the prefab
            Quaternion spawnRotation = rotation ?? Quaternion.identity;
            GameObject unitGO = Instantiate(archetype.UnitPrefab, spawnPosition, spawnRotation);

            // Parent to units container if set
            if (_unitsParent != null)
            {
                unitGO.transform.SetParent(_unitsParent);
            }

            // Get or add UnitController
            UnitController controller = unitGO.GetComponent<UnitController>();
            if (controller == null)
            {
                controller = unitGO.AddComponent<UnitController>();
            }

            // Initialize the controller
            controller.Initialize(archetype, teamId);

            // Subscribe to death event for cleanup
            controller.OnDeath += () => OnUnitDied(controller);

            // Track the unit
            RegisterUnit(controller);

            // Name the GameObject for debugging
            unitGO.name = $"{archetype.DisplayName ?? archetype.Id}_Team{teamId}_{UnitCount}";

            OnUnitSpawned?.Invoke(controller);

            return unitGO;
        }

        /// <summary>
        /// Spawns a unit at a spawn point.
        /// </summary>
        /// <param name="archetype">The archetype to spawn from.</param>
        /// <param name="spawnPoint">The spawn point to use.</param>
        /// <returns>The spawned unit's GameObject, or null if spawn failed.</returns>
        public GameObject SpawnAtPoint(UnitArchetypeSO archetype, SpawnPoint spawnPoint)
        {
            if (spawnPoint == null)
            {
                Debug.LogWarning("[UnitFactory] Cannot spawn unit: spawnPoint is null");
                return null;
            }

            return SpawnUnit(
                archetype,
                spawnPoint.GetSpawnPosition(),
                spawnPoint.TeamId,
                spawnPoint.GetSpawnRotation()
            );
        }

        /// <summary>
        /// Spawns multiple units at a spawn point.
        /// </summary>
        /// <param name="archetype">The archetype to spawn from.</param>
        /// <param name="spawnPoint">The spawn point to use.</param>
        /// <param name="count">Number of units to spawn.</param>
        /// <returns>List of spawned unit GameObjects.</returns>
        public List<GameObject> SpawnMultiple(UnitArchetypeSO archetype, SpawnPoint spawnPoint, int count)
        {
            var spawnedUnits = new List<GameObject>();

            for (int i = 0; i < count; i++)
            {
                var unit = SpawnAtPoint(archetype, spawnPoint);
                if (unit != null)
                {
                    spawnedUnits.Add(unit);
                }
            }

            return spawnedUnits;
        }

        #endregion

        #region Tracking Methods

        /// <summary>
        /// Gets all active units.
        /// </summary>
        /// <returns>Read-only list of all unit controllers.</returns>
        public IReadOnlyList<UnitController> GetAllUnits()
        {
            CleanupDestroyedUnits();
            return _allUnits.AsReadOnly();
        }

        /// <summary>
        /// Gets all units belonging to a team.
        /// </summary>
        /// <param name="teamId">The team ID to filter by.</param>
        /// <returns>Read-only list of unit controllers for the team.</returns>
        public IReadOnlyList<UnitController> GetUnitsByTeam(int teamId)
        {
            CleanupDestroyedUnits();

            if (!_unitsByTeam.TryGetValue(teamId, out var teamUnits))
            {
                return new List<UnitController>().AsReadOnly();
            }

            return teamUnits.AsReadOnly();
        }

        /// <summary>
        /// Gets the count of units for a team.
        /// </summary>
        /// <param name="teamId">The team ID to count.</param>
        /// <returns>Number of active units on the team.</returns>
        public int GetTeamUnitCount(int teamId)
        {
            CleanupDestroyedUnits();

            if (!_unitsByTeam.TryGetValue(teamId, out var teamUnits))
            {
                return 0;
            }

            return teamUnits.Count;
        }

        /// <summary>
        /// Gets all living units (excludes dead units).
        /// </summary>
        /// <returns>List of living unit controllers.</returns>
        public List<UnitController> GetLivingUnits()
        {
            CleanupDestroyedUnits();
            return _allUnits.Where(u => u != null && u.IsAlive).ToList();
        }

        /// <summary>
        /// Gets all living units for a team.
        /// </summary>
        /// <param name="teamId">The team ID to filter by.</param>
        /// <returns>List of living unit controllers for the team.</returns>
        public List<UnitController> GetLivingUnitsByTeam(int teamId)
        {
            CleanupDestroyedUnits();

            if (!_unitsByTeam.TryGetValue(teamId, out var teamUnits))
            {
                return new List<UnitController>();
            }

            return teamUnits.Where(u => u != null && u.IsAlive).ToList();
        }

        #endregion

        #region Cleanup Methods

        /// <summary>
        /// Destroys a specific unit.
        /// </summary>
        /// <param name="unit">The unit controller to destroy.</param>
        public void DestroyUnit(UnitController unit)
        {
            if (unit == null) return;

            UnregisterUnit(unit);
            OnUnitDestroyed?.Invoke(unit);

            if (unit.gameObject != null)
            {
                SafeDestroy(unit.gameObject);
            }
        }

        /// <summary>
        /// Destroys all tracked units.
        /// </summary>
        public void DestroyAllUnits()
        {
            // Create a copy to avoid modification during iteration
            var unitsToDestroy = new List<UnitController>(_allUnits);

            foreach (var unit in unitsToDestroy)
            {
                DestroyUnit(unit);
            }

            _allUnits.Clear();
            foreach (var teamList in _unitsByTeam.Values)
            {
                teamList.Clear();
            }
        }

        /// <summary>
        /// Destroys all units belonging to a team.
        /// </summary>
        /// <param name="teamId">The team ID to destroy.</param>
        public void DestroyTeamUnits(int teamId)
        {
            if (!_unitsByTeam.TryGetValue(teamId, out var teamUnits))
            {
                return;
            }

            var unitsToDestroy = new List<UnitController>(teamUnits);

            foreach (var unit in unitsToDestroy)
            {
                DestroyUnit(unit);
            }
        }

        #endregion

        #region Private Methods

        private void RegisterUnit(UnitController unit)
        {
            if (unit == null || _allUnits.Contains(unit)) return;

            _allUnits.Add(unit);

            int teamId = unit.TeamId;
            if (!_unitsByTeam.ContainsKey(teamId))
            {
                _unitsByTeam[teamId] = new List<UnitController>();
            }
            _unitsByTeam[teamId].Add(unit);
        }

        private void UnregisterUnit(UnitController unit)
        {
            if (unit == null) return;

            _allUnits.Remove(unit);

            int teamId = unit.TeamId;
            if (_unitsByTeam.TryGetValue(teamId, out var teamUnits))
            {
                teamUnits.Remove(unit);
            }
        }

        private void OnUnitDied(UnitController unit)
        {
            // Unit died naturally - remove from tracking but don't destroy immediately
            // (allows for death animations, etc.)
            UnregisterUnit(unit);
            OnUnitDestroyed?.Invoke(unit);
        }

        private void CleanupDestroyedUnits()
        {
            // Remove any null references (destroyed GameObjects)
            _allUnits.RemoveAll(u => u == null);

            foreach (var teamUnits in _unitsByTeam.Values)
            {
                teamUnits.RemoveAll(u => u == null);
            }
        }

        /// <summary>
        /// Safely destroys a GameObject in both Editor and Play mode.
        /// </summary>
        private void SafeDestroy(GameObject gameObjectToDestroy)
        {
            if (gameObjectToDestroy == null) return;

#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                DestroyImmediate(gameObjectToDestroy);
                return;
            }
#endif
            Destroy(gameObjectToDestroy);
        }

        #endregion
    }
}
