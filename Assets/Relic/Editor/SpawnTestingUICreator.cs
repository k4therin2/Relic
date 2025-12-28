#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using Relic.CoreRTS;
using Relic.UILayer;
using System.Linq;

namespace Relic.Editor
{
    /// <summary>
    /// Editor utility to create a spawn testing UI in the current scene.
    /// Run via menu: Relic/Debug/Create Spawn Testing UI
    /// </summary>
    public static class SpawnTestingUICreator
    {
        [MenuItem("Relic/Debug/Create Spawn Testing UI")]
        public static void CreateSpawnTestingUI()
        {
            // Find existing canvas or create one
            Canvas canvas = Object.FindFirstObjectByType<Canvas>();
            if (canvas == null)
            {
                var canvasGO = new GameObject("SpawnTestCanvas");
                canvas = canvasGO.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvasGO.AddComponent<CanvasScaler>();
                canvasGO.AddComponent<GraphicRaycaster>();

                // Ensure EventSystem exists
                if (Object.FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
                {
                    var eventSystemGO = new GameObject("EventSystem");
                    eventSystemGO.AddComponent<UnityEngine.EventSystems.EventSystem>();
                    eventSystemGO.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
                }
            }

            // Create UI Panel
            var panelGO = new GameObject("SpawnTestingPanel");
            panelGO.transform.SetParent(canvas.transform);
            var panelRect = panelGO.AddComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0, 1);
            panelRect.anchorMax = new Vector2(0, 1);
            panelRect.pivot = new Vector2(0, 1);
            panelRect.anchoredPosition = new Vector2(10, -10);
            panelRect.sizeDelta = new Vector2(250, 200);

            var panelImage = panelGO.AddComponent<Image>();
            panelImage.color = new Color(0.1f, 0.1f, 0.1f, 0.8f);

            // Add SpawnTestingUI component
            var spawnUI = panelGO.AddComponent<SpawnTestingUI>();

            // Create Title
            CreateText(panelGO.transform, "Title", "Spawn Testing", new Vector2(125, -15), 16);

            // Create Archetype Dropdown
            var dropdown = CreateDropdown(panelGO.transform, "ArchetypeDropdown", new Vector2(125, -45));
            SetPrivateField(spawnUI, "_archetypeDropdown", dropdown);

            // Create Spawn Red Button
            var redButton = CreateButton(panelGO.transform, "SpawnRedButton", "Spawn Red", new Vector2(65, -85), new Color(0.8f, 0.2f, 0.2f, 1f));
            SetPrivateField(spawnUI, "_spawnRedButton", redButton);

            // Create Spawn Blue Button
            var blueButton = CreateButton(panelGO.transform, "SpawnBlueButton", "Spawn Blue", new Vector2(185, -85), new Color(0.2f, 0.2f, 0.8f, 1f));
            SetPrivateField(spawnUI, "_spawnBlueButton", blueButton);

            // Create Clear All Button
            var clearButton = CreateButton(panelGO.transform, "ClearAllButton", "Clear All", new Vector2(125, -125), new Color(0.5f, 0.5f, 0.5f, 1f));
            SetPrivateField(spawnUI, "_clearAllButton", clearButton);

            // Create Unit Count Text
            var countText = CreateText(panelGO.transform, "UnitCountText", "Red: 0 | Blue: 0 | Total: 0", new Vector2(125, -165), 12);
            SetPrivateField(spawnUI, "_unitCountText", countText.GetComponent<Text>());

            // Try to find and connect references
            ConnectReferences(spawnUI);

            Selection.activeGameObject = panelGO;
            Debug.Log("Created Spawn Testing UI. Connect archetypes in inspector.");
        }

        private static void ConnectReferences(SpawnTestingUI spawnUI)
        {
            // Find UnitFactory
            var factory = Object.FindFirstObjectByType<UnitFactory>();
            if (factory != null)
            {
                SetPrivateField(spawnUI, "_unitFactory", factory);
                Debug.Log("Connected UnitFactory");
            }
            else
            {
                Debug.LogWarning("UnitFactory not found in scene. Please add one to BattlefieldRoot.");
            }

            // Find SpawnPoints
            var spawnPoints = Object.FindObjectsByType<SpawnPoint>(FindObjectsSortMode.None);
            foreach (var sp in spawnPoints)
            {
                if (sp.IsRedTeam)
                {
                    SetPrivateField(spawnUI, "_redSpawnPoint", sp);
                    Debug.Log($"Connected Red SpawnPoint: {sp.name}");
                }
                else if (sp.IsBlueTeam)
                {
                    SetPrivateField(spawnUI, "_blueSpawnPoint", sp);
                    Debug.Log($"Connected Blue SpawnPoint: {sp.name}");
                }
            }

            // Find archetypes
            string[] guids = AssetDatabase.FindAssets("t:UnitArchetypeSO");
            var archetypes = guids
                .Select(guid => AssetDatabase.LoadAssetAtPath<UnitArchetypeSO>(AssetDatabase.GUIDToAssetPath(guid)))
                .Where(a => a != null)
                .ToList();

            if (archetypes.Count > 0)
            {
                SetPrivateField(spawnUI, "_archetypes", archetypes);
                Debug.Log($"Found {archetypes.Count} archetypes");
            }
            else
            {
                Debug.LogWarning("No UnitArchetypeSO assets found. Create some using Relic > Create Sample Archetypes.");
            }
        }

        private static GameObject CreateText(Transform parent, string name, string text, Vector2 position, int fontSize)
        {
            var textGO = new GameObject(name);
            textGO.transform.SetParent(parent);

            var rect = textGO.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 1);
            rect.anchorMax = new Vector2(0.5f, 1);
            rect.pivot = new Vector2(0.5f, 1);
            rect.anchoredPosition = position;
            rect.sizeDelta = new Vector2(230, 30);

            var textComponent = textGO.AddComponent<Text>();
            textComponent.text = text;
            textComponent.fontSize = fontSize;
            textComponent.alignment = TextAnchor.MiddleCenter;
            textComponent.color = Color.white;
            textComponent.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            return textGO;
        }

        private static Button CreateButton(Transform parent, string name, string text, Vector2 position, Color color)
        {
            var buttonGO = new GameObject(name);
            buttonGO.transform.SetParent(parent);

            var rect = buttonGO.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 1);
            rect.anchorMax = new Vector2(0.5f, 1);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = new Vector2(100, 30);

            var image = buttonGO.AddComponent<Image>();
            image.color = color;

            var button = buttonGO.AddComponent<Button>();

            // Create button text
            var textGO = new GameObject("Text");
            textGO.transform.SetParent(buttonGO.transform);

            var textRect = textGO.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.sizeDelta = Vector2.zero;
            textRect.anchoredPosition = Vector2.zero;

            var textComponent = textGO.AddComponent<Text>();
            textComponent.text = text;
            textComponent.fontSize = 14;
            textComponent.alignment = TextAnchor.MiddleCenter;
            textComponent.color = Color.white;
            textComponent.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            return button;
        }

        private static Dropdown CreateDropdown(Transform parent, string name, Vector2 position)
        {
            var dropdownGO = new GameObject(name);
            dropdownGO.transform.SetParent(parent);

            var rect = dropdownGO.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 1);
            rect.anchorMax = new Vector2(0.5f, 1);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = new Vector2(200, 30);

            var image = dropdownGO.AddComponent<Image>();
            image.color = new Color(0.3f, 0.3f, 0.3f, 1f);

            var dropdown = dropdownGO.AddComponent<Dropdown>();

            // Create label
            var labelGO = new GameObject("Label");
            labelGO.transform.SetParent(dropdownGO.transform);
            var labelRect = labelGO.AddComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = new Vector2(10, 6);
            labelRect.offsetMax = new Vector2(-25, -7);
            var labelText = labelGO.AddComponent<Text>();
            labelText.alignment = TextAnchor.MiddleLeft;
            labelText.color = Color.white;
            labelText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            dropdown.captionText = labelText;

            // Create template (simplified for auto-generation)
            var templateGO = new GameObject("Template");
            templateGO.transform.SetParent(dropdownGO.transform);
            templateGO.SetActive(false);
            var templateRect = templateGO.AddComponent<RectTransform>();
            templateRect.anchorMin = new Vector2(0, 0);
            templateRect.anchorMax = new Vector2(1, 0);
            templateRect.pivot = new Vector2(0.5f, 1);
            templateRect.anchoredPosition = Vector2.zero;
            templateRect.sizeDelta = new Vector2(0, 150);
            templateGO.AddComponent<Image>().color = new Color(0.2f, 0.2f, 0.2f, 1f);
            var scrollRect = templateGO.AddComponent<ScrollRect>();

            // Create viewport
            var viewportGO = new GameObject("Viewport");
            viewportGO.transform.SetParent(templateGO.transform);
            var viewportRect = viewportGO.AddComponent<RectTransform>();
            viewportRect.anchorMin = Vector2.zero;
            viewportRect.anchorMax = Vector2.one;
            viewportRect.sizeDelta = Vector2.zero;
            viewportRect.pivot = new Vector2(0, 1);
            viewportGO.AddComponent<Image>().color = new Color(0.2f, 0.2f, 0.2f, 1f);
            viewportGO.AddComponent<Mask>().showMaskGraphic = false;
            scrollRect.viewport = viewportRect;

            // Create content
            var contentGO = new GameObject("Content");
            contentGO.transform.SetParent(viewportGO.transform);
            var contentRect = contentGO.AddComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0, 1);
            contentRect.anchorMax = new Vector2(1, 1);
            contentRect.pivot = new Vector2(0.5f, 1);
            contentRect.sizeDelta = new Vector2(0, 28);
            scrollRect.content = contentRect;

            // Create item
            var itemGO = new GameObject("Item");
            itemGO.transform.SetParent(contentGO.transform);
            var itemRect = itemGO.AddComponent<RectTransform>();
            itemRect.anchorMin = new Vector2(0, 0.5f);
            itemRect.anchorMax = new Vector2(1, 0.5f);
            itemRect.sizeDelta = new Vector2(0, 28);
            var toggle = itemGO.AddComponent<Toggle>();

            // Create item background
            var itemBgGO = new GameObject("Item Background");
            itemBgGO.transform.SetParent(itemGO.transform);
            var itemBgRect = itemBgGO.AddComponent<RectTransform>();
            itemBgRect.anchorMin = Vector2.zero;
            itemBgRect.anchorMax = Vector2.one;
            itemBgRect.sizeDelta = Vector2.zero;
            var itemBgImage = itemBgGO.AddComponent<Image>();
            itemBgImage.color = new Color(0.3f, 0.3f, 0.3f, 1f);
            toggle.targetGraphic = itemBgImage;

            // Create item label
            var itemLabelGO = new GameObject("Item Label");
            itemLabelGO.transform.SetParent(itemGO.transform);
            var itemLabelRect = itemLabelGO.AddComponent<RectTransform>();
            itemLabelRect.anchorMin = Vector2.zero;
            itemLabelRect.anchorMax = Vector2.one;
            itemLabelRect.offsetMin = new Vector2(10, 2);
            itemLabelRect.offsetMax = new Vector2(-10, -2);
            var itemLabelText = itemLabelGO.AddComponent<Text>();
            itemLabelText.alignment = TextAnchor.MiddleLeft;
            itemLabelText.color = Color.white;
            itemLabelText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            dropdown.template = templateRect;
            dropdown.itemText = itemLabelText;

            return dropdown;
        }

        private static void SetPrivateField<T>(object obj, string fieldName, T value)
        {
            var field = obj.GetType().GetField(fieldName,
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            field?.SetValue(obj, value);
        }
    }
}
#endif
