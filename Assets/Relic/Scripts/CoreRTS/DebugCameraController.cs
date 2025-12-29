using UnityEngine;

namespace Relic.CoreRTS
{
    /// <summary>
    /// RTS-style camera controller for flat debug scene.
    /// Provides WASD/arrow key panning, scroll wheel zoom, and camera bounds.
    /// </summary>
    /// <remarks>
    /// Attach to the main camera in the Flat_Debug scene.
    /// Works independently of AR systems.
    /// See WP-EXT-6.1 for requirements.
    /// </remarks>
    public class DebugCameraController : MonoBehaviour
    {
        #region Serialized Fields

        [Header("Movement Settings")]
        [Tooltip("Speed of camera panning (units per second)")]
        [SerializeField] private float _panSpeed = 20f;

        [Tooltip("Speed multiplier when holding shift")]
        [SerializeField] private float _fastPanMultiplier = 2f;

        [Tooltip("Enable edge panning (move camera when mouse at screen edge)")]
        [SerializeField] private bool _enableEdgePan = false;

        [Tooltip("Edge pan trigger distance from screen edge (pixels)")]
        [SerializeField] private float _edgePanBorderSize = 10f;

        [Header("Zoom Settings")]
        [Tooltip("Zoom speed (scroll sensitivity)")]
        [SerializeField] private float _zoomSpeed = 5f;

        [Tooltip("Minimum camera height (closest zoom)")]
        [SerializeField] private float _minHeight = 5f;

        [Tooltip("Maximum camera height (furthest zoom)")]
        [SerializeField] private float _maxHeight = 50f;

        [Tooltip("Smooth zoom interpolation speed")]
        [SerializeField] private float _zoomSmoothTime = 0.1f;

        [Header("Camera Bounds")]
        [Tooltip("Enable camera bounds to prevent going off battlefield")]
        [SerializeField] private bool _enableBounds = true;

        [Tooltip("Minimum X position")]
        [SerializeField] private float _minX = -50f;

        [Tooltip("Maximum X position")]
        [SerializeField] private float _maxX = 50f;

        [Tooltip("Minimum Z position")]
        [SerializeField] private float _minZ = -50f;

        [Tooltip("Maximum Z position")]
        [SerializeField] private float _maxZ = 50f;

        [Header("Middle Mouse Pan")]
        [Tooltip("Enable middle mouse button drag to pan")]
        [SerializeField] private bool _enableMiddleMousePan = true;

        [Tooltip("Middle mouse pan sensitivity")]
        [SerializeField] private float _middleMousePanSensitivity = 0.5f;

        #endregion

        #region Runtime State

        private float _targetHeight;
        private float _zoomVelocity;
        private Vector3 _lastMousePosition;
        private bool _isMiddleMouseDragging;

        #endregion

        #region Properties

        /// <summary>
        /// Current camera pan speed.
        /// </summary>
        public float PanSpeed
        {
            get => _panSpeed;
            set => _panSpeed = Mathf.Max(0, value);
        }

        /// <summary>
        /// Current camera zoom speed.
        /// </summary>
        public float ZoomSpeed
        {
            get => _zoomSpeed;
            set => _zoomSpeed = Mathf.Max(0, value);
        }

        /// <summary>
        /// Minimum camera height (zoom in limit).
        /// </summary>
        public float MinHeight => _minHeight;

        /// <summary>
        /// Maximum camera height (zoom out limit).
        /// </summary>
        public float MaxHeight => _maxHeight;

        /// <summary>
        /// Whether bounds checking is enabled.
        /// </summary>
        public bool BoundsEnabled
        {
            get => _enableBounds;
            set => _enableBounds = value;
        }

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            // Initialize target height to current position
            _targetHeight = transform.position.y;
            _targetHeight = Mathf.Clamp(_targetHeight, _minHeight, _maxHeight);
        }

        private void Update()
        {
            HandleKeyboardInput();
            HandleMouseInput();
            ApplyZoom();

            if (_enableBounds)
            {
                ClampToBounds();
            }
        }

        #endregion

        #region Keyboard Input

        private void HandleKeyboardInput()
        {
            Vector3 moveDirection = Vector3.zero;

            // WASD keys
            if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow))
            {
                moveDirection += Vector3.forward;
            }
            if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow))
            {
                moveDirection += Vector3.back;
            }
            if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow))
            {
                moveDirection += Vector3.left;
            }
            if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow))
            {
                moveDirection += Vector3.right;
            }

            // Q/E for zoom (alternative to scroll wheel)
            if (Input.GetKey(KeyCode.Q))
            {
                _targetHeight -= _zoomSpeed * Time.deltaTime * 5f;
            }
            if (Input.GetKey(KeyCode.E))
            {
                _targetHeight += _zoomSpeed * Time.deltaTime * 5f;
            }

            // Apply movement
            if (moveDirection != Vector3.zero)
            {
                float speed = _panSpeed;
                if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
                {
                    speed *= _fastPanMultiplier;
                }

                // Normalize for diagonal movement
                moveDirection.Normalize();

                // Transform direction relative to camera rotation (Y axis only)
                Vector3 forward = transform.forward;
                forward.y = 0;
                forward.Normalize();

                Vector3 right = transform.right;
                right.y = 0;
                right.Normalize();

                Vector3 worldDirection = forward * moveDirection.z + right * moveDirection.x;
                transform.position += worldDirection * speed * Time.deltaTime;
            }
        }

        #endregion

        #region Mouse Input

        private void HandleMouseInput()
        {
            // Scroll wheel zoom
            float scrollDelta = Input.mouseScrollDelta.y;
            if (Mathf.Abs(scrollDelta) > 0.01f)
            {
                _targetHeight -= scrollDelta * _zoomSpeed;
            }

            // Clamp target height
            _targetHeight = Mathf.Clamp(_targetHeight, _minHeight, _maxHeight);

            // Middle mouse button pan
            if (_enableMiddleMousePan)
            {
                HandleMiddleMousePan();
            }

            // Edge pan
            if (_enableEdgePan && !_isMiddleMouseDragging)
            {
                HandleEdgePan();
            }
        }

        private void HandleMiddleMousePan()
        {
            if (Input.GetMouseButtonDown(2))
            {
                _isMiddleMouseDragging = true;
                _lastMousePosition = Input.mousePosition;
            }

            if (Input.GetMouseButtonUp(2))
            {
                _isMiddleMouseDragging = false;
            }

            if (_isMiddleMouseDragging && Input.GetMouseButton(2))
            {
                Vector3 mouseDelta = Input.mousePosition - _lastMousePosition;
                _lastMousePosition = Input.mousePosition;

                // Convert screen space delta to world space movement
                float panMultiplier = _middleMousePanSensitivity * (transform.position.y / 20f);

                Vector3 forward = transform.forward;
                forward.y = 0;
                forward.Normalize();

                Vector3 right = transform.right;
                right.y = 0;
                right.Normalize();

                Vector3 movement = -right * mouseDelta.x * panMultiplier - forward * mouseDelta.y * panMultiplier;
                transform.position += movement * Time.deltaTime * 10f;
            }
        }

        private void HandleEdgePan()
        {
            Vector3 mousePos = Input.mousePosition;
            Vector3 moveDirection = Vector3.zero;

            // Check edges
            if (mousePos.x <= _edgePanBorderSize)
            {
                moveDirection += Vector3.left;
            }
            else if (mousePos.x >= Screen.width - _edgePanBorderSize)
            {
                moveDirection += Vector3.right;
            }

            if (mousePos.y <= _edgePanBorderSize)
            {
                moveDirection += Vector3.back;
            }
            else if (mousePos.y >= Screen.height - _edgePanBorderSize)
            {
                moveDirection += Vector3.forward;
            }

            if (moveDirection != Vector3.zero)
            {
                moveDirection.Normalize();

                Vector3 forward = transform.forward;
                forward.y = 0;
                forward.Normalize();

                Vector3 right = transform.right;
                right.y = 0;
                right.Normalize();

                Vector3 worldDirection = forward * moveDirection.z + right * moveDirection.x;
                transform.position += worldDirection * _panSpeed * Time.deltaTime;
            }
        }

        #endregion

        #region Zoom

        private void ApplyZoom()
        {
            Vector3 pos = transform.position;
            pos.y = Mathf.SmoothDamp(pos.y, _targetHeight, ref _zoomVelocity, _zoomSmoothTime);
            transform.position = pos;
        }

        #endregion

        #region Bounds

        private void ClampToBounds()
        {
            Vector3 pos = transform.position;
            pos.x = Mathf.Clamp(pos.x, _minX, _maxX);
            pos.z = Mathf.Clamp(pos.z, _minZ, _maxZ);
            transform.position = pos;
        }

        /// <summary>
        /// Sets the camera bounds.
        /// </summary>
        /// <param name="minX">Minimum X position.</param>
        /// <param name="maxX">Maximum X position.</param>
        /// <param name="minZ">Minimum Z position.</param>
        /// <param name="maxZ">Maximum Z position.</param>
        public void SetBounds(float minX, float maxX, float minZ, float maxZ)
        {
            _minX = minX;
            _maxX = maxX;
            _minZ = minZ;
            _maxZ = maxZ;
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Moves the camera to look at a world position.
        /// </summary>
        /// <param name="position">World position to center on.</param>
        public void CenterOn(Vector3 position)
        {
            Vector3 newPos = transform.position;
            newPos.x = position.x;
            newPos.z = position.z;

            if (_enableBounds)
            {
                newPos.x = Mathf.Clamp(newPos.x, _minX, _maxX);
                newPos.z = Mathf.Clamp(newPos.z, _minZ, _maxZ);
            }

            transform.position = newPos;
        }

        /// <summary>
        /// Sets the camera zoom level immediately (no smoothing).
        /// </summary>
        /// <param name="height">Camera height (Y position).</param>
        public void SetZoomImmediate(float height)
        {
            _targetHeight = Mathf.Clamp(height, _minHeight, _maxHeight);
            Vector3 pos = transform.position;
            pos.y = _targetHeight;
            transform.position = pos;
        }

        /// <summary>
        /// Sets the target camera zoom level (with smoothing).
        /// </summary>
        /// <param name="height">Target camera height.</param>
        public void SetZoomTarget(float height)
        {
            _targetHeight = Mathf.Clamp(height, _minHeight, _maxHeight);
        }

        #endregion

        #region Editor Visualization

        private void OnDrawGizmosSelected()
        {
            if (!_enableBounds) return;

            // Draw bounds box
            Gizmos.color = Color.yellow;
            Vector3 center = new Vector3((_minX + _maxX) / 2f, 0f, (_minZ + _maxZ) / 2f);
            Vector3 size = new Vector3(_maxX - _minX, 0.1f, _maxZ - _minZ);
            Gizmos.DrawWireCube(center, size);

            // Draw min/max height indicators
            Gizmos.color = Color.cyan;
            Vector3 cameraPos = transform.position;
            cameraPos.y = _minHeight;
            Gizmos.DrawLine(cameraPos, cameraPos + Vector3.down * 2f);

            cameraPos.y = _maxHeight;
            Gizmos.DrawLine(cameraPos, cameraPos + Vector3.down * 2f);
        }

        #endregion
    }
}
