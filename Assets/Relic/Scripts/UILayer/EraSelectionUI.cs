using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using Relic.Core;
using Relic.Data;

namespace Relic.UILayer
{
    /// <summary>
    /// UI component for selecting the game era.
    /// Provides buttons to cycle through available eras and displays era information.
    /// </summary>
    /// <remarks>
    /// This is a basic implementation for Milestone 1.
    /// Enhanced UI with full era details will be added in Milestone 4.
    /// </remarks>
    public class EraSelectionUI : MonoBehaviour
    {
        [Header("UI References")]
        [Tooltip("Text displaying the current era name")]
        [SerializeField] private TextMeshProUGUI _eraNameText;

        [Tooltip("Text displaying the era description")]
        [SerializeField] private TextMeshProUGUI _eraDescriptionText;

        [Tooltip("Image showing era-themed colors")]
        [SerializeField] private Image _eraColorPanel;

        [Tooltip("Button to select previous era")]
        [SerializeField] private Button _prevButton;

        [Tooltip("Button to select next era")]
        [SerializeField] private Button _nextButton;

        [Tooltip("Button to confirm era selection")]
        [SerializeField] private Button _selectButton;

        [Header("Optional References")]
        [Tooltip("Parent container for era selection buttons")]
        [SerializeField] private Transform _eraButtonContainer;

        [Tooltip("Prefab for individual era selection buttons")]
        [SerializeField] private GameObject _eraButtonPrefab;

        // Private state
        private List<Button> _eraButtons = new();
        private EraManager _eraManager;
        private bool _isInitialized;

        // Events
        /// <summary>
        /// Fired when user confirms era selection.
        /// </summary>
        public event System.Action<EraConfigSO> OnEraConfirmed;

        #region Unity Lifecycle

        private void Awake()
        {
            // Cache EraManager reference
            _eraManager = EraManager.Instance;
        }

        private void Start()
        {
            Initialize();
        }

        private void OnEnable()
        {
            // Subscribe to EraManager events
            if (_eraManager != null)
            {
                _eraManager.OnEraChanged += HandleEraChanged;
            }
        }

        private void OnDisable()
        {
            // Unsubscribe from EraManager events
            if (_eraManager != null)
            {
                _eraManager.OnEraChanged -= HandleEraChanged;
            }
        }

        #endregion

        #region Initialization

        /// <summary>
        /// Initializes the era selection UI.
        /// </summary>
        private void Initialize()
        {
            if (_isInitialized)
                return;

            SetupButtons();
            CreateEraButtons();
            UpdateDisplay();

            _isInitialized = true;
        }

        /// <summary>
        /// Sets up navigation button listeners.
        /// </summary>
        private void SetupButtons()
        {
            if (_prevButton != null)
            {
                _prevButton.onClick.AddListener(OnPrevClicked);
            }

            if (_nextButton != null)
            {
                _nextButton.onClick.AddListener(OnNextClicked);
            }

            if (_selectButton != null)
            {
                _selectButton.onClick.AddListener(OnSelectClicked);
            }
        }

        /// <summary>
        /// Creates individual era selection buttons if container and prefab are provided.
        /// </summary>
        private void CreateEraButtons()
        {
            if (_eraButtonContainer == null || _eraButtonPrefab == null)
                return;

            if (_eraManager == null || _eraManager.AvailableEras == null)
                return;

            // Clear existing buttons
            foreach (var button in _eraButtons)
            {
                if (button != null)
                    Destroy(button.gameObject);
            }
            _eraButtons.Clear();

            // Create a button for each era
            foreach (var era in _eraManager.AvailableEras)
            {
                if (era == null)
                    continue;

                var buttonObj = Instantiate(_eraButtonPrefab, _eraButtonContainer);
                var button = buttonObj.GetComponent<Button>();

                if (button != null)
                {
                    var capturedEra = era; // Capture for closure
                    button.onClick.AddListener(() => SelectEra(capturedEra));
                    _eraButtons.Add(button);

                    // Set button text if available
                    var buttonText = buttonObj.GetComponentInChildren<TextMeshProUGUI>();
                    if (buttonText != null)
                    {
                        buttonText.text = era.DisplayName;
                    }
                }
            }
        }

        #endregion

        #region Button Handlers

        /// <summary>
        /// Handles click on previous era button.
        /// </summary>
        private void OnPrevClicked()
        {
            if (_eraManager == null)
                return;

            _eraManager.CyclePrevious();
        }

        /// <summary>
        /// Handles click on next era button.
        /// </summary>
        private void OnNextClicked()
        {
            if (_eraManager == null)
                return;

            _eraManager.CycleNext();
        }

        /// <summary>
        /// Handles click on select/confirm button.
        /// </summary>
        private void OnSelectClicked()
        {
            if (_eraManager == null || _eraManager.CurrentEra == null)
                return;

            Debug.Log($"[EraSelectionUI] Era confirmed: {_eraManager.CurrentEra.DisplayName}");
            OnEraConfirmed?.Invoke(_eraManager.CurrentEra);
        }

        /// <summary>
        /// Directly selects a specific era.
        /// </summary>
        /// <param name="era">The era to select.</param>
        private void SelectEra(EraConfigSO era)
        {
            if (_eraManager == null || era == null)
                return;

            _eraManager.SetEra(era);
        }

        #endregion

        #region Event Handlers

        /// <summary>
        /// Handles era change events from EraManager.
        /// </summary>
        private void HandleEraChanged(EraConfigSO oldEra, EraConfigSO newEra)
        {
            UpdateDisplay();
        }

        #endregion

        #region Display Updates

        /// <summary>
        /// Updates the UI to reflect the current era.
        /// </summary>
        private void UpdateDisplay()
        {
            if (_eraManager == null)
                return;

            var currentEra = _eraManager.CurrentEra;

            // Update era name
            if (_eraNameText != null)
            {
                _eraNameText.text = currentEra?.DisplayName ?? "No Era Selected";
            }

            // Update era description
            if (_eraDescriptionText != null)
            {
                _eraDescriptionText.text = currentEra?.Description ?? "";
            }

            // Update color panel
            if (_eraColorPanel != null && currentEra != null)
            {
                _eraColorPanel.color = currentEra.PrimaryColor;
            }

            // Update button states
            UpdateButtonStates();
        }

        /// <summary>
        /// Updates the visual states of era buttons.
        /// </summary>
        private void UpdateButtonStates()
        {
            if (_eraManager == null)
                return;

            var currentEra = _eraManager.CurrentEra;

            // Highlight the currently selected era's button
            for (int buttonIndex = 0; buttonIndex < _eraButtons.Count; buttonIndex++)
            {
                var button = _eraButtons[buttonIndex];
                if (button == null)
                    continue;

                bool isSelected = buttonIndex < _eraManager.AvailableEras.Count &&
                                  _eraManager.AvailableEras[buttonIndex] == currentEra;

                // Apply visual feedback for selection state
                var colors = button.colors;
                colors.normalColor = isSelected ? Color.yellow : Color.white;
                button.colors = colors;
            }

            // Update navigation buttons
            bool hasMultipleEras = _eraManager.EraCount > 1;
            if (_prevButton != null)
                _prevButton.interactable = hasMultipleEras;
            if (_nextButton != null)
                _nextButton.interactable = hasMultipleEras;
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Refreshes the era list (useful if eras are added at runtime).
        /// </summary>
        public void Refresh()
        {
            CreateEraButtons();
            UpdateDisplay();
        }

        /// <summary>
        /// Shows the era selection UI.
        /// </summary>
        public void Show()
        {
            gameObject.SetActive(true);
            UpdateDisplay();
        }

        /// <summary>
        /// Hides the era selection UI.
        /// </summary>
        public void Hide()
        {
            gameObject.SetActive(false);
        }

        #endregion
    }
}
