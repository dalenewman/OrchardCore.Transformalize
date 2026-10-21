const test = require('node:test');
const assert = require('node:assert/strict');
const fs = require('node:fs');
const vm = require('node:vm');
const path = require('node:path');

function editor(values = {}, selected = ['B']) {
   const context = { Stimulus: { Controller: class {} }, window: { TransformalizeReportControllers: {} } };
   vm.runInNewContext(fs.readFileSync(path.join(__dirname, '../../src/OrchardCore.Transformalize/wwwroot/Scripts/report-editor-controller.js'), 'utf8'), context);
   const result = new context.window.TransformalizeReportControllers['report-editor']();
   const fields = Object.fromEntries(['h', 's', 'f1', 'f2', 'ta', 'e', 'o'].map(name => [name, { value: values[name] || '' }]));
   const boxes = ['A', 'B', 'C'].map(src => ({ checked: selected.includes(src), dataset: { src } }));
   result.element = { querySelector: () => ({ elements: { namedItem: name => fields[name] } }), querySelectorAll: selector => selector === '.field-check' ? boxes : [] };
   result.events = [];
   result.dispatch = event => result.events.push(event);
   result.connect();
   result.applyCommand = command => result.apply({ params: { command } });
   return { result, fields, boxes };
}

test('editor actions preserve existing choices and make filter types mutually exclusive', () => {
   for (const [command, target] of [['search', 's'], ['facet', 'f1'], ['facets', 'f2']]) {
      const { result, fields } = editor({ s: 'A.B', f1: 'B.C', f2: 'A.B' });
      result.applyCommand(command);
      assert.equal(fields[target].value.split('.').filter(src => src === 'B').length, 1);
      for (const other of ['s', 'f1', 'f2'].filter(name => name !== target)) {
         assert.equal(fields[other].value.split('.').includes('B'), false);
      }
      assert.deepEqual(result.events, ['search']);
   }
});

test('formatting and Clear keep the existing editor query contract', () => {
   const { result, fields } = editor({ s: 'A.B', f1: 'C', f2: 'B', ta: 'A.B', e: 'C' });
   result.applyCommand('timeago');
   assert.equal(fields.ta.value, 'A.B');
   result.applyCommand('ellipsis');
   assert.equal(fields.e.value, 'C.B');
   result.applyCommand('clear');
   assert.equal(fields.s.value, 'A');
   assert.equal(fields.f1.value, 'C');
   assert.equal(fields.f2.value, '');
   assert.equal(fields.ta.value, 'A');
   assert.equal(fields.e.value, 'C.B', 'Clear retains ellipsis, matching the existing contract');
});

test('field range selection resets when the editor reconnects', () => {
   const { result, boxes } = editor({}, ['A']);
   result.toggle({ currentTarget: boxes[0] });
   boxes[2].checked = true;
   result.toggle({ currentTarget: boxes[2], shiftKey: true });
   assert.deepEqual(boxes.map(box => box.checked), [true, true, true]);
   result.connect();
   boxes[1].checked = false;
   result.toggle({ currentTarget: boxes[2], shiftKey: true });
   assert.equal(boxes[1].checked, false);
});

function table() {
   const header = ['selection', 'A', 'B', 'C'].map(src => ({ dataset: src === 'selection' ? {} : { src }, draggable: src !== 'selection', focus() { this.focused = true; } }));
   const rows = [header, ['selection', 'sort-A', 'sort-B', 'sort-C'].map(value => ({ value })), ['record', 'data-A', 'data-B', 'data-C'].map(value => ({ value }))]
      .map(cells => ({ cells, insertBefore(cell, reference) {
         cells.splice(cells.indexOf(cell), 1);
         cells.splice(reference ? cells.indexOf(reference) : cells.length, 0, cell);
      } }));
   const result = { rows, tHead: { rows: [rows[0]] } };
   rows.forEach(row => row.cells.forEach(cell => {
      cell.parentElement = row;
      cell.closest = () => result;
      Object.defineProperty(cell, 'cellIndex', { get: () => row.cells.indexOf(cell) });
      Object.defineProperty(cell, 'nextSibling', { get: () => row.cells[row.cells.indexOf(cell) + 1] || null });
   }));
   return result;
}

test('column reordering keeps headers, sort/filter cells, and data rows aligned', () => {
   const { result, fields } = editor();
   const grid = table();
   result.move(grid.rows[0].cells[1], grid.rows[0].cells[3]);
   assert.equal(fields.o.value, 'B.C.A');
   assert.deepEqual(grid.rows[1].cells.map(cell => cell.value), ['selection', 'sort-B', 'sort-C', 'sort-A']);
   assert.deepEqual(grid.rows[2].cells.map(cell => cell.value), ['record', 'data-B', 'data-C', 'data-A']);
   assert.equal(grid.rows[0].cells[3].focused, true);
   assert.deepEqual(result.events, [], 'reordering is local until the next report request');
});

test('keyboard moves exclude the selection column and write the same ordering as drag/drop', () => {
   const { result, fields } = editor();
   const grid = table();
   let prevented = 0;
   result.moveKey({ currentTarget: grid.rows[0].cells[1], altKey: true, key: 'ArrowLeft', preventDefault() { prevented++; } });
   assert.equal(fields.o.value, '');
   result.moveKey({ currentTarget: grid.rows[0].cells[1], altKey: true, key: 'ArrowRight', preventDefault() { prevented++; } });
   assert.equal(fields.o.value, 'B.A.C');
   const dragged = grid.rows[0].cells[2];
   result.dragStart({ currentTarget: dragged, dataTransfer: { setData() {} } });
   result.drop({ currentTarget: grid.rows[0].cells[3], preventDefault() { prevented++; } });
   assert.equal(fields.o.value, 'B.C.A');
   assert.equal(result.dragged, null);
   assert.equal(prevented, 3);
});

test('Hide hides every aligned cell, clears hidden selections and formatting, and excludes hidden columns from order', () => {
   const { result, fields, boxes } = editor({ ta: 'A.B', e: 'B.C', o: 'A.B.C' });
   const grid = table();
   result.element.querySelectorAll = selector => selector === '.field-check' ? boxes : [grid];
   result.applyCommand('hide');
   assert.equal(fields.h.value, 'B');
   assert.equal(fields.ta.value, 'A');
   assert.equal(fields.e.value, 'C');
   assert.equal(fields.o.value, 'A.C');
   assert.equal(boxes[1].checked, false);
   assert.ok(grid.rows.every(row => row.cells[2].hidden));
   result.move(grid.rows[0].cells[1], grid.rows[0].cells[3]);
   assert.equal(fields.o.value, 'C.A');
   assert.deepEqual(result.events, [], 'Hide stays local until the next request, like the old editor');
});
