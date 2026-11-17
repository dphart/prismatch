'use strict';
const assert=require('node:assert/strict'),fs=require('node:fs');
const M=require('./model.js'),schema=require('./shared-schema.cjs');
const original=JSON.parse(fs.readFileSync(__dirname+'/../../Assets/Resources/Content/prismatch.json','utf8'));
const tiers=['Basic','Rare','Legendary'];
assert(original.powers.every(p=>tiers.includes(p.tier)));
for(const tier of tiers){
 const d=structuredClone(original);d.powers[0].tier=tier;
 schema.validate(d,original);assert.equal(d.powers[0].tier,tier);
 assert.deepEqual(d.powers[0].parameters,original.powers[0].parameters);
 const custom=original.powers.find(p=>p.source==='custom');
 assert.equal(M.validatePower({...custom,tier}).tier,tier);
}
const invalid=structuredClone(original);invalid.powers[0].tier='Mythic';
assert.throws(()=>schema.validate(invalid,original),/power tier/);
console.log('PASS all powers have tiers; native/custom edits preserve them; invalid tiers rejected');
