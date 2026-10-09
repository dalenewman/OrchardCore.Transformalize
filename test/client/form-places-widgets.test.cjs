const test = require('node:test');
const assert = require('node:assert/strict');
const fs = require('node:fs');
const path = require('node:path');
const vm = require('node:vm');

function setup(ready = false) {
   const scripts = [], timers = [], popups = [], widgets = [], observations = [];
   const window = {};
   const document = {
      head: { append: script => scripts.push(script) },
      createElement: () => ({ removed: false, remove() { this.removed = true; } }),
      querySelectorAll: () => popups.filter(p => !p.removed),
      getElementById: id => popups.find(p => p.id === id)
   };
   class Autocomplete {
      constructor(input, options) {
         this.input = input; this.options = options;
         this.popup = { id: 'popup-' + widgets.length, matches: () => true, removed: false, remove() { this.removed = true; } };
         popups.push(this.popup); widgets.push(this);
      }
      addListener(type, handler) { this.handler = handler; return { remove: () => { this.removed = true; } }; }
      getPlace() { return { address_components: [] }; }
      unbindAll() { this.unbound = true; }
   }
   const cleared = [];
   const google = { maps: { places: { Autocomplete }, event: { clearInstanceListeners: item => cleared.push(item) } } };
   if (ready) window.google = google;
   const context = {
      window, document, URLSearchParams,
      setTimeout: callback => { timers.push(callback); return timers.length; }, clearTimeout() {},
      MutationObserver: class { constructor(callback) { this.callback = callback; observations.push(this); } observe() {} disconnect() { this.removed = true; } }
   };
   vm.runInNewContext(fs.readFileSync(path.join(__dirname, '../../src/OrchardCore.Transformalize/wwwroot/Scripts/form-places-widgets.js'), 'utf8'), context);
   return { api: window.TransformalizePlacesWidgets, window, google, scripts, timers, popups, widgets, cleared, observations };
}

function finish(s) {
   s.window.google = s.google;
   const callback = new URL(s.scripts.at(-1).src).searchParams.get('callback').split('.').at(-1);
   s.api[callback]();
}

test('two controls share one SDK load and retain explicit existing API options', async () => {
   const s = setup(); const first = s.api.load('configured-key'), second = s.api.load('configured-key');
   assert.equal(first, second); assert.equal(s.scripts.length, 1);
   finish(s); await first;
   const input = { getAttribute: () => null };
   s.api.connect(input, s.google.maps.places, () => {});
   assert.equal(s.widgets[0].options.types.join(','), 'address');
   assert.equal(s.widgets[0].options.componentRestrictions.country.join(','), 'us');
   assert.equal(s.widgets[0].options.fields.join(','), 'address_components,geometry');
});

test('network failure and timeout offer retry while late SDK callbacks are harmless', async () => {
   const s = setup(); const first = s.api.load('key'); s.scripts[0].onerror();
   await assert.rejects(first); assert.equal(s.scripts[0].removed, true);
   const second = s.api.load('key'); assert.equal(s.scripts.length, 2);
   s.timers.at(-1)(); await assert.rejects(second);
   finish(s);
});

test('each widget removes only its popup and ignores late selection notifications', () => {
   const s = setup(true); let selected = 0;
   const input = { getAttribute: () => null };
   const first = s.api.connect(input, s.google.maps.places, () => selected++);
   s.api.connect(input, s.google.maps.places, () => selected++);
   s.widgets[0].handler(); assert.equal(selected, 1);
   first.disconnect(); s.widgets[0].handler(); s.widgets[1].handler();
   assert.equal(selected, 2); assert.equal(s.popups[0].removed, true); assert.equal(s.popups[1].removed, false);
   assert.equal(s.widgets[0].unbound, true); assert.equal(s.observations[0].removed, true);
   assert.equal(s.cleared.length, 2);
});

test('authentication errors notify active controls and retain any existing application hook', async () => {
   const s = setup(true); let prior = 0, active = 0;
   s.window.gm_authFailure = () => prior++;
   const stop = s.api.watchFailure(() => active++);
   s.window.gm_authFailure(); assert.equal(prior, 1); assert.equal(active, 1);
   stop(); s.window.gm_authFailure(); assert.equal(active, 1);
   await assert.rejects(s.api.load('key'));
});

test('an already-loaded SDK is reused and a missing key leaves manual entry possible', async () => {
   const s = setup(true); const places = await s.api.load(''); assert.equal(places, s.google.maps.places); assert.equal(s.scripts.length, 0);
   const unavailable = setup(); await assert.rejects(unavailable.api.load('')); assert.equal(unavailable.scripts.length, 0);
});
