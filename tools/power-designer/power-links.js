(function(root){
 'use strict';
 function makeCopy(document,powerId,levelId,id){
  const power=document.powers.find(p=>p.id===powerId),level=document.levels.find(l=>l.id===levelId);
  if(!power||!level)throw Error('Save the level before making a level-specific power.');
  const copy=JSON.parse(JSON.stringify(power));
  copy.id=id;copy.basePowerId=power.basePowerId||power.id;copy.levelId=levelId;
  const stem=(power.name.replace(/ · Level \d+ copy(?: \d+)?$/,'')).slice(0,65)+' · Level '+level.number+' copy';
  copy.name=stem;let suffix=2;while(document.powers.some(p=>p.name.toLowerCase()===copy.name.toLowerCase()))copy.name=stem+' '+suffix++;
  if(power.source!=='custom'){copy.source='variant';delete copy.nativeKey;}
  return copy;
 }
 function saveCopy(document,levelId,power){
  const level=document.levels.find(l=>l.id===levelId);
  if(!level||power.levelId!==levelId)throw Error('This power copy belongs to a different level.');
  if(document.powers.some(p=>p.id===power.id))throw Error('Power ID already exists.');
  const previous=level.powerOverrides?.[power.basePowerId];
  document.powers.push(power);replaceReferences(level,previous||power.basePowerId,power.id);level.powerOverrides={...level.powerOverrides,[power.basePowerId]:power.id};
 }
 function replaceReferences(level,before,after){
  if(!before)return;
  for(const key of ['starters','rewards'])if(level[key])level[key]=level[key].map(id=>id===before?after:id);
  for(const offer of level.roundOffers||[])for(const slot of offer.slots)if(slot.powerIds)slot.powerIds=slot.powerIds.map(id=>id===before?after:id);
 }
 const api={makeCopy,saveCopy,replaceReferences};if(typeof module!=='undefined'&&module.exports)module.exports=api;else root.LevelPowerLinks=api;
})(typeof window!=='undefined'?window:globalThis);
