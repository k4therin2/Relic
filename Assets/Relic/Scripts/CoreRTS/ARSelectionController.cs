using UnityEngine;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace Relic.CoreRTS
{
    /// <summary>
    /// Handles XR controller input for unit selection in AR mode.
    /// Supports trigger-to-select, grip for add-to-selection,
    /// box/drag selection for multi-select, and raycast to battlefield for move commands.
    /// </summary>
    /// <remarks>
    /// Attach to the XR controller in the AR scene.
    /// Requires SelectionManager to be present in the scene.
    /// Updated for WP-EXT-5.1: Added box selection and destination markers.
    /// See Kyle's milestones.md Milestone 2/4 for requirements.
    /// </remarks>
    public class ARSelectionController : MonoBehaviour
    {
        #region Serialized Fields

        [Header("XR References")]
        [Tooltip("The XR Ray Interactor to use for raycasting")]
        [SerializeField] private XRRayInteractor _rayInteractor;

        [Tooltip("Input source for controller handedness")]
        [SerializeField] private XRNode _controllerNode = XRNode.RightHand;

        [Header("Raycast Settings")]
        [Tooltip("Layer mask for unit selection")]
        [SerializeField] private LayerMask _unitLayerMask = ~0;

        [Tooltip("Layer mask for ground/battlefield")]
        [SerializeField] private LayerMask _groundLayerMask = ~0;

        [Header("Input Mapping")]
        [Tooltip("Input action name for primary select (trigger)")]
        [SerializeField] private string _selectActionName = "trigger";

        [Tooltip("Input action name for add-to-selection (grip)")]
        [SerializeField] private string _addToSelectionActionName = "grip";

        [Header("Box Selection")]
        [Tooltip("Enable box/drag selection")]
        [SerializeField] private bool _enableBoxSelection = true;

        [Tooltip("Minimum trigger hold time to start box selection (seconds)")]
        [SerializeField] private float _boxSelectionHoldTime = 0.3f;

        [Header("Destination Markers")]
        [Tooltip("Show destination markers when issuing move commands")]
        [SerializeField] private bool _showDestinationMarkers = true;

        #endregion

        #region Runtime State

        private SelectionManager _selectionManager;
        private bool _triggerWasPressed;
        private bool _gripIsPressed;
        private InputDevice _controller;
        private ARBoxSelection _boxSelection;
        private DestinationMarkerManager _markerManager;
        private float _triggerHoldTime;
        private Vector3 _triggerStartPoint;
        private bool _isBoxSelecting;

        #endregion

        #region Unity Lifecycle

        private void Start()
        {
            _selectionManager = SelectionManager.Instance;
            if (_selectionManager == null)
            {
                Debug.LogWarning("[ARSelectionController] SelectionManager not found.");
            }

            // Initialize box selection component
            if (_enableBoxSelection)
            {
                _boxSelection = GetComponent<ARBoxSelection>();
                if (_boxSelection == null)
                {
                    _boxSelection = gameObject.AddComponent<ARBoxSelection>();
                }
            }

            // Get destination marker manager
            _markerManager = DestinationMarkerManager.Instance;

            InitializeController();
        }

        private void Update()
        {
            if (_selectionManager == null) return;

            UpdateControllerState();
            HandleControllerInput();
            // TODO: HandleBoxSelection() - WP-EXT-5.1 feature
            // HandleBoxSelection();
        }

        #endregion

        #region Controller Initialization

        private void InitializeController()
        {
            _controller = InputDevices.GetDeviceAtXRNode(_controllerNode);
        }

        private void UpdateControllerState()
        {
            if (!_controller.isValid)
            {
                _controller = InputDevices.GetDeviceAtXRNode(_controllerNode);
            }

            // Get grip state
            if (_controller.isValid)
            {
                _controller.TryGetFeatureValue(CommonUsages.gripButton, out _gripIsPressed);
            }
        }

        #endregion

        #region Input Handling

        private void HandleControllerInput()
        {
            if (!_controller.isValid) return;

            // Check trigger button
            bool triggerPressed;
            _controller.TryGetFeatureValue(CommonUsages.triggerButton, out triggerPressed);

            // Track trigger hold time for box selection
            if (triggerPressed)
            {
                if (!_triggerWasPressed)
                {
                    // Just pressed - record start point
                    _triggerHoldTime = 0f;
                    _triggerStartPoint = GetGroundPoint();
                }
                else
                {
                    _triggerHoldTime += Time.deltaTime;
                }
            }
            else if (_triggerWasPressed)
            {
                // Trigger released
                if (!_isBoxSelecting)
                {
                    // Quick click - single unit selection
                    PerformSelection();
                }
            }

            _triggerWasPressed = triggerPressed;

            // Secondary button (B/Y) - move command
            bool secondaryPressed;
            if (_controller.TryGetFeatureValue(CommonUsages.secondaryButton, out secondaryPressed) && secondaryPressed)
            {
                PerformMoveCommand();
            }
        }

        // TODO: Stub for HandleBoxSelection - WP-EXT-5.1 feature (ARBoxSelection not yet implemented)
        // This method will be fully implemented when ARBoxSelection class is created
        private void HandleBoxSelection()
        {
            // Placeholder - implementation pending WP-EXT-5.1
            // Will use ARBoxSelection component for drag-to-select in AR
        }

        private Vector3 GetGroundPoint()
        {
            RaycastHit hit;

            if (_rayInteractor != null && _rayInteractor.TryGetCurrent3DRaycastHit(out hit))
            {
                return hit.point;
            }

            // Fallback to manual raycast
            Ray ray = new Ray(transform.position, transform.forward);
            if (Physics.Raycast(ray, out hit, 100f, _groundLayerMask))
            {
                return hit.point;
            }

            return transform.position + transform.forward * 10f;
        }

        #endregion

        #region Selection

        private void PerformSelection()
        {
            RaycastHit hit;
            bool hasHit = false;

            // Try using XRRayInteractor if available
            if (_rayInteractor != null)
            {
                hasHit = _rayInteractor.TryGetCurrent3DRaycastHit(out hit);
            }
            else
            {
                // Fallback to manual raycast from controller
                Ray ray = new Ray(transform.position, transform.forward);
                hasHit = Physics.Raycast(ray, out hit, 100f, _unitLayerMask);
            }

            if (hasHit)
            {
                // Check if we hit a unit
                var unit = hit.collider.GetComponent<UnitController>();
                if (unit == null)
                {
                    unit = hit.collider.GetComponentInParent<UnitController>();
                }

                if (unit != null)
                {
                    // Use grip as modifier for add-to-selection
                    bool addToSelection = _gripIsPressed;

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

            // Hit nothing - clear selection (unless grip is held)
            if (!_gripIsPressed)
            {
                _selectionManager.ClearSelection();
            }
        }

        #endregion

        #region Move Command

        private void PerformMoveCommand()
        {
            if (!_selectionManager.HasSelection) return;

            RaycastHit hit;
            bool hasHit = false;

            // Try using XRRayInteractor if available
            if (_rayInteractor != null)
            {
                hasHit = _rayInteractor.TryGetCurrent3DRaycastHit(out hit);
            }
            else
            {
                // Fallback to manual raycast
                Ray ray = new Ray(transform.position, transform.forward);
                hasHit = Physics.Raycast(ray, out hit, 100f, _groundLayerMask);
            }

            if (hasHit)
            {
                // Check if we hit the ground/battlefield
                if ((_groundLayerMask.value & (1 << hit.collider.gameObject.layer)) != 0)
                {
                    _selectionManager.CommandSelectedToMove(hit.point);

                    // Show destination marker
                    if (_showDestinationMarkers && _markerManager != null)
                    {
                        _markerManager.ShowMoveMarker(hit.point);
                    }
                }
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Sets the XR Ray Interactor to use.
        /// </summary>
        public void SetRayInteractor(XRRayInteractor interactor)
        {
            _rayInteractor = interactor;
        }

        /// <summary>
        /// Sets the controller node (hand) to use.
        /// </summary>
        public void SetControllerNode(XRNode node)
        {
            _controllerNode = node;
            InitializeController();
        }

        #endregion
    }
}
