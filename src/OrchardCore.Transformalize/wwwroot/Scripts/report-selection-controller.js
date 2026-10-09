// Selection and bulk POSTs are separate from the report's GET refreshes.
(function () {
   class SelectionController extends Stimulus.Controller {
      static targets = ['form', 'record', 'all', 'action', 'tokenContainer'];
      static values = { url: String };

      connect() {
         this.lastChecked = null;
         this.update();
      }

      get token() { return this.tokenContainerTarget.querySelector('input'); }
      get records() { return this.recordTargets.filter(box => !box.disabled); }
      get selected() { return this.records.filter(box => box.checked); }
      get allSelected() { return this.allTargets.some(box => box.checked); }

      toggle(event) {
         const input = event.currentTarget;
         if (this.allTargets.includes(input)) {
            this.allTargets.forEach(box => { box.checked = input.checked; });
            this.records.forEach(box => { box.checked = input.checked; });
            this.lastChecked = null;
         } else {
            const boxes = this.records;
            if (event.shiftKey && boxes.includes(this.lastChecked)) {
               const start = boxes.indexOf(input), end = boxes.indexOf(this.lastChecked);
               boxes.slice(Math.min(start, end), Math.max(start, end) + 1)
                  .forEach(box => { box.checked = this.lastChecked.checked; });
            }
            // Selecting visible rows individually never implies selecting other pages.
            this.allTargets.forEach(box => { box.checked = false; });
            this.lastChecked = input;
         }
         this.update();
      }

      update() {
         const count = this.allSelected ? 'All' : String(this.selected.length);
         this.actionTargets.forEach(action => {
            action.querySelector('[data-selection-count]').textContent = count;
            action.disabled = !this.allSelected && this.selected.length === 0;
         });
      }

      parameters(name) {
         // Retain repeated facets and query controls, but replace action metadata once.
         const data = new FormData(this.formTarget);
         for (const key of ['Records', 'select-all', 'ActionName', 'ActionCount', 'ReturnUrl', this.token.name]) data.delete(key);
         this.selected.forEach(box => data.append('Records', box.value));
         data.set('ActionName', name);
         // The existing server contract uses zero for the complete filtered result.
         data.set('ActionCount', this.allSelected ? '0' : String(this.selected.length));
         data.set('ReturnUrl', window.location.href);
         data.set(this.token.name, this.token.value);
         return data;
      }

      submit(event) {
         event.preventDefault();
         if (!this.allSelected && this.selected.length === 0) return;
         const form = document.createElement('form');
         form.method = 'post';
         form.action = this.urlValue;
         form.hidden = true;
         this.parameters(event.params.name).forEach((value, name) => {
            const input = document.createElement('input');
            input.type = 'hidden'; input.name = name; input.value = value;
            form.append(input);
         });
         if (event.params.modal) {
            const url = new URL(form.action);
            url.searchParams.set('modal', '1');
            form.action = url.href;
            this.dispatch('modal', { detail: { form, title: event.currentTarget.dataset.selectionLabel } });
         } else {
            document.body.append(form);
            form.submit();
            form.remove();
         }
      }
   }
   window.TransformalizeReportControllers.selection = SelectionController;
})();
