'use strict';
const assert=require('node:assert/strict'),fs=require('node:fs'),path=require('node:path'),{createServer}=require('./serve.cjs'),schema=require('./shared-schema.cjs');
(async()=>{const folder=fs.mkdtempSync(path.resolve(__dirname,'../../tmp/shared-content-test-')),contentFile=path.join(folder,'content.json'),baseline=fs.readFileSync(path.resolve(__dirname,'../../Assets/Resources/Content/prismatch.json'),'utf8');fs.writeFileSync(contentFile,baseline);const server=createServer({port:8788,contentFile,backupDir:path.join(folder,'backups')});await new Promise(r=>server.listen(8788,'127.0.0.1',r));
let checks=0;const check=(name,fn)=>{fn();checks++;console.log('PASS '+name);};
try{
 const get=async()=>await (await fetch('http://127.0.0.1:8788/api/content')).json();
 const put=async(d,revision,origin='http://127.0.0.1:8788')=>fetch('http://127.0.0.1:8788/api/content',{method:'PUT',headers:{'Content-Type':'application/json',Origin:origin},body:JSON.stringify({document:d,revision})});
 const initial=await get(),edited=structuredClone(initial.document),four=edited.powers.find(p=>p.type==='FourExactlyPower');four.parameters.basePointsMulti=7;
 const response=await put(edited,initial.revision),saved=await response.json();check('Save changes real JSON and renders wording from the same value',()=>{assert.equal(response.status,200);assert.equal(saved.document.powers.find(p=>p.id===four.id).description,'Exactly 4 tiles: +7 multiplier.');assert.notEqual(saved.revision,initial.revision);});
 check('Every write creates an exact recoverable backup',()=>assert.equal(fs.readFileSync(path.join(folder,'backups',fs.readdirSync(path.join(folder,'backups'))[0]),'utf8'),baseline));
 const stale=await put(initial.document,initial.revision);check('Concurrent stale edits are rejected',()=>assert.equal(stale.status,409));
 const bad=structuredClone(saved.document);bad.powers.find(p=>p.id===four.id).parameters.unsupported=42;
 const rejected=await put(bad,saved.revision);check('Unsupported native fields are rejected without writing',()=>{assert.equal(rejected.status,400);assert.equal(JSON.parse(fs.readFileSync(contentFile)).powers.find(p=>p.id===four.id).parameters.unsupported,undefined);});
 const external=await put(saved.document,saved.revision,'https://example.com');check('Other websites cannot write to the local game file',()=>assert.equal(external.status,403));
 check('Starter powers cannot leak into later rewards',()=>{const d=structuredClone(saved.document);d.levels[2].rewards=[d.powers.find(p=>p.openingOnly).id];assert.throws(()=>schema.validate(d,saved.document),/Starting powers/);});
 check('Structured powers reject fractional base rewards',()=>{const M=require('./model');assert.throws(()=>M.validatePower({id:'custom-power-test',name:'Test',trigger:'exact',animal:'Any',length:4,sequence:'4,5,6',reward:'base',value:0.5,unit:'match',permanent:false,threshold:1,goals:'Points'}),/whole numbers/);});
 const planned=structuredClone(saved.document);planned.levels[6].roundOffers=[{round:2,label:'Grow your build',slots:[{mode:'filters',count:3,match:'all',filters:{growth:'include',opening:'exclude'},repeats:'PreferNew'}]}];
 const planResponse=await put(planned,saved.revision),planSaved=await planResponse.json();
 check('Round offers persist through the real save API without replacing legacy defaults',()=>{assert.equal(planResponse.status,200);assert.equal(planSaved.document.levels[6].roundOffers[0].slots[0].filters.growth,'include');assert.deepEqual(planSaved.document.levels[6].roundDrafts,initial.document.levels[6].roundDrafts);assert(planSaved.document.powers.every(p=>Array.isArray(p.qualities)));});
 const invalidPlan=structuredClone(planSaved.document);invalidPlan.levels[6].roundOffers[0].slots[0].filters.typo='include';
 const invalidPlanResponse=await put(invalidPlan,planSaved.revision);check('Save API rejects unknown round filters',()=>assert.equal(invalidPlanResponse.status,400));
 const linked=structuredClone(planSaved.document),links=require('./power-links');
 const variant=links.makeCopy(linked,four.id,linked.levels[6].id,'custom-power-api-copy-test');variant.parameters.basePointsMulti=11;
 links.saveCopy(linked,linked.levels[6].id,variant);
 const copyResponse=await put(linked,planSaved.revision),copySaved=await copyResponse.json();
 check('Save API atomically creates a level copy and links only its level',()=>{
  assert.equal(copyResponse.status,200,JSON.stringify(copySaved));
  assert.equal(copySaved.document.levels[6].powerOverrides[four.id],variant.id);
  assert.equal(copySaved.document.levels[7].powerOverrides,undefined);
  assert.equal(copySaved.document.powers.find(p=>p.id===four.id).parameters.basePointsMulti,7);
  assert.equal(copySaved.document.powers.find(p=>p.id===variant.id).parameters.basePointsMulti,11);
 });
 const globalEdit=structuredClone(copySaved.document);globalEdit.powers.find(p=>p.id===four.id).parameters.basePointsMulti=4;
 const globalResponse=await put(globalEdit,copySaved.revision),globalSaved=await globalResponse.json();
 check('A later global edit preserves independent copy values',()=>{
  assert.equal(globalResponse.status,200);
  assert.equal(globalSaved.document.powers.find(p=>p.id===variant.id).parameters.basePointsMulti,11);
  assert.equal(globalSaved.document.powers.find(p=>p.id===four.id).parameters.basePointsMulti,4);
 });
 console.log(checks+' shared content checks passed.');
}finally{await new Promise(r=>server.close(r));fs.rmSync(folder,{recursive:true});}
})().catch(e=>{console.error(e);process.exitCode=1;});
