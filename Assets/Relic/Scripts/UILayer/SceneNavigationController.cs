using UnityEngine;
using UnityEngine.UI;
using Relic.Core;

namespace Relic.UILayer
{
    /// <summary>
    /// Handles navigation between scenes via UI buttons.
    /// Attach to any scene with navigation buttons.
    /// </summary>
    public class SceneNavigationController : MonoBehaviour
    {
        [Header("Scene Navigation Buttons")]
        [SerializeField] private Button mainMenuButton;
        [SerializeField] private Button startGameButton;
        [SerializeField] private Button arSessionButton;
        [SerializeField] private Button battlefieldSetupButton;
        [SerializeField] private Button battleButton;
        [SerializeField] private Button flatDebugButton;
        [SerializeField] private Button backButton;
        [SerializeField] private Button exitBattleButton;

        private void Start()
        {
            // Hook up button listeners
            if (mainMenuButton != null)
                mainMenuButton.onClick.AddListener(GoToMainMenu);

            if (startGameButton != null)
                startGameButton.onClick.AddListener(GoToARSession);

            if (arSessionButton != null)
                arSessionButton.onClick.AddListener(GoToARSession);

            if (battlefieldSetupButton != null)
                battlefieldSetupButton.onClick.AddListener(GoToBattlefieldSetup);

            if (battleButton != null)
                battleButton.onClick.AddListener(GoToBattle);

            if (flatDebugButton != null)
                flatDebugButton.onClick.AddListener(GoToFlatDebug);

            if (backButton != null)
                backButton.onClick.AddListener(GoBack);

            if (exitBattleButton != null)
                exitBattleButton.onClick.AddListener(GoToMainMenu);
        }

        public void GoToMainMenu()
        {
            SceneLoader.Instance.GoToMainMenu();
        }

        public void GoToARSession()
        {
            SceneLoader.Instance.GoToARSession();
        }

        public void GoToBattlefieldSetup()
        {
            SceneLoader.Instance.GoToBattlefieldSetup();
        }

        public void GoToBattle()
        {
            SceneLoader.Instance.GoToBattle();
        }

        public void GoToFlatDebug()
        {
            SceneLoader.Instance.GoToFlatDebug();
        }

        /// <summary>
        /// Go back to the previous logical scene.
        /// </summary>
        public void GoBack()
        {
            string currentScene = SceneLoader.Instance.CurrentSceneName;

            switch (currentScene)
            {
                case SceneLoader.Scenes.ARSession:
                case SceneLoader.Scenes.FlatDebug:
                    GoToMainMenu();
                    break;
                case SceneLoader.Scenes.BattlefieldSetup:
                    GoToARSession();
                    break;
                case SceneLoader.Scenes.Battle:
                    GoToBattlefieldSetup();
                    break;
                default:
                    GoToMainMenu();
                    break;
            }
        }
    }
}
