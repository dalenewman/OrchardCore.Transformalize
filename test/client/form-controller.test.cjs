const test = require('node:test');
const assert = require('node:assert/strict');
const fs = require('node:fs');
const path = require('node:path');
const vm = require('node:vm');

function setup() {
   const refreshes = [], messages = [], navigations = [];
   let allowCancel = true;
   const context = {
      URL,
      CustomEvent: class { constructor(type, options) { this.type = type; this.detail = options.detail; } },
      Stimulus: { Controller: class {} },
      htmx: { trigger: (form, event) => refreshes.push(event) },
      window: { location: { href: '/review', origin: 'http://localhost', assign: url => navigations.push(url) },
         confirm: () => allowCancel, parent: { postMessage: (...args) => messages.push(args) } }
   };
   vm.runInNewContext(fs.readFileSync(path.join(__dirname, '../../src/OrchardCore.Transformalize/wwwroot/Scripts/form-controller.js'), 'utf8'), context);
   const controller = new context.window.TransformalizeFormController();
   const elements = [];
   elements.namedItem = () => controller.focusField;
   controller.formTarget = { dataset: {}, elements, getAttribute: () => '/validate' };
   controller.focusField = { value: '' };
   controller.element = { setAttribute() {}, dispatchEvent() {} };
   controller.statusTarget = {};
   controller.errorTarget = { hidden: true };
   controller.submitTarget = { disabled: false, attributes: {}, setAttribute(name, value) { this.attributes[name] = value; } };
   controller.hasSubmitTarget = true;
   controller.widgets = { validateField: () => true, validateForm: () => true };
   controller.busy = false; controller.submitting = false; controller.dirtyValue = false;
   return { controller, refreshes, messages, navigations, declineCancel: () => { allowCancel = false; } };
}

function field(controller, postBack = 'true') {
   return { form: controller.formTarget, name: 'Value', dataset: { tflPostBack: postBack }, matches: () => true };
}

test('valid postback fields request validation without submitting or running', () => {
   const { controller, refreshes } = setup();
   controller.changed({ target: field(controller) });
   assert.deepEqual(refreshes, ['tfl:validate']);
   assert.equal(controller.dirtyValue, true);
   assert.equal(controller.submitting, false);
});

test('postback-disabled fields remain local, while browser-invalid values still reach server transforms', () => {
   const { controller, refreshes } = setup();
   controller.changed({ target: field(controller, 'false') });
   assert.deepEqual(refreshes, []);
   controller.widgets.validateField = () => false;
   controller.changed({ target: field(controller) });
   assert.deepEqual(refreshes, ['tfl:validate']);
});

test('map coordinates validate together and request only one refresh', () => {
   const { controller, refreshes } = setup();
   const latitude = field(controller), longitude = field(controller);
   const validated = [];
   controller.widgets.validateField = field => { validated.push(field); return true; };
   controller.groupChanged({ detail: { fields: [latitude, longitude] } });
   assert.deepEqual(validated, [latitude, longitude]);
   assert.deepEqual(refreshes, ['tfl:validate']);
   assert.equal(controller.dirtyValue, true);
});

test('browser-invalid coordinates are posted as one complete pair for server validation', () => {
   const { controller, refreshes } = setup();
   const latitude = field(controller), longitude = field(controller);
   controller.widgets.validateField = field => field !== latitude;
   controller.groupChanged({ detail: { fields: [latitude, longitude] } });
   assert.deepEqual(refreshes, ['tfl:validate']);
});

test('capturing location marks the form dirty without automatically posting it', () => {
   const { controller, refreshes } = setup();
   controller.edited({ type: 'tfl-location:changed' });
   assert.equal(controller.dirtyValue, true);
   assert.deepEqual(refreshes, []);
});

test('failed validation requests retain the form, restore Submit, and offer retry', () => {
   const { controller } = setup();
   controller.beforeRequest({ detail: { elt: controller.formTarget } });
   assert.equal(controller.submitTarget.disabled, false, 'validation must not remove the action from the Tab order');
   assert.equal(controller.submitTarget.attributes['aria-disabled'], 'true');
   controller.afterRequest({ detail: { elt: controller.formTarget, failed: true } });
   assert.equal(controller.submitTarget.disabled, false);
   assert.equal(controller.submitTarget.attributes['aria-disabled'], 'false');
   assert.equal(controller.errorTarget.hidden, false);
   assert.match(controller.statusTarget.textContent, /entries are still here/);
});

test('native submit reaches authoritative server validation even when browser checks fail', () => {
   const { controller } = setup();
   let prevented = 0;
   const event = { target: controller.formTarget, preventDefault: () => prevented++ };
   controller.widgets.validateForm = () => false;
   controller.submit(event);
   assert.equal(prevented, 0);
   assert.equal(controller.submitting, true);
   assert.equal(controller.submitTarget.disabled, false, 'submit name/value must remain in the native POST');
   controller.submit(event);
   assert.equal(prevented, 1, 'duplicate execution must be prevented');
});

test('submitting while a validation refresh is pending is prevented', () => {
   const { controller } = setup();
   controller.busy = true;
   let prevented = false;
   controller.submit({ target: controller.formTarget, preventDefault: () => { prevented = true; } });
   assert.equal(prevented, true);
   assert.equal(controller.submitting, false);
});

test('dirty state warns on navigation, but does not warn on an intentional native submit', () => {
   const { controller } = setup();
   controller.edited({ target: field(controller) });
   let prevented = 0;
   const event = { preventDefault: () => prevented++ };
   controller.leaving(event);
   assert.equal(prevented, 1);
   assert.equal(event.returnValue, '');
   controller.submitting = true;
   controller.leaving(event);
   assert.equal(prevented, 1);
});

test('modal Cancel respects unsaved changes and uses the same-origin protocol', () => {
   const { controller, messages, declineCancel } = setup();
   controller.cancel();
   assert.equal(messages[0][0].action, 'closeModal');
   assert.equal(messages[0][0].reason, 'cancel');
   assert.equal(messages[0][1], 'http://localhost');
   controller.dirtyValue = true;
   declineCancel();
   controller.cancel();
   assert.equal(messages.length, 1);
   assert.equal(controller.dirtyValue, true);
});

test('login redirects navigate to the complete login page instead of overwriting fields', () => {
   const { controller, navigations } = setup();
   const event = { detail: { target: controller.element, shouldSwap: true, xhr: { status: 200, responseURL: '/Login', getResponseHeader: () => null } } };
   controller.beforeSwap(event);
   assert.equal(event.detail.shouldSwap, false);
   assert.deepEqual(navigations, ['http://localhost/Login']);
   assert.equal(controller.submitting, true, 'intentional login navigation does not raise a dirty warning');
});

test('an unmarked validation response retains entries instead of navigating to an unstyled partial endpoint', () => {
   const { controller, navigations } = setup();
   const event = { detail: { target: controller.element, shouldSwap: true, xhr: { status: 200, responseURL: 'http://localhost/validate', getResponseHeader: () => null } } };
   controller.beforeSwap(event);
   assert.equal(event.detail.shouldSwap, false);
   assert.deepEqual(navigations, []);
   assert.equal(controller.errorTarget.hidden, false);
   assert.equal(controller.submitting, false);
   controller.afterRequest({ detail: { elt: controller.formTarget, failed: false } });
   assert.match(controller.statusTarget.textContent, /entries are still here/);
});

test('focus tracking uses the field name without changing user values', () => {
   const { controller } = setup();
   controller.focus({ target: field(controller) });
   assert.equal(controller.focusField.value, 'Value');
});

test('multiple pending uploads coalesce field refreshes and never auto-submit', () => {
   const { controller, refreshes } = setup();
   const first = {}, second = {};
   controller.uploadState({ target: first, detail: { blocked: true } });
   controller.uploadState({ target: second, detail: { blocked: true } });
   controller.changed({ target: field(controller) });
   controller.validate();
   assert.deepEqual(refreshes, []);
   assert.equal(controller.submitTarget.disabled, true);
   let prevented = false;
   controller.submit({ target: controller.formTarget, preventDefault: () => { prevented = true; } });
   assert.equal(prevented, true);
   controller.uploadState({ target: first, detail: { blocked: false } });
   assert.deepEqual(refreshes, []);
   controller.uploadState({ target: second, detail: { blocked: false } });
   assert.deepEqual(refreshes, ['tfl:validate']);
   assert.equal(controller.submitting, false);
});

test('direct refresh requests and a late response cannot discard a selected upload', () => {
   const { controller, refreshes } = setup();
   controller.uploadState({ target: {}, detail: { blocked: true } });
   let prevented = false;
   controller.beforeRequest({ detail: { elt: controller.formTarget }, preventDefault: () => { prevented = true; } });
   assert.equal(prevented, true);
   const response = { detail: { target: controller.element, shouldSwap: true } };
   controller.beforeSwap(response);
   assert.equal(response.detail.shouldSwap, false);
   assert.deepEqual(refreshes, []);
   assert.equal(controller.refreshQueued, true);
});

test('address selection validates its completed field group before one configured refresh', () => {
   const { controller, refreshes } = setup();
   const search = field(controller, 'false'), city = field(controller, 'false');
   const validated = [];
   controller.widgets.validateField = field => { validated.push(field); return true; };
   controller.groupChanged({ detail: { fields: [search, city], postBack: true } });
   assert.deepEqual(validated, [search, city]);
   assert.deepEqual(refreshes, ['tfl:validate']);
   assert.equal(controller.dirtyValue, true);
});

function response(controller) {
   return { detail: { target: controller.element, shouldSwap: true,
      xhr: { status: 200, getResponseHeader: name => name === 'X-Transformalize-Form' ? 'true' : null } } };
}

test('typing during validation discards stale HTML and coalesces the latest values into one refresh', () => {
   const { controller, refreshes } = setup();
   const notes = { ...field(controller, 'false'), value: 'before', disabled: false, readOnly: false };
   controller.beforeRequest({ detail: { elt: controller.formTarget } });
   for (const value of ['new', 'newer', 'newest']) {
      notes.value = value;
      controller.edited({ type: 'input', target: notes });
   }
   controller.changed({ target: notes });
   const stale = response(controller);
   controller.beforeSwap(stale);
   assert.equal(stale.detail.shouldSwap, false);
   assert.equal(notes.value, 'newest');
   assert.equal(notes.disabled, false);
   assert.equal(notes.readOnly, false);
   assert.deepEqual(refreshes, []);
   controller.afterRequest({ detail: { elt: controller.formTarget, failed: false } });
   assert.deepEqual(refreshes, ['tfl:validate']);
   controller.beforeRequest({ detail: { elt: controller.formTarget } });
   const current = response(controller);
   controller.beforeSwap(current);
   assert.equal(current.detail.shouldSwap, true, 'fresh server transformations must still replace every field');
});

test('a failed follow-up leaves newer entries visible and retryable', () => {
   const { controller, refreshes } = setup();
   controller.beforeRequest({ detail: { elt: controller.formTarget } });
   controller.edited({ type: 'input', target: field(controller, 'false') });
   controller.beforeSwap(response(controller));
   controller.afterRequest({ detail: { elt: controller.formTarget, failed: false } });
   controller.beforeRequest({ detail: { elt: controller.formTarget } });
   controller.afterRequest({ detail: { elt: controller.formTarget, failed: true } });
   assert.equal(controller.errorTarget.hidden, false);
   assert.equal(controller.busy, false);
   assert.equal(controller.submitting, false);
   assert.deepEqual(refreshes, ['tfl:validate'], 'a failed request must not start an automatic retry loop');
   controller.validate();
   assert.deepEqual(refreshes, ['tfl:validate', 'tfl:validate']);
});

test('device coordinates captured during validation also invalidate the older snapshot', () => {
   const { controller } = setup();
   controller.beforeRequest({ detail: { elt: controller.formTarget } });
   controller.edited({ type: 'tfl-location:changed' });
   const stale = response(controller);
   controller.beforeSwap(stale);
   assert.equal(stale.detail.shouldSwap, false);
});

function focusField(controller, name, document, type = 'text', value = 'typed text') {
   const target = { ...field(controller), name, id: 'id_' + name, type, value, offsetParent: {},
      selectionStart: 0, selectionEnd: 0, selectionDirection: 'none',
      hasAttribute() { return false; },
      matches(selector) { return selector.startsWith('[data-') ? false : selector === 'button' ? type === 'button' : selector.includes(':not(') ? !['button', 'submit', 'reset'].includes(type) : ['text', 'email', 'textarea'].includes(type); },
      focus() { document.activeElement = this; },
      select() { this.selectedAll = true; },
      setSelectionRange(start, end, direction) { this.caret = [start, end, direction]; } };
   controller.formTarget.elements.push(target);
   return target;
}

test('Tab during validation keeps the current field and caret after replacement instead of skipping ahead', () => {
   const { controller } = setup();
   const document = {};
   controller.element.ownerDocument = document;
   controller.element.isConnected = true;
   const first = focusField(controller, 'First', document);
   const next = focusField(controller, 'Next', document);
   first.focus();
   controller.beforeRequest({ detail: { elt: controller.formTarget } });
   let prevented = false;
   controller.advance({ key: 'Tab', target: first, preventDefault() { prevented = true; } });
   assert.equal(prevented, false, 'the browser must handle Tab normally');
   next.focus();
   controller.focus({ target: next });
   next.selectionStart = 2; next.selectionEnd = 5; next.selectionDirection = 'backward';
   const current = response(controller);
   controller.beforeSwap(current);
   assert.equal(current.detail.shouldSwap, true, 'focus movement alone does not require another request');
   controller.element.isConnected = false;
   const replacement = new controller.constructor();
   replacement.element = { ownerDocument: document };
   replacement.formTarget = { elements: [] };
   replacement.formTarget.elements.namedItem = () => ({ value: 'Later' });
   const retained = focusField(replacement, 'Next', document);
   focusField(replacement, 'Later', document);
   replacement.restoreFocus();
   assert.equal(document.activeElement, retained);
   assert.deepEqual(retained.caret, [2, 5, 'backward']);
   assert.equal(retained.selectedAll, undefined, 'continuing to type must not replace the entire field');
});

test('a fresh response after typing retains a zero-position caret and uses the server value', () => {
   const { controller } = setup();
   const document = {};
   controller.element.ownerDocument = document;
   controller.element.isConnected = true;
   const notes = focusField(controller, 'Notes', document);
   notes.focus();
   controller.beforeRequest({ detail: { elt: controller.formTarget } });
   controller.edited({ type: 'input', target: notes });
   controller.beforeSwap(response(controller));
   controller.afterRequest({ detail: { elt: controller.formTarget, failed: false } });
   controller.beforeRequest({ detail: { elt: controller.formTarget } });
   controller.beforeSwap(response(controller));
   controller.element.isConnected = false;
   const replacement = new controller.constructor();
   replacement.element = { ownerDocument: document };
   replacement.formTarget = { elements: [] };
   replacement.formTarget.elements.namedItem = () => ({ value: 'Later' });
   const retained = focusField(replacement, 'Notes', document, 'text', 'SERVER TRANSFORMED');
   replacement.restoreFocus();
   assert.equal(document.activeElement, retained);
   assert.equal(retained.value, 'SERVER TRANSFORMED');
   assert.deepEqual(retained.caret, [0, 0, 'none']);
});

test('Submit stays keyboard reachable while pending, but cannot execute until validation finishes', () => {
   const { controller } = setup();
   controller.beforeRequest({ detail: { elt: controller.formTarget } });
   assert.equal(controller.submitTarget.disabled, false);
   assert.equal(controller.submitTarget.attributes['aria-disabled'], 'true');
   let prevented = false;
   controller.submit({ target: controller.formTarget, preventDefault() { prevented = true; } });
   assert.equal(prevented, true);
   assert.equal(controller.submitting, false);
   controller.afterRequest({ detail: { elt: controller.formTarget, failed: false } });
   assert.equal(controller.submitTarget.attributes['aria-disabled'], 'false');
   controller.submit({ target: controller.formTarget, preventDefault() {} });
   assert.equal(controller.submitting, true);
});

test('a removed dependent field falls back to the server focus without consuming stale focus later', () => {
   const { controller } = setup();
   const document = {};
   controller.element.ownerDocument = document;
   controller.element.isConnected = true;
   const first = focusField(controller, 'First', document);
   const dependent = focusField(controller, 'Dependent', document);
   first.focus();
   controller.beforeRequest({ detail: { elt: controller.formTarget } });
   dependent.focus();
   controller.focus({ target: dependent });
   controller.beforeSwap(response(controller));
   controller.element.isConnected = false;
   const replacement = new controller.constructor();
   replacement.element = { ownerDocument: document };
   replacement.formTarget = { elements: [] };
   replacement.formTarget.elements.namedItem = () => ({ value: 'Fallback' });
   const fallback = focusField(replacement, 'Fallback', document);
   replacement.restoreFocus();
   assert.equal(document.activeElement, fallback);
   const reappeared = focusField(replacement, 'Dependent', document);
   replacement.restoreFocus();
   assert.notEqual(document.activeElement, reappeared);
});

test('opening focus falls back to the first editable field, skipping helpers and unavailable fields', () => {
   for (const specified of ['', 'Missing', 'Hidden', 'Readonly', 'Disabled', 'Invisible']) {
      const { controller } = setup();
      const document = {};
      controller.focusField.value = specified;
      focusField(controller, 'Calendar helper', document, 'button');
      focusField(controller, 'Hidden', document, 'hidden');
      focusField(controller, 'Readonly', document).readOnly = true;
      focusField(controller, 'Disabled', document).disabled = true;
      focusField(controller, 'Invisible', document).offsetParent = null;
      const first = focusField(controller, 'First editable', document);
      focusField(controller, 'Second editable', document);
      controller.restoreFocus();
      assert.equal(document.activeElement, first, specified);
   }
});

test('explicit autofocus and server-selected focus take precedence over the default opening field', () => {
   const { controller } = setup();
   const document = {};
   focusField(controller, 'First', document);
   const second = focusField(controller, 'Second', document);
   const explicit = focusField(controller, 'Explicit', document);
   controller.focusField.value = 'Second';
   controller.restoreFocus();
   assert.equal(document.activeElement, second);
   explicit.hasAttribute = name => name === 'autofocus';
   controller.restoreFocus();
   assert.equal(document.activeElement, explicit);
});

test('server-selected Run and the checked choice remain valid focus targets', () => {
   const { controller } = setup();
   const document = {};
   focusField(controller, 'First', document);
   const run = focusField(controller, 'Orchard.Submit', document, 'submit');
   controller.focusField.value = 'Orchard.Submit';
   controller.restoreFocus();
   assert.equal(document.activeElement, run);
   focusField(controller, 'Choice', document, 'radio').value = 'A';
   const checked = focusField(controller, 'Choice', document, 'radio');
   checked.value = 'B'; checked.checked = true;
   controller.focusField.value = 'Choice';
   controller.restoreFocus();
   assert.equal(document.activeElement, checked);
});

test('Tab into an unnamed Upload button retains that control across a validation replacement', () => {
   const { controller } = setup();
   const document = {};
   controller.element.ownerDocument = document; controller.element.isConnected = true;
   const first = focusField(controller, 'First', document);
   const upload = focusField(controller, '', document, 'button');
   upload.id = 'id_Evidence_choose'; upload.dataset.tflFocusName = 'Evidence';
   first.focus();controller.beforeRequest({ detail: { elt: controller.formTarget } });
   upload.focus();controller.focus({ target: upload });
   assert.equal(controller.focusField.value, 'Evidence');
   controller.beforeSwap(response(controller));controller.element.isConnected = false;
   const replacement = new controller.constructor();
   replacement.element = { ownerDocument: document };replacement.formTarget = { elements: [] };
   replacement.formTarget.elements.namedItem = () => ({ value: 'First' });
   focusField(replacement, 'First', document);
   const retained = focusField(replacement, '', document, 'button');
   retained.id = 'id_Evidence_choose';retained.dataset.tflFocusName = 'Evidence';
   replacement.restoreFocus();assert.equal(document.activeElement, retained);
});

test('server focus on an upload resolves to Upload or Clear, rather than the readonly filename or top field', () => {
   const { controller } = setup();const document = {};
   focusField(controller, 'First', document);
   const display = focusField(controller, '', document);display.id = 'id_Evidence';display.readOnly = true;display.tabIndex = -1;
   const file = focusField(controller, '', document, 'file');file.tabIndex = -1;
   const choose = focusField(controller, '', document, 'button');choose.dataset.tflFocusName = 'Evidence';
   const clear = focusField(controller, '', document, 'button');clear.dataset.tflFocusName = 'Evidence';clear.offsetParent = null;
   controller.focusField.value = 'Evidence';controller.restoreFocus();assert.equal(document.activeElement, choose);
   choose.disabled = true;clear.offsetParent = {};controller.restoreFocus();assert.equal(document.activeElement, clear);
});

test('an upload-only form initially focuses the visible button, and Enter advancement skips its file input helper', () => {
   const { controller } = setup();const document = {};
   const helper = focusField(controller, '', document, 'file');helper.tabIndex = -1;
   const choose = focusField(controller, '', document, 'button');choose.dataset.tflFocusName = 'Evidence';
   controller.restoreFocus();assert.equal(document.activeElement, choose);
   const first = focusField(controller, 'First', document);
   controller.formTarget.elements.splice(2,1);controller.formTarget.elements.unshift(first);
   first.focus();controller.advance({ key: 'Enter', target: first, preventDefault() {} });
   assert.equal(document.activeElement, choose);
});


test('modal readiness restores focus when initialization happened while hidden or Bootstrap blurred the field', () => {
   for (const active of [{}, { id: 'dialog' }]) {
      const { controller } = setup();
      controller.element.ownerDocument = { activeElement: active };
      let restores = 0;
      controller.restoreFocus = () => restores++;
      controller.modalFocus();
      assert.equal(restores, 1);
   }
});

test('modal readiness preserves a field already chosen by the user', () => {
   const { controller } = setup();
   controller.element.ownerDocument = { activeElement: { form: controller.formTarget } };
   controller.restoreFocus = () => assert.fail('do not reset the chosen field or its caret');
   controller.modalFocus();
});


test('Shift+Tab during a postback preserves the earlier field and selection rather than the server next-field hint', () => {
   for (const returnToRequestField of [false,true]) {
      const { controller } = setup(), document = {};
      controller.element.ownerDocument = document;controller.element.isConnected = true;
      const previous = focusField(controller,'Previous',document);
      const current = focusField(controller,'Current',document);
      const next = focusField(controller,'Next',document);
      current.focus();controller.beforeRequest({detail:{elt:controller.formTarget}});
      next.focus();controller.focus({target:next});
      const retained = returnToRequestField ? current : previous;
      controller.advance({key:'Tab',shiftKey:true,target:next,preventDefault(){assert.fail('Shift+Tab must stay native');}});
      retained.focus();controller.focus({target:retained});retained.selectionStart=1;retained.selectionEnd=4;retained.selectionDirection='backward';
      controller.beforeSwap(response(controller));controller.element.isConnected=false;
      const replacement=new controller.constructor();replacement.element={ownerDocument:document};replacement.formTarget={elements:[]};
      replacement.formTarget.elements.namedItem=()=>({value:'Next'});
      const restored=focusField(replacement,retained.name,document);focusField(replacement,'Next',document);
      replacement.restoreFocus();assert.equal(document.activeElement,restored);assert.deepEqual(restored.caret,[1,4,'backward']);
      assert.equal(restored.selectedAll,undefined);
   }
});


function formError(controller) {
   return {detail:{target:controller.element,shouldSwap:true,xhr:{status:200,
      getResponseHeader:name=>name==='X-Transformalize-Form-Error'?'true':null,
      responseText:JSON.stringify({message:'Correct the highlighted fields.',fields:[{name:'OtherReason',message:'Other Reason contains a disallowed apostrophe.'}]})}}};
}

test('marked field errors explain a rejected refresh inline and retain the form and selected values',()=>{
   const {controller}=setup();let errors;
   controller.widgets.showErrors=value=>errors=value;controller.hasErrorMessageTarget=true;controller.errorMessageTarget={};
   controller.beforeRequest({detail:{elt:controller.formTarget}});
   const event=formError(controller);controller.beforeSwap(event);controller.afterRequest({detail:{elt:controller.formTarget,failed:false}});
   assert.equal(event.detail.shouldSwap,false);assert.equal(errors[0].name,'OtherReason');
   assert.equal(controller.errorTarget.hidden,false);assert.equal(controller.errorMessageTarget.textContent,'Correct the highlighted fields.');
   assert.equal(controller.statusTarget.textContent,'Correct the highlighted fields.');assert.equal(controller.busy,false);
});

test('an error for text already corrected during the request is discarded and the latest form is validated',()=>{
   const {controller,refreshes}=setup();controller.widgets.showErrors=()=>assert.fail('do not show obsolete errors');
   controller.beforeRequest({detail:{elt:controller.formTarget}});controller.edited({type:'input',target:field(controller,'false')});
   const event=formError(controller);controller.beforeSwap(event);controller.afterRequest({detail:{elt:controller.formTarget,failed:false}});
   assert.equal(event.detail.shouldSwap,false);assert.equal(controller.errorTarget.hidden,true);assert.deepEqual(refreshes,['tfl:validate']);
});
