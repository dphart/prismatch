'use strict';
const assert=require('node:assert/strict');
const fs=require('node:fs');
const M=require('./model.js'),schema=require('./shared-schema.cjs');
const original=JSON.parse(fs.readFileSync(__dirname+'/../../Assets/Resources/Content/prismatch.json','utf8'));
const sample={id:'custom-power-display-test',name:'Display test',trigger:'exact',animal:'Any',length:4,sequence:'4,5,6',reward:'base',value:3,unit:'tile',permanent:true,threshold:2,consecutive:false,openingOnly:false,goals:'Points'};
const match={animal:'Duck',length:4,cascade:false,edge:false,corner:false,square:false};
for(const display of ['none','icon','tiles','iconAndTiles']){
 const configured=M.validatePower({...sample,displayWhenTriggered:display});
 assert.equal(configured.displayWhenTriggered,display);
 assert.deepEqual(M.score([configured],match),M.score([M.validatePower(sample)],match));
 const document=structuredClone(original);document.powers[0].displayWhenTriggered=display;
 schema.validate(document,original);
 assert.equal(document.powers[0].displayWhenTriggered,display);
 assert.deepEqual(document.powers[0].parameters,original.powers[0].parameters);
 assert.equal(document.powers[0].description,original.powers[0].description);
 console.log('PASS '+display+' persists without changing scoring or growth');
}
const invalid=structuredClone(original);invalid.powers[0].displayWhenTriggered='arbitrary-animation';
assert.throws(()=>schema.validate(invalid,original),/trigger display/);
assert.throws(()=>M.validatePower({...sample,displayWhenTriggered:'arbitrary-animation'}),/trigger display/);
assert.equal(M.validatePower(sample).displayWhenTriggered,undefined);
console.log('PASS invalid displays rejected and legacy definitions preserved');
