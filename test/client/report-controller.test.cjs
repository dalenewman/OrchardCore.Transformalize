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
      FormData: ReportFormData, URL, URLSearchParams,
      CustomEvent: class { constructor(type, init) { this.type = type; this.detail = init.detail; } },
      HTMLSelectElement: Select,
      window: { location: { href: 'http://localhost/report', assign: url => navigations.push(url) } },
      document: { getElementById: () => null }
   };
   vm.runInNewContext(fs.readFileSync(path.join(__dirname, '../../src/OrchardCore.Transformalize/wwwroot/Scripts/report-controller.js'), 'utf8'), context);
   const controller = new context.window.TransformalizeReportControllers.report();
   const inputs = Object.fromEntries(entries.map(([name]) => [name, selects.includes(name) ? new Select() : { disabled: false }]));
   controller.pageSizesValue = [20, 25, 50, 100];
   controller.defaultSizeValue = 20;
   controller.formTarget = { entries, elements: { namedItem: name => inputs[name] } };
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

test('choosing All omits every wildcard and keeps focus metadata', () => {
   const { controller } = controllerFor([['Category', '*'], ['Other', '*'], ['last', 'Category'], ['page', '1']], ['Category', 'Other']);
   controller.pageValue = 1;
   controller.formTarget.requestSubmit = () => {};
   controller.selectChanged({ name: 'Category', multiple: false, selectedOptions: ['*'] });
   const result = query(controller);
   assert.equal(result.has('Category'), false);
   assert.equal(result.has('Other'), false);
   assert.equal(result.get('last'), 'Category');
});

test('Clear removes wildcard facets, preserves sort and size, and leaves form controls enabled', () => {
   const facets = ['day', 'dayofweek', 'hour', 'district', 'beat', 'grid', 'ucr_ncic_code', 'geojson-color'];
   const { controller, inputs } = controllerFor([
      ['sort', 'Id.d'], ['size', '25'], ...facets.map(name => [name, '*']), ['last', ''], ['page', '1']
   ], facets);
   controller.widgets = { clear() {} };
   controller.formTarget.querySelectorAll = () => [];
   controller.pageValue = 1;
   controller.formTarget.requestSubmit = () => {};
   controller.clear();
   assert.equal(query(controller).toString(), 'sort=Id.d&size=25');
   assert.equal(controller.field('last').value, '');
   for (const input of Object.values(inputs)) assert.equal(!!input.disabled, false);
});

test('wildcards are omitted individually without dropping concrete repeated choices or page zero', () => {
   const { controller } = controllerFor([['Category', 'A'], ['Category', '*'], ['Category', 'B'], ['Other', ' * '], ['Name', 'Ada*'], ['page', '0']], ['Category', 'Other']);
   const result = query(controller);
   assert.deepEqual(result.getAll('Category'), ['A', 'B']);
   assert.equal(result.has('Other'), false);
   assert.equal(result.get('Name'), 'Ada*', 'only the whole-value wildcard is noise');
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

test('full-page display modes use the same cleaned query contract as table fragments', () => {
   const { controller, navigations, inputs } = controllerFor([
      ['Name', '  Ada & Bob  '], ['Category', 'A'], ['Category', 'B'],
      ['page', '1'], ['Wildcard', '*'], ['sort', '0d'], ['chart-type', 'bar'], ['chart-legend', 'false'], ['Records', '42']
   ]);
   controller.pageValue = 1;
   controller.formTarget.method = 'get';
   controller.formTarget.action = 'https://example.test/tenant/t/report/chart/demo';
   controller.formTarget.hasAttribute = () => false;
   let prevented = 0;
   controller.prepare({ target: controller.formTarget, submitter: controller.searchTarget, preventDefault() { prevented++; } });
   const url = new URL(navigations[0]);
   assert.equal(prevented, 1);
   assert.equal(url.pathname, '/tenant/t/report/chart/demo');
   assert.equal(url.searchParams.get('Name'), 'Ada & Bob');
   assert.deepEqual(url.searchParams.getAll('Category'), ['A', 'B']);
   assert.equal(url.searchParams.get('chart-type'), 'bar');
   assert.equal(url.searchParams.get('chart-legend'), 'false');
   assert.equal(url.searchParams.get('sort'), '0d');
   assert.equal(url.searchParams.has('Wildcard'), false);
   assert.equal(url.searchParams.has('Records'), false);
   assert.equal(url.searchParams.has('page'), false);
   assert.equal(inputs.Name.disabled, false);
});

test('table submissions remain owned by htmx and native POSTs are never intercepted', () => {
   const { controller, navigations } = controllerFor([['page', '2']]);
   let prevented = 0;
   controller.formTarget.method = 'get';
   controller.formTarget.hasAttribute = () => true;
   controller.prepare({ target: controller.formTarget, preventDefault() { prevented++; } });
   controller.formTarget.method = 'post';
   controller.formTarget.hasAttribute = () => false;
   controller.prepare({ target: controller.formTarget, preventDefault() { prevented++; } });
   assert.equal(prevented, 0);
   assert.deepEqual(navigations, []);
});

test('chart controls retain the other display options and use one report submission', () => {
   const { controller } = controllerFor([['chart-type', 'doughnut'], ['chart-legend', 'true'], ['chart-pct', 'false'], ['page', '3']]);
   for (const [name, value] of controller.formTarget.entries) controller.field(name).value = value;
   let submissions = 0;
   controller.formTarget.requestSubmit = () => submissions++;
   controller.chartType({ params: { type: 'hbar' }, preventDefault() {} });
   assert.equal(controller.field('chart-type').value, 'hbar');
   controller.chartToggle({ params: { field: 'chart-legend' } });
   assert.equal(controller.field('chart-legend').value, 'false');
   controller.chartToggle({ params: { field: 'chart-pct' } });
   assert.equal(controller.field('chart-pct').value, 'true');
   assert.equal(controller.field('chart-type').value, 'hbar');
   assert.equal(controller.field('page').value, 1);
   assert.equal(submissions, 3);
});

test('chart point filtering shares cleanup and preserves repeated real filters', () => {
   const { controller, navigations } = controllerFor([
      ['district', '*'], ['day', '*'], ['Category', 'A'], ['Category', 'B'], ['size', '25'], ['page', '3']
   ], ['district', 'day', 'Category']);
   controller.formTarget.action = 'http://localhost/tenant/t/report/chart/crime';
   controller.chartFilter({ detail: { name: 'district', value: '7' } });
   const url = new URL(navigations[0]);
   assert.equal(url.pathname, '/tenant/t/report/chart/crime');
   assert.equal(url.searchParams.has('day'), false);
   assert.equal(url.searchParams.has('page'), false);
   assert.equal(url.searchParams.get('district'), '7');
   assert.deepEqual(url.searchParams.getAll('Category'), ['A', 'B']);
});

test('chart clicks dispatch filtering to the shared controller', async () => {
   let chart;
   const events = [];
   const canvas = { style: {}, dataset: { labels: '["7","8"]', values: '[1,2]', parameterName: 'district', chartTitle: 'District' }, dispatchEvent: event => events.push(event) };
   const context = {
      document: { querySelectorAll: () => [canvas] },
      Chart: class { constructor(element, config) { this.config = config; this.data = config.data; chart = this; } },
      CustomEvent: class { constructor(type, init) { this.type = type; Object.assign(this, init); } },
      requestAnimationFrame() {}
   };
   await vm.runInNewContext(fs.readFileSync(path.join(__dirname, '../../src/OrchardCore.Transformalize/wwwroot/Scripts/chart/charts-loader.js'), 'utf8'), context);
   chart.config.options.onClick({}, []);
   assert.equal(events.length, 0);
   chart.config.options.onClick({}, [{ index: 0 }]);
   assert.equal(events[0].type, 'report-chart:filter');
   assert.equal(events[0].bubbles, true);
   assert.equal(events[0].detail.name, 'district');
   assert.equal(events[0].detail.value, '7');
});

test('configured default sizes are omitted and only genuine nondefault choices remain', () => {
   for (const [chosen, expected] of [['25', null], ['50', '50'], ['1', null], ['75', null], ['bad', null], ['10000', '100']]) {
      const { controller } = controllerFor([['size', chosen], ['sort', 'Id.d']]);
      controller.pageSizesValue = [25, 50, 100];
      controller.defaultSizeValue = 25;
      assert.equal(query(controller).get('size'), expected, chosen);
      assert.equal(query(controller).get('sort'), 'Id.d');
   }
});

test('chart filtering never serializes its one-row fetch size as the report preference', () => {
   const { controller, navigations } = controllerFor([['size', '25'], ['district', '*'], ['page', '1']]);
   controller.pageSizesValue = [25, 50, 100];
   controller.defaultSizeValue = 25;
   controller.formTarget.action = 'http://localhost/t/report/chart/crime';
   controller.chartFilter({ detail: { name: 'district', value: '7' } });
   assert.equal(new URL(navigations[0]).search, '?district=7');
});
