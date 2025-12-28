using UnityEngine;

namespace Relic.CoreRTS
{
    /// <summary>
    /// Static class for resolving combat between units.
    /// Implements per-bullet hit chance calculation with range, elevation, and squad modifiers.
    /// </summary>
    /// <remarks>
    /// Combat resolution follows Kyle's milestones.md Milestone 3 requirements:
    /// - Per-bullet hit chance evaluation
    /// - Range affects hit chance via weapon curve
    /// - Elevation affects hit chance via weapon curve
    /// - Squad upgrades modify hit chance and damage
    /// - Hit chance clamped to 5%-95% (always some chance to hit/miss)
    /// </remarks>
    public static class CombatResolver
    {
        #region Constants

        /// <summary>Minimum possible hit chance (5%).</summary>
        public const float MIN_HIT_CHANCE = 0.05f;

        /// <summary>Maximum possible hit chance (95%).</summary>
        public const float MAX_HIT_CHANCE = 0.95f;

        /// <summary>Maximum elevation difference considered for curve evaluation.</summary>
        private const float MAX_ELEVATION_DIFFERENCE = 10f;

        #endregion

        #region Main Combat Resolution

        /// <summary>
        /// Resolves combat between an attacker and target using the specified weapon.
        /// </summary>
        /// <param name="attacker">The attacking unit.</param>
        /// <param name="target">The target unit.</param>
        /// <param name="weapon">The weapon being used.</param>
        /// <returns>Combat result containing shots fired, hits, damage, and destruction status.</returns>
        public static CombatResult ResolveCombat(UnitController attacker, UnitController target, WeaponStatsSO weapon)
        {
            // Validate inputs
            if (attacker == null || target == null || weapon == null)
            {
                return CombatResult.Empty;
            }

            if (!attacker.IsAlive || !target.IsAlive)
            {
                return CombatResult.Empty;
            }

            // Calculate hit chance
            float hitChance = CalculateHitChance(attacker, target, weapon);

            // Calculate damage per hit (with squad modifier)
            float damagePerHit = CalculateDamagePerHit(attacker, weapon);

            // Resolve each bullet
            int shotsPerBurst = weapon.ShotsPerBurst;
            int shotsHit = 0;
            float totalDamage = 0f;
            bool targetDestroyed = false;

            for (int i = 0; i < shotsPerBurst && !targetDestroyed; i++)
            {
                // Roll for hit
                if (Random.value <= hitChance)
                {
                    shotsHit++;
                    totalDamage += damagePerHit;

                    // Apply damage to target
                    target.TakeDamage(Mathf.RoundToInt(damagePerHit));

                    // Check if target is destroyed
                    if (!target.IsAlive)
                    {
                        targetDestroyed = true;
                    }
                }
            }

            return new CombatResult(
                shotsFired: shotsPerBurst,
                shotsHit: shotsHit,
                totalDamage: totalDamage,
                targetDestroyed: targetDestroyed
            );
        }

        #endregion

        #region Hit Chance Calculation

        /// <summary>
        /// Calculates the hit chance for an attack.
        /// Applies range curve, elevation curve, and squad modifiers.
        /// </summary>
        /// <param name="attacker">The attacking unit.</param>
        /// <param name="target">The target unit.</param>
        /// <param name="weapon">The weapon being used.</param>
        /// <returns>Hit chance between MIN_HIT_CHANCE and MAX_HIT_CHANCE.</returns>
        public static float CalculateHitChance(UnitController attacker, UnitController target, WeaponStatsSO weapon)
        {
            if (attacker == null || target == null || weapon == null)
                return MIN_HIT_CHANCE;

            // Start with base hit chance
            float hitChance = weapon.BaseHitChance;

            // Apply range modifier
            float distance = CalculateDistance(attacker, target);
            float rangeModifier = CalculateRangeModifier(weapon, distance);
            hitChance *= rangeModifier;

            // Apply elevation modifier
            float elevationDiff = CalculateElevationDifference(attacker, target);
            float elevationModifier = CalculateElevationModifier(weapon, elevationDiff);
            hitChance *= elevationModifier;

            // Apply squad hit chance multiplier
            float squadModifier = attacker.GetSquadHitChanceMultiplier();
            hitChance *= squadModifier;

            // Apply squad elevation bonus (additive)
            float squadElevationBonus = attacker.GetSquadElevationBonus();
            hitChance += squadElevationBonus;

            // Clamp to valid range
            return Mathf.Clamp(hitChance, MIN_HIT_CHANCE, MAX_HIT_CHANCE);
        }

        /// <summary>
        /// Calculates the range modifier using the weapon's range curve.
        /// </summary>
        /// <param name="weapon">The weapon with the range curve.</param>
        /// <param name="distance">Distance to target.</param>
        /// <returns>Range modifier (0.0 to 1.0+).</returns>
        private static float CalculateRangeModifier(WeaponStatsSO weapon, float distance)
        {
            if (weapon.EffectiveRange <= 0)
                return 1f;

            // Normalize distance to 0-1 range (0 = point blank, 1 = effective range)
            float normalizedRange = Mathf.Clamp01(distance / weapon.EffectiveRange);

            // Evaluate weapon's range curve
            return weapon.EvaluateRangeCurve(normalizedRange);
        }

        /// <summary>
        /// Calculates the elevation modifier using the weapon's elevation curve.
        /// </summary>
        /// <param name="weapon">The weapon with the elevation curve.</param>
        /// <param name="elevationDiff">Height difference (positive = attacker higher).</param>
        /// <returns>Elevation modifier.</returns>
        private static float CalculateElevationModifier(WeaponStatsSO weapon, float elevationDiff)
        {
            // Normalize elevation to -1 to +1 range
            float normalizedElevation = Mathf.Clamp(elevationDiff / MAX_ELEVATION_DIFFERENCE, -1f, 1f);

            // Evaluate weapon's elevation curve
            return weapon.EvaluateElevationCurve(normalizedElevation);
        }

        #endregion

        #region Damage Calculation

        /// <summary>
        /// Calculates damage per hit, applying squad modifiers.
        /// </summary>
        /// <param name="attacker">The attacking unit.</param>
        /// <param name="weapon">The weapon being used.</param>
        /// <returns>Damage per successful hit.</returns>
        public static float CalculateDamagePerHit(UnitController attacker, WeaponStatsSO weapon)
        {
            if (weapon == null)
                return 0f;

            float baseDamage = weapon.BaseDamage;

            // Apply squad damage multiplier
            if (attacker != null)
            {
                float squadModifier = attacker.GetSquadDamageMultiplier();
                baseDamage *= squadModifier;
            }

            return baseDamage;
        }

        #endregion

        #region Distance and Elevation Helpers

        /// <summary>
        /// Calculates the horizontal distance between two units.
        /// Uses XZ plane distance (ignoring Y for distance, Y is used for elevation).
        /// </summary>
        /// <param name="from">Source unit.</param>
        /// <param name="to">Target unit.</param>
        /// <returns>Horizontal distance in world units.</returns>
        public static float CalculateDistance(UnitController from, UnitController to)
        {
            if (from == null || to == null)
                return 0f;

            Vector3 fromPos = from.transform.position;
            Vector3 toPos = to.transform.position;

            // Calculate XZ plane distance (horizontal)
            Vector3 horizontalDiff = new Vector3(toPos.x - fromPos.x, 0f, toPos.z - fromPos.z);
            return horizontalDiff.magnitude;
        }

        /// <summary>
        /// Calculates the elevation difference between attacker and target.
        /// Positive value means attacker is higher (advantage).
        /// </summary>
        /// <param name="attacker">The attacking unit.</param>
        /// <param name="target">The target unit.</param>
        /// <returns>Height difference in world units (positive = attacker higher).</returns>
        public static float CalculateElevationDifference(UnitController attacker, UnitController target)
        {
            if (attacker == null || target == null)
                return 0f;

            return attacker.transform.position.y - target.transform.position.y;
        }

        #endregion

        #region Utility Methods

        /// <summary>
        /// Checks if a target is within effective range of the weapon.
        /// </summary>
        /// <param name="attacker">The attacking unit.</param>
        /// <param name="target">The target unit.</param>
        /// <param name="weapon">The weapon to check range for.</param>
        /// <returns>True if target is within effective range.</returns>
        public static bool IsInRange(UnitController attacker, UnitController target, WeaponStatsSO weapon)
        {
            if (attacker == null || target == null || weapon == null)
                return false;

            float distance = CalculateDistance(attacker, target);
            return distance <= weapon.EffectiveRange;
        }

        /// <summary>
        /// Calculates the theoretical DPS (damage per second) at a given range.
        /// </summary>
        /// <param name="attacker">The attacking unit (for squad modifiers).</param>
        /// <param name="target">The target unit (for position).</param>
        /// <param name="weapon">The weapon to calculate DPS for.</param>
        /// <returns>Expected damage per second.</returns>
        public static float CalculateExpectedDPS(UnitController attacker, UnitController target, WeaponStatsSO weapon)
        {
            if (weapon == null)
                return 0f;

            float hitChance = CalculateHitChance(attacker, target, weapon);
            float damagePerHit = CalculateDamagePerHit(attacker, weapon);
            float shotsPerSecond = weapon.FireRate;
            float shotsPerBurst = weapon.ShotsPerBurst;

            // Expected hits per burst
            float expectedHits = shotsPerBurst * hitChance;

            // Expected damage per burst
            float damagePerBurst = expectedHits * damagePerHit;

            // Bursts per second (fire rate is bursts per second)
            return damagePerBurst * shotsPerSecond;
        }

        #endregion
    }
}
