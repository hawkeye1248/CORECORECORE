using System;
using Building;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Progression
{
    /// <summary>
    /// The end-of-level board. When the player reaches the exit the game freezes, this panel lists
    /// every time recorded on the level with the player's own row highlighted, and the level only
    /// moves on once they press a key.
    ///
    /// Like <see cref="LevelTimerUI"/> it builds its own canvas, so it needs no scene setup; it rides
    /// along with <see cref="LevelTimer"/>.
    /// </summary>
    public class LeaderboardUI : MonoBehaviour
    {
        public static LeaderboardUI Instance { get; private set; }

        [Header("Board")]
        [Tooltip("How many of the fastest runs to list. The player's own row is always shown, even " +
                 "when their time is slower than all of these.")]
        [SerializeField] private int maxRows = 10;

        [Tooltip("Ignore input for this long (real seconds) after the board appears, so the key the " +
                 "player was holding at the exit doesn't skip straight past it.")]
        [SerializeField] private float inputLockTime = 0.6f;

        [Header("Colors")]
        [SerializeField] private Color panelColor = new Color(0f, 0f, 0f, 0.85f);
        [SerializeField] private Color titleColor = Color.white;
        [SerializeField] private Color rowColor = new Color(0.8f, 0.8f, 0.8f, 1f);
        [SerializeField] private Color playerRowColor = new Color(0.2f, 0.8f, 1f, 1f);
        [SerializeField] private Color newBestColor = new Color(0.35f, 1f, 0.5f, 1f);
        [SerializeField] private Color hintColor = new Color(1f, 1f, 1f, 0.6f);

        private GameObject panel;
        private Transform rowsParent;
        private Text titleLabel;
        private Text summaryLabel;
        private Font font;

        private Action onContinue;
        private float shownAtRealtime;
        private bool isShowing;
        private float previousTimeScale = 1f;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }
            Instance = this;

            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            BuildUI();
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        /// <summary>
        /// Freezes the game and shows the board for a finished run. <paramref name="continueAction"/>
        /// runs once the player dismisses it — that's what carries on to the next scene.
        /// </summary>
        public void Show(LevelResult result, Action continueAction)
        {
            onContinue = continueAction;
            isShowing = true;
            shownAtRealtime = Time.realtimeSinceStartup;

            Populate(result);
            panel.SetActive(true);

            previousTimeScale = Time.timeScale;
            Time.timeScale = 0f;

            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;

            // Stop the player from placing blocks into the frozen level behind the board. Disabling the
            // component is the component's own clean exit: it drops build mode and the ghost with it.
            if (BuildingSystem.Instance != null) BuildingSystem.Instance.enabled = false;
        }

        private void Update()
        {
            if (!isShowing) return;

            // Real time, not game time: the game is frozen at timeScale 0 while the board is up.
            if (Time.realtimeSinceStartup - shownAtRealtime < inputLockTime) return;
            if (!AnyInputThisFrame()) return;

            Continue();
        }

        private static bool AnyInputThisFrame()
        {
            if (Keyboard.current != null && Keyboard.current.anyKey.wasPressedThisFrame) return true;
            if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame) return true;

            return false;
        }

        private void Continue()
        {
            isShowing = false;
            panel.SetActive(false);

            Time.timeScale = previousTimeScale;

            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;

            if (BuildingSystem.Instance != null) BuildingSystem.Instance.enabled = true;

            Action action = onContinue;
            onContinue = null;
            action?.Invoke();
        }

        private void Populate(LevelResult result)
        {
            titleLabel.text = "LEVEL COMPLETE";

            if (result.IsNewBest)
            {
                summaryLabel.text = $"{result.PlayerName}   {Leaderboard.FormatTime(result.RunTime)}   NEW BEST";
                summaryLabel.color = newBestColor;
            }
            else
            {
                summaryLabel.text = $"{result.PlayerName}   {Leaderboard.FormatTime(result.RunTime)}   " +
                                    $"(best {Leaderboard.FormatTime(result.BestTime)})";
                summaryLabel.color = playerRowColor;
            }

            for (int i = rowsParent.childCount - 1; i >= 0; i--)
                Destroy(rowsParent.GetChild(i).gameObject);

            int shown = Mathf.Min(maxRows, result.Entries.Count);
            for (int i = 0; i < shown; i++)
                CreateRow(i + 1, result.Entries[i], result.Entries[i].PlayerName == result.PlayerName);

            // A slow run falls outside the top rows, but the player still wants to see where they landed.
            if (result.Rank > shown)
            {
                CreateSeparator();
                CreateRow(result.Rank, result.Entries[result.Rank - 1], true);
            }
        }

        private void BuildUI()
        {
            var canvasGo = new GameObject("LeaderboardCanvas");
            canvasGo.transform.SetParent(transform, false);

            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            // Over the HUD and the timer, under the screen fader (9999) so the level transition fades it out.
            canvas.sortingOrder = 500;

            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);

            panel = new GameObject("Panel", typeof(RectTransform), typeof(Image), typeof(VerticalLayoutGroup));
            var panelRect = panel.GetComponent<RectTransform>();
            panelRect.SetParent(canvasGo.transform, false);
            panelRect.anchorMin = new Vector2(0.5f, 0.5f);
            panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            panelRect.pivot = new Vector2(0.5f, 0.5f);
            panelRect.sizeDelta = new Vector2(720f, 760f);

            panel.GetComponent<Image>().color = panelColor;

            var panelLayout = panel.GetComponent<VerticalLayoutGroup>();
            panelLayout.padding = new RectOffset(48, 48, 40, 40);
            panelLayout.spacing = 16f;
            panelLayout.childAlignment = TextAnchor.UpperCenter;
            panelLayout.childControlWidth = true;
            panelLayout.childControlHeight = false;
            panelLayout.childForceExpandWidth = true;
            panelLayout.childForceExpandHeight = false;

            titleLabel = CreateLabel(panel.transform, "Title", 44, FontStyle.Bold, TextAnchor.MiddleCenter,
                                     titleColor, 56f);
            summaryLabel = CreateLabel(panel.transform, "Summary", 28, FontStyle.Bold, TextAnchor.MiddleCenter,
                                       playerRowColor, 40f);

            var rowsGo = new GameObject("Rows", typeof(RectTransform), typeof(VerticalLayoutGroup),
                                        typeof(LayoutElement));
            rowsGo.GetComponent<RectTransform>().SetParent(panel.transform, false);
            rowsParent = rowsGo.transform;

            var rowsLayout = rowsGo.GetComponent<VerticalLayoutGroup>();
            rowsLayout.spacing = 6f;
            rowsLayout.padding = new RectOffset(0, 0, 16, 16);
            rowsLayout.childControlWidth = true;
            rowsLayout.childControlHeight = false;
            rowsLayout.childForceExpandWidth = true;
            rowsLayout.childForceExpandHeight = false;

            rowsGo.GetComponent<LayoutElement>().flexibleHeight = 1f;

            CreateLabel(panel.transform, "Hint", 22, FontStyle.Normal, TextAnchor.MiddleCenter,
                        hintColor, 32f).text = "Press any key to continue";

            panel.SetActive(false);
        }

        private void CreateRow(int rank, LeaderboardEntry entry, bool isPlayer)
        {
            var row = new GameObject($"Row_{rank}", typeof(RectTransform), typeof(HorizontalLayoutGroup),
                                     typeof(LayoutElement));
            row.GetComponent<RectTransform>().SetParent(rowsParent, false);

            var layout = row.GetComponent<HorizontalLayoutGroup>();
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = true;

            row.GetComponent<LayoutElement>().preferredHeight = 40f;

            Color color = isPlayer ? playerRowColor : rowColor;
            FontStyle style = isPlayer ? FontStyle.Bold : FontStyle.Normal;

            CreateCell(row.transform, "Rank", $"{rank}.", TextAnchor.MiddleLeft, style, color, 70f, 0f);
            CreateCell(row.transform, "Name", entry.PlayerName, TextAnchor.MiddleLeft, style, color, 0f, 1f);
            CreateCell(row.transform, "Time", Leaderboard.FormatTime(entry.Time), TextAnchor.MiddleRight, style,
                       color, 180f, 0f);
        }

        private void CreateSeparator()
        {
            CreateLabel(rowsParent, "Separator", 26, FontStyle.Normal, TextAnchor.MiddleCenter, rowColor, 28f)
                .text = ". . .";
        }

        private Text CreateLabel(Transform parent, string name, int size, FontStyle style, TextAnchor anchor,
                                 Color color, float height)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Text), typeof(LayoutElement));
            go.GetComponent<RectTransform>().SetParent(parent, false);
            go.GetComponent<LayoutElement>().preferredHeight = height;

            var text = go.GetComponent<Text>();
            text.font = font;
            text.fontSize = size;
            text.fontStyle = style;
            text.alignment = anchor;
            text.color = color;
            text.raycastTarget = false;

            return text;
        }

        private void CreateCell(Transform parent, string name, string content, TextAnchor anchor, FontStyle style,
                                Color color, float preferredWidth, float flexibleWidth)
        {
            Text text = CreateLabel(parent, name, 26, style, anchor, color, 40f);
            text.text = content;

            var layout = text.GetComponent<LayoutElement>();
            layout.preferredWidth = preferredWidth;
            layout.flexibleWidth = flexibleWidth;
        }
    }
}
