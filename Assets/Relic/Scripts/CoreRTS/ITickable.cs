namespace Relic.CoreRTS
{
    /// <summary>
    /// Interface for components that receive centralized tick updates.
    /// Use this instead of Unity's Update() for better performance with many entities.
    /// </summary>
    /// <remarks>
    /// The TickManager calls OnTick at configurable intervals, allowing
    /// different tick rates for different priorities (e.g., AI at 10Hz, physics at 60Hz).
    /// </remarks>
    public interface ITickable
    {
        /// <summary>
        /// Called by TickManager at the configured tick rate.
        /// </summary>
        /// <param name="deltaTime">Time since last tick for this priority level.</param>
        void OnTick(float deltaTime);

        /// <summary>
        /// The priority level of this tickable. Higher priority = more frequent ticks.
        /// </summary>
        TickPriority Priority { get; }

        /// <summary>
        /// Whether this tickable is currently active and should receive ticks.
        /// </summary>
        bool IsTickActive { get; }
    }

    /// <summary>
    /// Priority levels for tick frequency. Lower values = less frequent ticks.
    /// </summary>
    public enum TickPriority
    {
        /// <summary>Low frequency ticks (e.g., 5 Hz) for AI scanning, pathfinding.</summary>
        Low = 0,

        /// <summary>Medium frequency ticks (e.g., 10 Hz) for AI decision making.</summary>
        Medium = 1,

        /// <summary>Normal frequency ticks (e.g., 30 Hz) for unit movement updates.</summary>
        Normal = 2,

        /// <summary>High frequency ticks (e.g., 60 Hz) for combat, physics-adjacent logic.</summary>
        High = 3
    }
}
