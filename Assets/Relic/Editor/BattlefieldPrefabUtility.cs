#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

namespace Relic.Editor
{
    /// <summary>
    /// Editor utility to create battlefield and preview prefabs.
    /// Run via menu: Relic/Setup/Create Battlefield Prefabs
    /// </summary>
    public static class BattlefieldPrefabUtility
    {
        private const string PrefabsPath = "Assets/Relic/Prefabs/";

        [MenuItem("Relic/Setup/Create Battlefield Prefabs")]
        public static void CreateAllBattlefieldPrefabs()
        {
            EnsurePrefabsDirectoryExists();
            CreateBattlefieldPrefab();
            CreatePreviewPrefab();
            Debug.Log("BattlefieldPrefabUtility: All battlefield prefabs created!");
        }

        [MenuItem("Relic/Setup/Create Battlefield Prefab")]
        public static void CreateBattlefieldPrefab()
        {
            EnsurePrefabsDirectoryExists();

            var battlefield = new GameObject("BattlefieldRoot");

            // Create ground plane
            var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "Ground";
            ground.transform.SetParent(battlefield.transform);
            ground.transform.localPosition = Vector3.zero;
            ground.transform.localRotation = Quaternion.identity;
            ground.transform.localScale = Vector3.one;

            // Apply a basic material
            var groundRenderer = ground.GetComponent<Renderer>();
            var groundMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            groundMaterial.color = new Color(0.4f, 0.5f, 0.3f, 1f); // Green-brown
            groundRenderer.material = groundMaterial;

            // Create spawn points container
            var spawnPoints = new GameObject("SpawnPoints");
            spawnPoints.transform.SetParent(battlefield.transform);
            spawnPoints.transform.localPosition = Vector3.zero;

            // Red team spawn marker
            var redSpawn = CreateSpawnMarker("RedSpawn", new Color(0.9f, 0.2f, 0.2f, 0.8f));
            redSpawn.transform.SetParent(spawnPoints.transform);
            redSpawn.transform.localPosition = new Vector3(-4f, 0.01f, 0f);

            // Blue team spawn marker
            var blueSpawn = CreateSpawnMarker("BlueSpawn", new Color(0.2f, 0.2f, 0.9f, 0.8f));
            blueSpawn.transform.SetParent(spawnPoints.transform);
            blueSpawn.transform.localPosition = new Vector3(4f, 0.01f, 0f);

            // Create obstacles container (empty for now)
            var obstacles = new GameObject("Obstacles");
            obstacles.transform.SetParent(battlefield.transform);
            obstacles.transform.localPosition = Vector3.zero;

            // Save as prefab
            string prefabPath = PrefabsPath + "BattlefieldRoot.prefab";
            PrefabUtility.SaveAsPrefabAsset(battlefield, prefabPath);
            Object.DestroyImmediate(battlefield);

            Debug.Log($"Created: {prefabPath}");
            AssetDatabase.Refresh();
        }

        [MenuItem("Relic/Setup/Create Placement Preview Prefab")]
        public static void CreatePreviewPrefab()
        {
            EnsurePrefabsDirectoryExists();

            var preview = new GameObject("BattlefieldPreview");

            // Add preview component
            preview.AddComponent<ARLayer.BattlefieldPlacementPreview>();

            // Create ground preview (semi-transparent plane)
            var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "Ground";
            ground.transform.SetParent(preview.transform);
            ground.transform.localPosition = Vector3.zero;
            ground.transform.localRotation = Quaternion.identity;
            ground.transform.localScale = Vector3.one;

            // Remove collider from preview
            var collider = ground.GetComponent<Collider>();
            if (collider != null)
            {
                Object.DestroyImmediate(collider);
            }

            // Apply transparent material
            var groundRenderer = ground.GetComponent<Renderer>();
            var previewMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            previewMaterial.SetFloat("_Surface", 1); // Transparent
            previewMaterial.SetFloat("_Blend", 0); // Alpha
            previewMaterial.SetFloat("_AlphaClip", 0);
            previewMaterial.color = new Color(0.2f, 0.8f, 0.2f, 0.5f); // Semi-transparent green
            previewMaterial.renderQueue = 3000;
            groundRenderer.material = previewMaterial;

            // Create spawn point previews container
            var spawnPoints = new GameObject("SpawnPoints");
            spawnPoints.transform.SetParent(preview.transform);
            spawnPoints.transform.localPosition = Vector3.zero;

            // Red team spawn preview
            var redSpawn = CreateSpawnMarker("RedSpawn", new Color(0.9f, 0.2f, 0.2f, 0.6f), true);
            redSpawn.transform.SetParent(spawnPoints.transform);
            redSpawn.transform.localPosition = new Vector3(-4f, 0.02f, 0f);

            // Blue team spawn preview
            var blueSpawn = CreateSpawnMarker("BlueSpawn", new Color(0.2f, 0.2f, 0.9f, 0.6f), true);
            blueSpawn.transform.SetParent(spawnPoints.transform);
            blueSpawn.transform.localPosition = new Vector3(4f, 0.02f, 0f);

            // Save as prefab
            string prefabPath = PrefabsPath + "BattlefieldPreview.prefab";
            PrefabUtility.SaveAsPrefabAsset(preview, prefabPath);
            Object.DestroyImmediate(preview);

            Debug.Log($"Created: {prefabPath}");
            AssetDatabase.Refresh();
        }

        private static GameObject CreateSpawnMarker(string name, Color color, bool isPreview = false)
        {
            // Create a cylinder for spawn marker
            var marker = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            marker.name = name;
            marker.transform.localScale = new Vector3(1f, 0.1f, 1f);

            // Remove collider if preview
            if (isPreview)
            {
                var collider = marker.GetComponent<Collider>();
                if (collider != null)
                {
                    Object.DestroyImmediate(collider);
                }
            }

            // Apply material
            var renderer = marker.GetComponent<Renderer>();
            var material = new Material(Shader.Find("Universal Render Pipeline/Lit"));

            if (isPreview)
            {
                material.SetFloat("_Surface", 1); // Transparent
                material.SetFloat("_Blend", 0); // Alpha
                material.renderQueue = 3000;
            }

            material.color = color;
            renderer.material = material;

            return marker;
        }

        private static void EnsurePrefabsDirectoryExists()
        {
            if (!AssetDatabase.IsValidFolder("Assets/Relic"))
            {
                AssetDatabase.CreateFolder("Assets", "Relic");
            }

            if (!AssetDatabase.IsValidFolder("Assets/Relic/Prefabs"))
            {
                AssetDatabase.CreateFolder("Assets/Relic", "Prefabs");
            }
        }
    }
}
#endif
