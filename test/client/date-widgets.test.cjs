const test = require('node:test');
const assert = require('node:assert/strict');
const fs = require('node:fs');
const path = require('node:path');
const vm = require('node:vm');

function setup(value = '2026-10-07') {
   const attributes = new Map(), events = [], helpers = [], handlers = new Map(), day = { focus() { this.focused = true; } };
   let options;
   const picker = {
      isOpen: false, selected: null,
      calendarContainer: { querySelector: () => day, setAttribute() {} },
      setDate(value, change) { this.selected = value; assert.equal(change, false); },
      open() { this.isOpen = true; options.onOpen(); },
      close() { this.isOpen = false; options.onClose(); },
      destroy() { this.destroyed = true; options.onClose(); }
   };
   const context = { window: {}, Event: class { constructor(type, options) { this.type = type; this.bubbles = options.bubbles; } },
      document: { createElement: () => ({ setAttribute() {}, remove() { this.removed = true; } }) },
      flatpickr(helper, config) { options = config; return picker; }
   };
   vm.runInNewContext(fs.readFileSync(path.join(__dirname, '../../src/OrchardCore.Transformalize/wwwroot/Scripts/date-widgets.js'), 'utf8'), context);
   const input = { value, isConnected: true, parentElement: { append: helper => helpers.push(helper) }, focus() { this.focused = true; }, dispatchEvent: event => events.push(event) };
   const toggle = {
      setAttribute: (name, value) => attributes.set(name, value), getAttribute: name => attributes.get(name),
      removeAttribute: name => attributes.delete(name),
      addEventListener: (name, handler) => handlers.set(name, handler), removeEventListener: name => handlers.delete(name)
   };
   const adapter = context.window.TransformalizeDateWidgets.connect(input, toggle);
   return { input, toggle, picker, options, events, helpers, handlers, attributes, day, adapter,
      click() { handlers.get('click')({ preventDefault() {}, stopPropagation() {} }); } };
}

test('opening and cancelling preserve invalid typed text and initial valid values', () => {
   const s = setup('2026-02-30'); s.click();
   assert.equal(s.input.value, '2026-02-30'); assert.equal(s.picker.selected.length, 0);
   assert.equal(s.day.focused, true); assert.equal(s.attributes.get('aria-expanded'), 'true');
   s.click(); assert.equal(s.input.value, '2026-02-30'); assert.equal(s.input.focused, true);
   s.input.value = '2024-02-29'; s.click(); assert.equal(s.picker.selected, '2024-02-29');
   assert.equal(s.events.length, 0);
});

test('calendar selection emits one canonical change; helper never has a posted name', () => {
   const s = setup(); s.click(); s.options.onChange([], '2026-10-09');
   assert.equal(s.input.value, '2026-10-09');
   assert.equal(s.events.length, 1); assert.equal(s.events[0].type, 'change'); assert.equal(s.events[0].bubbles, true);
   assert.equal(s.helpers[0].name, undefined); assert.equal(s.helpers[0].hidden, true);
});

test('busy requests close the calendar and block interaction; teardown ignores late selection callbacks', () => {
   const s = setup(); s.click(); s.adapter.setBusy(true);
   assert.equal(s.toggle.disabled, true); assert.equal(s.picker.isOpen, false);
   s.options.onChange([], '2026-10-08'); assert.equal(s.events.length, 0);
   s.adapter.setBusy(false); assert.equal(s.toggle.disabled, false);
   s.input.readOnly = true; s.click(); assert.equal(s.picker.isOpen, false);
   s.options.onChange([], '2026-10-09'); assert.equal(s.events.length, 0);
   s.input.readOnly = false; s.adapter.disconnect();
   s.options.onChange([], '2026-10-10');
   assert.equal(s.events.length, 0); assert.equal(s.input.value, '2026-10-07');
   assert.equal(s.helpers[0].removed, true); assert.equal(s.picker.destroyed, true);
   assert.equal(s.handlers.size, 0); assert.equal(s.attributes.size, 0);
});
