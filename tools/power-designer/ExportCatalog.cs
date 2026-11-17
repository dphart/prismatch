using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using System.Text.Json;
using System.Security.Cryptography;
using System.Text;

// Run with refresh-catalog.ps1 after compiling the current game assembly.
class ExportCatalog {
 static object Get(object o,string key) => o.GetType().GetProperty(key)?.GetValue(o);
 static IEnumerable<object> Many(object o) => ((IEnumerable)o).Cast<object>();
 static object Call(object o,string method,params object[] args) => o.GetType().GetMethod(method).Invoke(o,args);
 static string Id(object o) => "game-"+Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(o.GetType().Name+":"+Get(o,"Name"))))[..16].ToLowerInvariant();
 static Dictionary<string,int> Parameters(object p) {
  var fields=new[]{"exactMatchNumber","basePointsMulti","basePointsAdded","atLeastXTiles","bonusPoints","requiredTiles","baseMulti","maxTiles","plusMulti","pointsPerTile","BasePoints","multi","NumRequired","BaseMulti","additiveMulti"};
  var supported=new[]{"FourExactlyPower","HighStakesPower","StackedOddsPower","UnderdogPower","BasePowerUp","BaseMultiPowerUp","IconTypeGivesBase","IconTypeGivesMulti","IconTypeStreakGivesBasePointsOrMulti","ConsistencyIsKey"};
  return supported.Contains(p.GetType().Name)?fields.Select(f=>p.GetType().GetField(f,BindingFlags.Instance|BindingFlags.NonPublic)).Where(f=>f!=null&&f.FieldType==typeof(int)).ToDictionary(f=>f.Name,f=>(int)f.GetValue(p)):new();
 }
 static IEnumerable<object> Opening(object level,IEnumerable<object> catalog) {
  var definition=Call(Get(level,"PowerOffers"),"ForRound",1);
  if(definition==null)return Many(Call(level,"GetPowerUpsForRound",1));
  var slots=Many(Get(definition,"Slots")).ToList();
  return catalog.Where(p=>(bool)Call(p,"Supports",Get(level,"Goal"),Get(level,"Mode")) &&
    (bool)Call(p,"CanUseWithMatchRules",Call(level,"GetMatchRules",1)) &&
    slots.Any(slot=>(bool)Call(Get(slot,"Query"),"Matches",Call(p,"OfferTags",Get(level,"Mode")))));
 }
 static int Main() { try { Run(); return 0; } catch(Exception e) { Console.Error.WriteLine(e); return 1; } }
 static void Run() {
  var root=Directory.GetCurrentDirectory();
  AssemblyLoadContext.Default.Resolving += (ctx,name)=> {
   foreach(var dir in new[]{"tmp/identity-check","tmp/ugs-sdk/compiled","Library/ScriptAssemblies","C:/Program Files/Unity/Hub/Editor/6000.6.2f1/Editor/Data/Managed/UnityEngine"}) {
    var p=Path.GetFullPath(Path.Combine(dir,name.Name+".dll")); if(File.Exists(p)) return ctx.LoadFromAssemblyPath(p);
   } return null;
  };
  var asm=AssemblyLoadContext.Default.LoadFromAssemblyPath(root+"/tmp/identity-check/GameAssembly.dll");
  var levels=Many(asm.GetType("LevelLibrary").GetField("AllLevels").GetValue(null)).ToList();
  var powers=Many(asm.GetType("PowerOfferBuilder").GetMethod("Catalog").Invoke(null,null))
    .Concat(levels.SelectMany(l=>Many(Call(l,"GetPowerUpsForRound",1)))).GroupBy(Id).Select(g=>g.First()).ToList();
  var powerRows=powers.Select(p=>new { id=Id(p),name=Get(p,"Name"),source="game",type=p.GetType().Name,nativeKey=p.GetType().Name+":"+Get(p,"Name"),parameters=Parameters(p),enabled=true,
   description=Get(p,"PowerUpDescription"),tags=Get(p,"Tags").ToString(),goals=Get(p,"SupportedGoals").ToString(),
   openingOnly=Get(p,"OpeningOnly"),basePerTile=Get(p,"FlatBasePerTile"),multi=Get(p,"FlatMultiplierBonus"),factor=Get(p,"FlatMultiplierFactor") });
  var levelRows=levels.Select(l=> {
   var s=Get(l,"Settings"); var n=(int)Get(l,"GetLevelNumber"); int rounds=(int)Get(l,"GetTotalRounds");
   var boss=Many(Call(Call(l,"GetBossPowersMap"),"GetBossPowersForRound",rounds)).LastOrDefault();
   return new { id="level-"+n,name=Get(l,"Name"),source="game",width=Get(s,"Width"),height=Get(s,"Height"),
    rounds,target=Get(s,"Target"),growth=Get(s,"GrowthPercent"),seconds=Get(s,"Seconds"),moves=Get(s,"Moves"),
    mode=Get(s,"Mode").ToString(),goal=Get(s,"Goal").ToString(),gravity=Get(s,"Replacement").ToString(),minimum=Get(s,"MinimumMatchLength"),
    basePerTile=Get(l,"BasePointsPerTile"),habitat=Get(l,"Habitat")?.ToString()??"Neutral",
    mechanic=(bool)Get(l,"LightningLines")?"Lightning":Get(l,"Bonus").ToString(),
    number=n,mechanicNotes=Get(l,"Description"),bossStyle=boss==null?"None":Get(boss,"Style")?.ToString()??"Coach",bossName=boss==null?"None":Get(boss,"Name"),bossRule=boss==null?"":Call(boss,"GetCurrentPowerDescription"),
    starters=Opening(l,powers).Select(Id).ToArray(),rewards=new string[0],
    draftNotes="Shared game catalog; inspect roundDrafts for exact slot queries.",
    roundDrafts=Enumerable.Range(1,rounds).Select(r=>new { round=r,definition=Call(Get(l,"PowerOffers"),"ForRound",r) }).Select(r=>new {
     round=r.round,label=r.definition==null?"Opening / ordinary pool":Get(r.definition,"Label"),
     slots=r.definition==null?Array.Empty<object>():Many(Get(r.definition,"Slots")).Select(slot=>(object)new { count=Get(slot,"Count"),query=Get(slot,"Query").ToString() }).ToArray() }).ToArray()
   };
  }).ToArray();
  var bossType=asm.GetType("BossStyle");var goalType=asm.GetType("GoalType");
  var bosses=Enum.GetValues(bossType).Cast<object>().Select(style=> {var boss=Activator.CreateInstance(asm.GetType("ChallengeBoss"),style,Enum.Parse(goalType,"Score"));return new { style=style.ToString(),name=Get(boss,"Name"),description=Get(boss,"PowerUpDescription") };}).ToArray();
  var data=new { schema="prismatch-shared-content",version=1,exportedAt=DateTimeOffset.UtcNow.ToString("O"),powers=powerRows,levels=levelRows,bosses };
  Directory.CreateDirectory("outputs/shared-content-review");
  File.WriteAllText("outputs/shared-content-review/code-catalog.json",JsonSerializer.Serialize(data,new JsonSerializerOptions{WriteIndented=true})+"\n");
  Console.WriteLine($"Exported {powers.Count} native candidates for review. Shared content was not overwritten.");
 }
}
