const test = require('node:test');
const assert = require('node:assert/strict');
const fs = require('node:fs');
const path = require('node:path');
const vm = require('node:vm');

function setup(value = '2026-10-07', calendarDate = null, calendarTime = null) {
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
   const input = { value, getAttribute: name => name === 'data-tfl-calendar-date' ? calendarDate
      : name === 'data-tfl-calendar-time' ? calendarTime : null,
      isConnected: true, parentElement: { append: helper => helpers.push(helper) }, focus() { this.focused = true; }, dispatchEvent: event => events.push(event) };
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

test('server-formatted datetime selects its calendar day without changing the submitted text or time', () => {
   const value = '1/31/2006 11:50:00\u202fPM';
   const s = setup(value, '2006-01-31'); s.click();
   assert.equal(s.picker.selected, '2006-01-31');
   assert.equal(s.input.value, value); assert.equal(s.events.length, 0);
   s.click(); s.click();
   assert.equal(s.picker.selected, '2006-01-31');
   assert.equal(s.input.value, value); assert.equal(s.events.length, 0);
});

test('edited, invalid and cleared text never reuse the original server calendar day', () => {
   for (const value of ['2024-02-29', '2026-02-30', 'not a date', '', '2/1/2006 11:50:00 PM']) {
      const s = setup('1/31/2006 11:50:00 PM', '2006-01-31');
      s.input.value = value; s.click();
      if (value === '2024-02-29') assert.equal(s.picker.selected, value);
      else assert.equal(s.picker.selected.length, 0);
      assert.equal(s.input.value, value); assert.equal(s.events.length, 0);
   }
   const s = setup('not a date', '2026-02-30'); s.click();
   assert.equal(s.picker.selected.length, 0);
});

test('form selections preserve a non-midnight server time across repeated picks and reopening', () => {
   const s = setup('1/31/2006 11:50:00\u202fPM', '2006-01-31', '23:50:00');
   s.click(); s.options.onChange([], '2006-02-01');
   assert.equal(s.input.value, '2006-02-01T23:50:00');
   s.click(); s.click();
   assert.equal(s.picker.selected, '2006-02-01');
   s.options.onChange([], '2006-02-02');
   assert.equal(s.input.value, '2006-02-02T23:50:00');
   assert.equal(s.events.length, 2);
});

test('form selections use the current ISO time including seconds and fractions; midnight stays date-only', () => {
   for (const time of ['23:50:01', '00:00:00.0000001', '12:01:02.1234567', '00:00:00', '00:00:00.000']) {
      const s = setup('1/31/2006 11:50:00 PM', '2006-01-31', '23:50:00');
      s.input.value = '2024-02-29T' + time;
      s.click(); assert.equal(s.picker.selected, '2024-02-29');
      s.options.onChange([], '2024-03-01');
      assert.equal(s.input.value, '2024-03-01' + (/[1-9]/.test(time) ? 'T' + time : ''));
      assert.equal(s.events.length, 1);
   }
   const midnight = setup('1/31/2006 12:00:00 AM', '2006-01-31', '');
   midnight.click(); midnight.options.onChange([], '2006-02-01');
   assert.equal(midnight.input.value, '2006-02-01');
});

test('clearing, replacing and invalidating the field do not restore its original time', () => {
   for (const value of ['', '2024-02-29', 'not a date', '2024-02-29T24:00:00', '2024-02-29T12:60:00', '2024-02-29T12:00:60']) {
      const s = setup('1/31/2006 11:50:00 PM', '2006-01-31', '23:50:00');
      s.input.value = value; s.click(); s.options.onChange([], '2024-03-01');
      assert.equal(s.input.value, '2024-03-01');
      assert.equal(s.events.length, 1);
   }
   const s = setup('1/31/2006 11:50:00 PM', '2006-01-31', '23:50:00');
   s.click(); s.options.onChange([], ''); assert.equal(s.input.value, '');
   s.options.onChange([], '2006-02-01'); assert.equal(s.input.value, '2006-02-01');
});

test('calendars without the form time attribute keep date-only selections', () => {
   const s = setup('2006-01-31T23:50:00');
   s.click(); s.options.onChange([], '2006-02-01');
   assert.equal(s.input.value, '2006-02-01');
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
