using UnityEngine;
using System.Collections.Generic;

namespace Relic.CoreRTS
{
    /// <summary>
    /// Handles mouse input for unit selection in debug/desktop mode.
    /// Supports click-to-select, shift+click for add-to-selection,
    /// and drag rectangle for multi-select.
    /// </summary>
    /// <remarks>
    /// Attach to a camera or empty game object in the debug scene.
    /// Requires SelectionManager to be present in the scene.
    /// See Kyle's milestones.md Milestone 2 for requirements.
    /// </remarks>
    public class DebugSelectionController : MonoBehaviour
    {
        #region Serialized Fields

        [Header("Raycast Settings")]
        [Tooltip("Camera to use for raycasting (defaults to main camera)")]
        [SerializeField] private Camera _camera;

        [Tooltip("Layer mask for unit selection")]
        [SerializeField] private LayerMask _unitLayerMask = ~0;

        [Tooltip("Layer mask for ground/battlefield")]
        [SerializeField] private LayerMask _groundLayerMask = ~0;

        [Tooltip("Maximum raycast distance")]
        [SerializeField] private float _maxRaycastDistance = 100f;

        [Header("Drag Selection")]
        [Tooltip("Minimum drag distance to trigger box selection (in pixels)")]
        [SerializeField] private float _minDragDistance = 10f;

        [Tooltip("Color of the selection box")]
        [SerializeField] private Color _selectionBoxColor = new Color(0.2f, 0.6f, 1f, 0.3f);

        [Tooltip("Border color of the selection box")]
        [SerializeField] private Color _selectionBoxBorderColor = new Color(0.2f, 0.6f, 1f, 0.8f);

        #endregion

        #region Runtime State

        private SelectionManager _selectionManager;
        private bool _isDragging;
        private Vector3 _dragStartPosition;
        private Texture2D _boxTexture;
        private Texture2D _borderTexture;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            if (_camera == null)
            {
                _camera = Camera.main;
            }

            CreateBoxTextures();
        }

        private void Start()
        {
            _selectionManager = SelectionManager.Instance;
            if (_selectionManager == null)
            {
                Debug.LogWarning("[DebugSelectionController] SelectionManager not found. Selection will not work.");
            }
        }

        private void Update()
        {
            if (_selectionManager == null || _camera == null) return;

            HandleMouseInput();
        }

        private void OnGUI()
        {
            if (_isDragging)
            {
                DrawSelectionBox();
            }
        }

        private void OnDestroy()
        {
            if (_boxTexture != null)
            {
                Destroy(_boxTexture);
            }
            if (_borderTexture != null)
            {
                Destroy(_borderTexture);
            }
        }

        #endregion

        #region Input Handling

        private void HandleMouseInput()
        {
            // Left mouse button down - start potential drag
            if (Input.GetMouseButtonDown(0))
            {
                _dragStartPosition = Input.mousePosition;
                _isDragging = false;
            }

            // Left mouse button held - check for drag
            if (Input.GetMouseButton(0))
            {
                float dragDistance = Vector3.Distance(_dragStartPosition, Input.mousePosition);
                if (dragDistance > _minDragDistance)
                {
                    _isDragging = true;
                }
            }

            // Left mouse button up - complete selection
            if (Input.GetMouseButtonUp(0))
            {
                bool isAdditive = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);

                if (_isDragging)
                {
                    // Box selection
                    PerformBoxSelection(isAdditive);
                }
                else
                {
                    // Click selection
                    PerformClickSelection(isAdditive);
                }

                _isDragging = false;
            }

            // Right mouse button - move command
            if (Input.GetMouseButtonDown(1))
            {
                PerformMoveCommand();
            }
        }

        #endregion

        #region Click Selection

        private void PerformClickSelection(bool addToSelection)
        {
            Ray ray = _camera.ScreenPointToRay(Input.mousePosition);

            if (Physics.Raycast(ray, out RaycastHit hit, _maxRaycastDistance, _unitLayerMask))
            {
                var unit = hit.collider.GetComponent<UnitController>();
                if (unit == null)
                {
                    unit = hit.collider.GetComponentInParent<UnitController>();
                }

                if (unit != null)
                {
                    if (addToSelection)
                    {
                        _selectionManager.ToggleSelection(unit);
                    }
                    else
                    {
                        _selectionManager.SelectUnit(unit);
                    }
                    return;
                }
            }

            // Clicked on nothing - clear selection (unless additive)
            if (!addToSelection)
            {
                _selectionManager.ClearSelection();
            }
        }

        #endregion

        #region Box Selection

        private void PerformBoxSelection(bool addToSelection)
        {
            Vector3 currentMousePos = Input.mousePosition;

            // Calculate selection rect in screen space
            Rect selectionRect = GetSelectionRect(_dragStartPosition, currentMousePos);

            // Find all units in the box
            var unitsInBox = new List<UnitController>();
            var allUnits = FindObjectsByType<UnitController>(FindObjectsSortMode.None);

            foreach (var unit in allUnits)
            {
                if (unit == null || !unit.IsAlive) continue;

                // Convert unit position to screen space
                Vector3 screenPos = _camera.WorldToScreenPoint(unit.transform.position);

                // Check if behind camera
                if (screenPos.z < 0) continue;

                // Check if inside selection rect
                if (selectionRect.Contains(new Vector2(screenPos.x, screenPos.y)))
                {
                    unitsInBox.Add(unit);
                }
            }

            if (unitsInBox.Count > 0)
            {
                _selectionManager.SelectUnits(unitsInBox, addToSelection);
            }
            else if (!addToSelection)
            {
                _selectionManager.ClearSelection();
            }
        }

        private Rect GetSelectionRect(Vector3 start, Vector3 end)
        {
            // Create rect from two corners, handling any direction of drag
            float x = Mathf.Min(start.x, end.x);
            float y = Mathf.Min(start.y, end.y);
            float width = Mathf.Abs(start.x - end.x);
            float height = Mathf.Abs(start.y - end.y);

            return new Rect(x, y, width, height);
        }

        #endregion

        #region Move Command

        private void PerformMoveCommand()
        {
            if (!_selectionManager.HasSelection) return;

            Ray ray = _camera.ScreenPointToRay(Input.mousePosition);

            if (Physics.Raycast(ray, out RaycastHit hit, _maxRaycastDistance, _groundLayerMask))
            {
                _selectionManager.CommandSelectedToMove(hit.point);
            }
        }

        #endregion

        #region Selection Box UI

        private void CreateBoxTextures()
        {
            // Create box fill texture
            _boxTexture = new Texture2D(1, 1);
            _boxTexture.SetPixel(0, 0, Color.white);
            _boxTexture.Apply();

            // Create border texture
            _borderTexture = new Texture2D(1, 1);
            _borderTexture.SetPixel(0, 0, Color.white);
            _borderTexture.Apply();
        }

        private void DrawSelectionBox()
        {
            Vector3 currentMousePos = Input.mousePosition;

            // Convert to GUI coordinates (Y is flipped)
            float startY = Screen.height - _dragStartPosition.y;
            float endY = Screen.height - currentMousePos.y;

            float x = Mathf.Min(_dragStartPosition.x, currentMousePos.x);
            float y = Mathf.Min(startY, endY);
            float width = Mathf.Abs(_dragStartPosition.x - currentMousePos.x);
            float height = Mathf.Abs(startY - endY);

            Rect boxRect = new Rect(x, y, width, height);

            // Draw fill
            GUI.color = _selectionBoxColor;
            GUI.DrawTexture(boxRect, _boxTexture);

            // Draw border
            GUI.color = _selectionBoxBorderColor;
            float borderWidth = 2f;

            // Top
            GUI.DrawTexture(new Rect(x, y, width, borderWidth), _borderTexture);
            // Bottom
            GUI.DrawTexture(new Rect(x, y + height - borderWidth, width, borderWidth), _borderTexture);
            // Left
            GUI.DrawTexture(new Rect(x, y, borderWidth, height), _borderTexture);
            // Right
            GUI.DrawTexture(new Rect(x + width - borderWidth, y, borderWidth, height), _borderTexture);

            GUI.color = Color.white;
        }

        #endregion
    }
}
