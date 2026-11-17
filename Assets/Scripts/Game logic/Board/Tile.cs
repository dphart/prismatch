using DG.Tweening;
using TMPro;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class Tile : MonoBehaviour, IInitializePotentialDragHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
{

    public int x, y;

    public Item _item;

    public bool isAnimating = false;
    private CanvasGroup extraClearMarker;
    public CanvasGroup ShowExtraClearMarker()
    {
        if(extraClearMarker==null) {
            var root=new GameObject("Extra tile clear",typeof(RectTransform),typeof(CanvasGroup),typeof(LayoutElement)).GetComponent<RectTransform>();
            root.SetParent(transform,false);root.anchorMin=Vector2.zero;root.anchorMax=Vector2.one;
            root.offsetMin=root.offsetMax=Vector2.zero;root.GetComponent<LayoutElement>().ignoreLayout=true;
            extraClearMarker=root.GetComponent<CanvasGroup>();extraClearMarker.blocksRaycasts=false;extraClearMarker.interactable=false;
            var frame=AddTreasureFrame(root,true);
            foreach(var stroke in frame.GetComponentsInChildren<Image>())stroke.color=PrismatchTheme.Orange;
            var label=new GameObject("Extra label",typeof(RectTransform),typeof(TextMeshProUGUI)).GetComponent<TextMeshProUGUI>();
            label.transform.SetParent(root,false);label.rectTransform.anchorMin=new Vector2(0,.72f);label.rectTransform.anchorMax=Vector2.one;
            label.rectTransform.offsetMin=label.rectTransform.offsetMax=Vector2.zero;
            label.font=TMP_Settings.defaultFontAsset;label.text="EXTRA";label.fontSize=20;label.fontStyle=FontStyles.Bold;
            label.enableAutoSizing=true;label.fontSizeMin=10;label.fontSizeMax=20;
            label.alignment=TextAlignmentOptions.Center;label.color=PrismatchTheme.Orange;
            label.outlineColor=PrismatchTheme.Navy;label.outlineWidth=.25f;label.raycastTarget=false;
        }
        extraClearMarker.transform.SetAsLastSibling();extraClearMarker.gameObject.SetActive(true);extraClearMarker.alpha=1;
        return extraClearMarker;
    }
    public Image background;
    public bool IsTreasure { get; private set; }
    private RectTransform treasureFrame;
    public void SetTreasure(bool treasure)
    {
        IsTreasure=treasure;
        if(treasure && treasureFrame==null && icon!=null) treasureFrame=AddTreasureFrame(icon.rectTransform,true);
        ShowTreasureFrame(treasure);
    }
    public void ShowTreasureFrame(bool shown) { if(treasureFrame!=null) treasureFrame.gameObject.SetActive(shown); }
    public static RectTransform AddTreasureFrame(RectTransform parent,bool shown)
    {
        if(!shown) return null;
        var root=new GameObject("Golden treasure tile",typeof(RectTransform)).GetComponent<RectTransform>();
        root.SetParent(parent,false); root.anchorMin=Vector2.zero; root.anchorMax=Vector2.one;
        root.offsetMin=new Vector2(-4,-4); root.offsetMax=new Vector2(4,4);
        for(int side=0;side<4;side++) {
            var image=new GameObject("Gold border",typeof(RectTransform),typeof(Image)).GetComponent<Image>();
            image.transform.SetParent(root,false); image.color=new Color(1f,.8f,.16f); image.raycastTarget=false;
            var rect=image.rectTransform;
            rect.anchorMin=side==0?new Vector2(0,1):side==1?new Vector2(1,0):Vector2.zero;
            rect.anchorMax=side==2?new Vector2(1,0):side==3?new Vector2(0,1):Vector2.one;
            rect.sizeDelta=side%2==0?new Vector2(0,4):new Vector2(4,0); rect.anchoredPosition=Vector2.zero;
        }
        return root;
    }
    public bool IsCharged { get; private set; }
    public bool IsPlanted { get; private set; }
    public bool IsSpotlight { get; private set; }
    public int LevelPointFactor => (IsPlanted ? 2 : 1) * (IsSpotlight ? 3 : 1);
    public void SetLevelEffects(bool planted, bool spotlight)
    {
        IsPlanted = planted; IsSpotlight = spotlight;
        SetSelected(selected);
    }
    private Color? strategyTint;
    private TextMeshProUGUI strategyLabel;
    private RectTransform cageBars, rescueFrame;
    private RectTransform releaseCage, releaseDoor, releaseCharge;
    private TextMeshProUGUI releaseLabel;
    private bool releaseWasOpen;
    private Image cageDragGhost;
    private bool cageDrag;
    private static Sprite cageSprite;
    public static Sprite CageSprite => cageSprite!=null?cageSprite:(cageSprite=Resources.Load<Sprite>("UI/Generated/wild-release-cage-metal"));
    public static RectTransform BuildCageArtwork(Transform parent)
    {
        Image Part(string name,Transform host,Vector2 min,Vector2 max,Color color) {
            var image=new GameObject(name,typeof(RectTransform),typeof(Image)).GetComponent<Image>();image.transform.SetParent(host,false);
            image.rectTransform.anchorMin=min;image.rectTransform.anchorMax=max;image.rectTransform.offsetMin=image.rectTransform.offsetMax=Vector2.zero;
            image.color=color;image.raycastTarget=false;return image;
        }
        Part("Opaque crate interior",parent,new(.27f,.44f),new(.73f,.82f),new Color(.16f,.19f,.25f,1f));
        var art=Part("Cage artwork",parent,new(.14f,.3f),new(.86f,1f),Color.white);
        art.sprite=CageSprite;art.preserveAspect=true;
        var door=new GameObject("Opening door",typeof(RectTransform)).GetComponent<RectTransform>();door.SetParent(parent,false);
        door.anchorMin=new(.27f,.44f);door.anchorMax=new(.73f,.82f);door.offsetMin=door.offsetMax=Vector2.zero;door.pivot=new(0,.5f);
        for(int i=0;i<3;i++) {
            var bar=Part("Metal door bar",door,new(.12f+i*.32f,0),new(.23f+i*.32f,1),new Color(.7f,.76f,.84f));
            PrismatchTheme.Round(bar,new Color(.7f,.76f,.84f));
            Part("Bar highlight",bar.transform,new(.2f,.08f),new(.5f,.92f),Color.white);
        }
        var latch=Part("Metal latch",door,new(.72f,.38f),new(.96f,.62f),new Color(.45f,.52f,.62f));PrismatchTheme.Round(latch,new Color(.45f,.52f,.62f));
        return door;
    }
    public void SetReleaseCage(bool shown,int matches,bool open)
    {
        if(shown&&releaseCage==null) {
            releaseCage=new GameObject("Wild Release cage",typeof(RectTransform),typeof(LayoutElement)).GetComponent<RectTransform>();
            releaseCage.SetParent(transform,false);releaseCage.anchorMin=Vector2.zero;releaseCage.anchorMax=Vector2.one;
            releaseCage.offsetMin=releaseCage.offsetMax=Vector2.zero;releaseCage.GetComponent<LayoutElement>().ignoreLayout=true;
            Image Stroke(string name,Transform parent,Vector2 min,Vector2 max) {
                var image=new GameObject(name,typeof(RectTransform),typeof(Image)).GetComponent<Image>();image.transform.SetParent(parent,false);
                var rect=image.rectTransform;rect.anchorMin=min;rect.anchorMax=max;rect.offsetMin=rect.offsetMax=Vector2.zero;
                image.color=PrismatchTheme.Cream;image.raycastTarget=false;return image;
            }
            releaseDoor=BuildCageArtwork(releaseCage);
            Stroke("Meter track",releaseCage,new(.09f,.27f),new(.91f,.33f)).color=new Color(.05f,.1f,.16f);
            releaseCharge=Stroke("Charge meter",releaseCage,new(.1f,.28f),new(.9f,.32f)).rectTransform;
            releaseCharge.pivot=new(0,.5f);
            releaseLabel=new GameObject("Cage counter",typeof(RectTransform),typeof(TextMeshProUGUI)).GetComponent<TextMeshProUGUI>();
            releaseLabel.transform.SetParent(releaseCage,false);var labelRect=releaseLabel.rectTransform;
            labelRect.anchorMin=new(0,0);labelRect.anchorMax=new(1,.26f);labelRect.offsetMin=labelRect.offsetMax=Vector2.zero;
            releaseLabel.font=TMP_Settings.defaultFontAsset;releaseLabel.fontSize=24;releaseLabel.enableAutoSizing=true;
            releaseLabel.fontSizeMin=16;releaseLabel.fontSizeMax=24;releaseLabel.alignment=TextAlignmentOptions.Center;
            releaseLabel.color=PrismatchTheme.Cream;releaseLabel.raycastTarget=false;
        }
        if(releaseCage==null)return;
        releaseCage.gameObject.SetActive(shown);
        if(!shown){DOTween.Kill(releaseCage);DOTween.Kill(releaseDoor);releaseDoor.localScale=Vector3.one;
            releaseCharge.GetComponent<Image>().color=PrismatchTheme.Cream;releaseWasOpen=false;return;}
        if(icon!=null)icon.enabled=false;
        strategyTint=open?new Color(.12f,.43f,.3f):PrismatchTheme.Navy;SetSelected(selected);
        releaseLabel.text=open?"OPEN":matches+"/10";
        releaseCharge.localScale=new Vector3(Mathf.Clamp01(matches/10f),1,1);
        if(open!=releaseWasOpen) {
            DOTween.Kill(releaseDoor);
            releaseDoor.DOScaleX(open?.12f:1f,.25f).SetUpdate(true).SetId(releaseDoor).SetLink(gameObject).Play();
            DOTween.Kill(releaseCage);
            if(open)releaseCharge.GetComponent<Image>().DOColor(new Color(1f,.8f,.2f),.55f).SetLoops(-1,LoopType.Yoyo)
                .SetUpdate(true).SetId(releaseCage).SetLink(gameObject).Play();
            else releaseCharge.GetComponent<Image>().color=PrismatchTheme.Cream;
        }
        releaseWasOpen=open;
    }
    public Tween CloseCaptureDoor()
    {
        DOTween.Kill(releaseDoor);
        return releaseDoor.DOScaleX(1f,.18f).SetEase(Ease.OutBack);
    }

    private static Sprite lightningRodSprite;
    public static Sprite LightningRodSprite => lightningRodSprite!=null?lightningRodSprite:(lightningRodSprite=Resources.Load<Sprite>("UI/Generated/lightning-rod"));
    private RectTransform lightningRod;
    private TextMeshProUGUI rodMeter;
    private readonly List<Image> rodLights=new();
    public void SetLightningRod(bool shown,int index,int charges)
    {
        if(shown&&lightningRod==null) {
            lightningRod=new GameObject("Lightning rod",typeof(RectTransform)).GetComponent<RectTransform>();
            lightningRod.SetParent(transform,false);lightningRod.anchorMin=Vector2.zero;lightningRod.anchorMax=Vector2.one;lightningRod.offsetMin=lightningRod.offsetMax=Vector2.zero;
            Image Part(string name,Vector2 min,Vector2 max,Color color) {
                var image=new GameObject(name,typeof(RectTransform),typeof(Image)).GetComponent<Image>();image.transform.SetParent(lightningRod,false);
                image.rectTransform.anchorMin=min;image.rectTransform.anchorMax=max;image.rectTransform.offsetMin=image.rectTransform.offsetMax=Vector2.zero;image.color=color;image.raycastTarget=false;return image;
            }
            var artwork=Part("Lightning rod artwork",new(.04f,.3f),new(.96f,.99f),Color.white);
            artwork.sprite=LightningRodSprite;artwork.preserveAspect=true;
            for(int i=0;i<3;i++)rodLights.Add(Part("Charge light",new(.19f+i*.23f,.21f),new(.35f+i*.23f,.29f),PrismatchTheme.Mist));
            rodMeter=new GameObject("Rod charge",typeof(RectTransform),typeof(TextMeshProUGUI)).GetComponent<TextMeshProUGUI>();
            rodMeter.transform.SetParent(lightningRod,false);rodMeter.rectTransform.anchorMin=Vector2.zero;rodMeter.rectTransform.anchorMax=new Vector2(1,.22f);rodMeter.rectTransform.offsetMin=rodMeter.rectTransform.offsetMax=Vector2.zero;
            rodMeter.font=TMP_Settings.defaultFontAsset;rodMeter.fontSize=18;rodMeter.alignment=TextAlignmentOptions.Center;rodMeter.color=PrismatchTheme.Cream;rodMeter.raycastTarget=false;
        }
        if(lightningRod==null)return;lightningRod.gameObject.SetActive(shown);if(!shown)return;
        icon.enabled=false;strategyTint=PrismatchTheme.Navy;SetSelected(selected);
        rodMeter.text=(index+1)+": "+charges+"/3";
        for(int i=0;i<rodLights.Count;i++)rodLights[i].color=i<charges?PrismatchTheme.Orange:PrismatchTheme.Mist;
    }

    public void SetGoldenRescue(bool golden)
    {
        if(golden&&rescueFrame==null&&icon!=null) {
            rescueFrame=AddTreasureFrame(icon.rectTransform,true);rescueFrame.name="Golden rescued tile";
        }
        if(rescueFrame!=null)rescueFrame.gameObject.SetActive(golden);
    }
    public void SetStrategyVisual(Color? tint,string label,bool vault,bool caged)
    {
        strategyTint=tint;
        if(icon!=null)icon.enabled=!vault;
        if(!string.IsNullOrEmpty(label)&&strategyLabel==null) {
            var rect=new GameObject("Level marker",typeof(RectTransform),typeof(TextMeshProUGUI)).GetComponent<RectTransform>();
            rect.SetParent(transform,false);rect.anchorMin=Vector2.zero;rect.anchorMax=Vector2.one;rect.offsetMin=rect.offsetMax=Vector2.zero;
            strategyLabel=rect.GetComponent<TextMeshProUGUI>();strategyLabel.font=TMP_Settings.defaultFontAsset;
            strategyLabel.fontSize=13;strategyLabel.enableAutoSizing=true;strategyLabel.fontSizeMin=8;strategyLabel.fontSizeMax=13;
            strategyLabel.alignment=TextAlignmentOptions.Center;strategyLabel.color=PrismatchTheme.Cream;strategyLabel.raycastTarget=false;
        }
        if(strategyLabel!=null){strategyLabel.text=label;strategyLabel.gameObject.SetActive(!string.IsNullOrEmpty(label));}
        if(caged&&cageBars==null) {
            cageBars=new GameObject("Cage",typeof(RectTransform)).GetComponent<RectTransform>();cageBars.SetParent(transform,false);
            cageBars.anchorMin=Vector2.zero;cageBars.anchorMax=Vector2.one;cageBars.offsetMin=cageBars.offsetMax=Vector2.zero;
            for(int i=0;i<3;i++) {
                var bar=new GameObject("Bar",typeof(RectTransform),typeof(Image)).GetComponent<RectTransform>();bar.SetParent(cageBars,false);
                bar.anchorMin=new Vector2(.22f+i*.28f,.08f);bar.anchorMax=new Vector2(.22f+i*.28f,.92f);bar.sizeDelta=new Vector2(4,0);
                var image=bar.GetComponent<Image>();image.color=new Color(.08f,.14f,.2f,.9f);image.raycastTarget=false;
            }
        }
        if(cageBars!=null)cageBars.gameObject.SetActive(caged);
        SetSelected(selected);
    }
    private Image blackoutCover;
    public void SetBlackout(bool hidden)
    {
        if(hidden&&blackoutCover==null) {
            blackoutCover=new GameObject("Hidden animal",typeof(RectTransform),typeof(Image),typeof(LayoutElement)).GetComponent<Image>();
            blackoutCover.transform.SetParent(transform,false);
            var rect=blackoutCover.rectTransform;rect.anchorMin=Vector2.zero;rect.anchorMax=Vector2.one;
            rect.offsetMin=rect.offsetMax=Vector2.zero;blackoutCover.color=Color.black;blackoutCover.raycastTarget=false;
            blackoutCover.GetComponent<LayoutElement>().ignoreLayout=true;
        }
        if(blackoutCover!=null){blackoutCover.gameObject.SetActive(hidden);blackoutCover.transform.SetAsLastSibling();}
    }
    private bool selected;
    public static Color ChargedColor => new Color(.76f,.9f,1f,1f);
    public void SetCharged(bool charged)
    {
        IsCharged = charged;
        SetSelected(selected);
    }
    public Item item
    {
        get => _item;
        set
        {
            _item = value;

            // ✅ Check before touching icon
            if (icon == null) return;

            if (_item != null && _item.sprite != null)
            {
                icon.sprite = _item.sprite;
                ItemUtils.StyleIcon(icon);
            }
        }
    }


    public Image icon;

    public Button button;


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {

        button.onClick.AddListener(() =>
        {
            Board.Instance.Select(this);
        });
    
    }

    public Tile Left => x > 0 ? Board.Instance.Tiles[x - 1, y] : null;
    public Tile Right => x < Board.Instance.Width - 1 ? Board.Instance.Tiles[x + 1, y] : null;
    public Tile Top => y > 0 ? Board.Instance.Tiles[x, y - 1] : null;
    public Tile Bottom => y < Board.Instance.Height - 1 ? Board.Instance.Tiles[x, y + 1] : null;

    public Tile[] Neighbours => new Tile[] { Left, Right, Top, Bottom };

    public List<Tile> GetConnectedTiles(List<Tile> exclude = null)
    {
        return Board.Instance != null ? Board.Instance.GetConnectedTiles(this) : new List<Tile>();
    }

    public void SetSelected(bool isSelected)
    {
        selected = isSelected;
        if (background == null) return;

        if (isSelected)
        {
            // ✅ Highlight visually
           background.color = Color.darkGray;
            
        }
        else
        {
            // ✅ Reset to normal
            background.color = strategyTint ?? (IsPlanted ? new Color(.78f,.93f,.8f,1) : IsCharged ? ChargedColor : Color.white);
        }
    }

    public void OnInitializePotentialDrag(PointerEventData eventData)
    {
        // Disable the gesture itself so finger drift does not cancel the button's tap.
        if (!GameController.SwipeToMoveEnabled&&Board.Instance?.CanDragIntoCage(this)!=true) eventData.pointerDrag = null;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        cageDrag=Board.Instance?.CanDragIntoCage(this)==true;
        if(cageDrag) {
            eventData.eligibleForClick=false;
            var canvas=GetComponentInParent<Canvas>()?.rootCanvas;
            if(canvas!=null&&icon!=null) {
                cageDragGhost=new GameObject("Dragged cage animal",typeof(RectTransform),typeof(Image)).GetComponent<Image>();
                cageDragGhost.transform.SetParent(canvas.transform,false);cageDragGhost.sprite=icon.sprite;
                cageDragGhost.preserveAspect=true;cageDragGhost.raycastTarget=false;
                cageDragGhost.rectTransform.sizeDelta=new Vector2(64,64);ItemUtils.StyleIcon(cageDragGhost);
                MoveCageDragGhost(eventData);
            }
            return;
        }
        if (!GameController.SwipeToMoveEnabled) return;
        if (Board.Instance == null || Board.Instance.isSwapping) return;
        Board.Instance.Select(this);
    }

    public void OnDrag(PointerEventData eventData)
    {
        MoveCageDragGhost(eventData);
        // Unity requires a drag handler in order to send begin/end drag events.
    }
    private void MoveCageDragGhost(PointerEventData eventData)
    {
        if(cageDragGhost==null)return;
        var parent=cageDragGhost.transform.parent as RectTransform;
        if(RectTransformUtility.ScreenPointToWorldPointInRectangle(parent,eventData.position,eventData.pressEventCamera,out var position))
            cageDragGhost.transform.position=position;
    }
    private void ClearCageDragGhost()
    {
        if(cageDragGhost!=null){Destroy(cageDragGhost.gameObject);cageDragGhost=null;}
    }
    private void OnDisable(){ClearCageDragGhost();cageDrag=false;releaseWasOpen=false;if(releaseCage!=null)DOTween.Kill(releaseCage);}

    public void OnEndDrag(PointerEventData eventData)
    {
        bool feed=cageDrag;cageDrag=false;ClearCageDragGhost();
        if (!feed&&!GameController.SwipeToMoveEnabled) return;
        if (Board.Instance == null || Board.Instance.isSwapping) return;
        var destination = eventData.pointerCurrentRaycast.gameObject != null
            ? eventData.pointerCurrentRaycast.gameObject.GetComponentInParent<Tile>()
            : null;

        if (destination == null)
        {
            var hits = new List<RaycastResult>();
            EventSystem.current?.RaycastAll(eventData, hits);
            foreach (var hit in hits)
            {
                destination = hit.gameObject.GetComponentInParent<Tile>();
                if (destination != null) break;
            }
        }

        if(feed){if(destination!=null)Board.Instance.FinishCageDrag(this,destination);return;}
        if (destination != null && destination != this)
            Board.Instance.Select(destination);
    }




}
