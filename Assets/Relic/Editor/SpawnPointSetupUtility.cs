#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using Relic.CoreRTS;

namespace Relic.Editor
{
    /// <summary>
    /// Editor utility to set up spawn points in battlefield prefabs.
    /// Run via menu: Relic/Setup/Add SpawnPoint Components
    /// </summary>
    public static class SpawnPointSetupUtility
    {
        private const string PrefabsPath = "Assets/Relic/Prefabs/";

        [MenuItem("Relic/Setup/Add SpawnPoint Components to Battlefield")]
        public static void AddSpawnPointsToBattlefield()
        {
            string prefabPath = PrefabsPath + "BattlefieldRoot.prefab";
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);

            if (prefab == null)
            {
                Debug.LogError($"Battlefield prefab not found at {prefabPath}. Run 'Relic/Setup/Create Battlefield Prefabs' first.");
                return;
            }

            // Open the prefab for editing
            string assetPath = AssetDatabase.GetAssetPath(prefab);
            var prefabRoot = PrefabUtility.LoadPrefabContents(assetPath);

            try
            {
                // Find spawn points container
                Transform spawnPoints = prefabRoot.transform.Find("SpawnPoints");
                if (spawnPoints == null)
                {
                    Debug.LogError("SpawnPoints container not found in battlefield prefab.");
                    return;
                }

                // Add SpawnPoint component to RedSpawn
                Transform redSpawn = spawnPoints.Find("RedSpawn");
                if (redSpawn != null)
                {
                    var redSpawnPoint = redSpawn.GetComponent<SpawnPoint>();
                    if (redSpawnPoint == null)
                    {
                        redSpawnPoint = redSpawn.gameObject.AddComponent<SpawnPoint>();
                    }
                    SetSpawnPointFields(redSpawnPoint, SpawnPoint.TEAM_RED, 1f);
                    // Face toward center (right)
                    redSpawn.rotation = Quaternion.Euler(0, 90, 0);
                    Debug.Log("Added SpawnPoint component to RedSpawn");
                }

                // Add SpawnPoint component to BlueSpawn
                Transform blueSpawn = spawnPoints.Find("BlueSpawn");
                if (blueSpawn != null)
                {
                    var blueSpawnPoint = blueSpawn.GetComponent<SpawnPoint>();
                    if (blueSpawnPoint == null)
                    {
                        blueSpawnPoint = blueSpawn.gameObject.AddComponent<SpawnPoint>();
                    }
                    SetSpawnPointFields(blueSpawnPoint, SpawnPoint.TEAM_BLUE, 1f);
                    // Face toward center (left)
                    blueSpawn.rotation = Quaternion.Euler(0, -90, 0);
                    Debug.Log("Added SpawnPoint component to BlueSpawn");
                }

                // Add UnitFactory to battlefield root
                var factory = prefabRoot.GetComponent<UnitFactory>();
                if (factory == null)
                {
                    factory = prefabRoot.AddComponent<UnitFactory>();
                    Debug.Log("Added UnitFactory component to BattlefieldRoot");
                }

                // Create Units container for spawned units
                Transform unitsContainer = prefabRoot.transform.Find("Units");
                if (unitsContainer == null)
                {
                    var units = new GameObject("Units");
                    units.transform.SetParent(prefabRoot.transform);
                    units.transform.localPosition = Vector3.zero;
                    unitsContainer = units.transform;
                    Debug.Log("Created Units container");
                }

                // Set factory's units parent via reflection
                SetFactoryUnitsParent(factory, unitsContainer);

                // Save the prefab
                PrefabUtility.SaveAsPrefabAsset(prefabRoot, assetPath);
                Debug.Log("SpawnPointSetupUtility: SpawnPoint components added to battlefield prefab!");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(prefabRoot);
            }
        }

        [MenuItem("Relic/Setup/Create Spawn Test Scene Objects")]
        public static void CreateSpawnTestSceneObjects()
        {
            // Create a simple battlefield in the current scene for testing

            // Create Battlefield Root
            var battlefield = new GameObject("BattlefieldRoot");
            battlefield.AddComponent<UnitFactory>();

            // Create Ground
            var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "Ground";
            ground.transform.SetParent(battlefield.transform);
            ground.transform.localPosition = Vector3.zero;
            ground.transform.localScale = new Vector3(2, 1, 2);

            // Create SpawnPoints container
            var spawnPoints = new GameObject("SpawnPoints");
            spawnPoints.transform.SetParent(battlefield.transform);

            // Create Red Spawn
            var redSpawn = new GameObject("RedSpawn");
            redSpawn.transform.SetParent(spawnPoints.transform);
            redSpawn.transform.localPosition = new Vector3(-8, 0, 0);
            redSpawn.transform.rotation = Quaternion.Euler(0, 90, 0);
            var redPoint = redSpawn.AddComponent<SpawnPoint>();
            SetSpawnPointFields(redPoint, SpawnPoint.TEAM_RED, 1.5f);

            // Create Blue Spawn
            var blueSpawn = new GameObject("BlueSpawn");
            blueSpawn.transform.SetParent(spawnPoints.transform);
            blueSpawn.transform.localPosition = new Vector3(8, 0, 0);
            blueSpawn.transform.rotation = Quaternion.Euler(0, -90, 0);
            var bluePoint = blueSpawn.AddComponent<SpawnPoint>();
            SetSpawnPointFields(bluePoint, SpawnPoint.TEAM_BLUE, 1.5f);

            // Create Units container
            var units = new GameObject("Units");
            units.transform.SetParent(battlefield.transform);

            // Set up factory
            var factory = battlefield.GetComponent<UnitFactory>();
            SetFactoryUnitsParent(factory, units.transform);

            Debug.Log("Created spawn test scene objects. Select BattlefieldRoot to view spawn points in Scene view.");

            Selection.activeGameObject = battlefield;
        }

        private static void SetSpawnPointFields(SpawnPoint spawnPoint, int teamId, float radius)
        {
            var teamField = typeof(SpawnPoint).GetField("_teamId",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            teamField?.SetValue(spawnPoint, teamId);

            var radiusField = typeof(SpawnPoint).GetField("_spawnRadius",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            radiusField?.SetValue(spawnPoint, radius);
        }

        private static void SetFactoryUnitsParent(UnitFactory factory, Transform parent)
        {
            var field = typeof(UnitFactory).GetField("_unitsParent",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            field?.SetValue(factory, parent);
        }
    }
}
#endif
