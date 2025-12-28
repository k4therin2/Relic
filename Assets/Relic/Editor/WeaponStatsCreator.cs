using UnityEngine;
using UnityEditor;
using System.IO;

namespace Relic.Editor
{
    /// <summary>
    /// Editor utility to create sample weapons for each era.
    /// Creates 4 weapons: Bow (Ancient), Crossbow (Medieval), Rifle (WWII), Laser (Future).
    /// </summary>
    public static class WeaponStatsCreator
    {
        private const string WEAPONS_PATH = "Assets/Relic/Data/Weapons";

        [MenuItem("Relic/Create Sample Weapons")]
        public static void CreateSampleWeapons()
        {
            // Ensure the directory exists
            EnsureDirectoryExists(WEAPONS_PATH);

            // Create the 4 sample weapons
            CreateAncientBow();
            CreateMedievalCrossbow();
            CreateWWIIRifle();
            CreateFutureLaser();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"Created 4 sample weapons in {WEAPONS_PATH}");
        }

        private static void EnsureDirectoryExists(string path)
        {
            if (!AssetDatabase.IsValidFolder(path))
            {
                // Create parent directories if needed
                string parentPath = Path.GetDirectoryName(path);
                if (!AssetDatabase.IsValidFolder(parentPath))
                {
                    EnsureDirectoryExists(parentPath);
                }
                string folderName = Path.GetFileName(path);
                AssetDatabase.CreateFolder(parentPath, folderName);
            }
        }

        private static void CreateAncientBow()
        {
            var weapon = ScriptableObject.CreateInstance<CoreRTS.WeaponStatsSO>();

            // Use SerializedObject to set private fields
            var serializedObject = new SerializedObject(weapon);

            serializedObject.FindProperty("_id").stringValue = "ancient_bow";
            serializedObject.FindProperty("_displayName").stringValue = "Bow";
            serializedObject.FindProperty("_description").stringValue = "A simple but effective ranged weapon. Slow to fire but deals significant damage at medium range.";
            serializedObject.FindProperty("_shotsPerBurst").intValue = 1;
            serializedObject.FindProperty("_fireRate").floatValue = 0.5f; // Slow: 1 shot every 2 seconds
            serializedObject.FindProperty("_baseHitChance").floatValue = 0.6f;
            serializedObject.FindProperty("_baseDamage").floatValue = 25f;
            serializedObject.FindProperty("_effectiveRange").floatValue = 15f;
            serializedObject.FindProperty("_maxRange").floatValue = 25f;

            // Custom range curve for bow (good at medium range)
            var rangeCurve = new AnimationCurve(
                new Keyframe(0f, 0.8f),    // 80% at point blank (awkward)
                new Keyframe(0.5f, 1f),     // 100% at half effective range (sweet spot)
                new Keyframe(1f, 0.6f),     // 60% at effective range
                new Keyframe(1.5f, 0.2f)    // 20% beyond effective range
            );
            serializedObject.FindProperty("_rangeHitCurve").animationCurveValue = rangeCurve;

            serializedObject.ApplyModifiedPropertiesWithoutUndo();

            string assetPath = $"{WEAPONS_PATH}/Bow_Ancient.asset";
            AssetDatabase.CreateAsset(weapon, assetPath);
            Debug.Log($"Created: {assetPath}");
        }

        private static void CreateMedievalCrossbow()
        {
            var weapon = ScriptableObject.CreateInstance<CoreRTS.WeaponStatsSO>();
            var serializedObject = new SerializedObject(weapon);

            serializedObject.FindProperty("_id").stringValue = "medieval_crossbow";
            serializedObject.FindProperty("_displayName").stringValue = "Crossbow";
            serializedObject.FindProperty("_description").stringValue = "A powerful mechanical weapon. Very slow to reload but devastating accuracy and damage at long range.";
            serializedObject.FindProperty("_shotsPerBurst").intValue = 1;
            serializedObject.FindProperty("_fireRate").floatValue = 0.25f; // Very slow: 1 shot every 4 seconds
            serializedObject.FindProperty("_baseHitChance").floatValue = 0.8f; // High accuracy
            serializedObject.FindProperty("_baseDamage").floatValue = 50f; // High damage
            serializedObject.FindProperty("_effectiveRange").floatValue = 25f;
            serializedObject.FindProperty("_maxRange").floatValue = 40f;

            // Crossbow has good accuracy across range
            var rangeCurve = new AnimationCurve(
                new Keyframe(0f, 1f),
                new Keyframe(0.5f, 0.95f),
                new Keyframe(1f, 0.7f),
                new Keyframe(1.5f, 0.3f)
            );
            serializedObject.FindProperty("_rangeHitCurve").animationCurveValue = rangeCurve;

            serializedObject.ApplyModifiedPropertiesWithoutUndo();

            string assetPath = $"{WEAPONS_PATH}/Crossbow_Medieval.asset";
            AssetDatabase.CreateAsset(weapon, assetPath);
            Debug.Log($"Created: {assetPath}");
        }

        private static void CreateWWIIRifle()
        {
            var weapon = ScriptableObject.CreateInstance<CoreRTS.WeaponStatsSO>();
            var serializedObject = new SerializedObject(weapon);

            serializedObject.FindProperty("_id").stringValue = "wwii_rifle";
            serializedObject.FindProperty("_displayName").stringValue = "M1 Garand";
            serializedObject.FindProperty("_description").stringValue = "Standard infantry rifle. Good balance of fire rate, damage, and range.";
            serializedObject.FindProperty("_shotsPerBurst").intValue = 1;
            serializedObject.FindProperty("_fireRate").floatValue = 1.5f; // Medium: about 1.5 shots per second
            serializedObject.FindProperty("_baseHitChance").floatValue = 0.65f;
            serializedObject.FindProperty("_baseDamage").floatValue = 30f;
            serializedObject.FindProperty("_effectiveRange").floatValue = 30f;
            serializedObject.FindProperty("_maxRange").floatValue = 50f;

            // Standard rifle curve
            var rangeCurve = new AnimationCurve(
                new Keyframe(0f, 1f),
                new Keyframe(0.5f, 0.85f),
                new Keyframe(1f, 0.5f),
                new Keyframe(2f, 0.1f)
            );
            serializedObject.FindProperty("_rangeHitCurve").animationCurveValue = rangeCurve;

            serializedObject.ApplyModifiedPropertiesWithoutUndo();

            string assetPath = $"{WEAPONS_PATH}/Rifle_WWII.asset";
            AssetDatabase.CreateAsset(weapon, assetPath);
            Debug.Log($"Created: {assetPath}");
        }

        private static void CreateFutureLaser()
        {
            var weapon = ScriptableObject.CreateInstance<CoreRTS.WeaponStatsSO>();
            var serializedObject = new SerializedObject(weapon);

            serializedObject.FindProperty("_id").stringValue = "future_laser";
            serializedObject.FindProperty("_displayName").stringValue = "Pulse Laser";
            serializedObject.FindProperty("_description").stringValue = "Advanced energy weapon. Rapid fire with excellent accuracy, but lower damage per hit.";
            serializedObject.FindProperty("_shotsPerBurst").intValue = 3; // 3-shot burst
            serializedObject.FindProperty("_fireRate").floatValue = 5f; // Fast: 5 shots per second
            serializedObject.FindProperty("_baseHitChance").floatValue = 0.85f; // Excellent accuracy
            serializedObject.FindProperty("_baseDamage").floatValue = 10f; // Lower damage per hit
            serializedObject.FindProperty("_effectiveRange").floatValue = 35f;
            serializedObject.FindProperty("_maxRange").floatValue = 60f;

            // Laser has great accuracy at all ranges
            var rangeCurve = new AnimationCurve(
                new Keyframe(0f, 1f),
                new Keyframe(0.5f, 0.95f),
                new Keyframe(1f, 0.8f),
                new Keyframe(1.5f, 0.5f),
                new Keyframe(2f, 0.2f)
            );
            serializedObject.FindProperty("_rangeHitCurve").animationCurveValue = rangeCurve;

            serializedObject.ApplyModifiedPropertiesWithoutUndo();

            string assetPath = $"{WEAPONS_PATH}/Laser_Future.asset";
            AssetDatabase.CreateAsset(weapon, assetPath);
            Debug.Log($"Created: {assetPath}");
        }
    }
}
