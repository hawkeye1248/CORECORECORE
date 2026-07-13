using MovementRework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace Progression
{
    /// <summary>
    /// Times a run through one level and hands the result to the <see cref="Leaderboard"/> when the
    /// player reaches the exit. Deaths cost time: a respawn at the last platform leaves the clock
    /// running, and only a full map restart (T) or reloading the scene starts it over.
    ///
    /// No scene setup needed — one of these is spawned automatically into any scene that contains a
    /// <see cref="LevelExitTrigger"/> (i.e. any actual level). Drop the component in a scene by hand
    /// only to change its settings; the auto-spawn then steps aside.
    /// </summary>
    [RequireComponent(typeof(LevelTimerUI), typeof(LeaderboardUI))]
    public class LevelTimer : MonoBehaviour
    {
        public static LevelTimer Instance { get; private set; }

        [Tooltip("Hold the clock at 0:00.00 until the player's first key or click, so the fade-in " +
                 "at the start of the level doesn't cost them time.")]
        [SerializeField] private bool startOnFirstInput = true;

        /// <summary>Seconds the current run has taken so far.</summary>
        public float Elapsed { get; private set; }

        /// <summary>True once the clock is ticking (see <c>startOnFirstInput</c>) and before the exit.</summary>
        public bool IsRunning { get; private set; }

        /// <summary>True after the player reached the exit. The clock is frozen at the final time.</summary>
        public bool HasFinished { get; private set; }

        /// <summary>The scene name doubles as the leaderboard key, so each level has its own board.</summary>
        public string LevelKey => gameObject.scene.name;

        // Spawns the timer into every level as it loads. RuntimeInitializeOnLoadMethod only fires for
        // the first scene of the session, so the sceneLoaded hook covers levels 2..n.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            SceneManager.sceneLoaded += (scene, mode) => SpawnIfLevel();
            SpawnIfLevel();
        }

        private static void SpawnIfLevel()
        {
            if (Instance != null) return;

            // An exit is what makes a scene a level. Menus and test scenes have none, and get no timer.
            if (FindFirstObjectByType<LevelExitTrigger>() == null) return;

            // AddComponent pulls in the two UI components via RequireComponent.
            new GameObject(nameof(LevelTimer)).AddComponent<LevelTimer>();
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            ResetTimer();
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private void Update()
        {
            if (HasFinished) return;

            if (!IsRunning)
            {
                if (!AnyInputThisFrame()) return;
                IsRunning = true;
            }

            Elapsed += Time.deltaTime;
        }

        private static bool AnyInputThisFrame()
        {
            if (Keyboard.current != null && Keyboard.current.anyKey.wasPressedThisFrame) return true;
            if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame) return true;

            return false;
        }

        /// <summary>Starts the run over. Called by a full map restart (T); a death does not reset the clock.</summary>
        public void ResetTimer()
        {
            Elapsed = 0f;
            HasFinished = false;
            IsRunning = !startOnFirstInput;
        }

        /// <summary>
        /// Stops the clock and records the run on this level's board. Called by
        /// <see cref="LevelExitTrigger"/>; the result is what the end screen displays.
        /// </summary>
        public LevelResult FinishLevel()
        {
            IsRunning = false;
            HasFinished = true;

            return Leaderboard.Submit(LevelKey, Elapsed);
        }
    }
}
