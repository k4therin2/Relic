using System;
using UnityEngine;

namespace Relic.ARLayer
{
    /// <summary>
    /// Controls battlefield scaling to fit on table surfaces.
    /// Provides presets and gesture-based scaling for AR.
    /// </summary>
    public class BattlefieldScaleController : MonoBehaviour
    {
        [Header("Scale Settings")]
        [SerializeField] private float minScale = 0.1f;
        [SerializeField] private float maxScale = 2.0f;
        [SerializeField] private float defaultScale = 0.5f;
        [SerializeField] private float scaleStep = 0.1f;

        [Header("Size Settings (World Units at Scale 1.0)")]
        [SerializeField] private Vector2 baseBattlefieldSize = new Vector2(2f, 1.2f);

        // Current state - initialized to defaultScale value to support EditMode tests
        private float currentScale = 0.5f;
        private bool isInitialized = false;
        private BattlefieldPlacer placer;

        /// <summary>
        /// Event fired when scale changes.
        /// </summary>
        public event Action<float, Vector2> OnScaleChanged;

        /// <summary>
        /// Predefined scale presets for different table sizes.
        /// </summary>
        public static class ScalePresets
        {
            /// <summary>Small coffee table (~30cm x 20cm battlefield)</summary>
            public const float Small = 0.25f;
            /// <summary>Standard desk (~60cm x 40cm battlefield)</summary>
            public const float Medium = 0.5f;
            /// <summary>Large table (~100cm x 60cm battlefield)</summary>
            public const float Large = 0.8f;
            /// <summary>Floor/large surface (~150cm x 90cm battlefield)</summary>
            public const float ExtraLarge = 1.2f;
        }

        /// <summary>
        /// Current scale factor.
        /// </summary>
        public float CurrentScale
        {
            get
            {
                EnsureInitialized();
                return currentScale;
            }
            set => SetScale(value);
        }

        /// <summary>
        /// Current battlefield size in world units.
        /// </summary>
        public Vector2 CurrentWorldSize
        {
            get
            {
                EnsureInitialized();
                return baseBattlefieldSize * currentScale;
            }
        }

        /// <summary>
        /// Minimum allowed scale.
        /// </summary>
        public float MinScale => minScale;

        /// <summary>
        /// Maximum allowed scale.
        /// </summary>
        public float MaxScale => maxScale;

        private void Awake()
        {
            EnsureInitialized();
            placer = FindFirstObjectByType<BattlefieldPlacer>();
        }

        /// <summary>
        /// Ensures the controller is initialized. Called lazily for EditMode test support.
        /// </summary>
        private void EnsureInitialized()
        {
            if (!isInitialized)
            {
                currentScale = defaultScale;
                isInitialized = true;
            }
        }

        private void OnEnable()
        {
            SyncWithPlacer();
        }

        /// <summary>
        /// Initialize with a placer reference.
        /// </summary>
        public void Initialize(BattlefieldPlacer battlefieldPlacer)
        {
            placer = battlefieldPlacer;
            SyncWithPlacer();
        }

        /// <summary>
        /// Set the battlefield scale.
        /// </summary>
        public void SetScale(float scale)
        {
            EnsureInitialized();
            float newScale = Mathf.Clamp(scale, minScale, maxScale);

            if (Mathf.Approximately(currentScale, newScale))
                return;

            currentScale = newScale;
            isInitialized = true;
            ApplyScale();
            OnScaleChanged?.Invoke(currentScale, CurrentWorldSize);
        }

        /// <summary>
        /// Apply a preset scale.
        /// </summary>
        public void ApplyPreset(float presetScale)
        {
            SetScale(presetScale);
        }

        /// <summary>
        /// Apply the small table preset.
        /// </summary>
        public void SetSmall() => ApplyPreset(ScalePresets.Small);

        /// <summary>
        /// Apply the medium/desk preset.
        /// </summary>
        public void SetMedium() => ApplyPreset(ScalePresets.Medium);

        /// <summary>
        /// Apply the large table preset.
        /// </summary>
        public void SetLarge() => ApplyPreset(ScalePresets.Large);

        /// <summary>
        /// Apply the extra large/floor preset.
        /// </summary>
        public void SetExtraLarge() => ApplyPreset(ScalePresets.ExtraLarge);

        /// <summary>
        /// Increase scale by one step.
        /// </summary>
        public void ScaleUp()
        {
            SetScale(currentScale + scaleStep);
        }

        /// <summary>
        /// Decrease scale by one step.
        /// </summary>
        public void ScaleDown()
        {
            SetScale(currentScale - scaleStep);
        }

        /// <summary>
        /// Scale by a factor (for pinch gestures).
        /// </summary>
        /// <param name="factor">Multiplier to apply to current scale.</param>
        public void ScaleBy(float factor)
        {
            SetScale(currentScale * factor);
        }

        /// <summary>
        /// Reset to default scale.
        /// </summary>
        public void ResetScale()
        {
            SetScale(defaultScale);
        }

        /// <summary>
        /// Get the estimated real-world size for a given scale.
        /// </summary>
        public Vector2 GetWorldSizeForScale(float scale)
        {
            return baseBattlefieldSize * Mathf.Clamp(scale, minScale, maxScale);
        }

        /// <summary>
        /// Calculate the scale needed to fit a specific world size.
        /// </summary>
        public float GetScaleForWorldSize(Vector2 targetSize)
        {
            // Use the smaller dimension to ensure it fits
            float scaleX = targetSize.x / baseBattlefieldSize.x;
            float scaleY = targetSize.y / baseBattlefieldSize.y;
            return Mathf.Clamp(Mathf.Min(scaleX, scaleY), minScale, maxScale);
        }

        /// <summary>
        /// Calculate the scale needed to fit within a detected plane.
        /// </summary>
        public float GetScaleToFitPlane(float planeWidth, float planeDepth, float padding = 0.1f)
        {
            float availableWidth = planeWidth - (padding * 2);
            float availableDepth = planeDepth - (padding * 2);
            return GetScaleForWorldSize(new Vector2(availableWidth, availableDepth));
        }

        private void ApplyScale()
        {
            if (placer != null)
            {
                placer.BattlefieldScale = currentScale;
                placer.BattlefieldSize = baseBattlefieldSize;
            }

            // Apply to existing battlefield if placed
            if (placer != null && placer.PlacedBattlefield != null)
            {
                var battlefield = placer.PlacedBattlefield;
                battlefield.transform.localScale = Vector3.one * currentScale;
            }
        }

        private void SyncWithPlacer()
        {
            if (placer != null)
            {
                placer.BattlefieldScale = currentScale;
                placer.BattlefieldSize = baseBattlefieldSize;
            }
        }
    }
}
