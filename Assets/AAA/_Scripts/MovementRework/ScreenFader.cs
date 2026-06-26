using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace MovementRework {
    /// <summary>
    /// Full-screen black overlay used to hide instant teleports (e.g. respawn).
    /// Builds its own Screen-Space Overlay canvas at runtime, so it needs no scene setup:
    /// just have one GameObject with this component in the scene.
    /// </summary>
    public class ScreenFader : MonoBehaviour
    {
        public static ScreenFader Instance { get; private set; }

        [SerializeField] private float defaultFadeTime = 0.15f;
        [SerializeField] private float defaultHoldTime = 0.1f;

        private CanvasGroup canvasGroup;
        private Coroutine fadeRoutine;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            BuildOverlay();
        }

        private void BuildOverlay()
        {
            // Canvas
            var canvasGo = new GameObject("ScreenFaderCanvas");
            canvasGo.transform.SetParent(transform, false);

            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 9999; // draw on top of everything
            canvasGo.AddComponent<CanvasScaler>();

            canvasGroup = canvasGo.AddComponent<CanvasGroup>();
            canvasGroup.alpha = 0f;
            canvasGroup.blocksRaycasts = false;
            canvasGroup.interactable = false;

            // Black full-screen image
            var imageGo = new GameObject("Black");
            imageGo.transform.SetParent(canvasGo.transform, false);

            var image = imageGo.AddComponent<Image>();
            image.color = Color.black;
            image.raycastTarget = false;

            var rect = image.rectTransform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        /// <summary>
        /// Fades to black, runs <paramref name="onBlack"/> while the screen is fully black,
        /// then fades back in. Use onBlack to teleport/reposition so the player never sees it.
        /// </summary>
        public void FlashBlack(Action onBlack, float? fadeTime = null, float? holdTime = null)
        {
            if (fadeRoutine != null) StopCoroutine(fadeRoutine);
            fadeRoutine = StartCoroutine(FlashRoutine(onBlack, fadeTime ?? defaultFadeTime, holdTime ?? defaultHoldTime));
        }

        private IEnumerator FlashRoutine(Action onBlack, float fadeTime, float holdTime)
        {
            yield return Fade(1f, fadeTime);

            onBlack?.Invoke();

            if (holdTime > 0f)
                yield return new WaitForSecondsRealtime(holdTime);

            yield return Fade(0f, fadeTime);

            fadeRoutine = null;
        }

        private IEnumerator Fade(float target, float duration)
        {
            float start = canvasGroup.alpha;

            if (duration <= 0f)
            {
                canvasGroup.alpha = target;
                yield break;
            }

            for (float t = 0f; t < duration; t += Time.unscaledDeltaTime)
            {
                canvasGroup.alpha = Mathf.Lerp(start, target, t / duration);
                yield return null;
            }
            canvasGroup.alpha = target;
        }
    }
}
