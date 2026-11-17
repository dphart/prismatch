'use strict';
const fs=require('node:fs'),path=require('node:path'),assert=require('node:assert/strict');
const M=require('./model.js'),{createServer}=require('./serve.cjs');
(async()=>{
 const root=path.resolve(__dirname,'../..'),folder=fs.mkdtempSync(path.join(root,'tmp/save-recovery-test-'));
 const contentFile=path.join(folder,'content.json'),backupDir=path.join(folder,'backups');
 const base=JSON.parse(fs.readFileSync(path.join(root,'Assets/Resources/Content/prismatch.json'),'utf8'));
 fs.writeFileSync(contentFile,JSON.stringify(base));
 const server=createServer({port:8789,contentFile,backupDir});await new Promise(r=>server.listen(8789,'127.0.0.1',r));
 const get=async()=>(await fetch('http://127.0.0.1:8789/api/content')).json();
 const put=(document,revision)=>fetch('http://127.0.0.1:8789/api/content',{method:'PUT',headers:{'Content-Type':'application/json',Origin:'http://127.0.0.1:8789'},body:JSON.stringify({document,revision})});
 try{
  const initial=await get(),remote=structuredClone(base);remote.levels[0].target+=1;
  assert.equal((await put(remote,initial.revision)).status,200);
  const local=structuredClone(base),newLevel={...structuredClone(base.levels.at(-1)),id:'custom-level-save-test',number:base.levels.length+1,name:'New level test',source:'custom',bossStyle:'None',bossTargetMultiplier:1};local.levels.push(newLevel);
  assert.equal((await put(local,initial.revision)).status,409);
  const recovered=JSON.parse(fs.readFileSync(path.join(folder,'designer-save-recovery/latest-save-attempt.json'),'utf8'));
  assert.deepEqual(recovered.document.levels.at(-1),newLevel);
  const latest=await get(),merged=M.mergeSharedEdits(base,local,latest.document);
  assert.equal(merged.levels[0].target,remote.levels[0].target);
  assert.deepEqual(base,initial.document);
  const result=await put(merged,latest.revision);assert.equal(result.status,200,await result.clone().text());
  assert.equal((await result.json()).document.levels.at(-1).name,'New level test');
  const conflict=structuredClone(base);conflict.levels[0].target+=50;
  assert.throws(()=>M.mergeSharedEdits(base,conflict,latest.document),/Someone changed/);
  const concurrent=structuredClone(latest.document);concurrent.levels.push({...newLevel,id:'custom-level-other'});
  assert.equal(M.mergeSharedEdits(base,local,concurrent).levels.at(-1).number,base.levels.length+2);
  console.log('PASS Stale new-level saves recover, merge unrelated changes, preserve drafts and reject conflicting edits.');
 }finally{
  await new Promise(r=>server.close(r));
  if(!path.resolve(folder).startsWith(root+path.sep+'tmp'+path.sep))throw Error('Unsafe cleanup path');
  fs.rmSync(folder,{recursive:true});
 }
})().catch(e=>{console.error(e);process.exitCode=1;});
