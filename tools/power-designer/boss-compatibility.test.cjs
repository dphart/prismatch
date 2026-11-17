'use strict';
const assert=require('node:assert/strict'),fs=require('node:fs'),M=require('./model.js'),schema=require('./shared-schema.cjs');
const d=JSON.parse(fs.readFileSync('Assets/Resources/Content/prismatch.json','utf8'));
for(const goal of ['Score','BlocksCleared'])for(const mode of ['Timed','LimitedMoves']){
 const level={goal,mode,mechanic:'None'};
 for(const style of ['CrowdTax','SmallChange','InnerToll','RepeatToll'])assert.equal(!M.bossCompatibility(style,level),mode==='LimitedMoves');
 for(const style of ['DuckEmbargo','MostMatched'])assert.equal(!M.bossCompatibility(style,level),goal==='Score');
 for(const style of ['Wallflower','CascadeSurfer','OpeningAct','Blackout','FenceFox','TideTurtle'])assert.equal(M.bossCompatibility(style,level),'');
 assert.equal(M.bossCompatibility('None',level),'');
 assert(M.bossCompatibility('TreasureCrab',level));assert(M.bossCompatibility('Overgrowth',level));
}
assert.equal(M.bossCompatibility('TreasureCrab',{goal:'Score',mode:'LimitedMoves',mechanic:'TreasureTrail'}),'');
assert.equal(M.bossCompatibility('Overgrowth',{goal:'BlocksCleared',mode:'Timed',mechanic:'GardenPlots'}),'');
for(const l of d.levels)assert.equal(M.bossCompatibility(l.bossStyle,l),'',l.name);
const blocks={goal:'BlocksCleared',mode:'Timed',mechanic:'None'};
assert(M.bossDescription(d.bosses.find(b=>b.style==='Blackout'),blocks).includes('block credit'));
for(const [style,goal,mode,mechanic] of [['MostMatched','BlocksCleared','Timed','None'],['CrowdTax','Score','Timed','None'],['Overgrowth','Score','Timed','None'],['TreasureCrab','Score','LimitedMoves','None']]){
 const next=structuredClone(d);Object.assign(next.levels[0],{bossStyle:style,goal,mode,mechanic,starters:[],rewards:[],roundOffers:[]});
 assert.throws(()=>schema.validate(next,d),/boss|Overgrowth|Treasure Crab/);
}
console.log('PASS Boss choices cover points/blocks, time/moves and mechanic prerequisites; incompatible saves are rejected.');
