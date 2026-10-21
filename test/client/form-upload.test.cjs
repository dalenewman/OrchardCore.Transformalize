const test=require('node:test');
const assert=require('node:assert/strict');
const fs=require('node:fs');
const path=require('node:path');
const vm=require('node:vm');
function setup(scan=false, arrangementSrc=null){
 const events=[],changes=[],created=[],revoked=[],confirmations=[]; let callbacks, destroyed=0, approveClear=true;
 class PreviewURL extends URL{static createObjectURL(file){created.push(file);return 'blob:preview-'+created.length;}static revokeObjectURL(url){revoked.push(url);}}
 const rawPreview={src:arrangementSrc,alt:'',style:'width:200px',hidden:false,listeners:new Map(),getAttribute(name){return this[name]??null;},removeAttribute(name){this[name]='';},addEventListener(name,handler){this.listeners.set(name,handler);},removeEventListener(name){this.listeners.delete(name);}};
 const context={Stimulus:{Controller:class{}},window:{confirm(message){confirmations.push(message);return approveClear;}},URL:PreviewURL,Event:class{constructor(type,options){this.type=type;this.bubbles=options.bubbles;}},TransformalizeUploadWidgets:{connect(input,options,cb){callbacks=cb;return{disconnect(){destroyed++;}};}}};
 vm.runInNewContext(fs.readFileSync(path.join(__dirname,'../../src/OrchardCore.Transformalize/wwwroot/Scripts/form-upload-controller.js'),'utf8'),context);
 const c=new context.window.TransformalizeFormUploadController();
 c.scanValue=scan;c.urlValue='/upload';c.nameValue='Evidence';c.element={ownerDocument:{baseURI:'http://localhost/form'},closest:()=>({getAttribute:()=>null,querySelectorAll:()=>arrangementSrc?[rawPreview]:[],addEventListener(){},removeEventListener(){}})};
 ['input','value','display','choose','clear','retry','progress','status','preview'].forEach(name=>c[name+'Target']={value:'',attributes:{},getAttribute(name){return this.attributes[name]??this[name]??null;},setAttribute(name,value){this.attributes[name]=value;},removeAttribute(name){delete this.attributes[name];if(name==='src')this.src='';},classList:{toggle(){}},dispatchEvent:e=>changes.push(e)});
 if(arrangementSrc)c.previewTarget.src='/t/file/stored-id';c.hasPreviewTarget=true;c.hasDisplayTarget=!scan;c.dispatch=(name,options)=>events.push({name,...options});c.connect();
 return{c,events,changes,created,revoked,confirmations,rawPreview,declineClear(){approveClear=false;},get callbacks(){return callbacks;},get destroyed(){return destroyed;}};
}
test('uploads block during processing, then publish only the canonical file ID',()=>{
 const s=setup();const job={};s.callbacks.selected(job);
 assert.equal(s.c.inputTarget.disabled,true);
 assert.equal(s.events[0].detail.blocked,true);
 s.callbacks.completed(job,{id:'stored-id',message:'a "quoted" image.jpg'});
 assert.equal(s.c.valueTarget.value,'stored-id');assert.equal(s.c.displayTarget.value,'a "quoted" image.jpg');
 assert.equal(s.c.inputTarget.value,'');assert.equal(s.changes.length,1);assert.equal(s.events.at(-1).detail.blocked,false);
});
test('HTTP and malformed responses retain a retryable selection and block save',()=>{
 const s=setup();let retries=0;s.callbacks.selected({retry(){retries++;}});
 s.callbacks.failed({},'Failed');assert.equal(s.c.retryTarget.hidden,false);assert.equal(s.c.inputTarget.disabled,true);
 assert.equal(s.events.length,1);s.c.retry();assert.equal(retries,1);
 s.c.completed({id:'',message:'No result'});assert.equal(s.events.length,1);
 s.c.clear();assert.equal(s.events.at(-1).detail.blocked,false);assert.equal(s.c.valueTarget.value,'');
});
test('clearing a pending upload aborts and reconnects its adapter',()=>{
 const s=setup();s.callbacks.selected({});s.c.clear();assert.equal(s.destroyed,1);
 assert.equal(s.c.pending,false);assert.equal(s.c.inputTarget.disabled,false);assert.equal(s.events.at(-1).detail.blocked,false);
 s.c.disconnect();assert.equal(s.destroyed,2);
});
test('scan failure offers manual entry; scan success posts decoded text',()=>{
 const s=setup(true);s.callbacks.selected({});s.callbacks.failed({},'Upload failed.');
 assert.match(s.c.statusTarget.textContent,/enter the value manually/);assert.equal(s.c.valueTarget.readOnly,true);
 s.c.clear();assert.equal(s.c.valueTarget.readOnly,false);s.callbacks.selected({});s.callbacks.completed({},{id:'scan-result',message:'123456'});
 assert.equal(s.c.valueTarget.value,'123456');assert.equal(s.c.inputTarget.disabled,false);
});
test('validation requests disable choosers and reject late selections',()=>{
 const s=setup();s.c.busyChanged({detail:{busy:true}});assert.equal(s.c.inputTarget.disabled,true);
 assert.equal(s.c.selected({}),false);assert.equal(s.events.length,0);
 s.c.busyChanged({detail:{busy:false}});assert.equal(s.c.inputTarget.disabled,false);
});

test('the native Upload button stays in the Tab order during validation and opens the picker when ready',()=>{
 const s=setup();let opens=0;s.c.inputTarget.click=()=>opens++;
 s.c.choose();assert.equal(opens,1);
 s.c.busyChanged({detail:{busy:true}});
 assert.equal(s.c.chooseTarget.disabled,false);
 assert.equal(s.c.chooseTarget.attributes['aria-disabled'],'true');
 s.c.choose();assert.equal(opens,1,'pending validation must not open a picker whose selection would be lost');
 s.c.busyChanged({detail:{busy:false}});
 assert.equal(s.c.chooseTarget.attributes['aria-disabled'],'false');
 s.c.choose();assert.equal(opens,2);
});

test('an existing attachment keeps Clear keyboard reachable during validation and prevents accidental replacement',()=>{
 const s=setup();let opens=0;s.c.inputTarget.click=()=>opens++;
 s.c.valueTarget.value='stored-id';s.c.render();
 assert.equal(s.c.chooseTarget.disabled,true);assert.equal(s.c.clearTarget.hidden,false);
 s.c.busyChanged({detail:{busy:true}});
 assert.notEqual(s.c.clearTarget.disabled,true);assert.equal(s.c.clearTarget.attributes['aria-disabled'],'true');
 s.c.clear();s.c.choose();assert.equal(opens,0);assert.equal(s.c.valueTarget.value,'stored-id');
 s.c.busyChanged({detail:{busy:false}});s.c.clear();
 assert.equal(s.c.chooseTarget.disabled,false);s.c.choose();assert.equal(opens,1);
});


test('an image preview appears before processing completes and stays through upload completion',()=>{
 const s=setup(),file={name:'photo.jpg',type:'image/jpeg'};
 s.callbacks.selected({file});
 assert.equal(s.c.pending,true);assert.equal(s.c.valueTarget.value,'');
 assert.equal(s.c.previewTarget.hidden,false);assert.equal(s.c.previewTarget.src,'blob:preview-1');
 assert.equal(s.c.previewTarget.alt,'Preview of photo.jpg');
 s.callbacks.completed({}, {id:'stored-id',message:'photo.jpg'});
 assert.equal(s.c.previewTarget.hidden,false);assert.equal(s.c.previewTarget.src,'blob:preview-1');
 assert.equal(s.c.valueTarget.value,'stored-id');assert.deepEqual(s.revoked,[]);
 s.c.disconnect();assert.deepEqual(s.revoked,['blob:preview-1']);
});

test('Clear confirmation cancellation preserves the stored ID, preview, and pending work',()=>{
 for(const pending of [false,true]) {
  const s=setup();s.c.valueTarget.value='stored-id';s.c.displayTarget.value='photo.jpg';
  s.callbacks.selected({file:{name:'photo.jpg',type:'image/jpeg'}});
  if(!pending)s.callbacks.completed({}, {id:'stored-id',message:'photo.jpg'});
  s.declineClear();s.c.clear();
  assert.equal(s.confirmations.length,1);assert.match(s.confirmations[0],/will not delete/);
  assert.equal(s.c.valueTarget.value,'stored-id');assert.equal(s.c.previewTarget.hidden,false);
  assert.equal(s.c.pending,pending);assert.equal(s.destroyed,0);assert.deepEqual(s.revoked,[]);
 }
});

test('confirmed Clear empties the canonical ID and preview, posts one change, and keeps keyboard focus on Upload',()=>{
 const s=setup();s.callbacks.selected({file:{name:'photo.jpg',type:'image/jpeg'}});
 s.callbacks.completed({}, {id:'stored-id',message:'photo.jpg'});
 s.c.element.ownerDocument={activeElement:s.c.clearTarget};let focused=0;s.c.chooseTarget.focus=()=>focused++;
 s.c.clear();
 assert.equal(s.c.valueTarget.value,'');assert.equal(s.c.displayTarget.value,'');assert.equal(s.c.inputTarget.value,'');
 assert.equal(s.c.previewTarget.hidden,true);assert.equal(s.c.previewTarget.src,'');
 assert.deepEqual(s.revoked,['blob:preview-1']);assert.equal(s.changes.length,2);
 assert.equal(s.events.at(-1).detail.blocked,false);assert.equal(focused,1);
});

test('retry replaces its local preview, non-images do not preview, and broken images hide without blocking upload',()=>{
 const s=setup();const file={name:'photo.jpg',type:'image/jpeg'};
 s.callbacks.selected({file});s.callbacks.failed({},'Failed');s.callbacks.selected({file});
 assert.deepEqual(s.revoked,['blob:preview-1']);assert.equal(s.c.previewTarget.src,'blob:preview-2');
 s.c.previewFailed();assert.equal(s.c.previewTarget.hidden,true);assert.equal(s.c.pending,true);
 assert.deepEqual(s.revoked,['blob:preview-1','blob:preview-2']);
 s.callbacks.failed({},'Failed');s.callbacks.selected({file:{name:'notes.txt',type:'text/plain'}});
 assert.equal(s.c.previewTarget.hidden,true);assert.equal(s.created.length,2);
});


test('an arrangement image for the same file is reused with one visible preview and its existing styling',()=>{
 const s=setup(false,'http://localhost/t/file/stored-id');
 assert.equal(s.c.previewTarget.hidden,true);assert.equal(s.rawPreview.hidden,false);
 assert.equal(s.c.previewElement,s.rawPreview);assert.equal(s.rawPreview.style,'width:200px');
 s.c.valueTarget.value='stored-id';s.c.render();s.c.clear();
 assert.equal(s.rawPreview.hidden,true);assert.equal(s.rawPreview.src,'');
 s.callbacks.selected({file:{name:'new.jpg',type:'image/jpeg'}});
 assert.equal(s.rawPreview.hidden,false);assert.equal(s.rawPreview.src,'blob:preview-1');
 assert.equal(s.c.previewTarget.hidden,true);s.c.disconnect();
 assert.equal(s.rawPreview.listeners.size,0);assert.deepEqual(s.revoked,['blob:preview-1']);
});

test('an unrelated decorative image is left untouched and the upload keeps its own preview',()=>{
 const s=setup(false,'/t/file/different-file');
 assert.equal(s.c.previewElement,s.c.previewTarget);assert.equal(s.rawPreview.src,'/t/file/different-file');
 s.callbacks.selected({file:{name:'new.jpg',type:'image/jpeg'}});
 assert.equal(s.c.previewTarget.hidden,false);assert.equal(s.rawPreview.src,'/t/file/different-file');
});
