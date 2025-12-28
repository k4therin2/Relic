using UnityEngine;
using System.Collections.Generic;

namespace Relic.CoreRTS
{
    /// <summary>
    /// ScriptableObject defining weapon statistics for combat.
    /// Contains fire rate, damage, hit chance, and curves for range/elevation modifiers.
    /// </summary>
    /// <remarks>
    /// Weapons are referenced by UnitArchetype to define combat capabilities.
    /// See Kyle's milestones.md Milestone 3 for requirements.
    /// </remarks>
    [CreateAssetMenu(fileName = "NewWeapon", menuName = "Relic/Weapon Stats")]
    public class WeaponStatsSO : ScriptableObject
    {
        // Constants
        private const int MIN_SHOTS_PER_BURST = 1;
        private const int MAX_SHOTS_PER_BURST = 100;
        private const float MIN_FIRE_RATE = 0.1f;
        private const float MAX_FIRE_RATE = 30f;
        private const float MIN_DAMAGE = 0.1f;
        private const float MAX_DAMAGE = 10000f;
        private const float MIN_RANGE = 1f;
        private const float MAX_RANGE = 500f;
        private const float MAX_ELEVATION_BONUS = 0.5f; // +/- 50%

        [Header("Identity")]
        [Tooltip("Unique identifier for this weapon type")]
        [SerializeField] private string _id;

        [Tooltip("Human-readable display name")]
        [SerializeField] private string _displayName;

        [TextArea(2, 4)]
        [Tooltip("Description of this weapon")]
        [SerializeField] private string _description;

        [Tooltip("Icon for UI display")]
        [SerializeField] private Sprite _icon;

        [Header("Fire Rate")]
        [Tooltip("Number of shots fired per burst")]
        [Range(MIN_SHOTS_PER_BURST, MAX_SHOTS_PER_BURST)]
        [SerializeField] private int _shotsPerBurst = 1;

        [Tooltip("Shots per second (fire rate)")]
        [Range(MIN_FIRE_RATE, MAX_FIRE_RATE)]
        [SerializeField] private float _fireRate = 1f;

        [Header("Accuracy")]
        [Tooltip("Base hit chance at point blank range (0-1)")]
        [Range(0f, 1f)]
        [SerializeField] private float _baseHitChance = 0.7f;

        [Tooltip("Curve for hit chance falloff based on range (0=point blank, 1=effective range)")]
        [SerializeField] private AnimationCurve _rangeHitCurve = CreateDefaultRangeCurve();

        [Tooltip("Curve for elevation bonus/penalty (-1=target much higher, 0=same level, 1=attacker much higher)")]
        [SerializeField] private AnimationCurve _elevationBonusCurve = CreateDefaultElevationCurve();

        [Header("Damage")]
        [Tooltip("Base damage per hit")]
        [Range(MIN_DAMAGE, MAX_DAMAGE)]
        [SerializeField] private float _baseDamage = 10f;

        [Header("Range")]
        [Tooltip("Effective range in units (beyond this, accuracy drops significantly)")]
        [Range(MIN_RANGE, MAX_RANGE)]
        [SerializeField] private float _effectiveRange = 20f;

        [Tooltip("Maximum range in units (beyond this, cannot hit at all)")]
        [Range(MIN_RANGE, MAX_RANGE)]
        [SerializeField] private float _maxRange = 40f;

        [Header("Visual/Audio")]
        [Tooltip("Projectile prefab (if any)")]
        [SerializeField] private GameObject _projectilePrefab;

        [Tooltip("Muzzle flash effect")]
        [SerializeField] private GameObject _muzzleFlashPrefab;

        [Tooltip("Fire sound")]
        [SerializeField] private AudioClip _fireSound;

        [Tooltip("Impact sound")]
        [SerializeField] private AudioClip _impactSound;

        // Public read-only properties
        public string Id => _id;
        public string DisplayName => _displayName;
        public string Description => _description;
        public Sprite Icon => _icon;
        public int ShotsPerBurst => _shotsPerBurst;
        public float FireRate => _fireRate;
        public float BaseHitChance => _baseHitChance;
        public float BaseDamage => _baseDamage;
        public float EffectiveRange => _effectiveRange;
        public float MaxRange => _maxRange;
        public AnimationCurve RangeHitCurve => _rangeHitCurve;
        public AnimationCurve ElevationBonusCurve => _elevationBonusCurve;
        public GameObject ProjectilePrefab => _projectilePrefab;
        public GameObject MuzzleFlashPrefab => _muzzleFlashPrefab;
        public AudioClip FireSound => _fireSound;
        public AudioClip ImpactSound => _impactSound;

        /// <summary>
        /// Time between individual shots based on fire rate.
        /// </summary>
        public float TimeBetweenShots => 1f / _fireRate;

        /// <summary>
        /// Duration of a complete burst.
        /// </summary>
        public float BurstDuration => (_shotsPerBurst - 1) * TimeBetweenShots;

        /// <summary>
        /// Creates the default range hit curve.
        /// 100% at point blank, dropping to 50% at effective range.
        /// </summary>
        private static AnimationCurve CreateDefaultRangeCurve()
        {
            return new AnimationCurve(
                new Keyframe(0f, 1f),      // 100% at point blank
                new Keyframe(0.5f, 0.8f),  // 80% at half effective range
                new Keyframe(1f, 0.5f),    // 50% at effective range
                new Keyframe(2f, 0.1f)     // 10% at 2x effective range
            );
        }

        /// <summary>
        /// Creates the default elevation bonus curve.
        /// Higher elevation = bonus, lower elevation = penalty.
        /// </summary>
        private static AnimationCurve CreateDefaultElevationCurve()
        {
            return new AnimationCurve(
                new Keyframe(-10f, -0.3f),  // -30% if target is 10 units higher
                new Keyframe(-5f, -0.15f),  // -15% if target is 5 units higher
                new Keyframe(0f, 0f),       // No modifier at same level
                new Keyframe(5f, 0.15f),    // +15% if attacker is 5 units higher
                new Keyframe(10f, 0.3f)     // +30% if attacker is 10 units higher
            );
        }

        /// <summary>
        /// Validates the weapon configuration.
        /// </summary>
        /// <param name="errors">List to receive validation errors.</param>
        /// <returns>True if the configuration is valid.</returns>
        public bool Validate(out List<string> errors)
        {
            errors = new List<string>();

            if (string.IsNullOrWhiteSpace(_id))
                errors.Add("Weapon ID is required");

            if (string.IsNullOrWhiteSpace(_displayName))
                errors.Add("Display name is required");

            if (_shotsPerBurst < MIN_SHOTS_PER_BURST)
                errors.Add($"Shots per burst must be at least {MIN_SHOTS_PER_BURST}");

            if (_fireRate < MIN_FIRE_RATE || _fireRate > MAX_FIRE_RATE)
                errors.Add($"Fire rate must be between {MIN_FIRE_RATE} and {MAX_FIRE_RATE}");

            if (_baseHitChance < 0f || _baseHitChance > 1f)
                errors.Add("Base hit chance must be between 0 and 1");

            if (_baseDamage < MIN_DAMAGE)
                errors.Add($"Base damage must be at least {MIN_DAMAGE}");

            if (_effectiveRange < MIN_RANGE)
                errors.Add($"Effective range must be at least {MIN_RANGE}");

            if (_maxRange < _effectiveRange)
                errors.Add("Max range must be greater than or equal to effective range");

            if (_rangeHitCurve == null || _rangeHitCurve.keys.Length == 0)
                errors.Add("Range hit curve is required");

            if (_elevationBonusCurve == null || _elevationBonusCurve.keys.Length == 0)
                errors.Add("Elevation bonus curve is required");

            return errors.Count == 0;
        }

        /// <summary>
        /// Gets the hit chance modifier based on range.
        /// </summary>
        /// <param name="range">Distance to target in units.</param>
        /// <returns>Hit chance at the given range (0-1).</returns>
        public float GetHitChanceAtRange(float range)
        {
            // Treat negative range as zero (point blank)
            range = Mathf.Max(0f, range);

            // Normalize range to effective range (0 = point blank, 1 = effective range)
            float normalizedRange = range / _effectiveRange;

            // Evaluate the range curve
            float rangeMultiplier = _rangeHitCurve.Evaluate(normalizedRange);

            // Apply to base hit chance and clamp
            float hitChance = _baseHitChance * rangeMultiplier;
            return Mathf.Max(0f, hitChance);
        }

        /// <summary>
        /// Gets the elevation bonus/penalty.
        /// </summary>
        /// <param name="elevationDifference">Attacker elevation - target elevation (positive = attacker higher).</param>
        /// <returns>Hit chance modifier (-0.5 to +0.5).</returns>
        public float GetElevationBonus(float elevationDifference)
        {
            // Evaluate the elevation curve
            float bonus = _elevationBonusCurve.Evaluate(elevationDifference);

            // Clamp to maximum bonus range
            return Mathf.Clamp(bonus, -MAX_ELEVATION_BONUS, MAX_ELEVATION_BONUS);
        }

        /// <summary>
        /// Calculates damage with a multiplier.
        /// </summary>
        /// <param name="multiplier">Damage multiplier (from squad upgrades, etc.).</param>
        /// <returns>Final damage value.</returns>
        public float CalculateDamage(float multiplier)
        {
            if (multiplier <= 0f)
                return 0f;

            return _baseDamage * multiplier;
        }

        /// <summary>
        /// Checks if a target is within maximum range.
        /// </summary>
        /// <param name="distance">Distance to target.</param>
        /// <returns>True if target is within range.</returns>
        public bool IsInRange(float distance)
        {
            return distance >= 0f && distance <= _maxRange;
        }

        /// <summary>
        /// Checks if a target is within effective range.
        /// </summary>
        /// <param name="distance">Distance to target.</param>
        /// <returns>True if target is within effective range.</returns>
        public bool IsInEffectiveRange(float distance)
        {
            return distance >= 0f && distance <= _effectiveRange;
        }
    }
}
