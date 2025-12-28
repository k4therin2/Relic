using UnityEngine;
using System;
using System.Collections.Generic;
using Relic.Data;

namespace Relic.Core
{
    /// <summary>
    /// Singleton manager for handling era selection, loading, and transitions.
    /// Persists across scene loads and provides global access to the current era.
    /// </summary>
    /// <remarks>
    /// The EraManager is responsible for:
    /// - Loading and caching era configurations
    /// - Providing access to the current era
    /// - Notifying systems when the era changes
    /// - Validating era configurations on load
    /// </remarks>
    public class EraManager : MonoBehaviour
    {
        // Singleton instance
        private static EraManager _instance;

        /// <summary>
        /// Gets the singleton instance of the EraManager.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// Thrown if accessed before the EraManager is initialized.
        /// </exception>
        public static EraManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    // Try to find existing instance in scene
                    _instance = FindObjectOfType<EraManager>();

                    if (_instance == null)
                    {
                        // Create new instance if none exists
                        var managerObject = new GameObject("EraManager");
                        _instance = managerObject.AddComponent<EraManager>();
                        DontDestroyOnLoad(managerObject);
                    }
                }
                return _instance;
            }
        }

        /// <summary>
        /// Returns true if the EraManager instance exists and is initialized.
        /// Use this for null-safe checks before accessing Instance.
        /// </summary>
        public static bool IsInitialized => _instance != null && _instance._isInitialized;

        [Header("Configuration")]
        [Tooltip("All available eras in the game")]
        [SerializeField] private List<EraConfigSO> _availableEras = new();

        [Tooltip("The default era to use if none is specified")]
        [SerializeField] private EraConfigSO _defaultEra;

        // Private state
        private EraConfigSO _currentEra;
        private bool _isInitialized;
        private readonly Dictionary<string, EraConfigSO> _eraLookup = new();

        // Events
        /// <summary>
        /// Fired when the current era is changed. Provides the old and new era.
        /// </summary>
        public event Action<EraConfigSO, EraConfigSO> OnEraChanged;

        /// <summary>
        /// Fired after era change is complete and all systems have updated.
        /// </summary>
        public event Action<EraConfigSO> OnEraApplied;

        // Properties
        /// <summary>
        /// Gets the currently active era configuration.
        /// </summary>
        public EraConfigSO CurrentEra => _currentEra;

        /// <summary>
        /// Gets all available eras as a read-only collection.
        /// </summary>
        public IReadOnlyList<EraConfigSO> AvailableEras => _availableEras;

        /// <summary>
        /// Gets the number of available eras.
        /// </summary>
        public int EraCount => _availableEras.Count;

        #region Unity Lifecycle

        private void Awake()
        {
            // Enforce singleton pattern
            if (_instance != null && _instance != this)
            {
                Debug.LogWarning("[EraManager] Duplicate instance detected. Destroying.");
                Destroy(gameObject);
                return;
            }

            _instance = this;
            DontDestroyOnLoad(gameObject);

            Initialize();
        }

        private void OnDestroy()
        {
            if (_instance == this)
            {
                _instance = null;
            }
        }

        #endregion

        #region Initialization

        /// <summary>
        /// Initializes the EraManager, building lookup tables and setting default era.
        /// </summary>
        private void Initialize()
        {
            if (_isInitialized)
                return;

            // Build lookup dictionary
            _eraLookup.Clear();
            foreach (var era in _availableEras)
            {
                if (era == null)
                {
                    Debug.LogWarning("[EraManager] Null era in available eras list");
                    continue;
                }

                if (string.IsNullOrEmpty(era.Id))
                {
                    Debug.LogWarning($"[EraManager] Era '{era.name}' has no ID set");
                    continue;
                }

                if (_eraLookup.ContainsKey(era.Id))
                {
                    Debug.LogError($"[EraManager] Duplicate era ID: {era.Id}");
                    continue;
                }

                _eraLookup[era.Id] = era;
            }

            // Validate all eras
            ValidateAllEras();

            // Set default era
            if (_defaultEra != null)
            {
                SetEra(_defaultEra, silent: true);
            }
            else if (_availableEras.Count > 0)
            {
                SetEra(_availableEras[0], silent: true);
            }

            _isInitialized = true;
            Debug.Log($"[EraManager] Initialized with {_availableEras.Count} eras. Current: {_currentEra?.DisplayName ?? "None"}");
        }

        /// <summary>
        /// Validates all registered eras and logs any configuration errors.
        /// </summary>
        private void ValidateAllEras()
        {
            foreach (var era in _availableEras)
            {
                if (era == null)
                    continue;

                if (!era.Validate(out var errors))
                {
                    Debug.LogWarning($"[EraManager] Era '{era.DisplayName}' has validation errors:");
                    foreach (var error in errors)
                    {
                        Debug.LogWarning($"  - {error}");
                    }
                }
            }
        }

        #endregion

        #region Era Selection

        /// <summary>
        /// Sets the current era by reference.
        /// </summary>
        /// <param name="era">The era to set as current.</param>
        /// <param name="silent">If true, suppresses events (used for initialization).</param>
        /// <returns>True if the era was successfully set.</returns>
        public bool SetEra(EraConfigSO era, bool silent = false)
        {
            if (era == null)
            {
                Debug.LogError("[EraManager] Cannot set null era");
                return false;
            }

            if (!_availableEras.Contains(era))
            {
                Debug.LogError($"[EraManager] Era '{era.DisplayName}' is not in the available eras list");
                return false;
            }

            if (_currentEra == era)
            {
                Debug.Log($"[EraManager] Era '{era.DisplayName}' is already active");
                return true;
            }

            var previousEra = _currentEra;
            _currentEra = era;

            Debug.Log($"[EraManager] Era changed: {previousEra?.DisplayName ?? "None"} -> {era.DisplayName}");

            if (!silent)
            {
                OnEraChanged?.Invoke(previousEra, era);
                ApplyEraSettings(era);
                OnEraApplied?.Invoke(era);
            }

            return true;
        }

        /// <summary>
        /// Sets the current era by ID.
        /// </summary>
        /// <param name="eraId">The ID of the era to set.</param>
        /// <returns>True if the era was successfully set.</returns>
        public bool SetEra(string eraId)
        {
            if (string.IsNullOrEmpty(eraId))
            {
                Debug.LogError("[EraManager] Cannot set era with null or empty ID");
                return false;
            }

            if (!_eraLookup.TryGetValue(eraId, out var era))
            {
                Debug.LogError($"[EraManager] Era with ID '{eraId}' not found");
                return false;
            }

            return SetEra(era);
        }

        /// <summary>
        /// Gets an era by its ID.
        /// </summary>
        /// <param name="eraId">The era ID to look up.</param>
        /// <returns>The era configuration, or null if not found.</returns>
        public EraConfigSO GetEra(string eraId)
        {
            if (string.IsNullOrEmpty(eraId))
                return null;

            _eraLookup.TryGetValue(eraId, out var era);
            return era;
        }

        /// <summary>
        /// Checks if an era with the given ID exists.
        /// </summary>
        /// <param name="eraId">The era ID to check.</param>
        /// <returns>True if the era exists.</returns>
        public bool HasEra(string eraId)
        {
            return !string.IsNullOrEmpty(eraId) && _eraLookup.ContainsKey(eraId);
        }

        /// <summary>
        /// Cycles to the next available era.
        /// </summary>
        /// <returns>The newly selected era.</returns>
        public EraConfigSO CycleNext()
        {
            if (_availableEras.Count == 0)
                return null;

            int currentIndex = _currentEra != null ? _availableEras.IndexOf(_currentEra) : -1;
            int nextIndex = (currentIndex + 1) % _availableEras.Count;

            SetEra(_availableEras[nextIndex]);
            return _currentEra;
        }

        /// <summary>
        /// Cycles to the previous available era.
        /// </summary>
        /// <returns>The newly selected era.</returns>
        public EraConfigSO CyclePrevious()
        {
            if (_availableEras.Count == 0)
                return null;

            int currentIndex = _currentEra != null ? _availableEras.IndexOf(_currentEra) : 0;
            int prevIndex = (currentIndex - 1 + _availableEras.Count) % _availableEras.Count;

            SetEra(_availableEras[prevIndex]);
            return _currentEra;
        }

        #endregion

        #region Era Application

        /// <summary>
        /// Applies the visual and audio settings for the given era.
        /// </summary>
        /// <param name="era">The era to apply.</param>
        private void ApplyEraSettings(EraConfigSO era)
        {
            if (era == null)
                return;

            // Apply skybox if available
            if (era.SkyboxMaterial != null)
            {
                RenderSettings.skybox = era.SkyboxMaterial;
                DynamicGI.UpdateEnvironment();
            }

            // Future: Apply battlefield material to ground
            // Future: Start background music
            // Future: Update UI colors

            Debug.Log($"[EraManager] Applied era settings for '{era.DisplayName}'");
        }

        #endregion

        #region Editor Support

#if UNITY_EDITOR
        /// <summary>
        /// Registers an era at runtime (Editor only, for testing).
        /// </summary>
        public void RegisterEra(EraConfigSO era)
        {
            if (era == null || _availableEras.Contains(era))
                return;

            _availableEras.Add(era);
            if (!string.IsNullOrEmpty(era.Id))
            {
                _eraLookup[era.Id] = era;
            }
        }

        /// <summary>
        /// Clears all eras (Editor only, for testing).
        /// </summary>
        public void ClearEras()
        {
            _availableEras.Clear();
            _eraLookup.Clear();
            _currentEra = null;
            _isInitialized = false;
        }
#endif

        #endregion
    }
}
