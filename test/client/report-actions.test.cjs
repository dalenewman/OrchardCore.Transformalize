const test = require('node:test');
const assert = require('node:assert/strict');
const fs = require('node:fs');
const vm = require('node:vm');
const path = require('node:path');

function controller(name, extra = {}) {
   const context = {
      Stimulus: { Controller: class {} },
      window: { TransformalizeReportControllers: {}, location: { href: 'http://localhost/report?Category=A&Category=B', origin: 'http://localhost' } },
      MutationObserver: class { observe() {} disconnect() {} },
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

function fieldLink(href, attributes = {}) {
   const attrs = { href, ...attributes };
   return {
      href: new URL(href, 'http://localhost/tenant/t/report/demo').href,
      title: 'Edit record',
      getAttribute: name => attrs[name] ?? null,
      hasAttribute: name => Object.hasOwn(attrs, name),
      attrs
   };
}

function fieldModal() {
   const result = controller('modal');
   result.formPathValue = '/tenant/t/form/';
   result.taskPathValue = '/tenant/t/task/';
   result.frameTarget = {};
   result.opened = [];
   result.show = (title, opener) => result.opened.push({ title, opener });
   return result;
}

function fieldClick(link, extra = {}) {
   return {
      // A nested icon can be the target; the wrapper delegates to its anchor.
      target: { closest: selector => selector === 'a[href]' ? link : null },
      currentTarget: { contains: element => element === link },
      defaultPrevented: false,
      preventDefault() { this.defaultPrevented = true; },
      ...extra
   };
}

test('modal fields open internal form/task pages and add modal mode only to the iframe URL', () => {
   for (const href of [
      '/tenant/t/form/crime-edit?Id=123&Category=A&Category=B#details',
      'http://localhost/tenant/t/task/review?Id=123&modal=0&modal=0',
      '//localhost/tenant/T/FORM/edit/?Id=123',
      '../form/edit?Id=123'
   ]) {
      const result = fieldModal();
      const link = fieldLink(href);
      const event = fieldClick(link);
      result.openField(event);
      assert.equal(event.defaultPrevented, true, href);
      assert.equal(result.opened.length, 1, href);
      assert.equal(result.opened[0].opener, link);
      assert.equal(result.opened[0].title, 'Edit record');
      const url = new URL(result.frameTarget.src);
      assert.deepEqual(url.searchParams.getAll('modal'), ['1']);
      assert.equal(url.searchParams.get('Id'), '123');
      if (href.includes('Category=')) {
         assert.deepEqual(url.searchParams.getAll('Category'), ['A', 'B']);
         assert.equal(url.hash, '#details');
      }
      assert.equal(link.attrs.href, href, 'Keep normal/new-tab navigation unchanged.');
   }
});

test('modal fields leave external, other-tenant, non-page, fragment and download links alone', () => {
   for (const href of [
      'https://example.test/tenant/t/form/edit',
      'http://localhost:8080/tenant/t/task/edit',
      'https://localhost/tenant/t/form/edit',
      '/other/t/form/edit', '/t/form/edit', '/tenant/t/report/demo',
      '/tenant/t/form/', '/tenant/t/task/',
      '/tenant/t/form/content/edit', '/tenant/t/form/json/edit',
      '/tenant/t/task/form/edit', '/tenant/t/task/run/edit', '/tenant/t/task/run/json/edit',
      '/tenant/t/form/edit/extra', '/tenant/t/forms/edit',
      'javascript:void(0)', 'mailto:someone@example.test', 'data:text/html,hello',
      '#details', ''
   ]) {
      const result = fieldModal();
      const event = fieldClick(fieldLink(href));
      result.openField(event);
      assert.equal(event.defaultPrevented, false, href);
      assert.equal(result.opened.length, 0, href);
   }
   const result = fieldModal();
   result.openField(fieldClick(fieldLink('/tenant/t/form/edit', { download: '' })));
   result.openField(fieldClick(null));
   result.openField(fieldClick(fieldLink('/tenant/t/form/edit'), { currentTarget: { contains: () => false } }));
   const malformed = fieldLink('/tenant/t/form/edit');
   malformed.href = 'http://[';
   result.openField(fieldClick(malformed));
   assert.equal(result.opened.length, 0);
});

test('modal fields preserve modified clicks and do not reopen clicks handled by explicit modal actions', () => {
   const result = fieldModal();
   const link = fieldLink('/tenant/t/form/edit?Id=123');
   for (const modifier of [{ ctrlKey: true }, { metaKey: true }, { shiftKey: true }, { altKey: true }, { button: 1 }]) {
      const event = fieldClick(link, modifier);
      result.openField(event);
      assert.equal(event.defaultPrevented, false);
   }
   result.openField(fieldClick(link, { defaultPrevented: true }));
   assert.equal(result.opened.length, 0);
   const event = fieldClick(link);
   result.openField(event);
   result.openField(event);
   assert.equal(result.opened.length, 1, 'Nested field wrappers must open just once.');
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

test('map drawing uses the same record targets, counts and bulk POST data as table rows', () => {
   const result = selection();
   result.recordTargets = [{ checked: true, value: 'map-1', hidden: true }, { checked: true, value: 'map-2', hidden: true }];
   result.recordTargetConnected();
   assert.equal(result.count.textContent, '2');
   assert.deepEqual(result.parameters('Archive').getAll('Records'), ['map-1', 'map-2']);
   assert.equal(result.parameters('Archive').get('ActionCount'), '2');
   result.recordTargets = [];
   result.recordTargetDisconnected();
   assert.equal(result.count.textContent, '0');
   assert.equal(result.actionTargets[0].disabled, true);
});

test('modal actions inserted later by map popups are adapted and observer ownership ends on disconnect', () => {
   let changed, disconnected = false;
   const attrs = { onclick: 'loadModal(this)', 'data-url': '/t/form/edit?modal=1', href: 'javascript:void()' };
   const link = { nodeType: 1, tagName: 'A', matches: () => true, querySelectorAll: () => [],
      getAttribute: name => attrs[name] ?? null, setAttribute: (name, value) => attrs[name] = value,
      removeAttribute: name => delete attrs[name] };
   const result = controller('modal', {
      MutationObserver: class { constructor(callback) { changed = callback; } observe() {} disconnect() { disconnected = true; } },
      bootstrap: { Modal: class { hide() {} dispose() {} } }
   });
   result.element = { querySelectorAll: () => [] };
   result.dialogTarget = { classList: { remove() {} } };
   result.connect();
   changed([{ addedNodes: [link] }]);
   assert.equal(attrs.onclick, undefined);
   assert.equal(attrs.href, '/t/form/edit?modal=1');
   assert.equal(attrs['data-action'], 'click->modal#open');
   result.disconnect();
   assert.equal(disconnected, true);
});
