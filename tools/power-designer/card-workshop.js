'use strict';
const $=s=>document.querySelector(s),catalog=window.PRISMATCH_CATALOG;
let images=[],selectedImage=null,pending=null,pendingUrl=null;
const message=(text,error=false)=>{$('#status').textContent=text;$('#status').classList.toggle('error',error);};
const option=(value,text)=>{const node=document.createElement('option');node.value=value;node.textContent=text;return node;};
function visiblePowers(){
 const level=catalog.levels.find(l=>l.id===$('#level').value),query=$('#search').value.toLowerCase();
 const candidates=level?window.RoundOffers.overview(level,catalog.powers).rounds.flatMap(r=>r.powers):catalog.powers;
 return [...new Map(candidates.map(p=>[p.id,p])).values()].filter(p=>p.name.toLowerCase().includes(query)).sort((a,b)=>a.name.localeCompare(b.name));
}
function updateList(){
 const previous=$('#power').value||new URLSearchParams(location.search).get('powerId'),list=visiblePowers();$('#power').replaceChildren(...list.map(p=>option(p.id,p.name)));
 if(list.some(p=>p.id===previous))$('#power').value=previous;
 if(!previous){const hippo=list.find(p=>p.name==='Hippo Growth (Points)');if(hippo)$('#power').value=hippo.id;}
 updateCard();
}
function updateCard(){
 const p=catalog.powers.find(p=>p.id===$('#power').value);
 $('#preview').style.setProperty('--card-width',$('#width').value+'px');$('#width-label').textContent=$('#width').value+' px';
 if(!p){$('#preview').textContent='No matching powers.';return;}
 const blocks=$('#goal').value==='Blocks';
 $('#preview').innerHTML=window.CardPreview.card(p,blocks?'Blocks':'Points',$('#budget').value);
 $('#scope-note').textContent=(blocks&&p.goals==='Points'||!blocks&&p.goals==='Blocks')?'This power is not offered for this goal. Showing its authored rule.':$('#level').value?'Configured level pool, before runtime roster and ownership checks.':'';
 for(const img of $('#preview').querySelectorAll('img')){const asset=images.find(a=>new URL(img.src).pathname===a.url);if(asset)img.src=asset.url+'?v='+asset.revision;}
}
function showGallery(){
 const query=$('#image-search').value.toLowerCase();
 $('#gallery').replaceChildren(...images.filter(a=>a.id.toLowerCase().includes(query)).map(asset=>{
  const button=document.createElement('button');button.type='button';button.classList.toggle('selected',asset.id===selectedImage?.id);
  const img=document.createElement('img');img.src=asset.url+'?v='+asset.revision;img.alt='';
  button.append(img,document.createTextNode(asset.id.replace('.png','')));button.onclick=()=>selectImage(asset);return button;
 }));
}
function clearPending(){
 pending=null;if(pendingUrl)URL.revokeObjectURL(pendingUrl);pendingUrl=null;
 $('#pending-image').hidden=true;$('#pending-image').removeAttribute('src');$('#save-image').disabled=true;$('#replacement').value='';
}
function selectImage(asset){
 clearPending();selectedImage=asset;$('#asset-name').textContent=asset.id;
 $('#asset-info').textContent=asset.width+' × '+asset.height+' px · Assets/Resources/UI/Generated/'+asset.id;
 $('#current-image').hidden=false;$('#current-image').src=asset.url+'?v='+asset.revision;$('#replacement').disabled=false;
 message('');showGallery();
}
$('#replacement').onchange=async()=>{
 const file=$('#replacement').files[0];if(!file)return;clearPending();
 try{
  if(file.size>10*1024*1024)throw Error('Choose a PNG smaller than 10 MB.');
  const signature=new Uint8Array(await file.slice(0,8).arrayBuffer());
  if([...signature].join(',')!=='137,80,78,71,13,10,26,10')throw Error('Choose a PNG image.');
  pendingUrl=URL.createObjectURL(file);const preview=new Image();preview.src=pendingUrl;await preview.decode();
  if(preview.naturalWidth!==selectedImage.width||preview.naturalHeight!==selectedImage.height)throw Error('Use a '+selectedImage.width+' × '+selectedImage.height+' PNG.');
  pending=file;$('#pending-image').src=pendingUrl;$('#pending-image').hidden=false;$('#save-image').disabled=false;message('Preview ready. Save image to apply it to the shared game asset.');
 }catch(e){clearPending();message(e.message,true);}
};
$('#save-image').onclick=async()=>{
 if(!pending||!selectedImage)return;$('#save-image').disabled=true;
 try{
  const response=await fetch('/api/artwork?id='+encodeURIComponent(selectedImage.id),{method:'PUT',headers:{'Content-Type':'image/png','If-Match':selectedImage.revision},body:pending});
  const result=await response.json();if(!response.ok)throw Error(result.error);
  images=images.map(a=>a.id===result.id?result:a);selectImage(result);updateCard();message('Image saved. Previous version backed up in outputs/artwork-backups. Rebuild the game to update installed copies.');
 }catch(e){message(e.message,true);$('#save-image').disabled=false;}
};
$('#power').onchange=updateCard;$('#goal').onchange=updateCard;$('#width').oninput=updateCard;$('#search').oninput=updateList;$('#image-search').oninput=showGallery;
$('#budget').onchange=updateCard;
$('#level').append(...catalog.levels.map(l=>option(l.id,'Level '+l.number+' · '+l.name)));
$('#level').onchange=()=>{const l=catalog.levels.find(l=>l.id===$('#level').value);if(l){$('#goal').value=l.goal==='BlocksCleared'?'Blocks':'Points';$('#budget').value=l.mode;}updateList();};
fetch('/api/artwork').then(async r=>{const result=await r.json();if(!r.ok)throw Error(result.error);images=result.images;showGallery();updateCard();}).catch(e=>message(e.message,true));
updateList();
const iconGuide=document.createElement('details');iconGuide.className='icon-guide';
const guideTitle=document.createElement('summary');guideTitle.textContent='Icon design language';iconGuide.append(guideTitle);
for(const [key,meaning]of [
 ['edge','Touch any outside edge'],['large','Six matched tiles'],['base','Added base points'],
 ['multiplier','Added multiplier'],['factor-5','Multiply score; timing is in the caption'],
 ['credits','Block credits'],['clears','Extra physical tile clears'],['clock','Extra seconds'],
 ['moves','Extra move / swap'],['interchange','Interchangeable animals'],['half','Half value or cost'],
 ['shuffle','Shuffle — same artwork as the game button']
]){
 const row=document.createElement('div');row.className='icon-guide-entry';row.innerHTML=window.PowerSymbols.render(key);
 const label=document.createElement('span');label.textContent=meaning;row.append(label);iconGuide.append(row);
}
const guideNote=document.createElement('p');guideNote.className='hint';guideNote.textContent='Captions state counts, units, scope, and timing. Growth is explained in words; no signal-bar decoration. Tier colors belong to card borders.';iconGuide.append(guideNote);
document.querySelector('.controls').append(iconGuide);
