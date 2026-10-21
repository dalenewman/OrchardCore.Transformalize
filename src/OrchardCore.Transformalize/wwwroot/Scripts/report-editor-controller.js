// Table editing shares the report's GET contract and scoped lifecycle.
(function () {
   class ReportEditorController extends Stimulus.Controller {
      connect() { this.lastChecked = null; this.dragged = null; }

      get form() { return this.element.querySelector('form'); }
      field(name) { return this.form.elements.namedItem(name); }
      get boxes() { return [...this.element.querySelectorAll('.field-check')]; }
      read(name) { return this.field(name).value.split('.').filter(Boolean); }
      write(name, values) { this.field(name).value = values.join('.'); }

      toggle(event) {
         const boxes = this.boxes, input = event.currentTarget;
         if (event.shiftKey && boxes.includes(this.lastChecked)) {
            const start = boxes.indexOf(input), end = boxes.indexOf(this.lastChecked);
            boxes.slice(Math.min(start, end), Math.max(start, end) + 1)
               .forEach(box => { box.checked = this.lastChecked.checked; });
         }
         this.lastChecked = input;
      }

      apply(event) {
         const fields = this.boxes.filter(box => box.checked).map(box => box.dataset.src);
         if (!fields.length) return;
         const state = Object.fromEntries(['h', 's', 'f1', 'f2', 'ta', 'e', 'o'].map(name => [name, this.read(name)]));
         const add = name => { state[name] = [...new Set([...state[name], ...fields])]; };
         const remove = (name, values) => { state[name] = state[name].filter(value => !values.includes(value)); };
         const command = event.params.command;
         switch (command) {
            case 'hide':
               add('h'); remove('ta', state.h); remove('e', state.h); remove('o', state.h);
               // Keep hidden columns in the DOM so subsequent local reordering stays aligned.
               this.element.querySelectorAll('table').forEach(table => {
                  [...table.tHead.rows[0].cells].forEach((header, index) => {
                     if (state.h.includes(header.dataset.src)) {
                        [...table.rows].forEach(row => { if (row.cells[index]) row.cells[index].hidden = true; });
                     }
                  });
               });
               this.boxes.forEach(box => { if (state.h.includes(box.dataset.src)) box.checked = false; });
               break;
            case 'search': add('s'); remove('f1', state.s); remove('f2', state.s); break;
            case 'facet': add('f1'); remove('s', state.f1); remove('f2', state.f1); break;
            case 'facets': add('f2'); remove('s', state.f2); remove('f1', state.f2); break;
            case 'timeago': add('ta'); break;
            case 'ellipsis': add('e'); break;
            case 'clear': ['s', 'f1', 'f2', 'ta'].forEach(name => remove(name, fields)); break;
            default: return;
         }
         Object.entries(state).forEach(([name, values]) => this.write(name, values));
         if (command !== 'hide') this.dispatch('search');
      }

      dragStart(event) {
         this.dragged = event.currentTarget;
         event.dataTransfer.effectAllowed = 'move';
         event.dataTransfer.setData('text/plain', this.dragged.dataset.src);
      }

      dragOver(event) {
         if (this.dragged && this.dragged.parentElement === event.currentTarget.parentElement) {
            event.preventDefault();
            event.dataTransfer.dropEffect = 'move';
         }
      }

      drop(event) {
         event.preventDefault();
         if (this.dragged) this.move(this.dragged, event.currentTarget);
         this.dragged = null;
      }

      dragEnd() { this.dragged = null; }

      moveKey(event) {
         if (!event.altKey || !['ArrowLeft', 'ArrowRight'].includes(event.key)) return;
         event.preventDefault();
         const header = event.currentTarget;
         const headers = [...header.parentElement.cells].filter(cell => cell.draggable && !cell.hidden);
         const target = headers[headers.indexOf(header) + (event.key === 'ArrowLeft' ? -1 : 1)];
         if (target) this.move(header, target);
      }

      move(header, target) {
         if (header === target || header.parentElement !== target.parentElement) return;
         const table = header.closest('table');
         const from = header.cellIndex, to = target.cellIndex;
         [...table.rows].forEach(row => {
            const cell = row.cells[from], destination = row.cells[to];
            if (cell && destination) row.insertBefore(cell, from < to ? destination.nextSibling : destination);
         });
         this.write('o', [...table.tHead.rows[0].cells]
            .filter(cell => cell.dataset.src && !cell.hidden).map(cell => cell.dataset.src));
         header.focus();
      }
   }
   window.TransformalizeReportControllers['report-editor'] = ReportEditorController;
})();
