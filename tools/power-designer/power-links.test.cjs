'use strict';
const assert=require('node:assert/strict'),fs=require('fs'),L=require('./power-links'),R=require('./round-offers'),schema=require('./shared-schema.cjs');
const baseline=JSON.parse(fs.readFileSync(__dirname+'/../../Assets/Resources/Content/prismatch.json','utf8'));
const d=structuredClone(baseline),level=d.levels[6],other=d.levels[7],base=d.powers.find(p=>p.type==='FourExactlyPower');
const copy=L.makeCopy(d,base.id,level.id,'custom-power-level-copy-test');copy.parameters.basePointsMulti=9;
L.saveCopy(d,level.id,copy);schema.validate(d,baseline);
assert.equal(base.parameters.basePointsMulti,5);assert.equal(copy.parameters.basePointsMulti,9);
assert.equal(level.powerOverrides[base.id],copy.id);assert.equal(other.powerOverrides,undefined);
assert.equal(copy.nativeKey,undefined);assert.equal(copy.source,'variant');
const slot={mode:'powers',count:1,powerIds:[base.id],repeats:'PreferNew'};
assert.deepEqual(R.candidates(slot,2,level,d.powers).map(p=>p.id),[copy.id]);
assert.deepEqual(R.candidates(slot,2,other,d.powers).map(p=>p.id),[base.id]);
assert.throws(()=>{const bad=structuredClone(d);bad.levels[7].powerOverrides={[base.id]:copy.id};schema.validate(bad,d);},/belong/);
assert.throws(()=>{const bad=structuredClone(d);bad.powers.find(p=>p.id===copy.id).type='ShufflePower';schema.validate(bad,d);},/behavior/);
assert.throws(()=>{const bad=structuredClone(d);bad.powers.find(p=>p.id===copy.id).parameters.unknown=4;schema.validate(bad,d);},/Unsupported/);
level.roundOffers=[{round:2,label:'Copy',slots:[{...slot,powerIds:[copy.id]}]}];
const next=L.makeCopy(d,copy.id,level.id,'custom-power-second-copy-test');
L.saveCopy(d,level.id,next);schema.validate(d,d);
assert.equal(next.basePowerId,base.id);assert.equal(level.roundOffers[0].slots[0].powerIds[0],next.id);
assert.deepEqual(R.candidates(slot,2,level,d.powers).map(p=>p.id),[next.id]);
console.log('PASS native copies preserve behavior, isolate level use, validate tuning and replace prior references');
const structuredBase=d.powers.find(p=>p.source==='custom'&&!p.levelId);
if(structuredBase){
 const structured=L.makeCopy(d,structuredBase.id,other.id,'custom-power-structured-copy-test');
 structured.value=structured.value+1;L.saveCopy(d,other.id,structured);schema.validate(d,d);
 assert.equal(structured.source,'custom');assert.notEqual(structured.value,structuredBase.value);
 assert.equal(other.powerOverrides[structuredBase.id],structured.id);
 console.log('PASS structured powers also retain independent level copies');
}
