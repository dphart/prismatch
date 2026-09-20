using TMPro;
using UnityEngine;
using UnityEngine.UI;

public static class PowerPanelLayout
{
    public const float Margin = 16f;
    public const float PanelHeight = 760f;
    public const float ViewportHeight = 580f;
    public const float CardHeight = 180f;
    public const float CardSpacing = 12f;

    public static float Configure(GameObject panel, Transform content, string heading)
    {
        if (panel == null || content == null) return 0f;
        if (panel.transform is RectTransform panelRect && panelRect.parent is RectTransform parentRect)
        {
            panelRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal,
                Mathf.Max(0f, parentRect.rect.width - Margin * 2f));
            panelRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical,
                Mathf.Min(PanelHeight, Mathf.Max(0f, parentRect.rect.height - Margin * 2f)));
        }

        if (panel.TryGetComponent(out Image panelImage))
        {
            PrismatchTheme.Round(panelImage,
                new Color(PrismatchTheme.Navy.r, PrismatchTheme.Navy.g, PrismatchTheme.Navy.b, .97f));
            PrismatchTheme.Shadow(panelImage, new Color(0f, .04f, .12f, .55f), new Vector2(0f, -7f));
        }

        foreach (var label in panel.GetComponentsInChildren<TextMeshProUGUI>(true))
        {
            if (!label.name.Contains("Text") && !label.name.Contains("Title")) continue;
            if (label.transform.IsChildOf(content)) continue;
            label.text = heading;
            label.fontSize = 38f;
            label.fontStyle = FontStyles.Bold;
            label.color = PrismatchTheme.Cream;
            label.alignment = TextAlignmentOptions.Center;
            break;
        }

        var scroll = panel.GetComponentInChildren<ScrollRect>(true);
        if (scroll != null)
        {
            scroll.vertical = true;
            if (scroll.transform is RectTransform scrollRect)
            {
                scrollRect.anchorMin = scrollRect.anchorMax = new Vector2(.5f, .5f);
                scrollRect.anchoredPosition = new Vector2(0f, -55f);
                scrollRect.sizeDelta = new Vector2(0f, ViewportHeight);
                scrollRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal,
                    Mathf.Max(0f, ((RectTransform)panel.transform).rect.width));
            }
        }

        if (content is RectTransform contentRect)
        {
            contentRect.anchorMin = new Vector2(0f, contentRect.anchorMin.y);
            contentRect.anchorMax = new Vector2(1f, contentRect.anchorMax.y);
            contentRect.pivot = new Vector2(.5f, contentRect.pivot.y);
            contentRect.anchoredPosition = new Vector2(0f, contentRect.anchoredPosition.y);
            contentRect.sizeDelta = new Vector2(0f, contentRect.sizeDelta.y);
        }
        if (content.TryGetComponent(out VerticalLayoutGroup layout))
        {
            layout.padding.left = layout.padding.right = Mathf.RoundToInt(Margin);
            layout.padding.top = layout.padding.bottom = 8;
            layout.spacing = CardSpacing;
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlWidth = true;
            layout.childForceExpandWidth = false;
        }
        var fitter = content.GetComponent<ContentSizeFitter>();
        if (fitter != null) fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;

        Canvas.ForceUpdateCanvases();
        float viewportWidth = scroll != null && scroll.viewport != null
            ? scroll.viewport.rect.width
            : ((RectTransform)panel.transform).rect.width;
        return Mathf.Max(0f, viewportWidth - Margin * 2f);
    }

    public static void SizeCard(RectTransform card, float width, float height = CardHeight)
    {
        if (card == null) return;
        card.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, width);
        card.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, height);
        var element = card.GetComponent<LayoutElement>() ?? card.gameObject.AddComponent<LayoutElement>();
        element.minWidth = element.preferredWidth = width;
        element.flexibleWidth = 0f;
        element.minHeight = element.preferredHeight = height;
        element.flexibleHeight = 0f;
    }
}
