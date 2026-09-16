namespace GameManager
{
    using UnityEngine;
    using UnityEngine.SceneManagement;
    using System.Collections;
    using System.Collections.Generic;

    [DisallowMultipleComponent]

    public class SceneController : MonoBehaviour
    {
        
        // public static SceneService instance:	Global singleton reference to this class
        // Awake():	Initializes singleton and makes it persistent
        // GoTo(): Loads a new scene (replacing the current one)
        // GoToRoutine(): Handles asynchronous loading and passes data (args)
        // LoadAdditive(): Loads a new scene on top of the current one
        // LoadAdditiveRoutine(): Async additive loading coroutine
        // Unload(): Starts unloading a scene
        // UnloadRoutine():	Waits until scene unload completes
        // SceneArgs.Payload:	Static data container for passing info between scenes
        
        public static SceneController instance { get; private set; }

        [Header("Persistent Run UI")]
        [Tooltip("Scene that stays loaded across Map/Battle/Shop for the whole run (DeckView, Currency, Owned Relics, Settings). " +
                 "Once loaded via LoadPersistentUi(), GoTo() stops doing a full Single-mode reload and instead swaps only the " +
                 "gameplay scene underneath it, so this scene is never touched by normal scene transitions.")]
        [SerializeField] string persistentUiSceneName = "PersistentUI";

        string _currentGameplayScene;
        bool _persistentUiLoaded;
        readonly List<string> _auxAdditiveScenes = new();

        public bool IsPersistentUiLoaded => _persistentUiLoaded;

        void Awake() {
            if (instance != null && instance != this) { Destroy(gameObject); return; }
            instance = this;
            DontDestroyOnLoad(gameObject);
            _currentGameplayScene = SceneManager.GetActiveScene().name;
        }

        public Coroutine GoTo(string sceneName, object args = null) =>
            StartCoroutine(GoToRoutine(sceneName, args));

        private IEnumerator GoToRoutine(string sceneName, object args) {
            SceneArgs.Payload = args; // your static handoff bucket

            if (_persistentUiLoaded)
            {
                // Keep the persistent UI scene alive: unload only the current gameplay scene
                // (plus any additive "aux" scenes stacked on top of it, e.g. UI_Battle - these
                // are never allowed to coexist with the persistent scene past this transition,
                // since a duplicate MonoBehaviour singleton in one would Destroy() the other's),
                // then additively load the new one and make it active (so its lighting/skybox apply).
                if (!string.IsNullOrEmpty(_currentGameplayScene))
                {
                    var unloadOp = SceneManager.UnloadSceneAsync(_currentGameplayScene);
                    while (unloadOp != null && !unloadOp.isDone) yield return null;
                }

                for (int i = 0; i < _auxAdditiveScenes.Count; i++)
                {
                    var auxUnloadOp = SceneManager.UnloadSceneAsync(_auxAdditiveScenes[i]);
                    while (auxUnloadOp != null && !auxUnloadOp.isDone) yield return null;
                }
                _auxAdditiveScenes.Clear();

                var loadOp = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
                while (!loadOp.isDone) yield return null;

                var loaded = SceneManager.GetSceneByName(sceneName);
                if (loaded.IsValid())
                    SceneManager.SetActiveScene(loaded);
            }
            else
            {
                var op = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);
                while (!op.isDone) yield return null;

                // Single-mode already unloaded everything, aux scenes included.
                _auxAdditiveScenes.Clear();
            }

            _currentGameplayScene = sceneName;
        }

        public Coroutine LoadAdditive(string sceneName) =>
            StartCoroutine(LoadAdditiveRoutine(sceneName));

        private IEnumerator LoadAdditiveRoutine(string sceneName) {
            var op = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
            while (!op.isDone) yield return null;

            // Tracked so the next GoTo() cleans it up too, instead of it silently stacking forever.
            if (sceneName != persistentUiSceneName && !_auxAdditiveScenes.Contains(sceneName))
                _auxAdditiveScenes.Add(sceneName);
        }

        public Coroutine Unload(string sceneName) =>
            StartCoroutine(UnloadRoutine(sceneName));

        private IEnumerator UnloadRoutine(string sceneName) {
            var op = SceneManager.UnloadSceneAsync(sceneName);
            while (op != null && !op.isDone) yield return null;
        }

        // --- Persistent Run UI lifecycle (DeckView / Currency / Owned Relics / Settings) ---

        public Coroutine LoadPersistentUi() => StartCoroutine(LoadPersistentUiRoutine());

        private IEnumerator LoadPersistentUiRoutine()
        {
            if (_persistentUiLoaded) yield break;
            if (string.IsNullOrEmpty(persistentUiSceneName)) yield break;

            var op = SceneManager.LoadSceneAsync(persistentUiSceneName, LoadSceneMode.Additive);
            while (!op.isDone) yield return null;

            _persistentUiLoaded = true;
        }

        public Coroutine UnloadPersistentUi() => StartCoroutine(UnloadPersistentUiRoutine());

        private IEnumerator UnloadPersistentUiRoutine()
        {
            if (!_persistentUiLoaded) yield break;

            var unloadOp = SceneManager.UnloadSceneAsync(persistentUiSceneName);
            while (unloadOp != null && !unloadOp.isDone) yield return null;

            _persistentUiLoaded = false;
        }
    }
    public static class SceneArgs { public static object Payload; } //stores data
}