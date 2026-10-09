const test=require('node:test');
const assert=require('node:assert/strict');
const fs=require('node:fs');
const path=require('node:path');
const vm=require('node:vm');
function setup(){
 let options;const events=[],calls=[];
 const plugin={fileupload(method,data){if(typeof method==='object')options=method;else calls.push({method,data});return this;}};
 const context={window:{},$:()=>plugin};
 vm.runInNewContext(fs.readFileSync(path.join(__dirname,'../../src/OrchardCore.Transformalize/wwwroot/Scripts/form-upload-widgets.js'),'utf8'),context);
 const adapter=context.window.TransformalizeUploadWidgets.connect({}, {url:'/upload',name:'Evidence'}, {
  selected:job=>{events.push(['selected',job]);return true;},progress:(job,value)=>events.push(['progress',value]),completed:(job,value)=>events.push(['completed',value]),failed:(job,value)=>events.push(['failed',value])
 });
 function data(){let done,fail;const d={files:[{name:'original.jpg'}],submits:0,aborts:0,process(fn){fn();return{done(callback){done=callback;return this;},fail(callback){fail=callback;return this;}};},submit(){this.submits++;},abort(){this.aborts++;options.fail(null,this);},ready:()=>done(),reject:()=>fail()};return d;}
 return{adapter,options,events,calls,data};
}
test('selection blocks before processing and upload starts only after processing completes',()=>{
 const s=setup(),d=s.data();s.options.add(null,d);
 assert.equal(s.events[0][0],'selected');assert.equal(s.events[0][1].file,d.files[0],'preview receives the original file before asynchronous processing');assert.equal(s.calls[0].method,'process');assert.equal(d.submits,0);
 d.ready();assert.equal(d.submits,1);s.options.done(null,{...d,result:{id:'stored',message:'original.jpg'}});
 s.options.fail(null,d);assert.deepEqual(s.events.map(e=>e[0]),['selected','completed']);
});
test('retry uses the original image rather than resizing the processed blob twice',()=>{
 const s=setup(),d=s.data();const original=d.files[0];s.options.add(null,d);
 d.files[0]={name:'resized.jpg'};d.ready();s.options.fail(null,d);d.tflJob.retry();
 assert.equal(s.calls.at(-1).method,'add');assert.equal(s.calls.at(-1).data.files[0],original);
});
test('disconnect aborts and destroys the widget, ignoring asynchronous processing and network callbacks',()=>{
 const s=setup(),d=s.data();s.options.add(null,d);s.adapter.disconnect();d.ready();
 s.options.done(null,{...d,result:{id:'late'}});assert.equal(d.aborts,1);assert.equal(d.submits,0);
 assert.equal(s.calls.at(-1).method,'destroy');assert.deepEqual(s.events.map(e=>e[0]),['selected']);
});
test('processing failures settle once and never send an upload',()=>{
 const s=setup(),d=s.data();s.options.add(null,d);d.reject();s.options.fail(null,d);
 assert.equal(d.submits,0);assert.deepEqual(s.events.map(e=>e[0]),['selected','failed']);
});
