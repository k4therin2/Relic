using UnityEngine;

namespace Relic.CoreRTS
{
    /// <summary>
    /// World-space health bar that displays above units.
    /// Shows current health as a colored bar that follows the unit.
    /// </summary>
    /// <remarks>
    /// Part of WP-EXT-5.1: AR UX Enhancements.
    /// Attach to unit prefabs alongside UnitController.
    /// Uses gradient coloring: green (full) -> yellow (mid) -> red (low).
    /// </remarks>
    public class HealthBar : MonoBehaviour
    {
        #region Serialized Fields

        [Header("Positioning")]
        [Tooltip("Height offset above the unit")]
        [SerializeField] private float _heightOffset = 1.5f;

        [Tooltip("Width of the health bar")]
        [SerializeField] private float _barWidth = 1f;

        [Tooltip("Height of the health bar")]
        [SerializeField] private float _barHeight = 0.1f;

        [Header("Colors")]
        [Tooltip("Color at full health")]
        [SerializeField] private Color _fullHealthColor = new Color(0.2f, 0.9f, 0.2f, 0.9f);

        [Tooltip("Color at mid health")]
        [SerializeField] private Color _midHealthColor = new Color(0.9f, 0.9f, 0.2f, 0.9f);

        [Tooltip("Color at low health")]
        [SerializeField] private Color _lowHealthColor = new Color(0.9f, 0.2f, 0.2f, 0.9f);

        [Tooltip("Background bar color")]
        [SerializeField] private Color _backgroundColor = new Color(0.2f, 0.2f, 0.2f, 0.7f);

        [Header("Thresholds")]
        [Tooltip("Health percent below which color is 'low'")]
        [SerializeField] private float _lowHealthThreshold = 0.3f;

        [Tooltip("Health percent below which color transitions to 'mid'")]
        [SerializeField] private float _midHealthThreshold = 0.6f;

        [Header("Behavior")]
        [Tooltip("Hide bar when at full health")]
        [SerializeField] private bool _hideWhenFull = true;

        [Tooltip("Billboard - always face camera")]
        [SerializeField] private bool _billboard = true;

        [Tooltip("Automatically update when health changes")]
        [SerializeField] private bool _autoUpdate = true;

        #endregion

        #region Runtime State

        private UnitController _unitController;
        private GameObject _barVisual;
        private GameObject _backgroundBar;
        private GameObject _fillBar;
        private Renderer _fillRenderer;
        private MaterialPropertyBlock _propertyBlock;
        private float _fillAmount = 1f;
        private Color _currentBarColor;
        private Camera _mainCamera;
        private static readonly int ColorProperty = Shader.PropertyToID("_BaseColor");

        #endregion

        #region Properties

        /// <summary>Gets the bar visual container.</summary>
        public GameObject BarVisual => _barVisual;

        /// <summary>Gets the current fill amount (0-1).</summary>
        public float FillAmount => _fillAmount;

        /// <summary>Gets whether the health bar is currently visible.</summary>
        public bool IsVisible => _barVisual != null && _barVisual.activeInHierarchy;

        /// <summary>Gets the current bar color.</summary>
        public Color BarColor => _currentBarColor;

        #endregion

        #region Unity Lifecycle

        private void Start()
        {
            Initialize();
        }

        public void LateUpdate()
        {
            if (_barVisual == null || _unitController == null) return;

            // Follow unit
            UpdatePosition();

            // Billboard
            if (_billboard)
            {
                if (_mainCamera == null)
                {
                    _mainCamera = Camera.main;
                }

                if (_mainCamera != null)
                {
                    UpdateBillboard(_mainCamera);
                }
            }
        }

        private void OnDestroy()
        {
            if (_unitController != null)
            {
                _unitController.OnHealthChanged -= OnHealthChanged;
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Initializes the health bar visual and links to unit controller.
        /// </summary>
        public void Initialize()
        {
            _propertyBlock = new MaterialPropertyBlock();

            // Get unit controller
            if (_unitController == null)
            {
                _unitController = GetComponent<UnitController>();
            }

            // Subscribe to health changes
            if (_unitController != null && _autoUpdate)
            {
                _unitController.OnHealthChanged += OnHealthChanged;
            }

            // Create visual
            CreateBarVisual();

            // Initial update
            UpdateHealthDisplay();
        }

        /// <summary>
        /// Updates the health bar display based on current unit health.
        /// </summary>
        public void UpdateHealthDisplay()
        {
            if (_unitController == null) return;

            // Get health percent
            _fillAmount = _unitController.HealthPercent;

            // Update fill scale
            if (_fillBar != null)
            {
                var scale = _fillBar.transform.localScale;
                scale.x = _fillAmount;
                _fillBar.transform.localScale = scale;

                // Center the fill bar (since it scales from center)
                var pos = _fillBar.transform.localPosition;
                pos.x = (_fillAmount - 1f) * 0.5f * _barWidth;
                _fillBar.transform.localPosition = pos;
            }

            // Update color
            UpdateBarColor();

            // Handle visibility
            if (_hideWhenFull)
            {
                bool shouldShow = _fillAmount < 0.999f;
                _barVisual.SetActive(shouldShow);
            }
        }

        /// <summary>
        /// Shows the health bar.
        /// </summary>
        public void Show()
        {
            if (_barVisual != null)
            {
                _barVisual.SetActive(true);
            }
        }

        /// <summary>
        /// Hides the health bar.
        /// </summary>
        public void Hide()
        {
            if (_barVisual != null)
            {
                _barVisual.SetActive(false);
            }
        }

        /// <summary>
        /// Sets whether to hide the bar at full health.
        /// </summary>
        public void SetHideWhenFull(bool hide)
        {
            _hideWhenFull = hide;
            UpdateHealthDisplay();
        }

        /// <summary>
        /// Sets whether the bar should billboard toward cameras.
        /// </summary>
        public void SetBillboard(bool billboard)
        {
            _billboard = billboard;
        }

        /// <summary>
        /// Sets whether to auto-update on health changes.
        /// </summary>
        public void SetAutoUpdate(bool autoUpdate)
        {
            _autoUpdate = autoUpdate;

            if (_unitController != null)
            {
                // Unsubscribe first to prevent duplicates
                _unitController.OnHealthChanged -= OnHealthChanged;

                if (_autoUpdate)
                {
                    _unitController.OnHealthChanged += OnHealthChanged;
                }
            }
        }

        /// <summary>
        /// Updates the bar to face the specified camera.
        /// </summary>
        public void UpdateBillboard(Camera camera)
        {
            if (_barVisual == null || camera == null) return;

            Vector3 directionToCamera = camera.transform.position - _barVisual.transform.position;
            directionToCamera.y = 0; // Keep level

            if (directionToCamera.sqrMagnitude > 0.001f)
            {
                _barVisual.transform.rotation = Quaternion.LookRotation(-directionToCamera);
            }
        }

        #endregion

        #region Private Methods

        private void CreateBarVisual()
        {
            // Container
            _barVisual = new GameObject("HealthBarVisual");
            _barVisual.transform.SetParent(transform);
            UpdatePosition();

            // Background bar
            _backgroundBar = CreateBarPart("Background", _barWidth, _barHeight, _backgroundColor);
            _backgroundBar.transform.SetParent(_barVisual.transform);
            _backgroundBar.transform.localPosition = Vector3.zero;

            // Fill bar (slightly smaller to show background as border)
            float fillWidth = _barWidth * 0.95f;
            float fillHeight = _barHeight * 0.8f;
            _fillBar = CreateBarPart("Fill", fillWidth, fillHeight, _fullHealthColor);
            _fillBar.transform.SetParent(_barVisual.transform);
            _fillBar.transform.localPosition = new Vector3(0, 0, -0.001f); // Slightly in front

            _fillRenderer = _fillBar.GetComponent<Renderer>();
            _currentBarColor = _fullHealthColor;
        }

        private GameObject CreateBarPart(string name, float width, float height, Color color)
        {
            var barPart = GameObject.CreatePrimitive(PrimitiveType.Quad);
            barPart.name = name;
            barPart.transform.localScale = new Vector3(width, height, 1f);

            // Remove collider
            var collider = barPart.GetComponent<Collider>();
            if (collider != null)
            {
                SafeDestroy(collider);
            }

            // Setup material
            var renderer = barPart.GetComponent<Renderer>();
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
                material.renderQueue = 3001;
                material.color = color;

                renderer.material = material;
            }

            return barPart;
        }

        private void UpdatePosition()
        {
            if (_barVisual == null || _unitController == null) return;

            Vector3 position = _unitController.HealthBarPoint.position;
            position.y += _heightOffset;
            _barVisual.transform.position = position;
        }

        private void UpdateBarColor()
        {
            // Calculate color based on health percent
            if (_fillAmount > _midHealthThreshold)
            {
                // Full to mid: green to yellow
                float t = (_fillAmount - _midHealthThreshold) / (1f - _midHealthThreshold);
                _currentBarColor = Color.Lerp(_midHealthColor, _fullHealthColor, t);
            }
            else if (_fillAmount > _lowHealthThreshold)
            {
                // Mid to low: yellow to red
                float t = (_fillAmount - _lowHealthThreshold) / (_midHealthThreshold - _lowHealthThreshold);
                _currentBarColor = Color.Lerp(_lowHealthColor, _midHealthColor, t);
            }
            else
            {
                // Low: red
                _currentBarColor = _lowHealthColor;
            }

            // Apply color
            if (_fillRenderer != null)
            {
                _fillRenderer.GetPropertyBlock(_propertyBlock);
                _propertyBlock.SetColor(ColorProperty, _currentBarColor);
                _fillRenderer.SetPropertyBlock(_propertyBlock);
            }
        }

        private void OnHealthChanged(int currentHealth, int maxHealth, int change)
        {
            UpdateHealthDisplay();
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
