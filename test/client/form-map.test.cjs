const test = require('node:test');
const assert = require('node:assert/strict');
const fs = require('node:fs');
const path = require('node:path');
const vm = require('node:vm');

function setup(lat = '38.5', lng = '-121.4', token = 'fixture-token') {
   const maps = [], markers = [], events = [], listeners = new Map();
   class MapStub {
      constructor(options) { this.options = options; this.handlers = new Map(); maps.push(this); }
      addControl() {}
      on(name, handler) { this.handlers.set(name, handler); }
      off(name) { this.handlers.delete(name); }
      flyTo(options) { this.center = options.center; }
      resize() { this.resized = true; }
      remove() { this.removed = true; }
   }
   class MarkerStub {
      constructor() { this.handlers = new Map(); markers.push(this); }
      setLngLat(position) { this.position = position; return this; }
      getLngLat() { return this.draggedPosition; }
      addTo() { return this; }
      on(name, handler) { this.handlers.set(name, handler); }
      off(name) { this.handlers.delete(name); }
      remove() { this.removed = true; }
   }
   const mapboxgl = { Map: MapStub, Marker: MarkerStub, NavigationControl: class {}, GeolocateControl: class {} };
   const context = { Stimulus: { Controller: class {} }, window: { mapboxgl }, mapboxgl };
   vm.runInNewContext(fs.readFileSync(path.join(__dirname, '../../src/OrchardCore.Transformalize/wwwroot/Scripts/form-map-controller.js'), 'utf8'), context);
   const controller = new context.window.TransformalizeFormMapController();
   const latitude = { value: lat }, longitude = { value: lng };
   const form = {
      elements: { namedItem: name => name === 'Lat' ? latitude : longitude },
      addEventListener: (name, handler) => listeners.set(name, handler),
      removeEventListener: name => listeners.delete(name)
   };
   controller.element = { closest: () => form }; controller.canvasTarget = {}; controller.errorTarget = { hidden: true };
   controller.latitudeValue = 'Lat'; controller.longitudeValue = 'Lng'; controller.tokenValue = token;
   controller.dispatch = (name, options) => events.push({ name, fields: options.detail.fields, values: [latitude.value, longitude.value] });
   controller.connect();
   return { controller, maps, markers, events, latitude, longitude, listeners };
}

test('map initializes existing coordinates and preserves zero latitude', () => {
   const { maps, markers } = setup('0', '12');
   assert.equal(maps[0].options.center.join(','), '12,0');
   assert.equal(maps[0].options.zoom, 14);
   assert.equal(markers[0].position.join(','), '12,0');
});

test('map click updates both coordinates before one notification and normalizes wrapped longitude', () => {
   const { maps, events, latitude, longitude } = setup();
   maps[0].handlers.get('click')({ lngLat: { lat: 12.34, lng: 190 } });
   assert.equal(events.length, 1);
   assert.deepEqual(events[0].values, ['12.3400000', '-170.0000000']);
   assert.equal(events[0].fields[0], latitude);
   assert.equal(events[0].fields[1], longitude);
});

test('marker drag reports the same pair and typing moves the marker only for valid coordinates', () => {
   const { maps, markers, events, latitude, longitude, listeners } = setup();
   markers[0].draggedPosition = { lat: 10, lng: 20 };
   markers[0].handlers.get('dragend')();
   assert.deepEqual(events[0].values, ['10.0000000', '20.0000000']);
   latitude.value = '0'; longitude.value = '0';
   listeners.get('change')({ target: latitude });
   assert.equal(maps[0].center.join(','), '0,0');
   latitude.value = '91';
   listeners.get('change')({ target: latitude });
   assert.equal(maps[0].center.join(','), '0,0');
   assert.equal(events.length, 1, 'typing is handled by the ordinary form change action');
});

test('disconnect removes map, marker and field listeners, including after repeated swaps', () => {
   const { controller, maps, markers, listeners } = setup();
   controller.disconnect();
   assert.equal(maps[0].removed, true); assert.equal(markers[0].removed, true);
   assert.equal(maps[0].handlers.size, 0); assert.equal(markers[0].handlers.size, 0);
   assert.equal(listeners.size, 0);
   controller.connect(); controller.disconnect();
   assert.equal(maps[1].removed, true);
   assert.equal(listeners.size, 0);
});

test('map failure leaves coordinate inputs available with visible feedback', () => {
   const { controller, maps, latitude } = setup();
   maps[0].handlers.get('error')({ error: new Error('unavailable') });
   assert.equal(controller.errorTarget.hidden, false);
   assert.equal(latitude.value, '38.5');
   const missingToken = setup('38.5', '-121.4', '');
   assert.equal(missingToken.maps.length, 0);
   assert.equal(missingToken.controller.errorTarget.hidden, false);
});
