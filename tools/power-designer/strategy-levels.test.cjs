'use strict';
const assert=require('node:assert/strict'),fs=require('node:fs'),M=require('./model.js'),schema=require('./shared-schema.cjs'),R=require('./round-offers.js');
const d=JSON.parse(fs.readFileSync('Assets/Resources/Content/prismatch.json','utf8'));
schema.validate(structuredClone(d),d);
const expected=[['MigrationSeason','MigrationWarden','Score','LimitedMoves'],['CrackTheVault','VaultWarden','BlocksCleared','LimitedMoves'],['SteppingStones','StoneKeeper','Score','Timed'],['AnimalRescue','RescueKeeper','BlocksCleared','LimitedMoves'],['Crosswinds','UnstableWinds','Score','Timed']];
for(let i=0;i<expected.length;i++){
 const [mechanic,bossStyle,goal,mode]=expected[i],l=d.levels[24+i];
 assert.equal(l.number,25+i);assert.equal(l.mechanic,mechanic);assert.equal(l.bossStyle,bossStyle);assert.equal(l.goal,goal);assert.equal(l.mode,mode);
 assert.equal(l.width,7);assert.equal(l.height,7);assert.equal(l.minimum,4);assert.equal(l.rounds,6);
 assert(M.mechanics.includes(mechanic));assert.equal(M.bossCompatibility(bossStyle,l),'');
 assert(M.bossCompatibility(bossStyle,{...l,mechanic:'None'}));
 const wrongGoal=goal==='Score'?'BlocksCleared':'Score';assert(M.bossCompatibility(bossStyle,{...l,goal:wrongGoal}));
 const invalid=structuredClone(d);invalid.levels[24+i].mechanic='None';assert.throws(()=>schema.validate(invalid,d),/boss|requires/);
 assert.equal(l.starters.length,0);assert.equal(l.rewards.length,0);assert.equal(l.roundOffers.length,l.rounds);
 for(const offer of l.roundOffers){assert(offer.slots.every(s=>s.mode==='filters'));assert.equal(R.preview(offer,l,d.powers).filled,3);}
 console.log('PASS '+l.number+' '+l.name+' persists with compatible powers and boss');
}
assert.equal(d.levels[23].name,'24','The previously authored level remains in place.');
