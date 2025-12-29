using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

namespace Relic.CoreRTS
{
    /// <summary>
    /// UI controller for spawning and clearing units in the debug scene.
    /// Provides buttons for spawning teams and clearing all units.
    /// </summary>
    /// <remarks>
    /// Part of WP-EXT-6.4: Unit Spawner UI.
    /// Attach to a Canvas in the Flat_Debug scene.
    /// </remarks>
    public class DebugSpawnerUI : MonoBehaviour
    {
        #region Serialized Fields

        [Header("References")]
        [Tooltip("Unit archetype to spawn")]
        [SerializeField] private UnitArchetypeSO _unitArchetype;

        [Tooltip("UnitFactory for spawning (auto-finds if null)")]
        [SerializeField] private UnitFactory _unitFactory;

        [Header("UI Elements")]
        [Tooltip("Button to spawn Team 0 units")]
        [SerializeField] private Button _spawnTeam0Button;

        [Tooltip("Button to spawn Team 1 units")]
        [SerializeField] private Button _spawnTeam1Button;

        [Tooltip("Button to clear all units")]
        [SerializeField] private Button _clearAllButton;

        [Tooltip("Text showing current unit count")]
        [SerializeField] private Text _unitCountText;

        [Header("Spawn Settings")]
        [Tooltip("Number of units to spawn per team")]
        [SerializeField] private int _unitsPerSpawn = 5;

        [Tooltip("Maximum units allowed per team")]
        [SerializeField] private int _maxUnitsPerTeam = 20;

        [Header("Spawn Positions")]
        [Tooltip("Center X position for Team 0 spawn area")]
        [SerializeField] private float _team0SpawnX = -15f;

        [Tooltip("Center X position for Team 1 spawn area")]
        [SerializeField] private float _team1SpawnX = 15f;

        [Tooltip("Spawn area width")]
        [SerializeField] private float _spawnAreaWidth = 10f;

        [Tooltip("Spawn area depth")]
        [SerializeField] private float _spawnAreaDepth = 10f;

        [Tooltip("Minimum spacing between units")]
        [SerializeField] private float _minUnitSpacing = 1.5f;

        #endregion

        #region Runtime State

        private List<UnitController> _spawnedUnits = new List<UnitController>();
        private int _team0Count;
        private int _team1Count;

        #endregion

        #region Properties

        /// <summary>
        /// Number of units to spawn per button press.
        /// </summary>
        public int UnitsPerSpawn
        {
            get => _unitsPerSpawn;
            set => _unitsPerSpawn = Mathf.Clamp(value, 1, 20);
        }

        /// <summary>
        /// Maximum units allowed per team.
        /// </summary>
        public int MaxUnitsPerTeam
        {
            get => _maxUnitsPerTeam;
            set => _maxUnitsPerTeam = Mathf.Max(1, value);
        }

        /// <summary>
        /// Current count of Team 0 units.
        /// </summary>
        public int Team0Count => _team0Count;

        /// <summary>
        /// Current count of Team 1 units.
        /// </summary>
        public int Team1Count => _team1Count;

        /// <summary>
        /// Total spawned unit count.
        /// </summary>
        public int TotalUnitCount => _spawnedUnits.Count;

        #endregion

        #region Unity Lifecycle

        private void Start()
        {
            // Find UnitFactory if not assigned
            if (_unitFactory == null)
            {
                _unitFactory = FindFirstObjectByType<UnitFactory>();
            }

            // Bind button events
            if (_spawnTeam0Button != null)
            {
                _spawnTeam0Button.onClick.AddListener(SpawnTeam0);
            }

            if (_spawnTeam1Button != null)
            {
                _spawnTeam1Button.onClick.AddListener(SpawnTeam1);
            }

            if (_clearAllButton != null)
            {
                _clearAllButton.onClick.AddListener(ClearAll);
            }

            UpdateUI();
        }

        private void OnDestroy()
        {
            // Unbind button events
            if (_spawnTeam0Button != null)
            {
                _spawnTeam0Button.onClick.RemoveListener(SpawnTeam0);
            }

            if (_spawnTeam1Button != null)
            {
                _spawnTeam1Button.onClick.RemoveListener(SpawnTeam1);
            }

            if (_clearAllButton != null)
            {
                _clearAllButton.onClick.RemoveListener(ClearAll);
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Spawns units for Team 0 on the left side.
        /// </summary>
        public void SpawnTeam0()
        {
            SpawnTeam(0, _team0SpawnX);
        }

        /// <summary>
        /// Spawns units for Team 1 on the right side.
        /// </summary>
        public void SpawnTeam1()
        {
            SpawnTeam(1, _team1SpawnX);
        }

        /// <summary>
        /// Clears all spawned units from the scene.
        /// </summary>
        public void ClearAll()
        {
            CleanupDeadUnits();

            foreach (var unit in _spawnedUnits)
            {
                if (unit != null)
                {
                    Destroy(unit.gameObject);
                }
            }

            _spawnedUnits.Clear();
            _team0Count = 0;
            _team1Count = 0;

            UpdateUI();
            Debug.Log("[DebugSpawnerUI] Cleared all units.");
        }

        /// <summary>
        /// Gets the spawn positions for a team.
        /// </summary>
        /// <param name="teamId">The team ID.</param>
        /// <param name="count">Number of positions to generate.</param>
        /// <returns>List of spawn positions.</returns>
        public List<Vector3> GetSpawnPositions(int teamId, int count)
        {
            float centerX = teamId == 0 ? _team0SpawnX : _team1SpawnX;
            return GenerateSpawnPositions(centerX, count);
        }

        #endregion

        #region Private Methods

        private void SpawnTeam(int teamId, float centerX)
        {
            // Check limits
            int currentCount = teamId == 0 ? _team0Count : _team1Count;
            if (currentCount >= _maxUnitsPerTeam)
            {
                Debug.LogWarning($"[DebugSpawnerUI] Team {teamId} at max capacity ({_maxUnitsPerTeam}).");
                return;
            }

            // Calculate how many we can actually spawn
            int toSpawn = Mathf.Min(_unitsPerSpawn, _maxUnitsPerTeam - currentCount);

            // Generate spawn positions
            List<Vector3> positions = GenerateSpawnPositions(centerX, toSpawn);

            // Spawn units
            for (int i = 0; i < positions.Count; i++)
            {
                UnitController unit = SpawnUnit(positions[i], teamId);
                if (unit != null)
                {
                    _spawnedUnits.Add(unit);
                    if (teamId == 0)
                    {
                        _team0Count++;
                    }
                    else
                    {
                        _team1Count++;
                    }
                }
            }

            UpdateUI();
            Debug.Log($"[DebugSpawnerUI] Spawned {positions.Count} units for Team {teamId}.");
        }

        private UnitController SpawnUnit(Vector3 position, int teamId)
        {
            // Try using UnitFactory if available
            if (_unitFactory != null && _unitArchetype != null)
            {
                GameObject go = _unitFactory.SpawnUnit(_unitArchetype, position, teamId);
                return go != null ? go.GetComponent<UnitController>() : null;
            }

            // Fallback: look for prefab and instantiate manually
            if (_unitArchetype != null && _unitArchetype.UnitPrefab != null)
            {
                GameObject go = Instantiate(_unitArchetype.UnitPrefab, position, Quaternion.identity);
                UnitController unit = go.GetComponent<UnitController>();
                if (unit != null)
                {
                    unit.Initialize(_unitArchetype, teamId);

                    // Apply team color
                    TeamColorApplier colorApplier = go.GetComponent<TeamColorApplier>();
                    if (colorApplier != null)
                    {
                        colorApplier.Initialize();
                    }
                }
                return unit;
            }

            Debug.LogWarning("[DebugSpawnerUI] No archetype or prefab configured for spawning.");
            return null;
        }

        private List<Vector3> GenerateSpawnPositions(float centerX, int count)
        {
            List<Vector3> positions = new List<Vector3>();

            // Grid-based spawn positions with some randomization
            int gridSize = Mathf.CeilToInt(Mathf.Sqrt(count));
            float spacing = Mathf.Max(_minUnitSpacing, _spawnAreaWidth / gridSize);

            int index = 0;
            for (int row = 0; row < gridSize && index < count; row++)
            {
                for (int col = 0; col < gridSize && index < count; col++)
                {
                    float offsetX = (col - gridSize / 2f) * spacing;
                    float offsetZ = (row - gridSize / 2f) * spacing;

                    // Add small random offset to prevent perfect alignment
                    float randomX = Random.Range(-0.3f, 0.3f);
                    float randomZ = Random.Range(-0.3f, 0.3f);

                    Vector3 pos = new Vector3(
                        centerX + offsetX + randomX,
                        0f,
                        offsetZ + randomZ
                    );

                    positions.Add(pos);
                    index++;
                }
            }

            return positions;
        }

        private void CleanupDeadUnits()
        {
            _spawnedUnits.RemoveAll(u => u == null || !u.IsAlive);

            // Recount
            _team0Count = 0;
            _team1Count = 0;
            foreach (var unit in _spawnedUnits)
            {
                if (unit.TeamId == 0)
                {
                    _team0Count++;
                }
                else
                {
                    _team1Count++;
                }
            }
        }

        private void UpdateUI()
        {
            CleanupDeadUnits();

            if (_unitCountText != null)
            {
                _unitCountText.text = $"Units: {_spawnedUnits.Count} (Team0: {_team0Count}, Team1: {_team1Count})";
            }

            // Update button states
            if (_spawnTeam0Button != null)
            {
                _spawnTeam0Button.interactable = _team0Count < _maxUnitsPerTeam;
            }

            if (_spawnTeam1Button != null)
            {
                _spawnTeam1Button.interactable = _team1Count < _maxUnitsPerTeam;
            }

            if (_clearAllButton != null)
            {
                _clearAllButton.interactable = _spawnedUnits.Count > 0;
            }
        }

        #endregion
    }
}
