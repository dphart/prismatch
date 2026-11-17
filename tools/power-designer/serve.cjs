'use strict';
const http=require('node:http'),fs=require('node:fs'),path=require('node:path'),crypto=require('node:crypto'),schema=require('./shared-schema.cjs');
const root=path.resolve(__dirname,'../..');
const artwork=require('./artwork.cjs');
function createServer({port=8787,contentFile=path.join(root,'Assets/Resources/Content/prismatch.json'),backupDir=path.join(root,'outputs/shared-content-backups'),artRoot=path.join(root,'Assets/Resources/UI/Generated')}={}){
 const current=()=>{const text=fs.readFileSync(contentFile,'utf8');return {document:JSON.parse(text),revision:crypto.createHash('sha256').update(text).digest('hex')};};
 const send=(res,status,value,type='application/json')=>res.writeHead(status,{'Content-Type':type,'Cache-Control':'no-store','X-Content-Type-Options':'nosniff'}).end(type==='application/json'?JSON.stringify(value):value);
 return http.createServer(async(req,res)=>{try{
  if(!['127.0.0.1:'+port,'localhost:'+port].includes(req.headers.host))return send(res,403,{error:'Local access only.'});
  const url=decodeURIComponent(new URL(req.url,'http://localhost').pathname);
  if(url==='/api/artwork'&&req.method==='GET')return send(res,200,{images:artwork.inventory(artRoot)});
  if(url==='/api/artwork'&&req.method==='PUT'){
   if(!['http://127.0.0.1:'+port,'http://localhost:'+port].includes(req.headers.origin)||req.headers['content-type']!=='image/png')return send(res,403,{error:'Save PNG images from the local editor.'});
   const chunks=[];let size=0;for await(const chunk of req){size+=chunk.length;if(size>10*1024*1024)throw Error('Choose a PNG smaller than 10 MB.');chunks.push(chunk);}
   const id=new URL(req.url,'http://localhost').searchParams.get('id');
   return send(res,200,artwork.replace(artRoot,path.join(path.dirname(backupDir),'artwork-backups'),id,req.headers['if-match'],Buffer.concat(chunks)));
  }
  if(url==='/api/content'&&req.method==='GET')return send(res,200,current());
  if(url==='/tools/power-designer/power-icons.js')return send(res,200,'window.PRISMATCH_POWER_ICONS='+fs.readFileSync(path.join(root,'Assets/Resources/Content/power-icons.json'),'utf8')+';','text/javascript');
  if(url==='/api/content'&&req.method==='PUT'){
   if(!['http://127.0.0.1:'+port,'http://localhost:'+port].includes(req.headers.origin)||req.headers['content-type']!=='application/json')return send(res,403,{error:'Save from the local editor.'});
   let body='';for await(const chunk of req){body+=chunk;if(body.length>3*1024*1024)throw Error('Document too large.');}
   const input=JSON.parse(body),before=current();
   const recoveryDir=path.join(path.dirname(backupDir),'designer-save-recovery');
   fs.mkdirSync(recoveryDir,{recursive:true});
   fs.writeFileSync(path.join(recoveryDir,'latest-save-attempt.json'),JSON.stringify({savedAt:new Date().toISOString(),...input},null,2)+'\n');
   if(input.revision!==before.revision)return send(res,409,{error:'The shared file changed. Reload the page before saving; your open edits have not been applied.'});
   const next=schema.validate(input.document,before.document);next.updatedAt=new Date().toISOString();
   fs.mkdirSync(backupDir,{recursive:true});fs.copyFileSync(contentFile,path.join(backupDir,Date.now()+'-'+before.revision.slice(0,8)+'.json'));
   fs.writeFileSync(contentFile+'.tmp',JSON.stringify(next,null,2)+'\n');fs.renameSync(contentFile+'.tmp',contentFile);
   return send(res,200,current());
  }
  if(req.method!=='GET')return send(res,405,{error:'Unsupported request.'});
  if(url==='/'){res.writeHead(302,{Location:'/tools/power-designer/index.html'}).end();return;}
  if(url==='/tools/power-designer/catalog.js'){const c=current();return send(res,200,'window.PRISMATCH_CATALOG='+JSON.stringify(c.document)+';window.PRISMATCH_SHARED_REVISION='+JSON.stringify(c.revision)+';','text/javascript');}
  const relative=url.replace(/^\//,''),allowed=/^tools\/power-designer\/(index.html|studio.css|studio.js|shared.js|model.js|round-offers.js|round-editor.js|power-links.js|scaling-import.js|card-workshop.html|card-workshop.css|card-workshop.js|card-preview.js|power-symbols.js)$/.test(relative)||/^Assets\/Resources\/UI\/Generated\/(Animals\/)?[a-z0-9-]+\.png$/.test(relative),file=path.resolve(root,relative);
  if(!allowed||!file.startsWith(root+path.sep)||!fs.existsSync(file))return send(res,404,{error:'Not found.'});
  const mime={'.html':'text/html','.css':'text/css','.js':'text/javascript','.png':'image/png'};
  send(res,200,fs.readFileSync(file),mime[path.extname(file)]);
 }catch(e){send(res,e.status||400,{error:e.message});}});
}
if(require.main===module){const port=Number(process.env.PRISMATCH_DESIGNER_PORT||8787);createServer({port}).listen(port,'127.0.0.1',()=>console.log('Prismatch shared editor: http://127.0.0.1:'+port+'/'));}
module.exports={createServer};
