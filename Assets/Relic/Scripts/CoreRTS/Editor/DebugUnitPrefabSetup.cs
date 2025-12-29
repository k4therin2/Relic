using UnityEngine;
using UnityEditor;
using UnityEngine.AI;

namespace Relic.CoreRTS.Editor
{
    /// <summary>
    /// Editor utility to create debug unit prefabs for flat debug scene.
    /// Creates simple capsule-based units with UnitController and TeamColorApplier.
    /// </summary>
    /// <remarks>
    /// See WP-EXT-6.2 for requirements.
    /// </remarks>
    public static class DebugUnitPrefabSetup
    {
        private const string PrefabFolderPath = "Assets/Prefabs/Debug";
        private const string ArchetypeFolderPath = "Assets/Data/Archetypes";
        private const string DebugUnitPrefabName = "DebugUnit.prefab";
        private const string DebugArchetypeName = "DebugUnitArchetype.asset";

        [MenuItem("Relic/Debug/Create Debug Unit Prefab")]
        public static void CreateDebugUnitPrefab()
        {
            // Ensure directories exist
            EnsureDirectoryExists(PrefabFolderPath);
            EnsureDirectoryExists(ArchetypeFolderPath);

            // Create the prefab
            GameObject unitGO = CreateDebugUnitGameObject();

            // Save as prefab
            string prefabPath = $"{PrefabFolderPath}/{DebugUnitPrefabName}";
            bool success;
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(unitGO, prefabPath, out success);

            // Clean up scene object
            Object.DestroyImmediate(unitGO);

            if (success)
            {
                Debug.Log($"[DebugUnitPrefabSetup] Created debug unit prefab at {prefabPath}");

                // Create matching archetype
                CreateDebugUnitArchetype(prefab);

                // Select the prefab in project window
                Selection.activeObject = prefab;
            }
            else
            {
                Debug.LogError("[DebugUnitPrefabSetup] Failed to create debug unit prefab.");
            }
        }

        [MenuItem("Relic/Debug/Create Debug Unit Archetype")]
        public static void CreateDebugUnitArchetypeMenu()
        {
            // Find the prefab
            string prefabPath = $"{PrefabFolderPath}/{DebugUnitPrefabName}";
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);

            if (prefab == null)
            {
                Debug.LogWarning("[DebugUnitPrefabSetup] Debug unit prefab not found. Creating it first...");
                CreateDebugUnitPrefab();
                prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            }

            if (prefab != null)
            {
                CreateDebugUnitArchetype(prefab);
            }
        }

        private static GameObject CreateDebugUnitGameObject()
        {
            // Create root object
            GameObject unitGO = new GameObject("DebugUnit");

            // Create visual mesh (capsule)
            GameObject capsule = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            capsule.name = "Visual";
            capsule.transform.SetParent(unitGO.transform);
            capsule.transform.localPosition = new Vector3(0f, 1f, 0f); // Capsule is 2 units tall, centered at 1
            capsule.transform.localScale = new Vector3(0.5f, 0.5f, 0.5f);

            // Remove the default collider on the capsule (we'll add one to the root)
            Object.DestroyImmediate(capsule.GetComponent<Collider>());

            // Add capsule collider to root
            CapsuleCollider collider = unitGO.AddComponent<CapsuleCollider>();
            collider.center = new Vector3(0f, 1f, 0f);
            collider.radius = 0.25f;
            collider.height = 1f;

            // Add NavMeshAgent
            NavMeshAgent agent = unitGO.AddComponent<NavMeshAgent>();
            agent.radius = 0.25f;
            agent.height = 1f;
            agent.speed = 3.5f;
            agent.angularSpeed = 360f;
            agent.acceleration = 8f;
            agent.stoppingDistance = 0.1f;

            // Add UnitController
            UnitController controller = unitGO.AddComponent<UnitController>();

            // Add TeamColorApplier
            TeamColorApplier colorApplier = unitGO.AddComponent<TeamColorApplier>();

            // Add SelectionIndicator component for visual selection feedback
            SelectionIndicator selectionIndicator = unitGO.AddComponent<SelectionIndicator>();

            // Add HealthBar spawn point (empty child above unit)
            GameObject healthBarPoint = new GameObject("HealthBarPoint");
            healthBarPoint.transform.SetParent(unitGO.transform);
            healthBarPoint.transform.localPosition = new Vector3(0f, 1.5f, 0f);

            return unitGO;
        }

        private static void CreateDebugUnitArchetype(GameObject prefab)
        {
            EnsureDirectoryExists(ArchetypeFolderPath);

            string archetypePath = $"{ArchetypeFolderPath}/{DebugArchetypeName}";

            // Check if archetype already exists
            var existingArchetype = AssetDatabase.LoadAssetAtPath<UnitArchetypeSO>(archetypePath);
            if (existingArchetype != null)
            {
                // Update existing archetype
                SetArchetypeValues(existingArchetype, prefab);
                EditorUtility.SetDirty(existingArchetype);
                AssetDatabase.SaveAssets();
                Debug.Log($"[DebugUnitPrefabSetup] Updated debug unit archetype at {archetypePath}");
                return;
            }

            // Create new archetype
            UnitArchetypeSO archetype = ScriptableObject.CreateInstance<UnitArchetypeSO>();
            SetArchetypeValues(archetype, prefab);

            AssetDatabase.CreateAsset(archetype, archetypePath);
            AssetDatabase.SaveAssets();

            Debug.Log($"[DebugUnitPrefabSetup] Created debug unit archetype at {archetypePath}");
        }

        private static void SetArchetypeValues(UnitArchetypeSO archetype, GameObject prefab)
        {
            // Use SerializedObject for setting private fields
            SerializedObject so = new SerializedObject(archetype);

            so.FindProperty("_id").stringValue = "debug_unit";
            so.FindProperty("_displayName").stringValue = "Debug Unit";
            so.FindProperty("_description").stringValue = "Simple debug unit for testing in flat debug scene.";
            so.FindProperty("_maxHealth").intValue = 100;
            so.FindProperty("_moveSpeed").floatValue = 3.5f;
            so.FindProperty("_detectionRange").floatValue = 10f;
            so.FindProperty("_armor").intValue = 0;
            so.FindProperty("_unitPrefab").objectReferenceValue = prefab;
            so.FindProperty("_scale").floatValue = 1f;
            so.FindProperty("_heightOffset").floatValue = 0f;

            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void EnsureDirectoryExists(string path)
        {
            if (!AssetDatabase.IsValidFolder(path))
            {
                string[] parts = path.Split('/');
                string currentPath = parts[0];

                for (int i = 1; i < parts.Length; i++)
                {
                    string newPath = currentPath + "/" + parts[i];
                    if (!AssetDatabase.IsValidFolder(newPath))
                    {
                        AssetDatabase.CreateFolder(currentPath, parts[i]);
                    }
                    currentPath = newPath;
                }
            }
        }

        [MenuItem("Relic/Debug/Spawn Test Units in Scene")]
        public static void SpawnTestUnitsInScene()
        {
            // Load the prefab
            string prefabPath = $"{PrefabFolderPath}/{DebugUnitPrefabName}";
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);

            if (prefab == null)
            {
                Debug.LogWarning("[DebugUnitPrefabSetup] Debug unit prefab not found. Creating it first...");
                CreateDebugUnitPrefab();
                prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            }

            if (prefab == null)
            {
                Debug.LogError("[DebugUnitPrefabSetup] Failed to load or create debug unit prefab.");
                return;
            }

            // Load archetype
            string archetypePath = $"{ArchetypeFolderPath}/{DebugArchetypeName}";
            UnitArchetypeSO archetype = AssetDatabase.LoadAssetAtPath<UnitArchetypeSO>(archetypePath);

            // Spawn Team 0 units (left side)
            SpawnTeamUnits(prefab, archetype, 0, new Vector3(-10f, 0f, 0f), 5);

            // Spawn Team 1 units (right side)
            SpawnTeamUnits(prefab, archetype, 1, new Vector3(10f, 0f, 0f), 5);

            Debug.Log("[DebugUnitPrefabSetup] Spawned 10 test units (5 per team)");
        }

        private static void SpawnTeamUnits(GameObject prefab, UnitArchetypeSO archetype, int teamId, Vector3 centerPos, int count)
        {
            // Create parent for team units
            string parentName = $"Team{teamId}Units";
            GameObject parent = GameObject.Find(parentName);
            if (parent == null)
            {
                parent = new GameObject(parentName);
            }

            for (int i = 0; i < count; i++)
            {
                // Calculate position (grid layout)
                float xOffset = (i % 3) * 2f - 2f;
                float zOffset = (i / 3) * 2f;
                Vector3 spawnPos = centerPos + new Vector3(xOffset, 0f, zOffset);

                // Instantiate unit
                GameObject unit = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                unit.name = $"DebugUnit_Team{teamId}_{i}";
                unit.transform.position = spawnPos;
                unit.transform.SetParent(parent.transform);

                // Initialize unit controller with team
                UnitController controller = unit.GetComponent<UnitController>();
                if (controller != null && archetype != null)
                {
                    controller.Initialize(archetype, teamId);
                }

                // Apply team color
                TeamColorApplier colorApplier = unit.GetComponent<TeamColorApplier>();
                if (colorApplier != null)
                {
                    colorApplier.Initialize();
                }

                // Register for undo
                Undo.RegisterCreatedObjectUndo(unit, "Spawn Debug Unit");
            }
        }
    }
}
