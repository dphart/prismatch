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

    public static void ApplyBackdrop(RectTransform canvas)
    {
        if (canvas == null || canvas.Find("PrismatchBackdrop") != null) return;
        var go = new GameObject("PrismatchBackdrop", typeof(RectTransform), typeof(Image), typeof(UiVerticalGradient));
        go.transform.SetParent(canvas, false);
        go.transform.SetAsFirstSibling();
        var rect = (RectTransform)go.transform;
        rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
        go.GetComponent<Image>().color = Color.white;
        var gradient = go.GetComponent<UiVerticalGradient>();
        gradient.top = RaisedNavy; gradient.bottom = Midnight;
        foreach (var data in new[]
        {
            (new Vector2(-.08f,.88f), new Vector2(210,110), 42f),
            (new Vector2(1.06f,.72f), new Vector2(190,100), -38f),
            (new Vector2(-.05f,.12f), new Vector2(180,95), -35f),
            (new Vector2(1.05f,.08f), new Vector2(220,110), 38f)
        })
        {
            var shape = new GameObject("BackdropShape", typeof(RectTransform), typeof(Image));
            shape.transform.SetParent(rect, false);
            var shapeRect = (RectTransform)shape.transform;
            shapeRect.anchorMin = shapeRect.anchorMax = data.Item1;
            shapeRect.sizeDelta = data.Item2;
            shapeRect.localEulerAngles = new Vector3(0, 0, data.Item3);
            Round(shape.GetComponent<Image>(), new Color(Sky.r, Sky.g, Sky.b, .07f));
            shape.GetComponent<Image>().raycastTarget = false;
        }
    }

    private static Color Hex(string value)
    {
        ColorUtility.TryParseHtmlString("#" + value, out var color);
        return color;
    }
}

public class UiVerticalGradient : BaseMeshEffect
{
    public Color top = Color.white;
    public Color bottom = Color.black;

    public override void ModifyMesh(VertexHelper helper)
    {
        if (!IsActive()) return;
        var vertex = new UIVertex();
        var rect = graphic.rectTransform.rect;
        for (int i = 0; i < helper.currentVertCount; i++)
        {
            helper.PopulateUIVertex(ref vertex, i);
            float t = Mathf.InverseLerp(rect.yMin, rect.yMax, vertex.position.y);
            Color baseColor = vertex.color;
            vertex.color = baseColor * Color.Lerp(bottom, top, t);
            helper.SetUIVertex(vertex, i);
        }
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
