using UnityEngine;

namespace Relic.CoreRTS
{
    /// <summary>
    /// ScriptableObject defining a unit archetype - the template for creating unit instances.
    /// Contains all static data that defines a unit type (health, speed, weapon, prefab, etc.).
    /// </summary>
    /// <remarks>
    /// Unit archetypes are referenced by EraConfig to define available unit types per era.
    /// See Kyle's milestones.md Milestone 2 for requirements.
    /// </remarks>
    [CreateAssetMenu(fileName = "NewUnitArchetype", menuName = "Relic/Unit Archetype")]
    public class UnitArchetypeSO : ScriptableObject
    {
        // Constants
        private const float MIN_MOVE_SPEED = 0.1f;
        private const float MAX_MOVE_SPEED = 20f;
        private const int MIN_HEALTH = 1;
        private const int MAX_HEALTH = 10000;

        [Header("Identity")]
        [Tooltip("Unique identifier for this unit type (e.g., 'ancient_legionnaire', 'wwii_rifleman')")]
        [SerializeField] private string _id;

        [Tooltip("Human-readable display name")]
        [SerializeField] private string _displayName;

        [TextArea(2, 4)]
        [Tooltip("Description of this unit type")]
        [SerializeField] private string _description;

        [Tooltip("Icon for UI display")]
        [SerializeField] private Sprite _icon;

        [Header("Stats")]
        [Tooltip("Maximum health points")]
        [Range(MIN_HEALTH, MAX_HEALTH)]
        [SerializeField] private int _maxHealth = 100;

        [Tooltip("Movement speed in units per second")]
        [Range(MIN_MOVE_SPEED, MAX_MOVE_SPEED)]
        [SerializeField] private float _moveSpeed = 3f;

        [Tooltip("Detection range for auto-targeting enemies")]
        [Range(0f, 50f)]
        [SerializeField] private float _detectionRange = 10f;

        [Header("Combat")]
        [Tooltip("Weapon used by this unit type")]
        [SerializeField] private WeaponStatsSO _weapon;

        [Tooltip("Base armor value (reduces incoming damage)")]
        [Range(0, 100)]
        [SerializeField] private int _armor = 0;

        [Header("Visual")]
        [Tooltip("Prefab to instantiate for this unit type")]
        [SerializeField] private GameObject _unitPrefab;

        [Tooltip("Scale multiplier for this unit")]
        [SerializeField] private float _scale = 1f;

        [Tooltip("Height offset from ground")]
        [SerializeField] private float _heightOffset = 0f;

        [Header("Audio")]
        [Tooltip("Sound played when unit is selected")]
        [SerializeField] private AudioClip _selectSound;

        [Tooltip("Sound played when unit receives a command")]
        [SerializeField] private AudioClip _commandSound;

        [Tooltip("Sound played when unit dies")]
        [SerializeField] private AudioClip _deathSound;

        // Public read-only properties
        public string Id => _id;
        public string DisplayName => _displayName;
        public string Description => _description;
        public Sprite Icon => _icon;
        public int MaxHealth => _maxHealth;
        public float MoveSpeed => _moveSpeed;
        public float DetectionRange => _detectionRange;
        public WeaponStatsSO Weapon => _weapon;
        public int Armor => _armor;
        public GameObject UnitPrefab => _unitPrefab;
        public float Scale => _scale;
        public float HeightOffset => _heightOffset;
        public AudioClip SelectSound => _selectSound;
        public AudioClip CommandSound => _commandSound;
        public AudioClip DeathSound => _deathSound;

        /// <summary>
        /// Validates the archetype configuration.
        /// </summary>
        /// <param name="errors">List to receive validation errors.</param>
        /// <returns>True if the configuration is valid.</returns>
        public bool Validate(out System.Collections.Generic.List<string> errors)
        {
            errors = new System.Collections.Generic.List<string>();

            if (string.IsNullOrWhiteSpace(_id))
                errors.Add("Archetype ID is required");

            if (string.IsNullOrWhiteSpace(_displayName))
                errors.Add("Display name is required");

            if (_maxHealth < MIN_HEALTH)
                errors.Add($"Max health must be at least {MIN_HEALTH}");

            if (_moveSpeed < MIN_MOVE_SPEED || _moveSpeed > MAX_MOVE_SPEED)
                errors.Add($"Move speed must be between {MIN_MOVE_SPEED} and {MAX_MOVE_SPEED}");

            if (_unitPrefab == null)
                errors.Add("Unit prefab is required");

            if (_scale <= 0)
                errors.Add("Scale must be greater than 0");

            return errors.Count == 0;
        }

        /// <summary>
        /// Creates runtime stats from this archetype.
        /// </summary>
        /// <returns>A new UnitStats instance with values from this archetype.</returns>
        public UnitStats CreateStats()
        {
            return new UnitStats
            {
                ArchetypeId = _id,
                MaxHealth = _maxHealth,
                CurrentHealth = _maxHealth,
                MoveSpeed = _moveSpeed,
                DetectionRange = _detectionRange,
                Armor = _armor
            };
        }

#if UNITY_EDITOR || UNITY_INCLUDE_TESTS
        /// <summary>
        /// Sets test values for unit testing. Only available in Editor/Test builds.
        /// </summary>
        /// <param name="id">Unit identifier.</param>
        /// <param name="displayName">Display name.</param>
        /// <param name="prefab">Unit prefab.</param>
        /// <param name="maxHealth">Max health.</param>
        /// <param name="moveSpeed">Movement speed.</param>
        /// <param name="detectionRange">Detection range.</param>
        /// <param name="armor">Armor value.</param>
        public void SetTestValues(string id, string displayName, GameObject prefab,
            int maxHealth = 100, float moveSpeed = 3f, float detectionRange = 10f, int armor = 0)
        {
            _id = id;
            _displayName = displayName;
            _unitPrefab = prefab;
            _maxHealth = maxHealth;
            _moveSpeed = moveSpeed;
            _detectionRange = detectionRange;
            _armor = armor;
        }
#endif
    }

    /// <summary>
    /// Runtime stats for a unit instance. Can be modified during gameplay.
    /// </summary>
    [System.Serializable]
    public struct UnitStats
    {
        public string ArchetypeId;
        public int MaxHealth;
        public int CurrentHealth;
        public float MoveSpeed;
        public float DetectionRange;
        public int Armor;

        /// <summary>
        /// Calculates health as a percentage (0-1).
        /// </summary>
        public float HealthPercent => MaxHealth > 0 ? (float)CurrentHealth / MaxHealth : 0f;

        /// <summary>
        /// Returns true if the unit is alive.
        /// </summary>
        public bool IsAlive => CurrentHealth > 0;

        /// <summary>
        /// Applies damage to the unit, respecting armor.
        /// </summary>
        /// <param name="damage">Raw damage to apply.</param>
        /// <returns>Actual damage applied after armor reduction.</returns>
        public int ApplyDamage(int damage)
        {
            if (damage <= 0) return 0;

            // Simple armor calculation: reduce damage by armor percentage
            float armorReduction = Armor / 100f;
            int actualDamage = Mathf.Max(1, Mathf.RoundToInt(damage * (1f - armorReduction)));

            CurrentHealth = Mathf.Max(0, CurrentHealth - actualDamage);
            return actualDamage;
        }

        /// <summary>
        /// Heals the unit.
        /// </summary>
        /// <param name="amount">Amount to heal.</param>
        /// <returns>Actual amount healed.</returns>
        public int Heal(int amount)
        {
            if (amount <= 0 || CurrentHealth >= MaxHealth) return 0;

            int previousHealth = CurrentHealth;
            CurrentHealth = Mathf.Min(MaxHealth, CurrentHealth + amount);
            return CurrentHealth - previousHealth;
        }
    }
}
