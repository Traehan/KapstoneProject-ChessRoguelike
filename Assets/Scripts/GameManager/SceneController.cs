namespace GameManager
{
    using UnityEngine;
    using UnityEngine.SceneManagement;
    using System.Collections;

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
        void Awake() {
            if (instance != null && instance != this) { Destroy(gameObject); return; }
            instance = this;
            DontDestroyOnLoad(gameObject);
        }

        public Coroutine GoTo(string sceneName, object args = null) =>
            StartCoroutine(GoToRoutine(sceneName, args));

        private IEnumerator GoToRoutine(string sceneName, object args) {
            SceneArgs.Payload = args; // your static handoff bucket
            var op = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);
            while (!op.isDone) yield return null;
        }

        public Coroutine LoadAdditive(string sceneName) =>
            StartCoroutine(LoadAdditiveRoutine(sceneName));

        private IEnumerator LoadAdditiveRoutine(string sceneName) {
            var op = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
            while (!op.isDone) yield return null;
        }

        public Coroutine Unload(string sceneName) =>
            StartCoroutine(UnloadRoutine(sceneName));

        private IEnumerator UnloadRoutine(string sceneName) {
            var op = SceneManager.UnloadSceneAsync(sceneName);
            while (op != null && !op.isDone) yield return null;
        }
    }
    public static class SceneArgs { public static object Payload; } //stores data
}