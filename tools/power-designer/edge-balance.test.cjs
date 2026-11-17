const assert=require('node:assert/strict'),fs=require('node:fs');
const M=require('./model.js'),S=require('./shared-schema.cjs');
const d=JSON.parse(fs.readFileSync('Assets/Resources/Content/prismatch.json','utf8'));
const p=d.powers.find(p=>p.type==='EdgeBalancePower');
assert.ok(p?.enabled);assert.equal(p.goals,'Both');assert.equal(p.basePerTile,0);
for(const key of ['edge','growth','base','points'])assert.ok(M.qualities(p).has(key),key);
S.validate(structuredClone(d),d);
console.log('Edge Balance native catalog and editor validation passed.');
