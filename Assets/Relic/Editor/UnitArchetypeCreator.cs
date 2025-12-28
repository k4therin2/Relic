using UnityEngine;
using UnityEditor;
using Relic.CoreRTS;

namespace Relic.Editor
{
    /// <summary>
    /// Editor utility for creating sample unit archetypes for each era.
    /// Run via menu: Relic > Create Sample Archetypes
    /// </summary>
    public static class UnitArchetypeCreator
    {
        private const string ARCHETYPES_PATH = "Assets/Relic/Configs/UnitArchetypes";

        [MenuItem("Relic/Create Sample Archetypes")]
        public static void CreateSampleArchetypes()
        {
            // Ensure directory exists
            if (!AssetDatabase.IsValidFolder(ARCHETYPES_PATH))
            {
                AssetDatabase.CreateFolder("Assets/Relic/Configs", "UnitArchetypes");
            }

            // Create archetypes for each era
            CreateArchetype(
                id: "ancient_legionnaire",
                displayName: "Legionnaire",
                description: "Roman infantry soldier. Well-armored and disciplined.",
                maxHealth: 100,
                moveSpeed: 2.5f,
                armor: 20,
                detectionRange: 8f
            );

            CreateArchetype(
                id: "medieval_knight",
                displayName: "Knight",
                description: "Heavy cavalry. Slow but powerful and well-armored.",
                maxHealth: 150,
                moveSpeed: 4f,
                armor: 40,
                detectionRange: 10f
            );

            CreateArchetype(
                id: "wwii_rifleman",
                displayName: "Rifleman",
                description: "Standard infantry soldier with M1 Garand rifle.",
                maxHealth: 80,
                moveSpeed: 3f,
                armor: 5,
                detectionRange: 15f
            );

            CreateArchetype(
                id: "future_drone",
                displayName: "Combat Drone",
                description: "Autonomous combat unit. Fast and agile with energy shields.",
                maxHealth: 60,
                moveSpeed: 5f,
                armor: 10,
                detectionRange: 20f
            );

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("[UnitArchetypeCreator] Created 4 sample archetypes in " + ARCHETYPES_PATH);
        }

        private static void CreateArchetype(
            string id,
            string displayName,
            string description,
            int maxHealth,
            float moveSpeed,
            int armor,
            float detectionRange)
        {
            string path = $"{ARCHETYPES_PATH}/{id}.asset";

            // Check if already exists
            var existing = AssetDatabase.LoadAssetAtPath<UnitArchetypeSO>(path);
            if (existing != null)
            {
                Debug.Log($"[UnitArchetypeCreator] Archetype already exists: {id}");
                return;
            }

            // Create new archetype
            var archetype = ScriptableObject.CreateInstance<UnitArchetypeSO>();

            // Use serialized property to set private fields
            var so = new SerializedObject(archetype);
            so.FindProperty("_id").stringValue = id;
            so.FindProperty("_displayName").stringValue = displayName;
            so.FindProperty("_description").stringValue = description;
            so.FindProperty("_maxHealth").intValue = maxHealth;
            so.FindProperty("_moveSpeed").floatValue = moveSpeed;
            so.FindProperty("_armor").intValue = armor;
            so.FindProperty("_detectionRange").floatValue = detectionRange;
            so.ApplyModifiedPropertiesWithoutUndo();

            AssetDatabase.CreateAsset(archetype, path);
            Debug.Log($"[UnitArchetypeCreator] Created archetype: {displayName} ({id})");
        }

        [MenuItem("Relic/Validate Archetypes")]
        public static void ValidateAllArchetypes()
        {
            var guids = AssetDatabase.FindAssets("t:UnitArchetypeSO", new[] { ARCHETYPES_PATH });
            int valid = 0;
            int invalid = 0;

            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var archetype = AssetDatabase.LoadAssetAtPath<UnitArchetypeSO>(path);

                if (archetype != null)
                {
                    if (archetype.Validate(out var errors))
                    {
                        valid++;
                        Debug.Log($"[Validate] OK: {archetype.DisplayName}");
                    }
                    else
                    {
                        invalid++;
                        Debug.LogWarning($"[Validate] INVALID: {path}");
                        foreach (var error in errors)
                        {
                            Debug.LogWarning($"  - {error}");
                        }
                    }
                }
            }

            Debug.Log($"[Validate] Results: {valid} valid, {invalid} invalid");
        }
    }
}
