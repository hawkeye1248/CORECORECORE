using System.Collections.Generic;
using Building;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Bottom-of-screen hotbar that shows the build catalog and highlights the selected entry.
/// Builds its slots programmatically from <see cref="BuildingSystem.Buildables"/>, so it only
/// needs to live on a GameObject under a Canvas — no slot prefab wiring required.
/// Visible only while build mode is active.
/// </summary>
[RequireComponent(typeof(RectTransform))]
public class BuildHotbarUI : MonoBehaviour
{
    [Header("Layout")]
    [SerializeField] private Vector2 slotSize = new Vector2(96f, 96f);
    [SerializeField] private float spacing = 8f;
    [SerializeField] private float bottomMargin = 24f;

    [Header("Colors")]
    [SerializeField] private Color slotColor = new Color(0f, 0f, 0f, 0.5f);
    [SerializeField] private Color selectedColor = new Color(0.2f, 0.8f, 1f, 0.85f);
    [SerializeField] private Color numberColor = Color.white;
    [SerializeField] private Color countColor = Color.white;
    [SerializeField] private Color emptyColor = new Color(1f, 0.3f, 0.3f, 1f);

    private readonly List<Image> _slotBackgrounds = new List<Image>();
    private readonly List<Text> _countLabels = new List<Text>();
    private GameObject _root;
    private Font _font;

    private void Start()
    {
        _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        BuildSlots();
        SetVisible(false);

        if (BuildingSystem.Instance != null)
        {
            BuildingSystem.Instance.OnBuildModeChanged += OnBuildModeChanged;
            BuildingSystem.Instance.OnSelectionChanged += Highlight;
            BuildingSystem.Instance.OnCountChanged += UpdateCountLabel;
        }
    }

    private void OnDestroy()
    {
        if (BuildingSystem.Instance != null)
        {
            BuildingSystem.Instance.OnBuildModeChanged -= OnBuildModeChanged;
            BuildingSystem.Instance.OnSelectionChanged -= Highlight;
            BuildingSystem.Instance.OnCountChanged -= UpdateCountLabel;
        }
    }

    private void OnBuildModeChanged(bool active)
    {
        SetVisible(active);
        if (active && BuildingSystem.Instance != null)
        {
            Highlight(BuildingSystem.Instance.SelectedIndex);
            for (int i = 0; i < _countLabels.Count; i++) UpdateCountLabel(i);
        }
    }

    private void SetVisible(bool visible)
    {
        if (_root != null) _root.SetActive(visible);
    }

    private void BuildSlots()
    {
        _root = new GameObject("HotbarRoot", typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(ContentSizeFitter));
        var rootRt = _root.GetComponent<RectTransform>();
        rootRt.SetParent(transform, false);
        rootRt.anchorMin = new Vector2(0.5f, 0f);
        rootRt.anchorMax = new Vector2(0.5f, 0f);
        rootRt.pivot = new Vector2(0.5f, 0f);
        rootRt.anchoredPosition = new Vector2(0f, bottomMargin);

        var layout = _root.GetComponent<HorizontalLayoutGroup>();
        layout.spacing = spacing;
        layout.childAlignment = TextAnchor.LowerCenter;
        layout.childControlWidth = false;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;

        var fitter = _root.GetComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        IReadOnlyList<BuildableItem> items = BuildingSystem.Instance != null
            ? BuildingSystem.Instance.Buildables
            : null;
        int count = items != null ? items.Count : 0;
        for (int i = 0; i < count; i++)
            CreateSlot(i, items[i]);
    }

    private void CreateSlot(int index, BuildableItem item)
    {
        var slot = new GameObject($"Slot_{index}", typeof(RectTransform), typeof(Image), typeof(LayoutElement));
        var slotRt = slot.GetComponent<RectTransform>();
        slotRt.SetParent(_root.transform, false);
        slotRt.sizeDelta = slotSize;

        var le = slot.GetComponent<LayoutElement>();
        le.preferredWidth = slotSize.x;
        le.preferredHeight = slotSize.y;

        var bg = slot.GetComponent<Image>();
        bg.color = slotColor;
        bg.raycastTarget = false;
        _slotBackgrounds.Add(bg);

        if (item != null && item.icon != null)
        {
            var iconGo = new GameObject("Icon", typeof(RectTransform), typeof(Image));
            var iconRt = iconGo.GetComponent<RectTransform>();
            iconRt.SetParent(slotRt, false);
            iconRt.anchorMin = new Vector2(0.1f, 0.1f);
            iconRt.anchorMax = new Vector2(0.9f, 0.9f);
            iconRt.offsetMin = Vector2.zero;
            iconRt.offsetMax = Vector2.zero;

            var iconImg = iconGo.GetComponent<Image>();
            iconImg.sprite = item.icon;
            iconImg.preserveAspect = true;
            iconImg.raycastTarget = false;
        }

        var labelGo = new GameObject("Number", typeof(RectTransform), typeof(Text));
        var labelRt = labelGo.GetComponent<RectTransform>();
        labelRt.SetParent(slotRt, false);
        labelRt.anchorMin = Vector2.zero;
        labelRt.anchorMax = Vector2.one;
        labelRt.offsetMin = new Vector2(4f, 2f);
        labelRt.offsetMax = new Vector2(-4f, -2f);

        var label = labelGo.GetComponent<Text>();
        label.font = _font;
        label.text = (index + 1).ToString();
        label.alignment = TextAnchor.UpperLeft;
        label.fontSize = 20;
        label.color = numberColor;
        label.raycastTarget = false;

        // Remaining-count badge ("x5") in the bottom-right corner.
        var countGo = new GameObject("Count", typeof(RectTransform), typeof(Text));
        var countRt = countGo.GetComponent<RectTransform>();
        countRt.SetParent(slotRt, false);
        countRt.anchorMin = Vector2.zero;
        countRt.anchorMax = Vector2.one;
        countRt.offsetMin = new Vector2(4f, 2f);
        countRt.offsetMax = new Vector2(-4f, -2f);

        var countLabel = countGo.GetComponent<Text>();
        countLabel.font = _font;
        countLabel.alignment = TextAnchor.LowerRight;
        countLabel.fontSize = 22;
        countLabel.fontStyle = FontStyle.Bold;
        countLabel.color = countColor;
        countLabel.raycastTarget = false;
        _countLabels.Add(countLabel);
        UpdateCountLabel(index);
    }

    /// <summary>Refresh the "xN" badge for one slot from the building's remaining stock.</summary>
    private void UpdateCountLabel(int index)
    {
        if (index < 0 || index >= _countLabels.Count) return;

        IReadOnlyList<BuildableItem> items = BuildingSystem.Instance != null
            ? BuildingSystem.Instance.Buildables
            : null;
        BuildableItem item = (items != null && index < items.Count) ? items[index] : null;

        Text label = _countLabels[index];
        if (item == null || item.Unlimited)
        {
            label.text = string.Empty; // unlimited buildings show no badge
            return;
        }

        label.text = "x" + item.Remaining;
        label.color = item.Remaining > 0 ? countColor : emptyColor;
    }

    private void Highlight(int index)
    {
        for (int i = 0; i < _slotBackgrounds.Count; i++)
            _slotBackgrounds[i].color = (i == index) ? selectedColor : slotColor;
    }
}
