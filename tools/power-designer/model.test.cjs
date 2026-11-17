'use strict';
const assert=require('node:assert/strict'),vm=require('node:vm'),fs=require('node:fs'),M=require('./model.js');
const catalog=JSON.parse(fs.readFileSync(__dirname+'/../../Assets/Resources/Content/prismatch.json','utf8'));
const p=(overrides={})=>M.validatePower({id:'custom-power-test',name:'Exact Four',trigger:'exact',animal:'Any',length:4,sequence:'4,5,6',reward:'base',value:3,unit:'tile',permanent:false,threshold:2,consecutive:false,openingOnly:false,goals:'Points',notes:'',...overrides});
const match=(overrides={})=>({animal:'Duck',length:4,cascade:false,edge:false,corner:false,square:false,...overrides});
let tests=0;function test(name,fn){fn();tests++;console.log('PASS '+name);}
test('Catalog contains current levels, latest openings and unique stable IDs',()=>{
 assert(catalog.levels.length>=29);assert(catalog.powers.some(p=>p.name==='Garden Harvest'));assert.equal(new Set(catalog.powers.map(p=>p.id)).size,catalog.powers.length);
 assert.equal(catalog.powers.find(p=>p.name==='Four is Good').description,'Exactly 4 tiles: +5 multiplier.');
 assert(catalog.powers.every(p=>!/base \/ tile|\bmulti\b|: \+|player-created|eligible|adjacent/.test(p.description)));
 for(const l of catalog.levels)for(const id of l.starters)assert(catalog.powers.some(p=>p.id===id));
});

test('New campaign mechanics remain editable and the corner opening pays only at corners',()=>{
 const levels=catalog.levels.slice(8,11);
 assert.deepEqual(levels.map(l=>l.name),['Border Patrol','Treasure Trail','Changing Tides']);
 for(const l of levels){assert(M.mechanics.includes(l.mechanic));assert.equal(M.validateLevel({...l,id:'custom-level-validation'}).mechanic,l.mechanic);assert.equal(l.rounds,6);assert.equal(l.starters.length,0);assert.equal(l.roundOffers.find(o=>o.round===1).slots.length,3);assert(catalog.bosses.some(b=>b.style===l.bossStyle));}
 const corner=catalog.powers.find(p=>p.id==='custom-power-corner-cache');
 assert.equal(M.score([corner],match()).score,16);
 assert.equal(M.score([corner],match({corner:true})).score,64);
});

test('Eight campaign remixes include six block goals and remain editable',()=>{
 const levels=catalog.levels.slice(11,19);
 assert.equal(levels.length,8);
 assert.equal(levels.filter(l=>l.goal==='BlocksCleared').length,6);
 assert.deepEqual(levels.map(l=>l.number),[12,13,14,15,16,17,18,19]);
 assert.equal(new Set(levels.map(l=>l.id)).size,8);
 for(const l of levels){
  const validated=M.validateLevel({...l,id:'custom-level-validation'});
  assert.equal(validated.goal,l.goal);
  assert.equal(validated.mechanic,l.mechanic);
  assert.deepEqual([...new Set([1,...l.roundDrafts.map(r=>r.round)])].sort((a,b)=>a-b),[1,2,3,4,5,6]);
  assert.equal(l.roundDrafts.at(-1).round,6);
  assert(catalog.bosses.some(b=>b.style===l.bossStyle));
 }
});

test('Shuffle levels and new native powers remain editable and use goal-appropriate rewards',()=>{
 const levels=catalog.levels.slice(19,23);
 assert.deepEqual(levels.map(l=>l.number),[20,21,22,23]);
 assert.equal(levels.filter(l=>l.goal==='BlocksCleared').length,3);
 for(const l of levels){
  assert.equal(M.validateLevel({...l,id:'custom-level-validation'}).goal,l.goal);
  assert.deepEqual(l.roundOffers.map(r=>r.round),[1,2,3]);
  for(const offer of l.roundOffers)for(const slot of offer.slots)for(const id of slot.powerIds||[]){
   const power=catalog.powers.find(p=>p.id===id);
   assert(power && power.tags.includes('Shuffle'));
   assert(power.goals==='Both'||power.goals===(l.goal==='Score'?'Points':'Blocks'));
  }
 }
 for(const name of ['Square Ticket','Big Ticket','Shuffle Spark','Clean Slate']){
  const p=catalog.powers.find(p=>p.name===name);assert(p&&p.engine==='native'&&!p.openingOnly);
 }
});

test('Every animal magnet has stable native identity and animal filter metadata',()=>{
 const magnets=catalog.powers.filter(p=>p.type==='AnimalSpawnPower');
 assert.equal(magnets.length,10);
 for(const p of magnets){
  const animal=p.name.replace(' Magnet','');
  assert.equal(p.nativeKey,'AnimalSpawnPower:'+p.name);
  assert.equal(p.goals,'Both');assert.equal(p.openingOnly,false);
  assert(p.qualities.includes('animal:'+animal));
  assert(M.matchesFilters(p,{['animal:'+animal]:'include'},'all'));
 }
 for(const l of catalog.levels.slice(19,23))for(const r of l.roundDrafts.filter(r=>r.round>=4)){
  assert(r.slots.some(s=>s.query.includes('any=')&&s.query.includes('Roster')));
  assert(r.slots.some(s=>s.query==='all=Shuffle; any=None; none=None'));
 }
});
test('Exact length reward distinguishes per-tile from per-match',()=>{
 assert.equal(M.description(p({unit:'match'})),'Matches that are exactly 4 gain 3 base points.');
 assert.equal(M.score([p()],match()).score,28);assert.equal(M.score([p()],match({length:5})).score,20);assert.equal(M.score([p({unit:'match'})],match()).score,19);
});
test('Animal and geometry conditions must both be met',()=>{
 const power=p({trigger:'square',animal:'Duck'});assert.equal(M.score([power],match({square:true})).score,28);assert.equal(M.score([power],match({square:true,animal:'Bear'})).score,16);assert.equal(M.score([power],match()).score,16);
});
test('Earned upgrades benefit future matches and apply without their earning condition',()=>{
 const power=p({permanent:true,reward:'multi',unit:'match',value:1});let r=M.score([power],match());assert.equal(r.score,16);
 r=M.score([power],match(),r.states);assert.equal(r.score,16);assert.equal(r.states[power.id].earned,1);
 r=M.score([power],match({length:5}),r.states);assert.equal(r.score,40);
});
test('Consecutive progress resets after an unrelated match',()=>{
 const power=p({permanent:true,consecutive:true});let r=M.score([power],match());r=M.score([power],match({length:5}),r.states);assert.equal(r.states[power.id].progress,0);assert.equal(r.states[power.id].earned,0);
});
test('Sequence requires 4 then 5 then 6 and restarts on a wrong length',()=>{
 const power=p({trigger:'sequence',permanent:true,threshold:1});let r=M.score([power],match());r=M.score([power],match({length:6}),r.states);assert.equal(r.states[power.id].earned,0);
 for(const length of [4,5,6])r=M.score([power],match({length}),r.states);assert.equal(r.states[power.id].earned,1);
 assert.throws(()=>p({trigger:'sequence',sequence:'4,,6'}));
});
test('Multipliers combine additively before score factors',()=>{
 const powers=[p(),p({id:'custom-power-multi',reward:'multi',value:1,unit:'match'}),p({id:'custom-power-factor',reward:'factor',value:2,unit:'match'})];assert.equal(M.score(powers,match()).score,112);
});
test('Block credits do not use points rewards or advance ineligible growth',()=>{
 const power=p({permanent:true,threshold:1});const r=M.score([power],match(),{},4,'BlocksCleared');assert.equal(r.score,4);assert.equal(r.states[power.id].earned,0);
 assert.equal(M.score([p({reward:'credits',goals:'Blocks',value:2})],match(),{},4,'BlocksCleared').score,12);
});
test('Passive permanent rewards are reserved for openings and numbers are bounded',()=>{
 assert.throws(()=>p({permanent:true,trigger:'always'}));assert.doesNotThrow(()=>p({permanent:true,trigger:'always',openingOnly:true}));assert.throws(()=>p({value:Infinity}));assert.throws(()=>p({threshold:1.5}));
});
test('Workspace round-trips and rejects duplicate IDs, missing references and opening rewards',()=>{
 const power=p();const level={...catalog.levels[6],id:'custom-level-test',source:'custom',starters:[power.id],rewards:[power.id]};
 const raw={schema:'prismatch-design-workspace',version:1,powers:[power],levels:[level]};const result=M.importWorkspace(JSON.parse(JSON.stringify(raw)),catalog);assert.equal(result.levels[0].name,'Garden Plots');
 assert.throws(()=>M.importWorkspace({...raw,powers:[power,power]},catalog));assert.throws(()=>M.importWorkspace({...raw,levels:[{...level,rewards:['missing']}]},catalog));assert.throws(()=>M.importWorkspace({...raw,powers:[{...power,openingOnly:true}]},catalog));assert.throws(()=>M.importWorkspace({...raw,version:99},catalog));
});
test('Target previews retain unrounded growth between rounds',()=>{assert.deepEqual(M.targets({target:400,rounds:3,growth:25}),[400,500,625]);assert.deepEqual(M.targets({target:1,rounds:3,growth:50}),[1,2,3]);});
test('Combined filters require all included qualities and always exclude blocked ones',()=>{
 const animalGrowth=p({animal:'Duck',permanent:true});
 assert(M.matchesFilters(animalGrowth,{animal:'include',growth:'include',opening:'exclude'}));
 assert(!M.matchesFilters({...animalGrowth,openingOnly:true},{animal:'include',growth:'include',opening:'exclude'}));
 assert(!M.matchesFilters(p(),{animal:'include',growth:'include'}));
 assert(M.matchesFilters(p(),{}));
});
test('Any matching mode still honors every exclusion',()=>{
 const power=p({animal:'Duck'});
 assert(M.matchesFilters(power,{animal:'include',growth:'include'},'any'));
 assert(!M.matchesFilters(power,{animal:'include',growth:'include','animal:Duck':'exclude'},'any'));
 assert(M.matchesFilters(power,{opening:'exclude'},'all'));
 assert(!M.matchesFilters(power,{custom:'exclude'},'any'));
});
test('Qualities classify both shared game references and custom designs',()=>{
 const double=catalog.powers.find(p=>p.name==='Duck Double');assert(!M.qualities(double).has('multi'));assert(!M.qualities(double).has('factor'));assert(M.matchesFilters(double,{'animal:Duck':'include',base:'include',growth:'exclude'}));
 const engine=catalog.powers.find(p=>p.name==='Garden Engine');assert(M.matchesFilters(engine,{square:'include',growth:'include'}));
 assert(M.matchesFilters(p({trigger:'cascade',reward:'multi'}),{cascade:'include',multi:'include',game:'exclude'}));
 assert(M.matchesFilters({...double,goals:'Both'},{points:'include',blocks:'include'}));
 assert.throws(()=>p({reward:'credits',goals:'Points'}),/Use Points/);
 assert(M.qualities(p({reward:'credits',goals:'Blocks'})).has('blocks')); 
});
console.log(`${tests} design workspace checks passed.`);

test('Boss target multiplier affects only the final round and stays capped',()=>{
 const level={target:100,rounds:3,growth:50,bossStyle:'Wallflower',bossTargetMultiplier:2};
 assert.deepEqual(M.targets(level),[100,150,450]);
 assert.deepEqual(M.targets({...level,bossStyle:'None'}),[100,150,225]);
 assert.deepEqual(M.targets({...level,bossTargetMultiplier:1}),[100,150,225]);
 assert.equal(M.targets({...level,target:1000000,rounds:10,growth:1000,bossTargetMultiplier:10}).at(-1),2147483647);
 const boosted=catalog.levels.filter(l=>l.bossTargetMultiplier===2);
 assert.equal(boosted.length,9);assert(boosted.every(l=>l.goal==='Score'&&l.number>1));
 for(const l of boosted)assert.equal(M.validateLevel({...l,id:'custom-level-validation'}).bossTargetMultiplier,2);
 const l=boosted[0];for(const factor of [0,1.5,11])assert.throws(()=>M.validateLevel({...l,id:'custom-level-validation',bossTargetMultiplier:factor}));
});
