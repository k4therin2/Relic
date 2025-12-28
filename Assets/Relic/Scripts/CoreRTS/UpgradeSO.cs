using UnityEngine;
using System.Collections.Generic;

namespace Relic.CoreRTS
{
    /// <summary>
    /// ScriptableObject defining a squad upgrade.
    /// Contains stat modifiers that affect all units in a squad.
    /// </summary>
    /// <remarks>
    /// Upgrades are applied to squads and stack multiplicatively for hit chance
    /// and damage. Elevation bonus stacks additively.
    /// See Kyle's milestones.md Milestone 3 for requirements.
    /// </remarks>
    [CreateAssetMenu(fileName = "NewUpgrade", menuName = "Relic/Upgrade")]
    public class UpgradeSO : ScriptableObject
    {
        // Constants
        private const float MIN_MULTIPLIER = 0f;
        private const float MAX_MULTIPLIER = 10f;
        private const float MIN_ELEVATION_BONUS = -1f;
        private const float MAX_ELEVATION_BONUS = 1f;

        [Header("Identity")]
        [Tooltip("Unique identifier for this upgrade")]
        [SerializeField] private string _id;

        [Tooltip("Human-readable display name")]
        [SerializeField] private string _displayName;

        [TextArea(2, 4)]
        [Tooltip("Description of this upgrade's effects")]
        [SerializeField] private string _description;

        [Tooltip("Icon for UI display")]
        [SerializeField] private Sprite _icon;

        [Header("Era Restriction")]
        [Tooltip("Which era this upgrade is available in (All = universal)")]
        [SerializeField] private EraType _era = EraType.Ancient;

        [Header("Stat Modifiers")]
        [Tooltip("Multiplier for hit chance (1.0 = no change, 1.2 = +20%, 0.9 = -10%)")]
        [Range(MIN_MULTIPLIER, MAX_MULTIPLIER)]
        [SerializeField] private float _hitChanceMultiplier = 1f;

        [Tooltip("Multiplier for damage (1.0 = no change, 1.25 = +25%)")]
        [Range(MIN_MULTIPLIER, MAX_MULTIPLIER)]
        [SerializeField] private float _damageMultiplier = 1f;

        [Tooltip("Flat bonus to elevation advantage (-1 to +1)")]
        [Range(MIN_ELEVATION_BONUS, MAX_ELEVATION_BONUS)]
        [SerializeField] private float _elevationBonus = 0f;

        [Header("Stacking Rules")]
        [Tooltip("Maximum number of times this upgrade can be applied to a squad")]
        [Range(1, 10)]
        [SerializeField] private int _maxStacks = 1;

        [Tooltip("Upgrade IDs that are mutually exclusive with this one")]
        [SerializeField] private List<string> _exclusiveWith = new();

        [Header("Cost")]
        [Tooltip("Resource cost to apply this upgrade")]
        [SerializeField] private int _cost = 100;

        // Public read-only properties
        public string Id => _id;
        public string DisplayName => _displayName;
        public string Description => _description;
        public Sprite Icon => _icon;
        public EraType Era => _era;
        public float HitChanceMultiplier => _hitChanceMultiplier;
        public float DamageMultiplier => _damageMultiplier;
        public float ElevationBonus => _elevationBonus;
        public int MaxStacks => _maxStacks;
        public IReadOnlyList<string> ExclusiveWith => _exclusiveWith;
        public int Cost => _cost;

        /// <summary>
        /// Validates the upgrade configuration.
        /// </summary>
        /// <param name="errors">List to receive validation errors.</param>
        /// <returns>True if the configuration is valid.</returns>
        public bool Validate(out List<string> errors)
        {
            errors = new List<string>();

            if (string.IsNullOrWhiteSpace(_id))
                errors.Add("Upgrade ID is required");

            if (string.IsNullOrWhiteSpace(_displayName))
                errors.Add("Display name is required");

            if (_hitChanceMultiplier < MIN_MULTIPLIER || _hitChanceMultiplier > MAX_MULTIPLIER)
                errors.Add($"Hit chance multiplier must be between {MIN_MULTIPLIER} and {MAX_MULTIPLIER}");

            if (_damageMultiplier < MIN_MULTIPLIER || _damageMultiplier > MAX_MULTIPLIER)
                errors.Add($"Damage multiplier must be between {MIN_MULTIPLIER} and {MAX_MULTIPLIER}");

            if (_elevationBonus < MIN_ELEVATION_BONUS || _elevationBonus > MAX_ELEVATION_BONUS)
                errors.Add($"Elevation bonus must be between {MIN_ELEVATION_BONUS} and {MAX_ELEVATION_BONUS}");

            if (_maxStacks < 1)
                errors.Add("Max stacks must be at least 1");

            if (_cost < 0)
                errors.Add("Cost cannot be negative");

            return errors.Count == 0;
        }

        /// <summary>
        /// Applies the hit chance multiplier to a base value.
        /// </summary>
        /// <param name="baseHitChance">The base hit chance to modify.</param>
        /// <returns>The modified hit chance.</returns>
        public float ApplyHitChance(float baseHitChance)
        {
            return baseHitChance * _hitChanceMultiplier;
        }

        /// <summary>
        /// Applies the damage multiplier to a base value.
        /// </summary>
        /// <param name="baseDamage">The base damage to modify.</param>
        /// <returns>The modified damage.</returns>
        public float ApplyDamage(float baseDamage)
        {
            return baseDamage * _damageMultiplier;
        }

        /// <summary>
        /// Applies the elevation bonus to a base value.
        /// </summary>
        /// <param name="baseElevation">The base elevation bonus.</param>
        /// <returns>The modified elevation bonus.</returns>
        public float ApplyElevationBonus(float baseElevation)
        {
            return baseElevation + _elevationBonus;
        }

        /// <summary>
        /// Checks if this upgrade is available for the specified era.
        /// </summary>
        /// <param name="era">The era to check.</param>
        /// <returns>True if this upgrade can be used in the specified era.</returns>
        public bool IsAvailableForEra(EraType era)
        {
            if (_era == EraType.All)
                return true;

            return _era == era;
        }

        /// <summary>
        /// Checks if this upgrade can stack with another upgrade.
        /// </summary>
        /// <param name="other">The other upgrade to check.</param>
        /// <returns>True if the upgrades can be applied together.</returns>
        public bool CanStackWith(UpgradeSO other)
        {
            if (other == null)
                return false;

            // Cannot stack with self (reference equality)
            if (this == other)
                return false;

            // If both have IDs, compare them
            if (!string.IsNullOrEmpty(_id) && !string.IsNullOrEmpty(other._id) && _id == other._id)
                return false;

            // Check mutual exclusivity
            if (!string.IsNullOrEmpty(other._id) && _exclusiveWith.Contains(other._id))
                return false;

            if (!string.IsNullOrEmpty(_id) && other._exclusiveWith.Contains(_id))
                return false;

            return true;
        }

        /// <summary>
        /// Creates a summary of the upgrade's effects for display.
        /// </summary>
        /// <returns>A formatted string describing the effects.</returns>
        public string GetEffectsSummary()
        {
            var effects = new List<string>();

            if (_hitChanceMultiplier != 1f)
            {
                float percent = (_hitChanceMultiplier - 1f) * 100f;
                string sign = percent >= 0 ? "+" : "";
                effects.Add($"{sign}{percent:F0}% Hit Chance");
            }

            if (_damageMultiplier != 1f)
            {
                float percent = (_damageMultiplier - 1f) * 100f;
                string sign = percent >= 0 ? "+" : "";
                effects.Add($"{sign}{percent:F0}% Damage");
            }

            if (_elevationBonus != 0f)
            {
                float percent = _elevationBonus * 100f;
                string sign = _elevationBonus >= 0 ? "+" : "";
                effects.Add($"{sign}{percent:F0}% Elevation Bonus");
            }

            return effects.Count > 0 ? string.Join(", ", effects) : "No stat changes";
        }
    }
}
