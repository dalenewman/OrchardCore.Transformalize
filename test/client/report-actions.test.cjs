const test = require('node:test');
const assert = require('node:assert/strict');
const fs = require('node:fs');
const vm = require('node:vm');
const path = require('node:path');

function controller(name, extra = {}) {
   const context = {
      Stimulus: { Controller: class {} },
      window: { TransformalizeReportControllers: {}, location: { href: 'http://localhost/report?Category=A&Category=B', origin: 'http://localhost' } },
      FormData, URL, CustomEvent: class { constructor(type) { this.type = type; } }, ...extra
   };
   vm.runInNewContext(fs.readFileSync(path.join(__dirname, `../../src/OrchardCore.Transformalize/wwwroot/Scripts/report-${name}-controller.js`), 'utf8'), context);
   return new context.window.TransformalizeReportControllers[name]();
}

function selection() {
   class FormDataStub extends FormData {
      constructor() {
         super();
         for (const [key, value] of [['Category', 'A'], ['Category', 'B'], ['Records', 'stale'], ['ActionName', 'stale'], ['ReturnUrl', 'stale'], ['page', '3']]) this.append(key, value);
      }
   }
   const result = controller('selection', { FormData: FormDataStub });
   result.formTarget = {};
   result.tokenContainerTarget = { querySelector: () => ({ name: '__RequestVerificationToken', value: 'token' }) };
   result.recordTargets = Array.from({ length: 5 }, (_, index) => ({ checked: false, value: String(index), disabled: index === 4 }));
   result.allTargets = [{ checked: false }, { checked: false }];
   result.actionTargets = [{ disabled: false, querySelector: () => result.count }];
   result.count = { textContent: '' };
   result.connect();
   return result;
}

test('individual and shift selections count enabled rows without selecting other pages', () => {
   const result = selection();
   assert.equal(result.actionTargets[0].disabled, true);
   result.recordTargets[0].checked = true;
   result.toggle({ currentTarget: result.recordTargets[0] });
   result.recordTargets[3].checked = true;
   result.toggle({ currentTarget: result.recordTargets[3], shiftKey: true });
   assert.equal(result.count.textContent, '4');
   assert.equal(result.allSelected, false);
   assert.equal(result.recordTargets[4].checked, false);
   assert.equal(result.parameters('Archive').get('ActionCount'), '4');
});

test('select entire result synchronizes headers; unchecking a row returns to individual selection', () => {
   const result = selection();
   result.allTargets[0].checked = true;
   result.toggle({ currentTarget: result.allTargets[0] });
   assert.equal(result.allTargets[1].checked, true);
   assert.equal(result.count.textContent, 'All');
   assert.equal(result.parameters('Archive').get('ActionCount'), '0');
   result.recordTargets[1].checked = false;
   result.toggle({ currentTarget: result.recordTargets[1] });
   assert.equal(result.allTargets.some(box => box.checked), false);
   assert.equal(result.parameters('Archive').get('ActionCount'), '3');
});

test('bulk POST preserves repeated filters and carries one token, action and current ReturnUrl', () => {
   const result = selection();
   result.recordTargets[2].checked = true;
   const data = result.parameters('An action with "quotes"');
   assert.deepEqual(data.getAll('Category'), ['A', 'B']);
   assert.deepEqual(data.getAll('Records'), ['2']);
   assert.deepEqual(data.getAll('ActionName'), ['An action with "quotes"']);
   assert.deepEqual(data.getAll('__RequestVerificationToken'), ['token']);
   assert.equal(data.get('ReturnUrl'), 'http://localhost/report?Category=A&Category=B');
   assert.equal(data.get('page'), '3');
   assert.equal(data.has('select-all'), false);
});

test('modal ignores unrelated, foreign-origin and malformed messages', () => {
   const result = controller('modal');
   const source = {};
   let hides = 0;
   result.active = true;
   result.frameTarget = { contentWindow: source };
   result.instance = { hide: () => hides++ };
   result.message({ origin: 'http://other', source, data: { action: 'closeModal', reason: 'confirmed' } });
   result.message({ origin: 'http://localhost', source: {}, data: { action: 'closeModal' } });
   result.message({ origin: 'http://localhost', source, data: null });
   assert.equal(hides, 0);
   result.message({ origin: 'http://localhost', source, data: { action: 'closeModal', reason: 'cancel' } });
   assert.equal(hides, 1);
   assert.equal(result.confirmed, false);
   result.message({ origin: 'http://localhost', source, data: { action: 'closeModal', reason: 'confirmed' } });
   assert.equal(result.confirmed, true);
});

test('modal resets the iframe and refreshes only after confirmed dismissal', () => {
   const result = controller('modal');
   const events = [];
   result.frameTarget = {};
   result.dispatch = event => events.push(event);
   result.confirmed = false;
   result.hidden();
   assert.equal(result.frameTarget.src, 'about:blank');
   assert.deepEqual(events, []);
   result.confirmed = true;
   result.hidden();
   assert.deepEqual(events, ['confirmed']);
});

test('arrangement modal links are adapted on each report connection without executing inline handlers', () => {
   const link = (tag, attrs) => ({ tagName: tag,
      getAttribute: name => attrs[name] ?? null,
      setAttribute: (name, value) => { attrs[name] = value; },
      removeAttribute: name => { delete attrs[name]; }, attrs
   });
   for (let refresh = 0; refresh < 2; refresh++) {
      const edit = link('A', { href: 'javascript:void()', onclick: 'loadModal(this)', 'data-url': '/t/form/got-up-edit?modal=1&GotUpId=1' });
      const task = link('A', { href: 'javascript:void()', onclick: 'return loadModal( this );', 'data-url': '/t/task/test?modal=1&GotUpId=1', 'data-action': 'focus->other#focus' });
      const custom = link('A', { onclick: 'custom(); loadModal(this)', 'data-url': '/custom' });
      const result = controller('modal', { bootstrap: { Modal: class {} } });
      result.element = { querySelectorAll: () => [edit, task, custom] };
      result.dialogTarget = {};
      result.connect();
      assert.equal(edit.attrs.onclick, undefined);
      assert.equal(edit.attrs.href, edit.attrs['data-url']);
      assert.equal(edit.attrs['data-action'], 'click->modal#open');
      assert.equal(task.attrs['data-action'], 'focus->other#focus click->modal#open');
      assert.equal(custom.attrs.onclick, 'custom(); loadModal(this)', 'unrelated custom handlers are preserved');
   }
});

test('modal links load their configured form URL and preserve modified-click navigation', () => {
   const result = controller('modal');
   const link = { title: 'Edit Got Up', href: '/t/form/got-up-edit?modal=1&GotUpId=1', getAttribute: () => null };
   let shows = 0, prevents = 0;
   result.show = (title, opener) => { shows++; assert.equal(title, link.title); assert.equal(opener, link); };
   result.frameTarget = {};
   result.open({ currentTarget: link, ctrlKey: true, preventDefault: () => prevents++ });
   assert.equal(shows, 0);
   assert.equal(prevents, 0);
   result.open({ currentTarget: link, preventDefault: () => prevents++ });
   assert.equal(result.frameTarget.src, link.href);
   assert.equal(shows, 1);
   assert.equal(prevents, 1);
});

test('modal asks the now-visible form to restore focus after both loading and the opening transition, in either order', () => {
   for (const order of [['loaded', 'shown'], ['shown', 'loaded']]) {
      const result = controller('modal');
      const focused = [];
      result.titleTarget = {};
      result.loadingTarget = {};
      result.frameTarget = { setAttribute() {}, focus() { focused.push('frame'); },
         contentWindow: { dispatchEvent(event) { focused.push(event.type); } } };
      result.instance = { show() {} };
      result.show('Edit entry', { focus() {} });
      assert.equal(result.titleTarget.textContent, 'Edit entry');
      assert.equal(result.frameTarget.title, 'Edit entry');
      result[order[0]]();
      assert.deepEqual(focused, [], 'do not compete with Bootstrap while its transition is running');
      result[order[1]]();
      assert.deepEqual(focused, ['frame', 'tfl-form:focus']);
      result.hidden();
      result.loaded();
      assert.deepEqual(focused, ['frame', 'tfl-form:focus'], 'late iframe events must not reopen a cancelled dialog');
   }
});

function cell(text, type, options = {}) {
   return {
      textContent: text, dataset: { type },
      querySelector: selector => selector.includes('checkbox') ? options.checkbox : options.bool ? { getAttribute: () => options.bool } : null,
      cloneNode() {
         const clone = { textContent: text, querySelectorAll: () => options.link ? [{
            ...options.link, replaceWith(value) { clone.textContent = value; }
         }] : [] };
         return clone;
      }
   };
}

test('Markdown skips selection columns and preserves pipes, boolean badges and links', async () => {
   let copied;
   const result = controller('clipboard', { navigator: { clipboard: { writeText: async value => { copied = value; } } } });
   const checkbox = cell('', '', { checkbox: true });
   const table = {
      tHead: { rows: [{ cells: [checkbox, cell(' Name | value ', 'string'), cell('Active', 'bool'), cell('Link', 'string')] }] },
      tBodies: [{ rows: [{ cells: [checkbox, cell(' A | B\nC ', 'string'), cell('', 'bool', { bool: 'Yes' }), cell('Example', 'string', { link: { textContent: 'Example', protocol: 'https:', href: 'https://example.com/' } })] }] }]
   };
   result.element = { querySelector: () => table };
   result.statusTarget = {};
   await result.copy({ preventDefault() {}, params: { table: 'table' } });
   assert.equal(copied, '| Name \\| value | Active | Link |\n| --- | --- | --- |\n| A \\| B C | ✅ | [Example](https://example.com/) |');
   assert.equal(table.tBodies[0].rows[0].cells[3].textContent, 'Example', 'copy must not mutate the displayed table');
   assert.match(result.statusTarget.textContent, /copied/);
});

test('clipboard rejection reports a useful failure instead of an unhandled promise', async () => {
   const result = controller('clipboard', { navigator: { clipboard: { writeText: async () => { throw new Error('denied'); } } } });
   result.element = { querySelector: () => ({ tHead: { rows: [{ cells: [cell('Name', 'string')] }] }, tBodies: [] }) };
   result.statusTarget = {};
   await result.copy({ preventDefault() {}, params: { table: 'table' } });
   assert.match(result.statusTarget.textContent, /Allow clipboard access/);
});

test('native bulk POST is outside the GET report form and modal submits use its own target', () => {
   const submitted = [];
   const forms = [];
   const document = { body: { append: form => { form.attached = true; } }, createElement: tag => {
      const node = { tag, children: [], append(input) { this.children.push(input); }, remove() { this.attached = false; }, submit() { submitted.push(this); } };
      if (tag === 'form') forms.push(node);
      return node;
   } };
   const result = controller('selection', { document });
   result.recordTargets = [{ checked: true, value: '1' }];
   result.allTargets = [];
   result.urlValue = 'http://localhost/t/bulk/create';
   result.parameters = () => new FormData();
   result.submit({ preventDefault() {}, params: { name: 'Archive', modal: false } });
   assert.equal(submitted[0].method, 'post');
   assert.equal(submitted[0].action, result.urlValue);
   assert.equal(forms[0].attached, false);
   let dispatched;
   result.dispatch = (event, options) => { dispatched = { event, options }; };
   result.submit({ preventDefault() {}, params: { name: 'Archive', modal: true }, currentTarget: { dataset: { selectionLabel: 'Archive' } } });
   assert.equal(dispatched.event, 'modal');
   assert.equal(dispatched.options.detail.form.action, 'http://localhost/t/bulk/create?modal=1');
   assert.equal(submitted.length, 1, 'modal controller owns iframe submission');
});
