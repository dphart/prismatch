const assert=require('node:assert/strict'),fs=require('node:fs'),R=require('./round-offers.js');
const d=JSON.parse(fs.readFileSync(__dirname+'/../../Assets/Resources/Content/prismatch.json','utf8'));
const exceptions=new Set([2,4,5,7,8,24]);
for(const l of d.levels){
 if(exceptions.has(l.number))continue;
 const opening=l.roundOffers.find(o=>o.round===1);
 assert(opening&&opening.slots.every(s=>s.mode==='filters'));
 assert.equal(opening.slots[0].filters.growth,"include");
 assert.equal(l.starters.length,0);assert.equal(l.rewards.length,0);
 assert.deepEqual(opening.slots.map(s=>Object.keys(s.filters).filter(k=>s.filters[k]==='include')[0]),l.goal==='Score'?['base','multi','factor']:['credits','clearing','credits']);
 assert.equal(R.preview(opening,l,d.powers).filled,3);
 if(l.goal==='Score')assert(l.basePerTile>0,'Multiplier-only picks still need base scoring.');
 if(l.number>=20&&l.number<=23)assert(opening.slots.every(s=>s.filters.shuffle==='exclude'));
}
for(const number of [4,5,7,8])assert.equal(d.levels.find(l=>l.number===number).starters.length,3);
for(const number of [20,21,22,23])assert(d.levels.find(l=>l.number===number).roundOffers.filter(o=>o.round===2||o.round===3).every(o=>o.slots.every(s=>s.mode==='powers')));
const level=d.levels.find(l=>l.number===35),slot=level.roundOffers[0].slots[0];
const future={id:'test-future-base',name:'Future base reward',source:'custom',trigger:'exact',animal:'Any',length:4,reward:'base',value:2,unit:'tile',goals:'Points',permanent:true,openingOnly:false};
assert(R.candidates(slot,1,level,[...d.powers,future]).some(p=>p.id===future.id));
console.log('PASS Campaign tag defaults, intentional teaching exceptions, scoring baseline, shuffle reservation and future matching powers.');
