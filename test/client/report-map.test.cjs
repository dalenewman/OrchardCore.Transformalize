const test = require('node:test');
const assert = require('node:assert/strict');
const fs = require('node:fs');
const path = require('node:path');
const vm = require('node:vm');

function setup(loaded = true) {
   const elements = [];
   const document = { createElement(tag) {
      const node = { tag, children: [], listeners: {}, value: '',
         append(...children) {
            this.children.push(...children);
            if (tag === 'select' && !this.value) this.value = children[0].value;
         },
         addEventListener(type, listener) { this.listeners[type] = listener; },
         remove() { this.removed = true; }
      };
      elements.push(node);
      return node;
   } };
   const handlers = [], sources = new Map(), layers = new Map(), fits = [], popups = [];
   const canvas = { style: {} };
   const map = {
      isStyleLoaded: () => loaded,
      on(type, layer, listener) { handlers.push({ type, layer: listener ? layer : null, listener: listener || layer }); },
      emit(type, event) { handlers.filter(handler => handler.type === type).forEach(handler => handler.listener(event)); },
      addControl(control, position) { this.control = control; this.position = position; control.onAdd(this); },
      setStyle(url) { this.style = url; sources.clear(); layers.clear(); },
      getSource: id => sources.get(id),
      getLayer: id => layers.get(id),
      addSource(id, source) {
         assert.equal(sources.has(id), false, 'must not add a duplicate source');
         sources.set(id, { ...source, setData(data) { this.data = data; } });
      },
      addLayer(layer) { assert.equal(layers.has(layer.id), false); layers.set(layer.id, layer); },
      fitBounds(bounds, options) { fits.push({ bounds, options }); },
      getCanvas: () => canvas
   };
   const context = { window: {}, document, mapboxgl: {
      LngLatBounds: class {
         coordinates = [];
         extend(coordinates) { this.coordinates.push(coordinates); }
      },
      Popup: class {
         setLngLat(coordinates) { this.coordinates = coordinates; return this; }
         setHTML(html) { this.html = html; return this; }
         addTo(map) { popups.push(this); return this; }
      }
   } };
   vm.runInNewContext(fs.readFileSync(path.join(__dirname, '../../src/OrchardCore.Transformalize/wwwroot/Scripts/report-map.js'), 'utf8'), context);
   const layer = { id: 'circle-layer', source: 'circle-layer', type: 'circle', paint: { 'circle-color': 'red' } };
   const adapter = context.window.TransformalizeReportMap.connect(map, layer);
   const select = elements.find(element => element.tag === 'select');
   return { map, adapter, select, sources, layers, fits, popups, handlers, canvas, layer };
}

const data = id => ({ type: 'FeatureCollection', features: [{
   type: 'Feature', geometry: { type: 'Point', coordinates: [-86, 42] },
   properties: { id, description: '<b>Report record ' + id + '</b>' }
}] });

function choose(state, style) {
   state.select.value = 'mapbox://styles/mapbox/' + style;
   state.select.listeners.change();
}

test('report control offers labelled basemaps without becoming a report filter', () => {
   const s = setup();
   assert.equal(s.map.position, 'top-left');
   assert.equal(s.select.value, 'mapbox://styles/mapbox/streets-v11');
   assert.equal(s.select.name, undefined, 'style is not a report parameter');
   assert.deepEqual(s.select.children.map(option => option.textContent),
      ['Streets', 'Satellite + streets', 'Satellite', 'Outdoors', 'Light', 'Dark']);
   const label = s.map.control.container.children[0];
   assert.equal(label.tag, 'label');
   assert.equal(label.children[0].textContent, 'Map style');
   assert.equal(label.children[1], s.select);
   let stopped = 0;
   for (const type of ['change', 'keydown']) {
      s.map.control.container.listeners[type]({ stopPropagation() { stopped++; } });
   }
   assert.equal(stopped, 2, 'map interaction must not reach report search handlers');
   s.map.control.onRemove();
   assert.equal(s.map.control.container.removed, true);
});

test('switching basemaps restores report data and paint without refitting or duplicate popups', () => {
   const s = setup();
   const records = data(1);
   s.adapter.setData(records, true);
   assert.equal(s.fits.length, 1);
   for (const style of ['satellite-streets-v12', 'dark-v11', 'streets-v11']) {
      choose(s, style);
      assert.equal(s.sources.size, 0);
      s.map.emit('style.load');
      assert.equal(s.sources.get('circle-layer').data, records);
      assert.equal(s.layers.get('circle-layer'), s.layer);
      assert.equal(s.fits.length, 1, 'preserve the current camera when switching styles');
   }
   s.map.emit('click', { features: records.features, lngLat: { lng: -86 } });
   assert.equal(s.popups.length, 1);
   assert.equal(s.popups[0].html, records.features[0].properties.description);
   s.map.emit('mouseenter'); assert.equal(s.canvas.style.cursor, 'pointer');
   s.map.emit('mouseleave'); assert.equal(s.canvas.style.cursor, '');
});

test('data arriving during initial or replacement style loads is restored from the latest response', () => {
   const s = setup(false);
   s.adapter.setData(data(1), true);
   assert.equal(s.sources.size, 0);
   choose(s, 'satellite-v9');
   const latest = data(2);
   s.adapter.setData(latest, false);
   assert.equal(s.sources.size, 0);
   s.map.emit('style.load');
   assert.equal(s.sources.get('circle-layer').data, latest);
   assert.equal(s.fits.length, 1, 'deferred initial fit happens once');
   choose(s, 'light-v11');
   choose(s, 'outdoors-v12');
   s.adapter.setData(data(3), false);
   s.map.emit('style.load');
   assert.equal(s.sources.get('circle-layer').data.features[0].properties.id, 3);
   assert.equal(s.map.style, 'mapbox://styles/mapbox/outdoors-v12');
   assert.equal(s.fits.length, 1);
});

test('refresh updates existing sources, empty results do not fit, and retained layers are reused', () => {
   const s = setup();
   s.adapter.setData({ type: 'FeatureCollection', features: [] }, true);
   assert.equal(s.fits.length, 0);
   const source = s.sources.get('circle-layer');
   const latest = data(4);
   s.adapter.setData(latest, false);
   s.map.emit('style.load');
   assert.equal(s.sources.get('circle-layer'), source);
   assert.equal(source.data, latest);
   assert.equal(s.sources.size, 1);
   assert.equal(s.layers.size, 1);
   assert.equal(s.fits.length, 0);
});
