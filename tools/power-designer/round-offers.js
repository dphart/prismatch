(function(root){
 'use strict';
 const M=typeof module!=='undefined'&&module.exports?require('./model.js'):root.DesignerModel;
 function filterKeys(powers){return [...new Set([...M.filterGroups.flatMap(g=>g.items.map(([k])=>k)),'game','custom',...powers.flatMap(p=>[...M.qualities(p)].filter(k=>k.startsWith('tag:')))])];}
 function groups(powers,savedKeys=[]){
  const normal=new Set(M.filterGroups.flatMap(g=>g.items.map(([k])=>k)));
  const legacy=[...new Set(savedKeys)].filter(k=>!normal.has(k)&&filterKeys(powers).includes(k));
  return [...M.filterGroups,...(legacy.length?[{name:'Saved draft filters',items:legacy.map(k=>[k,k.replace(/^tag:/,'').replace(/([a-z])([A-Z])/g,'$1 $2')+' (existing rule)'])}]:[])];
 }
 function validate(offers,rounds,powers){
  if(offers===undefined)return [];
  if(!Array.isArray(offers)||offers.length>30)throw Error('Invalid round offers.');
  const seen=new Set(),ids=new Set(powers.map(p=>p.id)),keys=new Set(filterKeys(powers));
  return offers.map(o=>{
   if(!Number.isInteger(o.round)||o.round<1||o.round>rounds||seen.has(o.round))throw Error('Round offers need unique round numbers within this level.');seen.add(o.round);
   if(typeof o.label!=='string'||!o.label.trim()||o.label.length>100)throw Error('Give each custom round offer a short title.');
   if(!Array.isArray(o.slots)||!o.slots.length||o.slots.length>3)throw Error('Use one to three power groups in each round.');
   const slots=o.slots.map(s=>{
    if(!Number.isInteger(s.count)||s.count<1||s.count>3||!['powers','filters'].includes(s.mode)||!['PreferNew','NewOnly'].includes(s.repeats||'PreferNew'))throw Error('Invalid power group.');
    const q={count:s.count,mode:s.mode,repeats:s.repeats||'PreferNew'};
    if(s.mode==='powers'){
     if(!Array.isArray(s.powerIds)||!s.powerIds.length||s.powerIds.length>500||s.powerIds.some(id=>!ids.has(id)))throw Error('Choose at least one saved power in each picked-power group.');
     q.powerIds=[...new Set(s.powerIds)];
     for(const id of q.powerIds){const p=powers.find(p=>p.id===id);if(p.enabled===false)throw Error(p.name+' is disabled.');if(o.round>1&&p.openingOnly)throw Error('Starting powers can only be offered in round 1.');}
    }else{
     if(!['all','any'].includes(s.match)||!s.filters||Array.isArray(s.filters)||typeof s.filters!=='object'||Object.entries(s.filters).some(([k,v])=>!keys.has(k)||!['include','exclude'].includes(v)))throw Error('Invalid include/exclude filters.');
     q.match=s.match;q.filters={...s.filters};
    }
    return q;
   });
   if(slots.reduce((n,s)=>n+s.count,0)>3)throw Error('A round can offer at most three powers.');
   return {round:o.round,label:o.label.trim(),slots};
  }).sort((a,b)=>a.round-b.round);
 }
 function matches(slot,power){return slot.mode==='powers'?(slot.powerIds.includes(power.id)||slot.powerIds.includes(power.basePowerId)):M.matchesFilters(power,slot.filters,slot.match);}
 function inherited(level,round,powers){
  const ids=round===1?level.starters:level.rewards;
  if(ids?.length)return {round,label:round===1?'STARTING POWERS':'ROUND POWERS',slots:[{mode:'powers',count:Math.min(3,ids.length),powerIds:[...ids],repeats:'PreferNew'}]};
  const draft=level.roundDrafts?.find(d=>d.round===round&&d.slots?.length);
  if(!draft)return null;
  const tags=value=>(value||'').split(',').map(t=>t.trim()).filter(t=>t&&t!=='None');
  return {round,label:draft.label,slots:draft.slots.map(s=>{
   const query=Object.fromEntries(s.query.split(';').map(part=>part.trim().split('=')));
   const all=tags(query.all),any=tags(query.any),none=tags(query.none);
   const powerIds=powers.filter(p=>{const own=new Set(tags(p.tags));
    if(level.goal==='BlocksCleared'&&p.blockDescriptionTemplate){const q=M.qualities(p);if(q.has('credits'))own.add('GoalCredit');if(q.has('clearing'))own.add('Clearing');}
    return all.every(t=>own.has(t))&&(!any.length||any.some(t=>own.has(t)))&&!none.some(t=>own.has(t));}).map(p=>p.id);
   return {mode:'powers',count:s.count,powerIds,repeats:'PreferNew'};
  })};
 }
 function candidates(slot,round,level,powers){
  const mapped=[...new Map(powers.filter(p=>!p.levelId||p.levelId===level.id&&level.powerOverrides?.[p.basePowerId]===p.id).map(p=>{
   const replacement=level.powerOverrides?.[p.id];const resolved=replacement?powers.find(q=>q.id===replacement):p;return [resolved?.id,resolved];
  })).values()].filter(Boolean);
  return mapped.filter(p=>p.enabled!==false&&(round===1||!p.openingOnly)&&(level.goal==='BlocksCleared'?p.goals!=='Points':p.goals!=='Blocks')&&matches(slot,p));
 }
 function preview(offer,level,powers){
  const lists=offer.slots.flatMap(s=>Array.from({length:s.count},()=>candidates(s,offer.round,level,powers))),owners=new Map();
  function assign(i,visited){for(const p of lists[i]){if(visited.has(p.id))continue;visited.add(p.id);if(!owners.has(p.id)||assign(owners.get(p.id),visited)){owners.set(p.id,i);return true;}}return false;}
  let filled=0;for(const i of lists.map((_,i)=>i).sort((a,b)=>lists[a].length-lists[b].length))if(assign(i,new Set()))filled++;
  return {requested:lists.length,filled,unique:new Set(lists.flat().map(p=>p.id)).size};
 }
 function overview(level,powers){
  const rounds=Array.from({length:level.rounds},(_,i)=>{
   const round=i+1,custom=level.roundOffers?.find(o=>o.round===round);
   const offer=custom||inherited(level,round,powers)||{round,label:'Standard catalog (approximate)',slots:[{mode:'filters',match:'all',filters:{},count:3}]};
   const slots=offer.slots.map(slot=>({...slot,powers:candidates(slot,round,level,powers)}));
   const pool=[...new Map(slots.flatMap(s=>s.powers).map(p=>[p.id,p])).values()];
   return {round,label:offer.label,approximate:!custom&&!inherited(level,round,powers),slots,powers:pool,
    signature:JSON.stringify(slots.map(s=>({count:s.count,repeats:s.repeats||'PreferNew',ids:s.powers.map(p=>p.id).sort()})))};
  });
  const union=[...new Map(rounds.flatMap(r=>r.powers).map(p=>[p.id,p])).values()];
  const families=M.condensePowers(union).map(f=>({...f,rounds:rounds.filter(r=>r.powers.some(p=>f.members.some(m=>m.id===p.id))).map(r=>r.round)}));
  const repeatedPools=[...new Set(rounds.map(r=>r.signature))].map(signature=>rounds.filter(r=>r.signature===signature).map(r=>r.round)).filter(r=>r.length>1);
  const singleFamilySlots=rounds.flatMap(r=>r.slots.flatMap((s,i)=>s.powers.length&&M.condensePowers(s.powers).length===1?[{round:r.round,group:i+1,count:s.count,name:M.condensePowers(s.powers)[0].name,copies:s.powers.length}]:[]));
  return {rounds,families,powerCount:union.length,repeatedPools,singleFamilySlots};
 }
 const api={overview,groups,filterKeys,validate,matches,candidates,preview,inherited};if(typeof module!=='undefined'&&module.exports)module.exports=api;else root.RoundOffers=api;
})(typeof window!=='undefined'?window:globalThis);
