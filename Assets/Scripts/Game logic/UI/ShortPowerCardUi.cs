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

    private bool activeView;
    private PowerUp currentPower;
    private Action<PowerUp> clickAction;
    private RectTransform layoutRoot;
    public string PowerName => currentPower?.Name;

    public void ApplyResponsiveLayout(float width, float height)
    {
        if (transform is not RectTransform cardRect) return;
        cardRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, width);
        cardRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, height);

        if (layoutRoot == null && nameText != null) layoutRoot = nameText.transform.parent as RectTransform;
        var container = layoutRoot;
        if (container == null) return;
        container.anchorMin = Vector2.zero;
        container.anchorMax = Vector2.one;
        container.offsetMin = Vector2.zero;
        container.offsetMax = Vector2.zero;
        var fitter = container.GetComponent<ContentSizeFitter>();
        if (fitter != null) fitter.enabled = false;

        var oldHorizontal=container.GetComponent<HorizontalLayoutGroup>();
        if(oldHorizontal!=null)oldHorizontal.enabled=false;
        var stack=container.Find("CardStack") as RectTransform;
        if(stack==null){stack=new GameObject("CardStack",typeof(RectTransform),typeof(VerticalLayoutGroup)).GetComponent<RectTransform>();stack.SetParent(container,false);}
        stack.anchorMin=Vector2.zero;stack.anchorMax=Vector2.one;stack.offsetMin=stack.offsetMax=Vector2.zero;
        var rootLayout=stack.GetComponent<VerticalLayoutGroup>();
        rootLayout.padding=new RectOffset(20,20,14,14);rootLayout.spacing=6;
        rootLayout.childAlignment=TextAnchor.UpperCenter;
        rootLayout.childControlWidth=rootLayout.childControlHeight=true;
        rootLayout.childForceExpandWidth=true;rootLayout.childForceExpandHeight=false;

        var header=stack.Find("Card heading") as RectTransform;
        if(header==null){header=new GameObject("Card heading",typeof(RectTransform),typeof(HorizontalLayoutGroup),typeof(LayoutElement)).GetComponent<RectTransform>();header.SetParent(stack,false);}
        header.SetAsFirstSibling();
        var headerLayout=header.GetComponent<HorizontalLayoutGroup>();
        headerLayout.spacing=20;headerLayout.childAlignment=TextAnchor.UpperLeft;
        headerLayout.childControlWidth=headerLayout.childControlHeight=true;
        headerLayout.childForceExpandWidth=headerLayout.childForceExpandHeight=false;
        float iconSize=Mathf.Clamp(width*.16f,64,100);
        if(icon!=null){
            icon.transform.SetParent(header,false);icon.transform.SetAsFirstSibling();
            var iconLayout=icon.GetComponent<LayoutElement>()??icon.gameObject.AddComponent<LayoutElement>();
            iconLayout.minWidth=iconLayout.preferredWidth=iconSize;
            iconLayout.minHeight=iconLayout.preferredHeight=iconSize;
            iconLayout.flexibleWidth=iconLayout.flexibleHeight=0;
        }
        var column=(stack.Find("TextColumn")??container.Find("TextColumn"))?.gameObject;
        if(column==null)column=new GameObject("TextColumn",typeof(RectTransform),typeof(VerticalLayoutGroup),typeof(LayoutElement));
        column.transform.SetParent(stack,false);
        column.transform.SetAsLastSibling();
        var columnLayout=column.GetComponent<LayoutElement>();columnLayout.minWidth=0;columnLayout.flexibleWidth=1;
        var vertical=column.GetComponent<VerticalLayoutGroup>();vertical.spacing=6;
        vertical.childAlignment=TextAnchor.UpperCenter;
        vertical.childControlWidth=vertical.childControlHeight=vertical.childForceExpandWidth=true;
        vertical.childForceExpandHeight=false;

        const float flowHeight=150;
        float textWidth=Mathf.Max(80,width-rootLayout.padding.horizontal);
        float titleWidth=Mathf.Max(80,textWidth-(icon!=null?iconSize+headerLayout.spacing:0));
        float nameSize=42,descriptionSize=32;
        float nameHeight=ConfigureText(nameText,header,nameSize,titleWidth,true);
        float descriptionHeight=ConfigureText(descriptionText,column.transform,descriptionSize,textWidth,true);
        while(!activeView && Mathf.Max(iconSize,nameHeight)+descriptionHeight+flowHeight+rootLayout.spacing+vertical.spacing+rootLayout.padding.vertical>height && descriptionSize>26){
            nameSize--;descriptionSize--;
            nameHeight=ConfigureText(nameText,header,nameSize,titleWidth,true);
            descriptionHeight=ConfigureText(descriptionText,column.transform,descriptionSize,textWidth,true);
        }
        nameText.alignment=TextAlignmentOptions.Left;
        descriptionText.alignment=TextAlignmentOptions.Center;
        var headerSize=header.GetComponent<LayoutElement>();
        headerSize.minHeight=headerSize.preferredHeight=Mathf.Max(iconSize,nameHeight);
        headerSize.minWidth=0;headerSize.flexibleWidth=1;
        ConfigureFlow(container,column.transform);
        columnLayout.minHeight=columnLayout.preferredHeight=descriptionHeight+flowHeight+vertical.spacing;
        columnLayout.flexibleHeight=0;
        float requiredHeight=headerSize.preferredHeight+columnLayout.preferredHeight+rootLayout.spacing+rootLayout.padding.vertical;
        cardRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical,requiredHeight);
        var cardLayout = GetComponent<LayoutElement>();
        if(cardLayout==null)cardLayout=gameObject.AddComponent<LayoutElement>();
        cardLayout.minHeight = cardLayout.preferredHeight = requiredHeight;
        LayoutRebuilder.ForceRebuildLayoutImmediate(stack);
    }

    private void ConfigureFlow(RectTransform container, Transform column)
    {
        foreach(var legacy in new[]{"Choice divider","Choice effect"})
            if(container.Find(legacy) is Transform old)old.gameObject.SetActive(false);
        var flow=column.Find("Condition to reward") as RectTransform;
        if(flow==null) {
            flow=new GameObject("Condition to reward",typeof(RectTransform),typeof(LayoutElement)).GetComponent<RectTransform>();
            flow.SetParent(column,false);
        }
        var layout=flow.GetComponent<LayoutElement>();
        layout.minWidth=0;layout.flexibleWidth=1;layout.minHeight=layout.preferredHeight=150;
        flow.SetAsLastSibling();PowerCardFlow.Build(flow,currentPower,nameText.font);
    }
    private static float ConfigureText(TextMeshProUGUI text, Transform parent, float fontSize, float width, bool wraps)
    {
        if (text == null) return 0f;
        text.transform.SetParent(parent, false);
        text.margin = Vector4.zero;
        text.rectTransform.localScale = Vector3.one;
        var fitter = text.GetComponent<ContentSizeFitter>();
        if (fitter != null) fitter.enabled = false;
        text.fontSize = fontSize;
        text.enableAutoSizing = false;
        text.textWrappingMode = wraps ? TextWrappingModes.Normal : TextWrappingModes.NoWrap;
        text.overflowMode = TextOverflowModes.Ellipsis;
        text.alignment = wraps ? TextAlignmentOptions.TopLeft : TextAlignmentOptions.Left;
        float height = Mathf.Ceil(text.GetPreferredValues(text.text, width, Mathf.Infinity).y) + 2f;
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
        activeView = useDynamicDescription;
        currentPower = power;
        clickAction = onClick;

        // Populate UI
        string scope=PowerUpIconLibrary.GrowthScope(power);
        Color scopeColor=PowerUpIconLibrary.ScopeColor(power);
        nameText.text = power.Name+(scope.Length==0?"":$"\n<size=60%><color=#{ColorUtility.ToHtmlStringRGB(scopeColor)}>{scope}</color></size>");
        descriptionText.text = useDynamicDescription ? power.GetCurrentPowerDescription() : power.PowerUpDescription;
        PowerUpIconLibrary.Apply(icon, power, !activeView);
        ApplyPowerStyle(power.Name);

        // Hook up click event
        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(() => clickAction?.Invoke(currentPower));
    }

    private void ApplyPowerStyle(string powerName)
    {
        Color accent = PrismatchTheme.TierColor(currentPower.Rarity);
        var background = GetComponent<Image>();
        PrismatchTheme.Round(background, PrismatchTheme.Card);
        PrismatchTheme.Shadow(background, new Color(0f, .05f, .15f, .42f), new Vector2(0f, -5f));
        var outline = GetComponent<Outline>();
        if (outline == null) outline = gameObject.AddComponent<Outline>();
        outline.effectColor = accent;
        outline.effectDistance = new Vector2(3f, -3f);
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
        arrow.gameObject.SetActive(false);
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

        PowerUpIconLibrary.Apply(icon, bossPower);
    }
}

public readonly struct PowerIconSpec
{
    public readonly ItemType? Animal;
    public readonly string Subject;
    public readonly string Effect;
    public readonly bool Growth;
    public PowerIconSpec(string subject, string effect, bool growth = false, ItemType? animal = null)
    { Subject = subject; Effect = effect; Growth = growth; Animal = animal; }
}

/// <summary>Shared visual vocabulary for selection, active powers, and boss cards.</summary>
public static class PowerUpIconLibrary
{
    public static string GrowthScope(PowerUp power) => power switch {
        AnimalMasteryPower animal => animal.ItemType.ToString().ToUpperInvariant()+" ONLY",
        IconTypeStreakGivesBasePointsOrMulti animal => animal.UsesBlockReward
            ? animal.ItemType.ToString().ToUpperInvariant()+" ONLY" : "ALL MATCHES",
        _ => ""
    };
    public static Color ScopeColor(PowerUp power) => power switch {
        AnimalMasteryPower => PrismatchTheme.Green,
        IconTypeStreakGivesBasePointsOrMulti animal => animal.UsesBlockReward?PrismatchTheme.Green:PrismatchTheme.Sky,
        _ => PrismatchTheme.PowerColor(power?.Name)
    };
    public static string GrowthExplanation(PowerUp power) => power switch {
        AnimalMasteryPower animal => $"Only {animal.ItemType} matches receive this growing bonus.\n",
        IconTypeStreakGivesBasePointsOrMulti animal => animal.UsesBlockReward
            ? $"Match {animal.ItemType} to grow bonuses for {animal.ItemType} matches only.\n"
            : $"Match {animal.ItemType} to grow bonuses for every animal's matches.\n",
        _ => ""
    };
    private static void DrawGrowthScope(RectTransform root,PowerUp power)
    {
        string scope=GrowthScope(power);if(scope.Length==0)return;
        var band=Rect("Bonus scope",root,new Vector2(0,.80f),Vector2.one);
        var background=band.gameObject.AddComponent<Image>();PrismatchTheme.Round(background,ScopeColor(power));background.raycastTarget=false;
        Label(Rect("Scope label",band,Vector2.zero,Vector2.one),scope,PrismatchTheme.Navy);
    }
    public static PowerIconSpec Describe(PowerUp power) => power switch
    {
        PowerUp shared when shared.UsesBlockReward => new("",shared.AffectsStat(false)?"+C":"CLR",
            (shared.Tags & PowerTags.Growth)!=0,shared is IconBonusPower animal?animal.ItemType:(ItemType?)null),
        LevelCharacteristicPower characteristic => new(characteristic.Lightning ? "BOLT" : characteristic.Bonus.ToString().ToUpperInvariant(),
            characteristic.Bonus == LevelBonus.WildRelease ? "10" : characteristic.Bonus == LevelBonus.GoldenRescue ? "x3" :
            characteristic.Lightning || characteristic.Bonus is LevelBonus.GardenPlots or LevelBonus.LeastMatched or LevelBonus.MigrationSeason or LevelBonus.CrackTheVault or LevelBonus.AnimalRescue ? "x2" :
            characteristic.Bonus is LevelBonus.AlternatingCurrent or LevelBonus.ChangingTides or LevelBonus.SteppingStones ? "x3" :
            characteristic.Bonus is LevelBonus.TreasureTrail or LevelBonus.Crosswinds ? "x5" : "+"),
        SharedRulePower rule when rule.AnimalCondition.HasValue => new("", "+", animal: rule.AnimalCondition),
        ChallengeBoss boss => BossPortrait(boss.Style),
        BiomeOpeningPower garden => new(garden.Square ? "SQUARE" : "GARDEN", "+6B"),
        SpotlightOpeningPower => new("ANIMAL", "+1M", animal: ItemType.Duck),
        BlockMatchFoundationPower foundation => new(foundation.Minimum + "+", foundation.CreditPerTile + "C"),
        MatchFoundationPower foundation => new(foundation.Minimum + "+",
            foundation.Style == MatchFoundationPower.FoundationStyle.StreakFive ? "+1M" :
            foundation.Style == MatchFoundationPower.FoundationStyle.SpeedFive ? "+2B" :
            foundation.Style == MatchFoundationPower.FoundationStyle.GiantSix ? "+1B" :
            "+" + foundation.BasePerTile),
        AnimalDoublePower animal => new("", "BASE x2", animal: animal.ItemType),
        AnimalSpawnPower magnet => new("", "MORE", animal: magnet.Animal),
        HabitatPower habitat => new(habitat.Style.ToString().ToUpperInvariant(), habitat.Tier == 0 ? "+B" : habitat.Tier == 1 ? "+3M" : "+0.5", habitat.Tier == 2),
        CollectionPower collection => new(collection.Style == CollectionStyle.ShapeCredit ? collection.Shape.ToString().ToUpperInvariant() : collection.Bloom switch {
            BloomCondition.Edge => "EDGE", BloomCondition.Square => "2x2", BloomCondition.Large => "6+", _ => "" },
            collection.Style == CollectionStyle.Transform ? "->" : collection.Style == CollectionStyle.Admission ? "JOIN" : "+1",
            animal: collection.Style == CollectionStyle.ShapeCredit ? (ItemType?)null : collection.Animal),
        RosterTradeoffPower roster => new("", roster.AddsAnimal ? "+30%" : "-25%", animal: roster.Animal),
        PatternAnimalPower pattern => new("RARE", pattern.HasCurse ? "+/-" : "+"),
        AnimalPairPower _ => new("PAIR", "1/2"),
        BlockClearPower block => new(block.Style switch
        {
            BlockClearStyle.Neighbor => "CLEAR",
            BlockClearStyle.LargeMatch => "4+",
            BlockClearStyle.Cascade => "CHAIN",
            BlockClearStyle.Edge => "EDGE",
            BlockClearStyle.Square => "2x2",
            BlockClearStyle.LongLine => "6+",
            _ => ""
        }, "CLR", animal: block.Style == BlockClearStyle.Animal ? block.Animal : (ItemType?)null),
        IconTypeGivesMulti animal => new("", "\u00D7", animal: animal.ItemType),
        IconTypeGivesBase animal => new("", "+", animal: animal.ItemType),
        AnimalMasteryPower animal => new(animal.Rival.HasValue ? "RIVAL" : "", animal.GrantsBasePoints ? "+" : "\u00D7", true, animal.ItemType),
        IconTypeStreakGivesBasePointsOrMulti animal => new("",
            animal.GrantsBasePoints ? (animal.GrantsMultiplier ? "+\u00D7" : "+") : "\u00D7",
            true, animal.ItemType),
        TacticalStarter tactical => new(tactical.IconLabel, "+\u00D7", tactical.IsGrowth),
        BudgetPower budget => new(budget.IconLabel, budget.RewardsMoves ? "+1" : "+3s"),
        ShufflePower shuffle => new("MIX", shuffle.Style == ShuffleStyle.Discount ? "-50%" : "FREE"),
        StarterPower starter => new(starter.Strategy == StarterStrategy.Precision ? "3" : "ALL", "+\u00D7",
            starter.Strategy == StarterStrategy.Momentum),
        BasePowerUp _ => new("ALL", "+"),
        BaseMultiPowerUp _ => new("ALL", "+\u00D7"),
        FlexibleMatchPower _ => new("ALL", "+"),
        BasePerTile _ => new("ALL", "+", true),
        EdgeBalancePower _ => new("EDGE", "+/-", true),
        FourExactlyPower _ => new("4", "+\u00D7"),
        ComboCrazePower _ => new("RUN", "\u00D7", true),
        HighStakesPower _ => new("5+", "+"),
        UnderdogPower _ => new("<=3", "\u00D7"),
        StackedOddsPower _ => new("4", "+"),
        SnowballPower _ => new("ALL", "\u00D7", true),
        ConsistencyIsKey _ => new("RUN", "\u00D7"),
        TwinShotPower _ => new("4", "\u00D7"),
        ColorFocusPower _ => new("SAME", "\u00D7"),
        JackpotFivePower _ => new("5+", "\u00D7"),
        FourFuryPower _ => new("4", "\u00D7"),
        PrecisionStrikePower _ => new("3", "\u00D7"),
        NoVerticalDoublePower _ => new("HORIZ", "x4"),
        VerticalMatchPower _ => new("VERT", "x4"),
        BossPowers _ => new("BOSS", "!"),
        _ => new("POWER", "?")
    };

    public static void Apply(Image host, PowerUp power, bool choicePortrait = false)
    {
        if (host == null) return;
        host.gameObject.SetActive(true);
        host.enabled = true;
        host.raycastTarget = false;
        host.preserveAspect = false;
        PrismatchTheme.Round(host, PrismatchTheme.Navy);
        // Rebinding a card must never retain the previous power's animal or badge.
        var previous = host.transform.Find("PowerIconLayers");
        if (previous != null)
        {
            previous.gameObject.SetActive(false);
            previous.SetParent(null, false);
            if (Application.isPlaying) UnityEngine.Object.Destroy(previous.gameObject);
            else UnityEngine.Object.DestroyImmediate(previous.gameObject);
        }
        // The three starters already have dedicated artwork; retain it intact.
        string starterResource = power switch
        {
            BasePowerUp _ => "UI/Generated/power-steady",
            StarterPower starter when starter.Strategy == StarterStrategy.Precision => "UI/Generated/power-precision",
            StarterPower _ => "UI/Generated/power-momentum",
            _ => null
        };
        var starterArtwork = starterResource == null ? null : Resources.Load<Sprite>(starterResource);
        if (starterArtwork != null)
        {
            host.sprite = starterArtwork;
            host.type = Image.Type.Simple;
            host.color = Color.white;
            host.preserveAspect = true;
            return;
        }
        var root = Rect("PowerIconLayers", host.transform, Vector2.zero, Vector2.one);
        var spec = Describe(power);
        if(power is IconTypeGivesBase animalBase) {
            PowerCardFlow.DrawAnimalBonus(root,animalBase.ItemType,power.RewardGoal==GoalType.BlocksCleared?"credits":"base-badge",$"+{animalBase.BonusPerTile}B");
            return;
        }
        if(power is AnimalMasteryPower mastery&&!mastery.Rival.HasValue) {
            PowerCardFlow.DrawMasteryAnimal(root,mastery.ItemType,mastery.GrantsBasePoints);
            return;
        }
        if(power is AnimalSpawnPower magnet) {
            PowerCardFlow.DrawRefillAnimal(root,magnet.Animal);
            return;
        }
        if(power is not BossPowers && power is not LevelCharacteristicPower && power is not AnimalPairPower && !spec.Animal.HasValue) {
            PowerSymbolLibrary.Draw(root,power is ShufflePower?"shuffle":PowerCardFlow.Describe(power).Shape);
            return;
        }
        if(power is BudgetPower budget) {
            PowerCardFlow.DrawBudgetCondition(root,budget.Style.ToString().ToLowerInvariant());
            return;
        }
        if(power is AnimalPairPower pact) {
            PowerCardFlow.DrawPactPortrait(root,pact.First,pact.Second,TMP_Settings.defaultFontAsset);
            return;
        }
        if (power is AnimalMasteryPower rivalry && rivalry.Rival.HasValue)
        {
            DrawRivalryAnimal(root, rivalry.ItemType, true);
            DrawRivalryAnimal(root, rivalry.Rival.Value, false);
            DrawGrowthScope(root,power);
            return;
        }
        if (!spec.Animal.HasValue && DrawPowerSymbol(root, power))
        {
            if (!choicePortrait) DrawEffectBadge(root, spec);
            return;
        }
        Sprite artwork = null;
        if (spec.Animal.HasValue)
        {
            var items = ItemDatabase.items ?? Resources.LoadAll<Item>("Items/");
            foreach (var item in items)
                if (item != null && item.ItemType == spec.Animal.Value && item.sprite != null)
                { artwork = item.sprite; break; }
        }
        else artwork = power?.Icon;

        if (artwork != null)
        {
            var subject = Rect("Subject", root, choicePortrait ? new Vector2(.04f, .04f) : new Vector2(.05f, .18f),
                choicePortrait ? new Vector2(.96f, .96f) : new Vector2(.82f, .95f));
            var picture = subject.gameObject.AddComponent<Image>();
            picture.sprite = artwork;
            if (spec.Animal.HasValue) ItemUtils.StyleIcon(picture);
            picture.preserveAspect = true;
            picture.raycastTarget = false;
        }
        else
        {
            var subject = Rect("Subject", root, new Vector2(.06f, .35f), new Vector2(.94f, .88f));
            Label(subject, spec.Animal?.ToString().ToUpperInvariant() ?? spec.Subject, Color.white);
        }
        if (!choicePortrait) DrawEffectBadge(root, spec);
        DrawGrowthScope(root,power);
        if (power is ChallengeBoss)
        {
            var band = Rect("Boss crown", root, new Vector2(.20f,.80f), new Vector2(.64f,.88f)).gameObject.AddComponent<Image>();
            band.color = PrismatchTheme.Orange; band.raycastTarget = false;
            for (int i=0; i<3; i++) {
                var tip = Rect("Crown point", root, new Vector2(.20f+i*.16f,.86f), new Vector2(.32f+i*.16f,.98f)).gameObject.AddComponent<Image>();
                tip.color = PrismatchTheme.Orange; tip.raycastTarget = false;
            }
        }
    }

    private static void DrawRivalryAnimal(RectTransform root, ItemType animal, bool wanted)
    {
        float left = wanted ? .02f : .51f;
        var subject = Rect(wanted ? "Wanted animal" : "Rival animal", root,
            new Vector2(left, .22f), new Vector2(left + .47f, .92f));
        var items = ItemDatabase.items ?? Resources.LoadAll<Item>("Items/");
        Sprite artwork = null;
        foreach (var item in items)
            if (item != null && item.ItemType == animal && item.sprite != null)
            { artwork = item.sprite; break; }
        if (artwork != null)
        {
            var picture = subject.gameObject.AddComponent<Image>();
            picture.sprite = artwork;
            ItemUtils.StyleIcon(picture);
            picture.preserveAspect = true;
            picture.raycastTarget = false;
        }
        else Label(subject, animal.ToString(), Color.white);
        var mark = Rect(wanted ? "Checkmark" : "Cross", root,
            new Vector2(left + .22f, .02f), new Vector2(left + .47f, .36f));
        Label(mark, wanted ? "\u2713" : "\u00D7", wanted ? new Color(.55f, 1f, .65f) : new Color(1f, .4f, .4f));
    }

    public static PowerIconSpec BossPortrait(BossStyle style) => style switch {
        BossStyle.Coach => new("", "4+", animal: ItemType.Bear),
        BossStyle.Lighthouse => new("", "N", animal: ItemType.Sloth),
        BossStyle.Seesaw => new("", "T/B", animal: ItemType.Hippo),
        BossStyle.MigrationWarden=>new("", "1/2",animal:ItemType.Giraffe),
        BossStyle.VaultWarden=>new("", "VAULT",animal:ItemType.Bear),
        BossStyle.StoneKeeper=>new("", "GLOW",animal:ItemType.Sloth),
        BossStyle.RescueKeeper=>new("", "CAGE",animal:ItemType.Walrus),
        BossStyle.CageWarden=>new("", "1:1",animal:ItemType.Walrus),
        BossStyle.UnstableWinds=>new("", "?",animal:ItemType.Hippo),
        BossStyle.Wallflower => new("", "EDGE", animal: ItemType.Bear),
        BossStyle.CenterStage => new("", "IN", animal: ItemType.Moose),
        BossStyle.BoxTroll => new("", "2x2", animal: ItemType.Hippo),
        BossStyle.Stretch => new("", "4+", animal: ItemType.Giraffe),
        BossStyle.Oddball => new("", "ODD", animal: ItemType.Sloth),
        BossStyle.EvenSteven => new("", "EVEN", animal: ItemType.Panda),
        BossStyle.CascadeSurfer => new("", "FALL", animal: ItemType.Duck),
        BossStyle.OpeningAct => new("", "3rd", animal: ItemType.Chicken),
        BossStyle.CornerCollector => new("", "L", animal: ItemType.Walrus),
        BossStyle.TwinKeeper => new("", "PAIR", animal: ItemType.Panda),
        BossStyle.DuckEmbargo => new("", "0", animal: ItemType.Duck),
        BossStyle.TrophyHunter => new("", "6+", animal: ItemType.Moose),
        BossStyle.CascadeTax => new("", "4x", animal: ItemType.Sloth),
        BossStyle.Jackpot => new("", "3rd", animal: ItemType.Walrus),
        BossStyle.MostMatched => new("", "0", animal: ItemType.Bear),
        BossStyle.Overgrowth => new("", "LEAF", animal: ItemType.Giraffe),
        BossStyle.Blackout => new("", "D/B", animal: ItemType.Panda),
        BossStyle.FenceFox => new("", "EDGE", animal: ItemType.Chicken),
        BossStyle.TreasureCrab => new("", "GOLD", animal: ItemType.Walrus),
        BossStyle.TideTurtle => new("", "TIDE", animal: ItemType.Hippo),
        BossStyle.CrowdTax => new("", "2M", animal: ItemType.Moose),
        BossStyle.SmallChange => new("", "2M", animal: ItemType.Chicken),
        BossStyle.InnerToll => new("", "2M", animal: ItemType.Hippo),
        _ => new("", "2M", animal: ItemType.Panda)
    };

    private static void DrawEffectBadge(RectTransform root, PowerIconSpec spec)
    {
        var badge = Rect("Effect", root, new Vector2(.51f, .01f), new Vector2(.99f, .34f));
        var background = badge.gameObject.AddComponent<Image>();
        PrismatchTheme.Round(background, spec.Effect == "+" ? PrismatchTheme.Green :
            spec.Effect == "!" || spec.Effect == "0" ? PrismatchTheme.Coral : PrismatchTheme.Sky);
        background.raycastTarget = false;
        Label(Rect("Symbol", badge, Vector2.zero, Vector2.one), spec.Effect, PrismatchTheme.Navy);
    }

    // Normalized UI geometry stays crisp at both card and HUD sizes.
    // One large silhouette replaces the overlapping subject/effect/growth labels.
    private static bool DrawPowerSymbol(RectTransform root, PowerUp power)
    {
        if((power.Tags&PowerTags.Edge)!=0){PowerSymbolLibrary.Draw(root,"edge");return true;}
        void Tile(float x, float y, Color color, float size = .19f)
        {
            var tile = Rect("SymbolTile", root, new Vector2(x, y), new Vector2(x + size, y + size));
            var image = tile.gameObject.AddComponent<Image>();
            PrismatchTheme.Round(image, color);
            image.raycastTarget = false;
        }
        void Bar(float x, float y, float width, float height, Color color)
        {
            var image = Rect("SymbolStroke", root, new Vector2(x, y), new Vector2(x + width, y + height)).gameObject.AddComponent<Image>();
            PrismatchTheme.Round(image, color);
            image.raycastTarget = false;
        }
        void Arrow(float y, bool right)
        {
            Bar(.23f, y, .54f, .07f, PrismatchTheme.Cream);
            // A chevron made of two diagonals, with a clear direction at small sizes.
            for (int i = 0; i < 2; i++)
            {
                var stroke = Rect("Arrowhead", root,
                    new Vector2(right ? .66f : .14f, y + (i == 0 ? .045f : -.045f)),
                    new Vector2(right ? .86f : .34f, y + (i == 0 ? .105f : .015f)));
                stroke.localRotation = Quaternion.Euler(0, 0, (i == 0 ? -35 : 35) * (right ? 1 : -1));
                var image = stroke.gameObject.AddComponent<Image>();
                PrismatchTheme.Round(image, PrismatchTheme.Cream);
                image.raycastTarget = false;
            }
        }
        void Grid(int columns, int rows, Color color)
        {
            if(columns==2&&rows==2){PowerSymbolLibrary.Draw(root,"square");return;}
            if(columns==3&&rows==2){PowerSymbolLibrary.Draw(root,"large");return;}
            float size = columns == 2 ? .25f : .18f;
            float step = size + .055f;
            float left = (1 - (columns * step - .055f)) * .5f;
            float bottom = (1 - (rows * step - .055f)) * .5f;
            for (int y = 0; y < rows; y++)
            for (int x = 0; x < columns; x++) Tile(left + x * step, bottom + y * step, color, size);
        }
        void Edge()
        {
            PowerSymbolLibrary.Draw(root,"edge");
    }
        void Plus()
        {
            PowerSymbolLibrary.Draw(root,"plus");
    }
        void Cascade()
        {
            PowerSymbolLibrary.Draw(root,"cascade");
    }
        void Shape(BlockShape shape)
        {
            PowerSymbolLibrary.Draw(root,shape.ToString().ToLowerInvariant());
    }
        if (power is LevelCharacteristicPower characteristic)
        {
            if(characteristic.Lightning) {
                for(int i=0;i<3;i++)Tile(.30f+i*.08f,.65f-i*.20f,PrismatchTheme.Orange,.22f);
            } else switch(characteristic.Bonus) {
                case LevelBonus.LightningRods:
                    var rodImage=Rect("Lightning rod artwork",root,new Vector2(.08f,.05f),new Vector2(.92f,.95f)).gameObject.AddComponent<Image>();
                    rodImage.sprite=global::Tile.LightningRodSprite;rodImage.preserveAspect=true;rodImage.raycastTarget=false;break;
                case LevelBonus.AnimalSpotlight:
                    Grid(3,3,PrismatchTheme.Cream);Tile(.18f,.4f,Color.black,.25f);Tile(.57f,.4f,Color.black,.25f);break;
                case LevelBonus.RollingDarkness:
                    Grid(3,3,PrismatchTheme.Cream);Tile(.1f,.4f,Color.black,.2f);Tile(.4f,.4f,Color.black,.2f);break;
                case LevelBonus.Blackout:
                    Grid(3,3,Color.black);Tile(.4f,.4f,PrismatchTheme.Green,.2f);break;
                case LevelBonus.WildRelease:
                    global::Tile.BuildCageArtwork(root);break;
                case LevelBonus.MigrationSeason: case LevelBonus.AnimalRescue:
                    Tile(.17f,.4f,PrismatchTheme.Green,.28f);Tile(.58f,.4f,PrismatchTheme.RaisedNavy,.28f);Arrow(.18f,true);break;
                case LevelBonus.CrackTheVault: Grid(3,3,PrismatchTheme.Orange);break;
                case LevelBonus.SteppingStones: Cascade();break;
                case LevelBonus.Crosswinds: Plus();Arrow(.13f,true);break;
                case LevelBonus.GardenPlots: Grid(2,2,PrismatchTheme.Green); break;
                case LevelBonus.Square: Grid(2,2,PrismatchTheme.Sky); break;
                case LevelBonus.BorderPatrol: Edge(); break;
                case LevelBonus.Cascade: case LevelBonus.ChainReaction: Cascade(); break;
                case LevelBonus.LargeGroup: Grid(3,2,PrismatchTheme.Orange); break;
                case LevelBonus.TreasureTrail:
                    Tile(.29f,.32f,PrismatchTheme.Orange,.42f); Bar(.33f,.47f,.34f,.08f,PrismatchTheme.Cream); break;
                case LevelBonus.ChangingTides: Arrow(.65f,true);Arrow(.30f,false);break;
                default: Tile(.16f,.45f,PrismatchTheme.Sky,.27f);Tile(.57f,.45f,PrismatchTheme.Coral,.27f);Arrow(.22f,true);break;
            }
            return true;
        }
        if (power is HabitatPower habitat)
        {
            switch (habitat.Style)
            {
                case HabitatStyle.Shore: Edge(); break;
                case HabitatStyle.Garden: Grid(2,2,PrismatchTheme.Green); break;
                case HabitatStyle.Giants: Grid(3,2,PrismatchTheme.Orange); break;
                case HabitatStyle.Rapids: Cascade(); break;
                default:
                    Tile(.16f,.52f,PrismatchTheme.Sky,.27f); Tile(.57f,.52f,PrismatchTheme.Coral,.27f);
                    Arrow(.28f,true); break;
            }
            return true;
        }
        if (power is MatchFoundationPower foundation)
        {
            for (int i = 0; i < foundation.Minimum; i++)
                Tile(.10f + i * .16f, .43f, power is BlockMatchFoundationPower ? PrismatchTheme.Green : PrismatchTheme.Sky, .13f);
            return true;
        }
        if (power is CollectionPower collection && collection.Style == CollectionStyle.ShapeCredit)
        {
            Shape(collection.Shape);
            return true;
        }
        if (power is ShufflePower)
        {
            Tile(.18f,.4f,PrismatchTheme.Sky,.22f); Tile(.59f,.4f,PrismatchTheme.Orange,.22f);
            Arrow(.72f,true); Arrow(.21f,false);
            return true;
        }
        if (power is AnimalPairPower pair)
        {
            var items = ItemDatabase.items ?? Resources.LoadAll<Item>("Items/");
            foreach (var entry in new[] { (animal: pair.First, left: .04f), (animal: pair.Second, left: .49f) })
            {
                var item = System.Array.Find(items, animal => animal != null && animal.ItemType == entry.animal);
                if (item == null) continue;
                var image = Rect("Paired animal",root,new Vector2(entry.left,.32f),new Vector2(entry.left + .46f,.91f)).gameObject.AddComponent<Image>();
                image.sprite = item.sprite; ItemUtils.StyleIcon(image); image.preserveAspect = true; image.raycastTarget = false;
            }
            return true;
        }
        if (power is PatternAnimalPower || power is ColorFocusPower)
        {
            Tile(.16f,.54f,PrismatchTheme.Sky,.26f);
            Tile(.58f,.54f,power is ColorFocusPower ? PrismatchTheme.Sky : PrismatchTheme.Coral,.26f);
            if (power is ColorFocusPower) Arrow(.27f,true);
            else for (int i = 0; i < 3; i++) Bar(.19f+i*.23f,.19f,.14f,.09f+i*.055f,PrismatchTheme.Green);
            return true;
        }
        if (power is BudgetPower budget)
        {
            if (budget.Style == BudgetStyle.Square) Grid(2, 2, PrismatchTheme.Sky);
            else if (budget.Style == BudgetStyle.Large) Grid(3, 2, PrismatchTheme.Orange);
            else
                for (int y = 0; y < 3; y++)
                for (int x = 0; x < 3; x++)
                    Tile(.17f + x * .235f, .17f + y * .235f,
                        x == 0 || y == 0 || x == 2 || y == 2 ? PrismatchTheme.Sky : PrismatchTheme.RaisedNavy);
            return true;
        }
        if (power is TacticalStarter tactical)
        {
            switch (tactical.Style)
            {
                case TacticalStyle.Square:
                    Grid(2, 2, PrismatchTheme.Sky);
                    break;
                case TacticalStyle.Plus:
                    for (int i = 0; i < 3; i++) Tile(.17f + i * .235f, .405f, PrismatchTheme.Green);
                    Tile(.405f, .17f, PrismatchTheme.Green); Tile(.405f, .64f, PrismatchTheme.Green);
                    break;
                case TacticalStyle.Large:
                    Grid(3, 2, PrismatchTheme.Orange);
                    break;
                case TacticalStyle.FourStreak:
                    for (int i = 0; i < 4; i++) Tile(.115f + i * .2f, .51f, PrismatchTheme.Sky, .17f);
                    Arrow(.30f, true);
                    break;
                case TacticalStyle.Opening:
                    Bar(.25f, .20f, .065f, .62f, PrismatchTheme.Cream);
                    Bar(.315f, .53f, .43f, .28f, PrismatchTheme.Orange);
                    Bar(.18f, .17f, .25f, .065f, PrismatchTheme.Cream);
                    break;
                case TacticalStyle.DoubleSwap:
                    Tile(.18f, .38f, PrismatchTheme.Sky, .24f);
                    Tile(.58f, .38f, PrismatchTheme.Orange, .24f);
                    Arrow(.73f, true); Arrow(.20f, false);
                    break;
                case TacticalStyle.Alternating:
                    Tile(.16f, .52f, PrismatchTheme.Sky, .27f);
                    Tile(.57f, .52f, PrismatchTheme.Coral, .27f);
                    Arrow(.28f, true);
                    break;
                default:
                    for (int y = 0; y < 3; y++)
                    for (int x = 0; x < 3; x++)
                        Tile(.17f + x * .235f, .17f + y * .235f,
                            x == 0 || y == 0 || x == 2 || y == 2 ? PrismatchTheme.Sky : PrismatchTheme.RaisedNavy);
                    break;
            }
            return true;
        }
        if (power is AnimalPairPower || power is PatternAnimalPower || power is ColorFocusPower || power is BossPowers)
            return false;
        if (power is DirectionalMatchPower direction)
        {
            for (int i = 0; i < 3; i++)
                Tile(direction.Direction == MatchType.Horizontal ? .17f + i * .235f : .405f,
                    direction.Direction == MatchType.Vertical ? .17f + i * .235f : .405f, PrismatchTheme.Sky);
            return true;
        }
        if (power is BasePerTile || power is SnowballPower || power is ComboCrazePower || power is ConsistencyIsKey)
        {
            for (int i = 0; i < 3; i++) Bar(.18f + i * .23f, .20f, .17f, .22f + i * .18f, PrismatchTheme.Green);
            return true;
        }
        if (power is BlockClearPower block)
        {
            if (block.Style == BlockClearStyle.Edge) Edge();
            else if (block.Style == BlockClearStyle.Square) Grid(2,2,PrismatchTheme.Green);
            else if (block.Style == BlockClearStyle.LongLine) Grid(3,2,PrismatchTheme.Orange);
            else if (block.Style == BlockClearStyle.Cascade) Cascade();
            else
            {
                Grid(2,2,PrismatchTheme.Sky);
                Bar(.43f, .30f, .10f, .40f, PrismatchTheme.Cream);
                Bar(.28f, .45f, .40f, .10f, PrismatchTheme.Cream);
            }
            return true;
        }
        int count = power is PrecisionStrikePower || power is UnderdogPower ? 3 :
            power is FourExactlyPower || power is FourFuryPower || power is TwinShotPower || power is StackedOddsPower ? 4 :
            power is HighStakesPower || power is JackpotFivePower ? 5 : 0;
        if (count > 0)
        {
            // Dice-like tile groups communicate the required match size without tiny type.
            if (count != 3)
            {
                Tile(.22f, .22f, PrismatchTheme.Sky); Tile(.59f, .22f, PrismatchTheme.Sky);
                Tile(.22f, .59f, PrismatchTheme.Sky); Tile(.59f, .59f, PrismatchTheme.Sky);
            }
            else for (int i = 0; i < 3; i++) Tile(.17f + i * .235f, .405f, PrismatchTheme.Sky);
            if (count == 5) Tile(.405f, .405f, PrismatchTheme.Orange);
            return true;
        }
        if (power is FlexibleMatchPower || power is BaseMultiPowerUp || power is BasePowerUp || power is StarterPower)
        {
            Bar(.40f, .18f, .20f, .64f, PrismatchTheme.Green);
            Bar(.18f, .40f, .64f, .20f, PrismatchTheme.Green);
            return true;
        }
        return false;
    }

    private static RectTransform Rect(string name, Transform parent, Vector2 min, Vector2 max)
    {
        var rect = (RectTransform)new GameObject(name, typeof(RectTransform)).transform;
        rect.SetParent(parent, false);
        rect.anchorMin = min;
        rect.anchorMax = max;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
        return rect;
    }

    private static void Label(RectTransform rect, string value, Color color)
    {
        var label = rect.gameObject.AddComponent<TextMeshProUGUI>();
        label.text = value;
        label.fontStyle = FontStyles.Bold;
        label.alignment = TextAlignmentOptions.Center;
        label.enableAutoSizing = true;
        label.fontSizeMin = 8f;
        label.fontSizeMax = 48f;
        label.textWrappingMode = TextWrappingModes.NoWrap;
        label.color = color;
        label.raycastTarget = false;
    }
}
