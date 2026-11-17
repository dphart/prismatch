(function(root){
 'use strict';
 const definitions=typeof module!=='undefined'?require('../../Assets/Resources/Content/power-icons.json'):root.PRISMATCH_POWER_ICONS;
 const esc=v=>String(v).replace(/[&<>"']/g,c=>({'&':'&amp;','<':'&lt;','>':'&gt;','"':'&quot;',"'":'&#39;'}[c]));
 const canonical=key=>key.startsWith('budget-')?key.slice(7):key==='reward'?'plus':key==='any'?'match':key;
 function render(key,source=definitions,animal=null,textOverride=null){
  key=canonical(key);const marks=source.symbols[key]||source.symbols.match;
  return '<svg class="shared-power-symbol" data-power-symbol="'+esc(key)+'" viewBox="0 0 100 100" role="img" aria-label="'+esc(key)+'">'+marks.map(m=>{
   const n=v=>Number(v)*100;
   if(m.animal)return animal?'<image href="/Assets/Resources/UI/Generated/Animals/'+esc(animal.toLowerCase())+'.png" x="'+n(m.x)+'" y="'+n(m.y)+'" width="'+n(m.w)+'" height="'+n(m.h)+'" preserveAspectRatio="xMidYMid meet"/>':'';
   if(m.image)return '<image href="/Assets/Resources/'+esc(m.image)+'.png" x="'+n(m.x)+'" y="'+n(m.y)+'" width="'+n(m.w)+'" height="'+n(m.h)+'" preserveAspectRatio="xMidYMid meet"/>';
   const color='#'+source.palette[m.color];
   if(m.text)return '<text x="'+n(m.x+m.w/2)+'" y="'+n(m.y+m.h/2)+'" dominant-baseline="central" text-anchor="middle" font-size="30" font-weight="800" fill="'+esc(color)+'">'+esc(m.text==='{value}'?(textOverride||'+B'):m.text)+'</text>';
   return '<rect x="'+n(m.x)+'" y="'+n(m.y)+'" width="'+n(m.w)+'" height="'+n(m.h)+'" rx="'+n(m.rounding||0)+'" fill="'+esc(color)+'" transform="rotate('+Number(m.angle||0)+' '+n(m.x+m.w/2)+' '+n(m.y+m.h/2)+')"/>';
  }).join('')+'</svg>';
 }
 const api={canonical,render,definitions};if(typeof module!=='undefined')module.exports=api;else root.PowerSymbols=api;
})(typeof window!=='undefined'?window:globalThis);
