using UnityEngine;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit;

namespace Relic.CoreRTS
{
    /// <summary>
    /// Handles XR controller input for unit selection in AR mode.
    /// Supports trigger-to-select, grip for add-to-selection,
    /// and raycast to battlefield for move commands.
    /// </summary>
    /// <remarks>
    /// Attach to the XR controller in the AR scene.
    /// Requires SelectionManager to be present in the scene.
    /// See Kyle's milestones.md Milestone 2 for requirements.
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

        #endregion

        #region Runtime State

        private SelectionManager _selectionManager;
        private bool _triggerWasPressed;
        private bool _gripIsPressed;
        private InputDevice _controller;

        #endregion

        #region Unity Lifecycle

        private void Start()
        {
            _selectionManager = SelectionManager.Instance;
            if (_selectionManager == null)
            {
                Debug.LogWarning("[ARSelectionController] SelectionManager not found.");
            }

            InitializeController();
        }

        private void Update()
        {
            if (_selectionManager == null) return;

            UpdateControllerState();
            HandleControllerInput();
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

            // Trigger pressed - select
            if (triggerPressed && !_triggerWasPressed)
            {
                PerformSelection();
            }

            _triggerWasPressed = triggerPressed;

            // Secondary button (B/Y) - move command
            bool secondaryPressed;
            if (_controller.TryGetFeatureValue(CommonUsages.secondaryButton, out secondaryPressed) && secondaryPressed)
            {
                PerformMoveCommand();
            }
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
