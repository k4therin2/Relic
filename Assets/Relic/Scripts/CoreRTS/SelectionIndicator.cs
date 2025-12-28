using UnityEngine;

namespace Relic.CoreRTS
{
    /// <summary>
    /// Visual indicator for unit selection.
    /// Shows a circle/ring under selected units.
    /// </summary>
    /// <remarks>
    /// Attach to unit prefabs. Automatically shows/hides based on selection state.
    /// See Kyle's milestones.md Milestone 2 for requirements.
    /// </remarks>
    public class SelectionIndicator : MonoBehaviour
    {
        #region Serialized Fields

        [Header("References")]
        [Tooltip("The UnitController this indicator is attached to")]
        [SerializeField] private UnitController _unitController;

        [Tooltip("The visual indicator GameObject (circle/ring mesh)")]
        [SerializeField] private GameObject _indicatorVisual;

        [Header("Settings")]
        [Tooltip("Offset from the ground")]
        [SerializeField] private float _heightOffset = 0.02f;

        [Tooltip("Scale multiplier for the indicator")]
        [SerializeField] private float _scaleMultiplier = 1.5f;

        [Header("Colors")]
        [Tooltip("Color for selected friendly units")]
        [SerializeField] private Color _friendlySelectedColor = new Color(0.2f, 0.8f, 0.2f, 0.8f);

        [Tooltip("Color for selected enemy units")]
        [SerializeField] private Color _enemySelectedColor = new Color(0.8f, 0.2f, 0.2f, 0.8f);

        [Tooltip("Color for hovered (not selected) units")]
        [SerializeField] private Color _hoverColor = new Color(1f, 1f, 0.5f, 0.5f);

        #endregion

        #region Runtime State

        private Renderer _indicatorRenderer;
        private MaterialPropertyBlock _propertyBlock;
        private bool _isHovered;
        private static readonly int ColorProperty = Shader.PropertyToID("_BaseColor");

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            if (_unitController == null)
            {
                _unitController = GetComponentInParent<UnitController>();
            }

            if (_indicatorVisual == null)
            {
                CreateDefaultIndicator();
            }
            else
            {
                _indicatorRenderer = _indicatorVisual.GetComponent<Renderer>();
            }

            _propertyBlock = new MaterialPropertyBlock();

            // Start hidden
            SetIndicatorVisible(false);
        }

        private void Start()
        {
            if (_unitController != null)
            {
                _unitController.OnSelectionChanged += OnSelectionChanged;
            }
        }

        private void OnDestroy()
        {
            if (_unitController != null)
            {
                _unitController.OnSelectionChanged -= OnSelectionChanged;
            }
        }

        #endregion

        #region Event Handlers

        private void OnSelectionChanged(bool isSelected)
        {
            if (isSelected)
            {
                ShowSelection();
            }
            else if (!_isHovered)
            {
                SetIndicatorVisible(false);
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Shows the hover state for this unit.
        /// </summary>
        public void ShowHover()
        {
            _isHovered = true;
            if (_unitController == null || !_unitController.IsSelected)
            {
                SetIndicatorVisible(true);
                SetIndicatorColor(_hoverColor);
            }
        }

        /// <summary>
        /// Hides the hover state for this unit.
        /// </summary>
        public void HideHover()
        {
            _isHovered = false;
            if (_unitController == null || !_unitController.IsSelected)
            {
                SetIndicatorVisible(false);
            }
        }

        /// <summary>
        /// Forces an update of the indicator state.
        /// </summary>
        public void UpdateIndicator()
        {
            if (_unitController != null && _unitController.IsSelected)
            {
                ShowSelection();
            }
            else if (_isHovered)
            {
                ShowHover();
            }
            else
            {
                SetIndicatorVisible(false);
            }
        }

        #endregion

        #region Private Methods

        private void ShowSelection()
        {
            SetIndicatorVisible(true);

            // Determine color based on team
            Color selectionColor = _friendlySelectedColor;

            var selectionManager = SelectionManager.Instance;
            if (selectionManager != null && _unitController != null)
            {
                if (_unitController.TeamId != selectionManager.PlayerTeamId)
                {
                    selectionColor = _enemySelectedColor;
                }
            }

            SetIndicatorColor(selectionColor);
        }

        private void SetIndicatorVisible(bool visible)
        {
            if (_indicatorVisual != null)
            {
                _indicatorVisual.SetActive(visible);
            }
        }

        private void SetIndicatorColor(Color color)
        {
            if (_indicatorRenderer == null) return;

            _indicatorRenderer.GetPropertyBlock(_propertyBlock);
            _propertyBlock.SetColor(ColorProperty, color);
            _indicatorRenderer.SetPropertyBlock(_propertyBlock);
        }

        private void CreateDefaultIndicator()
        {
            // Create a simple circle/ring indicator
            _indicatorVisual = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            _indicatorVisual.name = "SelectionIndicator";
            _indicatorVisual.transform.SetParent(transform);
            _indicatorVisual.transform.localPosition = new Vector3(0, _heightOffset, 0);
            _indicatorVisual.transform.localScale = new Vector3(_scaleMultiplier, 0.01f, _scaleMultiplier);

            // Remove collider
            var collider = _indicatorVisual.GetComponent<Collider>();
            if (collider != null)
            {
                Destroy(collider);
            }

            // Set up renderer
            _indicatorRenderer = _indicatorVisual.GetComponent<Renderer>();
            if (_indicatorRenderer != null)
            {
                // Try to use URP Lit shader, fall back to Standard
                var material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                if (material.shader == null || material.shader.name == "Hidden/InternalErrorShader")
                {
                    material = new Material(Shader.Find("Standard"));
                }

                // Make it transparent
                material.SetFloat("_Surface", 1); // Transparent
                material.SetFloat("_Blend", 0); // Alpha
                material.renderQueue = 3000;
                material.color = _friendlySelectedColor;

                _indicatorRenderer.material = material;
            }

            _indicatorVisual.SetActive(false);
        }

        #endregion
    }
}
