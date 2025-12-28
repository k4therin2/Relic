using UnityEngine;
using System.Collections.Generic;

namespace Relic.CoreRTS
{
    /// <summary>
    /// Handles drag/box selection for multi-unit selection in AR mode.
    /// Creates a selection box from start to current point and selects all units within.
    /// </summary>
    /// <remarks>
    /// Part of WP-EXT-5.1: AR UX Enhancements.
    /// Works alongside ARSelectionController for enhanced selection capabilities.
    /// Use StartSelection, UpdateSelection, and EndSelection to drive the selection.
    /// </remarks>
    public class ARBoxSelection : MonoBehaviour
    {
        #region Serialized Fields

        [Header("Selection Settings")]
        [Tooltip("Minimum drag distance to count as a box selection (prevents accidental clicks)")]
        [SerializeField] private float _minimumSelectionSize = 0.5f;

        [Tooltip("Height of the selection box for 3D physics overlap")]
        [SerializeField] private float _selectionHeight = 10f;

        [Tooltip("Layer mask for selectable units")]
        [SerializeField] private LayerMask _unitLayerMask = ~0;

        [Header("Visual")]
        [Tooltip("Color of the selection box visual")]
        [SerializeField] private Color _selectionColor = new Color(0.2f, 0.8f, 0.2f, 0.3f);

        [Tooltip("Color of the selection box border")]
        [SerializeField] private Color _borderColor = new Color(0.2f, 0.8f, 0.2f, 0.8f);

        #endregion

        #region Runtime State

        private Vector3 _startPoint;
        private Vector3 _currentPoint;
        private bool _isSelecting;
        private bool _addToSelection;
        private int _teamFilter = -1; // -1 means no filter
        private GameObject _selectionVisual;
        private LineRenderer _borderRenderer;
        private SelectionManager _selectionManager;

        #endregion

        #region Properties

        /// <summary>Gets whether a selection drag is in progress.</summary>
        public bool IsSelecting => _isSelecting;

        /// <summary>Gets the start point of the selection box.</summary>
        public Vector3 StartPoint => _startPoint;

        /// <summary>Gets the current point of the selection box.</summary>
        public Vector3 CurrentPoint => _currentPoint;

        /// <summary>Gets whether the selection visual is active.</summary>
        public bool IsVisualActive => _selectionVisual != null && _selectionVisual.activeInHierarchy;

        #endregion

        #region Unity Lifecycle

        private void Start()
        {
            _selectionManager = SelectionManager.Instance;
            CreateSelectionVisual();
            HideVisual();
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Starts a new selection box at the given point.
        /// </summary>
        /// <param name="startPoint">World position where selection starts.</param>
        public void StartSelection(Vector3 startPoint)
        {
            _startPoint = startPoint;
            _currentPoint = startPoint;
            _isSelecting = true;

            ShowVisual();
            UpdateVisual();
        }

        /// <summary>
        /// Updates the selection box to the current point.
        /// </summary>
        /// <param name="currentPoint">Current world position of the drag.</param>
        public void UpdateSelection(Vector3 currentPoint)
        {
            _currentPoint = currentPoint;
            UpdateVisual();
        }

        /// <summary>
        /// Ends the selection and optionally applies it.
        /// </summary>
        /// <param name="apply">If true, applies the selection to SelectionManager.</param>
        public void EndSelection(bool apply = true)
        {
            _isSelecting = false;
            HideVisual();

            if (apply && IsValidSelection())
            {
                ApplySelection();
            }
        }

        /// <summary>
        /// Gets the bounds of the current selection box.
        /// </summary>
        /// <returns>Bounds representing the selection area.</returns>
        public Bounds GetSelectionBounds()
        {
            Vector3 min = Vector3.Min(_startPoint, _currentPoint);
            Vector3 max = Vector3.Max(_startPoint, _currentPoint);

            // Add height for 3D volume
            min.y -= _selectionHeight / 2f;
            max.y += _selectionHeight / 2f;

            Vector3 center = (min + max) / 2f;
            Vector3 size = max - min;

            return new Bounds(center, size);
        }

        /// <summary>
        /// Gets all units within the current selection bounds.
        /// </summary>
        /// <returns>List of UnitControllers in the selection.</returns>
        public List<UnitController> GetUnitsInSelection()
        {
            var result = new List<UnitController>();

            if (!IsValidSelection())
            {
                return result;
            }

            Bounds bounds = GetSelectionBounds();

            // Use Physics.OverlapBox to find all colliders in the selection
            Collider[] colliders = Physics.OverlapBox(
                bounds.center,
                bounds.extents,
                Quaternion.identity,
                _unitLayerMask
            );

            foreach (var collider in colliders)
            {
                var unit = collider.GetComponent<UnitController>();
                if (unit == null)
                {
                    unit = collider.GetComponentInParent<UnitController>();
                }

                if (unit != null && unit.IsAlive)
                {
                    // Apply team filter if set
                    if (_teamFilter >= 0 && unit.TeamId != _teamFilter)
                    {
                        continue;
                    }

                    // Avoid duplicates
                    if (!result.Contains(unit))
                    {
                        result.Add(unit);
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// Applies the current selection to the SelectionManager.
        /// </summary>
        public void ApplySelection()
        {
            if (_selectionManager == null)
            {
                _selectionManager = SelectionManager.Instance;
            }

            if (_selectionManager == null)
            {
                Debug.LogWarning("[ARBoxSelection] No SelectionManager found.");
                return;
            }

            var units = GetUnitsInSelection();

            if (units.Count > 0)
            {
                _selectionManager.SelectUnits(units, _addToSelection);
            }
            else if (!_addToSelection)
            {
                // Empty selection clears (unless in add mode)
                _selectionManager.ClearSelection();
            }
        }

        /// <summary>
        /// Checks if the current selection is large enough to be valid.
        /// </summary>
        /// <returns>True if the selection is valid.</returns>
        public bool IsValidSelection()
        {
            Vector3 diff = _currentPoint - _startPoint;
            diff.y = 0; // Ignore height difference

            return diff.magnitude >= _minimumSelectionSize;
        }

        /// <summary>
        /// Sets whether new selections should add to existing selection.
        /// </summary>
        /// <param name="addToSelection">True to add, false to replace.</param>
        public void SetAddToSelection(bool addToSelection)
        {
            _addToSelection = addToSelection;
        }

        /// <summary>
        /// Sets the team filter for selection (-1 for no filter).
        /// </summary>
        /// <param name="teamId">Team ID to filter to, or -1 for all teams.</param>
        public void SetTeamFilter(int teamId)
        {
            _teamFilter = teamId;
        }

        /// <summary>
        /// Gets the bounds of the selection visual.
        /// </summary>
        /// <returns>Bounds of the visual representation.</returns>
        public Bounds GetVisualBounds()
        {
            if (_selectionVisual == null)
            {
                return new Bounds();
            }

            var renderer = _selectionVisual.GetComponent<Renderer>();
            if (renderer != null)
            {
                return renderer.bounds;
            }

            return GetSelectionBounds();
        }

        #endregion

        #region Visual Methods

        private void CreateSelectionVisual()
        {
            // Create a simple quad for the selection area
            _selectionVisual = GameObject.CreatePrimitive(PrimitiveType.Quad);
            _selectionVisual.name = "SelectionBoxVisual";
            _selectionVisual.transform.SetParent(transform);
            _selectionVisual.transform.rotation = Quaternion.Euler(90, 0, 0); // Face up

            // Remove collider
            var collider = _selectionVisual.GetComponent<Collider>();
            if (collider != null)
            {
                SafeDestroy(collider);
            }

            // Setup material
            var renderer = _selectionVisual.GetComponent<Renderer>();
            if (renderer != null)
            {
                var material = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
                if (material.shader == null || material.shader.name == "Hidden/InternalErrorShader")
                {
                    material = new Material(Shader.Find("Unlit/Color"));
                }

                // Make transparent
                material.SetFloat("_Surface", 1);
                material.SetFloat("_Blend", 0);
                material.renderQueue = 3000;
                material.color = _selectionColor;

                renderer.material = material;
            }

            // Create border line renderer
            var borderGO = new GameObject("SelectionBorder");
            borderGO.transform.SetParent(_selectionVisual.transform);
            _borderRenderer = borderGO.AddComponent<LineRenderer>();
            _borderRenderer.positionCount = 5;
            _borderRenderer.loop = true;
            _borderRenderer.startWidth = 0.02f;
            _borderRenderer.endWidth = 0.02f;
            _borderRenderer.material = new Material(Shader.Find("Sprites/Default"));
            _borderRenderer.startColor = _borderColor;
            _borderRenderer.endColor = _borderColor;

            _selectionVisual.SetActive(false);
        }

        private void ShowVisual()
        {
            if (_selectionVisual != null)
            {
                _selectionVisual.SetActive(true);
            }
        }

        private void HideVisual()
        {
            if (_selectionVisual != null)
            {
                _selectionVisual.SetActive(false);
            }
        }

        private void UpdateVisual()
        {
            if (_selectionVisual == null) return;

            // Calculate box dimensions
            Vector3 min = Vector3.Min(_startPoint, _currentPoint);
            Vector3 max = Vector3.Max(_startPoint, _currentPoint);
            Vector3 center = (min + max) / 2f;
            Vector3 size = max - min;

            // Position and scale the quad
            _selectionVisual.transform.position = new Vector3(center.x, 0.05f, center.z);
            _selectionVisual.transform.localScale = new Vector3(size.x, size.z, 1f);

            // Update border
            if (_borderRenderer != null)
            {
                float halfWidth = size.x / 2f;
                float halfHeight = size.z / 2f;
                _borderRenderer.SetPositions(new Vector3[]
                {
                    new Vector3(-halfWidth, 0, -halfHeight),
                    new Vector3(halfWidth, 0, -halfHeight),
                    new Vector3(halfWidth, 0, halfHeight),
                    new Vector3(-halfWidth, 0, halfHeight),
                    new Vector3(-halfWidth, 0, -halfHeight)
                });
            }
        }

        /// <summary>
        /// Safely destroys an object in both Editor and Play mode.
        /// </summary>
        private void SafeDestroy(Object objectToDestroy)
        {
            if (objectToDestroy == null) return;

#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                DestroyImmediate(objectToDestroy);
                return;
            }
#endif
            Destroy(objectToDestroy);
        }

        #endregion
    }
}
