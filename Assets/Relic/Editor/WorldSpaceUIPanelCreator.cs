using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;
using Relic.UILayer;
using Relic.CoreRTS;

namespace Relic.Editor
{
    /// <summary>
    /// Editor utility for creating WorldSpaceUIPanel prefab and setup.
    /// Creates a complete world-space UI with all sections configured.
    /// </summary>
    public static class WorldSpaceUIPanelCreator
    {
        private const float CANVAS_SCALE = 0.001f;
        private const float PANEL_WIDTH = 400f;
        private const float PANEL_HEIGHT = 300f;
        private const float SECTION_HEIGHT = 60f;
        private const float BUTTON_HEIGHT = 30f;
        private const float MARGIN = 10f;

        [MenuItem("Relic/Create World Space UI Panel")]
        public static void CreateWorldSpaceUIPanel()
        {
            // Create Canvas
            var canvasGO = new GameObject("WorldSpaceUICanvas");
            var canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;

            var canvasScaler = canvasGO.AddComponent<CanvasScaler>();
            canvasScaler.dynamicPixelsPerUnit = 100f;

            canvasGO.AddComponent<GraphicRaycaster>();

            // Set canvas scale for world space
            var canvasRect = canvasGO.GetComponent<RectTransform>();
            canvasRect.localScale = Vector3.one * CANVAS_SCALE;
            canvasRect.sizeDelta = new Vector2(PANEL_WIDTH, PANEL_HEIGHT);

            // Add WorldSpaceUIPanel component
            var panel = canvasGO.AddComponent<WorldSpaceUIPanel>();

            // Create background panel
            var backgroundGO = CreatePanel(canvasGO.transform, "Background", new Vector2(PANEL_WIDTH, PANEL_HEIGHT));
            var backgroundImage = backgroundGO.GetComponent<Image>();
            backgroundImage.color = new Color(0.1f, 0.1f, 0.1f, 0.9f);

            // Create sections
            float yOffset = PANEL_HEIGHT / 2 - MARGIN - SECTION_HEIGHT / 2;

            // Spawn Controls Section
            var spawnSection = CreateSection(backgroundGO.transform, "SpawnControls", yOffset);
            CreateSpawnControlsUI(spawnSection.transform, panel);
            yOffset -= SECTION_HEIGHT + MARGIN;

            // Era Selector Section
            var eraSection = CreateSection(backgroundGO.transform, "EraSelector", yOffset);
            CreateEraSelectorUI(eraSection.transform, panel);
            yOffset -= SECTION_HEIGHT + MARGIN;

            // Upgrade Panel Section
            var upgradeSection = CreateSection(backgroundGO.transform, "UpgradePanel", yOffset);
            CreateUpgradePanelUI(upgradeSection.transform, panel);
            yOffset -= SECTION_HEIGHT + MARGIN;

            // Match Controls Section
            var matchSection = CreateSection(backgroundGO.transform, "MatchControls", yOffset);
            CreateMatchControlsUI(matchSection.transform, panel);

            // Wire up section roots via SerializedObject
            var serializedPanel = new SerializedObject(panel);
            serializedPanel.FindProperty("_spawnControlsRoot").objectReferenceValue = spawnSection;
            serializedPanel.FindProperty("_eraSelectorRoot").objectReferenceValue = eraSection;
            serializedPanel.FindProperty("_upgradePanelRoot").objectReferenceValue = upgradeSection;
            serializedPanel.FindProperty("_matchControlsRoot").objectReferenceValue = matchSection;
            serializedPanel.ApplyModifiedProperties();

            // Select the created object
            Selection.activeGameObject = canvasGO;
            Undo.RegisterCreatedObjectUndo(canvasGO, "Create World Space UI Panel");

            Debug.Log("[WorldSpaceUIPanelCreator] Created WorldSpaceUIPanel. Add UnitFactory, SpawnPoints, and configure archetypes/upgrades.");
        }

        private static GameObject CreatePanel(Transform parent, string panelName, Vector2 size)
        {
            var panelGO = new GameObject(panelName);
            panelGO.transform.SetParent(parent, false);

            var rect = panelGO.AddComponent<RectTransform>();
            rect.sizeDelta = size;
            rect.anchoredPosition = Vector2.zero;

            var image = panelGO.AddComponent<Image>();
            image.color = new Color(0.2f, 0.2f, 0.2f, 0.8f);

            return panelGO;
        }

        private static GameObject CreateSection(Transform parent, string sectionName, float yPosition)
        {
            var sectionGO = new GameObject(sectionName);
            sectionGO.transform.SetParent(parent, false);

            var rect = sectionGO.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0, 0.5f);
            rect.anchorMax = new Vector2(1, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(-MARGIN * 2, SECTION_HEIGHT);
            rect.anchoredPosition = new Vector2(0, yPosition);

            var layout = sectionGO.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 5f;
            layout.padding = new RectOffset(5, 5, 5, 5);
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;

            var image = sectionGO.AddComponent<Image>();
            image.color = new Color(0.15f, 0.15f, 0.15f, 0.9f);

            return sectionGO;
        }

        private static void CreateSpawnControlsUI(Transform parent, WorldSpaceUIPanel panel)
        {
            // Label
            var labelGO = CreateLabel(parent, "Spawn:", 60f);

            // Dropdown
            var dropdownGO = CreateDropdown(parent, "ArchetypeDropdown", 100f);
            var dropdown = dropdownGO.GetComponent<TMP_Dropdown>();

            // Spawn Team 0 button
            var team0BtnGO = CreateButton(parent, "SpawnTeam0", "Team 0", 70f);
            var team0Btn = team0BtnGO.GetComponent<Button>();

            // Spawn Team 1 button
            var team1BtnGO = CreateButton(parent, "SpawnTeam1", "Team 1", 70f);
            var team1Btn = team1BtnGO.GetComponent<Button>();

            // Unit count text
            var countTextGO = CreateLabel(parent, "0 | 0", 60f);
            var countText = countTextGO.GetComponent<TextMeshProUGUI>();

            // Wire up references
            var serializedPanel = new SerializedObject(panel);
            serializedPanel.FindProperty("_archetypeDropdown").objectReferenceValue = dropdown;
            serializedPanel.FindProperty("_spawnTeam0Button").objectReferenceValue = team0Btn;
            serializedPanel.FindProperty("_spawnTeam1Button").objectReferenceValue = team1Btn;
            serializedPanel.FindProperty("_unitCountText").objectReferenceValue = countText;
            serializedPanel.ApplyModifiedProperties();
        }

        private static void CreateEraSelectorUI(Transform parent, WorldSpaceUIPanel panel)
        {
            // Label
            CreateLabel(parent, "Era:", 40f);

            // Era button container
            var containerGO = new GameObject("EraButtons");
            containerGO.transform.SetParent(parent, false);

            var containerRect = containerGO.AddComponent<RectTransform>();
            containerRect.sizeDelta = new Vector2(200f, BUTTON_HEIGHT);

            var layout = containerGO.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 5f;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;

            // Current era text
            var eraTextGO = CreateLabel(parent, "Ancient", 80f);
            var eraText = eraTextGO.GetComponent<TextMeshProUGUI>();

            // Wire up references
            var serializedPanel = new SerializedObject(panel);
            serializedPanel.FindProperty("_eraButtonContainer").objectReferenceValue = containerGO.transform;
            serializedPanel.FindProperty("_currentEraText").objectReferenceValue = eraText;
            serializedPanel.ApplyModifiedProperties();
        }

        private static void CreateUpgradePanelUI(Transform parent, WorldSpaceUIPanel panel)
        {
            // Label
            CreateLabel(parent, "Upgrades:", 70f);

            // Upgrade button container
            var containerGO = new GameObject("UpgradeButtons");
            containerGO.transform.SetParent(parent, false);

            var containerRect = containerGO.AddComponent<RectTransform>();
            containerRect.sizeDelta = new Vector2(180f, BUTTON_HEIGHT);

            var layout = containerGO.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 5f;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;

            // Squad info text
            var squadTextGO = CreateLabel(parent, "No Squad", 100f);
            var squadText = squadTextGO.GetComponent<TextMeshProUGUI>();

            // Wire up references
            var serializedPanel = new SerializedObject(panel);
            serializedPanel.FindProperty("_upgradeButtonContainer").objectReferenceValue = containerGO.transform;
            serializedPanel.FindProperty("_squadInfoText").objectReferenceValue = squadText;
            serializedPanel.ApplyModifiedProperties();
        }

        private static void CreateMatchControlsUI(Transform parent, WorldSpaceUIPanel panel)
        {
            // Reset button
            var resetBtnGO = CreateButton(parent, "ResetMatch", "Reset", 80f);
            var resetBtn = resetBtnGO.GetComponent<Button>();

            // Pause/Resume button
            var pauseBtnGO = CreateButton(parent, "PauseResume", "Pause", 80f);
            var pauseBtn = pauseBtnGO.GetComponent<Button>();
            var pauseText = pauseBtnGO.GetComponentInChildren<TextMeshProUGUI>();

            // Spacer
            var spacerGO = new GameObject("Spacer");
            spacerGO.transform.SetParent(parent, false);
            var spacerRect = spacerGO.AddComponent<RectTransform>();
            spacerRect.sizeDelta = new Vector2(100f, BUTTON_HEIGHT);
            var spacerLayout = spacerGO.AddComponent<LayoutElement>();
            spacerLayout.flexibleWidth = 1f;

            // Wire up references
            var serializedPanel = new SerializedObject(panel);
            serializedPanel.FindProperty("_resetMatchButton").objectReferenceValue = resetBtn;
            serializedPanel.FindProperty("_pauseResumeButton").objectReferenceValue = pauseBtn;
            serializedPanel.FindProperty("_pauseResumeText").objectReferenceValue = pauseText;
            serializedPanel.ApplyModifiedProperties();
        }

        private static GameObject CreateLabel(Transform parent, string text, float width)
        {
            var labelGO = new GameObject("Label");
            labelGO.transform.SetParent(parent, false);

            var rect = labelGO.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(width, BUTTON_HEIGHT);

            var tmpText = labelGO.AddComponent<TextMeshProUGUI>();
            tmpText.text = text;
            tmpText.fontSize = 14f;
            tmpText.alignment = TextAlignmentOptions.Center;
            tmpText.color = Color.white;

            var layoutElement = labelGO.AddComponent<LayoutElement>();
            layoutElement.preferredWidth = width;
            layoutElement.preferredHeight = BUTTON_HEIGHT;

            return labelGO;
        }

        private static GameObject CreateButton(Transform parent, string buttonName, string text, float width)
        {
            var buttonGO = new GameObject(buttonName);
            buttonGO.transform.SetParent(parent, false);

            var rect = buttonGO.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(width, BUTTON_HEIGHT);

            var image = buttonGO.AddComponent<Image>();
            image.color = new Color(0.3f, 0.3f, 0.3f, 1f);

            var button = buttonGO.AddComponent<Button>();
            var colors = button.colors;
            colors.highlightedColor = new Color(0.4f, 0.4f, 0.4f, 1f);
            colors.pressedColor = new Color(0.25f, 0.25f, 0.25f, 1f);
            button.colors = colors;

            // Add text
            var textGO = new GameObject("Text");
            textGO.transform.SetParent(buttonGO.transform, false);

            var textRect = textGO.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.sizeDelta = Vector2.zero;
            textRect.anchoredPosition = Vector2.zero;

            var tmpText = textGO.AddComponent<TextMeshProUGUI>();
            tmpText.text = text;
            tmpText.fontSize = 12f;
            tmpText.alignment = TextAlignmentOptions.Center;
            tmpText.color = Color.white;

            var layoutElement = buttonGO.AddComponent<LayoutElement>();
            layoutElement.preferredWidth = width;
            layoutElement.preferredHeight = BUTTON_HEIGHT;

            return buttonGO;
        }

        private static GameObject CreateDropdown(Transform parent, string dropdownName, float width)
        {
            var dropdownGO = new GameObject(dropdownName);
            dropdownGO.transform.SetParent(parent, false);

            var rect = dropdownGO.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(width, BUTTON_HEIGHT);

            var image = dropdownGO.AddComponent<Image>();
            image.color = new Color(0.25f, 0.25f, 0.25f, 1f);

            var dropdown = dropdownGO.AddComponent<TMP_Dropdown>();

            // Create label
            var labelGO = new GameObject("Label");
            labelGO.transform.SetParent(dropdownGO.transform, false);

            var labelRect = labelGO.AddComponent<RectTransform>();
            labelRect.anchorMin = new Vector2(0.1f, 0);
            labelRect.anchorMax = new Vector2(0.8f, 1);
            labelRect.sizeDelta = Vector2.zero;
            labelRect.anchoredPosition = Vector2.zero;

            var labelText = labelGO.AddComponent<TextMeshProUGUI>();
            labelText.text = "Select...";
            labelText.fontSize = 12f;
            labelText.alignment = TextAlignmentOptions.MidlineLeft;
            labelText.color = Color.white;

            dropdown.captionText = labelText;

            // Create template (simple, Unity will handle the rest)
            var templateGO = new GameObject("Template");
            templateGO.transform.SetParent(dropdownGO.transform, false);
            templateGO.SetActive(false);

            var templateRect = templateGO.AddComponent<RectTransform>();
            templateRect.anchorMin = new Vector2(0, 0);
            templateRect.anchorMax = new Vector2(1, 0);
            templateRect.pivot = new Vector2(0.5f, 1f);
            templateRect.sizeDelta = new Vector2(0, 150);
            templateRect.anchoredPosition = Vector2.zero;

            var templateImage = templateGO.AddComponent<Image>();
            templateImage.color = new Color(0.2f, 0.2f, 0.2f, 1f);

            var scrollRect = templateGO.AddComponent<ScrollRect>();

            // Viewport
            var viewportGO = new GameObject("Viewport");
            viewportGO.transform.SetParent(templateGO.transform, false);

            var viewportRect = viewportGO.AddComponent<RectTransform>();
            viewportRect.anchorMin = Vector2.zero;
            viewportRect.anchorMax = Vector2.one;
            viewportRect.sizeDelta = Vector2.zero;
            viewportRect.anchoredPosition = Vector2.zero;

            viewportGO.AddComponent<Mask>();
            var viewportImage = viewportGO.AddComponent<Image>();
            viewportImage.color = Color.white;

            // Content
            var contentGO = new GameObject("Content");
            contentGO.transform.SetParent(viewportGO.transform, false);

            var contentRect = contentGO.AddComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0, 1);
            contentRect.anchorMax = new Vector2(1, 1);
            contentRect.pivot = new Vector2(0.5f, 1);
            contentRect.sizeDelta = new Vector2(0, 28);
            contentRect.anchoredPosition = Vector2.zero;

            // Item
            var itemGO = new GameObject("Item");
            itemGO.transform.SetParent(contentGO.transform, false);

            var itemRect = itemGO.AddComponent<RectTransform>();
            itemRect.anchorMin = new Vector2(0, 0.5f);
            itemRect.anchorMax = new Vector2(1, 0.5f);
            itemRect.pivot = new Vector2(0.5f, 0.5f);
            itemRect.sizeDelta = new Vector2(0, 25);
            itemRect.anchoredPosition = Vector2.zero;

            var itemToggle = itemGO.AddComponent<Toggle>();

            var itemLabelGO = new GameObject("Label");
            itemLabelGO.transform.SetParent(itemGO.transform, false);

            var itemLabelRect = itemLabelGO.AddComponent<RectTransform>();
            itemLabelRect.anchorMin = Vector2.zero;
            itemLabelRect.anchorMax = Vector2.one;
            itemLabelRect.sizeDelta = Vector2.zero;
            itemLabelRect.anchoredPosition = Vector2.zero;

            var itemLabelText = itemLabelGO.AddComponent<TextMeshProUGUI>();
            itemLabelText.text = "Item";
            itemLabelText.fontSize = 12f;
            itemLabelText.alignment = TextAlignmentOptions.MidlineLeft;
            itemLabelText.color = Color.white;

            dropdown.template = templateRect;
            dropdown.itemText = itemLabelText;

            scrollRect.viewport = viewportRect;
            scrollRect.content = contentRect;

            var layoutElement = dropdownGO.AddComponent<LayoutElement>();
            layoutElement.preferredWidth = width;
            layoutElement.preferredHeight = BUTTON_HEIGHT;

            return dropdownGO;
        }
    }
}
