using UnityEngine;
using UnityEditor;
using UnityEditor.AI;
using UnityEngine.AI;

namespace Relic.Editor
{
    /// <summary>
    /// Editor utility for setting up NavMesh on the battlefield.
    /// Uses Unity's built-in NavMesh baking (Window > AI > Navigation).
    /// </summary>
    public static class NavMeshSetupUtility
    {
        [MenuItem("Relic/Setup NavMesh/Open Navigation Window")]
        public static void OpenNavigationWindow()
        {
            // Open the built-in Navigation window for NavMesh baking
            EditorApplication.ExecuteMenuItem("Window/AI/Navigation");
            Debug.Log("[NavMeshSetup] Opened Navigation window. Use 'Bake' tab to bake NavMesh.");
        }

        [MenuItem("Relic/Setup NavMesh/Add NavMesh Obstacle to Selected")]
        public static void AddNavMeshObstacle()
        {
            if (Selection.activeGameObject == null)
            {
                EditorUtility.DisplayDialog("No Selection",
                    "Please select a GameObject to add NavMeshObstacle to.", "OK");
                return;
            }

            var go = Selection.activeGameObject;

            // Check if already has obstacle
            var existingObstacle = go.GetComponent<NavMeshObstacle>();
            if (existingObstacle != null)
            {
                EditorUtility.DisplayDialog("Already Exists",
                    $"{go.name} already has a NavMeshObstacle component.", "OK");
                return;
            }

            // Add NavMeshObstacle
            var obstacle = go.AddComponent<NavMeshObstacle>();
            obstacle.carving = true;
            obstacle.carveOnlyStationary = false;

            // Try to size based on collider
            var collider = go.GetComponent<Collider>();
            if (collider is BoxCollider box)
            {
                obstacle.shape = NavMeshObstacleShape.Box;
                obstacle.size = box.size;
                obstacle.center = box.center;
            }
            else if (collider is SphereCollider sphere)
            {
                obstacle.shape = NavMeshObstacleShape.Capsule;
                obstacle.radius = sphere.radius;
                obstacle.height = sphere.radius * 2;
            }
            else if (collider is CapsuleCollider capsule)
            {
                obstacle.shape = NavMeshObstacleShape.Capsule;
                obstacle.radius = capsule.radius;
                obstacle.height = capsule.height;
            }

            Debug.Log($"[NavMeshSetup] Added NavMeshObstacle to {go.name}");
            EditorUtility.SetDirty(go);
        }

        [MenuItem("Relic/Setup NavMesh/Mark Selected as Static Navigation")]
        public static void MarkAsStaticNavigation()
        {
            if (Selection.activeGameObject == null)
            {
                EditorUtility.DisplayDialog("No Selection",
                    "Please select a GameObject to mark as Navigation Static.", "OK");
                return;
            }

            var go = Selection.activeGameObject;

            // Mark as Navigation Static (required for NavMesh baking)
            GameObjectUtility.SetStaticEditorFlags(go,
                GameObjectUtility.GetStaticEditorFlags(go) | StaticEditorFlags.NavigationStatic);

            // Also mark children
            foreach (var child in go.GetComponentsInChildren<Transform>())
            {
                GameObjectUtility.SetStaticEditorFlags(child.gameObject,
                    GameObjectUtility.GetStaticEditorFlags(child.gameObject) | StaticEditorFlags.NavigationStatic);
            }

            Debug.Log($"[NavMeshSetup] Marked {go.name} and children as Navigation Static. Open Navigation window and Bake.");
            EditorUtility.SetDirty(go);
        }

        [MenuItem("Relic/Setup NavMesh/Create Test NavMesh Scene")]
        public static void CreateTestNavMeshScene()
        {
            // Create a simple test scene for movement testing
            var root = new GameObject("NavMesh_Test_Root");

            // Create ground plane
            var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "Ground";
            ground.transform.SetParent(root.transform);
            ground.transform.localScale = new Vector3(5, 1, 5);

            // Mark ground as Navigation Static
            GameObjectUtility.SetStaticEditorFlags(ground, StaticEditorFlags.NavigationStatic);

            // Create some obstacles (dynamic, so they carve)
            for (int i = 0; i < 4; i++)
            {
                var obstacle = GameObject.CreatePrimitive(PrimitiveType.Cube);
                obstacle.name = $"Obstacle_{i + 1}";
                obstacle.transform.SetParent(root.transform);
                obstacle.transform.position = new Vector3(
                    Random.Range(-15f, 15f),
                    0.5f,
                    Random.Range(-15f, 15f)
                );

                // Add carving obstacle
                var navObstacle = obstacle.AddComponent<NavMeshObstacle>();
                navObstacle.carving = true;
                navObstacle.carveOnlyStationary = true;
            }

            Debug.Log("[NavMeshSetup] Created test NavMesh scene. Open Navigation window (Window > AI > Navigation) and click Bake.");
            Selection.activeGameObject = ground;
        }

        [MenuItem("Relic/Setup NavMesh/Add Agents to NavMesh Settings")]
        public static void ShowNavMeshSettings()
        {
            EditorApplication.ExecuteMenuItem("Edit/Project Settings...");
            Debug.Log("[NavMeshSetup] Opened Project Settings. Navigate to 'Navigation' to configure agent types.");
        }
    }
}
