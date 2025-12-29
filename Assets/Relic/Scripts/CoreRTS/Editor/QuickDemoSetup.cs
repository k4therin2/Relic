using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.AI;

namespace Relic.CoreRTS.Editor
{
    /// <summary>
    /// One-click playable demo setup utility.
    /// Creates a complete debug scene with pre-positioned units ready for combat.
    /// </summary>
    /// <remarks>
    /// Part of WP-EXT-6.5: Quick Setup Utility.
    /// Builds on Batch EXT-6 (Flat Debug Demo) infrastructure.
    /// </remarks>
    public static class QuickDemoSetup
    {
        private const string ScenePath = "Assets/Scenes/Flat_Debug.unity";
        private const string PrefabPath = "Assets/Prefabs/Debug/DebugUnit.prefab";
        private const string ArchetypePath = "Assets/Data/Archetypes/DebugUnitArchetype.asset";

        /// <summary>
        /// Configuration for the quick demo setup.
        /// </summary>
        public struct DemoConfig
        {
            public int UnitsPerTeam;
            public float Team0CenterX;
            public float Team1CenterX;
            public float SpawnSpacing;
            public bool EnterPlayMode;

            public static DemoConfig Default => new DemoConfig
            {
                UnitsPerTeam = 15,
                Team0CenterX = -8f,  // Closer together for combat
                Team1CenterX = 8f,
                SpawnSpacing = 2f,
                EnterPlayMode = true
            };
        }

        [MenuItem("Relic/Demo/Quick Combat Demo Setup")]
        public static void SetupQuickDemo()
        {
            SetupQuickDemo(DemoConfig.Default);
        }

        [MenuItem("Relic/Demo/Quick Combat Demo (No Play Mode)")]
        public static void SetupQuickDemoNoPlay()
        {
            var config = DemoConfig.Default;
            config.EnterPlayMode = false;
            SetupQuickDemo(config);
        }

        /// <summary>
        /// Sets up a quick combat demo with the specified configuration.
        /// </summary>
        /// <param name="config">Demo configuration options.</param>
        /// <returns>True if setup succeeded, false otherwise.</returns>
        public static bool SetupQuickDemo(DemoConfig config)
        {
            Debug.Log("[QuickDemoSetup] Starting quick demo setup...");

            // Step 1: Create or load scene
            if (!EnsureSceneExists())
            {
                Debug.LogError("[QuickDemoSetup] Failed to create or load debug scene.");
                return false;
            }

            // Step 2: Bake NavMesh
            if (!BakeNavMesh())
            {
                Debug.LogWarning("[QuickDemoSetup] NavMesh bake may have issues. Continuing anyway...");
            }

            // Step 3: Ensure prefab and archetype exist
            if (!EnsurePrefabsExist())
            {
                Debug.LogError("[QuickDemoSetup] Failed to create required prefabs.");
                return false;
            }

            // Step 4: Configure DebugSpawnerUI with archetype
            ConfigureSpawnerUI();

            // Step 5: Clear any existing units
            ClearExistingUnits();

            // Step 6: Spawn units in combat positions
            SpawnCombatUnits(config);

            // Step 7: Save scene
            EditorSceneManager.SaveOpenScenes();

            Debug.Log("[QuickDemoSetup] Demo setup complete!");
            Debug.Log($"  - Spawned {config.UnitsPerTeam * 2} units ({config.UnitsPerTeam} per team)");
            Debug.Log("  - Teams positioned facing each other for combat");

            // Step 8: Enter play mode if requested
            if (config.EnterPlayMode)
            {
                Debug.Log("[QuickDemoSetup] Entering Play Mode...");
                EditorApplication.isPlaying = true;
            }

            return true;
        }

        private static bool EnsureSceneExists()
        {
            // Check if scene file exists
            if (!System.IO.File.Exists(ScenePath))
            {
                Debug.Log("[QuickDemoSetup] Debug scene not found. Creating it...");
                FlatDebugSceneSetup.CreateFlatDebugScene();
            }

            // Open the scene
            var scene = EditorSceneManager.OpenScene(ScenePath);
            return scene.IsValid();
        }

        private static bool BakeNavMesh()
        {
            // Use the existing NavMesh baking utility
            var sources = new System.Collections.Generic.List<NavMeshBuildSource>();
            var markups = new System.Collections.Generic.List<NavMeshBuildMarkup>();

            Bounds bounds = new Bounds(Vector3.zero, new Vector3(200, 50, 200));
            NavMeshBuilder.CollectSources(bounds, ~0, NavMeshCollectGeometry.RenderMeshes, 0, markups, sources);

            if (sources.Count == 0)
            {
                Debug.LogWarning("[QuickDemoSetup] No NavMesh sources found.");
                return false;
            }

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
                Debug.Log("[QuickDemoSetup] NavMesh baked successfully.");
                return true;
            }

            return false;
        }

        private static bool EnsurePrefabsExist()
        {
            // Check if prefab exists
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            if (prefab == null)
            {
                Debug.Log("[QuickDemoSetup] Creating debug unit prefab...");
                DebugUnitPrefabSetup.CreateDebugUnitPrefab();
                prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            }

            // Check if archetype exists
            UnitArchetypeSO archetype = AssetDatabase.LoadAssetAtPath<UnitArchetypeSO>(ArchetypePath);
            if (archetype == null)
            {
                Debug.Log("[QuickDemoSetup] Creating debug unit archetype...");
                DebugUnitPrefabSetup.CreateDebugUnitArchetypeMenu();
                archetype = AssetDatabase.LoadAssetAtPath<UnitArchetypeSO>(ArchetypePath);
            }

            return prefab != null && archetype != null;
        }

        private static void ConfigureSpawnerUI()
        {
            // Find DebugSpawnerUI in scene
            DebugSpawnerUI spawnerUI = Object.FindFirstObjectByType<DebugSpawnerUI>();
            if (spawnerUI == null)
            {
                Debug.LogWarning("[QuickDemoSetup] DebugSpawnerUI not found in scene.");
                return;
            }

            // Load archetype
            UnitArchetypeSO archetype = AssetDatabase.LoadAssetAtPath<UnitArchetypeSO>(ArchetypePath);
            if (archetype == null)
            {
                Debug.LogWarning("[QuickDemoSetup] Archetype not found.");
                return;
            }

            // Configure via SerializedObject
            SerializedObject so = new SerializedObject(spawnerUI);
            so.FindProperty("_unitArchetype").objectReferenceValue = archetype;
            so.ApplyModifiedPropertiesWithoutUndo();

            EditorUtility.SetDirty(spawnerUI);
            Debug.Log("[QuickDemoSetup] Configured DebugSpawnerUI with archetype.");
        }

        private static void ClearExistingUnits()
        {
            // Clear Team0Units parent
            GameObject team0Parent = GameObject.Find("Team0Units");
            if (team0Parent != null)
            {
                Object.DestroyImmediate(team0Parent);
            }

            // Clear Team1Units parent
            GameObject team1Parent = GameObject.Find("Team1Units");
            if (team1Parent != null)
            {
                Object.DestroyImmediate(team1Parent);
            }

            // Also find any stray UnitController objects
            var units = Object.FindObjectsByType<UnitController>(FindObjectsSortMode.None);
            foreach (var unit in units)
            {
                Object.DestroyImmediate(unit.gameObject);
            }
        }

        private static void SpawnCombatUnits(DemoConfig config)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            UnitArchetypeSO archetype = AssetDatabase.LoadAssetAtPath<UnitArchetypeSO>(ArchetypePath);

            if (prefab == null || archetype == null)
            {
                Debug.LogError("[QuickDemoSetup] Missing prefab or archetype for spawning.");
                return;
            }

            // Create parent containers
            GameObject team0Parent = new GameObject("Team0Units");
            GameObject team1Parent = new GameObject("Team1Units");

            // Spawn Team 0 (left side, facing right)
            SpawnTeamFormation(prefab, archetype, 0, config.Team0CenterX, config.UnitsPerTeam, config.SpawnSpacing, team0Parent.transform, 0f);

            // Spawn Team 1 (right side, facing left)
            SpawnTeamFormation(prefab, archetype, 1, config.Team1CenterX, config.UnitsPerTeam, config.SpawnSpacing, team1Parent.transform, 180f);

            Debug.Log($"[QuickDemoSetup] Spawned {config.UnitsPerTeam * 2} units in combat formation.");
        }

        private static void SpawnTeamFormation(GameObject prefab, UnitArchetypeSO archetype, int teamId, float centerX, int count, float spacing, Transform parent, float yRotation)
        {
            // Calculate grid dimensions (prefer wider formations for battle line)
            int columns = Mathf.CeilToInt(Mathf.Sqrt(count) * 1.5f);  // Wider than tall
            int rows = Mathf.CeilToInt((float)count / columns);

            int index = 0;
            for (int row = 0; row < rows && index < count; row++)
            {
                for (int col = 0; col < columns && index < count; col++)
                {
                    float offsetX = (col - columns / 2f) * spacing;
                    float offsetZ = (row - rows / 2f) * spacing;

                    // Small random offset to look more natural
                    float randomX = Random.Range(-0.2f, 0.2f);
                    float randomZ = Random.Range(-0.2f, 0.2f);

                    Vector3 pos = new Vector3(
                        centerX + offsetX + randomX,
                        0f,
                        offsetZ + randomZ
                    );

                    GameObject unit = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                    unit.name = $"Unit_Team{teamId}_{index}";
                    unit.transform.position = pos;
                    unit.transform.rotation = Quaternion.Euler(0f, yRotation, 0f);
                    unit.transform.SetParent(parent);

                    // Initialize unit
                    UnitController controller = unit.GetComponent<UnitController>();
                    if (controller != null)
                    {
                        controller.Initialize(archetype, teamId);
                    }

                    // Apply team color
                    TeamColorApplier colorApplier = unit.GetComponent<TeamColorApplier>();
                    if (colorApplier != null)
                    {
                        colorApplier.Initialize();
                    }

                    Undo.RegisterCreatedObjectUndo(unit, "Spawn Demo Unit");
                    index++;
                }
            }
        }

        /// <summary>
        /// Validates the current scene is ready for demo.
        /// </summary>
        /// <returns>True if all components are present.</returns>
        public static bool ValidateSetup()
        {
            bool valid = true;
            var sb = new System.Text.StringBuilder();
            sb.AppendLine("[QuickDemoSetup] Validation:");

            // Check camera
            if (Camera.main == null)
            {
                sb.AppendLine("  X Main Camera missing");
                valid = false;
            }
            else
            {
                if (Camera.main.GetComponent<DebugCameraController>() == null)
                {
                    sb.AppendLine("  X DebugCameraController missing");
                    valid = false;
                }
                else
                {
                    sb.AppendLine("  OK DebugCameraController");
                }

                if (Camera.main.GetComponent<DebugSelectionController>() == null)
                {
                    sb.AppendLine("  X DebugSelectionController missing");
                    valid = false;
                }
                else
                {
                    sb.AppendLine("  OK DebugSelectionController");
                }
            }

            // Check SelectionManager
            if (SelectionManager.Instance == null)
            {
                sb.AppendLine("  X SelectionManager missing");
                valid = false;
            }
            else
            {
                sb.AppendLine("  OK SelectionManager");
            }

            // Check NavMesh
            NavMeshHit hit;
            if (NavMesh.SamplePosition(Vector3.zero, out hit, 1f, NavMesh.AllAreas))
            {
                sb.AppendLine("  OK NavMesh present");
            }
            else
            {
                sb.AppendLine("  X NavMesh not baked");
                valid = false;
            }

            // Check prefab
            if (AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath) == null)
            {
                sb.AppendLine("  X Debug unit prefab missing");
                valid = false;
            }
            else
            {
                sb.AppendLine("  OK Debug unit prefab");
            }

            // Check archetype
            if (AssetDatabase.LoadAssetAtPath<UnitArchetypeSO>(ArchetypePath) == null)
            {
                sb.AppendLine("  X Debug unit archetype missing");
                valid = false;
            }
            else
            {
                sb.AppendLine("  OK Debug unit archetype");
            }

            sb.AppendLine(valid ? "Validation PASSED" : "Validation FAILED");
            Debug.Log(sb.ToString());

            return valid;
        }

        [MenuItem("Relic/Demo/Validate Demo Setup")]
        public static void ValidateSetupMenu()
        {
            ValidateSetup();
        }
    }
}
