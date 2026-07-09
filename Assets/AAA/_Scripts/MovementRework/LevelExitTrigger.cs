using Eflatun.SceneReference;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MovementRework {
    /// <summary>
    /// Place at the end of a level on a GameObject with a Collider marked "Is Trigger".
    /// When the player enters, the screen fades to black and the target scene is loaded.
    /// Assign the destination scene in the inspector (it must also be added to Build Settings).
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class LevelExitTrigger : MonoBehaviour
    {
        [Tooltip("Scene to load when the player reaches this exit. Must be in Build Settings.")]
        [SerializeField] private SceneReference targetScene;

        [Tooltip("How long the fade to black takes, in seconds.")]
        [SerializeField] private float fadeTime = 0.5f;

        // Guards against the trigger firing twice while the fade is still playing.
        private bool triggered;

        private void Reset()
        {
            // Make sure the collider is a trigger when the component is first added.
            GetComponent<Collider>().isTrigger = true;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (triggered) return;
            // GetComponentInParent so a child collider (e.g. the model) still finds the Player.
            if (other.GetComponentInParent<Player>() == null) return;

            triggered = true;
            BeginTransition();
        }

        private void BeginTransition()
        {
            if (ScreenFader.Instance != null)
            {
                ScreenFader.Instance.FadeToBlack(LoadTargetScene, fadeTime);
            }
            else
            {
                LoadTargetScene();
            }
        }

        private void LoadTargetScene()
        {
            int buildIndex = targetScene.BuildIndex;
            if (buildIndex < 0)
            {
                Debug.LogError($"{nameof(LevelExitTrigger)} on '{name}': target scene is unassigned " +
                               "or not added to Build Settings.", this);
                return;
            }

            SceneManager.LoadScene(buildIndex);
        }
    }
}
