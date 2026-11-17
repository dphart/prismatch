'use strict';
const assert=require('node:assert/strict'),fs=require('node:fs');
const M=require('./model.js'),R=require('./round-offers.js'),S=require('./shared-schema.cjs');
const data=JSON.parse(fs.readFileSync('Assets/Resources/Content/prismatch.json','utf8'));
const level=data.levels.find(l=>l.mechanic==='WildRelease');
assert(level);assert.equal(level.number,31);assert.equal(level.goal,'Score');assert.equal(level.mode,'LimitedMoves');
assert.equal(level.minimum,4);assert.equal(level.basePerTile,4);assert.equal(level.moves,36);assert.equal(level.rounds,6);
assert(M.mechanics.includes('WildRelease'));assert.equal(level.bossStyle,'None');
assert(!level.powerOverrides);assert(!data.powers.some(p=>p.levelId===level.id));
const report=R.overview(level,data.powers);
for(const round of report.rounds){
 assert(round.powers.length>30,'Broad shared powers remain available.');
 assert(round.powers.every(p=>p.goals!=='Blocks'&&!p.openingOnly&&!p.levelId));
 assert.equal(R.preview(level.roundOffers.find(o=>o.round===round.round),level,data.powers).filled,3);
}
S.validate(structuredClone(data),data);
console.log('PASS Wild Release is editable, has six complete shared drafts and requires no per-level power copies.');
