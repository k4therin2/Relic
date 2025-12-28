using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Relic.Core
{
    /// <summary>
    /// Centralized scene loading system with transition support.
    /// Wraps Unity's SceneManager with additional functionality for AR RTS game.
    /// </summary>
    public class SceneLoader : MonoBehaviour
    {
        private static SceneLoader instance;
        public static SceneLoader Instance
        {
            get
            {
                if (instance == null)
                {
                    var go = new GameObject("SceneLoader");
                    instance = go.AddComponent<SceneLoader>();
                    DontDestroyOnLoad(go);
                }
                return instance;
            }
        }

        /// <summary>
        /// Scene names as defined in Kyle's milestones.
        /// </summary>
        public static class Scenes
        {
            public const string MainMenu = "MainMenu";
            public const string ARSession = "ARSession";
            public const string BattlefieldSetup = "BattlefieldSetup";
            public const string Battle = "Battle";
            public const string FlatDebug = "Flat_Debug";
        }

        /// <summary>
        /// Event fired when scene loading begins.
        /// </summary>
        public event Action<string> OnSceneLoadStarted;

        /// <summary>
        /// Event fired when scene loading completes.
        /// </summary>
        public event Action<string> OnSceneLoadCompleted;

        /// <summary>
        /// Event fired with loading progress (0-1).
        /// </summary>
        public event Action<float> OnLoadingProgress;

        /// <summary>
        /// Currently active scene name.
        /// </summary>
        public string CurrentSceneName => SceneManager.GetActiveScene().name;

        /// <summary>
        /// True if a scene is currently loading.
        /// </summary>
        public bool IsLoading { get; private set; }

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }
            instance = this;
            DontDestroyOnLoad(gameObject);
        }

        /// <summary>
        /// Load a scene by name asynchronously.
        /// </summary>
        /// <param name="sceneName">Name of the scene to load.</param>
        /// <param name="mode">How to load the scene (single replaces current, additive adds to existing).</param>
        public void LoadScene(string sceneName, LoadSceneMode mode = LoadSceneMode.Single)
        {
            if (IsLoading)
            {
                Debug.LogWarning($"SceneLoader: Already loading a scene, ignoring request for {sceneName}");
                return;
            }
            StartCoroutine(LoadSceneAsync(sceneName, mode));
        }

        /// <summary>
        /// Load a scene with a fade transition (placeholder - can be enhanced later).
        /// </summary>
        /// <param name="sceneName">Name of the scene to load.</param>
        public void LoadSceneWithTransition(string sceneName)
        {
            LoadScene(sceneName, LoadSceneMode.Single);
        }

        /// <summary>
        /// Quick navigation methods for common scene transitions.
        /// </summary>
        public void GoToMainMenu() => LoadScene(Scenes.MainMenu);
        public void GoToARSession() => LoadScene(Scenes.ARSession);
        public void GoToBattlefieldSetup() => LoadScene(Scenes.BattlefieldSetup);
        public void GoToBattle() => LoadScene(Scenes.Battle);
        public void GoToFlatDebug() => LoadScene(Scenes.FlatDebug);

        private IEnumerator LoadSceneAsync(string sceneName, LoadSceneMode mode)
        {
            IsLoading = true;
            OnSceneLoadStarted?.Invoke(sceneName);

            AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneName, mode);
            if (asyncLoad == null)
            {
                Debug.LogError($"SceneLoader: Failed to start loading scene {sceneName}");
                IsLoading = false;
                yield break;
            }

            asyncLoad.allowSceneActivation = true;

            while (!asyncLoad.isDone)
            {
                OnLoadingProgress?.Invoke(asyncLoad.progress);
                yield return null;
            }

            IsLoading = false;
            OnSceneLoadCompleted?.Invoke(sceneName);
        }

        /// <summary>
        /// Check if a scene is loaded in the current scene list.
        /// </summary>
        /// <param name="sceneName">Name of the scene to check.</param>
        /// <returns>True if the scene is currently loaded.</returns>
        public bool IsSceneLoaded(string sceneName)
        {
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                if (SceneManager.GetSceneAt(i).name == sceneName)
                    return true;
            }
            return false;
        }

        /// <summary>
        /// Unload a scene by name.
        /// </summary>
        /// <param name="sceneName">Name of the scene to unload.</param>
        public void UnloadScene(string sceneName)
        {
            if (IsSceneLoaded(sceneName))
            {
                SceneManager.UnloadSceneAsync(sceneName);
            }
        }
    }
}
