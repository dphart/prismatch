using System;
using System.Linq;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public readonly struct PowerFlowSpec
{
    public readonly string Condition, Reward, Shape;
    public readonly string RewardShape;
    public readonly string BaseBadge;
    public readonly ItemType? ConditionAnimal, RewardAnimal;
    public readonly ItemType? PartnerAnimal;
    public readonly bool AllAnimals, Growth;
    public readonly bool Single;
    public PowerFlowSpec(string condition,string reward,string shape="match",ItemType? conditionAnimal=null,
        ItemType? rewardAnimal=null,bool allAnimals=false,bool growth=false,ItemType? partnerAnimal=null,string rewardShape=null,bool single=false,string baseBadge=null)
    {Condition=condition;Reward=reward;Shape=shape;ConditionAnimal=conditionAnimal;RewardAnimal=rewardAnimal;AllAnimals=allAnimals;Growth=growth;PartnerAnimal=partnerAnimal;RewardShape=rewardShape;Single=single;BaseBadge=baseBadge;}
    public PowerFlowSpec WithBaseBadge(string badge)=>new(Condition,Reward,Shape,ConditionAnimal,RewardAnimal,AllAnimals,Growth,PartnerAnimal,RewardShape,Single,badge);
}

/// <summary>Read-only card diagrams. Never evaluate scoring hooks to build a preview.</summary>
public static class PowerCardFlow
{
    public static PowerFlowSpec Describe(PowerUp power)
    {
        var spec=DescribeCore(power);
        if(power.RewardGoal!=GoalType.Score || !Regex.IsMatch(power.PowerUpDescription,@"\bbase\b",RegexOptions.IgnoreCase) || power is AnimalDoublePower or RosterTradeoffPower ||
            spec.Shape is "mastery-base" or "base-badge" || spec.RewardShape=="base-badge")return spec;
        var amount=Regex.Match(power.PowerUpDescription,@"(?:\+|gains?\s+|adds?\s+|gives?\s+|:\s*)(\d+(?:\.\d+)?)\s+(?:bonus\s+)?base",RegexOptions.IgnoreCase);
        return spec.WithBaseBadge(amount.Success?"+"+amount.Groups[1].Value+"B":"+B");
    }
    private static PowerFlowSpec DescribeCore(PowerUp power)
    {
        string description=power.PowerUpDescription; // Apply authored tuning before reading metadata.
        JObject definition=SharedGameContent.Power(power);
        int Value(string key,int fallback)=>(int?)definition?["parameters"]?[key]??fallback;
        bool blocks=power.RewardGoal==GoalType.BlocksCleared;
        string Unit(bool points)=>blocks?(points?"BLOCKS":"CLEARS"):(points?"BASE / TILE":"MULTIPLIER");
        if(power is AnimalSpawnPower magnet)return new("REFILLS + SHUFFLES","2x "+magnet.Animal.ToString().ToUpperInvariant()+" CHANCE","refill",rewardAnimal:magnet.Animal,single:true);
        if(power is AnimalPairPower pact)
            return new("MATCH TOGETHER",blocks?"HALF FINAL BLOCKS":"HALF POINTS","pair",pact.First,partnerAnimal:pact.Second);
        if(power is JackpotFivePower)return new("5+ TILES",blocks?RewardFromRule(description):"NEXT MATCH","count-5plus",rewardShape:blocks?"clears":"factor-5");
        if(power is TwinShotPower)return new("EXACTLY 4",blocks?RewardFromRule(description):"NEXT MATCH","count-4",rewardShape:blocks?"clears":"factor-2");
        if(power is FourFuryPower)return new("EXACTLY 4",blocks?RewardFromRule(description):"NEXT MATCH","count-4",rewardShape:blocks?"clears":"factor-3");
        if(power is PrecisionStrikePower)return new("TWO 3-TILE MATCHES IN A ROW",blocks?RewardFromRule(description):"NEXT MATCH","count-3",rewardShape:blocks?"clears":"factor-4");
        if(power is ColorFocusPower)return new("SAME ANIMAL TWICE","SECOND MATCH","repeat",rewardShape:blocks?"clears":"factor-2");
        if(power is ShufflePower shuffle) return shuffle.Style switch {
            ShuffleStyle.Earned=>new("5 MATCHING MOVES","FREE SHUFFLE","match",rewardShape:"shuffle"),
            ShuffleStyle.SquareTicket=>new("2 SQUARE MATCHING MOVES","FREE SHUFFLE","square",rewardShape:"shuffle"),
            ShuffleStyle.LargeTicket=>new("2 MATCHING MOVES OF 6+","FREE SHUFFLE","large",rewardShape:"shuffle"),
            ShuffleStyle.Discount=>new("SHUFFLE","HALF COST","shuffle",rewardShape:"half"),
            ShuffleStyle.Opening=>new("FIRST 3 EACH ROUND","FREE SHUFFLES","shuffle",rewardShape:"shuffle"),
            ShuffleStyle.Afterglow=>new("SHUFFLE",blocks?"NEXT MATCH: +8 BLOCKS":"NEXT PLAY","shuffle",rewardShape:blocks?"credits":"factor-2"),
            _=>new("SHUFFLE",blocks?"NEXT MATCH: +8 BLOCKS":"NEXT PLAY: +32 BASE","shuffle",rewardShape:blocks?"credits":"base")
        };
        if(power is IconTypeStreakGivesBasePointsOrMulti growth) {
            bool points=growth.GrantsBasePoints;
            return new($"{growth.RequiredTiles} TILES",$"+{Value(points?"BasePoints":"BaseMulti",1)} {Unit(points)}",
                conditionAnimal:growth.ItemType,rewardAnimal:blocks?growth.ItemType:null,allAnimals:!blocks,growth:true);
        }
        if(power is AnimalMasteryPower mastery)
        {
            if(!mastery.Rival.HasValue)return new("EVERY 2 PLAYER MATCHES · "+mastery.ItemType.ToString().ToUpperInvariant()+" ONLY",
                mastery.GrantsBasePoints?"+1 BASE POINT PER TILE":"+1 MULTIPLIER",mastery.GrantsBasePoints?"mastery-base":"mastery-multiplier",
                rewardAnimal:mastery.ItemType,single:true);
            return new($"{mastery.RequiredMatches} MATCH{(mastery.RequiredMatches==1?"":"ES")}",
                "+1 "+Unit(mastery.GrantsBasePoints),conditionAnimal:mastery.ItemType,rewardAnimal:mastery.ItemType,growth:true);
        }
        if(power is AnimalDoublePower doubled)
            return new("MATCH",blocks?"+1 BLOCK / TILE":"2x BASE",conditionAnimal:doubled.ItemType,rewardAnimal:doubled.ItemType);
        if(power is IconTypeGivesBase animalBase)
            return new(animalBase.ItemType.ToString().ToUpperInvariant()+" MATCHES",blocks?$"+{animalBase.BonusPerTile} BLOCKS / MATCH":$"+{animalBase.BonusPerTile}B PER TILE",
                blocks?"credits":"base-badge",rewardAnimal:animalBase.ItemType,rewardShape:blocks?"credits":"base-badge",single:true);
        if(power is IconTypeGivesMulti animalMulti)
            return new("MATCH",$"+{Value("multi",2)} "+(blocks?"CLEARS":"MULTIPLIER"),conditionAnimal:animalMulti.ItemType,rewardAnimal:animalMulti.ItemType);
        if(power is CollectionPower collection) {
            string shape=collection.Style==CollectionStyle.ShapeCredit?collection.Shape.ToString().ToLowerInvariant():collection.Bloom.ToString().ToLowerInvariant();
            if(collection.Style==CollectionStyle.Transform)
                return new(collection.Bloom==BloomCondition.Any?"PLAYER MATCH":collection.Bloom==BloomCondition.Large?"6+ TILES":collection.Bloom.ToString().ToUpperInvariant(),
                    $"TURN {collection.TransformCount} TILES",shape,rewardAnimal:collection.Animal);
            if(collection.Style==CollectionStyle.Admission)return new("ADD TO BOARD","COUNTS FOR GOAL",rewardAnimal:collection.Animal);
            return new(collection.Style==CollectionStyle.ShapeCredit?collection.Shape.ToString().ToUpperInvariant():"MATCH",
                blocks?"+1 BLOCK / TILE":"+4 BASE / TILE",shape,collection.Style==CollectionStyle.AnimalCredit?collection.Animal:null);
        }
        if(power is HabitatPower habitat) {
            string condition=habitat.Style switch {HabitatStyle.Shore=>"EDGE",HabitatStyle.Rapids=>"CASCADE",HabitatStyle.Giants=>"6+ TILES",HabitatStyle.Garden=>"SQUARE",_=>habitat.Tier==2?"ALL ANIMALS":"SWITCH ANIMAL"};
            string shape=habitat.Style switch {HabitatStyle.Shore=>"edge",HabitatStyle.Rapids=>"cascade",HabitatStyle.Garden=>"square",HabitatStyle.Giants=>"large",_=>"match"};
            return new(condition,RewardFromRule(description),shape,growth:habitat.Tier==2,rewardShape:habitat.Tier==0?(blocks?"credits":"base"):(blocks?"clears":"multiplier"));
        }
        if(power is RosterTradeoffPower roster)
            return new(roster.AddsAnimal?"ADD ANIMAL":"REMOVE ANIMAL",(roster.AddsAnimal?"+25% ":"-25% ")+(blocks?"BLOCKS":"POINTS"),
                conditionAnimal:roster.Animal,allAnimals:true);
        if(power is BudgetPower budget)
            return new($"{budget.MatchesRequired} MATCHES",budget.RewardsMoves?"+1 MOVE":"+3 SECONDS",
                "budget-"+budget.Style.ToString().ToLowerInvariant(),rewardShape:budget.RewardsMoves?"moves":"clock");
        if(power is TacticalStarter) {
            string upgrade=description.Split('\n').Last();
            return new(ConditionFromRule(upgrade),RewardFromRule(upgrade),
                upgrade.Contains("2x2")?"square":upgrade.Contains("edge")?"edge":"match",growth:true);
        }
        // Native and structured cards use their authored rule to retain tuned amounts.
        string conditionText=power switch {
            FourExactlyPower=>$"EXACTLY {Value("exactMatchNumber",4)}",
            HighStakesPower=>$"{Value("atLeastXTiles",6)}+ TILES",
            UnderdogPower=>$"{Value("maxTiles",4)} OR FEWER",
            StackedOddsPower=>$"EXACTLY {Value("requiredTiles",4)}",
            ConsistencyIsKey=>"SAME SIZE AGAIN",
            TwinShotPower or FourFuryPower=>"EXACTLY 4",
            ColorFocusPower=>"SAME ANIMAL TWICE",
            JackpotFivePower=>"5+ TILES",
            PrecisionStrikePower=>"TWO MATCHES OF 3",
            NoVerticalDoublePower=>"ROW MATCH",
            VerticalMatchPower=>"COLUMN MATCH",
            AnimalSpawnPower=>"REFILL / SHUFFLE",
            EdgeBalancePower=>"EDGE + / INSIDE -",
            PatternAnimalPower=>"LEAST MATCHED",
            _=>ConditionFromRule(description)
        };
        string shapeKey=conditionText.Contains("SQUARE")?"square":conditionText.Contains("EDGE")?"edge":
            conditionText.Contains("CASCADE")||conditionText.Contains("FALLING")?"cascade":
            conditionText.Contains("ROW")?"horizontal":conditionText.Contains("COLUMN")?"vertical":"match";
        shapeKey=power switch {
            FourExactlyPower=>"count-"+Value("exactMatchNumber",4),
            HighStakesPower=>"count-"+Value("atLeastXTiles",6)+"plus",
            UnderdogPower=>"count-"+Value("maxTiles",4)+"max",
            StackedOddsPower=>"count-"+Value("requiredTiles",4),
            MatchFoundationPower foundation=>"count-"+foundation.Minimum+"plus",
            ShufflePower=>"shuffle",ConsistencyIsKey=>"repeat",_=>shapeKey
        };
        ItemType? animal=(power is IconBonusPower icon && power is not BossPowers)?icon.ItemType:null;
        if(power is AnimalSpawnPower spawn)animal=spawn.Animal;
        string rewardShape=power is BlockClearPower?"clears":power is AnimalSpawnPower?"spawn":power is ShufflePower?"shuffle":
            power.AffectsStat(false)?(blocks?"credits":"base"):power.AffectsStat(true)?(blocks?"clears":"multiplier"):"plus";
        return new(conditionText,RewardFromRule(description),shapeKey,rewardAnimal:animal,growth:(power.Tags&PowerTags.Growth)!=0,rewardShape:rewardShape);
    }

    private static string ConditionFromRule(string rule)
    {
        string first=rule.Split('\n')[0];
        // Show the actual trigger, not a made-up numeric requirement.
        var part=Regex.Split(first,@"\s+(?:gain|earn|give|turn|clear|add)\b|:",RegexOptions.IgnoreCase)[0];
        return part.Trim().TrimEnd('.').ToUpperInvariant();
    }
    private static string RewardFromRule(string rule)
    {
        // Preserve non-numeric rewards and tradeoffs rather than showing an unexplained '+'.
        var part=Regex.Match(rule,@"(?:gains?|earns?|gives?|turns?|clears?|adds?|scores?|:)\s*(.+?)(?<!\d)\.(?!\d)|(?:gains?|earns?|gives?|turns?|clears?|adds?|scores?|:)\s*(.+)$",RegexOptions.IgnoreCase);
        string reward=part.Success?(part.Groups[1].Success?part.Groups[1].Value:part.Groups[2].Value):rule.Split('\n')[0];
        return reward.Trim().ToUpperInvariant().Replace("BASE POINTS PER TILE","BASE / TILE")
            .Replace("EXTRA BLOCK CREDITS","BLOCKS").Replace("EXTRA NEIGHBORING TILES","EXTRA CLEARS")
            .Replace("EXTRA TILES NEXT TO THEM","EXTRA CLEARS").Replace("FOR ALL MATCHES THIS RUN","")
            .Replace("PERMANENTLY","").Replace("THIS RUN","").Trim();
    }

    public static void Build(RectTransform root,PowerUp power,TMP_FontAsset font)
    {
        foreach(Transform old in root){old.gameObject.SetActive(false);if(Application.isPlaying)UnityEngine.Object.Destroy(old.gameObject);else UnityEngine.Object.DestroyImmediate(old.gameObject);}
        root=Rect("Centered diagram",root,new(.10f,0),new(.90f,1));
        var spec=Describe(power);
        if(spec.Single) {
            var single=Rect("Single effect",root,new(.06f,0),new(.94f,1));
            var portrait=Rect("Effect portrait",single,new(.30f,.39f),new(.70f,1));
            if(spec.Shape.StartsWith("mastery-"))DrawMasteryAnimal(portrait,spec.RewardAnimal.Value,spec.Shape=="mastery-base");
            else if(spec.Shape=="base-badge"||spec.Shape=="credits")DrawAnimalBonus(portrait,spec.RewardAnimal.Value,spec.Shape,spec.Reward.Split(' ')[0]);
            else DrawRefillAnimal(portrait,spec.RewardAnimal.Value);
            Text(Rect("Effect",single,new(0,.20f),new(1,.39f)),spec.Reward,font,23,PrismatchTheme.Navy);
            Text(Rect("Applies to",single,Vector2.zero,new(1,.20f)),spec.Condition,font,18,PrismatchTheme.RaisedNavy);
            return;
        }
        var left=Rect("Condition",root,new(0,0),new(.44f,1));
        var right=Rect("Reward",root,new(.56f,0),Vector2.one);
        DrawEndpoint(left,spec.Condition,spec.ConditionAnimal,false,spec.Shape,font,false,spec.PartnerAnimal);
        DrawEndpoint(right,spec.Reward,spec.RewardAnimal,spec.AllAnimals,spec.RewardShape??(spec.Shape=="pair"?"penalty":"reward"),font,spec.Growth,baseBadge:spec.BaseBadge);
        PowerSymbolLibrary.Draw(Rect("Condition arrow",root,new(.455f,.52f),new(.545f,.78f)),"arrow");
    }
    public static void DrawPactPortrait(RectTransform root,ItemType first,ItemType second,TMP_FontAsset font)
    {
        DrawAnimal(Rect("First animal",root,new(0,.10f),new(.38f,.96f)),first);
        DrawAnimal(Rect("Partner animal",root,new(.62f,.10f),new(1,.96f)),second);
        var arrows=Rect("Interchangeable animals",root,new(.39f,.32f),new(.61f,.72f));
        PowerSymbolLibrary.Draw(arrows,"interchange");
    }
    public static void DrawRefillAnimal(RectTransform root,ItemType animal)
    {
        PowerSymbolLibrary.Draw(root,"refill");
        DrawAnimal(Rect("Favored animal",root,new(.48f,0),new(1,.58f)),animal);
    }
    public static void DrawMasteryAnimal(RectTransform root,ItemType animal,bool basePoints)
    {
        DrawAnimalBonus(root,animal,basePoints?"mastery-base":"mastery-multiplier");
    }
    public static void DrawAnimalBonus(RectTransform root,ItemType animal,string badge,string textOverride=null)
    {
        DrawAnimal(Rect("Mastered animal",root,new(0,.12f),new(.88f,1)),animal);
        PowerSymbolLibrary.Draw(Rect("Animal bonus",root,new(.48f,0),new(1,.52f)),badge,textOverride:textOverride);
    }
    private static void DrawEndpoint(RectTransform root,string caption,ItemType? animal,bool all,string shape,TMP_FontAsset font,bool growth,ItemType? partner=null,string baseBadge=null)
    {
        var graphic=Rect("Graphic",root,new(.18f,.40f),new(.82f,1));
        if(baseBadge!=null) {
            bool badgeOnly=!all&&!animal.HasValue&&(shape is "base" or "plus" or "reward" or "base-badge");
            if(badgeOnly){PowerSymbolLibrary.Draw(graphic,"base-badge",textOverride:baseBadge);Text(Rect("Caption",root,Vector2.zero,new(1,.39f)),caption,font,23,PrismatchTheme.Navy);return;}
        }
        if(shape.StartsWith("budget-"))DrawBudgetCondition(graphic,shape.Substring(7));
        else if(shape=="clock"||shape=="moves")DrawBudgetReward(graphic,shape=="moves");
        else if(animal.HasValue&&partner.HasValue)DrawPactPortrait(graphic,animal.Value,partner.Value,font);
        else if(animal.HasValue&&PowerSymbolLibrary.Canonical(shape)=="match")PowerSymbolLibrary.Draw(graphic,"animal-match",animal.Value);
        else if(animal.HasValue)DrawAnimal(graphic,animal.Value);
        else if(shape=="penalty")PowerSymbolLibrary.Draw(graphic,"half");
        else if(all) {
            var animals=new[]{ItemType.Duck,ItemType.Hippo,ItemType.Bear,ItemType.Giraffe,ItemType.Sloth,ItemType.Chicken};
            for(int i=0;i<animals.Length;i++) {
                int column=i%3,row=i/3;
                DrawAnimal(Rect("Animal",graphic,new(column/3f,(1-row)/2f),new((column+1)/3f,(2-row)/2f)),animals[i]);
            }
        }
        else PowerSymbolLibrary.Draw(graphic,shape=="reward"?"plus":shape,textOverride:shape=="base-badge"?caption.Split(' ')[0]:null);
        if(baseBadge!=null)PowerSymbolLibrary.Draw(Rect("Added base badge",root,new(.58f,.40f),new(.90f,.72f)),"base-badge",textOverride:baseBadge);
        Text(Rect("Caption",root,Vector2.zero,new(1,.39f)),caption,font,23,PrismatchTheme.Navy);
    }
    public static void DrawBudgetCondition(RectTransform root,string shape)
    {
        PowerSymbolLibrary.Draw(root,shape);
    }
    private static void DrawBudgetReward(RectTransform root,bool moves)
    {
        PowerSymbolLibrary.Draw(root,moves?"moves":"clock");
    }
    private static void DrawAnimal(RectTransform root,ItemType animal)
    {
        var items=ItemDatabase.items??Resources.LoadAll<Item>("Items/");
        var item=items.FirstOrDefault(i=>i!=null&&i.ItemType==animal);
        if(item?.sprite==null){Text(root,animal.ToString(),TMP_Settings.defaultFontAsset,18,PrismatchTheme.Navy);return;}
        var image=root.gameObject.AddComponent<Image>();image.sprite=item.sprite;image.preserveAspect=true;image.raycastTarget=false;ItemUtils.StyleIcon(image);
    }
    private static void Text(RectTransform root,string value,TMP_FontAsset font,int size,Color color)
    {
        var label=root.gameObject.AddComponent<TextMeshProUGUI>();label.font=font;label.text=value;
        label.fontStyle=FontStyles.Bold;label.color=color;label.alignment=TextAlignmentOptions.Center;
        label.enableAutoSizing=true;label.fontSizeMin=14;label.fontSizeMax=size;label.raycastTarget=false;
    }
    private static RectTransform Stroke(Transform parent,Vector2 min,Vector2 max,Color color)
    {var rect=Rect("Mark",parent,min,max);var image=rect.gameObject.AddComponent<Image>();image.color=color;image.raycastTarget=false;return rect;}
    private static RectTransform Rect(string name,Transform parent,Vector2 min,Vector2 max)
    {var rect=new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>();rect.SetParent(parent,false);rect.anchorMin=min;rect.anchorMax=max;rect.offsetMin=rect.offsetMax=Vector2.zero;return rect;}
}
