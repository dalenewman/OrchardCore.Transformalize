const test = require('node:test');
const assert = require('node:assert/strict');
const fs = require('node:fs');
const path = require('node:path');
const vm = require('node:vm');

function setup(multiple = true) {
   let focus;
   const node = () => ({ hidden: false, disabled: false, style: {}, dataset: {}, attrs: {},
      setAttribute(name, value) { this.attrs[name] = value; },
      removeAttribute(name) { delete this.attrs[name]; },
      focus() { focus = this; }, contains(target) { return target === this; },
      getBoundingClientRect() { return { left: 30, top: 30, bottom: 60, width: 220, height: 240 }; }
   });
   const context = { Stimulus: { Controller: class {} }, Event,
      window: { innerWidth: 900, innerHeight: 700 }, document: { createElement: node } };
   vm.runInNewContext(fs.readFileSync(path.join(__dirname, '../../src/OrchardCore.Transformalize/wwwroot/Scripts/report-filter-controller.js'), 'utf8'), context);
   const controller = new context.window.TransformalizeReportFilterController();
   const options = ['Alpha (3)', 'Beta (2)', 'Gamma (1)', 'Unavailable'].map((textContent, index) =>
      ({ textContent, selected: index === 0, disabled: index === 3 }));
   const changes = [], searches = [], listeners = new Map();
   const select = Object.assign(node(), { name: 'Category', multiple, options,
      dispatchEvent(event) { changes.push(event); }
   });
   Object.defineProperties(select, {
      selectedOptions: { get() { return options.filter(option => option.selected); } },
      selectedIndex: { set(index) { options.forEach((option, i) => { option.selected = i === index; }); } }
   });
   const boundary = { addEventListener(type, fn) { listeners.set(type, fn); }, removeEventListener(type) { listeners.delete(type); }, getAttribute() { return 'false'; } };
   controller.element = { closest: selector => selector === 'form' ? { elements: { namedItem: () => ({ value: 'Category' }) } } : boundary, contains: () => false };
   controller.selectTarget = select;
   for (const name of ['trigger', 'summary', 'panel', 'query', 'list', 'status']) controller[name + 'Target'] = node();
   controller.panelTarget.hidden = true; controller.triggerTarget.hidden = true; controller.queryTarget.value = '';
   controller.listTarget.append = () => {};
   controller.listTarget.replaceChildren = () => { controller.listCleared = true; };
   controller.dispatch = (name, init) => searches.push([name, init.detail.name]);
   controller.connect();
   return { controller, select, changes, searches, listeners, focused: () => focus };
}

function key(controller, target, value) {
   let prevented = false;
   controller.key({ target, key: value, preventDefault() { prevented = true; }, stopPropagation() {} });
   return prevented;
}

test('multiple selections remain canonical, matching On/Off exclude disabled choices, Search requests once', () => {
   const { controller: c, select, changes, searches } = setup();
   c.open(); c.queryTarget.value = 'beTA'; c.filter(); c.all();
   assert.deepEqual(select.selectedOptions.map(option => option.textContent), ['Alpha (3)', 'Beta (2)']);
   assert.equal(c.panelTarget.hidden, false);
   assert.equal(changes.length, 1); assert.equal(changes[0].bubbles, true);
   c.search(); assert.deepEqual(searches, [['search', 'Category']]);
   c.open(); c.queryTarget.value = 'not found'; c.filter();
   assert.equal(c.statusTarget.textContent, 'Not Found');
   c.queryTarget.value = ''; c.filter(); c.all();
   assert.equal(select.selectedOptions.length, 3); assert.equal(c.summaryTarget.textContent, '3 selected');
   c.none(); assert.equal(select.selectedOptions.length, 0); assert.equal(c.panelTarget.hidden, true);
});

test('single choices dispatch a native change and close, keyboard skips disabled and hidden options', () => {
   const { controller: c, select, changes, focused } = setup(false);
   assert.equal(focused(), c.triggerTarget, 'fragment connection restores the replacement trigger');
   key(c, c.triggerTarget, 'ArrowUp'); assert.equal(focused(), c.rows[2].button);
   key(c, c.rows[2].button, 'Home'); assert.equal(focused(), c.rows[0].button);
   c.queryTarget.value = 'Beta'; c.filter();
   key(c, c.queryTarget, 'Enter');
   assert.equal(select.selectedOptions[0].textContent, 'Beta (2)');
   assert.equal(changes.length, 1); assert.equal(c.panelTarget.hidden, true);
   assert.equal(focused(), c.triggerTarget);
});

test('busy closes popup without disabling submitted select; disconnect restores native fallback and listeners', () => {
   const { controller: c, select, changes, listeners, focused } = setup();
   c.open(); key(c, c.queryTarget, 'Escape'); assert.equal(focused(), c.triggerTarget);
   c.open(); listeners.get('report:busy')({ detail: { busy: true } });
   assert.equal(c.panelTarget.hidden, true); assert.equal(c.triggerTarget.disabled, true);
   assert.equal(select.disabled, false); c.choose({ params: { index: 1 } }); assert.equal(changes.length, 0);
   c.disconnect(); assert.equal(select.hidden, false); assert.equal(c.triggerTarget.hidden, true);
   assert.equal(listeners.size, 0); assert.equal(c.listCleared, true);
});

test('Tab can reach popup actions and leaving closes without pulling focus back', () => {
   const { controller: c, focused } = setup();
   c.element.contains = target => target === c.queryTarget || target === c.triggerTarget;
   c.open();
   assert.equal(key(c, c.queryTarget, 'Tab'), false, 'native Tab reaches On/Off/Search');
   c.focusLeft({ relatedTarget: c.triggerTarget });
   assert.equal(c.panelTarget.hidden, false);
   c.focusLeft({ relatedTarget: {} });
   assert.equal(c.panelTarget.hidden, true);
   assert.equal(focused(), c.queryTarget, 'closing must not refocus the trigger on Tab exit');
});

test('keyboard navigation wraps through enabled matches and preserves query editing keys', () => {
   const { controller: c, focused, changes } = setup();
   key(c, c.triggerTarget, 'ArrowDown');
   assert.equal(focused(), c.rows[0].button);
   key(c, c.rows[0].button, 'ArrowUp');
   assert.equal(focused(), c.rows[2].button);
   key(c, c.rows[2].button, 'ArrowDown');
   assert.equal(focused(), c.rows[0].button);
   key(c, c.rows[0].button, 'End');
   assert.equal(focused(), c.rows[2].button);

   c.queryTarget.value = 'Beta';
   c.filter();
   key(c, c.queryTarget, 'ArrowUp');
   assert.equal(focused(), c.rows[1].button);
   key(c, c.rows[1].button, 'ArrowDown');
   assert.equal(focused(), c.rows[1].button, 'a single match wraps to itself');
   key(c, c.rows[1].button, ' ');
   assert.equal(c.rows[1].option.selected, true);
   assert.equal(changes.length, 1);

   for (const value of ['Home', 'End', ' ', 'Tab']) {
      assert.equal(key(c, c.queryTarget, value), false, value + ' keeps its native query behavior');
   }
   c.queryTarget.value = 'no matching options';
   c.filter();
   c.queryTarget.focus();
   key(c, c.queryTarget, 'ArrowDown');
   key(c, c.queryTarget, 'Enter');
   assert.equal(focused(), c.queryTarget);
   assert.equal(changes.length, 1, 'an empty search cannot change the selection');
});
