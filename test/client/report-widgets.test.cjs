const test = require('node:test');
const assert = require('node:assert/strict');
const fs = require('node:fs');
const path = require('node:path');
const vm = require('node:vm');

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
