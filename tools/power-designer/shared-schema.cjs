'use strict';
const M=require('./model.js');
const R=require('./round-offers.js');
const render=p=>Object.entries(p.parameters||{}).reduce((s,[k,v])=>s.replaceAll('{'+k+'}',String(v)),p.descriptionTemplate??p.description??'');
function validate(d,old){
 if(d?.schema!=='prismatch-shared-content'||d.version!==1||!Array.isArray(d.powers)||!Array.isArray(d.levels)||d.powers.length>700||d.levels.length>200)throw Error('Invalid shared content document.');
 if(JSON.stringify(d.bosses)!==JSON.stringify(old.bosses))throw Error('Boss handlers require a code change.');
 const ids=new Set(),native=new Map(old.powers.filter(p=>p.source==='game').map(p=>[p.id,p]));
 for(const p of d.powers){
  if(p.tier!==undefined&&!['Basic','Rare','Legendary'].includes(p.tier))throw Error('Unknown power tier.');
  if(p.displayWhenTriggered!==undefined&&!['none','icon','tiles','iconAndTiles'].includes(p.displayWhenTriggered))throw Error('Unknown trigger display.');
  if(typeof p.id!=='string'||ids.has(p.id)||!p.name?.trim()||p.name.length>100)throw Error('Power IDs must be unique; every power needs a name.');ids.add(p.id);
  if(native.has(p.id)||p.source==='variant'){
   const base=p.source==='variant'?d.powers.find(q=>q.id===p.basePowerId&&q.source==='game'):null;
   if(p.source==='variant'&&(!base||!d.levels.some(l=>l.id===p.levelId)||p.nativeKey!==undefined))throw Error('A level copy needs a valid base power and level.');
   const existing=old.powers.find(q=>q.id===p.id);
   if(existing&&p.source==='variant'&&(existing.source!=='variant'||existing.basePowerId!==p.basePowerId||existing.levelId!==p.levelId))throw Error('A level copy cannot change its base or level.');
   const before=native.get(p.id)||(existing?.source==='variant'?existing:{...base,source:'variant',nativeKey:undefined});
   for(const key of ['source','type','nativeKey','tags','goals','openingOnly','engine','descriptionTemplate','blockDescriptionTemplate','pointDescriptionTemplate','goalReview'])if(JSON.stringify(p[key])!==JSON.stringify(before[key]))throw Error('Native power behavior requires code: '+key);
   if(JSON.stringify(Object.keys(p.parameters||{}).sort())!==JSON.stringify(Object.keys(before.parameters||{}).sort()))throw Error('Unsupported native parameter.');
   for(const [k,v] of Object.entries(p.parameters||{})){const size=['exactMatchNumber','atLeastXTiles','requiredTiles','maxTiles','minimumMatch'].includes(k);if(!Number.isInteger(v)||v<(size?3:k==='NumRequired'?1:0)||v>(size?10:10000))throw Error('Invalid parameter: '+k);
    if(['MatchFoundationPower','BlockMatchFoundationPower'].includes(p.type)&&k==='pointsPerTile'&&v<1)throw Error('Foundations need positive credit or points.');
    if(p.type==='IconTypeStreakGivesBasePointsOrMulti'&&['BasePoints','BaseMulti'].includes(k)&&(v===0)!==(before.parameters[k]===0))throw Error('Keep the growth reward type; change its amount or create a new structured power.');
   }
   if(typeof p.description!=='string'||p.description.length>4000)throw Error('Invalid power wording.');
   p.description=render(p);
  }else{
   if(p.source!=='custom')throw Error('New native handlers must be registered in code and content together.');
   Object.assign(p,M.validatePower(p));p.description=M.description(p);p.engine='structured';
  }
  if(p.levelId&&(!d.levels.some(l=>l.id===p.levelId)||!d.powers.some(q=>q.id===p.basePowerId&&!q.levelId)))throw Error('A level copy needs a valid base power and level.');
  if(p.enabled!==undefined&&typeof p.enabled!=='boolean')throw Error('Enabled must be true or false.');
  p.qualities=[...M.qualities(p)].sort();
 }
 for(const id of native.keys())if(!ids.has(id))throw Error('Disable native powers instead of deleting them.');
 if(new Set(d.powers.map(p=>p.name.trim().toLowerCase())).size!==d.powers.length)throw Error('Power names must be unique so drafts can track your picks.');
 d.offerFilterKeys=R.filterKeys(d.powers);
 const levelIds=new Set();
 for(let i=0;i<d.levels.length;i++){
  const l=d.levels[i],before=old.levels.find(x=>x.id===l.id);
  if(levelIds.has(l.id)||l.number!==i+1)throw Error('Level IDs must be unique and numbers consecutive.');levelIds.add(l.id);
  if(before&&before.number!==l.number)throw Error('Existing level numbers cannot change.');
  if(l.powerOverrides!==undefined){
   if(!l.powerOverrides||Array.isArray(l.powerOverrides)||typeof l.powerOverrides!=='object')throw Error('Invalid level power copies.');
   for(const [baseId,copyId]of Object.entries(l.powerOverrides)){
    const base=d.powers.find(p=>p.id===baseId),copy=d.powers.find(p=>p.id===copyId);
    if(!base||base.levelId||!copy||copy.levelId!==l.id||copy.basePowerId!==baseId)throw Error('Power copies must belong to their level and original power.');
   }
  }
  if(before&&JSON.stringify(l.roundDrafts)!==JSON.stringify(before.roundDrafts))throw Error('Special draft schedules require code review. Use the reward pool to override them.');
  M.validateLevel({...l,id:'custom-level-validation'},ids);
  if(l.roundOffers!==undefined)l.roundOffers=R.validate(l.roundOffers,l.rounds,d.powers);
  for(const o of l.roundOffers||[])for(const s of o.slots)if(s.mode==='powers')for(const id of s.powerIds){const p=d.powers.find(p=>p.id===id);if(l.goal==='Score'?p.goals==='Blocks':p.goals==='Points')throw Error(p.name+' does not support this round goal.');}
  for(const id of [...l.starters,...l.rewards,...(l.roundOffers||[]).flatMap(o=>o.slots.flatMap(s=>s.powerIds||[]))]){
   const p=d.powers.find(p=>p.id===id);if(p?.levelId&&(p.levelId!==l.id||l.powerOverrides?.[p.basePowerId]!==p.id))throw Error('A level-specific power must be the active copy for this level.');
  }
  if(!Number.isInteger(l.basePerTile)||l.mechanic==='Custom')throw Error('Choose an implemented mechanic and whole base points.');
  const bossError=M.bossCompatibility(l.bossStyle,l);if(bossError)throw Error(bossError);
  const boss=d.bosses.find(b=>b.style===l.bossStyle);
  if(l.bossStyle!=='None'&&!boss)throw Error('Choose an implemented boss.');
  l.bossName=boss?.name||'None';l.bossRule=boss?M.bossDescription(boss,l):'';
  for(const [key,pool] of [['starters',l.starters],['rewards',l.rewards]])for(const id of pool){
   const p=d.powers.find(p=>p.id===id);
   if(p.enabled===false)throw Error('A disabled power is selected in '+l.name);
   if(key==='rewards'&&p.openingOnly)throw Error('Starting powers cannot be later rewards.');
   if(l.goal==='Score'?p.goals==='Blocks':p.goals==='Points')throw Error(p.name+' does not support this level goal.');
  }
 }
 for(const l of old.levels)if(!levelIds.has(l.id))throw Error('Existing levels cannot be deleted here.');
 return d;
}
module.exports={validate,render};
