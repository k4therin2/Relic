using UnityEngine;
using UnityEngine.Profiling;
using System;
using System.Collections.Generic;

namespace Relic.CoreRTS
{
    /// <summary>
    /// Centralized tick manager that replaces per-entity Update() calls.
    /// Provides configurable tick rates for different priority levels.
    /// </summary>
    /// <remarks>
    /// Performance optimization for large unit counts (100v100 target).
    /// Instead of each unit having Update(), TickManager batches updates.
    /// Different priorities run at different frequencies (e.g., AI at 10Hz).
    /// </remarks>
    public class TickManager : MonoBehaviour
    {
        #region Constants

        private const float LOW_PRIORITY_INTERVAL = 0.2f;    // 5 Hz
        private const float MEDIUM_PRIORITY_INTERVAL = 0.1f; // 10 Hz
        private const float NORMAL_PRIORITY_INTERVAL = 0.033f; // ~30 Hz
        private const float HIGH_PRIORITY_INTERVAL = 0.0f;   // Every frame (60+ Hz)

        #endregion

        #region Singleton

        private static TickManager _instance;
        private static bool _applicationQuitting;

        /// <summary>
        /// Gets the singleton instance of TickManager.
        /// Creates one if it doesn't exist.
        /// </summary>
        public static TickManager Instance
        {
            get
            {
                if (_applicationQuitting)
                    return null;

                if (_instance == null)
                {
                    _instance = FindObjectOfType<TickManager>();
                    if (_instance == null)
                    {
                        var go = new GameObject("TickManager");
                        _instance = go.AddComponent<TickManager>();
                        DontDestroyOnLoad(go);
                    }
                }
                return _instance;
            }
        }

        /// <summary>
        /// Returns true if a TickManager instance exists without creating one.
        /// </summary>
        public static bool HasInstance => _instance != null;

        #endregion

        #region Serialized Fields

        [Header("Tick Intervals (seconds)")]
        [Tooltip("Interval for Low priority ticks (AI scanning)")]
        [SerializeField] private float _lowPriorityInterval = LOW_PRIORITY_INTERVAL;

        [Tooltip("Interval for Medium priority ticks (AI decisions)")]
        [SerializeField] private float _mediumPriorityInterval = MEDIUM_PRIORITY_INTERVAL;

        [Tooltip("Interval for Normal priority ticks (movement)")]
        [SerializeField] private float _normalPriorityInterval = NORMAL_PRIORITY_INTERVAL;

        [Tooltip("Interval for High priority ticks (combat)")]
        [SerializeField] private float _highPriorityInterval = HIGH_PRIORITY_INTERVAL;

        [Header("Debug")]
        [Tooltip("Enable profiler markers for tick phases")]
        [SerializeField] private bool _enableProfiling = true;

        #endregion

        #region Private Fields

        // Registered tickables by priority
        private readonly List<ITickable> _lowPriorityTickables = new List<ITickable>();
        private readonly List<ITickable> _mediumPriorityTickables = new List<ITickable>();
        private readonly List<ITickable> _normalPriorityTickables = new List<ITickable>();
        private readonly List<ITickable> _highPriorityTickables = new List<ITickable>();

        // Accumulators for tick timing
        private float _lowPriorityAccumulator;
        private float _mediumPriorityAccumulator;
        private float _normalPriorityAccumulator;

        // Pending additions/removals (to avoid modifying during iteration)
        private readonly List<ITickable> _pendingAdditions = new List<ITickable>();
        private readonly List<ITickable> _pendingRemovals = new List<ITickable>();
        private bool _isProcessingTicks;

        // Stats
        private int _totalTickablesProcessed;
        private int _ticksThisFrame;

        #endregion

        #region Properties

        /// <summary>Total number of registered tickables.</summary>
        public int TotalRegistered =>
            _lowPriorityTickables.Count +
            _mediumPriorityTickables.Count +
            _normalPriorityTickables.Count +
            _highPriorityTickables.Count;

        /// <summary>Number of tickables processed last frame.</summary>
        public int LastFrameTickables => _totalTickablesProcessed;

        /// <summary>Number of tick batches processed last frame.</summary>
        public int LastFrameTicks => _ticksThisFrame;

        /// <summary>Low priority tick interval.</summary>
        public float LowPriorityInterval
        {
            get => _lowPriorityInterval;
            set => _lowPriorityInterval = Mathf.Max(0f, value);
        }

        /// <summary>Medium priority tick interval.</summary>
        public float MediumPriorityInterval
        {
            get => _mediumPriorityInterval;
            set => _mediumPriorityInterval = Mathf.Max(0f, value);
        }

        /// <summary>Normal priority tick interval.</summary>
        public float NormalPriorityInterval
        {
            get => _normalPriorityInterval;
            set => _normalPriorityInterval = Mathf.Max(0f, value);
        }

        /// <summary>High priority tick interval.</summary>
        public float HighPriorityInterval
        {
            get => _highPriorityInterval;
            set => _highPriorityInterval = Mathf.Max(0f, value);
        }

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void Update()
        {
            float deltaTime = Time.deltaTime;

            _totalTickablesProcessed = 0;
            _ticksThisFrame = 0;

            // Process pending additions/removals
            ProcessPendingChanges();

            // Accumulate time
            _lowPriorityAccumulator += deltaTime;
            _mediumPriorityAccumulator += deltaTime;
            _normalPriorityAccumulator += deltaTime;

            _isProcessingTicks = true;

            // High priority - every frame
            if (_highPriorityInterval <= 0f || true)
            {
                ProcessTicks(_highPriorityTickables, deltaTime, "TickManager.High");
            }

            // Normal priority
            if (_normalPriorityAccumulator >= _normalPriorityInterval)
            {
                ProcessTicks(_normalPriorityTickables, _normalPriorityAccumulator, "TickManager.Normal");
                _normalPriorityAccumulator = 0f;
            }

            // Medium priority
            if (_mediumPriorityAccumulator >= _mediumPriorityInterval)
            {
                ProcessTicks(_mediumPriorityTickables, _mediumPriorityAccumulator, "TickManager.Medium");
                _mediumPriorityAccumulator = 0f;
            }

            // Low priority
            if (_lowPriorityAccumulator >= _lowPriorityInterval)
            {
                ProcessTicks(_lowPriorityTickables, _lowPriorityAccumulator, "TickManager.Low");
                _lowPriorityAccumulator = 0f;
            }

            _isProcessingTicks = false;

            // Process any changes that happened during ticks
            ProcessPendingChanges();
        }

        private void OnApplicationQuit()
        {
            _applicationQuitting = true;
        }

        private void OnDestroy()
        {
            if (_instance == this)
            {
                _instance = null;
            }
        }

        #endregion

        #region Registration

        /// <summary>
        /// Registers a tickable to receive tick updates.
        /// </summary>
        /// <param name="tickable">The tickable to register.</param>
        public void Register(ITickable tickable)
        {
            if (tickable == null)
                return;

            if (_isProcessingTicks)
            {
                _pendingAdditions.Add(tickable);
                return;
            }

            AddToList(tickable);
        }

        /// <summary>
        /// Unregisters a tickable from receiving tick updates.
        /// </summary>
        /// <param name="tickable">The tickable to unregister.</param>
        public void Unregister(ITickable tickable)
        {
            if (tickable == null)
                return;

            if (_isProcessingTicks)
            {
                _pendingRemovals.Add(tickable);
                return;
            }

            RemoveFromList(tickable);
        }

        /// <summary>
        /// Checks if a tickable is registered.
        /// </summary>
        /// <param name="tickable">The tickable to check.</param>
        /// <returns>True if registered.</returns>
        public bool IsRegistered(ITickable tickable)
        {
            if (tickable == null)
                return false;

            return GetListForPriority(tickable.Priority).Contains(tickable);
        }

        /// <summary>
        /// Gets the count of tickables for a specific priority.
        /// </summary>
        /// <param name="priority">The priority level.</param>
        /// <returns>Number of tickables at that priority.</returns>
        public int GetCount(TickPriority priority)
        {
            return GetListForPriority(priority).Count;
        }

        /// <summary>
        /// Clears all registered tickables.
        /// </summary>
        public void ClearAll()
        {
            _lowPriorityTickables.Clear();
            _mediumPriorityTickables.Clear();
            _normalPriorityTickables.Clear();
            _highPriorityTickables.Clear();
            _pendingAdditions.Clear();
            _pendingRemovals.Clear();
        }

        #endregion

        #region Private Methods

        private void ProcessTicks(List<ITickable> tickables, float deltaTime, string profilerLabel)
        {
            if (tickables.Count == 0)
                return;

            if (_enableProfiling)
            {
                Profiler.BeginSample(profilerLabel);
            }

            _ticksThisFrame++;

            for (int i = tickables.Count - 1; i >= 0; i--)
            {
                var tickable = tickables[i];

                // Check if tickable was destroyed
                if (tickable == null || (tickable is UnityEngine.Object obj && obj == null))
                {
                    tickables.RemoveAt(i);
                    continue;
                }

                // Only tick if active
                if (!tickable.IsTickActive)
                    continue;

                try
                {
                    tickable.OnTick(deltaTime);
                    _totalTickablesProcessed++;
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[TickManager] Error in OnTick for {tickable}: {ex}");
                }
            }

            if (_enableProfiling)
            {
                Profiler.EndSample();
            }
        }

        private void ProcessPendingChanges()
        {
            // Process removals first
            foreach (var tickable in _pendingRemovals)
            {
                RemoveFromList(tickable);
            }
            _pendingRemovals.Clear();

            // Then additions
            foreach (var tickable in _pendingAdditions)
            {
                AddToList(tickable);
            }
            _pendingAdditions.Clear();
        }

        private void AddToList(ITickable tickable)
        {
            var list = GetListForPriority(tickable.Priority);
            if (!list.Contains(tickable))
            {
                list.Add(tickable);
            }
        }

        private void RemoveFromList(ITickable tickable)
        {
            GetListForPriority(tickable.Priority).Remove(tickable);
        }

        private List<ITickable> GetListForPriority(TickPriority priority)
        {
            return priority switch
            {
                TickPriority.Low => _lowPriorityTickables,
                TickPriority.Medium => _mediumPriorityTickables,
                TickPriority.Normal => _normalPriorityTickables,
                TickPriority.High => _highPriorityTickables,
                _ => _normalPriorityTickables
            };
        }

        #endregion

        #region Editor/Debug

#if UNITY_EDITOR
        private void OnValidate()
        {
            _lowPriorityInterval = Mathf.Max(0f, _lowPriorityInterval);
            _mediumPriorityInterval = Mathf.Max(0f, _mediumPriorityInterval);
            _normalPriorityInterval = Mathf.Max(0f, _normalPriorityInterval);
            _highPriorityInterval = Mathf.Max(0f, _highPriorityInterval);
        }
#endif

        #endregion
    }
}
