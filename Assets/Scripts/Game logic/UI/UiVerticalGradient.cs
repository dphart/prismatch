using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

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

/// <summary>Shared geometry feeds the Unity background and the exported visual contact sheet.</summary>
public static class HabitatArtwork
{
    public readonly struct Palette
    {
        public readonly Color Top, Bottom, Accent, Detail;
        public Palette(Color top, Color bottom, Color accent, Color detail) { Top = top; Bottom = bottom; Accent = accent; Detail = detail; }
    }
    public readonly struct Shape
    {
        public readonly Vector2[] Points;
        public readonly Color Color;
        public Shape(Color color, params Vector2[] points) { Color = color; Points = points; }
    }
    private static Color RGB(int r, int g, int b) => new Color(r / 255f, g / 255f, b / 255f);
    public static Palette Colors(HabitatStyle habitat) => habitat switch {
        HabitatStyle.Shore => new Palette(RGB(22,83,105), RGB(10,40,56), RGB(103,218,207), RGB(241,205,143)),
        HabitatStyle.Garden => new Palette(RGB(31,76,62), RGB(12,33,35), RGB(161,205,111), RGB(236,190,149)),
        HabitatStyle.Rapids => new Palette(RGB(33,59,106), RGB(9,24,50), RGB(126,199,249), RGB(185,226,246)),
        HabitatStyle.Giants => new Palette(RGB(82,55,83), RGB(31,26,44), RGB(231,166,113), RGB(171,155,204)),
        _ => new Palette(RGB(74,68,103), RGB(29,27,51), RGB(209,157,222), RGB(242,200,122)) };
    private static Color Alpha(Color color, float alpha) => new Color(color.r, color.g, color.b, alpha);
    public static List<Shape> Shapes(HabitatStyle habitat)
    {
        var shapes = new List<Shape>();
        var palette = Colors(habitat);
        void Ellipse(float x, float y, float rx, float ry, float angle, Color color)
        {
            var points = new Vector2[24];
            double turn = angle * System.Math.PI / 180;
            for (int i = 0; i < points.Length; i++)
            {
                double a = i * System.Math.PI * 2 / points.Length;
                float dx = rx * (float)System.Math.Cos(a), dy = ry * (float)System.Math.Sin(a);
                points[i] = new Vector2(x + dx * (float)System.Math.Cos(turn) - dy * (float)System.Math.Sin(turn),
                    y + dx * (float)System.Math.Sin(turn) + dy * (float)System.Math.Cos(turn));
            }
            shapes.Add(new Shape(color, points));
        }
        void Ribbon(float y, float amplitude, float thickness, float phase, Color color)
        {
            for (int i = 0; i < 32; i++)
            {
                float x = i / 32f, next = (i + 1) / 32f;
                float a = y + amplitude * (float)System.Math.Sin(x * System.Math.PI * 3 + phase);
                float b = y + amplitude * (float)System.Math.Sin(next * System.Math.PI * 3 + phase);
                shapes.Add(new Shape(color, new Vector2(x,a), new Vector2(next,b), new Vector2(next,b + thickness), new Vector2(x,a + thickness)));
            }
        }
        switch (habitat)
        {
            case HabitatStyle.Shore:
                for (int i = 0; i < 4; i++) Ribbon(.025f + i * .035f, .013f, .012f, i * .6f, Alpha(palette.Accent, .15f));
                Ribbon(.87f, .025f, .008f, .5f, Alpha(palette.Detail, .2f));
                Ellipse(.92f,.91f,.13f,.06f,0,Alpha(palette.Detail,.12f));
                for (int i = 0; i < 5; i++) Ellipse(.03f + i * .23f,.18f,.006f,.003f,0,Alpha(palette.Detail,.25f));
                break;
            case HabitatStyle.Garden:
                for (int side = 0; side < 2; side++)
                for (int i = 0; i < 8; i++)
                {
                    float x = side == 0 ? .025f : .975f;
                    Ellipse(x,.09f + i * .12f,.07f,.018f,side == 0 ? 35 : -35,Alpha(palette.Accent,.17f));
                    Ellipse(x,.12f + i * .12f,.05f,.014f,side == 0 ? -25 : 25,Alpha(palette.Accent,.09f));
                }
                for (int i = 0; i < 7; i++) Ellipse(.12f + i * .13f,.055f + i % 2 * .025f,.012f,.006f,0,Alpha(palette.Detail,.24f));
                break;
            case HabitatStyle.Rapids:
                for (int side = 0; side < 2; side++)
                for (int i = 0; i < 12; i++)
                {
                    float x = side == 0 ? .035f : .965f, y = i * .09f;
                    shapes.Add(new Shape(Alpha(palette.Accent,.16f),new Vector2(x-.018f,y),new Vector2(x+.022f,y+.035f),
                        new Vector2(x+.008f,y+.075f),new Vector2(x-.03f,y+.04f)));
                    Ellipse(x + (side == 0 ? .05f : -.05f),y + .018f,.007f,.009f,0,Alpha(palette.Detail,.15f));
                }
                for (int i = 0; i < 3; i++) Ribbon(.045f + i * .035f,.018f,.008f,i,Alpha(palette.Accent,.12f));
                break;
            case HabitatStyle.Giants:
                for (int layer = 0; layer < 2; layer++)
                for (int i = 0; i < 5; i++)
                {
                    float x = i * .29f - .13f + layer * .1f;
                    float peak = .13f + (i % 2) * .055f + layer * .025f;
                    shapes.Add(new Shape(Alpha(layer == 0 ? palette.Detail : palette.Accent,.16f),
                        new Vector2(x,-.02f),new Vector2(x+.2f,peak),new Vector2(x+.4f,-.02f)));
                }
                Ellipse(.86f,.9f,.105f,.05f,0,Alpha(palette.Accent,.18f));
                Ribbon(.85f,.012f,.004f,0,Alpha(palette.Detail,.15f));
                break;
            case HabitatStyle.Menagerie:
                for (int i = 0; i < 7; i++)
                {
                    Ellipse(i * .19f-.04f,1.015f,.14f,.11f,-15,Alpha(palette.Accent,.13f));
                    Ellipse(i * .19f,.01f,.13f,.085f,20,Alpha(palette.Detail,.08f));
                }
                for (int side = 0; side < 2; side++)
                for (int i = 0; i < 7; i++)
                {
                    float x = side == 0 ? .035f : .965f, y = .14f + i * .105f;
                    Ellipse(x,y,.02f,.011f,i * 25,Alpha(palette.Detail,.18f));
                    Ellipse(x + (side == 0 ? .035f : -.035f),y+.02f,.008f,.004f,0,Alpha(palette.Accent,.23f));
                }
                break;
        }
        return shapes;
    }
}

/// <summary>Static, non-interactive scenery. Geometry stays near the perimeter to preserve board readability.</summary>
public sealed class HabitatScenery : MaskableGraphic
{
    private HabitatStyle habitat;
    private List<HabitatArtwork.Shape> shapes;
    public void SetHabitat(HabitatStyle value)
    {
        if (shapes == null || habitat != value) { habitat = value; shapes = HabitatArtwork.Shapes(value); SetVerticesDirty(); }
        raycastTarget = false;
    }
    protected override void OnPopulateMesh(VertexHelper helper)
    {
        helper.Clear();
        if (shapes == null) return;
        var rect = rectTransform.rect;
        foreach (var shape in shapes)
        {
            int start = helper.currentVertCount;
            foreach (var point in shape.Points)
                helper.AddVert(new Vector3(rect.xMin + point.x * rect.width, rect.yMin + point.y * rect.height), shape.Color, Vector2.zero);
            for (int i = 1; i < shape.Points.Length - 1; i++) helper.AddTriangle(start,start+i,start+i+1);
        }
    }
}

