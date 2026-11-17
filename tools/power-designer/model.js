(function (root) {
 'use strict';
 const animals=['Any','Duck','Bear','Chicken','Giraffe','Hippo','Sloth','Walrus','Elephant','Panda','Moose'];
 const triggers=['always','animal','exact','minimum','edge','corner','square','cascade','initial','sequence'];
 const rewards=['base','multi','factor','credits'];
 const mechanics=['LightningRods','AnimalSpotlight','RollingDarkness','Blackout','WildRelease','GoldenRescue','MigrationSeason','CrackTheVault','SteppingStones','AnimalRescue','Crosswinds','None','Lightning','ChainReaction','GardenPlots','AlternatingCurrent','LeastMatched','BorderPatrol','TreasureTrail','ChangingTides','Cascade','LargeGroup','Square','Custom'];
 const filterGroups=[
  {name:'Where it appears',items:[['opening','Opening only'],['points','Points mode'],['blocks','Block mode']]},
  {name:'What it gives',items:[['base','Base points'],['multi','Multiplier'],['factor','Score boost'],['credits','Block credits'],['clearing','Extra clears'],['transform','Animal conversions'],['growth','Permanent growth'],['budget','Extra time or moves'],['shuffle','Shuffle effects']]},
  {name:'How it works',items:[['animal','Animal powers'],['exact','Exact match size'],['minimum','Minimum match size'],['small','Small matches'],['square','Squares'],['plus','Plus shapes'],['line','Straight lines'],['edge','Edges'],['corner','Corners'],['cascade','Cascades'],['sequence','Streaks, sets or sequences'],['progress','Match counters'],['matching','Match rules'],['roster','Add or remove animals'],['spawn','Animal spawn rates'],['tradeoff','Tradeoffs']]},
  {name:'Which animal',items:animals.slice(1).map(a=>['animal:'+a,a])}
 ];
 function qualities(p){
  const result = new Set([p.source === 'custom' ? 'custom' : 'game']);
  const tags = new Set(String(p.tags || '').split(',').map(tag => tag.trim()));
  if(p.openingOnly) result.add('opening');
  if(p.goals !== 'Blocks') result.add('points');
  if(p.goals !== 'Points') result.add('blocks');
  if(p.source === 'custom'){
   tags.add('Scoring');
   if(p.openingOnly) tags.add('Starter');
   if(p.permanent) tags.add('Growth');
   if(p.animal !== 'Any') tags.add('Animal');
   if(p.reward === 'base') tags.add('BasePoints');
   if(p.reward === 'multi' || p.reward === 'factor') tags.add('Multiplier');
   if(p.reward === 'credits') tags.add('GoalCredit');
  }
  for(const tag of tags) if(tag && tag !== 'None') result.add('tag:' + tag);
  if(p.source === 'custom'){
   result.add(p.reward);
   if(p.permanent) result.add('growth');
   if(p.permanent || p.trigger === 'sequence') result.add('progress');
   if(p.animal !== 'Any'){ result.add('animal'); result.add('animal:' + p.animal); }
   if(['exact','minimum','square','edge','corner','cascade','sequence'].includes(p.trigger)) result.add(p.trigger);
   return result;
  }
  let text = clean(p.pointDescriptionTemplate || p.descriptionTemplate || p.description).toLowerCase();
  let blockText = p.goals === 'Points' ? '' : clean(p.blockDescriptionTemplate || p.description).toLowerCase();
  for(const [key, value] of Object.entries(p.parameters || {})){
   text = text.replaceAll('{' + key.toLowerCase() + '}', String(value));
   blockText = blockText.replaceAll('{' + key.toLowerCase() + '}', String(value));
  }
  for(const [key, tag] of [['base','BasePoints'],['multi','Multiplier'],['growth','Growth'],['animal','Animal'],
   ['square','Square'],['edge','Edge'],['cascade','Cascade'],['matching','Matching'],['budget','Budget'],
   ['shuffle','Shuffle'],['transform','Transform'],['tradeoff','Tradeoff']]){
   if(tags.has(tag)) result.add(key);
  }
  if(tags.has('Roster') && p.type !== 'AnimalSpawnPower') result.add('roster');
  if(p.type === 'AnimalSpawnPower') result.add('spawn');
  if(/(?:same|different|every) animal/.test(text)) result.add('animal');
  if(/base\b/.test(text)) result.add('base');
  if(/\bmulti\b|multiplier/.test(text)) result.add('multi');
  const amount = '(?:[0-9]+(?:\\.[0-9]+)?|\\{[a-z0-9_]+\\})';
  const creditReward = new RegExp('\\b(?:gain|add|earn|give)[a-z]*\\s+' + amount + '[^.!]*block credits?');
  if(tags.has('GoalCredit') || p.type !== 'AnimalPairPower' && creditReward.test(blockText)){
   result.add('credits'); result.add('tag:GoalCredit');
  }
  const clearReward = new RegExp('\\b(?:gain|add|earn|give)[a-z]*\\s+' + amount + '\\s+(?:permanent\\s+)?extra clears?');
  const clearTiles = new RegExp('\\bclear[a-z]*\\s+(?:up to\\s+)?' + amount + '\\s+(?:extra|neighboring)\\s+tiles');
  if(clearReward.test(blockText) || clearTiles.test(blockText) || clearTiles.test(text)){
   result.add('clearing'); result.add('tag:Clearing');
  }
  if(/\bx[0-9]|\u00d7\s*[0-9]|\b[0-9]+x points|double(?! (?:their )?base)|gain \d+% extra points/.test(text)) result.add('factor');
  if(/permanent|stacking multi/.test(text)) result.add('growth');
  if(/exactly|exact-four/.test(text)) result.add('exact');
  if(tags.has('LargeMatch') || /\b[4-9]\+ (?:tiles|animals|matches)|\bmatch(?:es)?(?: of)? [4-9] or more/.test(text)) result.add('minimum');
  if(/\b[0-9]+ or fewer/.test(text)) result.add('small');
  if(/2x2|2\u00d72|square/.test(text)) result.add('square');
  if(/plus shape/.test(text)) result.add('plus');
  if(/horizontal line|vertical line|straight rows|straight columns/.test(text)) result.add('line');
  if(/board edge|outer edge|edge match|touch.*edge/.test(text)) result.add('edge');
  if(/match[^.!]*corner|touch[^.!]*corner/.test(text)) result.add('corner');
  const cascadeText = text.replace(/(?:falling matches|cascades) (?:do not|don't|never)[^.!]*/g, '');
  if(/cascade|falling tiles|falling matches/.test(cascadeText)) result.add('cascade');
  if(/streak|in a row|previous match|previous animal|same size as your last|different animal than your last|switch animals?|match every animal/.test(text)) result.add('sequence');
  if(p.type === 'AnimalMasteryPower' || p.type === 'BudgetPower' || /every \d+|make \d+ (?:matching )?(?:matches|moves)|match every animal|same size as your last/.test(text)) result.add('progress');
  for(const animal of animals.slice(1)){
   if(new RegExp('\\b' + animal + '\\b', 'i').test(text)){
    result.add('animal:' + animal); result.add('animal');
   }
  }
  return result;
 }
 function matchesFilters(p,selection,mode='all'){
  const q=qualities(p),include=Object.entries(selection).filter(([,v])=>v==='include').map(([k])=>k),exclude=Object.entries(selection).filter(([,v])=>v==='exclude').map(([k])=>k);
  return !exclude.some(key=>q.has(key))&&(!include.length||(mode==='any'?include.some(key=>q.has(key)):include.every(key=>q.has(key))));
 }
 const clean = s => String(s??'').replace(/<[^>]*>/g,'').trim();
 function number(v,name,min,max){if(typeof v!=='number'||!Number.isFinite(v)||v<min||v>max)throw Error(`${name} must be between ${min} and ${max}.`);return v;}
 function integer(v,name,min,max){number(v,name,min,max);if(!Number.isInteger(v))throw Error(`${name} must be a whole number.`);return v;}
 function text(v,name,max=4000){if(typeof v!=='string'||v.length>max)throw Error(`${name} must be text of at most ${max} characters.`);return v;}
 function one(v,choices,name){if(!choices.includes(v))throw Error(`Unknown ${name}: ${v}`);return v;}
 function ids(v){if(!Array.isArray(v)||v.length>500)throw Error('Invalid power pool.');return [...new Set(v.map(x=>text(x,'Power ID',150)))];}
 function validatePower(p){
  if(!p||typeof p!=='object')throw Error('Invalid power.');
  const q={id:text(p.id,'ID',150),source:'custom',tier:one(p.tier??'Basic',['Basic','Rare','Legendary'],'power tier'),name:text(p.name,'Name',100).trim(),notes:text(p.notes??'','Notes'),openingOnly:!!p.openingOnly,
   goals:one(p.goals,['Points','Blocks','Both'],'goal'),trigger:one(p.trigger,triggers,'condition'),animal:one(p.animal,animals,'animal'),length:integer(p.length,'Match length',3,10),
   sequence:text(p.sequence??'4,5,6','Sequence',80),reward:one(p.reward,rewards,'reward'),value:number(p.value,'Reward',p.reward==='factor'?1:0,10000),
   unit:one(p.unit,['match','tile'],'reward unit'),permanent:!!p.permanent,threshold:integer(p.threshold,'Activations',1,100),consecutive:!!p.consecutive};
  if(p.displayWhenTriggered!==undefined&&p.displayWhenTriggered!=='')q.displayWhenTriggered=one(p.displayWhenTriggered,['none','icon','tiles','iconAndTiles'],'trigger display');
  if(!q.id.startsWith('custom-power-')||!q.name)throw Error('A custom power needs an ID and a name.');
  if(q.reward!=='multi'&&!Number.isInteger(q.value))throw Error('Base points, block credits and score factors need whole numbers.');
  if(q.reward==='base'&&q.goals!=='Points'||q.reward==='multi'&&q.goals!=='Points'||q.reward==='factor'&&q.goals!=='Points'||q.reward==='credits'&&q.goals!=='Blocks')throw Error('Use Points for scoring bonuses and Blocks for credit bonuses.');
  if(q.trigger==='animal'&&q.animal==='Any')throw Error('Choose an animal for this condition.');
  if(q.trigger==='sequence'&&!/^\s*(?:[3-9]|10)(?:\s*,\s*(?:[3-9]|10))*\s*$/.test(q.sequence))throw Error('Sequence must contain match lengths 3–10 separated by commas.');
  if(q.permanent&&q.trigger==='always'&&!q.openingOnly)throw Error('Unconditional permanent rewards are reserved for openings. Choose a condition.');
  return q;
 }
 function validateLevel(l,powerIds){
  if(!l||typeof l!=='object')throw Error('Invalid level.');
  const q={id:text(l.id,'ID',150),source:'custom',name:text(l.name,'Name',100).trim(),width:integer(l.width,'Width',3,10),height:integer(l.height,'Height',3,10),
   bossTargetMultiplier:integer(l.bossTargetMultiplier??1,'Boss target multiplier',1,10),rounds:integer(l.rounds,'Rounds',1,30),target:integer(l.target,'Target',1,1000000),growth:integer(l.growth,'Growth',0,1000),seconds:integer(l.seconds,'Seconds',10,3600),moves:integer(l.moves,'Moves',1,999),
   minimum:integer(l.minimum,'Minimum match',3,10),basePerTile:number(l.basePerTile,'Level base',0,100),mode:one(l.mode,['Timed','LimitedMoves'],'budget'),goal:one(l.goal,['Score','BlocksCleared'],'goal'),
   gravity:one(l.gravity,['InPlace','South','North','East','West'],'gravity'),habitat:one(l.habitat,['Neutral','Menagerie','Shore','Garden','Rapids','Giants'],'habitat'),mechanic:one(l.mechanic,mechanics,'mechanic'),
   mechanicNotes:text(l.mechanicNotes??'','Mechanic'),bossName:text(l.bossName??'','Boss name',100),bossRule:text(l.bossRule??'','Boss rule'),starters:ids(l.starters),rewards:ids(l.rewards),draftNotes:text(l.draftNotes??'','Draft notes')};
  if(!q.id.startsWith('custom-level-')||!q.name)throw Error('A custom level needs an ID and name.');
  if(q.minimum>Math.max(q.width,q.height))throw Error('Match minimum is larger than the board supports.');
  if(powerIds)for(const id of [...q.starters,...q.rewards])if(!powerIds.has(id))throw Error(`Unknown power reference: ${id}`);
  return q;
 }
 function condition(p){
  const a=p.animal!=='Any'?`${p.animal} `:'';
  const names={always:'All matches',animal:`Matches with ${p.animal}`,exact:`Matches that are exactly ${p.length}`,minimum:`Matches of ${p.length} or more`,edge:'Matches touching an edge',corner:'Matches touching a corner',square:'Matches with a 2×2 square',cascade:'Matches made by falling tiles',initial:'Matches you make',sequence:`Match ${p.sequence} in order`};
  return (names[p.trigger]||'Custom condition')+(p.animal!=='Any'&&p.trigger!=='animal'?` containing ${a.trim()}`:'');
 }
 function description(p){
  const unit=p.reward==='base'?'base points':p.reward==='multi'?'multiplier':p.reward==='credits'?'extra blocks':'points';
  const effect=p.reward==='factor'?`${p.value}x points`:`${p.value} ${unit}${p.unit==='tile'?' per tile':''}`;
  if(p.permanent){
   const task=p.trigger==='sequence'?`Complete the ${p.sequence} match sequence ${p.threshold} time${p.threshold===1?'':'s'}`:`Make ${p.threshold} ${condition(p).replace(/^All matches/,'matches').replace(/^Matches/,'matches')}`;
   return `${task}${p.consecutive?' in a row':''} to gain ${effect} for all future matches.`;
  }
  if(p.trigger==='sequence')return `${condition(p)} to gain ${effect}.`;
  return `${condition(p)} ${p.reward==='factor'?'score':'gain'} ${effect}.`;
 }
 function matches(p,m){
  if(p.animal!=='Any'&&p.animal!==m.animal)return false;
  return ({always:true,animal:p.animal===m.animal,exact:m.length===p.length,minimum:m.length>=p.length,edge:m.edge,corner:m.corner,square:m.square,cascade:m.cascade,initial:!m.cascade,sequence:true})[p.trigger]===true;
 }
 function evaluate(p,m,previous={}){
  const state={progress:0,earned:0,sequenceIndex:0,...previous};let qualifies=matches(p,m);
  if(p.trigger==='sequence'){
   const seq=p.sequence.split(',').map(Number);
   const allowed=p.animal==='Any'||p.animal===m.animal;
   if(allowed&&m.length===seq[state.sequenceIndex])state.sequenceIndex++;
   else state.sequenceIndex=allowed&&m.length===seq[0]?1:0;
   qualifies=state.sequenceIndex===seq.length;
   if(qualifies)state.sequenceIndex=0;
  }
  const amount=p.permanent?state.earned:(qualifies?1:0);
  if(p.permanent){
   if(qualifies)state.progress++;else if(p.consecutive&&p.trigger!=='sequence')state.progress=0;
   if(state.progress>=p.threshold){state.earned++;state.progress=0;}
  }
  return {amount,state,qualifies};
 }
 function score(powers,m,states={},basePerTile=4,mode='Score'){
  let base=basePerTile*m.length,multi=1,factor=1,credits=m.length;const next={},log=[];
  for(const p of powers){
   const allowed=mode==='Score'?p.goals!=='Blocks':p.goals!=='Points';
   if(!allowed){next[p.id]=states[p.id]||{progress:0,earned:0,sequenceIndex:0};log.push(`${p.name}: unavailable for this goal`);continue;}
   const r=evaluate(p,m,states[p.id]);next[p.id]=r.state;
   const quantity=p.unit==='tile'?m.length:1;
   if(allowed&&r.amount){
    const amount=p.value*r.amount;
    if(p.reward==='base')base+=amount*quantity;
    if(p.reward==='multi')multi+=amount*quantity;
    if(p.reward==='factor')factor*=Math.pow(p.value,r.amount);
    if(p.reward==='credits')credits+=amount*quantity;
   }
   log.push(`${p.name}: ${r.qualifies?'condition met':'condition not met'}${p.permanent?` · ${r.state.earned} earned, ${r.state.progress}/${p.threshold} progress` : ''}${p.trigger==='sequence'?` · sequence step ${r.state.sequenceIndex}`:''}`);
  }
  const clamp=n=>Math.min(2147483647,Math.max(0,Math.floor(n)));
  return {score:mode==='Score'?clamp(base*multi*factor):clamp(credits),base,multi,factor,states:next,log};
 }
 function targets(l){let n=l.target;return Array.from({length:l.rounds},(_,i)=>{const factor=i===l.rounds-1&&l.bossStyle&&l.bossStyle!=='None'?(l.bossTargetMultiplier??1):1;const value=Math.min(2147483647,Math.ceil(n-1e-8)*factor);n*=1+l.growth/100;return value;});}
 function importWorkspace(raw,catalog){
  if(!raw||raw.schema!=='prismatch-design-workspace'||raw.version!==1)throw Error('This is not a supported Prismatch workspace (version 1).');
  if(!Array.isArray(raw.powers)||!Array.isArray(raw.levels)||raw.powers.length>500||raw.levels.length>200)throw Error('Workspace is too large or invalid.');
  const powers=raw.powers.map(validatePower);const allIds=new Set(catalog.powers.map(p=>p.id));
  for(const p of powers){if(allIds.has(p.id))throw Error('Duplicate power ID.');allIds.add(p.id);}
  const levels=raw.levels.map(l=>validateLevel(l,allIds));const levelIds=new Set();
  for(const l of levels){if(levelIds.has(l.id))throw Error('Duplicate level ID.');levelIds.add(l.id);}
  for(const l of levels)for(const id of l.rewards){const p=powers.find(p=>p.id===id)||catalog.powers.find(p=>p.id===id);if(p.openingOnly)throw Error(`${p.name} is opening-only and cannot be in a reward pool.`);}
  return {powers,levels};
 }
 function mergeSharedEdits(base,edited,latest){
  const same=(a,b)=>JSON.stringify(a)===JSON.stringify(b),next=JSON.parse(JSON.stringify(latest));
  for(const key of ['powers','levels']){
   const old=new Map(base[key].map(x=>[x.id,x])),changes=new Map(edited[key].map(x=>[x.id,x]));
   for(const id of new Set([...old.keys(),...changes.keys()])){
    const before=old.get(id),after=changes.get(id);if(same(before,after))continue;
    const index=next[key].findIndex(x=>x.id===id),remote=index<0?undefined:next[key][index];
    if(!same(remote,before)&&!same(remote,after))throw Error('Someone changed '+(before?.name||after?.name||id)+'. Your edits are still open; resolve the conflict before saving.');
    if(after===undefined){if(index>=0)next[key].splice(index,1);continue;}
    const value=JSON.parse(JSON.stringify(after));
    if(key==='levels'&&!before&&index<0)value.number=next.levels.length+1;
    if(index<0)next[key].push(value);else next[key][index]=value;
   }
  }
  return next;
 }
 function bossCompatibility(style,level){
  if(!style||style==='None')return '';
  const strategies={NightKeeper:['Blackout','Score',null],CageWarden:['GoldenRescue','BlocksCleared','LimitedMoves'],MigrationWarden:['MigrationSeason','Score','LimitedMoves'],VaultWarden:['CrackTheVault','BlocksCleared','LimitedMoves'],StoneKeeper:['SteppingStones','Score',null],RescueKeeper:['AnimalRescue','BlocksCleared','LimitedMoves'],UnstableWinds:['Crosswinds','Score',null]};
  const strategy=strategies[style];
  if(strategy&&(level.mechanic!==strategy[0]||level.goal!==strategy[1]||strategy[2]&&level.mode!==strategy[2]))return 'This boss requires '+strategy[0]+' with '+strategy[1]+(strategy[2]?' and '+strategy[2]:'')+'.';
  if(['CrowdTax','SmallChange','InnerToll','RepeatToll'].includes(style)&&level.mode!=='LimitedMoves')return 'This boss requires limited moves.';
  if(['DuckEmbargo','MostMatched','TreasureCrab'].includes(style)&&level.goal!=='Score')return 'This boss affects points only.';
  if(style==='Overgrowth'&&level.mechanic!=='GardenPlots')return 'Overgrowth requires Garden plots.';
  if(style==='TreasureCrab'&&level.mechanic!=='TreasureTrail')return 'Treasure Crab requires Treasure trail.';
  return '';
 }
 function bossDescription(boss,level){
  return level.goal==='BlocksCleared'&&!bossCompatibility(boss.style,level)?boss.description.replaceAll('points','block credit'):boss.description;
 }
 function condensePowers(powers){
  const animalPattern=new RegExp('\\b('+animals.slice(1).join('|')+')\\b','gi');
  const groups=new Map();
  for(const power of powers){
   const wording=power.source==='custom'?description(power):clean(power.description);
   const found=new Set(([power.name,wording,power.animal].join(' ').match(animalPattern)||[]).map(a=>a.toLowerCase()));
   // Multi-animal pacts are separate mechanics, rather than interchangeable animal copies.
   const normalize=value=>typeof value==='string'?value.replace(animalPattern,'Animal'):
    Array.isArray(value)?value.map(normalize):value&&typeof value==='object'?
     Object.fromEntries(Object.keys(value).sort().filter(k=>k!=='id').map(k=>[k,normalize(value[k])])):value;
   const key=found.size===1?JSON.stringify(normalize(power)):'single:'+power.id;
   if(!groups.has(key))groups.set(key,{power,members:[],name:power.name,description:wording});
   groups.get(key).members.push(power);
  }
  return [...groups.values()].map(group=>group.members.length>1?
   {...group,name:group.name.replace(animalPattern,'Animal').replace(/^Animal Animal Animal$/,'Animal Match Multiplier').replace(/^\+\+Animal\+\+$/,'Animal Match Points'),description:group.description.replace(animalPattern,'Animal')}:group);
 }
 const api={condensePowers,mergeSharedEdits,bossCompatibility,bossDescription,animals,triggers,rewards,mechanics,filterGroups,qualities,matchesFilters,clean,validatePower,validateLevel,condition,description,matches,evaluate,score,targets,importWorkspace};
 if(typeof module!=='undefined'&&module.exports)module.exports=api;else root.DesignerModel=api;
})(typeof window!=='undefined'?window:globalThis);
