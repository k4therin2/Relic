#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using Relic.CoreRTS;

namespace Relic.Editor
{
    /// <summary>
    /// Editor utility to set up selection system components in scenes.
    /// Run via menu: Relic/Setup/Add Selection System
    /// </summary>
    public static class SelectionSystemSetupUtility
    {
        [MenuItem("Relic/Setup/Add Selection System (Debug Mode)")]
        public static void AddDebugSelectionSystem()
        {
            // Check if SelectionManager exists
            var existingManager = Object.FindFirstObjectByType<SelectionManager>();
            if (existingManager != null)
            {
                Debug.Log("SelectionManager already exists in scene.");
            }
            else
            {
                var managerGO = new GameObject("SelectionManager");
                managerGO.AddComponent<SelectionManager>();
                Debug.Log("Added SelectionManager to scene.");
            }

            // Check if DebugSelectionController exists
            var existingController = Object.FindFirstObjectByType<DebugSelectionController>();
            if (existingController != null)
            {
                Debug.Log("DebugSelectionController already exists in scene.");
            }
            else
            {
                // Attach to main camera if exists
                var mainCamera = Camera.main;
                if (mainCamera != null)
                {
                    mainCamera.gameObject.AddComponent<DebugSelectionController>();
                    Debug.Log("Added DebugSelectionController to Main Camera.");
                }
                else
                {
                    var controllerGO = new GameObject("DebugSelectionController");
                    controllerGO.AddComponent<DebugSelectionController>();
                    Debug.Log("Added DebugSelectionController to scene (no Main Camera found).");
                }
            }

            Debug.Log("Selection system setup complete for Debug mode.");
        }

        [MenuItem("Relic/Setup/Add Selection System (AR Mode)")]
        public static void AddARSelectionSystem()
        {
            // Check if SelectionManager exists
            var existingManager = Object.FindFirstObjectByType<SelectionManager>();
            if (existingManager != null)
            {
                Debug.Log("SelectionManager already exists in scene.");
            }
            else
            {
                var managerGO = new GameObject("SelectionManager");
                managerGO.AddComponent<SelectionManager>();
                Debug.Log("Added SelectionManager to scene.");
            }

            // AR controller will need to be attached to the XR controller manually
            Debug.Log("For AR mode, attach ARSelectionController to your XR controller prefab.");
            Debug.Log("Selection system setup complete. Configure ARSelectionController manually.");
        }

        [MenuItem("Relic/Setup/Add Selection Indicator to Selected")]
        public static void AddSelectionIndicatorToSelected()
        {
            var selected = Selection.gameObjects;
            int count = 0;

            foreach (var go in selected)
            {
                var unit = go.GetComponent<UnitController>();
                if (unit == null)
                {
                    unit = go.GetComponentInParent<UnitController>();
                }

                if (unit != null)
                {
                    var indicator = unit.GetComponent<SelectionIndicator>();
                    if (indicator == null)
                    {
                        indicator = unit.gameObject.AddComponent<SelectionIndicator>();
                        count++;
                    }
                }
            }

            if (count > 0)
            {
                Debug.Log($"Added SelectionIndicator to {count} units.");
            }
            else
            {
                Debug.LogWarning("No UnitController components found in selection.");
            }
        }

        [MenuItem("Relic/Setup/Add Selection Indicator to All Units")]
        public static void AddSelectionIndicatorToAllUnits()
        {
            var allUnits = Object.FindObjectsByType<UnitController>(FindObjectsSortMode.None);
            int count = 0;

            foreach (var unit in allUnits)
            {
                var indicator = unit.GetComponent<SelectionIndicator>();
                if (indicator == null)
                {
                    unit.gameObject.AddComponent<SelectionIndicator>();
                    count++;
                }
            }

            Debug.Log($"Added SelectionIndicator to {count} units (total: {allUnits.Length}).");
        }
    }
}
#endif
