'use strict';
const fs=require('node:fs'),path=require('node:path'),crypto=require('node:crypto');
const revision=b=>crypto.createHash('sha256').update(b).digest('hex');
function dimensions(b){
 if(b.length<33||b.subarray(0,8).toString('hex')!=='89504e470d0a1a0a'||b.toString('ascii',12,16)!=='IHDR')throw Error('Choose a PNG image.');
 const width=b.readUInt32BE(16),height=b.readUInt32BE(20);
 if(!width||!height||width>8192||height>8192)throw Error('PNG dimensions must be between 1 and 8192 pixels.');
 return {width,height};
}
function inventory(root){
 return ['','Animals'].flatMap(folder=>{
  const dir=path.join(root,folder);if(!fs.existsSync(dir))return [];
  return fs.readdirSync(dir).filter(name=>/^[a-z0-9-]+\.png$/.test(name)).map(name=>{
   const id=[folder,name].filter(Boolean).join('/'),data=fs.readFileSync(path.join(root,id));
   return {id,url:'/Assets/Resources/UI/Generated/'+id,revision:revision(data),...dimensions(data)};
  });
 });
}
function replace(root,backupDir,id,expected,data){
 const asset=inventory(root).find(a=>a.id===id);if(!asset)throw Error('Choose an existing image from the gallery.');
 if(expected!==asset.revision){const e=Error('This image changed. Reload it before saving.');e.status=409;throw e;}
 const size=dimensions(data);
 if(size.width!==asset.width||size.height!==asset.height)throw Error(`Use a ${asset.width} × ${asset.height} PNG to preserve the Unity sprite layout.`);
 const target=path.join(root,id),backup=path.join(backupDir,new Date().toISOString().replace(/[:.]/g,'-')+'-'+asset.revision.slice(0,8),id);
 fs.mkdirSync(path.dirname(backup),{recursive:true});fs.copyFileSync(target,backup);
 fs.writeFileSync(target+'.tmp',data);fs.renameSync(target+'.tmp',target);
 return {...asset,revision:revision(data),backup};
}
module.exports={inventory,replace,dimensions};
