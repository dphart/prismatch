(function(root){
 'use strict';
 const Symbols=typeof module!=='undefined'?require('./power-symbols.js'):root.PowerSymbols;
 const animals=['Bear','Sloth','Chicken','Duck','Hippo','Giraffe','Walrus','Moose','Panda','Elephant'];
 const tiers={Basic:'#42CC72',Rare:'#A56EFF',Legendary:'#FFB52E'};
 const esc=v=>String(v??'').replace(/[&<>"']/g,c=>({'&':'&amp;','<':'&lt;','>':'&gt;','"':'&quot;',"'":'&#39;'}[c]));
 const render=(text,p)=>Object.entries(p.parameters||{}).reduce((s,[key,value])=>s.replaceAll('{'+key+'}',String(value)),text||'');
 function describe(p,goal='Points',mode='Timed'){
  const blocks=goal==='Blocks',a=p.parameters||{},animal=animals.find(n=>new RegExp('\\b'+n+'\\b').test(p.name)),type=p.type;
  const text=render((blocks?p.blockDescriptionTemplate:p.pointDescriptionTemplate)||p.descriptionTemplate||p.description,p);
  const spec={text,condition:'MATCH',reward:'BONUS',animal:null,rewardAnimal:null,all:false,growth:/Growth|Mastery|Rivalry|Engine/.test(p.name),shape:'match',scope:''};
  if(type==='AnimalSpawnPower'){
   Object.assign(spec,{layout:'single',shape:'refill',rewardAnimal:animal,condition:'REFILLS + SHUFFLES',reward:'2x '+animal.toUpperCase()+' CHANCE'});
  }else if(type==='BudgetPower'){
   const edge=/edge/i.test(text),square=/square|2x2/i.test(text);
   Object.assign(spec,{condition:(edge?3:2)+' MATCHES',reward:mode==='LimitedMoves'?'+1 MOVE':'+3 SECONDS',
    shape:'budget-'+(edge?'edge':square?'square':'large'),rewardShape:mode==='LimitedMoves'?'moves':'clock',
    text:text.replace('3 seconds or 1 move',mode==='LimitedMoves'?'1 move':'3 seconds')});
  }else if(type==='AnimalPairPower'){
   const pair=(p.qualities||[]).filter(q=>q.startsWith('animal:')).map(q=>q.slice(7));
   const fallback=animals.filter(n=>new RegExp('\\b'+n+'\\b').test(p.nativeKey||p.name));
   Object.assign(spec,{condition:'MATCH TOGETHER',reward:blocks?'HALF FINAL BLOCKS':'HALF POINTS',animal:pair[0]||fallback[0],partner:pair[1]||fallback[1],shape:'pair'});
  }else if(type==='IconTypeStreakGivesBasePointsOrMulti'){
   const points=a.BasePoints>0;Object.assign(spec,{condition:a.NumRequired+' TILES',animal,rewardAnimal:blocks?animal:null,all:!blocks,
    reward:'+'+(points?a.BasePoints:a.BaseMulti)+' '+(blocks?(points?'BLOCKS':'CLEARS'):(points?'BASE / TILE':'MULTIPLIER')),scope:blocks?animal.toUpperCase()+' ONLY':'ALL MATCHES'});
  }else if(type==='AnimalMasteryPower'){
   const points=/Points/.test(p.name),rival=/Rivalry/.test(p.name);
   Object.assign(spec,{condition:(rival?(points?3:1):2)+' MATCHES',animal,rewardAnimal:animal,reward:'+1 '+(points?'BASE / TILE':'MULTIPLIER'),scope:animal.toUpperCase()+' ONLY'});
   if(!rival)Object.assign(spec,{layout:'single',shape:points?'mastery-base':'mastery-multiplier',condition:'EVERY 2 PLAYER MATCHES · '+animal.toUpperCase()+' ONLY',reward:points?'+1 BASE POINT PER TILE':'+1 MULTIPLIER'});
  }else if(type==='CollectionPower'&&/Bloom/.test(p.name)){
   Object.assign(spec,{condition:/Edge/.test(p.name)?'EDGE':/Square/.test(p.name)?'SQUARE':/Giant/.test(p.name)?'6+ TILES':'PLAYER MATCH',reward:'TURN '+(/Edge|Square|Giant/.test(p.name)?4:2)+' TILES',rewardAnimal:animal});
  }else{
   spec.condition=(text.split(/\s+(?:gains?|earns?|gives?|turns?|clears?|adds?)\b|:/i)[0]||'MATCH').replace(/\.$/,'').toUpperCase();
   const reward=text.match(/(?:gains?|earns?|gives?|turns?|clears?|adds?|scores?|:)\s*(.+?)(?:\.(?!\d)|$)/i);
   spec.reward=(reward?.[1]||text.split('\n')[0]).toUpperCase().replaceAll('BASE POINTS PER TILE','BASE / TILE').replaceAll('EXTRA BLOCK CREDITS','BLOCKS');
   if(['IconTypeGivesBase','IconTypeGivesMulti','AnimalDoublePower'].includes(type)){
    spec.condition='MATCH';spec.animal=spec.rewardAnimal=animal;
    spec.reward=type==='AnimalDoublePower'?(blocks?'+1 BLOCK / TILE':'2x BASE'):type==='IconTypeGivesBase'?'+'+a.BasePoints+' '+(blocks?'BLOCKS':'BASE / TILE'):'+'+a.multi+' '+(blocks?'CLEARS':'MULTIPLIER');
    if(type==='IconTypeGivesBase')Object.assign(spec,{layout:'single',shape:blocks?'credits':'base-badge',animal:null,rewardAnimal:animal,condition:animal.toUpperCase()+' MATCHES',reward:blocks?'+'+a.BasePoints+' BLOCKS / MATCH':'+'+a.BasePoints+'B PER TILE',rewardShape:blocks?'credits':'base-badge'});
   }
  }
  if(spec.layout!=='single'&&!['AnimalPairPower','BudgetPower','AnimalSpawnPower'].includes(type))spec.shape=/SQUARE|2X2/.test(spec.condition)?'square':/EDGE/.test(spec.condition)?'edge':/CASCADE|FALLING/.test(spec.condition)?'cascade':/6\+|6 OR MORE/.test(spec.condition)?'large':'match';
  const next={JackpotFivePower:['count-5plus','5+ TILES','factor-5'],TwinShotPower:['count-4','EXACTLY 4','factor-2'],FourFuryPower:['count-4','EXACTLY 4','factor-3'],PrecisionStrikePower:['count-3','TWO 3-TILE MATCHES IN A ROW','factor-4'],ColorFocusPower:['repeat','SAME ANIMAL TWICE','factor-2']}[type];
  if(next){spec.shape=next[0];spec.condition=next[1];spec.reward=blocks?spec.reward:'NEXT MATCH';spec.rewardShape=blocks?'clears':next[2];}
  const counted={FourExactlyPower:['exactMatchNumber',4,''],HighStakesPower:['atLeastXTiles',6,'plus'],UnderdogPower:['maxTiles',4,'max'],StackedOddsPower:['requiredTiles',4,''],MatchFoundationPower:['minimumMatch',4,'plus'],BlockMatchFoundationPower:['minimumMatch',4,'plus']}[type];
  if(counted)spec.shape='count-'+(a[counted[0]]??counted[1])+counted[2];
  if(type==='ShufflePower'){
   const identity=p.nativeKey||p.name;
   if(/Shuffle Streak/.test(identity))Object.assign(spec,{shape:'match',condition:'5 MATCHING MOVES',reward:'FREE SHUFFLE',rewardShape:'shuffle'});
   else if(/Square Ticket/.test(identity))Object.assign(spec,{shape:'square',condition:'2 SQUARE MATCHING MOVES',reward:'FREE SHUFFLE',rewardShape:'shuffle'});
   else if(/Big Ticket/.test(identity))Object.assign(spec,{shape:'large',condition:'2 MATCHING MOVES OF 6+',reward:'FREE SHUFFLE',rewardShape:'shuffle'});
   else if(/Half-Price/.test(identity))Object.assign(spec,{shape:'shuffle',condition:'SHUFFLE',reward:'HALF COST',rewardShape:'half'});
   else if(/Fresh Start/.test(identity))Object.assign(spec,{shape:'shuffle',condition:'FIRST 3 EACH ROUND',reward:'FREE SHUFFLES',rewardShape:'shuffle'});
   else if(/Shuffle Spark/.test(identity))Object.assign(spec,{shape:'shuffle',condition:'SHUFFLE',reward:blocks?'NEXT MATCH: +8 BLOCKS':'NEXT PLAY',rewardShape:blocks?'credits':'factor-2'});
   else Object.assign(spec,{shape:'shuffle',condition:'SHUFFLE',reward:blocks?'NEXT MATCH: +8 BLOCKS':'NEXT PLAY: +32 BASE',rewardShape:blocks?'credits':'base'});
  }
  if(type==='ConsistencyIsKey')spec.shape='repeat';
  if(!spec.rewardShape&&!spec.animal&&!spec.rewardAnimal&&!spec.all){
   const rule=spec.reward.toLowerCase();
   spec.rewardShape=type==='AnimalSpawnPower'?'spawn':type==='ShufflePower'?'shuffle':type==='BlockClearPower'||/clear/.test(rule)?'clears':/multiplier/.test(rule)?'multiplier':/block|credit/.test(rule)?'credits':/base/.test(rule)?'base':null;
  }
  if(!blocks&&type!=='AnimalDoublePower'&&type!=='RosterTradeoffPower'&&/\bbase\b/i.test(text)&&spec.shape!=='mastery-base'&&spec.rewardShape!=='base-badge'){
   const base=text.match(/(?:\+|gains?\s+|adds?\s+|gives?\s+|:\s*)(\d+(?:\.\d+)?)\s+(?:bonus\s+)?base/i);
   spec.baseBadge=base?'+'+base[1]+'B':'+B';
  }
  return spec;
 }
 const picture=(animal)=>'<img src="/Assets/Resources/UI/Generated/Animals/'+animal.toLowerCase()+'.png" alt="'+esc(animal)+'">';
 function graphic(animal,all,shape,reward,partner,rewardText,baseBadge){
  if(reward&&baseBadge){
   const badge=Symbols.render('base-badge',undefined,null,baseBadge);
   if(!animal&&!all&&['base','plus','reward','base-badge'].includes(shape))return badge;
   return '<span class="base-reward-composite"><span class="base-reward-scope">'+graphic(animal,all,shape,reward,partner,rewardText)+'</span><span class="base-reward-badge">'+badge+'</span></span>';
  }
  if(reward&&shape==='base-badge')return Symbols.render('base-badge',undefined,null,rewardText?.split(' ')[0]||'+B');
  if(animal&&partner)return '<span class="pact-portrait" aria-label="'+esc(animal+' and '+partner)+' are interchangeable">'+picture(animal)+Symbols.render('interchange')+picture(partner)+'</span>';
  if(animal&&!reward&&Symbols.canonical(shape)==='match')return Symbols.render('animal-match',undefined,animal);
  if(animal)return picture(animal);
  if(all)return '<span class="all-animals" role="img" aria-label="All animals">'+['Duck','Hippo','Bear','Giraffe','Sloth','Chicken'].map(picture).join('')+'</span>';
  const key=reward?(shape==='pair'?'half':/^(clock|moves|base|credits|multiplier|clears|spawn|shuffle|half|factor-\d+)$/.test(shape)?shape:'plus'):shape;
  return Symbols.render(key);
 }
 function artwork(p,spec){
  if(p.type==='IconTypeGivesBase')return '<span class="mastery-animal">'+picture(spec.rewardAnimal)+'<span class="mastery-bonus">'+Symbols.render(spec.rewardShape,undefined,null,spec.reward.split(' ')[0])+'</span></span>';
  if(spec.layout==='single'&&spec.shape.startsWith('mastery-'))return '<span class="mastery-animal">'+picture(spec.rewardAnimal)+'<span class="mastery-bonus">'+Symbols.render(spec.shape)+'</span></span>';
  if(spec.layout==='single')return '<span class="refill-animal">'+Symbols.render('refill')+picture(spec.rewardAnimal)+'</span>';
  if(p.type==='ShufflePower')return Symbols.render('shuffle');
  if(spec.partner)return graphic(spec.animal,false,'pair',false,spec.partner);
  const starter=p.type==='BasePowerUp'?'power-steady':p.type==='StarterPower'?(/Precision/.test(p.name)?'power-precision':'power-momentum'):null;
  return starter?'<img src="/Assets/Resources/UI/Generated/'+starter+'.png" alt="'+esc(p.name)+' artwork">':graphic(spec.animal||spec.rewardAnimal,false,spec.shape,false);
 }
 function card(p,goal='Points',mode='Timed'){
  const s=describe(p,goal,mode),tier=tiers[p.tier]?p.tier:'Basic';
  return '<article class="game-power-card" style="--tier:'+tiers[tier]+'"><div class="game-card-heading"><div class="game-portrait">'+artwork(p,s)+'</div><div class="game-title"><h2>'+esc(p.name)+'</h2>'+(s.scope?'<div class="game-scope">'+esc(s.scope)+'</div>':'')+'</div></div><div class="game-card-body"><p class="game-description">'+esc(s.text)+'</p>'+(s.layout==='single'?'<div class="game-single-effect" aria-label="Power effect">'+artwork(p,s)+'<strong>'+esc(s.reward)+'</strong><small>'+esc(s.condition)+'</small></div>':'<div class="game-flow" aria-label="Condition to reward"><div>'+graphic(s.animal,false,s.shape,false,s.partner)+'<strong>'+esc(s.condition)+'</strong></div><span class="flow-arrow" aria-hidden="true">'+Symbols.render('arrow')+'</span><div>'+graphic(s.rewardAnimal,s.all,s.rewardShape||s.shape,true,null,s.reward,s.baseBadge)+'<strong>'+esc(s.reward)+'</strong></div></div>')+'</div></article>';
 }
 const api={describe,card,tiers,animals};if(typeof module!=='undefined')module.exports=api;else root.CardPreview=api;
})(typeof window!=='undefined'?window:globalThis);
