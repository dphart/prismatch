using System;
using System.Linq;
using Newtonsoft.Json.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Unity and the designer render the same normalized icon primitives.</summary>
public static class PowerSymbolLibrary
{
    private static JObject document;
    public static void Load(string json) { document=JObject.Parse(json); }
    public static string Canonical(string key) => key.StartsWith("budget-")?key.Substring(7):key=="reward"?"plus":key=="any"?"match":key;
    private static void EnsureLoaded()
    {
        if(document==null){var asset=Resources.Load<TextAsset>("Content/power-icons");if(asset==null)throw new InvalidOperationException("Shared power icons are missing.");Load(asset.text);}
    }
    public static string ResourceFor(string key)
    {
        EnsureLoaded();return (string)document["symbols"]?[Canonical(key)]?[0]?["image"];
    }
    public static void Draw(RectTransform parent,string key,ItemType? animal=null,string textOverride=null)
    {
        EnsureLoaded();
        key=Canonical(key);
        var marks=document["symbols"]?[key] as JArray??document["symbols"]?["match"] as JArray;
        if(marks==null)throw new InvalidOperationException("Shared power icon is missing: "+key);
        var square=new GameObject("Shared icon: "+key,typeof(RectTransform),typeof(AspectRatioFitter)).GetComponent<RectTransform>();
        square.SetParent(parent,false);square.anchorMin=Vector2.zero;square.anchorMax=Vector2.one;square.offsetMin=square.offsetMax=Vector2.zero;
        var aspect=square.GetComponent<AspectRatioFitter>();aspect.aspectMode=AspectRatioFitter.AspectMode.FitInParent;aspect.aspectRatio=1;
        foreach(JObject mark in marks) {
            float x=(float)mark["x"],y=(float)mark["y"],w=(float)mark["w"],h=(float)mark["h"];
            var rect=new GameObject("Icon mark",typeof(RectTransform)).GetComponent<RectTransform>();rect.SetParent(square,false);
            rect.anchorMin=new Vector2(x,1-y-h);rect.anchorMax=new Vector2(x+w,1-y);rect.offsetMin=rect.offsetMax=Vector2.zero;
            rect.localRotation=Quaternion.Euler(0,0,-((float?)mark["angle"]??0));
            if((bool?)mark["animal"]==true){
                if(!animal.HasValue){UnityEngine.Object.Destroy(rect.gameObject);continue;}
                var item=(ItemDatabase.items??Resources.LoadAll<Item>("Items/")).FirstOrDefault(i=>i!=null&&i.ItemType==animal.Value);
                var portrait=rect.gameObject.AddComponent<Image>();portrait.sprite=item?.sprite;portrait.preserveAspect=true;portrait.raycastTarget=false;
                ItemUtils.StyleIcon(portrait);continue;
            }
            if(mark["image"]!=null){
                var picture=rect.gameObject.AddComponent<Image>();picture.sprite=Resources.Load<Sprite>((string)mark["image"]);
                picture.preserveAspect=true;picture.color=Color.white;picture.raycastTarget=false;continue;
            }
            ColorUtility.TryParseHtmlString("#"+(string)document["palette"][(string)mark["color"]],out var color);
            if(mark["text"]!=null){
                var label=rect.gameObject.AddComponent<TextMeshProUGUI>();label.font=TMP_Settings.defaultFontAsset;label.text=(string)mark["text"]=="{value}"?(textOverride??"+B"):(string)mark["text"];
                label.color=color;label.fontStyle=FontStyles.Bold;label.alignment=TextAlignmentOptions.Center;label.enableAutoSizing=true;label.fontSizeMin=8;label.fontSizeMax=64;label.raycastTarget=false;
            } else {
                var image=rect.gameObject.AddComponent<Image>();image.color=color;image.raycastTarget=false;
                if((float?)mark["rounding"]>0)PrismatchTheme.Round(image,color);
            }
        }
    }
}
