const test = require('node:test');
const assert = require('node:assert/strict');
const fs = require('node:fs');
const path = require('node:path');
const vm = require('node:vm');

function setup(available = true) {
   const requests = [], events = [];
   const context = {
      Stimulus: { Controller: class {} }, window: {},
      navigator: { geolocation: available ? { getCurrentPosition: (success, failure, options) => requests.push({ success, failure, options }) } : undefined }
   };
   vm.runInNewContext(fs.readFileSync(path.join(__dirname, '../../src/OrchardCore.Transformalize/wwwroot/Scripts/form-location-controller.js'), 'utf8'), context);
   const controller = new context.window.TransformalizeFormLocationController();
   const fields = ['latitude', 'longitude', 'accuracy', 'altitude', 'altitudeaccuracy', 'speed', 'heading'].map(name => ({
      dataset: { tflLocationCoordinate: name }, value: 'previous'
   }));
   controller.element = { isConnected: true, querySelectorAll: () => fields };
   controller.buttonTarget = { disabled: false, setAttribute() {}, classList: { add() {}, remove() {} } };
   controller.spinnerTarget = {}; controller.iconTarget = {}; controller.statusTarget = {};
   controller.highAccuracyValue = true; controller.maximumAgeValue = 15000; controller.timeoutValue = -1;
   controller.automaticValue = false;
   controller.dispatch = name => events.push(name);
   return { controller, fields, requests, events };
}

test('location uses configured options and writes all coordinates, including missing optional values', () => {
   const { controller, fields, requests, events } = setup();
   controller.connect(); controller.locate();
   assert.equal(requests[0].options.enableHighAccuracy, true);
   assert.equal(requests[0].options.maximumAge, 15000);
   assert.equal(requests[0].options.timeout, Infinity);
   assert.equal(controller.buttonTarget.disabled, true);
   requests[0].success({ coords: { latitude: 0, longitude: 12, accuracy: 4.2, altitude: null, altitudeAccuracy: null, speed: 0, heading: null } });
   assert.deepEqual(fields.map(f => f.value), [0, 12, 4.2, '', '', 0, '']);
   assert.deepEqual(events, ['changed']);
   assert.equal(controller.buttonTarget.disabled, false);
   assert.match(controller.statusTarget.textContent, /accuracy 4 m/);
});

test('location errors retain previous coordinates and allow another attempt', () => {
   const { controller, fields, requests, events } = setup();
   controller.connect(); controller.locate();
   requests[0].failure({ code: 1 });
   assert.ok(fields.every(f => f.value === 'previous'));
   assert.match(controller.statusTarget.textContent, /blocked/);
   assert.equal(controller.buttonTarget.disabled, false);
   assert.deepEqual(events, []);
   controller.locate();
   assert.equal(requests.length, 2);
});

test('a callback from a replaced form cannot write coordinates or mark it dirty', () => {
   const { controller, fields, requests, events } = setup();
   controller.connect(); controller.locate();
   controller.disconnect();
   controller.connect(); // The same Stimulus instance can reconnect.
   assert.equal(controller.buttonTarget.disabled, false);
   requests[0].success({ coords: { latitude: 50, longitude: 10, accuracy: 2 } });
   assert.ok(fields.every(f => f.value === 'previous'));
   assert.deepEqual(events, []);
});

test('automatic acquisition is opt-in per render and duplicate clicks do not create requests', () => {
   const { controller, requests } = setup();
   controller.connect();
   assert.equal(requests.length, 0, 'POST fragments must not reacquire location');
   controller.automaticValue = true;
   controller.connect(); controller.locate();
   assert.equal(requests.length, 1);
});

test('unsupported browsers show feedback and disable only the location control', () => {
   const { controller, requests } = setup(false);
   controller.connect();
   assert.equal(controller.buttonTarget.disabled, true);
   assert.match(controller.statusTarget.textContent, /unavailable/);
   assert.equal(requests.length, 0);
});
