using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Collections.Generic;
using Relic.Core;
using Relic.CoreRTS;
using Relic.Data;
using Relic.ARLayer;

namespace Relic.UILayer
{
    /// <summary>
    /// World-space UI panel for in-game controls.
    /// Provides spawning, era switching, upgrade, and match control functionality.
    /// Anchored to the battlefield and optionally billboards toward player.
    /// </summary>
    /// <remarks>
    /// Attach to a Canvas set to World Space render mode.
    /// See Kyle's milestones.md Milestone 4 for requirements.
    /// </remarks>
    public class WorldSpaceUIPanel : MonoBehaviour
    {
        #region Constants

        private const float DEFAULT_PANEL_DISTANCE = 0.5f;
        private const float DEFAULT_PANEL_HEIGHT = 0.3f;
        private const float MIN_SCALE = 0.0001f;
        private const float MAX_SCALE = 0.01f;

        #endregion

        #region Serialized Fields

        [Header("Anchoring")]
        [Tooltip("Transform to anchor this panel to (typically the battlefield root)")]
        [SerializeField] private Transform _anchorTarget;

        [Tooltip("Offset from anchor position in local space")]
        [SerializeField] private Vector3 _anchorOffset = new Vector3(0f, DEFAULT_PANEL_HEIGHT, -DEFAULT_PANEL_DISTANCE);

        [Tooltip("Should the panel face the camera (billboard mode)")]
        [SerializeField] private bool _billboardToCamera = true;

        [Tooltip("Lock vertical rotation when billboarding (keeps panel upright)")]
        [SerializeField] private bool _lockVerticalRotation = true;

        [Header("Panel Sections")]
        [Tooltip("Root object for spawn controls section")]
        [SerializeField] private GameObject _spawnControlsRoot;

        [Tooltip("Root object for era selector section")]
        [SerializeField] private GameObject _eraSelectorRoot;

        [Tooltip("Root object for upgrade panel section")]
        [SerializeField] private GameObject _upgradePanelRoot;

        [Tooltip("Root object for match controls section")]
        [SerializeField] private GameObject _matchControlsRoot;

        [Header("Spawn Controls")]
        [Tooltip("Dropdown for archetype selection")]
        [SerializeField] private TMP_Dropdown _archetypeDropdown;

        [Tooltip("Button to spawn for team 0 (red)")]
        [SerializeField] private Button _spawnTeam0Button;

        [Tooltip("Button to spawn for team 1 (blue)")]
        [SerializeField] private Button _spawnTeam1Button;

        [Tooltip("Text showing unit counts")]
        [SerializeField] private TextMeshProUGUI _unitCountText;

        [Header("Era Selector")]
        [Tooltip("Container for era buttons")]
        [SerializeField] private Transform _eraButtonContainer;

        [Tooltip("Text showing current era name")]
        [SerializeField] private TextMeshProUGUI _currentEraText;

        [Header("Upgrade Panel")]
        [Tooltip("Container for upgrade buttons")]
        [SerializeField] private Transform _upgradeButtonContainer;

        [Tooltip("Prefab for upgrade buttons")]
        [SerializeField] private GameObject _upgradeButtonPrefab;

        [Tooltip("Text showing selected squad info")]
        [SerializeField] private TextMeshProUGUI _squadInfoText;

        [Header("Match Controls")]
        [Tooltip("Button to reset the match")]
        [SerializeField] private Button _resetMatchButton;

        [Tooltip("Button to pause/resume")]
        [SerializeField] private Button _pauseResumeButton;

        [Tooltip("Text on pause/resume button")]
        [SerializeField] private TextMeshProUGUI _pauseResumeText;

        [Header("References")]
        [Tooltip("Reference to UnitFactory")]
        [SerializeField] private UnitFactory _unitFactory;

        [Tooltip("Available unit archetypes")]
        [SerializeField] private List<UnitArchetypeSO> _archetypes = new List<UnitArchetypeSO>();

        [Tooltip("Available upgrades")]
        [SerializeField] private List<UpgradeSO> _upgrades = new List<UpgradeSO>();

        [Tooltip("Red team spawn point")]
        [SerializeField] private SpawnPoint _team0SpawnPoint;

        [Tooltip("Blue team spawn point")]
        [SerializeField] private SpawnPoint _team1SpawnPoint;

        [Header("Visibility")]
        [Tooltip("Show spawn controls section")]
        [SerializeField] private bool _showSpawnControls = true;

        [Tooltip("Show era selector section")]
        [SerializeField] private bool _showEraSelector = true;

        [Tooltip("Show upgrade panel section")]
        [SerializeField] private bool _showUpgradePanel = true;

        [Tooltip("Show match controls section")]
        [SerializeField] private bool _showMatchControls = true;

        #endregion

        #region Runtime State

        private int _selectedArchetypeIndex;
        private bool _isPaused;
        private Camera _mainCamera;
        private Canvas _canvas;
        private EraManager _eraManager;
        private SelectionManager _selectionManager;
        private List<Button> _eraButtons = new List<Button>();
        private List<Button> _upgradeButtons = new List<Button>();
        private Squad _selectedSquad;
        private bool _isInitialized;

        #endregion

        #region Events

        /// <summary>
        /// Fired when a unit is spawned via the UI.
        /// </summary>
        public event Action<UnitController, int> OnUnitSpawned;

        /// <summary>
        /// Fired when the era is changed via the UI.
        /// </summary>
        public event Action<EraConfigSO> OnEraChanged;

        /// <summary>
        /// Fired when an upgrade is applied via the UI.
        /// </summary>
        public event Action<UpgradeSO, Squad> OnUpgradeApplied;

        /// <summary>
        /// Fired when the match is reset.
        /// </summary>
        public event Action OnMatchReset;

        /// <summary>
        /// Fired when pause state changes.
        /// </summary>
        public event Action<bool> OnPauseStateChanged;

        #endregion

        #region Properties

        /// <summary>
        /// The currently selected archetype.
        /// </summary>
        public UnitArchetypeSO SelectedArchetype =>
            _archetypes.Count > _selectedArchetypeIndex ? _archetypes[_selectedArchetypeIndex] : null;

        /// <summary>
        /// Whether the game is paused.
        /// </summary>
        public bool IsPaused => _isPaused;

        /// <summary>
        /// The transform this panel is anchored to.
        /// </summary>
        public Transform AnchorTarget
        {
            get => _anchorTarget;
            set
            {
                _anchorTarget = value;
                UpdatePosition();
            }
        }

        /// <summary>
        /// Offset from anchor in local space.
        /// </summary>
        public Vector3 AnchorOffset
        {
            get => _anchorOffset;
            set
            {
                _anchorOffset = value;
                UpdatePosition();
            }
        }

        /// <summary>
        /// Whether billboard mode is enabled.
        /// </summary>
        public bool BillboardToCamera
        {
            get => _billboardToCamera;
            set => _billboardToCamera = value;
        }

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            _canvas = GetComponent<Canvas>();
            _mainCamera = Camera.main;
        }

        private void Start()
        {
            Initialize();
        }

        private void OnEnable()
        {
            SubscribeToEvents();
        }

        private void OnDisable()
        {
            UnsubscribeFromEvents();
        }

        private void LateUpdate()
        {
            if (_anchorTarget != null)
            {
                UpdatePosition();
            }

            if (_billboardToCamera && _mainCamera != null)
            {
                UpdateBillboard();
            }
        }

        #endregion

        #region Initialization

        /// <summary>
        /// Initializes the UI panel and all sections.
        /// </summary>
        public void Initialize()
        {
            if (_isInitialized) return;

            CacheReferences();
            SetupSpawnControls();
            SetupEraSelector();
            SetupUpgradePanel();
            SetupMatchControls();
            UpdateSectionVisibility();
            UpdateDisplays();

            _isInitialized = true;
        }

        private void CacheReferences()
        {
            if (_mainCamera == null)
                _mainCamera = Camera.main;

            _eraManager = EraManager.Instance;
            _selectionManager = SelectionManager.Instance;

            // Try to find UnitFactory if not set
            if (_unitFactory == null)
                _unitFactory = FindFirstObjectByType<UnitFactory>();

            // Try to find BattlefieldPlacer for anchor
            if (_anchorTarget == null)
            {
                var placer = FindFirstObjectByType<BattlefieldPlacer>();
                if (placer != null && placer.PlacedBattlefield != null)
                {
                    _anchorTarget = placer.PlacedBattlefield.transform;
                }
            }
        }

        private void SubscribeToEvents()
        {
            if (_eraManager != null)
            {
                _eraManager.OnEraChanged += HandleEraChanged;
            }

            if (_selectionManager != null)
            {
                _selectionManager.OnSelectionChanged += HandleSelectionChanged;
            }
        }

        private void UnsubscribeFromEvents()
        {
            if (_eraManager != null)
            {
                _eraManager.OnEraChanged -= HandleEraChanged;
            }

            if (_selectionManager != null)
            {
                _selectionManager.OnSelectionChanged -= HandleSelectionChanged;
            }
        }

        #endregion

        #region Spawn Controls

        private void SetupSpawnControls()
        {
            // Setup archetype dropdown
            if (_archetypeDropdown != null)
            {
                _archetypeDropdown.ClearOptions();

                var options = new List<TMP_Dropdown.OptionData>();
                foreach (var archetype in _archetypes)
                {
                    string displayName = archetype != null ? (archetype.DisplayName ?? archetype.Id) : "None";
                    options.Add(new TMP_Dropdown.OptionData(displayName));
                }

                _archetypeDropdown.AddOptions(options);
                _archetypeDropdown.onValueChanged.AddListener(OnArchetypeSelected);
            }

            // Setup spawn buttons
            if (_spawnTeam0Button != null)
            {
                _spawnTeam0Button.onClick.AddListener(() => SpawnUnit(0));
            }

            if (_spawnTeam1Button != null)
            {
                _spawnTeam1Button.onClick.AddListener(() => SpawnUnit(1));
            }
        }

        private void OnArchetypeSelected(int index)
        {
            _selectedArchetypeIndex = index;
        }

        /// <summary>
        /// Spawns a unit for the specified team.
        /// </summary>
        /// <param name="teamId">Team ID (0 or 1).</param>
        public void SpawnUnit(int teamId)
        {
            if (_unitFactory == null)
            {
                Debug.LogWarning("[WorldSpaceUIPanel] UnitFactory not set");
                return;
            }

            var archetype = SelectedArchetype;
            if (archetype == null)
            {
                Debug.LogWarning("[WorldSpaceUIPanel] No archetype selected");
                return;
            }

            SpawnPoint spawnPoint = teamId == 0 ? _team0SpawnPoint : _team1SpawnPoint;
            if (spawnPoint == null)
            {
                Debug.LogWarning($"[WorldSpaceUIPanel] SpawnPoint for team {teamId} not set");
                return;
            }

            var unitGO = _unitFactory.SpawnAtPoint(archetype, spawnPoint);
            if (unitGO != null)
            {
                var controller = unitGO.GetComponent<UnitController>();
                OnUnitSpawned?.Invoke(controller, teamId);
                UpdateUnitCountDisplay();
                Debug.Log($"[WorldSpaceUIPanel] Spawned {archetype.DisplayName} for team {teamId}");
            }
        }

        private void UpdateUnitCountDisplay()
        {
            if (_unitCountText == null || _unitFactory == null) return;

            int team0Count = _unitFactory.GetTeamUnitCount(0);
            int team1Count = _unitFactory.GetTeamUnitCount(1);
            _unitCountText.text = $"Team 0: {team0Count} | Team 1: {team1Count} | Total: {_unitFactory.UnitCount}";
        }

        #endregion

        #region Era Selector

        private void SetupEraSelector()
        {
            if (_eraManager == null || _eraButtonContainer == null) return;

            // Clear existing buttons
            foreach (var button in _eraButtons)
            {
                if (button != null)
                    Destroy(button.gameObject);
            }
            _eraButtons.Clear();

            // Create buttons for each era
            foreach (var era in _eraManager.AvailableEras)
            {
                if (era == null) continue;

                var buttonGO = new GameObject($"Era_{era.DisplayName}");
                buttonGO.transform.SetParent(_eraButtonContainer, false);

                var button = buttonGO.AddComponent<Button>();
                var image = buttonGO.AddComponent<Image>();
                image.color = era.PrimaryColor;

                // Add text
                var textGO = new GameObject("Text");
                textGO.transform.SetParent(buttonGO.transform, false);
                var text = textGO.AddComponent<TextMeshProUGUI>();
                text.text = era.DisplayName;
                text.alignment = TextAlignmentOptions.Center;
                text.fontSize = 12;
                text.color = Color.white;

                // Setup RectTransform
                var buttonRect = buttonGO.GetComponent<RectTransform>();
                buttonRect.sizeDelta = new Vector2(80, 30);

                var textRect = textGO.GetComponent<RectTransform>();
                textRect.anchorMin = Vector2.zero;
                textRect.anchorMax = Vector2.one;
                textRect.sizeDelta = Vector2.zero;
                textRect.anchoredPosition = Vector2.zero;

                // Setup click handler
                var capturedEra = era;
                button.onClick.AddListener(() => SelectEra(capturedEra));

                _eraButtons.Add(button);
            }

            UpdateCurrentEraDisplay();
        }

        /// <summary>
        /// Selects and applies an era.
        /// </summary>
        /// <param name="era">The era to select.</param>
        public void SelectEra(EraConfigSO era)
        {
            if (_eraManager == null || era == null) return;

            _eraManager.SetEra(era);
            OnEraChanged?.Invoke(era);
        }

        private void HandleEraChanged(EraConfigSO oldEra, EraConfigSO newEra)
        {
            UpdateCurrentEraDisplay();
            UpdateUpgradeButtonsForEra();
        }

        private void UpdateCurrentEraDisplay()
        {
            if (_currentEraText == null || _eraManager == null) return;

            var currentEra = _eraManager.CurrentEra;
            _currentEraText.text = currentEra != null ? currentEra.DisplayName : "No Era";

            // Update button highlight
            for (int btnIndex = 0; btnIndex < _eraButtons.Count && btnIndex < _eraManager.AvailableEras.Count; btnIndex++)
            {
                var button = _eraButtons[btnIndex];
                var era = _eraManager.AvailableEras[btnIndex];
                if (button == null) continue;

                var image = button.GetComponent<Image>();
                if (image != null)
                {
                    bool isSelected = era == currentEra;
                    image.color = isSelected ? Color.yellow : (era?.PrimaryColor ?? Color.gray);
                }
            }
        }

        #endregion

        #region Upgrade Panel

        private void SetupUpgradePanel()
        {
            CreateUpgradeButtons();
            UpdateSquadInfo();
        }

        private void CreateUpgradeButtons()
        {
            if (_upgradeButtonContainer == null) return;

            // Clear existing buttons
            foreach (var button in _upgradeButtons)
            {
                if (button != null)
                    Destroy(button.gameObject);
            }
            _upgradeButtons.Clear();

            // Create buttons for each upgrade
            foreach (var upgrade in _upgrades)
            {
                if (upgrade == null) continue;

                GameObject buttonGO;

                if (_upgradeButtonPrefab != null)
                {
                    buttonGO = Instantiate(_upgradeButtonPrefab, _upgradeButtonContainer);
                }
                else
                {
                    buttonGO = new GameObject($"Upgrade_{upgrade.DisplayName}");
                    buttonGO.transform.SetParent(_upgradeButtonContainer, false);

                    var button = buttonGO.AddComponent<Button>();
                    var image = buttonGO.AddComponent<Image>();
                    image.color = Color.gray;

                    // Add text
                    var textGO = new GameObject("Text");
                    textGO.transform.SetParent(buttonGO.transform, false);
                    var text = textGO.AddComponent<TextMeshProUGUI>();
                    text.text = upgrade.DisplayName;
                    text.alignment = TextAlignmentOptions.Center;
                    text.fontSize = 10;
                    text.color = Color.white;

                    // Setup RectTransform
                    var buttonRect = buttonGO.GetComponent<RectTransform>();
                    buttonRect.sizeDelta = new Vector2(100, 25);

                    var textRect = textGO.GetComponent<RectTransform>();
                    textRect.anchorMin = Vector2.zero;
                    textRect.anchorMax = Vector2.one;
                    textRect.sizeDelta = Vector2.zero;
                    textRect.anchoredPosition = Vector2.zero;
                }

                var btn = buttonGO.GetComponent<Button>();
                if (btn != null)
                {
                    var capturedUpgrade = upgrade;
                    btn.onClick.AddListener(() => ApplyUpgrade(capturedUpgrade));
                    _upgradeButtons.Add(btn);
                }
            }

            UpdateUpgradeButtonsForEra();
        }

        /// <summary>
        /// Applies an upgrade to the currently selected squad.
        /// </summary>
        /// <param name="upgrade">The upgrade to apply.</param>
        public void ApplyUpgrade(UpgradeSO upgrade)
        {
            if (upgrade == null || _selectedSquad == null)
            {
                Debug.LogWarning("[WorldSpaceUIPanel] No upgrade or squad selected");
                return;
            }

            // Check era compatibility
            if (_eraManager != null && _eraManager.CurrentEra != null)
            {
                // Get current era type from config
                // Note: EraConfigSO doesn't have EraType directly, so we check by ID or use All
                if (upgrade.Era != EraType.All)
                {
                    // For now, allow all upgrades - era filtering would need EraType on EraConfigSO
                    Debug.Log($"[WorldSpaceUIPanel] Upgrade {upgrade.DisplayName} era: {upgrade.Era}");
                }
            }

            if (_selectedSquad.ApplyUpgrade(upgrade))
            {
                OnUpgradeApplied?.Invoke(upgrade, _selectedSquad);
                UpdateSquadInfo();
                Debug.Log($"[WorldSpaceUIPanel] Applied {upgrade.DisplayName} to squad {_selectedSquad.Id}");
            }
            else
            {
                Debug.LogWarning($"[WorldSpaceUIPanel] Failed to apply {upgrade.DisplayName}");
            }
        }

        private void HandleSelectionChanged(IReadOnlyList<UnitController> selectedUnits)
        {
            // Find a squad from selected units (use first unit's squad if any)
            _selectedSquad = null;

            if (selectedUnits != null && selectedUnits.Count > 0)
            {
                var firstUnit = selectedUnits[0];
                if (firstUnit != null)
                {
                    _selectedSquad = firstUnit.Squad;
                }
            }

            UpdateSquadInfo();
            UpdateUpgradeButtonInteractability();
        }

        private void UpdateSquadInfo()
        {
            if (_squadInfoText == null) return;

            if (_selectedSquad == null)
            {
                _squadInfoText.text = "No squad selected";
            }
            else
            {
                int memberCount = _selectedSquad.MemberCount;
                int upgradeCount = _selectedSquad.UpgradeCount;
                float healthPercent = _selectedSquad.GetHealthPercent() * 100f;

                _squadInfoText.text = $"Squad: {_selectedSquad.Id}\n" +
                                      $"Units: {memberCount}\n" +
                                      $"Upgrades: {upgradeCount}\n" +
                                      $"Health: {healthPercent:F0}%";
            }
        }

        private void UpdateUpgradeButtonsForEra()
        {
            if (_eraManager == null) return;

            // For now, enable all upgrade buttons - era filtering would need more infrastructure
            foreach (var button in _upgradeButtons)
            {
                if (button != null)
                {
                    button.interactable = _selectedSquad != null;
                }
            }
        }

        private void UpdateUpgradeButtonInteractability()
        {
            foreach (var button in _upgradeButtons)
            {
                if (button != null)
                {
                    button.interactable = _selectedSquad != null;
                }
            }
        }

        #endregion

        #region Match Controls

        private void SetupMatchControls()
        {
            if (_resetMatchButton != null)
            {
                _resetMatchButton.onClick.AddListener(ResetMatch);
            }

            if (_pauseResumeButton != null)
            {
                _pauseResumeButton.onClick.AddListener(TogglePause);
            }

            UpdatePauseButtonText();
        }

        /// <summary>
        /// Resets the match - clears all units.
        /// </summary>
        public void ResetMatch()
        {
            if (_unitFactory != null)
            {
                _unitFactory.DestroyAllUnits();
            }

            // Resume if paused
            if (_isPaused)
            {
                SetPaused(false);
            }

            OnMatchReset?.Invoke();
            UpdateUnitCountDisplay();
            Debug.Log("[WorldSpaceUIPanel] Match reset");
        }

        /// <summary>
        /// Toggles the pause state.
        /// </summary>
        public void TogglePause()
        {
            SetPaused(!_isPaused);
        }

        /// <summary>
        /// Sets the pause state.
        /// </summary>
        /// <param name="paused">Whether to pause.</param>
        public void SetPaused(bool paused)
        {
            _isPaused = paused;
            Time.timeScale = _isPaused ? 0f : 1f;

            UpdatePauseButtonText();
            OnPauseStateChanged?.Invoke(_isPaused);

            Debug.Log($"[WorldSpaceUIPanel] Game {(_isPaused ? "paused" : "resumed")}");
        }

        private void UpdatePauseButtonText()
        {
            if (_pauseResumeText != null)
            {
                _pauseResumeText.text = _isPaused ? "Resume" : "Pause";
            }
        }

        #endregion

        #region Positioning and Billboard

        private void UpdatePosition()
        {
            if (_anchorTarget == null) return;

            // Apply offset in anchor's local space
            transform.position = _anchorTarget.TransformPoint(_anchorOffset);
        }

        private void UpdateBillboard()
        {
            if (_mainCamera == null) return;

            Vector3 directionToCamera = _mainCamera.transform.position - transform.position;

            if (_lockVerticalRotation)
            {
                directionToCamera.y = 0;
            }

            if (directionToCamera.sqrMagnitude > 0.001f)
            {
                transform.rotation = Quaternion.LookRotation(-directionToCamera);
            }
        }

        #endregion

        #region Section Visibility

        private void UpdateSectionVisibility()
        {
            if (_spawnControlsRoot != null)
                _spawnControlsRoot.SetActive(_showSpawnControls);

            if (_eraSelectorRoot != null)
                _eraSelectorRoot.SetActive(_showEraSelector);

            if (_upgradePanelRoot != null)
                _upgradePanelRoot.SetActive(_showUpgradePanel);

            if (_matchControlsRoot != null)
                _matchControlsRoot.SetActive(_showMatchControls);
        }

        /// <summary>
        /// Shows or hides a UI section.
        /// </summary>
        /// <param name="section">Section name: "spawn", "era", "upgrade", "match"</param>
        /// <param name="visible">Whether to show the section.</param>
        public void SetSectionVisible(string section, bool visible)
        {
            switch (section.ToLower())
            {
                case "spawn":
                    _showSpawnControls = visible;
                    if (_spawnControlsRoot != null) _spawnControlsRoot.SetActive(visible);
                    break;
                case "era":
                    _showEraSelector = visible;
                    if (_eraSelectorRoot != null) _eraSelectorRoot.SetActive(visible);
                    break;
                case "upgrade":
                    _showUpgradePanel = visible;
                    if (_upgradePanelRoot != null) _upgradePanelRoot.SetActive(visible);
                    break;
                case "match":
                    _showMatchControls = visible;
                    if (_matchControlsRoot != null) _matchControlsRoot.SetActive(visible);
                    break;
            }
        }

        #endregion

        #region Display Updates

        private void UpdateDisplays()
        {
            UpdateUnitCountDisplay();
            UpdateCurrentEraDisplay();
            UpdateSquadInfo();
            UpdatePauseButtonText();
        }

        /// <summary>
        /// Refreshes all UI displays.
        /// </summary>
        public void Refresh()
        {
            SetupSpawnControls();
            SetupEraSelector();
            CreateUpgradeButtons();
            UpdateDisplays();
        }

        #endregion

        #region Public Configuration

        /// <summary>
        /// Sets the available archetypes for spawning.
        /// </summary>
        /// <param name="archetypes">List of unit archetypes.</param>
        public void SetArchetypes(List<UnitArchetypeSO> archetypes)
        {
            _archetypes = archetypes ?? new List<UnitArchetypeSO>();
            SetupSpawnControls();
        }

        /// <summary>
        /// Sets the available upgrades.
        /// </summary>
        /// <param name="upgrades">List of upgrades.</param>
        public void SetUpgrades(List<UpgradeSO> upgrades)
        {
            _upgrades = upgrades ?? new List<UpgradeSO>();
            CreateUpgradeButtons();
        }

        /// <summary>
        /// Sets the spawn points.
        /// </summary>
        /// <param name="team0Spawn">Team 0 spawn point.</param>
        /// <param name="team1Spawn">Team 1 spawn point.</param>
        public void SetSpawnPoints(SpawnPoint team0Spawn, SpawnPoint team1Spawn)
        {
            _team0SpawnPoint = team0Spawn;
            _team1SpawnPoint = team1Spawn;
        }

        /// <summary>
        /// Sets the unit factory reference.
        /// </summary>
        /// <param name="factory">The unit factory.</param>
        public void SetUnitFactory(UnitFactory factory)
        {
            _unitFactory = factory;
        }

        /// <summary>
        /// Shows the panel.
        /// </summary>
        public void Show()
        {
            gameObject.SetActive(true);
            UpdateDisplays();
        }

        /// <summary>
        /// Hides the panel.
        /// </summary>
        public void Hide()
        {
            gameObject.SetActive(false);
        }

        #endregion

        #region Editor Support

#if UNITY_EDITOR
        /// <summary>
        /// Sets test references for unit testing.
        /// </summary>
        public void SetTestReferences(
            UnitFactory factory,
            List<UnitArchetypeSO> archetypes,
            List<UpgradeSO> upgrades,
            SpawnPoint team0Spawn,
            SpawnPoint team1Spawn)
        {
            _unitFactory = factory;
            _archetypes = archetypes ?? new List<UnitArchetypeSO>();
            _upgrades = upgrades ?? new List<UpgradeSO>();
            _team0SpawnPoint = team0Spawn;
            _team1SpawnPoint = team1Spawn;
        }

        /// <summary>
        /// Sets a squad for testing upgrade functionality.
        /// </summary>
        public void SetTestSquad(Squad squad)
        {
            _selectedSquad = squad;
            UpdateSquadInfo();
            UpdateUpgradeButtonInteractability();
        }
#endif

        #endregion
    }
}
