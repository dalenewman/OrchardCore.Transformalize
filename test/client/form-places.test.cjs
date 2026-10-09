const test = require('node:test');
const assert = require('node:assert/strict');
const fs = require('node:fs');
const path = require('node:path');
const vm = require('node:vm');

function setup(load = () => Promise.resolve({})) {
   const events = [], adapters = [], listeners = new Map();
   let notifyFailure;
   const context = {
      window: {}, Stimulus: { Controller: class {} },
      TransformalizePlacesWidgets: {
         load,
         watchFailure(callback) { notifyFailure = callback; return () => { notifyFailure = null; }; },
         connect(input, places, selected) {
            const adapter = { selected, disconnected: false, disconnect() { this.disconnected = true; } };
            adapters.push(adapter); return adapter;
         }
      }
   };
   vm.runInNewContext(fs.readFileSync(path.join(__dirname, '../../src/OrchardCore.Transformalize/wwwroot/Scripts/form-places-controller.js'), 'utf8'), context);
   const c = new context.window.TransformalizeFormPlacesController();
   const fields = {};
   const form = { elements: { namedItem: name => fields[name] } };
   const field = value => ({ value, form, readOnly: false, blur() {} });
   fields.City = field('Previous city'); fields.Zip = field('Old zip'); fields.Suffix = field('1234'); fields.Country = field('United States');
   c.inputTarget = field('Selected address'); c.statusTarget = {}; c.retryTarget = {};
   c.element = { closest: () => ({ addEventListener: (type, handler) => listeners.set(type, handler), removeEventListener: type => listeners.delete(type), getAttribute: () => 'false' }) };
   c.keyValue = 'test'; c.componentsValue = { locality: 'City', postal_code: 'Zip', postal_code_suffix: 'Suffix', country: 'Country', route: 'Missing' };
   c.postBackValue = true;
   c.dispatch = (name, options) => events.push({ name, ...options, values: Object.fromEntries(Object.entries(fields).map(([name, field]) => [name, field.value])) });
   c.connect();
   return { c, fields, events, adapters, listeners, fail: () => notifyFailure() };
}

const tick = () => new Promise(resolve => setImmediate(resolve));
const place = { address_components: [
   { types: ['political', 'locality'], long_name: 'New city' },
   { types: ['postal_code'], long_name: '90210' },
   { types: ['country'], long_name: 'United States' }
] };

test('all mapped components change before one notification and missing components clear stale data', async () => {
   const s = setup(); await tick(); s.adapters[0].selected(place);
   assert.equal(s.events.length, 1);
   assert.equal(s.events[0].values.City, 'New city');
   assert.equal(s.events[0].values.Zip, '90210');
   assert.equal(s.events[0].values.Suffix, '');
   assert.equal(s.events[0].detail.fields.length, 5);
   assert.equal(s.events[0].detail.postBack, true);
});

test('mapped controls from another form or disabled controls remain untouched', async () => {
   const s = setup(); await tick(); s.fields.City.form = {}; s.fields.Zip.disabled = true;
   s.c.selected(place);
   assert.equal(s.fields.City.value, 'Previous city');
   assert.equal(s.fields.Zip.value, 'Old zip');
});

test('incomplete place details leave component values available for manual editing', async () => {
   const s = setup(); await tick(); s.c.selected({ name: 'Typed text' });
   assert.equal(s.fields.City.value, 'Previous city');
   assert.equal(s.events.length, 0);
   assert.match(s.c.statusTarget.textContent, /enter the details manually/);
});

test('disconnect before SDK readiness cannot initialize an obsolete form', async () => {
   let resolve; const s = setup(() => new Promise(done => { resolve = done; }));
   s.c.disconnect(); resolve({}); await tick();
   assert.equal(s.adapters.length, 0); assert.equal(s.listeners.size, 0);
});

test('late selection callbacks and disconnected widgets cannot mutate replaced fields', async () => {
   const s = setup(); await tick(); s.c.disconnect(); s.adapters[0].selected(place);
   assert.equal(s.adapters[0].disconnected, true);
   assert.equal(s.fields.City.value, 'Previous city'); assert.equal(s.events.length, 0);
});

test('SDK failures keep the input editable and offer retry; authentication failure tears down the widget', async () => {
   let unavailable = true;
   const s = setup(() => unavailable ? Promise.reject(new Error('offline')) : Promise.resolve({}));
   await tick(); assert.equal(s.c.retryTarget.hidden, false); assert.equal(s.c.inputTarget.readOnly, false);
   assert.match(s.c.statusTarget.textContent, /enter address details manually/);
   unavailable = false; s.c.retry(); await tick(); assert.equal(s.adapters.length, 1);
   s.c.inputTarget.disabled = true;
   s.c.inputTarget.placeholder = 'Provider error';
   s.fail(); assert.equal(s.adapters[0].disconnected, true); assert.equal(s.c.retryTarget.hidden, false);
   assert.notEqual(s.c.inputTarget.disabled, true);
   assert.equal(s.c.inputTarget.placeholder, s.c.originalPlaceholder);
});

test('Enter stays with Google selection and busy validation keeps values in the native POST', async () => {
   const s = setup(); await tick(); let prevented = false;
   s.c.key({ target: s.c.inputTarget, key: 'Enter', preventDefault: () => { prevented = true; } });
   assert.equal(prevented, true);
   s.listeners.get('tfl-form:busy')({ detail: { busy: true } });
   assert.equal(s.c.inputTarget.readOnly, true); assert.notEqual(s.c.inputTarget.disabled, true);
   s.c.selected(place); assert.equal(s.events.length, 0);
   s.listeners.get('tfl-form:busy')({ detail: { busy: false } });
   assert.equal(s.c.inputTarget.readOnly, false);
});
