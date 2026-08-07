using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class DebugToolsPanel : MonoBehaviour
{
    private IDebugToolsService debugTools;
    private DebugContext context;
    private GameObject panelRoot;
    private Transform contentRoot;
    private bool isOpen;

    public void Initialize(
        IDebugToolsService service,
        ISaveService saveService,
        IEconomyService economyService,
        IAdsService adsService)
    {
        debugTools = service;
        context = new DebugContext(saveService, economyService, adsService, service);
        BuildUi();
        DebugToolsService.PanelRefreshRequested += RebuildContent;
    }

    private void OnDestroy()
    {
        DebugToolsService.PanelRefreshRequested -= RebuildContent;
    }

    private void BuildUi()
    {
        var canvasGo = new GameObject("DebugToolsCanvas");
        canvasGo.transform.SetParent(transform, false);

        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = short.MaxValue;

        var scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080f, 1920f);
        scaler.matchWidthOrHeight = 0.5f;

        canvasGo.AddComponent<GraphicRaycaster>();

        CreateOpenButton(canvasGo.transform);
        CreatePanel(canvasGo.transform);
        RebuildContent();
    }

    private void CreateOpenButton(Transform parent)
    {
        var buttonGo = CreateUiObject("OpenButton", parent);
        var rect = buttonGo.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = new Vector2(16f, -16f);
        rect.sizeDelta = new Vector2(96f, 48f);

        var image = buttonGo.AddComponent<Image>();
        image.color = new Color(0.1f, 0.1f, 0.1f, 0.85f);

        var button = buttonGo.AddComponent<Button>();
        button.targetGraphic = image;
        button.onClick.AddListener(TogglePanel);

        CreateLabel(buttonGo.transform, "DBG", 20, TextAlignmentOptions.Center);
    }

    private void CreatePanel(Transform parent)
    {
        panelRoot = CreateUiObject("Panel", parent);
        var panelRect = panelRoot.GetComponent<RectTransform>();
        panelRect.anchorMin = Vector2.zero;
        panelRect.anchorMax = Vector2.one;
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;

        var backdrop = panelRoot.AddComponent<Image>();
        backdrop.color = new Color(0f, 0f, 0f, 0.75f);

        var window = CreateUiObject("Window", panelRoot.transform);
        var windowRect = window.GetComponent<RectTransform>();
        windowRect.anchorMin = new Vector2(0.5f, 0.5f);
        windowRect.anchorMax = new Vector2(0.5f, 0.5f);
        windowRect.pivot = new Vector2(0.5f, 0.5f);
        windowRect.sizeDelta = new Vector2(760f, 1200f);

        var windowImage = window.AddComponent<Image>();
        windowImage.color = new Color(0.12f, 0.12f, 0.14f, 0.98f);

        CreateLabel(window.transform, "Debug Tools", 34, TextAlignmentOptions.Center, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -40f), new Vector2(680f, 60f));

        var closeGo = CreateUiObject("CloseButton", window.transform);
        var closeRect = closeGo.GetComponent<RectTransform>();
        closeRect.anchorMin = new Vector2(1f, 1f);
        closeRect.anchorMax = new Vector2(1f, 1f);
        closeRect.pivot = new Vector2(1f, 1f);
        closeRect.anchoredPosition = new Vector2(-16f, -16f);
        closeRect.sizeDelta = new Vector2(120f, 56f);

        var closeImage = closeGo.AddComponent<Image>();
        closeImage.color = new Color(0.25f, 0.25f, 0.28f, 1f);
        var closeButton = closeGo.AddComponent<Button>();
        closeButton.targetGraphic = closeImage;
        closeButton.onClick.AddListener(ClosePanel);
        CreateLabel(closeGo.transform, "Close", 22, TextAlignmentOptions.Center);

        var scrollGo = CreateUiObject("Scroll", window.transform);
        var scrollRect = scrollGo.GetComponent<RectTransform>();
        scrollRect.anchorMin = new Vector2(0f, 0f);
        scrollRect.anchorMax = new Vector2(1f, 1f);
        scrollRect.offsetMin = new Vector2(24f, 24f);
        scrollRect.offsetMax = new Vector2(-24f, -100f);

        var scroll = scrollGo.AddComponent<ScrollRect>();
        scroll.horizontal = false;
        scroll.movementType = ScrollRect.MovementType.Clamped;

        var viewport = CreateUiObject("Viewport", scrollGo.transform);
        Stretch(viewport.GetComponent<RectTransform>());
        viewport.AddComponent<RectMask2D>();

        contentRoot = CreateUiObject("Content", viewport.transform).transform;
        var contentRect = contentRoot.GetComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0f, 1f);
        contentRect.anchorMax = new Vector2(1f, 1f);
        contentRect.pivot = new Vector2(0f, 1f);
        contentRect.anchoredPosition = Vector2.zero;
        contentRect.sizeDelta = new Vector2(0f, 0f);

        var layout = contentRoot.gameObject.AddComponent<VerticalLayoutGroup>();
        layout.spacing = 12f;
        layout.padding = new RectOffset(16, 16, 8, 8);
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;
        layout.childAlignment = TextAnchor.UpperLeft;

        var fitter = contentRoot.gameObject.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        scroll.viewport = viewport.GetComponent<RectTransform>();
        scroll.content = contentRect;

        panelRoot.SetActive(false);
    }

    private void RebuildContent()
    {
        if (contentRoot == null || debugTools == null)
            return;

        for (int i = contentRoot.childCount - 1; i >= 0; i--)
            Destroy(contentRoot.GetChild(i).gameObject);

        var categories = new Dictionary<string, List<IDebugAction>>();
        foreach (var action in debugTools.Actions)
        {
            if (!categories.TryGetValue(action.Category, out var list))
            {
                list = new List<IDebugAction>();
                categories[action.Category] = list;
            }

            list.Add(action);
        }

        foreach (var pair in categories)
        {
            CreateCategoryHeader(pair.Key);

            foreach (var action in pair.Value)
                CreateActionRow(action);
        }

#if UNITY_EDITOR
        CreateCategoryHeader("Editor Shortcuts");
        CreateInfoRow("W - Force Win");
        CreateInfoRow("L - Force Lose");
#endif

        LayoutRebuilder.ForceRebuildLayoutImmediate(contentRoot.GetComponent<RectTransform>());
    }

    private static LayoutElement AddRowLayoutElement(GameObject row, float minHeight)
    {
        var layout = row.GetComponent<LayoutElement>();
        if (layout == null)
            layout = row.AddComponent<LayoutElement>();

        layout.minHeight = minHeight;
        layout.preferredHeight = minHeight;
        layout.flexibleWidth = 1f;
        layout.minWidth = 0f;
        return layout;
    }

    private void CreateCategoryHeader(string title)
    {
        var row = CreateUiObject("Category", contentRoot);
        AddRowLayoutElement(row, 44f);
        ConfigureLabel(CreateAnchoredLabel(row.transform, title, 26, TextAlignmentOptions.MidlineLeft, 0f, 1f, 16f, 8f));
    }

    private void CreateInfoRow(string text)
    {
        var row = CreateUiObject("Info", contentRoot);
        AddRowLayoutElement(row, 36f);
        ConfigureLabel(CreateAnchoredLabel(row.transform, text, 20, TextAlignmentOptions.MidlineLeft, 0f, 1f, 16f, 8f));
    }

    private void CreateActionRow(IDebugAction action)
    {
        var row = CreateUiObject(action.Id, contentRoot);
        AddRowLayoutElement(row, action.Kind == DebugActionKind.IntField ? 80f : 72f);

        ConfigureLabel(CreateAnchoredLabel(
            row.transform,
            action.Label,
            22,
            TextAlignmentOptions.MidlineLeft,
            0f,
            action.Kind == DebugActionKind.IntField ? 0.42f : 0.58f,
            16f,
            8f));

        var available = action.IsAvailable(context);

        switch (action.Kind)
        {
            case DebugActionKind.Button:
                CreateActionButton(row.transform, action, available, 0.6f, 0.98f);
                break;
            case DebugActionKind.Toggle:
                CreateActionToggle(row.transform, action, available, 0.6f, 0.98f);
                break;
            case DebugActionKind.IntField:
                CreateIntFieldRow(row.transform, action, available);
                break;
        }
    }

    private void CreateActionButton(Transform parent, IDebugAction action, bool available, float anchorMinX, float anchorMaxX)
    {
        var buttonGo = CreateUiObject("Run", parent);
        SetAnchoredRect(buttonGo.GetComponent<RectTransform>(), anchorMinX, 0.12f, anchorMaxX, 0.88f, 0f, 0f);

        var image = buttonGo.AddComponent<Image>();
        image.color = available ? new Color(0.2f, 0.45f, 0.25f, 1f) : new Color(0.25f, 0.25f, 0.25f, 0.6f);

        var button = buttonGo.AddComponent<Button>();
        button.targetGraphic = image;
        button.interactable = available;
        button.onClick.AddListener(() => RunActionAndClose(() => action.Execute(context)));
        CreateCenteredLabel(buttonGo.transform, "Run", 20);
    }

    private void CreateActionToggle(Transform parent, IDebugAction action, bool available, float anchorMinX, float anchorMaxX)
    {
        var buttonGo = CreateUiObject("Toggle", parent);
        SetAnchoredRect(buttonGo.GetComponent<RectTransform>(), anchorMinX, 0.12f, anchorMaxX, 0.88f, 0f, 0f);

        var image = buttonGo.AddComponent<Image>();
        image.color = available ? new Color(0.28f, 0.28f, 0.32f, 1f) : new Color(0.25f, 0.25f, 0.25f, 0.6f);

        var button = buttonGo.AddComponent<Button>();
        button.targetGraphic = image;
        button.interactable = available;

        var label = CreateCenteredLabel(buttonGo.transform, action.GetToggleState(context) ? "On" : "Off", 20);
        button.onClick.AddListener(() =>
        {
            var nextState = !action.GetToggleState(context);
            action.Execute(context, nextState ? 1 : 0);
            label.text = nextState ? "On" : "Off";
            ClosePanel();
        });
    }

    private void CreateIntFieldRow(Transform parent, IDebugAction action, bool available)
    {
        var fieldGo = CreateUiObject("Field", parent);
        SetAnchoredRect(fieldGo.GetComponent<RectTransform>(), 0.45f, 0.12f, 0.72f, 0.88f, 0f, 0f);

        var fieldImage = fieldGo.AddComponent<Image>();
        fieldImage.color = new Color(0.18f, 0.18f, 0.2f, 1f);

        var input = fieldGo.AddComponent<TMP_InputField>();
        input.textComponent = CreateInputText(fieldGo.transform);
        input.contentType = TMP_InputField.ContentType.IntegerNumber;
        input.text = action.Id == DebugActionIds.AddCoins ? "100" : "500";

        var applyGo = CreateUiObject("Apply", parent);
        SetAnchoredRect(applyGo.GetComponent<RectTransform>(), 0.74f, 0.12f, 0.98f, 0.88f, 0f, 0f);

        var applyImage = applyGo.AddComponent<Image>();
        applyImage.color = available ? new Color(0.2f, 0.35f, 0.55f, 1f) : new Color(0.25f, 0.25f, 0.25f, 0.6f);
        var applyButton = applyGo.AddComponent<Button>();
        applyButton.targetGraphic = applyImage;
        applyButton.interactable = available;
        applyButton.onClick.AddListener(() =>
        {
            if (!int.TryParse(input.text, out var value))
                value = 0;

            RunActionAndClose(() => action.Execute(context, value));
        });
        CreateCenteredLabel(applyGo.transform, "Apply", 20);
    }

    private TextMeshProUGUI CreateInputText(Transform parent)
    {
        var textGo = CreateUiObject("Text", parent);
        Stretch(textGo.GetComponent<RectTransform>(), 12f, 8f, 12f, 8f);
        var text = textGo.AddComponent<TextMeshProUGUI>();
        text.fontSize = 22f;
        text.alignment = TextAlignmentOptions.MidlineLeft;
        text.color = Color.white;
        return text;
    }

    private static void ConfigureLabel(TextMeshProUGUI label)
    {
        label.enableWordWrapping = true;
        label.overflowMode = TextOverflowModes.Ellipsis;
        label.margin = new Vector4(4f, 0f, 4f, 0f);
    }

    private static void SetAnchoredRect(
        RectTransform rect,
        float anchorMinX,
        float anchorMinY,
        float anchorMaxX,
        float anchorMaxY,
        float leftPadding,
        float rightPadding)
    {
        rect.anchorMin = new Vector2(anchorMinX, anchorMinY);
        rect.anchorMax = new Vector2(anchorMaxX, anchorMaxY);
        rect.offsetMin = new Vector2(leftPadding, 0f);
        rect.offsetMax = new Vector2(-rightPadding, 0f);
    }

    private TextMeshProUGUI CreateAnchoredLabel(
        Transform parent,
        string text,
        float fontSize,
        TextAlignmentOptions alignment,
        float anchorMinX,
        float anchorMaxX,
        float leftPadding,
        float rightPadding)
    {
        var labelGo = CreateUiObject("Label", parent);
        SetAnchoredRect(labelGo.GetComponent<RectTransform>(), anchorMinX, 0f, anchorMaxX, 1f, leftPadding, rightPadding);

        var label = labelGo.AddComponent<TextMeshProUGUI>();
        label.text = text;
        label.fontSize = fontSize;
        label.alignment = alignment;
        label.color = Color.white;
        ConfigureLabel(label);
        return label;
    }

    private TextMeshProUGUI CreateCenteredLabel(Transform parent, string text, float fontSize)
    {
        var labelGo = CreateUiObject("Label", parent);
        Stretch(labelGo.GetComponent<RectTransform>());

        var label = labelGo.AddComponent<TextMeshProUGUI>();
        label.text = text;
        label.fontSize = fontSize;
        label.alignment = TextAlignmentOptions.Center;
        label.color = Color.white;
        return label;
    }

    private TextMeshProUGUI CreateLabel(
        Transform parent,
        string text,
        float fontSize,
        TextAlignmentOptions alignment,
        Vector2? anchorMin = null,
        Vector2? anchorMax = null,
        Vector2? anchoredPosition = null,
        Vector2? sizeDelta = null,
        bool stretch = false)
    {
        var labelGo = CreateUiObject("Label", parent);
        var rect = labelGo.GetComponent<RectTransform>();

        if (stretch)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0f, 0.5f);
            rect.offsetMin = new Vector2(4f, 0f);
            rect.offsetMax = new Vector2(-4f, 0f);
        }
        else
        {
            rect.anchorMin = anchorMin ?? Vector2.zero;
            rect.anchorMax = anchorMax ?? Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = anchoredPosition ?? Vector2.zero;
            rect.sizeDelta = sizeDelta ?? Vector2.zero;
        }

        var label = labelGo.AddComponent<TextMeshProUGUI>();
        label.text = text;
        label.fontSize = fontSize;
        label.alignment = alignment;
        label.color = Color.white;
        ConfigureLabel(label);
        return label;
    }

    private static GameObject CreateUiObject(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return go;
    }

    private static void Stretch(RectTransform rect, float left = 0f, float bottom = 0f, float right = 0f, float top = 0f)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = new Vector2(left, bottom);
        rect.offsetMax = new Vector2(-right, -top);
    }

    private void TogglePanel()
    {
        if (isOpen)
            ClosePanel();
        else
            OpenPanel();
    }

    private void OpenPanel()
    {
        isOpen = true;
        panelRoot.SetActive(true);
        RebuildContent();
    }

    private void RunActionAndClose(Action action)
    {
        action?.Invoke();
        ClosePanel();
    }

    private void ClosePanel()
    {
        isOpen = false;
        panelRoot.SetActive(false);
    }
}
