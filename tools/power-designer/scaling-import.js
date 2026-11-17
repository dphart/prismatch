'use strict';
(function(root){
 function apply(document,patch){
  if(patch?.schema!=='prismatch-level-scaling'||patch.version!==1||!Array.isArray(patch.levels)||!patch.levels.length||patch.levels.length>200)throw Error('Invalid level scaling export.');
  const seen=new Set(),next=JSON.parse(JSON.stringify(document));
  for(const change of patch.levels){
   const level=next.levels.find(l=>l.id===change.id);
   if(!level||seen.has(change.id))throw Error('Unknown or duplicate level: '+change.id);
   seen.add(change.id);
   if(!Number.isInteger(change.target)||change.target<1||change.target>1000000||!Number.isInteger(change.growth)||change.growth<0||change.growth>1000)throw Error('Invalid target or growth for '+change.id);
   level.target=change.target;level.growth=change.growth;
  }
  return next;
 }
 const api={apply};if(typeof module!=='undefined')module.exports=api;else root.LevelScalingImport=api;
})(typeof window==='undefined'?globalThis:window);
