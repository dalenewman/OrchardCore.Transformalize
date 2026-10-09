const test = require('node:test');
const assert = require('node:assert/strict');
const fs = require('node:fs');
const vm = require('node:vm');
const path = require('node:path');

// Exercise the controller's query/error contract without a browser or npm dependencies.
// Real FormData supplies the repeated-value behavior; only the HTML form input is stubbed.
function controllerFor(entries, selects = []) {
   let Controller;
   class Select {}
   class ReportFormData extends FormData {
      constructor(form) {
         super();
         form.entries.forEach(([name, value]) => this.append(name, value));
      }
   }
   const navigations = [];
   const context = {
      Stimulus: { Controller: class {}, Application: { start: () => ({ register: (_, type) => { Controller = type; } }) } },
      FormData: ReportFormData,
      CustomEvent: class { constructor(type, init) { this.type = type; this.detail = init.detail; } },
      HTMLSelectElement: Select,
      window: { location: { href: '/report', assign: url => navigations.push(url) } },
      document: { getElementById: () => null }
   };
   vm.runInNewContext(fs.readFileSync(path.join(__dirname, '../../src/OrchardCore.Transformalize/wwwroot/Scripts/report-controller.js'), 'utf8'), context);
   const controller = new context.window.TransformalizeReportControllers.report();
   const inputs = Object.fromEntries(entries.map(([name]) => [name, selects.includes(name) ? new Select() : { disabled: false }]));
   controller.formTarget = { entries, elements: { namedItem: name => inputs[name] } };
   controller.lastFilter = null;
   controller.element = { setAttribute() {}, dispatchEvent() {} };
   controller.searchTarget = { disabled: true };
   controller.statusTarget = { textContent: '' };
   controller.errorTarget = { hidden: true };
   return { controller, inputs, navigations };
}

function query(controller) {
   const event = { detail: { elt: controller.formTarget } };
   controller.configureRequest(event);
   return new URLSearchParams(event.detail.parameters);
}

test('queries trim text, omit defaults, and preserve repeated facet values', () => {
   const { controller, inputs } = controllerFor([
      ['Name', '  Ada & Bob  '], ['Category', 'A'], ['Category', 'B'],
      ['All', '*'], ['page', '1'], ['sort', '0d'], ['size', '25'], ['edit', ''],
      ['Records', '123'], ['select-all', 'on']
   ], ['Category', 'All']);
   const result = query(controller);
   assert.equal(result.get('Name'), 'Ada & Bob');
   assert.deepEqual(result.getAll('Category'), ['A', 'B']);
   assert.equal(result.get('sort'), '0d');
   assert.equal(result.get('size'), '25');
   for (const name of ['All', 'page', 'edit', 'Records', 'select-all']) assert.equal(result.has(name), false);
   assert.equal(inputs.Name.disabled, false);
   assert.equal(query(controller).toString(), result.toString(), 'repeated requests must not mutate or disable the form');
});

test('an explicitly changed All facet remains in the request', () => {
   const { controller } = controllerFor([['Category', '*'], ['Other', '*']], ['Category', 'Other']);
   controller.lastFilter = 'Category';
   assert.equal(query(controller).get('Category'), '*');
   assert.equal(query(controller).has('Other'), false);
});

test('clearing preserves explicit All choices and unpaged reports retain page zero', () => {
   const { controller } = controllerFor([['Category', '*'], ['Other', '*'], ['page', '0']], ['Category', 'Other']);
   controller.lastFilter = '_Cleared';
   const result = query(controller);
   assert.equal(result.get('Category'), '*');
   assert.equal(result.get('Other'), '*');
   assert.equal(result.get('page'), '0');
});

test('failed requests reenable search and show an actionable error', () => {
   const { controller } = controllerFor([]);
   controller.afterRequest({ detail: { elt: controller.formTarget, failed: true } });
   assert.equal(controller.searchTarget.disabled, false);
   assert.equal(controller.errorTarget.hidden, false);
   assert.match(controller.statusTarget.textContent, /could not be refreshed/);
});

test('login/diagnostic HTML navigates the full page instead of replacing the report', () => {
   const { controller, navigations } = controllerFor([]);
   const event = { detail: { target: controller.element, shouldSwap: true, xhr: {
      status: 200, responseURL: '/Login?ReturnUrl=report', getResponseHeader: () => null
   } } };
   controller.beforeSwap(event);
   assert.equal(event.detail.shouldSwap, false);
   assert.deepEqual(navigations, ['/Login?ReturnUrl=report']);
});

test('native multiple choices stage until Search, with immediate refresh for single or empty selection', () => {
   const { controller } = controllerFor([['page', '3'], ['last', '']]);
   const submitted = [];
   controller.pageValue = 1;
   controller.formTarget.requestSubmit = () => submitted.push(controller.field('last').value);
   controller.selectChanged({ name: 'Category', multiple: true, selectedOptions: ['A', 'B'] });
   assert.equal(controller.field('page').value, 1);
   assert.equal(submitted.length, 0);
   controller.filterSearch({ detail: { name: 'Category' } });
   assert.deepEqual(submitted, ['Category']);
   controller.selectChanged({ name: 'Category', multiple: true, selectedOptions: [] });
   controller.selectChanged({ name: 'Reason', multiple: false, selectedOptions: ['A'] });
   assert.deepEqual(submitted, ['Category', 'Category', 'Reason']);
   controller.clearing = true;
   controller.selectChanged({ name: 'Category', multiple: true, selectedOptions: [] });
   assert.equal(submitted.length, 3, 'Clear resets all selects before one coordinated request');
});
