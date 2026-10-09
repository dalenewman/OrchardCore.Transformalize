// Razor owns report data. htmx owns HTML requests. This controller owns browser state.
(function () {
   class ReportController extends Stimulus.Controller {
      static targets = ['form', 'search', 'status', 'error'];
      static values = { page: Number };

      connect() {
         this.lastFilter = null;
         this.clearing = false;
         this.widgets = TransformalizeReportWidgets.connect(this.formTarget);
         this.setBusy(false);
         this.restoreFocus();
      }

      disconnect() {
         this.widgets?.disconnect();
      }

      field(name) {
         return this.formTarget.elements.namedItem(name);
      }

      prepare(event) {
         if (event.target !== this.formTarget || this.formTarget.method.toLowerCase() !== 'get') return;
         if (event.submitter === this.searchTarget) this.field('page').value = this.firstPage;
      }

      get firstPage() {
         return this.pageValue === 0 ? 0 : 1;
      }

      search(name) {
         this.field('page').value = this.firstPage;
         if (name) this.field('last').value = name;
         this.formTarget.requestSubmit();
      }

      searchKey(event) {
         if (event.key !== 'Enter' || !event.target.matches('.field-search, .form-control.date')) return;
         event.preventDefault();
         this.lastFilter = event.target.name;
         this.search(event.target.name);
      }

      filterChanged(event) {
         const input = event.target;
         if (!input.matches('select, .field-search, .form-control.date')) return;
         if (input.matches('select')) { this.selectChanged(input); return; }
         this.lastFilter = input.name;
         this.field('page').value = this.firstPage;
         if (input.matches('select, .form-control.date')) this.search(input.name);
      }

      selectChanged(select) {
         if (this.clearing) return;
         this.lastFilter = select.name;
         this.field('page').value = this.firstPage;
         if (!select.multiple || select.selectedOptions.length === 0) this.search(select.name);
      }

      filterSearch(event) { this.search(event.detail.name); }

      clear() {
         this.clearing = true;
         this.widgets.clear();
         this.formTarget.querySelectorAll('.field-search, .form-control.date').forEach(input => { input.value = ''; });
         this.clearing = false;
         this.lastFilter = '_Cleared';
         this.field('last').value = '';
         this.search();
      }

      paginate(event) {
         if (this.modifiedClick(event)) return;
         event.preventDefault();
         this.field('page').value = event.params.page;
         this.formTarget.requestSubmit();
      }

      resize(event) {
         if (this.modifiedClick(event)) return;
         event.preventDefault();
         this.field('size').value = event.params.size;
         this.search();
      }

      modifiedClick(event) {
         return event.ctrlKey || event.metaKey || event.shiftKey || event.altKey || event.button > 0;
      }

      sort(event) {
         event.preventDefault();
         const button = event.currentTarget;
         button.classList.toggle('btn-primary');
         button.classList.toggle('btn-sort');
         button.parentElement.querySelectorAll('.sortable').forEach(sibling => {
            if (sibling !== button) {
               sibling.classList.remove('btn-primary');
               sibling.classList.add('btn-sort');
            }
         });
         const expression = [];
         this.formTarget.querySelectorAll('td.sorter[data-src]').forEach(cell => {
            const active = cell.querySelector('.sortable.btn-primary');
            if (active) expression.push(cell.dataset.src + active.dataset.reportDirectionParam);
         });
         this.field('sort').value = expression.join('.');
         this.search();
      }

      configureRequest(event) {
         if (event.detail.elt !== this.formTarget) return;
         const parameters = new FormData(this.formTarget);
         for (const name of [...new Set(parameters.keys())]) {
            const input = this.field(name);
            if (name === 'Records' || name === 'select-all') {
               parameters.delete(name);
               continue;
            }
            const values = parameters.getAll(name).map(value => typeof value === 'string' ? value.trim() : value);
            const select = input instanceof HTMLSelectElement;
            const keepAll = select && (this.lastFilter === name || this.lastFilter === '_Cleared');
            const kept = values.filter(value => value !== '' && (value !== '*' || keepAll));
            parameters.delete(name);
            if (name === 'page' && kept[0] === '1') continue;
            kept.forEach(value => parameters.append(name, value));
         }
         // Rebuild request data, rather than disabling live controls to clean up a URL.
         event.detail.parameters = parameters;
      }

      refresh() {
         this.formTarget.requestSubmit();
      }

      download(event) {
         if (this.modifiedClick(event)) return;
         event.preventDefault();
         const url = new URL(event.currentTarget.href);
         url.searchParams.set('random', Math.random());
         window.location.assign(url.href);
      }

      focus(event) {
         if (event.target.matches('input[type="text"]') && event.target.value === '*') event.target.select();
      }

      restoreFocus() {
         const name = this.field('last').value;
         const input = [...this.formTarget.elements].find(element => element.name === name && element.matches('input, select, textarea'));
         if (!name || !input) return;
         const trigger = input.closest?.('[data-controller~="report-filter"]')?.querySelector('[data-report-filter-target="trigger"]');
         if (trigger && !trigger.hidden) {
            trigger.focus({ preventScroll: true });
         } else {
            input.focus({ preventScroll: true });
            if (input.type === 'text') input.setSelectionRange(input.value.length, input.value.length);
         }
      }

      beforeRequest(event) {
         if (event.detail.elt !== this.formTarget) return;
         this.errorTarget.hidden = true;
         this.setBusy(true);
      }

      beforeSwap(event) {
         if (event.detail.target !== this.element) return;
         const xhr = event.detail.xhr;
         // A login redirect or diagnostic view belongs in the full page, not the report fragment.
         if (xhr.status >= 200 && xhr.status < 300 && xhr.getResponseHeader('X-Transformalize-Report') !== 'true') {
            event.detail.shouldSwap = false;
            window.location.assign(xhr.responseURL || window.location.href);
         }
      }

      afterRequest(event) {
         if (event.detail.elt !== this.formTarget) return;
         this.setBusy(false);
         if (event.detail.failed) this.error();
      }

      error() {
         this.setBusy(false);
         this.errorTarget.hidden = false;
         this.statusTarget.textContent = 'The report could not be refreshed.';
      }

      setBusy(busy) {
         this.widgets?.setBusy?.(busy);
         this.element.setAttribute('aria-busy', String(busy));
         this.element.dispatchEvent(new CustomEvent('report:busy', { detail: { busy } }));
         this.searchTarget.disabled = busy;
         this.statusTarget.textContent = busy ? 'Refreshing report…' : 'Report ready.';
         const spinner = document.getElementById('busy');
         if (spinner) spinner.style.display = busy ? 'block' : 'none';
      }
   }

   window.TransformalizeReportControllers = { report: ReportController };
})();
