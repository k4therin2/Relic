using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using UnityEngine.AI;

namespace Relic.CoreRTS.Editor
{
    /// <summary>
    /// Editor utility to create and set up the Flat Debug scene.
    /// Provides menu options for scene creation and NavMesh baking.
    /// </summary>
    /// <remarks>
    /// See WP-EXT-6.1 for requirements.
    /// </remarks>
    public static class FlatDebugSceneSetup
    {
        private const string ScenePath = "Assets/Scenes/Flat_Debug.unity";

        [MenuItem("Relic/Debug/Create Flat Debug Scene")]
        public static void CreateFlatDebugScene()
        {
            // Create new scene
            Scene newScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // Add Directional Light
            GameObject lightGO = new GameObject("Directional Light");
            Light light = lightGO.AddComponent<Light>();
            light.type = LightType.Directional;
            light.color = new Color(1f, 0.956f, 0.839f);
            light.intensity = 1f;
            light.shadows = LightShadows.Soft;
            lightGO.transform.position = new Vector3(0, 30, 0);
            lightGO.transform.rotation = Quaternion.Euler(50, -30, 0);

            // Add Main Camera with DebugCameraController
            GameObject cameraGO = new GameObject("Main Camera");
            cameraGO.tag = "MainCamera";
            Camera camera = cameraGO.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.192f, 0.302f, 0.475f);
            camera.fieldOfView = 60f;
            cameraGO.AddComponent<AudioListener>();
            cameraGO.AddComponent<DebugCameraController>();
            cameraGO.AddComponent<DebugSelectionController>();
            cameraGO.transform.position = new Vector3(0, 25, -20);
            cameraGO.transform.rotation = Quaternion.Euler(45, 0, 0);

            // Add Ground Plane (100x100 unit battlefield)
            GameObject groundGO = GameObject.CreatePrimitive(PrimitiveType.Plane);
            groundGO.name = "Ground";
            groundGO.transform.position = Vector3.zero;
            groundGO.transform.localScale = new Vector3(10, 1, 10); // Plane is 10x10 by default, so 10x scale = 100x100

            // Mark as static for NavMesh
            GameObjectUtility.SetStaticEditorFlags(groundGO, StaticEditorFlags.NavigationStatic);

            // Apply green material
            Renderer renderer = groundGO.GetComponent<Renderer>();
            Material groundMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            if (groundMat != null)
            {
                groundMat.color = new Color(0.3f, 0.5f, 0.3f);
                renderer.sharedMaterial = groundMat;
            }

            // Add SelectionManager
            GameObject managersGO = new GameObject("Managers");
            managersGO.AddComponent<SelectionManager>();

            // Add UnitFactory
            UnitFactory factory = managersGO.AddComponent<UnitFactory>();

            // Add TickManager
            managersGO.AddComponent<TickManager>();

            // Add DestinationMarkerManager for move command visual feedback
            managersGO.AddComponent<DestinationMarkerManager>();

            // Save scene
            EditorSceneManager.SaveScene(newScene, ScenePath);

            Debug.Log($"[FlatDebugSceneSetup] Created Flat_Debug scene at {ScenePath}");
            Debug.Log("[FlatDebugSceneSetup] Next steps:");
            Debug.Log("  1. Open Window > AI > Navigation to bake NavMesh");
            Debug.Log("  2. Select Ground, ensure it's Navigation Static");
            Debug.Log("  3. Click Bake in Navigation window");

            // Select ground to help user bake NavMesh
            Selection.activeGameObject = groundGO;
        }

        [MenuItem("Relic/Debug/Bake NavMesh for Current Scene")]
        public static void BakeNavMesh()
        {
            // Get all NavMesh build sources from the scene
            var sources = new System.Collections.Generic.List<NavMeshBuildSource>();
            var markups = new System.Collections.Generic.List<NavMeshBuildMarkup>();

            // Calculate bounds for the scene
            Bounds bounds = new Bounds(Vector3.zero, new Vector3(200, 50, 200));

            // Collect sources from static game objects
            NavMeshBuilder.CollectSources(bounds, ~0, NavMeshCollectGeometry.RenderMeshes, 0, markups, sources);

            if (sources.Count == 0)
            {
                Debug.LogWarning("[FlatDebugSceneSetup] No NavMesh sources found. Ensure Ground is marked Navigation Static.");
                return;
            }

            // Build the NavMesh
            var settings = NavMesh.GetSettingsByID(0);
            NavMeshData navMeshData = NavMeshBuilder.BuildNavMeshData(
                settings,
                sources,
                bounds,
                Vector3.zero,
                Quaternion.identity
            );

            if (navMeshData != null)
            {
                NavMesh.RemoveAllNavMeshData();
                NavMesh.AddNavMeshData(navMeshData);
                Debug.Log($"[FlatDebugSceneSetup] NavMesh baked successfully with {sources.Count} source(s).");
            }
            else
            {
                Debug.LogWarning("[FlatDebugSceneSetup] NavMesh bake failed. Ensure ground is marked Navigation Static.");
            }
        }

        [MenuItem("Relic/Debug/Open Flat Debug Scene")]
        public static void OpenFlatDebugScene()
        {
            if (System.IO.File.Exists(ScenePath))
            {
                EditorSceneManager.OpenScene(ScenePath);
            }
            else
            {
                Debug.LogWarning($"[FlatDebugSceneSetup] Scene not found at {ScenePath}. Use 'Create Flat Debug Scene' first.");
            }
        }

        [MenuItem("Relic/Debug/Validate Scene Setup")]
        public static void ValidateSceneSetup()
        {
            bool isValid = true;
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            sb.AppendLine("[FlatDebugSceneSetup] Validating Flat_Debug scene setup...");

            // Check Main Camera
            Camera mainCamera = Camera.main;
            if (mainCamera == null)
            {
                sb.AppendLine("  X No Main Camera found");
                isValid = false;
            }
            else
            {
                sb.AppendLine("  OK Main Camera found");

                // Check DebugCameraController
                if (mainCamera.GetComponent<DebugCameraController>() == null)
                {
                    sb.AppendLine("  X DebugCameraController missing on Main Camera");
                    isValid = false;
                }
                else
                {
                    sb.AppendLine("  OK DebugCameraController attached");
                }

                // Check DebugSelectionController
                if (mainCamera.GetComponent<DebugSelectionController>() == null)
                {
                    sb.AppendLine("  X DebugSelectionController missing on Main Camera");
                    isValid = false;
                }
                else
                {
                    sb.AppendLine("  OK DebugSelectionController attached");
                }
            }

            // Check SelectionManager
            if (SelectionManager.Instance == null)
            {
                sb.AppendLine("  X SelectionManager not found in scene");
                isValid = false;
            }
            else
            {
                sb.AppendLine("  OK SelectionManager found");
            }

            // Check for Ground
            GameObject ground = GameObject.Find("Ground");
            if (ground == null)
            {
                sb.AppendLine("  X Ground object not found");
                isValid = false;
            }
            else
            {
                sb.AppendLine("  OK Ground object found");

                // Check NavMesh
                NavMeshHit hit;
                if (NavMesh.SamplePosition(Vector3.zero, out hit, 1f, NavMesh.AllAreas))
                {
                    sb.AppendLine("  OK NavMesh present on ground");
                }
                else
                {
                    sb.AppendLine("  X NavMesh not baked - use Window > AI > Navigation to bake");
                    isValid = false;
                }

                // Check collider
                if (ground.GetComponent<Collider>() == null)
                {
                    sb.AppendLine("  X Ground missing Collider (needed for raycasting)");
                    isValid = false;
                }
                else
                {
                    sb.AppendLine("  OK Ground has Collider");
                }
            }

            // Check Directional Light
            Light[] lights = Object.FindObjectsByType<Light>(FindObjectsSortMode.None);
            bool hasDirectionalLight = false;
            foreach (var light in lights)
            {
                if (light.type == LightType.Directional)
                {
                    hasDirectionalLight = true;
                    break;
                }
            }
            if (hasDirectionalLight)
            {
                sb.AppendLine("  OK Directional Light found");
            }
            else
            {
                sb.AppendLine("  WARN No Directional Light found (lighting may be dark)");
            }

            // Summary
            sb.AppendLine("");
            if (isValid)
            {
                sb.AppendLine("Scene validation PASSED - ready for testing!");
            }
            else
            {
                sb.AppendLine("Scene validation FAILED - fix issues above");
            }

            Debug.Log(sb.ToString());
        }
    }
}
