'use strict';
const assert=require('node:assert/strict'),fs=require('node:fs'),vm=require('node:vm');
const M=require('./model.js'),R=require('./round-offers.js'),data=require('../../Assets/Resources/Content/prismatch.json');
const source=fs.readFileSync(__dirname+'/round-editor.js','utf8');
function renderer(view){
 const context={M,KEY:'test',window:{RoundOffers:R,LevelPowerActions:{buttons:p=>`<button data-level-power="${p.id}">Edit</button>`}},
  localStorage:{getItem:()=>view},powerTags:()=>'<div class="power-tags"></div>',esc:s=>String(s).replaceAll('&','&amp;').replaceAll('<','&lt;').replaceAll('"','&quot;'),desc:p=>p.description};
 vm.runInNewContext(source,context);
 return context.window.RoundOfferEditor.matchingPowersMarkup;
}
const powers=data.powers.filter(p=>p.type==='AnimalDoublePower'),before=JSON.stringify(powers);
const condensed=renderer('condensed')(powers),all=renderer('all')(powers);
assert(condensed.includes('Animal Double'));
assert(condensed.includes('10 copies'));
assert.equal((condensed.match(/class="animal-copies"/g)||[]).length,1);
for(const p of powers)assert(condensed.includes(`data-level-power="${p.id}"`),'Every animal copy must keep its edit action.');
assert(!all.includes('animal-copies'));
assert(!all.includes('10 copies'));
for(const p of powers)assert(all.includes(p.name));
assert(renderer('condensed')([]).includes('No powers match'));
assert.equal(JSON.stringify(powers),before);
console.log('PASS Round matching lists toggle between generic families and all copies, preserve copy editing, and never change the power pool.');
