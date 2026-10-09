const test = require('node:test');
const assert = require('node:assert/strict');
const fs = require('node:fs');
const vm = require('node:vm');
const path = require('node:path');

function node() {
   const attributes = new Map(), classes = new Set();
   return { attributes, classes, textContent: '', removed: false,
      classList: { contains: name => classes.has(name), toggle(name, on) { on ? classes.add(name) : classes.delete(name); } },
      getAttribute: name => attributes.get(name) ?? null,
      setAttribute: (name, value) => attributes.set(name, value),
      removeAttribute: name => attributes.delete(name),
      remove() { this.removed = true; }
   };
}
function setup() {
   const listeners = new Map(), boundaryListeners = new Map(), nodes = [];
   const context = { window: {}, document: { createElement() {
      const n = node(); nodes.push(n);
      Object.defineProperty(n, 'validity', { get: () => ({ typeMismatch: n.type === 'email' ? !/^[^\s@]+@[^\s@]+$/.test(n.value) : !/^(https?|ftp):\/\/[^\s/]+(?:\/.*)?$/.test(n.value) }) });
      return n;
   } } };
   vm.runInNewContext(fs.readFileSync(path.join(__dirname, '../../src/OrchardCore.Transformalize/wwwroot/Scripts/date-widgets.js'), 'utf8'), context);
   context.TransformalizeDateWidgets = context.window.TransformalizeDateWidgets;
   vm.runInNewContext(fs.readFileSync(path.join(__dirname, '../../src/OrchardCore.Transformalize/wwwroot/Scripts/form-widgets.js'), 'utf8'), context);
   const form = { elements: [], querySelectorAll: () => [], closest: () => ({
      addEventListener: (name, cb) => boundaryListeners.set(name, cb), removeEventListener: name => boundaryListeners.delete(name)
   }), addEventListener: (name, cb) => listeners.set(name, cb), removeEventListener: name => listeners.delete(name) };
   const field = (value, rules = {}, type = 'text') => {
      const n = node(), container = node(), group = node();
      container.children = []; container.append = child => container.children.push(child);
      container.querySelectorAll = () => container.children.filter(child => !child.removed && child.getAttribute('data-tfl-validation-message') == null);
      group.querySelector = () => container;
      Object.assign(n, { name: 'Field' + form.elements.length, value, type, form, validity: {}, minLength: -1, maxLength: -1,
         focus() { this.focused = true; }, closest: () => group,
         matches(selector) { return selector === 'input, textarea, select' || this.excluded === true || type === 'hidden' && !this.upload; } });
      n.setAttribute('data-tfl-validation', JSON.stringify(rules)); n.group = group; n.container = container;
      form.elements.push(n); return n;
   };
   const widgets = context.window.TransformalizeFormWidgets.connect(form);
   return { field, form, widgets, listeners, boundaryListeners, nodes };
}

test('required and optional fields, radio groups, stored upload IDs, and disabled/foreign controls', () => {
   const s = setup(), required = s.field('  ', { required: 'true' });
   assert.equal(s.widgets.validateField(required), false);
   assert.equal(required.getAttribute('aria-invalid'), 'true');
   assert.match(required.container.children[0].textContent, /required/);
   required.value = 'Filled'; assert.equal(s.widgets.validateField(required), true);
   assert.equal(s.widgets.validateField(s.field('', { type: 'integer', date: 'true' })), true);
   const hidden = s.field('', { required: 'true', 'required-message': 'a file is required' }, 'hidden'); hidden.upload = true;
   assert.equal(s.widgets.validateField(hidden), false);
   hidden.value = 'content-id'; assert.equal(s.widgets.validateField(hidden), true);
   const plainHidden = s.field('', { required: 'true' }, 'hidden');
   assert.equal(s.widgets.validateField(plainHidden), true);
   const disabled = s.field('', { required: 'true' }); disabled.disabled = true;
   assert.equal(s.widgets.validateField(disabled), true);
   const foreign = s.field('', { required: 'true' }); foreign.form = {};
   assert.equal(s.widgets.validateField(foreign), true);
   const a = s.field('A', { required: 'true' }, 'radio'), b = s.field('B', { required: 'true' }, 'radio');
   b.name = a.name; assert.equal(s.widgets.validateField(a), false);
   b.checked = true; assert.equal(s.widgets.validateField(a), true);
});

test('parameter types, ranges, length, patterns and native bad input/step errors', () => {
   const s = setup();
   const cases = [
      ['1.5', { type: 'integer' }, false], ['-12', { type: 'integer' }, true],
      ['1e3', { type: 'number' }, true], ['Infinity', { type: 'number' }, false],
      ['a_1', { type: 'alphanum' }, true], ['a-1', { type: 'alphanum' }, false],
      ['012', { type: 'digits' }, true], ['-12', { type: 'digits' }, false],
      ['2', { min: '3' }, false], ['9', { max: '9' }, true], ['ten', { max: '9' }, false],
      ['abcd', { length: '[4,4]' }, true], ['abc', { length: '[4,4]' }, false],
      ['ab', { pattern: '[a-z]{2}' }, true], ['xabx', { pattern: '[a-z]{2}' }, false],
      ['AB', { pattern: '/ab/i' }, true], ['text', { pattern: '\\Atext\\z' }, true],
      ['text', { pattern: '[' }, true],
      ['a@example.com', { type: 'email' }, true], ['broken email', { type: 'email' }, false],
      ['example.com/path', { type: 'url' }, true], ['bad url', { type: 'url' }, false]
   ];
   cases.forEach(([value, rules, expected]) => assert.equal(s.widgets.validateField(s.field(value, rules)), expected, value + JSON.stringify(rules)));
   const bad = s.field('', {}, 'number'); bad.validity.badInput = true;
   assert.equal(s.widgets.validateField(bad), false);
   const step = s.field('1.1', {}, 'number'); step.validity.stepMismatch = true;
   assert.equal(s.widgets.validateField(step), false);
});

test('fields with several failing rules retain the first error as earlier rules are corrected', () => {
   const s = setup();
   const f = s.field('', { required: 'true', type: 'integer', pattern: '\\d{2}', length: '[3,3]', min: '200' });
   f.validity.stepMismatch = true;
   const cases = [
      ['', 'This value is required.'],
      ['abc', 'This value should be a valid integer.'],
      ['1', 'This value seems to be invalid.'],
      ['12', 'This value should have 3 characters.']
   ];
   for (const [value, expected] of cases) {
      f.value = value;
      assert.equal(s.widgets.validateField(f), false);
      assert.equal(f.container.children[0].textContent, expected);
   }
   f.setAttribute('data-tfl-validation', JSON.stringify({ type: 'integer', min: '200' }));
   f.value = '123';
   s.widgets.validateField(f);
   assert.equal(f.container.children[0].textContent, 'This value should be greater than or equal to 200.');
   f.value = '234';
   s.widgets.validateField(f);
   assert.equal(f.container.children[0].textContent, 'This value should match the specified numeric step.');
});

test('date validation rejects nonexistent dates and accepts leap days, times and existing US date text', () => {
   const s = setup();
   ['2024-02-29', '2026-10-07', '2026-10-07T12:30:00Z', '10/7/2026'].forEach(value => assert.equal(s.widgets.validateField(s.field(value, { date: 'true' })), true, value));
   ['2026-02-29', '2026-02-30', '02/30/2026', '2026-13-01', 'not a date'].forEach(value => assert.equal(s.widgets.validateField(s.field(value, { date: 'true' })), false, value));
});

test('inline errors preserve hint descriptions, clear while correcting, focus invalid input and tear down', () => {
   const s = setup(), f = s.field('', { required: 'true' });
   f.setAttribute('aria-describedby', 'existing-hint');
   assert.equal(s.widgets.validateForm(), false); assert.equal(f.focused, true);
   assert.match(f.getAttribute('aria-describedby'), /^existing-hint tfl-validation-/);
   f.value = 'Fixed'; s.listeners.get('input')({ target: f });
   assert.equal(f.getAttribute('aria-describedby'), 'existing-hint');
   assert.equal(f.container.children[0].textContent, '');
   f.value = ''; s.listeners.get('focusout')({ target: f });
   const message = f.container.children[0];
   s.widgets.disconnect();
   assert.equal(message.removed, true); assert.equal(s.listeners.size, 0); assert.equal(s.boundaryListeners.size, 0);
   assert.equal(f.getAttribute('aria-describedby'), 'existing-hint');
});

test('Scott and other server-only errors survive blur, local success and submit checks until a new response', () => {
   const s = setup(), f = s.field('Scott', {}, 'select'), serverMessage = node();
   serverMessage.textContent = 'Scott is remote. Choose someone else.';
   f.group.classes.add('is-invalid'); f.container.children.push(serverMessage);
   s.listeners.get('focusout')({ target: f });
   assert.equal(serverMessage.removed, false);
   assert.equal(serverMessage.textContent, 'Scott is remote. Choose someone else.');
   assert.equal(f.getAttribute('aria-invalid'), 'true');
   assert.equal(f.group.classes.has('is-invalid'), true);
   assert.equal(f.classes.has('is-valid'), false);
   assert.match(f.getAttribute('aria-describedby'), /tfl-server-validation-/);
   // Local validation cannot know whether changing this or another field resolves
   // the server rule. Only the next Razor fragment can clear its result.
   f.value = 'Jeremy'; s.listeners.get('input')({ target: f });
   assert.equal(s.widgets.validateForm(), true, 'client results are advisory');
   assert.equal(serverMessage.removed, false);
   assert.equal(f.getAttribute('aria-invalid'), 'true');
});

test('a missing required attachment focuses the visible Upload button instead of its hidden chooser', () => {
   const s = setup(), storedId = s.field('', { required: 'true' }, 'hidden');
   storedId.upload = true;
   const choose = { focused: false, focus() { this.focused = true; } };
   storedId.group.querySelector = selector => selector === '.help-container' ? storedId.container
      : selector === '[data-tfl-upload-target~="choose"]' ? choose : null;
   assert.equal(s.widgets.validateForm(), false);
   assert.equal(choose.focused, true);
   assert.equal(storedId.focused, undefined);
});


test('server refresh errors are shown as safe field text and clear when the rejected value is corrected',()=>{
 const s=setup(),f=s.field("I don't want to say");
 const old=node();old.id='old-required';old.textContent='Explain why you got up.';
 f.container.children.push(old);f.group.classes.add('is-invalid');f.setAttribute('aria-describedby','hint old-required');
 s.widgets.showErrors([{name:f.name,message:'Other Reason contains a disallowed apostrophe.'},{name:'missing',message:'Ignore missing fields'}]);
 assert.equal(old.removed,true);assert.equal(f.getAttribute('aria-invalid'),'true');assert.equal(f.container.children[1].textContent,'Other Reason contains a disallowed apostrophe.');
 assert.match(f.getAttribute('aria-describedby'),/tfl-validation-/);
 assert.doesNotMatch(f.getAttribute('aria-describedby'),/old-required/);
 f.value='I do not want to say';s.listeners.get('input')({target:f});
 assert.equal(f.getAttribute('aria-invalid'),'false');assert.equal(f.container.children[1].textContent,'');
 assert.equal(f.getAttribute('aria-describedby'),'hint');
});
