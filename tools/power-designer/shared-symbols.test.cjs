'use strict';
const assert=require('node:assert/strict'),fs=require('node:fs');
const Symbols=require('./power-symbols.js'),Preview=require('./card-preview.js');
const source=JSON.parse(fs.readFileSync('Assets/Resources/Content/power-icons.json','utf8'));
assert.deepEqual(Symbols.definitions,source);
assert.equal(source.symbols.match[0].image,'UI/Generated/match-tiles-v2');
assert.equal(source.symbols['animal-match'].filter(m=>m.animal).length,4);
const bearMatch=Symbols.render('animal-match',undefined,'Bear');
assert.equal((bearMatch.match(/Animals\/bear.png/g)||[]).length,4);
assert(!bearMatch.includes('match-tiles-v2.png'),'Animal matches show clear portraits without green tile backgrounds');
assert.equal(source.symbols['animal-match'].filter(m=>m.image).length,0);
assert.notEqual(source.palette[source.symbols['mastery-base'].find(m=>m.color==='sky').color],source.palette[source.symbols['mastery-multiplier'].find(m=>m.color==='multiplier').color],'Base and multiplier Mastery badges have distinct colors');
assert.equal(source.symbols['mastery-multiplier'][0].color,'navy','Multiplier badges have a contrasting outline behind the fill');
assert.equal(source.symbols.shuffle[0].image,'UI/Generated/nav-restart');
assert.equal(source.symbols.growth,undefined,'The signal-like growth decoration is retired');
assert.match(Symbols.render('shuffle'),/UI\/Generated\/nav-restart\.png/);
assert.equal(source.symbols.large.length,6,'The six-tile icon contains exactly six tiles');
for(let n=3;n<=10;n++)for(const suffix of ['','plus','max'])
 assert.equal(source.symbols['count-'+n+suffix].filter(m=>m.color==='green'&&!m.text).length,n,'Numeric match icons show the actual tile count');
assert.equal(Symbols.canonical('budget-edge'),'edge');
assert.equal(Symbols.render('budget-edge'),Symbols.render('edge'));
const catalog=JSON.parse(fs.readFileSync('Assets/Resources/Content/prismatch.json','utf8'));
const hippoGrowth=catalog.powers.find(p=>p.name==='Hippo Growth (Points)');
assert.equal(Preview.describe(hippoGrowth).baseBadge,'+2B');
assert.equal(Preview.describe(hippoGrowth).all,true);
assert.match(Preview.card(hippoGrowth),/>\+2B<\/text>/);
assert.match(Preview.card(hippoGrowth),/all-animals/);
assert.equal(Preview.describe(hippoGrowth,'Blocks').baseBadge,undefined);
for(const p of catalog.powers){
 const spec=Preview.describe(p),text=spec.text;
 if(/\bbase\b/i.test(text)&&!['AnimalDoublePower','RosterTradeoffPower'].includes(p.type))
  assert(spec.baseBadge||spec.rewardShape==='base-badge'||spec.shape==='mastery-base',p.name+' needs its base badge');
}
for(const base of catalog.powers.filter(p=>p.type==='IconTypeGivesBase')){
 assert.match(base.name,/Bonus \(Points\)$/);
 assert.equal(Preview.describe(base).rewardShape,'base-badge');
 assert(Preview.describe(base).rewardAnimal);
 assert.equal(Preview.describe(base).layout,'single');
 assert(!Preview.card(base).includes('flow-arrow'));
 assert.match(Preview.card(base),/mastery-animal/);
 assert.match(Preview.card(base),/>\+5B<\/text>/);
 const tuned={...base,parameters:{...base.parameters,BasePoints:7}};
 assert.match(Preview.card(tuned),/>\+7B<\/text>/,'Badges reflect authored tuning');
 assert.equal(Preview.describe(base,'Blocks').rewardShape,'credits');
}
for(const magnet of catalog.powers.filter(p=>p.type==='AnimalSpawnPower')){
 const spec=Preview.describe(magnet),html=Preview.card(magnet);
 assert.equal(spec.layout,'single');assert.equal(spec.shape,'refill');assert(spec.rewardAnimal);
 assert.match(html,/game-single-effect/);assert.match(html,/2x .* CHANCE/);
 assert(!html.includes('flow-arrow'));assert(!html.includes('Condition to reward'));
 assert.equal(Preview.describe(magnet,'Blocks').layout,'single');
}
for(const mastery of catalog.powers.filter(p=>p.type==='AnimalMasteryPower'&&!p.name.includes('Rivalry'))){
 const spec=Preview.describe(mastery),html=Preview.card(mastery);
 assert.equal(spec.layout,'single');assert.match(spec.shape,/^mastery-(base|multiplier)$/);
 assert.match(spec.condition,/EVERY 2 PLAYER MATCHES/);assert.match(spec.condition,/PLAYER MATCHES · .* ONLY/);
 assert.match(html,/mastery-animal/);assert(!html.includes('flow-arrow'));
}
assert.equal(Preview.describe(catalog.powers.find(p=>p.name==='Giraffe Giant Bloom')).shape,'large');
const jackpot=catalog.powers.find(p=>p.type==='JackpotFivePower');
assert.equal(Preview.describe(jackpot).shape,'count-5plus');
assert.equal(Preview.describe(jackpot).rewardShape,'factor-5');
assert.equal(Preview.describe(jackpot).reward,'NEXT MATCH');
assert.match(Preview.card(jackpot),/data-power-symbol="factor-5"/);
assert(!Preview.card(jackpot).includes('data-power-symbol="plus"'));
for(const p of catalog.powers)for(const goal of ['Points','Blocks']){
 const s=Preview.describe(p,goal);
 if(s.rewardShape)assert(Symbols.definitions.symbols[s.rewardShape],p.name+' reward icon must be shared');
 if(!s.partner)assert(Symbols.definitions.symbols[Symbols.canonical(s.shape)],p.name+' condition icon must be shared');
}
const balance=catalog.powers.find(p=>p.type==='EdgeBalancePower'),explorer=catalog.powers.find(p=>p.name==='Edge Explorer');
for(const power of [balance,explorer])assert.equal((Preview.card(power).match(/data-power-symbol="edge"/g)||[]).length,2,'Header and condition use the same edge symbol');
const old=Symbols.definitions.symbols.edge[0].color;
try{
 Symbols.definitions.symbols.edge[0].color='coral';
 const symbol=Symbols.render('edge');
 for(const p of [balance,explorer])assert.equal(Preview.card(p).split(symbol).length-1,2,'One definition change reaches both places on both powers');
}finally{Symbols.definitions.symbols.edge[0].color=old;}
for(const [key,marks]of Object.entries(source.symbols)){
 assert(marks.length);assert(Symbols.render(key).includes('<svg'));
 for(const m of marks){if(m.image)assert(fs.existsSync('Assets/Resources/'+m.image+'.png'));else if(!m.animal)assert(source.palette[m.color]);for(const k of ['x','y','w','h'])assert(Number.isFinite(m[k]));assert(m.w>0&&m.h>0);}
}
for(const p of catalog.powers)assert(!Preview.card(p).includes('growth-bars'),'No unexplained growth decorations remain');
const streak=catalog.powers.find(p=>p.name==='Shuffle Streak');
assert.equal(Preview.describe(streak).rewardShape,'shuffle');
assert.equal(Preview.describe(streak).shape,'match');
assert.equal(Preview.describe(catalog.powers.find(p=>p.name==='Half-Price Shuffle')).rewardShape,'half');
console.log('PASS shared definitions, edge aliases, one-change propagation across cards and valid icon geometry');
