using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json.Linq;
using UnityEngine;

/// <summary>The same document is read by Unity and the local design studio.</summary>
public static class SharedGameContent
{
    public static JObject Document { get; private set; }
    private static Dictionary<string,JObject> native = new();
    private static Dictionary<string,JObject> ids = new();
    public static bool IsLoaded => Document != null;
    public static readonly Dictionary<string,string[]> TunableFields = new() {
        ["MatchFoundationPower"] = new[]{"minimumMatch","pointsPerTile"},
        ["BlockMatchFoundationPower"] = new[]{"minimumMatch","pointsPerTile"},
        ["FourExactlyPower"] = new[]{"exactMatchNumber","basePointsMulti"},
        ["HighStakesPower"] = new[]{"atLeastXTiles","bonusPoints"},
        ["StackedOddsPower"] = new[]{"requiredTiles","baseMulti"},
        ["UnderdogPower"] = new[]{"maxTiles","plusMulti"},
        ["BasePowerUp"] = new[]{"pointsPerTile"},
        ["BaseMultiPowerUp"] = new[]{"pointsPerTile","baseMulti"},
        ["IconTypeGivesBase"] = new[]{"BasePoints"},
        ["IconTypeGivesMulti"] = new[]{"multi"},
        ["IconTypeStreakGivesBasePointsOrMulti"] = new[]{"BasePoints","NumRequired","BaseMulti"},
        ["ConsistencyIsKey"] = new[]{"additiveMulti"}
    };
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Initialize()
    {
        var asset = Resources.Load<TextAsset>("Content/prismatch");
        if (asset == null) throw new InvalidOperationException("Shared game content is missing: Resources/Content/prismatch.json");
        Load(asset.text);
        LevelLibrary.ReloadSharedContent();
        ValidateCoverage();
    }
    public static void Load(string json)
    {
        var doc = JObject.Parse(json);
        if ((int?)doc["version"] != 1 || doc["powers"] is not JArray || doc["levels"] is not JArray)
            throw new ArgumentException("Invalid shared content version or collections.");
        var powers = doc["powers"].Cast<JObject>().ToList();
        foreach(var power in powers)
            if(power["tier"]!=null && !new[]{"Basic","Rare","Legendary"}.Contains((string)power["tier"]))
                throw new ArgumentException("Invalid power tier: "+power["id"]);
        var byId = powers.ToDictionary(p => (string)p["id"]);
        var byNative = powers.Where(p => p["nativeKey"] != null).ToDictionary(p => (string)p["nativeKey"]);
        foreach(var copy in powers.Where(p=>p["levelId"]!=null)) {
            if(!byId.TryGetValue((string)copy["basePowerId"],out var basis)||basis["levelId"]!=null||
               !doc["levels"].Cast<JObject>().Any(l=>(string)l["id"]==(string)copy["levelId"]))throw new ArgumentException("Invalid level power copy.");
            if((string)copy["source"]=="variant"&&((string)basis["source"]!="game"||copy["nativeKey"]!=null||
               new[]{"type","tags","goals","openingOnly","engine"}.Any(key=>!JToken.DeepEquals(copy[key],basis[key]))))
                throw new ArgumentException("A native copy must retain its base mechanic.");
        }
        foreach(var p in powers.Where(p=>(string)p["source"]=="custom")) {
            string[] triggers={"always","animal","exact","minimum","edge","corner","square","cascade","initial","sequence"};
            string reward=(string)p["reward"],goal=(string)p["goals"];
            if(!triggers.Contains((string)p["trigger"])||!new[]{"base","multi","factor","credits"}.Contains(reward)||
               (reward=="credits"?goal!="Blocks":goal!="Points")||!Enum.TryParse<ItemType>((string)p["animal"],out _)&&(string)p["animal"]!="Any"||
               (int?)p["length"]<3||(int?)p["length"]>10||(int?)p["threshold"]<1||(int?)p["threshold"]>100||
               (float?)p["value"]<0||(float?)p["value"]>10000||(reward!="multi"&&(float)p["value"]!=(int)p["value"]))
                throw new ArgumentException("Invalid structured power: "+p["id"]);
            if(reward=="factor"&&(int)p["value"]<1)throw new ArgumentException("Score factors must be positive.");
            if((string)p["trigger"]=="sequence"&&((string)p["sequence"]).Split(',').Any(s=>!int.TryParse(s,out int n)||n<3||n>10))throw new ArgumentException("Invalid sequence.");
        }
        foreach (var p in powers.Where(p => p["parameters"] is JObject))
        foreach (var value in (JObject)p["parameters"])
        {
            if (!TunableFields.TryGetValue((string)p["type"],out var allowed) || !allowed.Contains(value.Key))
                throw new ArgumentException("Unsupported native parameter: " + value.Key);
            if(value.Value.Type != JTokenType.Integer) throw new ArgumentException("Parameters must be whole numbers.");
            int n = value.Value.Value<int>();
            bool size = value.Key == "exactMatchNumber" || value.Key == "atLeastXTiles" || value.Key == "requiredTiles" || value.Key == "maxTiles" || value.Key == "minimumMatch";
            if (n < (size ? 3 : value.Key == "NumRequired" ? 1 : 0) || n > (size ? 10 : 10000))
                throw new ArgumentException("Invalid native parameter: " + value.Key);
            if(((string)p["type"]=="MatchFoundationPower"||(string)p["type"]=="BlockMatchFoundationPower")&&value.Key=="pointsPerTile"&&n<1)
                throw new ArgumentException("Foundations need positive credit or points.");
        }
        foreach (var level in doc["levels"].Cast<JObject>())
        {
            if(level["powerOverrides"] is JObject overrides) foreach(var pair in overrides) {
                if(!byId.TryGetValue(pair.Key,out var basis)||basis["levelId"]!=null||!byId.TryGetValue((string)pair.Value,out var copy)||
                   (string)copy["levelId"]!=(string)level["id"]||(string)copy["basePowerId"]!=pair.Key)
                    throw new ArgumentException("A power copy must belong to its level and original power.");
            }
            ValidateRoundOffers(level,byId,new HashSet<string>((doc["offerFilterKeys"] as JArray??new JArray()).Values<string>()));
            foreach (string key in new[]{"starters","rewards"})
                foreach (var id in level[key] as JArray ?? new JArray())
                    if (!byId.ContainsKey((string)id)) throw new ArgumentException("Unknown power in level: " + id);
        }
        Document = doc; native = byNative; ids = byId;
    }
    public static string RenderForGoal(JObject definition,GoalType goal)
    {
        string template=(string)definition[goal==GoalType.Score?"pointDescriptionTemplate":"blockDescriptionTemplate"];
        if(template==null)return Render(definition);
        if(definition["parameters"] is JObject values)foreach(var pair in values)template=template.Replace("{"+pair.Key+"}",pair.Value.ToString());
        return template;
    }
    public static JObject Power(PowerUp power) => power is SharedRulePower rule ? ids.GetValueOrDefault(rule.DefinitionId) : power.SharedDefinitionId != null ? ids.GetValueOrDefault(power.SharedDefinitionId) : native.TryGetValue(power.NativeContentKey,out var p) ? p : null;
    public static JObject Boss(PowerUp power) => power is ChallengeBoss boss
        ? Document?["bosses"]?.Cast<JObject>().FirstOrDefault(b => (string)b["style"] == boss.Style.ToString()) : null;
    public static string Render(JObject definition)
    {
        string text = (string)(definition["descriptionTemplate"] ?? definition["description"]) ?? "";
        if (definition["parameters"] is JObject parameters)
            foreach (var pair in parameters) text = text.Replace("{"+pair.Key+"}",pair.Value.ToString());
        return text;
    }
    public static void Configure(PowerUp power)
    {
        var p = Power(power);
        if (p?["parameters"] is not JObject values) return;
        foreach (var pair in values)
        {
            var field = power.GetType().GetField(pair.Key,BindingFlags.Instance|BindingFlags.NonPublic|BindingFlags.Public);
            if (field == null || field.FieldType != typeof(int)) throw new InvalidOperationException("Content parameter no longer matches code: " + pair.Key);
            field.SetValue(power,pair.Value.Value<int>());
        }
    }
    public static bool Enabled(PowerUp power) {
        // Retired until their animal-goal behavior is redesigned, including level copies.
        if(power is CollectionPower collection && collection.Style==CollectionStyle.Admission)return false;
        power.ConfigureSharedContent();return (bool?)Power(power)?["enabled"] != false;
    }
    public static bool HasPool(int number,int round) => (Level(number)?[round==1?"starters":"rewards"] as JArray)?.Count>0;
    public static void ValidateCoverage()
    {
        var catalog=PowerOfferBuilder.Catalog().Concat(new PowerUp[]{new BasePowerUp(2),new BiomeOpeningPower(true),new BiomeOpeningPower(false),new SpotlightOpeningPower()}).ToList();
        foreach(var power in catalog.Where(p=>p is not SharedRulePower)) {
            if(Power(power)==null)throw new InvalidOperationException("Add the new power to shared content: "+power.NativeContentKey);
            power.ConfigureSharedContent();
        }
        foreach(var d in Document["levels"].Cast<JObject>()) {
            var level=ApplyLevel(new CampaignLevel((int)d["number"],"","",new FreePlayConfig()),d);
            foreach(int round in new[]{1,2}) if(HasPool(level.GetLevelNumber,round)) Pool(level.GetLevelNumber,round);
        }
    }
    public static List<PowerUp> Pool(int number,int round)
    {
        var pool=Level(number)?[round==1?"starters":"rewards"] as JArray;
        if(pool==null||pool.Count==0)return null;
        var catalog=PowerOfferBuilder.Catalog().Concat(new PowerUp[]{new BasePowerUp(2),new BiomeOpeningPower(true),new BiomeOpeningPower(false),new SpotlightOpeningPower()}).ToList();
        var overrides=Level(number)?["powerOverrides"] as JObject;
        return pool.Select(id=>Find((string)overrides?[(string)id]??(string)id,catalog)??throw new InvalidOperationException("Unknown shared power: "+id)).ToList();
    }
    public static IEnumerable<PowerUp> CustomPowers() => Document?["powers"]?.Cast<JObject>()
        .Where(p => (string)p["source"] == "custom" && p["levelId"] == null && (bool?)p["enabled"] != false).Select(p => (PowerUp)new SharedRulePower(p)) ?? Enumerable.Empty<PowerUp>();
    public static PowerUp Find(string id,IEnumerable<PowerUp> candidates)
    {
        var definition=ids.GetValueOrDefault(id);
        if ((string)definition?["source"]=="custom") return new SharedRulePower(definition);
        string baseId=(string)definition?["basePowerId"]??id;
        var seed=candidates.FirstOrDefault(p => (string)Power(p)?["id"]==baseId);
        var result=seed?.Clone();
        return result!=null && baseId!=id ? result.WithSharedDefinition(id) : result;
    }
    public static List<PowerUp> ForLevel(IEnumerable<PowerUp> powers,int number)
    {
        var overrides=Level(number)?["powerOverrides"] as JObject;
        return powers.Select(p=>{
            string id=p is SharedRulePower rule?rule.DefinitionId:(string)Power(p)?["id"];
            string replacement=id==null?null:(string)overrides?[id];
            return replacement==null?p:Find(replacement,new[]{p})??throw new InvalidOperationException("Missing level power copy: "+replacement);
        }).ToList();
    }
    private static void ValidateRoundOffers(JObject level,Dictionary<string,JObject> powers,HashSet<string> keys)
    {
        if(level["roundOffers"]==null)return;
        if(level["roundOffers"] is not JArray offers||offers.Count>30)throw new ArgumentException("Invalid round offers.");
        var seen=new HashSet<int>();
        foreach(var offer in offers.Cast<JObject>()) {
            if(offer["round"]?.Type!=JTokenType.Integer||(int)offer["round"]<1||(int)offer["round"]>(int)level["rounds"]||!seen.Add((int)offer["round"])||
               string.IsNullOrWhiteSpace((string)offer["label"])||((string)offer["label"]).Length>100||offer["slots"] is not JArray slots||slots.Count<1||slots.Count>3)
                throw new ArgumentException("Round offers must fit the level and have a title and power groups.");
            int total=0;
            foreach(var slot in slots.Cast<JObject>()) {
                if(slot["count"]?.Type!=JTokenType.Integer||(int)slot["count"]<1||(int)slot["count"]>3)throw new ArgumentException("Invalid round group count.");
                total+=(int)slot["count"];
                if(!new[]{"PreferNew","NewOnly"}.Contains((string)slot["repeats"]??"PreferNew"))throw new ArgumentException("Invalid repeat rule.");
                if((string)slot["mode"]=="powers") {
                    if(slot["powerIds"] is not JArray pool||pool.Count==0)throw new ArgumentException("A picked group needs powers.");
                    foreach(string id in pool.Values<string>()) {
                        if(!powers.TryGetValue(id,out var p))throw new ArgumentException("Unknown round power: "+id);
                        if((int)offer["round"]>1&&(bool?)p["openingOnly"]==true)throw new ArgumentException("Starting powers can only appear in round 1.");
                        if((string)level["goal"]=="Score"?(string)p["goals"]=="Blocks":(string)p["goals"]=="Points")throw new ArgumentException("Round power does not support this goal.");
                    }
                }else if((string)slot["mode"]=="filters") {
                    if(!new[]{"all","any"}.Contains((string)slot["match"])||slot["filters"] is not JObject filters||
                       filters.Properties().Any(p=>!keys.Contains(p.Name)||!new[]{"include","exclude"}.Contains((string)p.Value)))throw new ArgumentException("Invalid round filters.");
                }else throw new ArgumentException("Unknown round group mode.");
            }
            if(total>3)throw new ArgumentException("A round can offer at most three powers.");
        }
    }
    public static bool MatchesOfferGroup(JObject slot,PowerUp power)
    {
        var definition=power is SharedRulePower custom?ids.GetValueOrDefault(custom.DefinitionId):Power(power);
        if(definition==null)return false;
        if((string)slot["mode"]=="powers")return ((JArray)slot["powerIds"]).Values<string>().Any(id=>id==(string)definition["id"]||id==(string)definition["basePowerId"]);
        var qualities=new HashSet<string>((definition["qualities"] as JArray??new JArray()).Values<string>());
        var filters=((JObject)slot["filters"]).Properties().ToArray();
        if(filters.Any(f=>(string)f.Value=="exclude"&&qualities.Contains(f.Name)))return false;
        var include=filters.Where(f=>(string)f.Value=="include").ToArray();
        return include.Length==0||((string)slot["match"]=="any"?include.Any(f=>qualities.Contains(f.Name)):include.All(f=>qualities.Contains(f.Name)));
    }
    public static CampaignLevel ApplyLevel(CampaignLevel seed,JObject d)
    {
        var settings = new FreePlayConfig(MatchType.Snake,(int)d["width"],(int)d["height"],Parse<GoalType>(d,"goal"),(int)d["target"],
            Parse<FreePlayMode>(d,"mode"),(int)d["seconds"],(int)d["moves"],(int)d["minimum"],Parse<ReplacementStrategy>(d,"gravity"),(int)d["rounds"],(int)d["growth"]);
        var rounds = new Dictionary<int,PowerOfferDefinition>();
        foreach (var r in (d["roundDrafts"] as JArray ?? new JArray()).Cast<JObject>())
        {
            int n=(int)r["round"]; if(n>settings.Levels || r["slots"] is not JArray slots || slots.Count==0) continue;
            rounds[n]=new PowerOfferDefinition((string)r["label"],slots.Cast<JObject>().Select(slot => {
                var parts=((string)slot["query"]).Split(';').Select(s=>s.Trim().Split('=')).ToDictionary(s=>s[0],s=>s[1]);
                return new PowerOfferSlot(new PowerTagQuery(ParseTags(parts["all"]),ParseTags(parts["any"]),ParseTags(parts["none"])),(int)slot["count"]);
            }));
        }
        var mechanic=(string)d["mechanic"];
        if((d["starters"] as JArray)?.Count>0)rounds.Remove(1);
        if((d["rewards"] as JArray)?.Count>0)foreach(int n in rounds.Keys.Where(n=>n>1).ToList())rounds.Remove(n);
        foreach(var offer in (d["roundOffers"] as JArray??new JArray()).Cast<JObject>()) {
            var slots=((JArray)offer["slots"]).Cast<JObject>().Select(s=>new PowerOfferSlot(new PowerTagQuery(),(int)s["count"],
                (OfferRepeatPolicy)Enum.Parse(typeof(OfferRepeatPolicy),(string)s["repeats"]??"PreferNew"),p=>MatchesOfferGroup(s,p)));
            rounds[(int)offer["round"]]=new PowerOfferDefinition((string)offer["label"],slots);
        }
        var bossStyle=(string)d["bossStyle"];
        if (!string.IsNullOrEmpty(bossStyle) && bossStyle != "None" &&
            !ChallengeBoss.CanAffect(Parse<BossStyle>(d,"bossStyle"), settings.Goal, settings.Mode,
                mechanic == "Lightning" ? LevelBonus.None : (LevelBonus)Enum.Parse(typeof(LevelBonus),mechanic)))
            throw new ArgumentException("The selected boss cannot affect this level's goal, mode or mechanic.");
        var result=new CampaignLevel(seed.GetLevelNumber,(string)d["name"],(string)d["mechanicNotes"],settings,
            bossFinal:!string.IsNullOrEmpty(bossStyle)&&bossStyle!="None",powerOffers:new LevelPowerOffers(rounds),
            finalBossStyle:!string.IsNullOrEmpty(bossStyle)&&bossStyle!="None"?(BossStyle?)Enum.Parse(typeof(BossStyle),bossStyle):null,
            habitat:(string)d["habitat"]=="Neutral"?null:(HabitatStyle?)Parse<HabitatStyle>(d,"habitat"),
            bonus:mechanic=="Lightning"?LevelBonus.None:(LevelBonus)Enum.Parse(typeof(LevelBonus),mechanic),
            basePointsPerTile:(int)d["basePerTile"],lightningLines:mechanic=="Lightning",bossTargetMultiplier:(int?)d["bossTargetMultiplier"]??1);
        return result;
    }
    private static T Parse<T>(JObject o,string key) => (T)Enum.Parse(typeof(T),(string)o[key]);
    private static PowerTags ParseTags(string text) => (PowerTags)Enum.Parse(typeof(PowerTags),text);
    public static JObject Level(int number) => Document?["levels"]?.Cast<JObject>().FirstOrDefault(l => (int?)l["number"] == number);
}

/// <summary>Structured editor powers use the same condition/reward rules as the browser sandbox.</summary>
public sealed class SharedRulePower : PowerUp
{
    private readonly JObject definition;
    private int progress,earned,sequenceIndex;
    public override bool HasConditionalTrigger => (string)definition["trigger"]!="always" || AnimalCondition.HasValue || Permanent && (int?)definition["threshold"]>1;
    public override PowerTriggerDisplay DisplayWhenTriggered => PowerTriggerFeedback.Parse((string)definition["displayWhenTriggered"]);
    public bool Permanent => (bool?)definition["permanent"] == true;
    public int EarnedActivations => earned;
    public ItemType? AnimalCondition => Enum.TryParse<ItemType>((string)definition["animal"],out var animal) ? animal : null;
    private float reward;
    public string DefinitionId => (string)definition["id"];
    public SharedRulePower(JObject d) { definition=(JObject)d.DeepClone(); Name=(string)d["name"]; PowerUpDescription=(string)d["description"]; }
    public override bool OpeningOnly => (bool?)definition["openingOnly"] == true;
    public override PowerGoals SupportedGoals => (PowerGoals)Enum.Parse(typeof(PowerGoals),(string)definition["goals"]);
    public override PowerTags Tags => PowerTags.Scoring | (OpeningOnly?PowerTags.Starter:0) | ((bool?)definition["permanent"]==true?PowerTags.Growth:0) |
        ((string)definition["animal"]!="Any"?PowerTags.Animal:0);
    public override PowerStatEffects StatEffects => (string)definition["reward"]=="base"?PowerStatEffects.Base:PowerStatEffects.Multiplier;
    public override string BaseCondition => GetCurrentPowerDescription();
    public override string MultiplierCondition => GetCurrentPowerDescription();
    public override float FlatBasePerTile => (string)definition["reward"]=="base"&&(string)definition["unit"]=="tile"?earned*(float)definition["value"]:0;
    public override float FlatMultiplierBonus => (string)definition["reward"]=="multi"&&(string)definition["unit"]=="match"?earned*(float)definition["value"]:0;
    public override bool SupportsItems(IList<ItemType> items) => (string)definition["animal"]=="Any"||items.Any(t=>t.ToString()==(string)definition["animal"]);
    public override bool CanUseWithMatchRules(IReadOnlyList<MatchRule> rules) => (string)definition["trigger"]!="exact"||CanMatchExactly(rules,(int)definition["length"]);
    private void Evaluate(List<Tile> tiles)
    {
        bool animal=(string)definition["animal"]=="Any"||tiles.Any(t=>t.item.ItemType.ToString()==(string)definition["animal"]);
        var move=MatchInfo as PlayerMoveInfo; string trigger=(string)definition["trigger"];
        bool qualifies=animal && (trigger switch {
            "exact"=>tiles.Count==(int)definition["length"],"minimum"=>tiles.Count>=(int)definition["length"],
            "cascade"=>MatchInfo?.wasUserInitiatedMatch()==false,"initial"=>MatchInfo?.wasUserInitiatedMatch()!=false,
            "edge"=>tiles.Any(t=>t.x==0||t.y==0||t.x==(move?.BoardWidth??7)-1||t.y==(move?.BoardHeight??7)-1),
            "corner"=>tiles.Any(t=>(t.x==0||t.x==(move?.BoardWidth??7)-1)&&(t.y==0||t.y==(move?.BoardHeight??7)-1)),
            "square"=>TacticalStarter.CountShapes(tiles,false)>0,_=>true });
        if(trigger=="sequence") {
            var steps=((string)definition["sequence"]).Split(',').Select(int.Parse).ToArray();
            sequenceIndex=animal&&tiles.Count==steps[sequenceIndex]?sequenceIndex+1:animal&&tiles.Count==steps[0]?1:0;
            qualifies=sequenceIndex==steps.Length;if(qualifies)sequenceIndex=0;
        }
        bool permanent=(bool?)definition["permanent"]==true;
        reward=(float)definition["value"]*(permanent?earned:qualifies?1:0);
        if(permanent){if(qualifies)progress++;else if((bool?)definition["consecutive"]==true&&trigger!="sequence")progress=0;
            if(progress>=(int)definition["threshold"]){earned++;progress=0;}}
    }
    private float Units(List<Tile> tiles) => (string)definition["unit"]=="tile"?tiles.Count:1;
    private static int Bounded(float value) => (int)Math.Min(int.MaxValue,Math.Max(0,(double)value));
    public override int AdditiveBaseMod(List<Tile> tiles) { if((string)definition["reward"]!="credits")Evaluate(tiles); return (string)definition["reward"]=="base"?Bounded(reward*Units(tiles)):0; }
    public override float FractionalMultiplierMod(List<Tile> tiles) => (string)definition["reward"]=="multi"?reward*Units(tiles):0;
    public override int CurrentMatchMultiplier(List<Tile> tiles) => (string)definition["reward"]=="factor"?(int)Math.Min(int.MaxValue,Math.Max(1,Math.Pow((float)definition["value"],reward/(float)definition["value"]))):1;
    public int ExtraCredits(List<Tile> tiles) { if((string)definition["reward"]!="credits")return 0; Evaluate(tiles);return Bounded(reward*Units(tiles)); }
    public override void Apply(GameState state) { progress=earned=sequenceIndex=0; }
    public override string GetCurrentPowerDescription() => PowerUpDescription;
    public override PowerUp Clone() => new SharedRulePower(definition);
}
