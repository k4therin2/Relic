using UnityEngine;

namespace Relic.CoreRTS
{
    /// <summary>
    /// Types of destination markers.
    /// </summary>
    public enum MarkerType
    {
        /// <summary>Move command destination.</summary>
        Move,

        /// <summary>Attack command destination.</summary>
        Attack,

        /// <summary>Rally point marker.</summary>
        Rally
    }

    /// <summary>
    /// Visual marker for move/attack command destinations.
    /// Shows a ring/circle at the target position.
    /// </summary>
    /// <remarks>
    /// Part of WP-EXT-5.1: AR UX Enhancements.
    /// Managed by DestinationMarkerManager - use the manager to create/show markers.
    /// </remarks>
    public class DestinationMarker : MonoBehaviour
    {
        #region Serialized Fields

        [Header("Visual Settings")]
        [Tooltip("Height offset from ground")]
        [SerializeField] private float _heightOffset = 0.05f;

        [Tooltip("Size of the marker ring")]
        [SerializeField] private float _markerSize = 1f;

        [Tooltip("Pulse animation speed (0 to disable)")]
        [SerializeField] private float _pulseSpeed = 2f;

        [Tooltip("Pulse amplitude (scale variation)")]
        [SerializeField] private float _pulseAmplitude = 0.2f;

        #endregion

        #region Runtime State

        private Vector3 _position;
        private MarkerType _type;
        private Color _color;
        private float _lifetime;
        private float _spawnTime;
        private GameObject _visual;
        private Renderer _visualRenderer;
        private MaterialPropertyBlock _propertyBlock;
        private static readonly int ColorProperty = Shader.PropertyToID("_BaseColor");

        #endregion

        #region Properties

        /// <summary>Gets the marker's world position.</summary>
        public Vector3 Position => _position;

        /// <summary>Gets the marker type.</summary>
        public MarkerType Type => _type;

        /// <summary>Gets the marker color.</summary>
        public Color Color => _color;

        /// <summary>Gets the marker lifetime in seconds.</summary>
        public float Lifetime => _lifetime;

        /// <summary>Gets the visual GameObject.</summary>
        public GameObject Visual => _visual;

        /// <summary>Gets whether the marker is currently active/visible.</summary>
        public bool IsActive => _visual != null && _visual.activeInHierarchy;

        /// <summary>Gets the remaining time before auto-hide.</summary>
        public float RemainingTime => Mathf.Max(0f, _lifetime - (Time.time - _spawnTime));

        #endregion

        #region Public Methods

        /// <summary>
        /// Initializes the marker with position, type, and visual properties.
        /// </summary>
        /// <param name="position">World position for the marker.</param>
        /// <param name="type">Type of marker (Move/Attack).</param>
        /// <param name="color">Color of the marker.</param>
        /// <param name="lifetime">How long to show the marker (0 for indefinite).</param>
        public void Initialize(Vector3 position, MarkerType type, Color color, float lifetime)
        {
            _position = position;
            _type = type;
            _color = color;
            _lifetime = lifetime;
            _spawnTime = Time.time;

            if (_propertyBlock == null)
            {
                _propertyBlock = new MaterialPropertyBlock();
            }

            if (_visual == null)
            {
                CreateVisual();
            }

            UpdateVisual();
            Show();
        }

        /// <summary>
        /// Sets a new position for the marker.
        /// </summary>
        /// <param name="position">New world position.</param>
        public void SetPosition(Vector3 position)
        {
            _position = position;
            UpdateVisual();
        }

        /// <summary>
        /// Shows the marker visual.
        /// </summary>
        public void Show()
        {
            if (_visual != null)
            {
                _visual.SetActive(true);
            }
        }

        /// <summary>
        /// Hides the marker visual.
        /// </summary>
        public void Hide()
        {
            if (_visual != null)
            {
                _visual.SetActive(false);
            }
        }

        /// <summary>
        /// Updates the marker color.
        /// </summary>
        /// <param name="color">New color.</param>
        public void SetColor(Color color)
        {
            _color = color;
            ApplyColor();
        }

        #endregion

        #region Unity Lifecycle

        private void Update()
        {
            if (!IsActive) return;

            // Auto-hide after lifetime expires
            if (_lifetime > 0f && RemainingTime <= 0f)
            {
                Hide();
                return;
            }

            // Pulse animation
            if (_pulseSpeed > 0f && _visual != null)
            {
                float pulse = 1f + Mathf.Sin(Time.time * _pulseSpeed) * _pulseAmplitude;
                _visual.transform.localScale = new Vector3(_markerSize * pulse, 0.01f, _markerSize * pulse);
            }
        }

        #endregion

        #region Private Methods

        private void CreateVisual()
        {
            // Create a simple cylinder as the marker ring
            _visual = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            _visual.name = "MarkerVisual";
            _visual.transform.SetParent(transform);
            _visual.transform.localPosition = Vector3.zero;
            _visual.transform.localScale = new Vector3(_markerSize, 0.01f, _markerSize);

            // Remove collider
            var collider = _visual.GetComponent<Collider>();
            if (collider != null)
            {
                SafeDestroy(collider);
            }

            // Setup material
            _visualRenderer = _visual.GetComponent<Renderer>();
            if (_visualRenderer != null)
            {
                // Try to use URP Lit shader
                var material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                if (material.shader == null || material.shader.name == "Hidden/InternalErrorShader")
                {
                    material = new Material(Shader.Find("Standard"));
                }

                // Make transparent
                material.SetFloat("_Surface", 1); // Transparent
                material.SetFloat("_Blend", 0); // Alpha
                material.renderQueue = 3000;

                _visualRenderer.material = material;
            }
        }

        private void UpdateVisual()
        {
            if (_visual == null) return;

            // Position with height offset
            transform.position = _position + Vector3.up * _heightOffset;

            ApplyColor();
        }

        private void ApplyColor()
        {
            if (_visualRenderer == null) return;

            _visualRenderer.GetPropertyBlock(_propertyBlock);
            _propertyBlock.SetColor(ColorProperty, _color);
            _visualRenderer.SetPropertyBlock(_propertyBlock);
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
