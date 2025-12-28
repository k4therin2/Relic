using UnityEngine;
using System.Collections.Generic;

namespace Relic.CoreRTS
{
    /// <summary>
    /// Applies team-based colors to unit mesh materials.
    /// Uses MaterialPropertyBlock for efficient GPU instancing compatibility.
    /// </summary>
    /// <remarks>
    /// Part of WP-EXT-5.1: AR UX Enhancements.
    /// Attach to unit prefabs alongside UnitController.
    /// Colors are applied automatically on Start.
    /// </remarks>
    public class TeamColorApplier : MonoBehaviour
    {
        #region Serialized Fields

        [Header("Team Colors")]
        [Tooltip("Color for Team 0 (typically red/player 1)")]
        [SerializeField] private Color _team0Color = new Color(0.9f, 0.2f, 0.2f, 1f);

        [Tooltip("Color for Team 1 (typically blue/player 2)")]
        [SerializeField] private Color _team1Color = new Color(0.2f, 0.4f, 0.9f, 1f);

        [Header("Configuration")]
        [Tooltip("Tag for renderers that should not receive team colors")]
        [SerializeField] private string _excludeTag = "IgnoreTeamColor";

        [Tooltip("Apply to child renderers recursively")]
        [SerializeField] private bool _applyToChildren = true;

        [Header("Color Properties")]
        [Tooltip("Shader property to set for base color (URP)")]
        [SerializeField] private string _colorPropertyName = "_BaseColor";

        [Tooltip("Fallback color property (Standard shader)")]
        [SerializeField] private string _fallbackColorProperty = "_Color";

        #endregion

        #region Runtime State

        private UnitController _unitController;
        private MaterialPropertyBlock _propertyBlock;
        private List<Renderer> _targetRenderers;
        private static readonly Dictionary<int, Color> _customTeamColors = new Dictionary<int, Color>();

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            _propertyBlock = new MaterialPropertyBlock();
            _targetRenderers = new List<Renderer>();
            CacheRenderers();
        }

        private void Start()
        {
            Initialize();
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Initializes the color applier and applies team colors.
        /// </summary>
        public void Initialize()
        {
            if (_unitController == null)
            {
                _unitController = GetComponent<UnitController>();
            }

            ApplyTeamColor();
        }

        /// <summary>
        /// Gets the color for a specific team.
        /// </summary>
        /// <param name="teamId">The team ID.</param>
        /// <returns>The color for that team.</returns>
        public Color GetTeamColor(int teamId)
        {
            // Check for custom colors first
            if (_customTeamColors.TryGetValue(teamId, out Color customColor))
            {
                return customColor;
            }

            // Return default colors
            return teamId switch
            {
                0 => _team0Color,
                1 => _team1Color,
                _ => Color.gray // Default for additional teams
            };
        }

        /// <summary>
        /// Sets a custom color for a team (global setting).
        /// </summary>
        /// <param name="teamId">The team ID.</param>
        /// <param name="color">The color to use.</param>
        public void SetTeamColor(int teamId, Color color)
        {
            // Update instance field for default teams
            if (teamId == 0)
            {
                _team0Color = color;
            }
            else if (teamId == 1)
            {
                _team1Color = color;
            }

            // Store in static dictionary for custom teams
            _customTeamColors[teamId] = color;
        }

        /// <summary>
        /// Applies the team color to all target renderers.
        /// </summary>
        public void ApplyTeamColor()
        {
            if (_targetRenderers == null || _targetRenderers.Count == 0)
            {
                CacheRenderers();
            }

            int teamId = _unitController != null ? _unitController.TeamId : 0;
            Color teamColor = GetTeamColor(teamId);

            foreach (var renderer in _targetRenderers)
            {
                if (renderer == null) continue;
                ApplyColorToRenderer(renderer, teamColor);
            }
        }

        /// <summary>
        /// Refreshes the cached renderers and reapplies colors.
        /// Call this if the mesh hierarchy changes at runtime.
        /// </summary>
        public void RefreshRenderers()
        {
            CacheRenderers();
            ApplyTeamColor();
        }

        #endregion

        #region Private Methods

        private void CacheRenderers()
        {
            _targetRenderers.Clear();

            if (_applyToChildren)
            {
                var allRenderers = GetComponentsInChildren<Renderer>(true);
                foreach (var renderer in allRenderers)
                {
                    if (ShouldColorRenderer(renderer))
                    {
                        _targetRenderers.Add(renderer);
                    }
                }
            }
            else
            {
                var renderer = GetComponent<Renderer>();
                if (renderer != null && ShouldColorRenderer(renderer))
                {
                    _targetRenderers.Add(renderer);
                }
            }
        }

        private bool ShouldColorRenderer(Renderer renderer)
        {
            // Exclude renderers with the exclude tag
            if (!string.IsNullOrEmpty(_excludeTag) && renderer.CompareTag(_excludeTag))
            {
                return false;
            }

            // Skip particle systems
            if (renderer is ParticleSystemRenderer)
            {
                return false;
            }

            return true;
        }

        private void ApplyColorToRenderer(Renderer renderer, Color color)
        {
            renderer.GetPropertyBlock(_propertyBlock);

            // Try URP property first, then fallback
            int propertyId = Shader.PropertyToID(_colorPropertyName);
            if (renderer.sharedMaterial != null && renderer.sharedMaterial.HasProperty(propertyId))
            {
                _propertyBlock.SetColor(propertyId, color);
            }
            else
            {
                // Try fallback property
                propertyId = Shader.PropertyToID(_fallbackColorProperty);
                if (renderer.sharedMaterial != null && renderer.sharedMaterial.HasProperty(propertyId))
                {
                    _propertyBlock.SetColor(propertyId, color);
                }
                else
                {
                    // Last resort: try to set _BaseColor anyway (works in many cases)
                    _propertyBlock.SetColor(Shader.PropertyToID("_BaseColor"), color);
                }
            }

            renderer.SetPropertyBlock(_propertyBlock);
        }

        #endregion

        #region Editor Support

#if UNITY_EDITOR
        private void OnValidate()
        {
            // Reapply colors when values change in editor
            if (Application.isPlaying && _targetRenderers != null && _targetRenderers.Count > 0)
            {
                ApplyTeamColor();
            }
        }
#endif

        #endregion
    }
}
