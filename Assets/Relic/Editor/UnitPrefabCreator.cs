using UnityEngine;
using UnityEditor;
using UnityEngine.AI;
using Relic.CoreRTS;

namespace Relic.Editor
{
    /// <summary>
    /// Editor utility for creating base unit prefabs.
    /// Run via menu: Relic > Create Base Unit Prefab
    /// </summary>
    public static class UnitPrefabCreator
    {
        private const string PREFABS_PATH = "Assets/Relic/Prefabs/Units";

        [MenuItem("Relic/Create Base Unit Prefab")]
        public static void CreateBaseUnitPrefab()
        {
            // Ensure directory exists
            if (!AssetDatabase.IsValidFolder(PREFABS_PATH))
            {
                AssetDatabase.CreateFolder("Assets/Relic/Prefabs", "Units");
            }

            string path = $"{PREFABS_PATH}/BaseUnit.prefab";

            // Check if already exists
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (existing != null)
            {
                Debug.Log("[UnitPrefabCreator] BaseUnit prefab already exists. Delete it first to recreate.");
                Selection.activeObject = existing;
                return;
            }

            // Create unit GameObject hierarchy
            var unitRoot = new GameObject("BaseUnit");

            // Add components
            var controller = unitRoot.AddComponent<UnitController>();

            // Add capsule collider
            var collider = unitRoot.AddComponent<CapsuleCollider>();
            collider.height = 2f;
            collider.radius = 0.5f;
            collider.center = new Vector3(0, 1f, 0);

            // Add NavMeshAgent
            var agent = unitRoot.AddComponent<NavMeshAgent>();
            agent.speed = 3f;
            agent.angularSpeed = 360f;
            agent.acceleration = 8f;
            agent.stoppingDistance = 0.1f;
            agent.radius = 0.5f;
            agent.height = 2f;

            // Create visual representation (placeholder capsule)
            var visual = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            visual.name = "Visual";
            visual.transform.SetParent(unitRoot.transform);
            visual.transform.localPosition = new Vector3(0, 1f, 0);
            visual.transform.localScale = Vector3.one;

            // Remove the primitive's collider (we use the parent's)
            var visualCollider = visual.GetComponent<CapsuleCollider>();
            if (visualCollider != null)
            {
                Object.DestroyImmediate(visualCollider);
            }

            // Create selection indicator point (below unit)
            var selectionPoint = new GameObject("SelectionIndicatorPoint");
            selectionPoint.transform.SetParent(unitRoot.transform);
            selectionPoint.transform.localPosition = new Vector3(0, 0.05f, 0);

            // Create health bar point (above unit)
            var healthBarPoint = new GameObject("HealthBarPoint");
            healthBarPoint.transform.SetParent(unitRoot.transform);
            healthBarPoint.transform.localPosition = new Vector3(0, 2.2f, 0);

            // Save as prefab
            PrefabUtility.SaveAsPrefabAsset(unitRoot, path);
            Object.DestroyImmediate(unitRoot);

            Debug.Log($"[UnitPrefabCreator] Created base unit prefab at {path}");

            // Select the new prefab
            Selection.activeObject = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        }

        [MenuItem("Relic/Create All Era Unit Prefabs")]
        public static void CreateAllEraUnitPrefabs()
        {
            // Ensure directory exists
            if (!AssetDatabase.IsValidFolder(PREFABS_PATH))
            {
                AssetDatabase.CreateFolder("Assets/Relic/Prefabs", "Units");
            }

            // Create prefabs for each era unit
            CreateEraPrefab("Legionnaire", Color.red);
            CreateEraPrefab("Knight", Color.blue);
            CreateEraPrefab("Rifleman", Color.green);
            CreateEraPrefab("CombatDrone", Color.cyan);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("[UnitPrefabCreator] Created 4 era unit prefabs");
        }

        private static void CreateEraPrefab(string name, Color color)
        {
            string path = $"{PREFABS_PATH}/{name}.prefab";

            // Check if already exists
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (existing != null)
            {
                Debug.Log($"[UnitPrefabCreator] {name} prefab already exists");
                return;
            }

            // Create unit GameObject
            var unitRoot = new GameObject(name);

            // Add components
            unitRoot.AddComponent<UnitController>();

            var collider = unitRoot.AddComponent<CapsuleCollider>();
            collider.height = 2f;
            collider.radius = 0.5f;
            collider.center = new Vector3(0, 1f, 0);

            var agent = unitRoot.AddComponent<NavMeshAgent>();
            agent.speed = 3f;
            agent.angularSpeed = 360f;
            agent.acceleration = 8f;
            agent.stoppingDistance = 0.1f;

            // Create colored visual
            var visual = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            visual.name = "Visual";
            visual.transform.SetParent(unitRoot.transform);
            visual.transform.localPosition = new Vector3(0, 1f, 0);

            // Set material color
            var renderer = visual.GetComponent<MeshRenderer>();
            if (renderer != null)
            {
                var material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                material.color = color;
                renderer.sharedMaterial = material;

                // Save material as asset
                string matPath = $"{PREFABS_PATH}/{name}_Material.mat";
                AssetDatabase.CreateAsset(material, matPath);
            }

            // Remove visual collider
            var visualCollider = visual.GetComponent<CapsuleCollider>();
            if (visualCollider != null)
            {
                Object.DestroyImmediate(visualCollider);
            }

            // Selection/health bar points
            var selectionPoint = new GameObject("SelectionIndicatorPoint");
            selectionPoint.transform.SetParent(unitRoot.transform);
            selectionPoint.transform.localPosition = new Vector3(0, 0.05f, 0);

            var healthBarPoint = new GameObject("HealthBarPoint");
            healthBarPoint.transform.SetParent(unitRoot.transform);
            healthBarPoint.transform.localPosition = new Vector3(0, 2.2f, 0);

            // Save as prefab
            PrefabUtility.SaveAsPrefabAsset(unitRoot, path);
            Object.DestroyImmediate(unitRoot);

            Debug.Log($"[UnitPrefabCreator] Created {name} prefab");
        }
    }
}
