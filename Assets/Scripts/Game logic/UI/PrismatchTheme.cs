using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public static class PrismatchTheme
{
    public static readonly Color Midnight = Hex("0B1F3A");
    public static readonly Color Navy = Hex("132A4F");
    public static readonly Color RaisedNavy = Hex("1D3B68");
    public static readonly Color Sky = Hex("58A6F6");
    public static readonly Color Orange = Hex("FF9F1C");
    public static readonly Color Coral = Hex("FF6B6B");
    public static readonly Color Green = Hex("42CC72");
    public static readonly Color Cream = Hex("FFF7E8");
    public static readonly Color Mist = Hex("AFC7E8");
    public static readonly Color Card = Hex("F7F3EA");
    public static Color TierColor(PowerTier tier) => tier switch {
        PowerTier.Rare => Hex("A56EFF"),
        PowerTier.Legendary => Hex("FFB52E"),
        _ => Green
    };

    private static Sprite roundedSprite;

    public static Sprite RoundedSprite
    {
        get
        {
            if (roundedSprite != null) return roundedSprite;
            const int size = 64;
            const float radius = 15f;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                name = "RuntimeRoundedRectangle",
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.HideAndDontSave
            };
            var pixels = new Color32[size * size];
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float dx = Mathf.Max(radius - x, x - (size - 1 - radius), 0f);
                float dy = Mathf.Max(radius - y, y - (size - 1 - radius), 0f);
                float alpha = Mathf.Clamp01(radius + .5f - Mathf.Sqrt(dx * dx + dy * dy));
                pixels[y * size + x] = new Color(1, 1, 1, alpha);
            }
            texture.SetPixels32(pixels);
            texture.Apply();
            roundedSprite = Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(.5f, .5f), 100f,
                0, SpriteMeshType.FullRect, new Vector4(16, 16, 16, 16));
            roundedSprite.name = "RuntimeRoundedRectangle";
            return roundedSprite;
        }
    }

    public static void Round(Image image, Color color)
    {
        if (image == null) return;
        image.sprite = RoundedSprite;
        image.type = Image.Type.Sliced;
        image.color = color;
    }

    public static void Shadow(Graphic graphic, Color color, Vector2 distance)
    {
        if (graphic == null) return;
        var shadow = graphic.GetComponent<Shadow>();
        if (shadow == null) shadow = graphic.gameObject.AddComponent<Shadow>();
        shadow.effectColor = color;
        shadow.effectDistance = distance;
        shadow.useGraphicAlpha = true;
    }

    public static Color PowerColor(string name) => name switch
    {
        "Momentum Start" => Coral,
        "Steady Start" => Sky,
        "Precision Start" => Green,
        _ => Sky
    };

    public static void ApplyBackdrop(RectTransform canvas, HabitatStyle? habitat = null, bool boss = false)
    {
        if (canvas == null) return;
        var existing = canvas.Find("PrismatchBackdrop");
        var go = existing != null ? existing.gameObject : new GameObject("PrismatchBackdrop", typeof(RectTransform), typeof(Image), typeof(UiVerticalGradient));
        if (existing == null) go.transform.SetParent(canvas, false);
        if (go.transform.GetSiblingIndex() != 0) go.transform.SetAsFirstSibling();
        var rect = (RectTransform)go.transform;
        rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
        var image = go.GetComponent<Image>();
        image.color = Color.white; image.raycastTarget = false;
        var gradient = go.GetComponent<UiVerticalGradient>() ?? go.AddComponent<UiVerticalGradient>();
        gradient.top = habitat.HasValue ? HabitatArtwork.Colors(habitat.Value).Top : RaisedNavy;
        gradient.bottom = habitat.HasValue ? HabitatArtwork.Colors(habitat.Value).Bottom : Midnight;
        if (boss)
        {
            gradient.top = Color.Lerp(gradient.top, Midnight, .2f);
            gradient.bottom = Color.Lerp(gradient.bottom, Midnight, .15f);
        }
        image.SetVerticesDirty();
        foreach (Transform child in rect)
            if (child.name == "BackdropShape") child.gameObject.SetActive(!habitat.HasValue);
        var scenery = rect.Find("HabitatScenery");
        if (habitat.HasValue && scenery == null)
        {
            scenery = new GameObject("HabitatScenery", typeof(RectTransform), typeof(HabitatScenery)).transform;
            scenery.SetParent(rect, false);
            var sceneryRect = (RectTransform)scenery;
            sceneryRect.anchorMin = Vector2.zero; sceneryRect.anchorMax = Vector2.one;
            sceneryRect.offsetMin = sceneryRect.offsetMax = Vector2.zero;
        }
        if (scenery != null)
        {
            scenery.gameObject.SetActive(habitat.HasValue);
            if (habitat.HasValue) scenery.GetComponent<HabitatScenery>().SetHabitat(habitat.Value);
        }
    }
    private static Color Hex(string value)
    {
        ColorUtility.TryParseHtmlString("#" + value, out var color);
        return color;
    }
}

public class PowerCardFeedback : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
{
    private RectTransform rect;
    private Vector3 baseScale;

    private void Awake()
    {
        rect = transform as RectTransform;
        baseScale = rect != null ? rect.localScale : Vector3.one;
    }

    private void Scale(float amount, float duration)
    {
        if (rect == null) return;
        rect.DOKill();
        rect.DOScale(baseScale * amount, duration).SetUpdate(true).SetEase(Ease.OutQuad);
    }

    public void OnPointerEnter(PointerEventData eventData) => Scale(1.025f, .12f);
    public void OnPointerExit(PointerEventData eventData) => Scale(1f, .12f);
    public void OnPointerDown(PointerEventData eventData) => Scale(.975f, .07f);
    public void OnPointerUp(PointerEventData eventData) => Scale(1.025f, .09f);
}
