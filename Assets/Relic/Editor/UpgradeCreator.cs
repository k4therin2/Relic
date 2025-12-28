using UnityEngine;
using UnityEditor;
using Relic.CoreRTS;

namespace Relic.Editor
{
    /// <summary>
    /// Editor tool for creating sample squad upgrades.
    /// Creates 8 upgrades (2 per era) with balanced stats.
    /// </summary>
    public static class UpgradeCreator
    {
        private const string UPGRADE_FOLDER = "Assets/Relic/Configs/Upgrades";

        /// <summary>
        /// Creates all 8 sample upgrades.
        /// </summary>
        [MenuItem("Relic/Create Sample Upgrades")]
        public static void CreateAllSampleUpgrades()
        {
            EnsureFolderExists();

            // Ancient Era Upgrades
            CreateUpgrade(
                id: "ancient_veterans",
                displayName: "Veterans",
                description: "Battle-hardened soldiers with improved accuracy.",
                era: EraType.Ancient,
                hitChanceMultiplier: 1.1f,  // +10% hit chance
                damageMultiplier: 1f,
                elevationBonus: 0f,
                cost: 150
            );

            CreateUpgrade(
                id: "ancient_shield_wall",
                displayName: "Shield Wall",
                description: "Defensive formation that increases damage output through coordinated strikes.",
                era: EraType.Ancient,
                hitChanceMultiplier: 1f,
                damageMultiplier: 1.20f,  // +20% damage (conceptually from coordinated attacks)
                elevationBonus: 0f,
                cost: 200
            );

            // Medieval Era Upgrades
            CreateUpgrade(
                id: "medieval_heavy_armor",
                displayName: "Heavy Armor",
                description: "Thick plate armor allows soldiers to stand and deliver heavier blows.",
                era: EraType.Medieval,
                hitChanceMultiplier: 1f,
                damageMultiplier: 1.15f,  // +15% damage
                elevationBonus: 0f,
                cost: 175
            );

            CreateUpgrade(
                id: "medieval_marksmen",
                displayName: "Marksmen",
                description: "Elite archers trained for precision shooting.",
                era: EraType.Medieval,
                hitChanceMultiplier: 1.15f,  // +15% hit chance
                damageMultiplier: 1f,
                elevationBonus: 0f,
                cost: 175
            );

            // WWII Era Upgrades
            CreateUpgrade(
                id: "wwii_elite_training",
                displayName: "Elite Training",
                description: "Special forces training for improved marksmanship.",
                era: EraType.WWII,
                hitChanceMultiplier: 1.20f,  // +20% hit chance
                damageMultiplier: 1f,
                elevationBonus: 0f,
                cost: 225
            );

            CreateUpgrade(
                id: "wwii_heavy_weapons",
                displayName: "Heavy Weapons",
                description: "Upgraded weapons with increased stopping power.",
                era: EraType.WWII,
                hitChanceMultiplier: 1f,
                damageMultiplier: 1.25f,  // +25% damage
                elevationBonus: 0f,
                cost: 250
            );

            // Future Era Upgrades
            CreateUpgrade(
                id: "future_targeting_system",
                displayName: "Targeting System",
                description: "Advanced AI-assisted targeting for superior accuracy.",
                era: EraType.Future,
                hitChanceMultiplier: 1.30f,  // +30% hit chance
                damageMultiplier: 1f,
                elevationBonus: 0f,
                cost: 300
            );

            CreateUpgrade(
                id: "future_overcharge",
                displayName: "Overcharge",
                description: "Weapons run at maximum power at the cost of stability.",
                era: EraType.Future,
                hitChanceMultiplier: 0.90f,  // -10% hit chance
                damageMultiplier: 1.40f,     // +40% damage
                elevationBonus: 0f,
                cost: 275
            );

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("[UpgradeCreator] Created 8 sample upgrades in " + UPGRADE_FOLDER);
        }

        /// <summary>
        /// Creates a single upgrade asset.
        /// </summary>
        private static void CreateUpgrade(
            string id,
            string displayName,
            string description,
            EraType era,
            float hitChanceMultiplier,
            float damageMultiplier,
            float elevationBonus,
            int cost)
        {
            string assetPath = $"{UPGRADE_FOLDER}/{id}.asset";

            // Check if already exists
            var existing = AssetDatabase.LoadAssetAtPath<UpgradeSO>(assetPath);
            if (existing != null)
            {
                Debug.Log($"[UpgradeCreator] Upgrade already exists: {id}");
                return;
            }

            var upgrade = ScriptableObject.CreateInstance<UpgradeSO>();

            // Use SerializedObject to set private fields
            var serializedObject = new SerializedObject(upgrade);
            serializedObject.FindProperty("_id").stringValue = id;
            serializedObject.FindProperty("_displayName").stringValue = displayName;
            serializedObject.FindProperty("_description").stringValue = description;
            serializedObject.FindProperty("_era").enumValueIndex = (int)era;
            serializedObject.FindProperty("_hitChanceMultiplier").floatValue = hitChanceMultiplier;
            serializedObject.FindProperty("_damageMultiplier").floatValue = damageMultiplier;
            serializedObject.FindProperty("_elevationBonus").floatValue = elevationBonus;
            serializedObject.FindProperty("_cost").intValue = cost;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();

            AssetDatabase.CreateAsset(upgrade, assetPath);

            Debug.Log($"[UpgradeCreator] Created upgrade: {displayName}");
        }

        /// <summary>
        /// Ensures the upgrade folder exists.
        /// </summary>
        private static void EnsureFolderExists()
        {
            if (!AssetDatabase.IsValidFolder("Assets/Relic/Configs"))
            {
                AssetDatabase.CreateFolder("Assets/Relic", "Configs");
            }
            if (!AssetDatabase.IsValidFolder(UPGRADE_FOLDER))
            {
                AssetDatabase.CreateFolder("Assets/Relic/Configs", "Upgrades");
            }
        }

        /// <summary>
        /// Validates all existing upgrades.
        /// </summary>
        [MenuItem("Relic/Validate Upgrades")]
        public static void ValidateAllUpgrades()
        {
            var guids = AssetDatabase.FindAssets("t:UpgradeSO", new[] { UPGRADE_FOLDER });
            int validCount = 0;
            int invalidCount = 0;

            foreach (var guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var upgrade = AssetDatabase.LoadAssetAtPath<UpgradeSO>(path);

                if (upgrade == null)
                {
                    invalidCount++;
                    Debug.LogWarning($"[UpgradeCreator] Failed to load upgrade at {path}");
                    continue;
                }

                if (upgrade.Validate(out var errors))
                {
                    validCount++;
                }
                else
                {
                    invalidCount++;
                    Debug.LogWarning($"[UpgradeCreator] Invalid upgrade at {path}:\n" +
                                     string.Join("\n", errors));
                }
            }

            Debug.Log($"[UpgradeCreator] Validation complete: {validCount} valid, {invalidCount} invalid");
        }
    }
}
