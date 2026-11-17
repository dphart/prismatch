'use strict';
const assert=require('node:assert/strict'),fs=require('node:fs'),path=require('node:path'),os=require('node:os');
const Preview=require('./card-preview.js'),Art=require('./artwork.cjs'),{createServer}=require('./serve.cjs');
const catalog=JSON.parse(fs.readFileSync('Assets/Resources/Content/prismatch.json','utf8'));
const hippo=catalog.powers.find(p=>p.name==='Hippo Growth (Points)');
const edge=catalog.powers.find(p=>p.name==='Edge Explorer');
assert.equal(Preview.describe(edge).condition,'3 MATCHES');
assert.equal(Preview.describe(edge).shape,'budget-edge');
assert.equal(Preview.describe(edge).reward,'+3 SECONDS');
assert.equal(Preview.describe(edge,'Points','LimitedMoves').reward,'+1 MOVE');
assert.match(Preview.card(edge),/data-power-symbol="clock"/);
assert.match(Preview.card(edge,'Blocks','LimitedMoves'),/data-power-symbol="moves"/);
assert(!Preview.card(edge).includes('reward-plus'));
assert(!Preview.describe(edge).text.includes('or 1 move'));
const pact=catalog.powers.find(p=>p.name==='Bear + Sloth Pact');
assert.deepEqual([Preview.describe(pact).animal,Preview.describe(pact).partner],['Bear','Sloth']);
assert.equal(Preview.describe(pact).reward,'HALF POINTS');
assert.equal(Preview.describe(pact,'Blocks').reward,'HALF FINAL BLOCKS');
assert.match(Preview.card(pact),/data-power-symbol="half"/);assert(!Preview.card(pact).includes('reward-plus'));
assert.equal(Preview.describe(hippo).condition,'10 TILES');assert.equal(Preview.describe(hippo).all,true);
assert.equal(Preview.describe(hippo,'Blocks').rewardAnimal,'Hippo');assert.equal(Preview.describe(hippo,'Blocks').all,false);
assert.match(Preview.card(hippo),/game-flow/);assert.match(Preview.card(hippo),/Hippo Growth/);assert.match(Preview.card(hippo),/#FFB52E/);
const hostile=Preview.card({...hippo,name:'<img src=x onerror=alert(1)>',description:'<script>bad</script>',descriptionTemplate:null});
assert(!hostile.includes('<script>'));assert(hostile.includes('&lt;img'));
for(const p of catalog.powers)for(const goal of ['Points','Blocks']){const s=Preview.describe(p,goal);assert(s.condition);assert(s.reward);assert(Preview.card(p,goal).includes('game-power-card'));}
(async()=>{
 const folder=fs.mkdtempSync(path.join(os.tmpdir(),'prismatch-artwork-')),artRoot=path.join(folder,'art');fs.mkdirSync(artRoot);
 const original=fs.readFileSync('Assets/Resources/UI/Generated/Animals/hippo.png'),next=fs.readFileSync('Assets/Resources/UI/Generated/Animals/bear.png');
 assert.deepEqual(Art.dimensions(original),Art.dimensions(next));fs.writeFileSync(path.join(artRoot,'sample.png'),original);
 const port=18993,server=createServer({port,artRoot,backupDir:path.join(folder,'content-backups')});await new Promise(r=>server.listen(port,'127.0.0.1',r));
 try{
  const base='http://127.0.0.1:'+port;
  const images=(await (await fetch(base+'/api/artwork')).json()).images;assert.equal(images.length,1);
  const put=(id,data,revision,origin=base)=>fetch(base+'/api/artwork?id='+encodeURIComponent(id),{method:'PUT',headers:{Origin:origin,'Content-Type':'image/png','If-Match':revision},body:data});
  assert.equal((await put('sample.png',next,images[0].revision,'https://example.com')).status,403);
  assert.equal((await put('../sample.png',next,images[0].revision)).status,400);
  assert.equal((await put('sample.png',Buffer.from('invalid'),images[0].revision)).status,400);
  const response=await put('sample.png',next,images[0].revision);assert.equal(response.status,200);const saved=await response.json();
  assert.deepEqual(fs.readFileSync(saved.backup),original);assert.deepEqual(fs.readFileSync(path.join(artRoot,'sample.png')),next);
  assert.equal((await put('sample.png',original,images[0].revision)).status,409);
  for(const file of ['card-workshop.html','card-preview.js','card-workshop.js','card-workshop.css','power-symbols.js','power-icons.js'])assert.equal((await fetch(base+'/tools/power-designer/'+file)).status,200);
  const sandbox={window:{}};require('node:vm').runInNewContext(await (await fetch(base+'/tools/power-designer/power-icons.js')).text(),sandbox);
  assert.deepEqual(JSON.parse(JSON.stringify(sandbox.window.PRISMATCH_POWER_ICONS)),JSON.parse(fs.readFileSync('Assets/Resources/Content/power-icons.json','utf8')));
  assert.equal((await fetch(base+'/Assets/Resources/UI/Generated/power-steady.png')).status,200);
  console.log('PASS all card previews; scope and escaping; image inventory, backups, replacement, stale edits and origin/path validation');
 }finally{await new Promise(r=>server.close(r));}
})().catch(e=>{console.error(e);process.exitCode=1;});
