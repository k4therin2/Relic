using UnityEngine;
using System.Collections.Generic;

namespace Relic.CoreRTS
{
    /// <summary>
    /// Manages destination markers for move/attack commands.
    /// Uses object pooling for efficiency.
    /// </summary>
    /// <remarks>
    /// Part of WP-EXT-5.1: AR UX Enhancements.
    /// Singleton - access via DestinationMarkerManager.Instance.
    /// Integrates with SelectionManager to show markers for selected units.
    /// </remarks>
    public class DestinationMarkerManager : MonoBehaviour
    {
        #region Singleton

        private static DestinationMarkerManager _instance;

        /// <summary>
        /// The singleton instance of DestinationMarkerManager.
        /// </summary>
        public static DestinationMarkerManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindFirstObjectByType<DestinationMarkerManager>();
                    if (_instance == null)
                    {
                        var go = new GameObject("DestinationMarkerManager");
                        _instance = go.AddComponent<DestinationMarkerManager>();
                    }
                }
                return _instance;
            }
        }

        #endregion

        #region Serialized Fields

        [Header("Colors")]
        [Tooltip("Color for move command markers")]
        [SerializeField] private Color _moveMarkerColor = new Color(0.2f, 0.9f, 0.3f, 0.7f);

        [Tooltip("Color for attack command markers")]
        [SerializeField] private Color _attackMarkerColor = new Color(0.9f, 0.2f, 0.2f, 0.7f);

        [Tooltip("Color for rally point markers")]
        [SerializeField] private Color _rallyMarkerColor = new Color(0.9f, 0.8f, 0.2f, 0.7f);

        [Header("Timing")]
        [Tooltip("Default lifetime for markers in seconds")]
        [SerializeField] private float _defaultLifetime = 2f;

        [Header("Pooling")]
        [Tooltip("Initial pool size")]
        [SerializeField] private int _initialPoolSize = 10;

        #endregion

        #region Runtime State

        private readonly List<DestinationMarker> _activeMarkers = new List<DestinationMarker>();
        private readonly Queue<DestinationMarker> _markerPool = new Queue<DestinationMarker>();
        private Transform _markerContainer;

        #endregion

        #region Properties

        /// <summary>Gets the number of currently active markers.</summary>
        public int ActiveMarkerCount => _activeMarkers.Count;

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

            // Create container for markers
            _markerContainer = new GameObject("Markers").transform;
            _markerContainer.SetParent(transform);

            // Pre-populate pool
            for (int i = 0; i < _initialPoolSize; i++)
            {
                var marker = CreateMarker();
                marker.Hide();
                _markerPool.Enqueue(marker);
            }
        }

        private void OnDestroy()
        {
            if (_instance == this)
            {
                _instance = null;
            }
        }

        private void Update()
        {
            // Clean up expired markers
            CleanupExpiredMarkers();
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Shows a move command marker at the specified position.
        /// </summary>
        /// <param name="position">World position for the marker.</param>
        /// <param name="lifetime">How long to show (default uses _defaultLifetime).</param>
        /// <returns>The created marker.</returns>
        public DestinationMarker ShowMoveMarker(Vector3 position, float lifetime = -1f)
        {
            if (lifetime < 0f) lifetime = _defaultLifetime;

            var marker = GetOrCreateMarker();
            marker.Initialize(position, MarkerType.Move, _moveMarkerColor, lifetime);
            _activeMarkers.Add(marker);

            return marker;
        }

        /// <summary>
        /// Shows an attack command marker at the specified position.
        /// </summary>
        /// <param name="position">World position for the marker.</param>
        /// <param name="lifetime">How long to show (default uses _defaultLifetime).</param>
        /// <returns>The created marker.</returns>
        public DestinationMarker ShowAttackMarker(Vector3 position, float lifetime = -1f)
        {
            if (lifetime < 0f) lifetime = _defaultLifetime;

            var marker = GetOrCreateMarker();
            marker.Initialize(position, MarkerType.Attack, _attackMarkerColor, lifetime);
            _activeMarkers.Add(marker);

            return marker;
        }

        /// <summary>
        /// Shows a rally point marker at the specified position.
        /// </summary>
        /// <param name="position">World position for the marker.</param>
        /// <param name="lifetime">How long to show (0 for indefinite).</param>
        /// <returns>The created marker.</returns>
        public DestinationMarker ShowRallyMarker(Vector3 position, float lifetime = 0f)
        {
            var marker = GetOrCreateMarker();
            marker.Initialize(position, MarkerType.Rally, _rallyMarkerColor, lifetime);
            _activeMarkers.Add(marker);

            return marker;
        }

        /// <summary>
        /// Shows move markers for a list of units moving to a destination.
        /// </summary>
        /// <param name="units">Units that are moving.</param>
        /// <param name="destination">The destination position.</param>
        /// <param name="lifetime">How long to show markers.</param>
        /// <returns>List of created markers.</returns>
        public List<DestinationMarker> ShowMoveMarkersForUnits(
            IList<UnitController> units,
            Vector3 destination,
            float lifetime = -1f)
        {
            var markers = new List<DestinationMarker>();

            foreach (var unit in units)
            {
                if (unit == null) continue;

                // For now, all units get markers at the same position
                // Future: could offset markers in formation
                var marker = ShowMoveMarker(destination, lifetime);
                markers.Add(marker);
            }

            return markers;
        }

        /// <summary>
        /// Shows a move marker for the currently selected units.
        /// </summary>
        /// <param name="destination">The destination position.</param>
        /// <param name="lifetime">How long to show the marker.</param>
        /// <returns>The created marker (or null if no selection).</returns>
        public DestinationMarker ShowMoveMarkerForSelection(Vector3 destination, float lifetime = -1f)
        {
            var selectionManager = SelectionManager.Instance;
            if (selectionManager == null || !selectionManager.HasSelection)
            {
                return null;
            }

            // Show single marker at destination (not per-unit)
            return ShowMoveMarker(destination, lifetime);
        }

        /// <summary>
        /// Clears all active markers.
        /// </summary>
        public void ClearAllMarkers()
        {
            foreach (var marker in _activeMarkers)
            {
                if (marker != null)
                {
                    marker.Hide();
                    _markerPool.Enqueue(marker);
                }
            }
            _activeMarkers.Clear();
        }

        /// <summary>
        /// Returns a marker to the pool.
        /// </summary>
        /// <param name="marker">The marker to return.</param>
        public void ReturnToPool(DestinationMarker marker)
        {
            if (marker == null) return;

            if (_activeMarkers.Contains(marker))
            {
                _activeMarkers.Remove(marker);
            }

            marker.Hide();
            _markerPool.Enqueue(marker);
        }

        #endregion

        #region Private Methods

        private DestinationMarker GetOrCreateMarker()
        {
            if (_markerPool.Count > 0)
            {
                return _markerPool.Dequeue();
            }

            return CreateMarker();
        }

        private DestinationMarker CreateMarker()
        {
            var markerGO = new GameObject("DestinationMarker");
            markerGO.transform.SetParent(_markerContainer);
            return markerGO.AddComponent<DestinationMarker>();
        }

        private void CleanupExpiredMarkers()
        {
            for (int i = _activeMarkers.Count - 1; i >= 0; i--)
            {
                var marker = _activeMarkers[i];
                if (marker == null)
                {
                    _activeMarkers.RemoveAt(i);
                    continue;
                }

                if (!marker.IsActive)
                {
                    _activeMarkers.RemoveAt(i);
                    _markerPool.Enqueue(marker);
                }
            }
        }

        #endregion
    }
}
