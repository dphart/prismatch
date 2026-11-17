'use strict';
const assert=require('node:assert/strict'),fs=require('node:fs'),M=require('./model.js'),R=require('./round-offers.js');
const d=JSON.parse(fs.readFileSync(__dirname+'/../../Assets/Resources/Content/prismatch.json','utf8')),powers=d.powers,level=d.levels[6],four=powers.find(p=>p.type==='FourExactlyPower');
const filters=(f={},match='all',count=3)=>({mode:'filters',filters:f,match,count,repeats:'PreferNew'});
const picked=(ids,count=1)=>({mode:'powers',powerIds:ids,count,repeats:'NewOnly'});
let count=0;const test=(name,fn)=>{fn();count++;console.log('PASS '+name);};

test('Inherited offers show exact opening powers and prefer explicit reward pools',()=>{
 const size=d.levels.find(l=>l.number===4),before=JSON.stringify(size);
 const opening=R.inherited(size,1,powers);
 assert.deepEqual(opening.slots[0].powerIds,size.starters);
 assert.equal(R.candidates(opening.slots[0],1,size,powers).length,3);
 const custom={...size,rewards:[four.id]};
 assert.deepEqual(R.inherited(custom,2,powers).slots[0].powerIds,[four.id]);
 assert.equal(JSON.stringify(size),before);
});

test('Inherited native drafts retain group counts and tag intersections and exclusions',()=>{
 const l=d.levels.find(l=>l.number===5),draft=l.roundDrafts.find(r=>r.round===2);
 const offer=R.inherited(l,2,powers);
 assert.deepEqual(offer.slots.map(s=>s.count),draft.slots.map(s=>s.count));
 const first=R.candidates(offer.slots[0],2,l,powers);
 assert(first.length>0);
 assert(first.every(p=>(p.tags.split(', ').includes('Clearing')||p.blockDescriptionTemplate&&M.qualities(p).has('clearing'))&&!p.tags.split(', ').includes('MatchFoundation')));
 assert.equal(R.inherited({...l,starters:[],rewards:[],roundDrafts:[]},2,powers),null);
});
test('Round filters use the finder include-all/any and exclusion semantics',()=>{
 const s=filters({exact:'include',growth:'include'});assert(!R.matches(s,four));s.match='any';assert(R.matches(s,four));s.filters.multi='exclude';assert(!R.matches(s,four));
 for(const p of powers)assert.equal(R.matches(s,p),M.matchesFilters(p,s.filters,s.match));
});
test('Simple categories replace duplicate tags while saved rules keep their meaning',()=>{
 assert(!R.groups(powers).some(g=>g.items.some(([k])=>k==='tag:Animal')));
 assert(R.groups(powers).some(g=>g.items.some(([k])=>k==='animal')));
 assert(R.groups(powers,['tag:Animal']).some(g=>g.items.some(([k])=>k==='tag:Animal')));
 assert(R.filterKeys(powers).includes('tag:Animal'));
 assert.equal(R.validate([{round:2,label:'Legacy animal rule',slots:[filters({'tag:Animal':'include'})]}],level.rounds,powers)[0].slots[0].filters['tag:Animal'],'include');
 const s=filters({'tag:Animal':'include','animal:Duck':'include',opening:'exclude'});const candidates=R.candidates(s,2,level,powers);assert(candidates.length>0);assert(candidates.every(p=>M.qualities(p).has('animal:Duck')&&!p.openingOnly));
});
test('Mixed groups are saved by round and can overlap without duplicating cards',()=>{
 const offer={round:2,label:'Mixed',slots:[picked([four.id]),filters({base:'include'},'all',2)]};
 assert.equal(R.validate([offer],7,powers)[0].slots.length,2);const p=R.preview(offer,level,powers);assert.equal(p.filled,3);assert.equal(p.requested,3);
 const narrow={round:2,label:'Only four',slots:[picked([four.id]),picked([four.id])]};assert.equal(R.preview(narrow,level,powers).filled,1);
});
test('Preview keeps starting powers in round one and filters by goal and enabled status',()=>{
 assert(R.candidates(filters({opening:'include'}),1,level,powers).length>0);
 assert.equal(R.candidates(filters({opening:'include'}),2,level,powers).length,0);
 assert(R.candidates(filters(),2,{goal:'BlocksCleared'},powers).every(p=>p.goals!=='Points'));
 assert.equal(R.candidates(picked([four.id]),2,level,[{...four,enabled:false}]).length,0);
});
test('Filters include future matching powers instead of freezing an ID list',()=>{
 const slot=filters({growth:'include'}),custom=M.validatePower({id:'custom-power-later',name:'Later growth',trigger:'exact',animal:'Any',length:4,sequence:'4,5,6',reward:'base',value:2,unit:'match',permanent:true,threshold:3,goals:'Points'});
 const before=R.candidates(slot,2,level,powers).length;assert.equal(R.candidates(slot,2,level,[...powers,custom]).length,before+1);
});
test('Invalid rounds, unknown filters, too many choices and later starters are rejected',()=>{
 const o={round:2,label:'Test',slots:[filters()]};assert.throws(()=>R.validate([o,o],7,powers),/unique/);assert.throws(()=>R.validate([{...o,round:8}],7,powers),/within/);
 assert.throws(()=>R.validate([{...o,slots:[filters({typo:'include'})]}],7,powers),/filters/);
 assert.throws(()=>R.validate([{...o,slots:[filters(),filters()]}],7,powers),/three powers/);
 assert.throws(()=>R.validate([{...o,slots:[picked([powers.find(p=>p.openingOnly).id])]}],7,powers),/Starting powers/);
 assert.throws(()=>R.validate([{...o,slots:[picked([])]}],7,powers),/at least one/);
});
console.log(count+' round offer checks passed.');
