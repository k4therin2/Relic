using UnityEngine;
using System.Collections.Generic;

namespace Relic.Data
{
    /// <summary>
    /// ScriptableObject that defines an era's configuration including
    /// unit archetypes, visual themes, and gameplay settings.
    /// </summary>
    /// <remarks>
    /// Eras are data-driven configurations that can be swapped at runtime
    /// to completely change the game's theme and unit roster.
    /// See Kyle's milestones.md for era requirements.
    /// </remarks>
    [CreateAssetMenu(fileName = "NewEra", menuName = "Relic/Era Config")]
    public class EraConfigSO : ScriptableObject
    {
        // Constants
        private const int MAX_UNIT_ARCHETYPES = 10;
        private const int MAX_UPGRADES = 20;

        [Header("Identity")]
        [Tooltip("Unique identifier for this era (e.g., 'ancient', 'medieval')")]
        [SerializeField] private string _id;

        [Tooltip("Human-readable display name")]
        [SerializeField] private string _displayName;

        [TextArea(2, 5)]
        [Tooltip("Description of this era for UI display")]
        [SerializeField] private string _description;

        [Header("Visual Theme")]
        [Tooltip("Primary color for this era's UI and effects")]
        [SerializeField] private Color _primaryColor = Color.white;

        [Tooltip("Secondary/accent color")]
        [SerializeField] private Color _secondaryColor = Color.gray;

        [Tooltip("Material to apply to battlefield ground in this era")]
        [SerializeField] private Material _battlefieldMaterial;

        [Tooltip("Skybox or environment settings for this era")]
        [SerializeField] private Material _skyboxMaterial;

        [Header("Audio Theme")]
        [Tooltip("Background music for battles in this era")]
        [SerializeField] private AudioClip _battleMusic;

        [Tooltip("Ambient audio for this era")]
        [SerializeField] private AudioClip _ambientAudio;

        [Header("Unit Configuration")]
        [Tooltip("Unit archetypes available in this era (max 10)")]
        [SerializeField] private List<UnitArchetypeReference> _unitArchetypes = new();

        [Header("Upgrade Configuration")]
        [Tooltip("Upgrades available to squads in this era (max 20)")]
        [SerializeField] private List<UpgradeReference> _availableUpgrades = new();

        [Header("Economy Settings")]
        [Tooltip("Starting resources for this era")]
        [SerializeField] private int _startingResources = 1000;

        [Tooltip("Resource regeneration rate per second")]
        [SerializeField] private float _resourceRegenRate = 10f;

        [Tooltip("Maximum resource cap")]
        [SerializeField] private int _maxResources = 5000;

        // Public Properties (read-only)
        public string Id => _id;
        public string DisplayName => _displayName;
        public string Description => _description;
        public Color PrimaryColor => _primaryColor;
        public Color SecondaryColor => _secondaryColor;
        public Material BattlefieldMaterial => _battlefieldMaterial;
        public Material SkyboxMaterial => _skyboxMaterial;
        public AudioClip BattleMusic => _battleMusic;
        public AudioClip AmbientAudio => _ambientAudio;
        public IReadOnlyList<UnitArchetypeReference> UnitArchetypes => _unitArchetypes;
        public IReadOnlyList<UpgradeReference> AvailableUpgrades => _availableUpgrades;
        public int StartingResources => _startingResources;
        public float ResourceRegenRate => _resourceRegenRate;
        public int MaxResources => _maxResources;

        /// <summary>
        /// Validates the era configuration for completeness and correctness.
        /// </summary>
        /// <returns>True if the configuration is valid.</returns>
        public bool Validate(out List<string> errors)
        {
            errors = new List<string>();

            if (string.IsNullOrWhiteSpace(_id))
                errors.Add("Era ID is required");

            if (string.IsNullOrWhiteSpace(_displayName))
                errors.Add("Display name is required");

            if (_unitArchetypes.Count == 0)
                errors.Add("At least one unit archetype is required");

            if (_unitArchetypes.Count > MAX_UNIT_ARCHETYPES)
                errors.Add($"Too many unit archetypes (max {MAX_UNIT_ARCHETYPES})");

            if (_availableUpgrades.Count > MAX_UPGRADES)
                errors.Add($"Too many upgrades (max {MAX_UPGRADES})");

            if (_startingResources < 0)
                errors.Add("Starting resources cannot be negative");

            if (_resourceRegenRate < 0)
                errors.Add("Resource regeneration rate cannot be negative");

            if (_maxResources < _startingResources)
                errors.Add("Max resources must be >= starting resources");

            return errors.Count == 0;
        }

        /// <summary>
        /// Gets a unit archetype reference by ID.
        /// </summary>
        /// <param name="archetypeId">The archetype ID to find.</param>
        /// <returns>The archetype reference, or null if not found.</returns>
        public UnitArchetypeReference GetArchetype(string archetypeId)
        {
            if (string.IsNullOrEmpty(archetypeId))
                return null;

            return _unitArchetypes.Find(archetype => archetype.Id == archetypeId);
        }

        /// <summary>
        /// Gets an upgrade reference by ID.
        /// </summary>
        /// <param name="upgradeId">The upgrade ID to find.</param>
        /// <returns>The upgrade reference, or null if not found.</returns>
        public UpgradeReference GetUpgrade(string upgradeId)
        {
            if (string.IsNullOrEmpty(upgradeId))
                return null;

            return _availableUpgrades.Find(upgrade => upgrade.Id == upgradeId);
        }
    }

    /// <summary>
    /// Lightweight reference to a unit archetype within an era.
    /// The actual archetype ScriptableObject will be defined in Milestone 2.
    /// </summary>
    [System.Serializable]
    public class UnitArchetypeReference
    {
        [Tooltip("Unique identifier for this unit type")]
        [SerializeField] private string _id;

        [Tooltip("Display name for this unit")]
        [SerializeField] private string _displayName;

        [Tooltip("Cost to spawn this unit")]
        [SerializeField] private int _cost = 100;

        [Tooltip("Maximum units of this type allowed (0 = unlimited)")]
        [SerializeField] private int _maxCount = 0;

        [Tooltip("Icon for UI display")]
        [SerializeField] private Sprite _icon;

        // Future: Reference to actual UnitArchetypeSO when implemented in Milestone 2
        // [SerializeField] private UnitArchetypeSO _archetype;

        public string Id => _id;
        public string DisplayName => _displayName;
        public int Cost => _cost;
        public int MaxCount => _maxCount;
        public Sprite Icon => _icon;
    }

    /// <summary>
    /// Lightweight reference to an upgrade within an era.
    /// The actual upgrade ScriptableObject will be defined in Milestone 3.
    /// </summary>
    [System.Serializable]
    public class UpgradeReference
    {
        [Tooltip("Unique identifier for this upgrade")]
        [SerializeField] private string _id;

        [Tooltip("Display name for this upgrade")]
        [SerializeField] private string _displayName;

        [TextArea(1, 3)]
        [Tooltip("Description of what this upgrade does")]
        [SerializeField] private string _description;

        [Tooltip("Cost to purchase this upgrade")]
        [SerializeField] private int _cost = 200;

        [Tooltip("Icon for UI display")]
        [SerializeField] private Sprite _icon;

        // Future: Reference to actual UpgradeDefinitionSO when implemented in Milestone 3
        // [SerializeField] private UpgradeDefinitionSO _upgrade;

        public string Id => _id;
        public string DisplayName => _displayName;
        public string Description => _description;
        public int Cost => _cost;
        public Sprite Icon => _icon;
    }
}
