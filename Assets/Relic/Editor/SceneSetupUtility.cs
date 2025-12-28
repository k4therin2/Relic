#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.XR.ARFoundation;
using UnityEngine.UI;

namespace Relic.Editor
{
    /// <summary>
    /// Editor utility to create the required scenes for the AR RTS game.
    /// Run this via menu: Relic/Setup/Create All Scenes
    /// </summary>
    public static class SceneSetupUtility
    {
        private const string ScenesPath = "Assets/Relic/Scenes/";

        [MenuItem("Relic/Setup/Create All Scenes")]
        public static void CreateAllScenes()
        {
            CreateMainMenuScene();
            CreateARSessionScene();
            CreateBattlefieldSetupScene();
            CreateBattleScene();
            Debug.Log("SceneSetupUtility: All scenes created successfully!");
        }

        [MenuItem("Relic/Setup/Create MainMenu Scene")]
        public static void CreateMainMenuScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            scene.name = "MainMenu";

            // Create Main Camera
            var cameraGO = CreateMainCamera();

            // Create Canvas with basic UI
            var canvas = CreateUICanvas("MainMenuCanvas");

            // Create title text
            var titleText = CreateTextElement(canvas.transform, "TitleText", "RELIC: AR RTS",
                new Vector2(0, 100), 48, TextAnchor.MiddleCenter);

            // Create start button
            var startButton = CreateButton(canvas.transform, "StartButton", "Start Game",
                new Vector2(0, 0), new Vector2(200, 60));

            // Create flat debug button
            var debugButton = CreateButton(canvas.transform, "DebugButton", "Flat Debug",
                new Vector2(0, -80), new Vector2(200, 60));

            // Create EventSystem
            CreateEventSystem();

            EditorSceneManager.SaveScene(scene, ScenesPath + "MainMenu.unity");
            Debug.Log("Created MainMenu.unity");
        }

        [MenuItem("Relic/Setup/Create ARSession Scene")]
        public static void CreateARSessionScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            scene.name = "ARSession";

            // Create AR Session
            var arSessionGO = new GameObject("AR Session");
            arSessionGO.AddComponent<ARSession>();

            // Create XR Origin (AR Camera)
            var xrOriginGO = new GameObject("XR Origin");
            var xrOrigin = xrOriginGO.AddComponent<Unity.XR.CoreUtils.XROrigin>();

            // Create Camera Offset
            var cameraOffsetGO = new GameObject("Camera Offset");
            cameraOffsetGO.transform.SetParent(xrOriginGO.transform);
            xrOrigin.CameraFloorOffsetObject = cameraOffsetGO;

            // Create AR Camera
            var arCameraGO = new GameObject("AR Camera");
            arCameraGO.transform.SetParent(cameraOffsetGO.transform);
            arCameraGO.tag = "MainCamera";
            var arCamera = arCameraGO.AddComponent<Camera>();
            arCamera.clearFlags = CameraClearFlags.SolidColor;
            arCamera.backgroundColor = Color.black;
            arCameraGO.AddComponent<ARCameraManager>();
            arCameraGO.AddComponent<ARCameraBackground>();
            arCameraGO.AddComponent<AudioListener>();
            xrOrigin.Camera = arCamera;

            // Create AR Plane Manager
            xrOriginGO.AddComponent<ARPlaneManager>();
            xrOriginGO.AddComponent<ARRaycastManager>();

            // Create Canvas for AR UI
            var canvas = CreateUICanvas("ARCanvas");
            canvas.GetComponent<Canvas>().renderMode = RenderMode.WorldSpace;
            canvas.GetComponent<RectTransform>().sizeDelta = new Vector2(1, 0.5f);

            // Create instruction text
            var instructionText = CreateTextElement(canvas.transform, "InstructionText",
                "Point at a flat surface and tap to place battlefield",
                Vector2.zero, 24, TextAnchor.MiddleCenter);

            CreateEventSystem();

            EditorSceneManager.SaveScene(scene, ScenesPath + "ARSession.unity");
            Debug.Log("Created ARSession.unity");
        }

        [MenuItem("Relic/Setup/Create BattlefieldSetup Scene")]
        public static void CreateBattlefieldSetupScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            scene.name = "BattlefieldSetup";

            // Create Main Camera
            var cameraGO = CreateMainCamera();

            // Create Canvas with setup UI
            var canvas = CreateUICanvas("SetupCanvas");

            // Create header text
            var headerText = CreateTextElement(canvas.transform, "HeaderText", "Battlefield Setup",
                new Vector2(0, 200), 36, TextAnchor.MiddleCenter);

            // Create era selection buttons (placeholder)
            var eraLabel = CreateTextElement(canvas.transform, "EraLabel", "Select Era:",
                new Vector2(0, 100), 24, TextAnchor.MiddleCenter);

            var ancientButton = CreateButton(canvas.transform, "AncientButton", "Ancient",
                new Vector2(-150, 50), new Vector2(120, 40));
            var medievalButton = CreateButton(canvas.transform, "MedievalButton", "Medieval",
                new Vector2(-30, 50), new Vector2(120, 40));
            var wwiiButton = CreateButton(canvas.transform, "WWIIButton", "WWII",
                new Vector2(90, 50), new Vector2(120, 40));
            var futureButton = CreateButton(canvas.transform, "FutureButton", "Future",
                new Vector2(210, 50), new Vector2(120, 40));

            // Create start battle button
            var startBattleButton = CreateButton(canvas.transform, "StartBattleButton", "Start Battle",
                new Vector2(0, -100), new Vector2(200, 60));

            // Create back button
            var backButton = CreateButton(canvas.transform, "BackButton", "Back",
                new Vector2(0, -180), new Vector2(120, 40));

            CreateEventSystem();

            EditorSceneManager.SaveScene(scene, ScenesPath + "BattlefieldSetup.unity");
            Debug.Log("Created BattlefieldSetup.unity");
        }

        [MenuItem("Relic/Setup/Create Battle Scene")]
        public static void CreateBattleScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            scene.name = "Battle";

            // Create Main Camera
            var cameraGO = CreateMainCamera();
            cameraGO.transform.position = new Vector3(0, 10, -10);
            cameraGO.transform.rotation = Quaternion.Euler(45, 0, 0);

            // Create directional light
            var lightGO = new GameObject("Directional Light");
            var light = lightGO.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1f;
            lightGO.transform.rotation = Quaternion.Euler(50, -30, 0);

            // Create Battlefield Root (placeholder)
            var battlefieldRoot = new GameObject("BattlefieldRoot");

            // Create ground plane (placeholder)
            var groundGO = GameObject.CreatePrimitive(PrimitiveType.Plane);
            groundGO.name = "Ground";
            groundGO.transform.SetParent(battlefieldRoot.transform);
            groundGO.transform.localScale = new Vector3(2, 1, 2);

            // Create spawn points
            var spawnPointsGO = new GameObject("SpawnPoints");
            spawnPointsGO.transform.SetParent(battlefieldRoot.transform);

            var redSpawn = new GameObject("RedSpawn");
            redSpawn.transform.SetParent(spawnPointsGO.transform);
            redSpawn.transform.localPosition = new Vector3(-8, 0, 0);

            var blueSpawn = new GameObject("BlueSpawn");
            blueSpawn.transform.SetParent(spawnPointsGO.transform);
            blueSpawn.transform.localPosition = new Vector3(8, 0, 0);

            // Create Canvas for battle HUD
            var canvas = CreateUICanvas("BattleHUD");

            // Create back button
            var backButton = CreateButton(canvas.transform, "BackButton", "Exit Battle",
                new Vector2(-400, 250), new Vector2(120, 40));

            // Create pause button
            var pauseButton = CreateButton(canvas.transform, "PauseButton", "Pause",
                new Vector2(-280, 250), new Vector2(80, 40));

            CreateEventSystem();

            EditorSceneManager.SaveScene(scene, ScenesPath + "Battle.unity");
            Debug.Log("Created Battle.unity");
        }

        private static GameObject CreateMainCamera()
        {
            var cameraGO = new GameObject("Main Camera");
            cameraGO.tag = "MainCamera";
            var camera = cameraGO.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.2f, 0.2f, 0.2f, 1f);
            cameraGO.AddComponent<AudioListener>();
            return cameraGO;
        }

        private static GameObject CreateUICanvas(string name)
        {
            var canvasGO = new GameObject(name);
            var canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasGO.AddComponent<CanvasScaler>();
            canvasGO.AddComponent<GraphicRaycaster>();
            return canvasGO;
        }

        private static GameObject CreateTextElement(Transform parent, string name, string text,
            Vector2 position, int fontSize, TextAnchor alignment)
        {
            var textGO = new GameObject(name);
            textGO.transform.SetParent(parent);
            var rectTransform = textGO.AddComponent<RectTransform>();
            rectTransform.anchoredPosition = position;
            rectTransform.sizeDelta = new Vector2(600, 100);

            var textComponent = textGO.AddComponent<Text>();
            textComponent.text = text;
            textComponent.fontSize = fontSize;
            textComponent.alignment = alignment;
            textComponent.color = Color.white;
            textComponent.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            return textGO;
        }

        private static GameObject CreateButton(Transform parent, string name, string text,
            Vector2 position, Vector2 size)
        {
            var buttonGO = new GameObject(name);
            buttonGO.transform.SetParent(parent);
            var rectTransform = buttonGO.AddComponent<RectTransform>();
            rectTransform.anchoredPosition = position;
            rectTransform.sizeDelta = size;

            var image = buttonGO.AddComponent<Image>();
            image.color = new Color(0.3f, 0.3f, 0.3f, 1f);

            buttonGO.AddComponent<Button>();

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
            textComponent.fontSize = 18;
            textComponent.alignment = TextAnchor.MiddleCenter;
            textComponent.color = Color.white;
            textComponent.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            return buttonGO;
        }

        private static void CreateEventSystem()
        {
            if (Object.FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
            {
                var eventSystemGO = new GameObject("EventSystem");
                eventSystemGO.AddComponent<UnityEngine.EventSystems.EventSystem>();
                eventSystemGO.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
            }
        }
    }
}
#endif
