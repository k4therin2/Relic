using UnityEngine;

namespace Relic.ARLayer
{
    /// <summary>
    /// Visual feedback component for battlefield placement preview.
    /// Displays a semi-transparent preview with spawn point indicators.
    /// </summary>
    public class BattlefieldPlacementPreview : MonoBehaviour
    {
        [Header("Visual Settings")]
        [SerializeField] private Color validPlacementColor = new Color(0.2f, 0.8f, 0.2f, 0.5f);
        [SerializeField] private Color invalidPlacementColor = new Color(0.8f, 0.2f, 0.2f, 0.5f);
        [SerializeField] private float pulseSpeed = 2f;
        [SerializeField] private float pulseIntensity = 0.2f;

        [Header("Spawn Point Colors")]
        [SerializeField] private Color redTeamColor = new Color(0.9f, 0.2f, 0.2f, 0.8f);
        [SerializeField] private Color blueTeamColor = new Color(0.2f, 0.2f, 0.9f, 0.8f);

        // Components
        private Renderer groundRenderer;
        private Renderer redSpawnRenderer;
        private Renderer blueSpawnRenderer;
        private MaterialPropertyBlock propertyBlock;

        // State
        private bool isValid = true;
        private float pulseTime;

        /// <summary>
        /// Whether the current placement is valid.
        /// </summary>
        public bool IsValid
        {
            get => isValid;
            set
            {
                isValid = value;
                UpdateColors();
            }
        }

        private void Awake()
        {
            propertyBlock = new MaterialPropertyBlock();
            FindRenderers();
            SetupMaterials();
        }

        private void Update()
        {
            UpdatePulse();
        }

        /// <summary>
        /// Show the preview.
        /// </summary>
        public void Show()
        {
            gameObject.SetActive(true);
        }

        /// <summary>
        /// Hide the preview.
        /// </summary>
        public void Hide()
        {
            gameObject.SetActive(false);
        }

        /// <summary>
        /// Update the preview position and rotation.
        /// </summary>
        public void UpdateTransform(Vector3 position, Quaternion rotation)
        {
            transform.position = position;
            transform.rotation = rotation;
        }

        /// <summary>
        /// Set the preview scale to match battlefield size.
        /// </summary>
        public void SetScale(Vector2 size, float scale)
        {
            transform.localScale = new Vector3(size.x * scale, 1f, size.y * scale);
        }

        private void FindRenderers()
        {
            // Find ground renderer
            var ground = transform.Find("Ground");
            if (ground != null)
            {
                groundRenderer = ground.GetComponent<Renderer>();
            }

            // Find spawn point renderers
            var spawnPoints = transform.Find("SpawnPoints");
            if (spawnPoints != null)
            {
                var redSpawn = spawnPoints.Find("RedSpawn");
                var blueSpawn = spawnPoints.Find("BlueSpawn");

                if (redSpawn != null)
                {
                    redSpawnRenderer = redSpawn.GetComponent<Renderer>();
                }

                if (blueSpawn != null)
                {
                    blueSpawnRenderer = blueSpawn.GetComponent<Renderer>();
                }
            }
        }

        private void SetupMaterials()
        {
            // Configure materials for transparency
            if (groundRenderer != null)
            {
                var material = groundRenderer.material;
                SetMaterialTransparent(material);
            }

            if (redSpawnRenderer != null)
            {
                var material = redSpawnRenderer.material;
                SetMaterialTransparent(material);
                material.color = redTeamColor;
            }

            if (blueSpawnRenderer != null)
            {
                var material = blueSpawnRenderer.material;
                SetMaterialTransparent(material);
                material.color = blueTeamColor;
            }

            UpdateColors();
        }

        private void SetMaterialTransparent(Material material)
        {
            if (material == null) return;

            // Standard shader transparency settings
            material.SetFloat("_Mode", 3); // Transparent
            material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            material.SetInt("_ZWrite", 0);
            material.DisableKeyword("_ALPHATEST_ON");
            material.EnableKeyword("_ALPHABLEND_ON");
            material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            material.renderQueue = 3000;
        }

        private void UpdateColors()
        {
            if (groundRenderer == null) return;

            var baseColor = isValid ? validPlacementColor : invalidPlacementColor;
            groundRenderer.GetPropertyBlock(propertyBlock);
            propertyBlock.SetColor("_Color", baseColor);
            groundRenderer.SetPropertyBlock(propertyBlock);
        }

        private void UpdatePulse()
        {
            if (groundRenderer == null) return;

            pulseTime += Time.deltaTime * pulseSpeed;
            float pulse = Mathf.Sin(pulseTime) * pulseIntensity;

            var baseColor = isValid ? validPlacementColor : invalidPlacementColor;
            var pulsedColor = new Color(
                baseColor.r + pulse,
                baseColor.g + pulse,
                baseColor.b + pulse,
                baseColor.a
            );

            groundRenderer.GetPropertyBlock(propertyBlock);
            propertyBlock.SetColor("_Color", pulsedColor);
            groundRenderer.SetPropertyBlock(propertyBlock);
        }
    }
}
