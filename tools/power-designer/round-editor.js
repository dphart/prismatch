(function(){
 'use strict';
 const R=window.RoundOffers;
 let original,offers={},activeRound=1,searches={},expanded={};
 let condensedMatches=true;
 try{condensedMatches=localStorage.getItem(KEY+'.roundPowerView')!=='all';}catch{}
 function powerRow(p){return `<div class="inherited-power"><b>${esc(p.name)}</b><p>${esc(desc(p))}</p>${powerTags([p])}${window.LevelPowerActions?.buttons(p)||''}</div>`;}
 function matchingPowersMarkup(list){
  if(!list.length)return '<p class="warning">No powers match this group yet.</p>';
  if(!condensedMatches)return list.map(powerRow).join('');
  return M.condensePowers(list).map(f=>f.members.length===1?powerRow(f.power):`<details class="inherited-power condensed-matches"><summary><b>${esc(f.name)}</b><span class="pill family-count">${f.members.length} copies</span></summary><p>${esc(f.description)}</p>${powerTags(f.members)}<div class="animal-copies">${f.members.map(powerRow).join('')}</div></details>`).join('');
 }
 const blank=()=>({mode:'powers',count:3,powerIds:[],repeats:'PreferNew'});
 const currentLevel=()=>({...original,...readLevel()});
 function totalRounds(){return Math.max(1,Math.min(30,Number($('#level-form [name="rounds"]').value)||1));}
 function mount(level){
  original=clone(level);offers=Object.fromEntries((level.roundOffers||[]).map(o=>[o.round,clone(o)]));activeRound=1;searches={};
  const form=$('#level-form'),head=[...form.querySelectorAll('.form-section')][2];head.textContent='03 / ROUND-BY-ROUND POWERS';
  const host=document.createElement('div');host.id='round-editor';host.className='round-editor field full';head.remove();form.insertBefore(host,form.querySelector('.actions'));
  const defaults=document.createElement('details');defaults.className='round-defaults field full';defaults.innerHTML='<summary>Default pools for rounds without an override</summary><p class="hint">Keep these as the fallback. A custom round offer takes priority.</p>';
  host.after(defaults);
  for(const field of [form.querySelector('[data-pool-search="starter"]').closest('.field'),form.querySelector('[data-pool-search="reward"]').closest('.field')])defaults.append(field);
  form.querySelector('[name="draftNotes"]').closest('label').hidden=true;
  host.onclick=handleClick;host.oninput=handleInput;
  form.addEventListener('input',e=>{if(e.target.matches('[name="rounds"],[name="goal"],[name="mode"],[name="starter"],[name="rewardPool"]'))draw();});
  draw();
 }
 function defaultSummary(n){
  const l=currentLevel(),pool=n===1?l.starters:l.rewards;
  if(pool.length)return `${Math.min(3,pool.length)} choices from the ${n===1?'starting':'reward'} pool`;
  const draft=original.roundDrafts?.find(d=>d.round===n&&d.slots.length);
  return draft?draft.label:'Standard compatible powers';
 }
 function summary(n){const o=offers[n];return o?`${o.slots.reduce((n,s)=>n+(s.count||0),0)} choices · ${o.slots.map(s=>s.mode==='powers'?'picked powers':'filters').join(' + ')}`:defaultSummary(n);}
 function inheritedOffer(n){return R.inherited({...original,...currentLevel()},n,C.powers);}
 function inheritedMarkup(n){
  const offer=inheritedOffer(n),level=currentLevel();
  if(!offer)return '<p class="hint">The game uses its standard compatible catalog for this round.</p>';
  return `<div class="inherited-powers"><p class="round-default-title">${esc(offer.label)}</p>${offer.slots.map((slot,i)=>{
   const list=R.candidates(slot,n,level,C.powers);
   return `<section class="inherited-power-group"><h4>${offer.slots.length>1?'Group '+(i+1)+' · ':''}Choose ${slot.count} from ${list.length} powers</h4>${matchingPowersMarkup(list)}</section>`;
  }).join('')}</div>`;
 }
 function draw(){
  document.querySelectorAll('[data-round-filter-key]').forEach(el=>expanded[el.dataset.roundFilterKey]=el.open);
  const count=totalRounds(),last=Math.max(count,...Object.keys(offers).map(Number)),o=offers[activeRound];
  $('#round-editor').innerHTML=`<div class="form-section">03 / ROUND-BY-ROUND POWERS</div><details class="level-variety-editor"><summary>Level power variety and repetition</summary><div id="level-variety-report">${levelPowerOverviewMarkup({...currentLevel(),roundOffers:Object.values(offers)})}</div></details><p class="hint">Pick a round. Choose specific powers, use filters, or mix groups. The game picks distinct cards from each group.</p><div class="round-tabs" role="group" aria-label="Choose a round">${Array.from({length:last},(_,i)=>i+1).map(n=>`<button type="button" class="round-tab ${n===activeRound?'active':''}" data-edit-round="${n}" aria-pressed="${n===activeRound}">Round ${n}${offers[n]?' •':''}${n>count?' (outside level)':''}</button>`).join('')}</div><label class="matching-view-control">Matching powers view<select data-matching-view aria-label="Matching powers view">${selectOptions([['condensed','Condensed'],['all','All powers']],condensedMatches?'condensed':'all')}</select></label><div class="round-heading"><h3>Round ${activeRound}${activeRound===1?' · Starting powers':''}</h3><label class="inline-toggle"><input type="checkbox" data-round-override ${o?'checked':''}> Set this round’s offer</label></div>${o?`<div class="round-title">${input('roundOfferTitle','Choice screen title',o.label,'text','maxlength="100" required data-offer-title')}</div><div class="round-groups">${o.slots.map(groupMarkup).join('')}</div><div class="round-actions"><button type="button" class="button secondary" data-add-group ${o.slots.length>=3?'disabled':''}>+ Add a power group</button>${activeRound<count?'<button type="button" class="button quiet" data-copy-round>Copy to next round</button>':''}<button type="button" class="button quiet" data-reset-round>Use level default</button></div><div id="round-health" aria-live="polite"></div>`:`<div class="round-inherited"><b>Using the level default</b><p>${esc(defaultSummary(activeRound))}</p>${inheritedMarkup(activeRound)}<p class="hint">Turn on “Set this round’s offer” to change this round.</p></div>`}<p class="hint">Starting powers stay in round 1. The game also checks the current animals, match rules, and powers the player owns. If a group runs short, it shows the available cards without filling from unrelated powers.</p><details class="round-overview"><summary>All rounds at a glance</summary><div id="round-overview-list"></div></details>`;
  refreshPreview();
 }
 function groupMarkup(s,i){
  const l=currentLevel(),eligible=R.candidates({mode:'filters',match:'all',filters:{}},activeRound,l,C.powers),list=eligible; 
  return `<section class="round-group" data-round-group="${i}"><div class="round-group-heading"><h4>Group ${i+1}</h4>${offers[activeRound].slots.length>1?`<button type="button" class="button quiet" data-remove-group="${i}">Remove group</button>`:''}</div><div class="round-group-controls"><label class="field">Cards to offer<input type="number" min="1" max="3" step="1" value="${s.count}" required data-group-count="${i}"></label><label class="field">Choose from<select data-group-mode="${i}">${selectOptions([['powers','Specific powers'],['filters','Powers matching filters']],s.mode)}</select></label><label class="field">Owned powers<select data-group-repeats="${i}">${selectOptions([['PreferNew','Prefer powers not owned'],['NewOnly','Only powers not owned']],s.repeats||'PreferNew')}</select></label></div>${s.mode==='powers'?`<input type="search" aria-label="Search powers for group ${i+1}" placeholder="Find powers…" value="${esc(searches[activeRound+':'+i]||'')}" data-round-search="${i}"><div class="check-list round-picks">${list.map(p=>`<label class="check-row" data-round-pick-row data-search="${esc((p.name+' '+desc(p)).toLowerCase())}"><input type="checkbox" data-pick-group="${i}" value="${esc(p.id)}" ${(s.powerIds||[]).includes(p.id)?'checked':''}><span>${esc(p.name)}</span><small>${p.openingOnly?'STARTER':p.goals}</small></label>`).join('')}</div>`:`<label class="filter-mode">Show powers matching <select data-group-match="${i}">${selectOptions([['all','all included qualities'],['any','any included quality']],s.match||'all')}</select></label><p class="hint">Click to include, click again to exclude, then click to clear. Exclusions always win.</p><div class="round-active-filters">${Object.entries(s.filters||{}).map(([key,state])=>`<button type="button" class="filter-chip ${state}" data-clear-round-quality="${esc(key)}" data-quality-group="${i}">${state==='include'?'+ Include':'− Exclude'} ${esc(new Map(R.groups(C.powers,Object.keys(s.filters||{})).flatMap(g=>g.items)).get(key)||key)} ×</button>`).join('')||'<span class="hint">No filters: all compatible powers.</span>'}</div>${R.groups(C.powers,Object.keys(s.filters||{})).map(g=>`<details class="round-quality-group" data-round-filter-key="${activeRound}:${i}:${esc(g.name)}" ${(expanded[activeRound+':'+i+':'+g.name]??g.name==='What it gives')?'open':''}><summary>${esc(g.name)}</summary><div class="quality-list">${g.items.map(([key,label])=>{const state=s.filters?.[key]||'off';return `<button type="button" class="quality ${state}" data-round-quality="${esc(key)}" data-quality-group="${i}" aria-label="Group ${i+1}: ${esc(label)}: ${state}" aria-pressed="${state!=='off'}">${state==='include'?'+':state==='exclude'?'−':'○'} ${esc(label)}</button>`;}).join('')}</div></details>`).join('')}`}<div class="round-matches" id="round-matches-${i}" aria-live="polite"></div></section>`;
 }
 function refreshPreview(){
  const o=offers[activeRound],l=currentLevel();
  $('#level-variety-report').innerHTML=levelPowerOverviewMarkup({...l,roundOffers:Object.values(offers)});
  if(o){
   for(let i=0;i<o.slots.length;i++){
    const list=R.candidates(o.slots[i],activeRound,l,C.powers);
    const expanded=$('#round-matches-'+i).querySelector('details')?.open??true;
    const count=condensedMatches?M.condensePowers(list).length+' families / '+list.length+' powers':list.length+' matching powers';
    $('#round-matches-'+i).innerHTML=`<details ${expanded?'open':''}><summary>${count} ? choose ${o.slots[i].count}</summary><div>${matchingPowersMarkup(list)}</div></details>`;
   }
   const p=R.preview(o,l,C.powers);
   $('#round-health').innerHTML=p.requested>3?'<div class="warning">Choose at most 3 cards across all groups.</div>':p.filled<p.requested?`<div class="warning">Only ${p.filled} distinct cards fit this offer. You asked for ${p.requested}. Add powers or broaden a filter.</div>`:`<div class="round-ready">${p.requested} choices from ${p.unique} matching powers. Current game rules may narrow this pool.</div>`;
  }
  $('#round-overview-list').innerHTML=Array.from({length:Math.max(totalRounds(),...Object.keys(offers).map(Number))},(_,i)=>`<p><b>Round ${i+1}</b> ${esc(summary(i+1))}</p>`).join('');
  for(const el of document.querySelectorAll('[data-round-search]'))filterSearch(Number(el.dataset.roundSearch),el.value);
 }
 function filterSearch(i,text){const group=$(`[data-round-group="${i}"]`);group?.querySelectorAll('[data-round-pick-row]').forEach(row=>row.hidden=!row.dataset.search.includes(text.toLowerCase()));}
 function handleInput(e){
  e.stopPropagation();const t=e.target,o=offers[activeRound];
  if(t.matches('[data-matching-view]')){condensedMatches=t.value==='condensed';try{localStorage.setItem(KEY+'.roundPowerView',t.value);}catch{}if(o)refreshPreview();else draw();return;}
  if(t.matches('[data-round-search]')){searches[activeRound+':'+t.dataset.roundSearch]=t.value;filterSearch(Number(t.dataset.roundSearch),t.value);return;}
  if(t.matches('[data-round-override]')){if(t.checked)offers[activeRound]={round:activeRound,label:'PICK A POWER',slots:[blank()],...inheritedOffer(activeRound)};else delete offers[activeRound];dirtyMark();draw();return;}
  if(!o)return;
  if(t.matches('[data-offer-title]'))o.label=t.value;
  if(t.matches('[data-group-count]'))o.slots[Number(t.dataset.groupCount)].count=Math.max(0,Math.min(3,Number(t.value)||0));
  if(t.matches('[data-group-mode]')){const s=o.slots[Number(t.dataset.groupMode)];s.mode=t.value;s.powerIds??=[];s.filters??={};s.match??='all';dirtyMark();draw();return;}
  if(t.matches('[data-group-repeats]'))o.slots[Number(t.dataset.groupRepeats)].repeats=t.value;
  if(t.matches('[data-group-match]'))o.slots[Number(t.dataset.groupMatch)].match=t.value;
  if(t.matches('[data-pick-group]')){const s=o.slots[Number(t.dataset.pickGroup)],set=new Set(s.powerIds);if(t.checked)set.add(t.value);else set.delete(t.value);s.powerIds=[...set];}
  dirtyMark();refreshPreview();
 }
 function handleClick(e){
  const t=e.target.closest('button');if(!t)return;
  e.stopPropagation();
  if(t.matches('[data-level-power]')){window.LevelPowerActions.open(t.dataset.levelPower,t.dataset.powerAction);return;}
  if(t.matches('[data-edit-round]')){activeRound=Number(t.dataset.editRound);draw();return;}
  const o=offers[activeRound];if(!o)return;
  if(t.matches('[data-clear-round-quality]'))delete o.slots[Number(t.dataset.qualityGroup)].filters[t.dataset.clearRoundQuality];
  else if(t.matches('[data-round-quality]')){const s=o.slots[Number(t.dataset.qualityGroup)],key=t.dataset.roundQuality;s.filters??={};if(s.filters[key]==='exclude')delete s.filters[key];else s.filters[key]=s.filters[key]==='include'?'exclude':'include';}
  else if(t.matches('[data-add-group]')){if(o.slots.length>=3)return;const total=o.slots.reduce((n,s)=>n+s.count,0);if(total>=3){const s=o.slots.find(s=>s.count>1);if(s)s.count--;}o.slots.push({mode:'filters',count:1,filters:{},match:'all',repeats:'PreferNew'});}
  else if(t.matches('[data-remove-group]'))o.slots.splice(Number(t.dataset.removeGroup),1);
  else if(t.matches('[data-reset-round]'))delete offers[activeRound];
  else if(t.matches('[data-copy-round]')){offers[activeRound+1]={...clone(o),round:activeRound+1};activeRound++;}
  else return;
  dirtyMark();draw();
 }
 function read(){return R.validate(Object.values(offers),totalRounds(),C.powers);}
 function snapshot(){return {original:clone(original),offers:clone(offers),activeRound,searches:clone(searches),expanded:clone(expanded)};}
 function restore(state){offers=clone(state.offers);activeRound=state.activeRound;searches=clone(state.searches);expanded=clone(state.expanded);draw();}
 window.RoundOfferEditor={mount,read,snapshot,restore,matchingPowersMarkup};
})();
