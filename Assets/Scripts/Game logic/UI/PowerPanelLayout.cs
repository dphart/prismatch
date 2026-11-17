using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class PowerPanelLayout : MonoBehaviour
{
    public const float Margin = 16f;
    public const float PanelHeight = 1120f;
    public const float ScreenMargin = 48f;
    public const float VerticalListPadding = 8f;
    public const float ViewportHeight = PanelHeight - 164f;
    public const float CardHeight = 212f;
    public const float CardSpacing = 18f;

    // Preserve edge-to-edge cards on phones/tablets, even in landscape.
    // Narrow desktop windows also naturally retain the mobile layout.
    public static float PanelWidth(float availableWidth, bool mobile)
    {
        float width = Mathf.Max(0f, availableWidth - Margin * 2f);
        return mobile ? width : Mathf.Min(900f, width);
    }

    public static float Configure(GameObject panel, Transform content, string heading,
        string subtitle = null, bool reserveCloseButton = true)
    {
        if (panel == null || content == null) return 0f;
        var view = panel.GetComponent<PowerPanelLayout>();
        if(view==null)view=panel.AddComponent<PowerPanelLayout>();
        return view.ConfigureView(content, heading, subtitle, reserveCloseButton);
    }

    private float ConfigureView(Transform content, string heading, string subtitle, bool reserveCloseButton)
    {
        var panel = gameObject;
        if (panel.transform is RectTransform panelRect && panelRect.parent is RectTransform parentRect)
        {
            panelRect.anchorMin = panelRect.anchorMax = panelRect.pivot = new Vector2(.5f, .5f);
            panelRect.anchoredPosition = Vector2.zero;
            panelRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal,
                PanelWidth(parentRect.rect.width, Application.isMobilePlatform));
            ApplySharedHeight(PanelHeight);
        }

        panel.transform.SetAsLastSibling();
        var modalCanvas = panel.GetComponent<Canvas>();
        if(modalCanvas==null)modalCanvas=panel.AddComponent<Canvas>();
        modalCanvas.overrideSorting = true;
        modalCanvas.sortingOrder = 50;
        if (panel.GetComponent<GraphicRaycaster>() == null) panel.AddComponent<GraphicRaycaster>();
        if (panel.TryGetComponent(out Image panelImage))
        {
            PrismatchTheme.Round(panelImage,
                PrismatchTheme.Navy);
            PrismatchTheme.Shadow(panelImage, new Color(0f, .04f, .12f, .55f), new Vector2(0f, -7f));
        }

        foreach (var label in panel.GetComponentsInChildren<TextMeshProUGUI>(true))
        {
            if (label.name == "PowerChoiceSubtitle" || label.GetComponentInParent<Button>() != null) continue;
            if (label.transform.IsChildOf(content)) continue;
            label.rectTransform.anchorMin = new Vector2(0, 1);
            label.rectTransform.anchorMax = Vector2.one;
            label.rectTransform.pivot = new Vector2(.5f, 1);
            label.rectTransform.anchoredPosition = new Vector2(reserveCloseButton ? -36f : 0f, -20f);
            label.rectTransform.sizeDelta = new Vector2(reserveCloseButton ? -110f : -32f, 68f);
            label.raycastTarget = false;
            label.text = heading;
            label.fontSize = 46f;
            label.enableAutoSizing = true;
            label.fontSizeMin = 28f;
            label.fontSizeMax = 46f;
            label.fontStyle = FontStyles.Bold;
            label.color = PrismatchTheme.Cream;
            label.alignment = TextAlignmentOptions.Center;
            break;
        }

        var subtitleLabel = panel.transform.Find("PowerChoiceSubtitle")?.GetComponent<TextMeshProUGUI>();
        if (!string.IsNullOrEmpty(subtitle))
        {
            if (subtitleLabel == null)
            {
                subtitleLabel = new GameObject("PowerChoiceSubtitle", typeof(RectTransform), typeof(TextMeshProUGUI)).GetComponent<TextMeshProUGUI>();
                subtitleLabel.transform.SetParent(panel.transform, false);
            }
            subtitleLabel.gameObject.SetActive(true);
            subtitleLabel.text = subtitle;
            subtitleLabel.rectTransform.anchorMin = new Vector2(0f, 1f);
            subtitleLabel.rectTransform.anchorMax = Vector2.one;
            subtitleLabel.rectTransform.pivot = new Vector2(.5f, 1f);
            subtitleLabel.rectTransform.anchoredPosition = new Vector2(0f, -92f);
            subtitleLabel.rectTransform.sizeDelta = new Vector2(-32f, 36f);
            subtitleLabel.fontSize = 24f;
            subtitleLabel.enableAutoSizing = true;
            subtitleLabel.fontSizeMin = 16f;
            subtitleLabel.fontSizeMax = 24f;
            subtitleLabel.alignment = TextAlignmentOptions.Center;
            subtitleLabel.color = PrismatchTheme.Mist;
            subtitleLabel.raycastTarget = false;
        }
        else if (subtitleLabel != null) subtitleLabel.gameObject.SetActive(false);

        var scroll = panel.GetComponentInChildren<ScrollRect>(true);
        if (scroll != null)
        {
            scroll.vertical = true;
            if (scroll.transform is RectTransform scrollRect)
            {
                scrollRect.anchorMin = Vector2.zero;
                scrollRect.anchorMax = Vector2.one;
                scrollRect.offsetMin = new Vector2(0, 16);
                scrollRect.offsetMax = new Vector2(0, -148);
            }
        }

        if (content is RectTransform contentRect)
        {
            contentRect.anchorMin = new Vector2(0f, 1f);
            contentRect.anchorMax = new Vector2(1f, 1f);
            contentRect.pivot = new Vector2(.5f, 1f);
            contentRect.anchoredPosition = new Vector2(0f, contentRect.anchoredPosition.y);
            contentRect.sizeDelta = new Vector2(0f, contentRect.sizeDelta.y);
        }
        if (content.TryGetComponent(out VerticalLayoutGroup layout))
        {
            layout.padding.left = layout.padding.right = Mathf.RoundToInt(Margin);
            layout.padding.top = layout.padding.bottom = 8;
            layout.spacing = CardSpacing;
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlHeight = true;
            layout.childForceExpandHeight = false;
            layout.childControlWidth = true;
            layout.childForceExpandWidth = false;
        }
        var fitter = content.GetComponent<ContentSizeFitter>();
        if (fitter != null)
        {
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        }

        Canvas.ForceUpdateCanvases();
        float viewportWidth = scroll != null && scroll.viewport != null
            ? scroll.viewport.rect.width
            : ((RectTransform)panel.transform).rect.width;
        return Mathf.Max(0f, viewportWidth - Margin * 2f);
    }

    public static void FitToContent(GameObject panel, Transform content)
    {
        var scroll = panel.GetComponentInChildren<ScrollRect>(true);
        if (scroll == null || scroll.viewport == null ||
            panel.transform is not RectTransform panelRect || panelRect.parent is not RectTransform parentRect) return;
        // Long descriptions can make one card taller than its initial third of the list.
        // Grow the panel to fit the actual cards rather than clipping the last choice.
        float contentHeight = VerticalListPadding * 2f;
        int count = 0;
        foreach (Transform child in content)
        {
            if (!child.gameObject.activeSelf || child.GetComponent<ShortPowerCardUI>() == null) continue;
            contentHeight += LayoutUtility.GetPreferredHeight((RectTransform)child);
            count++;
        }
        contentHeight += CardSpacing * Mathf.Max(0, count - 1);
        float extraHeight = Mathf.Max(0f, contentHeight - scroll.viewport.rect.height);
        panel.GetComponent<PowerPanelLayout>().ApplySharedHeight(panelRect.rect.height + extraHeight);
        Canvas.ForceUpdateCanvases();
    }

    private void ApplySharedHeight(float desiredHeight)
    {
        if (transform.parent is not RectTransform parent) return;
        var views = parent.GetComponentsInChildren<PowerPanelLayout>(true);
        foreach (var view in views)
            if (view.transform.parent == parent)
                desiredHeight = Mathf.Max(desiredHeight, ((RectTransform)view.transform).rect.height);
        float height = Mathf.Min(desiredHeight, Mathf.Max(0f, parent.rect.height - ScreenMargin * 2f));
        foreach (var view in views)
            if (view.transform.parent == parent)
                ((RectTransform)view.transform).SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, height);
    }

    public static void SizeCard(RectTransform card, float width, float height = CardHeight)
    {
        if (card == null) return;
        card.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, width);
        card.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, height);
        var element = card.GetComponent<LayoutElement>();
        if(element==null)element=card.gameObject.AddComponent<LayoutElement>();
        element.minWidth = element.preferredWidth = width;
        element.flexibleWidth = 0f;
        element.minHeight = element.preferredHeight = height;
        element.flexibleHeight = 0f;
    }
}
