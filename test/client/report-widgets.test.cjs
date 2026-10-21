const test = require('node:test');
const assert = require('node:assert/strict');
const fs = require('node:fs');
const path = require('node:path');
const vm = require('node:vm');

function calendarWidgets() {
   const context = { window: {} };
   vm.runInNewContext(fs.readFileSync(path.join(__dirname, '../../src/OrchardCore.Transformalize/wwwroot/Scripts/report-widgets.js'), 'utf8'), context);
   return context.window.TransformalizeReportWidgets;
}

test('calendar data uses distinct lowercase IDs and accepts mixed-case aliases without changing source rows', () => {
   const rows = [
      { Id: 168, Title: 'First event', URL: '/event/168', Class: 'event-info', Start: 1000, End: 2000 },
      { ID: 458, title: 'Second event', url: '/event/458', class: 'event-success', start: 3000, end: 4000 },
      { id: 0, Id: 999, title: '' }
   ];
   const events = calendarWidgets().calendarEvents(rows);
   assert.deepEqual(Array.from(events, event => event.id), [168, 458, 0]);
   assert.equal(events[0].title, 'First event');
   assert.equal(events[0].url, '/event/168');
   assert.equal(events[0].class, 'event-info');
   assert.equal(events[0].start, 1000);
   assert.equal(events[0].end, 2000);
   assert.equal(events[2].title, '');
   assert.equal(Object.hasOwn(rows[0], 'id'), false);
});

test('calendar markers open the day list without navigating, while event title links keep their URL behavior', () => {
   let click, captures;
   const marker = {};
   const root = {
      addEventListener(type, handler, capture) { assert.equal(type, 'click'); click = handler; captures = capture; },
      contains(element) { return element === marker; }
   };
   calendarWidgets().connectCalendar(root);
   assert.equal(captures, true, 'Handle clicks before the plugin stops propagation at the day cell.');
   let prevented = 0, stopped = 0;
   const event = {
      target: { closest(selector) { assert.equal(selector, 'a.event'); return marker; } },
      preventDefault() { prevented++; }, stopPropagation() { stopped++; }
   };
   click(event);
   assert.equal(prevented, 1);
   assert.equal(stopped, 0, 'Allow the plugin to open the event list.');
   click({ ...event, target: { closest: () => null } });
   assert.equal(prevented, 1, 'Leave event-item title links alone.');
});

test('report calendars disconnect and clear resets native selections with one change per select', () => {
   const adapters = [], changes = [];
   const selects = [
      { multiple: true, options: [{ selected: true }, { selected: true }] },
      { multiple: false, value: 'A' }
   ];
   selects.forEach(select => { select.dispatchEvent = event => changes.push([select, event]); });
   const root = { querySelectorAll: selector => selector === 'select' ? selects :
      [1, 2].map(id => ({ id, parentElement: { querySelector: () => 'toggle' + id } })) };
   const context = { window: {}, Event, TransformalizeDateWidgets: { connect(input, toggle) {
      const adapter = { input, toggle, setBusy(busy) { this.busy = busy; }, disconnect() { this.disconnected = true; } };
      adapters.push(adapter); return adapter;
   } } };
   vm.runInNewContext(fs.readFileSync(path.join(__dirname, '../../src/OrchardCore.Transformalize/wwwroot/Scripts/report-widgets.js'), 'utf8'), context);
   const widgets = context.window.TransformalizeReportWidgets.connect(root);
   assert.equal(adapters.length, 2); assert.equal(adapters[1].toggle, 'toggle2');
   widgets.setBusy(true); assert.equal(adapters.every(adapter => adapter.busy), true);
   widgets.clear();
   assert.equal(selects[0].options.every(option => !option.selected), true);
   assert.equal(selects[1].value, '*');
   assert.equal(changes.length, 2);
   assert.equal(changes.every(([, event]) => event.type === 'change' && event.bubbles), true);
   widgets.disconnect(); assert.equal(adapters.every(adapter => adapter.disconnected), true);
});
