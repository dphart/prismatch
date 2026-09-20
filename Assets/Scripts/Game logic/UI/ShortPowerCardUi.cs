using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ShortPowerCardUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI descriptionText;
    [SerializeField] private Image icon;
    [SerializeField] private Button button;

    private PowerUp currentPower;
    private Action<PowerUp> clickAction;
    public string PowerName => currentPower?.Name;

    public void ApplyResponsiveLayout(float width, float height)
    {
        if (transform is not RectTransform cardRect) return;
        cardRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, width);
        cardRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, height);

        var container = nameText != null ? nameText.transform.parent as RectTransform : null;
        if (container == null) return;
        container.anchorMin = Vector2.zero;
        container.anchorMax = Vector2.one;
        container.offsetMin = Vector2.zero;
        container.offsetMax = Vector2.zero;
        var fitter = container.GetComponent<ContentSizeFitter>();
        if (fitter != null) fitter.enabled = false;

        var horizontal = container.GetComponent<HorizontalLayoutGroup>();
        if (horizontal == null) horizontal = container.gameObject.AddComponent<HorizontalLayoutGroup>();
        horizontal.padding = new RectOffset(18, 58, 18, 18);
        horizontal.spacing = 18f;
        horizontal.childAlignment = TextAnchor.MiddleLeft;
        horizontal.childControlWidth = true;
        horizontal.childControlHeight = true;
        horizontal.childForceExpandWidth = false;
        horizontal.childForceExpandHeight = false;

        if (icon != null)
        {
            var iconLayout = icon.GetComponent<LayoutElement>();
            if (iconLayout == null) iconLayout = icon.gameObject.AddComponent<LayoutElement>();
            iconLayout.minWidth = iconLayout.preferredWidth = 110f;
            iconLayout.minHeight = iconLayout.preferredHeight = 110f;
            iconLayout.flexibleWidth = iconLayout.flexibleHeight = 0f;
            icon.transform.SetSiblingIndex(0);
        }

        var column = new GameObject("TextColumn", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(LayoutElement));
        column.transform.SetParent(container, false);
        var columnLayout = column.GetComponent<LayoutElement>();
        columnLayout.minWidth = 0f;
        columnLayout.flexibleWidth = 1f;
        var vertical = column.GetComponent<VerticalLayoutGroup>();
        vertical.spacing = 8f;
        vertical.childAlignment = TextAnchor.MiddleLeft;
        vertical.childControlWidth = true;
        vertical.childControlHeight = true;
        vertical.childForceExpandWidth = true;
        vertical.childForceExpandHeight = false;

        float textWidth = Mathf.Max(80f, width - 204f);
        float nameHeight = ConfigureText(nameText, column.transform, 28f, textWidth, false);
        float descriptionHeight = ConfigureText(descriptionText, column.transform, 20f, textWidth, true);
        columnLayout.minHeight = nameHeight + descriptionHeight + vertical.spacing;
        columnLayout.preferredHeight = columnLayout.minHeight;
        columnLayout.flexibleHeight = 0f;
        LayoutRebuilder.ForceRebuildLayoutImmediate(container);
    }

    private static float ConfigureText(TextMeshProUGUI text, Transform parent, float fontSize, float width, bool wraps)
    {
        if (text == null) return 0f;
        text.transform.SetParent(parent, false);
        text.fontSize = fontSize;
        text.enableAutoSizing = false;
        text.textWrappingMode = wraps ? TextWrappingModes.Normal : TextWrappingModes.NoWrap;
        text.overflowMode = TextOverflowModes.Ellipsis;
        text.alignment = wraps ? TextAlignmentOptions.TopLeft : TextAlignmentOptions.Left;
        float height = Mathf.Ceil(text.GetPreferredValues(text.text, width, Mathf.Infinity).y) + 2f;
        if (wraps) height = Mathf.Min(height, 106f);
        var layout = text.GetComponent<LayoutElement>();
        if (layout == null) layout = text.gameObject.AddComponent<LayoutElement>();
        layout.minWidth = 0f;
        layout.preferredWidth = 0f;
        layout.flexibleWidth = 1f;
        layout.minHeight = height;
        layout.preferredHeight = height;
        layout.flexibleHeight = 0f;
        return height;
    }

    /// <summary>
    /// Setup the card UI and attach the click callback.
    /// </summary>
    public void Setup(PowerUp power, Action<PowerUp> onClick, bool useDynamicDescription = false)
    {
        currentPower = power;
        clickAction = onClick;

        // Populate UI
        nameText.text = power.Name;
        descriptionText.text = useDynamicDescription ? power.GetCurrentPowerDescription() : power.PowerUpDescription;
        var powerIcon = power.Icon != null ? power.Icon : PowerUpIconLibrary.For(power.Name);
        if (icon != null && powerIcon != null)
        {
            icon.sprite = powerIcon;
            icon.color = Color.white;
            icon.preserveAspect = true;
        }
        ApplyPowerStyle(power.Name);

        // Hook up click event
        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(() => clickAction?.Invoke(currentPower));
    }

    private void ApplyPowerStyle(string powerName)
    {
        Color accent = PrismatchTheme.PowerColor(powerName);
        var background = GetComponent<Image>();
        PrismatchTheme.Round(background, Color.Lerp(PrismatchTheme.Card, accent, .09f));
        PrismatchTheme.Shadow(background, new Color(0f, .05f, .15f, .42f), new Vector2(0f, -5f));
        var outline = GetComponent<Outline>();
        if (outline == null) outline = gameObject.AddComponent<Outline>();
        outline.effectColor = new Color(accent.r, accent.g, accent.b, .5f);
        outline.effectDistance = new Vector2(1.5f, -1.5f);
        if (nameText != null) { nameText.color = PrismatchTheme.Navy; nameText.fontStyle = FontStyles.Bold; }
        if (descriptionText != null) descriptionText.color = new Color(.20f, .25f, .34f, 1f);
        if (GetComponent<PowerCardFeedback>() == null) gameObject.AddComponent<PowerCardFeedback>();

        var arrow = transform.Find("SelectionArrow")?.GetComponent<TextMeshProUGUI>();
        if (arrow == null)
        {
            var go = new GameObject("SelectionArrow", typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(transform, false);
            var rect = (RectTransform)go.transform;
            rect.anchorMin = rect.anchorMax = new Vector2(1f, .5f);
            rect.pivot = new Vector2(1f, .5f);
            rect.anchoredPosition = new Vector2(-16f, 0f);
            rect.sizeDelta = new Vector2(36f, 64f);
            arrow = go.GetComponent<TextMeshProUGUI>();
        }
        arrow.text = "›";
        arrow.fontSize = 50f;
        arrow.fontStyle = FontStyles.Bold;
        arrow.alignment = TextAlignmentOptions.Center;
        arrow.color = new Color(PrismatchTheme.Navy.r, PrismatchTheme.Navy.g, PrismatchTheme.Navy.b, .4f);
        arrow.raycastTarget = false;
    }

    public void SetupBossPower(BossPowers bossPower)
    {
        nameText.text = bossPower.Name;
        descriptionText.text = bossPower.PowerUpDescription;

        if (bossPower.Icon != null)
            icon.sprite = bossPower.Icon;
    }
}

public static class PowerUpIconLibrary
{
    private static readonly System.Collections.Generic.Dictionary<string, Sprite> Cache = new();

    public static Sprite For(string powerName)
    {
        if (string.IsNullOrWhiteSpace(powerName)) return null;
        if (Cache.TryGetValue(powerName, out var cached)) return cached;
        string resource = powerName switch
        {
            "Momentum Start" => "UI/Generated/power-momentum",
            "Steady Start" => "UI/Generated/power-steady",
            "Precision Start" => "UI/Generated/power-precision",
            _ => null
        };
        var sprite = resource == null ? null : Resources.Load<Sprite>(resource);
        Cache[powerName] = sprite;
        return sprite;
    }
}
