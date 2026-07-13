using UnityEngine;
using UnityEngine.UI;

namespace Progression
{
    /// <summary>
    /// The run clock at the top of the screen. Builds its own overlay canvas at runtime, the same way
    /// <see cref="MovementRework.ScreenFader"/> does, so a level needs no UI wiring — it is added
    /// automatically alongside <see cref="LevelTimer"/>.
    /// </summary>
    public class LevelTimerUI : MonoBehaviour
    {
        [Header("Layout")]
        [SerializeField] private float topMargin = 24f;
        [SerializeField] private int fontSize = 48;

        [Header("Colors")]
        [SerializeField] private Color runningColor = Color.white;
        [SerializeField] private Color finishedColor = new Color(0.35f, 1f, 0.5f, 1f);
        [SerializeField] private Color shadowColor = new Color(0f, 0f, 0f, 0.65f);

        private Text label;

        private void Awake()
        {
            BuildUI();
        }

        private void BuildUI()
        {
            var canvasGo = new GameObject("LevelTimerCanvas");
            canvasGo.transform.SetParent(transform, false);

            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            // Above gameplay HUD, below the leaderboard (500) and the screen fader (9999), so the
            // fade-out at the end of the level covers the clock too.
            canvas.sortingOrder = 100;

            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);

            var labelGo = new GameObject("TimeLabel", typeof(RectTransform), typeof(Text), typeof(Shadow));
            var rect = labelGo.GetComponent<RectTransform>();
            rect.SetParent(canvasGo.transform, false);
            rect.anchorMin = new Vector2(0.5f, 1f);
            rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = new Vector2(0f, -topMargin);
            rect.sizeDelta = new Vector2(400f, 64f);

            label = labelGo.GetComponent<Text>();
            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            label.fontSize = fontSize;
            label.fontStyle = FontStyle.Bold;
            label.alignment = TextAnchor.UpperCenter;
            label.color = runningColor;
            label.raycastTarget = false;
            label.text = Leaderboard.FormatTime(0f);

            // The level behind the clock can be any color; a drop shadow keeps it readable on all of them.
            var shadow = labelGo.GetComponent<Shadow>();
            shadow.effectColor = shadowColor;
            shadow.effectDistance = new Vector2(2f, -2f);
        }

        private void LateUpdate()
        {
            LevelTimer timer = LevelTimer.Instance;
            if (timer == null) return;

            label.text = Leaderboard.FormatTime(timer.Elapsed);
            label.color = timer.HasFinished ? finishedColor : runningColor;
        }
    }
}
