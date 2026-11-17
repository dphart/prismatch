'use strict';
function triggerDisplayField(power){
 return '<label class="field"><span>Display when triggered</span><select name="displayWhenTriggered">'+selectOptions([
  ['', 'Keep current display'], ['none','None'], ['icon','Power icon'], ['tiles','Highlight matched tile backgrounds'], ['iconAndTiles','Power icon + tile highlight']
 ],power.displayWhenTriggered??'')+'</select><small class="hint">Presentation only. Existing powers keep their current display unless you change this.</small></label>';
}
// Live editing uses the same JSON that Unity loads. Local imports remain drafts until saved.
let sharedRevision=window.PRISMATCH_SHARED_REVISION;
const originalRender=render,originalEditPower=editPower,originalEditLevel=editLevel;
function adoptShared(data){Object.assign(C,data.document);sharedRevision=data.revision;$('#snapshot').textContent='Shared with Unity Â· '+new Date(C.updatedAt).toLocaleString();}
async function saveShared(change){
 const base=clone(C),next=clone(C);change(next);
 const put=(document,revision)=>fetch('/api/content',{method:'PUT',headers:{'Content-Type':'application/json'},body:JSON.stringify({revision,document})});
 let response=await put(next,sharedRevision);
 if(response.status===409){
  const latestResponse=await fetch('/api/content');if(!latestResponse.ok)throw Error('Could not reload the game file. Your edits are still open.');
  const latest=await latestResponse.json();
  const merged=M.mergeSharedEdits(base,next,latest.document);
  response=await put(merged,latest.revision);
 }
 const result=await response.json();if(!response.ok)throw Error(result.error||'Save failed. Your edits are still open.');adoptShared(result);
 toast('Saved to the game file. Unity uses it on the next Play or build.');
}

function rememberDrafts(){localStorage.setItem(KEY,JSON.stringify(packageData()));}
let levelPowerContext=null;
async function persistEditedPower(power,test=false){
 const context=levelPowerContext;
 await saveShared(d=>{
  if(context?.mode==='copy')window.LevelPowerLinks.saveCopy(d,context.level.id,power);
  else {const i=d.powers.findIndex(p=>p.id===power.id);if(i<0)d.powers.push(power);else d.powers[i]={...d.powers[i],...power};}
 });
 workspace.powers=workspace.powers.filter(p=>p.id!==power.id);rememberDrafts();dirty=false;
 if(context){
  if(context.mode==='copy'){
   window.LevelPowerLinks.replaceReferences(context.level,context.level.powerOverrides?.[power.basePowerId]||power.basePowerId,power.id);
   context.level.powerOverrides={...context.level.powerOverrides,[power.basePowerId]:power.id};
  }
  returnToLevel(false);return;
 }
 $('#editor').close();if(test){selectedSandbox.add(power.id);tab='sandbox';}render();
}
function returnToLevel(check=true){
 if(check&&dirty&&!confirm('Discard unsaved power changes and return to the level?'))return;
 const context=levelPowerContext;if(!context)return;levelPowerContext=null;
 editLevel(context.level);window.RoundOfferEditor.restore(context.roundState);
 dirty=context.dirty;if(dirty)dirtyMark();$('#editor').scrollTop=context.scroll;
}
window.LevelPowerActions={
 buttons(power){return `<div class="level-power-actions"><button type="button" class="button quiet" data-level-power="${esc(power.id)}" data-power-action="global">Edit everywhere</button><button type="button" class="button secondary" data-level-power="${esc(power.id)}" data-power-action="copy">Edit a copy for this level</button></div>`;},
 open(id,mode){
  const power=C.powers.find(p=>p.id===id);if(!power)return;
  const state=window.RoundOfferEditor.snapshot();
  const level={...state.original,...readLevel(),roundOffers:Object.values(state.offers)};
  if(mode==='copy'&&!C.levels.some(l=>l.id===level.id)){toast('Save this new level before making a level-specific power.',true);return;}
  const editingPower=mode==='copy'?window.LevelPowerLinks.makeCopy(C,id,level.id,uid('power')):clone(power);
  levelPowerContext={level,roundState:state,dirty,scroll:$('#editor').scrollTop,mode,editingPower};
  detailPower(editingPower.id,editingPower);
  $('#editor-body').insertAdjacentHTML('afterbegin',`<div class="power-edit-scope"><b>${mode==='copy'?'New copy for '+esc(levelTitle(level)):'Editing everywhere this power is used'}</b><p>${mode==='copy'?'Saving creates an independent power and replaces the original throughout this level. Other levels keep their existing power.':'Saving updates this shared power in every level that uses it. Independent level copies keep their own settings.'}</p><button type="button" class="button quiet" id="return-to-level">Back to level</button></div>`);
  $('#return-to-level').onclick=()=>returnToLevel();
  $('#power-test')?.remove();
  $('#editor-body button[type="submit"]').textContent=mode==='copy'?'Save copy for this level':'Save changes everywhere';
 }
};
const closeSharedEditor=closeEditor;
closeEditor=function(){if(levelPowerContext){returnToLevel();return;}closeSharedEditor();};
$('#close-editor').onclick=()=>closeEditor();
powers=()=>[...workspace.powers.filter(p=>!C.powers.some(q=>q.id===p.id)),...C.powers];
levels=()=>[...C.levels,...workspace.levels.filter(l=>!C.levels.some(q=>q.id===l.id))];
render=function(){originalRender();document.querySelector('.section-note')?.replaceChildren(document.createTextNode(tab==='levels'?'Edit the shared level settings. Empty reward pools keep the levelâ€™s special draft schedule.':'The sandbox evaluates structured powers. Special C# powers and bosses are not simulated.'));document.querySelectorAll('[data-level]').forEach(b=>b.textContent='Edit level');document.querySelectorAll('.level-card .pill').forEach(b=>{if(b.textContent==='GAME REFERENCE')b.textContent='SHARED GAME';});};
detailPower=function(id,supplied=null){
 const p=supplied||findPower(id);if(!p)return;if(p.source==='custom'){editPower(p);return;}
 showDialog(p.name,'power');activeId=id;
 const params=Object.entries(p.parameters||{});
 $('#editor-body').innerHTML=`<form id="native-form" class="editor-grid"><div class="fields">${input('name','Power name',p.name,'text','required maxlength="100"')}${triggerDisplayField(p)}${select('tier','Power tier',p.tier||'Basic',['Basic','Rare','Legendary'])}${section('GAME RULE')}${params.length?params.map(([k,v])=>input(k,({minimumMatch:'Minimum match size',exactMatchNumber:'Exact match size',basePointsMulti:'Extra multiplier',basePointsAdded:'Extra base points',atLeastXTiles:'Minimum match size',bonusPoints:'Extra base points',requiredTiles:p.type==='StackedOddsPower'?'Exact match size':'Minimum match size',baseMulti:p.type==='StackedOddsPower'?'Extra base points':'Extra multiplier',maxTiles:'Largest match size',plusMulti:'Extra multiplier',pointsPerTile:p.type==='BlockMatchFoundationPower'?'Block credits per tile':'Base points per tile',BasePoints:'Base points per tile',NumRequired:'Animal tiles per upgrade',BaseMulti:'Multiplier per upgrade',multi:'Extra multiplier',additiveMulti:'Extra multiplier'})[k]||k,v,'number','min="0" max="10000" step="1" required')).join(''):'<p class="hint">This power uses a special C# mechanic. Its behavior needs a code change.</p>'}${input('description','Card wording',M.clean(p.description),'textarea',p.descriptionTemplate?'readonly':'maxlength="4000" required')}<p class="hint">${p.descriptionTemplate?'Wording updates automatically when these values change.':'Keep the wording true to the existing mechanic.'}</p><label class="field inline-toggle"><span><input name="enabled" type="checkbox" ${p.enabled!==false?'checked':''}> Available in power drafts</span></label></div><aside class="preview"><div id="native-preview">${powerCard(p,false)}</div><p class="hint">${esc(p.type)} Â· ${esc(p.goals)}${p.openingOnly?' Â· Starting power only':''}</p></aside><div class="actions"><button class="button primary" type="submit">Save to game</button></div></form>`;
 const read=()=>{const f=formData($('#native-form')),q=clone(p);q.name=f.name;q.tier=f.tier;if(f.displayWhenTriggered)q.displayWhenTriggered=f.displayWhenTriggered;else delete q.displayWhenTriggered;q.enabled=!!f.enabled;q.description=f.description;for(const [k]of params)q.parameters[k]=Number(f[k]);if(q.descriptionTemplate)q.description=Object.entries(q.parameters).reduce((s,[k,v])=>s.replaceAll('{'+k+'}',v),q.descriptionTemplate);return q;};
 $('#native-form').oninput=()=>{dirtyMark();const q=read();$('#native-preview').innerHTML=powerCard(q,false);if(q.descriptionTemplate)$('#native-form [name="description"]').value=q.description;};
 $('#native-form').onsubmit=async e=>{e.preventDefault();if(!e.target.reportValidity())return;try{await persistEditedPower(read());}catch(e){toast(e.message,true);}};
};
editPower=function(p=newPower(),alreadyOpen=false){originalEditPower(p,alreadyOpen);$('#power-form .fields').insertAdjacentHTML('beforeend',triggerDisplayField(p));$('#power-form [name="goals"] option[value="Both"]')?.remove();$('#delete-design')?.remove();$('#power-form .preview .hint').textContent='These rules run in Unity and in this sandbox. Earned bonuses apply to later matches.';$('#power-form button[type="submit"]').textContent='Save to game';};
savePower=async function(test){try{if(!$('#power-form').reportValidity())return;const raw=readPower(),p={...(levelPowerContext?.editingPower||C.powers.find(q=>q.id===raw.id)),...M.validatePower(raw)};await persistEditedPower(p,test);}catch(e){toast(e.message,true);}};
const levelRuleGuide={
 LightningRods:['Lightning rods','3 x 3','Three fixed lightning rods each need 3 charges. Each matched tile directly beside a rod adds 1 charge; touching three sides can fill it in one match. Cascades count. When all three are full, lightning chains through them after the play settles and charges every animal square for 2x points until it clears. The rods reset and can charge again.'],
 MigrationSeason:['Migration season','2x','Follow the highlighted animal for 2x points. It changes after each successful move.'],
 CrackTheVault:['Crack the vault','2/SIDE','Hit every side twice to double the remaining tiles. Refills are unboosted.'],
 SteppingStones:['Stepping stones','3x','Overlap at least two glowing spaces for 3x points, then extend the path.'],
 GoldenRescue:['Golden rescue','3x','Every 2 tiles in a match cage an animal. Match beside cages to free gold tiles worth 3x block credit. The buff follows the tile.'],
 AnimalRescue:['Animal rescue','2x','Marked animals count double. Rescue three different animals for one extra move.'],
 Crosswinds:['Crosswinds','3 / 5x','Every 3 player matches, turn gravity and clear the middle cross for no points. Its cascades earn 5x.'],
 AnimalSpotlight:['Animal spotlight','2','Two animal types are dark; all others are visible. Every 5 player matches, the next two animal types become dark after the play finishes. Hidden animals still match. Cascades do not advance the counter.'],
 RollingDarkness:['Rolling darkness','1/2','Every 2 tiles in player-created matches darken 1 square after the play finishes. Odd tiles carry over. Darkness advances through alternating rows and columns, skipping already dark squares. Matches reveal replacement tiles and adjacent neighbors. Cascades reveal tiles without spreading darkness.'],
 Blackout:['Blackout','5','Every 5 player matches, unprotected squares go black. Clearing darkness colors a square green and protects it for the rest of the round. Matches reveal replacement squares and adjacent neighbors; cascades reveal squares too. Boss: only replacement squares are revealed.'],
 None:['Normal matching','4+','Standard matching, with no extra level bonus.'],
 Lightning:['Lightning','5+','Match 5+ in a straight line to clear it. Other tiles become charged for double points until cleared.'],
 ChainReaction:['Chain reaction','4x','Your matches score zero. Matches made by falling animals score 4x points.'],
 GardenPlots:['Garden plots','2x2','Match a 2x2 square to plant its spaces. Planted spaces give double points for the rest of the round.'],
 AlternatingCurrent:['Alternating current','3x','Duck and Bear take turns giving 3x points. They switch after each successful play.'],
 LeastMatched:['Least matched','2x','Your least matched animal this round gives double points. Tied animals share the bonus.'],
 BorderPatrol:['Border patrol','EDGE','Touch each outside edge to light it. Completing all four gives that match 4x points, then resets.'],
 TreasureTrail:['Treasure trail','5x','Match the golden tile. Every third treasure makes your next successful play worth 5x, including cascades.'],
 ChangingTides:['Changing tides','TURN','Gravity turns after four successful moves. The first play after each turn scores 3x, including cascades.'],
 Cascade:['Cascade bonus','+25%','Matches made by falling animals earn 25% extra points or block credit.'],
 LargeGroup:['Large-group bonus','6+','Groups of 6+ earn 50% extra points or block credit.'],
 Square:['Square bonus','2x2','Groups containing a 2x2 square earn 25% extra points or block credit.']
};
function mountLevelRuleGuides(){
 const form=$('#level-form'),mechanic=form.querySelector('[name="mechanic"]'),boss=form.querySelector('[name="bossStyle"]');
 for(const select of [mechanic,boss]){
  const help=document.createElement('div');help.className='rule-guide';help.id=select.name+'-guide';
  help.innerHTML='<div class="selected-rule" aria-live="polite"></div><details class="rule-reference"><summary>Compare '+(select===mechanic?'level rules':'boss rules')+'</summary><div class="rule-reference-list"></div></details>';
  select.closest('label').append(help);select.setAttribute('aria-describedby',help.id);
 }
 for(const option of mechanic.options)option.textContent=levelRuleGuide[option.value]?.[0]||option.textContent;
 const moveStyles=new Set(['CrowdTax','SmallChange','InnerToll','RepeatToll']);
 const context=()=>({goal:form.querySelector('[name="goal"]').value,mode:form.querySelector('[name="mode"]').value,mechanic:mechanic.value});
 function bossInfo(value){
  const b=C.bosses.find(b=>b.style===value);
  if(!b)return ['No boss','ï¿½','No extra boss rule in the final round.'];
  const description=M.bossDescription(b,context());
  return [b.name,moveStyles.has(value)?'2 moves':'BOSS',description+(moveStyles.has(value)?' Limited-move levels only.':'')];
 }
 const markup=info=>'<span class="rule-badge" aria-hidden="true">'+esc(info[1])+'</span><div><strong>'+esc(info[0])+'</strong><p>'+esc(info[2])+'</p></div>';
 const refresh=(event)=>{
  const previous=boss.value;
  const available=C.bosses.filter(b=>!M.bossCompatibility(b.style,context()));
  boss.innerHTML=selectOptions([['None','No boss'],...available.map(b=>[b.style,b.name])],previous);
  boss.value=previous==='None'||available.some(b=>b.style===previous)?previous:'None';
  if(previous!==boss.value&&previous!=='None'){
   dirtyMark();
   toast('Boss cleared: '+M.bossCompatibility(previous,context()));
  }
  boss.onchange?.();
  if(event)previewLevel();
  for(const select of [mechanic,boss]){
   const guide=document.getElementById(select.name+'-guide'),info=value=>select===mechanic?levelRuleGuide[value]:bossInfo(value);
   guide.querySelector('.selected-rule').innerHTML=markup(info(select.value));
   guide.querySelector('.rule-reference-list').innerHTML=[...select.options].map(option=>'<div class="rule-reference-item">'+markup(info(option.value))+'</div>').join('');
  }
 };
 mechanic.addEventListener('change',refresh);boss.addEventListener('change',refresh);form.querySelector('[name="goal"]').addEventListener('change',refresh);form.querySelector('[name="mode"]').addEventListener('change',refresh);
 // The selected boss explanation replaces the duplicate read-only paragraph.
 form.querySelector('[name="bossRule"]').closest('label').hidden=true;
 refresh();
}

editLevel=function(level=newLevel()){
 const original=clone(level),l={...level,source:'custom',mechanic:level.mechanic==='Custom'?'None':level.mechanic};originalEditLevel(l);activeId=original.id;
 $('#delete-design')?.remove();$('#level-form [name="basePerTile"]').step='1';
 const bossField=$('#level-form [name="bossName"]').closest('label');bossField.outerHTML=select('bossStyle','Boss rule',original.bossStyle||'None',[['None','No boss'],...C.bosses.map(b=>[b.style,b.name])]);
 $('#level-form [name="bossRule"]').readOnly=true;$('#level-form [name="mechanic"]').querySelector('[value="Custom"]').remove();$('#level-form [name="draftNotes"]').readOnly=true;
 $('#level-form .preview-label').textContent='LEVEL SUMMARY';$('#level-form button[type="submit"]').textContent='Save to game';
 window.RoundOfferEditor.mount(original);
 const setBoss=()=>{const boss=C.bosses.find(b=>b.style===$('#level-form [name="bossStyle"]').value);$('#level-form [name="bossRule"]').value=boss?M.bossDescription(boss,readLevel()):'';};setBoss();$('#level-form [name="bossStyle"]').onchange=setBoss;
 mountLevelRuleGuides();
 previewLevel();
 const levelForm=$('#level-form');
 levelForm.addEventListener('invalid',e=>{
  for(let parent=e.target.parentElement;parent&&parent!==levelForm;parent=parent.parentElement)if(parent.tagName==='DETAILS')parent.open=true;
  toast('Check '+(e.target.closest('label')?.childNodes[0]?.textContent?.trim()||e.target.name)+': '+e.target.validationMessage,true);
 },true);
 let savingLevel=false;
 levelForm.onsubmit=async e=>{e.preventDefault();if(savingLevel||!e.target.reportValidity())return;
 const button=e.target.querySelector('button[type="submit"]');savingLevel=true;button.disabled=true;button.textContent='Saving...';
 try{
  const raw=readLevel(),q=M.validateLevel({...raw,id:'custom-level-validation'},new Set(C.powers.map(p=>p.id)));
  const saved={...original,...q,id:original.id,source:original.source,number:original.number||C.levels.length+1,bossStyle:raw.bossStyle,roundDrafts:original.roundDrafts||[],roundOffers:window.RoundOfferEditor.read(),engine:original.engine||'structured'};
  await saveShared(d=>{const i=d.levels.findIndex(x=>x.id===saved.id);if(i<0)d.levels.push(saved);else d.levels[i]=saved;});workspace.levels=workspace.levels.filter(x=>x.id!==saved.id);rememberSelectedLevel(saved.id);rememberDrafts();dirty=false;$('#editor').close();render();
 }catch(e){toast(e.message,true);}finally{savingLevel=false;button.disabled=false;button.textContent='Save to game';}};
};
// Published custom powers are available to the same sandbox as local drafts.
const draftPowers=()=>powers().filter(p=>p.source==='custom');
const oldSandbox=renderSandbox;
renderSandbox=function(){const drafts=workspace.powers;workspace.powers=draftPowers();try{oldSandbox();}finally{workspace.powers=drafts;}};
sandboxResult=function(){if(!$('#sandbox-form').reportValidity())return null;const f=formData($('#sandbox-form'));return M.score(draftPowers().filter(p=>selectedSandbox.has(p.id)),{animal:f.animal,length:Number(f.length),cascade:!!f.cascade,edge:!!f.edge||!!f.corner,corner:!!f.corner,square:!!f.square},sandboxStates,Number(f.base),f.goal);};
$('.rail-note').innerHTML='<span class="dot"></span> Shared game content<p>Save powers and levels straight to the game file.</p><strong>Changes apply on the next Unity Play or build.</strong>';
$('#export').textContent='â†“ Export drafts';$('#import').textContent='â†‘ Import draft JSON';
adoptShared({document:C,revision:sharedRevision});render();
setInterval(async()=>{try{const r=await fetch('/api/content');if(!r.ok)return;const data=await r.json();if(data.revision!==sharedRevision){if($('#editor').open){$('#snapshot').textContent='Shared file changed Â· close and reload before saving';return;}adoptShared(data);render();}}catch{$('#snapshot').textContent='Disconnected Â· restart the local editor server';}},5000);

// Import portable in-game scaling edits without replacing powers or other level settings.
const importDesignDrafts = $('#import-file').onchange;
$('#import-file').onchange = async e => {
 const file=e.target.files[0];if(!file)return;
 let data;try{if(file.size>5*1024*1024)throw Error('Choose a JSON file smaller than 5 MB.');data=JSON.parse(await file.text());}
 catch(error){toast('Import failed: '+error.message,true);e.target.value='';return;}
 if(data.schema!=='prismatch-level-scaling'){await importDesignDrafts(e);return;}
 try{
  const next=window.LevelScalingImport.apply(C,data);
  if(!confirm('Save scaling changes for '+data.levels.length+' levels to the game file? A backup is created automatically.'))return;
  await saveShared(d=>Object.assign(d,next));render();
 }catch(error){toast('Import failed: '+error.message,true);}
 finally{e.target.value='';}
};
