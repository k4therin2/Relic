using UnityEngine;
using UnityEngine.UI;
using Relic.CoreRTS;
using System.Collections.Generic;

namespace Relic.UILayer
{
    /// <summary>
    /// Simple UI for testing unit spawning.
    /// Allows selecting archetypes and spawning units at spawn points.
    /// </summary>
    /// <remarks>
    /// Attach to a Canvas in the scene. Requires UnitFactory and SpawnPoints to be set up.
    /// See Kyle's milestones.md Milestone 2 for requirements.
    /// </remarks>
    public class SpawnTestingUI : MonoBehaviour
    {
        #region Serialized Fields

        [Header("References")]
        [Tooltip("UnitFactory to use for spawning")]
        [SerializeField] private UnitFactory _unitFactory;

        [Tooltip("Available archetypes to spawn")]
        [SerializeField] private List<UnitArchetypeSO> _archetypes = new List<UnitArchetypeSO>();

        [Tooltip("Red team spawn point")]
        [SerializeField] private SpawnPoint _redSpawnPoint;

        [Tooltip("Blue team spawn point")]
        [SerializeField] private SpawnPoint _blueSpawnPoint;

        [Header("UI Elements")]
        [Tooltip("Dropdown for archetype selection")]
        [SerializeField] private Dropdown _archetypeDropdown;

        [Tooltip("Button to spawn red team unit")]
        [SerializeField] private Button _spawnRedButton;

        [Tooltip("Button to spawn blue team unit")]
        [SerializeField] private Button _spawnBlueButton;

        [Tooltip("Button to clear all units")]
        [SerializeField] private Button _clearAllButton;

        [Tooltip("Text to display unit counts")]
        [SerializeField] private Text _unitCountText;

        #endregion

        #region Runtime State

        private int _selectedArchetypeIndex = 0;

        #endregion

        #region Properties

        /// <summary>
        /// Currently selected archetype.
        /// </summary>
        public UnitArchetypeSO SelectedArchetype =>
            _archetypes.Count > _selectedArchetypeIndex ? _archetypes[_selectedArchetypeIndex] : null;

        #endregion

        #region Unity Lifecycle

        private void Start()
        {
            SetupUI();
            UpdateUnitCount();
        }

        private void Update()
        {
            // Update unit count display
            UpdateUnitCount();
        }

        #endregion

        #region UI Setup

        private void SetupUI()
        {
            // Populate archetype dropdown
            if (_archetypeDropdown != null)
            {
                _archetypeDropdown.ClearOptions();

                var options = new List<Dropdown.OptionData>();
                foreach (var archetype in _archetypes)
                {
                    string name = archetype != null ? (archetype.DisplayName ?? archetype.Id) : "None";
                    options.Add(new Dropdown.OptionData(name));
                }

                _archetypeDropdown.AddOptions(options);
                _archetypeDropdown.onValueChanged.AddListener(OnArchetypeSelected);
            }

            // Setup button listeners
            if (_spawnRedButton != null)
            {
                _spawnRedButton.onClick.AddListener(SpawnRedUnit);
            }

            if (_spawnBlueButton != null)
            {
                _spawnBlueButton.onClick.AddListener(SpawnBlueUnit);
            }

            if (_clearAllButton != null)
            {
                _clearAllButton.onClick.AddListener(ClearAllUnits);
            }
        }

        #endregion

        #region UI Callbacks

        private void OnArchetypeSelected(int index)
        {
            _selectedArchetypeIndex = index;
        }

        #endregion

        #region Spawning Methods

        /// <summary>
        /// Spawns a unit for the red team.
        /// </summary>
        public void SpawnRedUnit()
        {
            SpawnUnit(_redSpawnPoint);
        }

        /// <summary>
        /// Spawns a unit for the blue team.
        /// </summary>
        public void SpawnBlueUnit()
        {
            SpawnUnit(_blueSpawnPoint);
        }

        /// <summary>
        /// Spawns a unit at a spawn point.
        /// </summary>
        private void SpawnUnit(SpawnPoint spawnPoint)
        {
            if (_unitFactory == null)
            {
                Debug.LogWarning("[SpawnTestingUI] UnitFactory not set");
                return;
            }

            if (spawnPoint == null)
            {
                Debug.LogWarning("[SpawnTestingUI] SpawnPoint not set");
                return;
            }

            var archetype = SelectedArchetype;
            if (archetype == null)
            {
                Debug.LogWarning("[SpawnTestingUI] No archetype selected");
                return;
            }

            var unit = _unitFactory.SpawnAtPoint(archetype, spawnPoint);
            if (unit != null)
            {
                Debug.Log($"[SpawnTestingUI] Spawned {archetype.DisplayName} for team {spawnPoint.TeamId}");
            }
        }

        /// <summary>
        /// Clears all spawned units.
        /// </summary>
        public void ClearAllUnits()
        {
            if (_unitFactory != null)
            {
                _unitFactory.DestroyAllUnits();
                Debug.Log("[SpawnTestingUI] Cleared all units");
            }
        }

        #endregion

        #region Display Methods

        private void UpdateUnitCount()
        {
            if (_unitCountText == null || _unitFactory == null) return;

            int redCount = _unitFactory.GetTeamUnitCount(SpawnPoint.TEAM_RED);
            int blueCount = _unitFactory.GetTeamUnitCount(SpawnPoint.TEAM_BLUE);

            _unitCountText.text = $"Red: {redCount} | Blue: {blueCount} | Total: {_unitFactory.UnitCount}";
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Sets up references programmatically.
        /// </summary>
        public void Initialize(UnitFactory factory, SpawnPoint redSpawn, SpawnPoint blueSpawn, List<UnitArchetypeSO> archetypes)
        {
            _unitFactory = factory;
            _redSpawnPoint = redSpawn;
            _blueSpawnPoint = blueSpawn;
            _archetypes = archetypes ?? new List<UnitArchetypeSO>();
            SetupUI();
        }

        /// <summary>
        /// Adds an archetype to the available list.
        /// </summary>
        public void AddArchetype(UnitArchetypeSO archetype)
        {
            if (archetype != null && !_archetypes.Contains(archetype))
            {
                _archetypes.Add(archetype);
                SetupUI();
            }
        }

        #endregion
    }
}
