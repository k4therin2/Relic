namespace Relic.CoreRTS
{
    /// <summary>
    /// Immutable struct containing the results of a combat resolution.
    /// Returned by CombatResolver.ResolveCombat().
    /// </summary>
    /// <remarks>
    /// See Kyle's milestones.md Milestone 3 for combat system requirements.
    /// </remarks>
    public readonly struct CombatResult
    {
        /// <summary>Number of shots fired in this combat resolution.</summary>
        public readonly int ShotsFired;

        /// <summary>Number of shots that hit the target.</summary>
        public readonly int ShotsHit;

        /// <summary>Total damage dealt to the target.</summary>
        public readonly float TotalDamage;

        /// <summary>Whether the target was destroyed by this attack.</summary>
        public readonly bool TargetDestroyed;

        /// <summary>
        /// Creates a new CombatResult with the specified values.
        /// </summary>
        public CombatResult(int shotsFired, int shotsHit, float totalDamage, bool targetDestroyed)
        {
            ShotsFired = shotsFired;
            ShotsHit = shotsHit;
            TotalDamage = totalDamage;
            TargetDestroyed = targetDestroyed;
        }

        /// <summary>
        /// Gets the accuracy of this combat (0.0 to 1.0).
        /// Returns 0 if no shots were fired.
        /// </summary>
        public float Accuracy => ShotsFired > 0 ? (float)ShotsHit / ShotsFired : 0f;

        /// <summary>
        /// Gets the average damage per shot (including misses).
        /// </summary>
        public float AverageDamagePerShot => ShotsFired > 0 ? TotalDamage / ShotsFired : 0f;

        /// <summary>
        /// Gets the average damage per hit.
        /// </summary>
        public float AverageDamagePerHit => ShotsHit > 0 ? TotalDamage / ShotsHit : 0f;

        /// <summary>
        /// Returns a formatted summary of the combat result.
        /// </summary>
        public override string ToString()
        {
            return $"Combat: {ShotsHit}/{ShotsFired} hits ({Accuracy:P0}), {TotalDamage:F0} damage" +
                   (TargetDestroyed ? " [DESTROYED]" : "");
        }

        /// <summary>
        /// Empty result for invalid combat scenarios.
        /// </summary>
        public static CombatResult Empty => new CombatResult(0, 0, 0f, false);
    }
}
