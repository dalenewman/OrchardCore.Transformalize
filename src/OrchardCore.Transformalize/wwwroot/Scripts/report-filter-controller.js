// Razor's native select owns names, values, labels/counts and submitted selections.
// This controller owns only the searchable popup and its keyboard/focus lifecycle.
(function () {
   let sequence = 0;
   class FilterController extends Stimulus.Controller {
      static targets = ['select', 'trigger', 'summary', 'panel', 'query', 'list', 'status'];

      connect() {
         this.originalHidden = this.selectTarget.hidden;
         this.rows = [...this.selectTarget.options].map((option, index) => this.createOptionRow(option, index));
         this.listTarget.id = 'tfl-filter-list-' + ++sequence;
         this.triggerTarget.setAttribute('aria-controls', this.listTarget.id);
         this.queryTarget.setAttribute('aria-controls', this.listTarget.id);
         this.boundary = this.element.closest('[data-controller~="report"]');
         this.busyChanged = event => this.setBusy(event.detail.busy);
         this.boundary?.addEventListener('report:busy', this.busyChanged);
         this.render();
         this.selectTarget.hidden = true;
         this.triggerTarget.hidden = false;
         this.setBusy(this.boundary?.getAttribute('aria-busy') === 'true');
         // The parent may connect before this child has replaced the native select.
         if (this.element.closest('form')?.elements.namedItem('last')?.value === this.selectTarget.name) {
            this.triggerTarget.focus({ preventScroll: true });
         }
      }

      createOptionRow(option, index) {
         const button = document.createElement('button');
         button.type = 'button';
         button.className = 'tfl-filter-option';
         button.tabIndex = -1;
         button.setAttribute('role', 'option');
         button.textContent = option.textContent;
         button.disabled = option.disabled;
         button.dataset.reportFilterIndexParam = index;
         button.setAttribute('data-action', 'report-filter#choose');
         this.listTarget.append(button);
         return { option, button, text: option.textContent.toLocaleLowerCase() };
      }

      disconnect() {
         this.boundary?.removeEventListener('report:busy', this.busyChanged);
         this.close(false);
         this.listTarget.replaceChildren();
         this.selectTarget.hidden = this.originalHidden;
         this.triggerTarget.hidden = true;
         this.triggerTarget.removeAttribute('aria-controls');
         this.queryTarget.removeAttribute('aria-controls');
      }

      setBusy(busy) {
         this.busy = busy;
         this.triggerTarget.disabled = busy || this.selectTarget.disabled;
         if (busy) this.close(false);
         // Keep the canonical select enabled so it remains in FormData.
      }

      toggle() {
         if (!this.panelTarget.hidden) this.close(true);
         else this.open();
      }

      open() {
         if (this.busy || this.selectTarget.disabled) return;
         this.queryTarget.value = '';
         this.filter();
         this.panelTarget.hidden = false;
         this.triggerTarget.setAttribute('aria-expanded', 'true');
         this.position();
         this.queryTarget.focus({ preventScroll: true });
      }

      close(focus) {
         const wasOpen = !this.panelTarget.hidden;
         this.panelTarget.hidden = true;
         this.triggerTarget.setAttribute('aria-expanded', 'false');
         if (focus && wasOpen) this.triggerTarget.focus({ preventScroll: true });
      }

      outside(event) { if (!this.element.contains(event.target)) this.close(false); }
      focusLeft(event) { if (!this.element.contains(event.relatedTarget)) this.close(false); }
      scrolled(event) { if (!this.panelTarget.contains(event.target)) this.close(false); }

      position() {
         if (this.panelTarget.hidden) return;
         const anchor = this.triggerTarget.getBoundingClientRect();
         const panel = this.panelTarget.getBoundingClientRect();
         this.panelTarget.style.left = Math.max(8, Math.min(anchor.left, window.innerWidth - panel.width - 8)) + 'px';
         const fitsAbove = anchor.top > panel.height;
         const overflowsBelow = anchor.bottom + panel.height > window.innerHeight;
         const top = overflowsBelow && fitsAbove ? anchor.top - panel.height : anchor.bottom;
         this.panelTarget.style.top = top + 'px';
      }

      changed(event) { if (event.target === this.selectTarget) this.render(); }

      render() {
         const selected = this.rows.filter(row => row.option.selected);
         this.summaryTarget.textContent = this.selectionSummary(selected);
         this.rows.forEach(row => row.button.setAttribute('aria-selected', String(row.option.selected)));
         this.filter();
      }

      selectionSummary(selected) {
         if (!selected.length) return 'All';
         if (selected.length > 2) return selected.length + ' selected';
         return selected.map(row => row.option.textContent).join(', ');
      }

      visibleEnabledRows() {
         return this.rows.filter(row => !row.button.hidden && !row.option.disabled);
      }

      filter() {
         const query = this.queryTarget.value.trim().toLocaleLowerCase();
         this.rows.forEach(row => { row.button.hidden = !row.text.includes(query); });
         const count = this.rows.filter(row => !row.button.hidden).length;
         this.statusTarget.textContent = count ? count + (count === 1 ? ' option' : ' options') : 'Not Found';
      }

      choose(event) {
         if (this.busy) return;
         const row = this.rows[event.params.index];
         if (!row || row.option.disabled || row.button.hidden) return;
         if (this.selectTarget.multiple) row.option.selected = !row.option.selected;
         else this.selectTarget.selectedIndex = event.params.index;
         this.notify();
      }

      all() { this.changeMatching(true); }
      none() { this.changeMatching(false); }
      changeMatching(selected) {
         if (this.busy || !this.selectTarget.multiple) return;
         this.visibleEnabledRows().forEach(row => { row.option.selected = selected; });
         this.notify();
      }

      notify() {
         this.render();
         if (!this.selectTarget.multiple || !this.selectTarget.selectedOptions.length) this.close(true);
         this.selectTarget.dispatchEvent(new Event('change', { bubbles: true }));
      }

      search() {
         if (this.busy) return;
         this.close(true);
         this.dispatch('search', { detail: { name: this.selectTarget.name } });
      }

      key(event) {
         if (this.busy) return;
         if (event.key === 'Escape') {
            event.preventDefault();
            event.stopPropagation();
            this.close(true);
            return;
         }
         const isTrigger = event.target === this.triggerTarget;
         const isQuery = event.target === this.queryTarget;
         const row = this.rows.find(row => row.button === event.target);
         if (!isTrigger && !isQuery && !row) return;

         switch (event.key) {
            case 'Home':
            case 'End':
               if (!row) return; // Preserve native caret movement in the query.
               // Fall through to option navigation.
            case 'ArrowDown':
            case 'ArrowUp':
               event.preventDefault();
               this.moveFocus(event.key, row);
               return;
            case ' ':
               if (!row) return; // Spaces remain text in the query.
               // Fall through to option selection.
            case 'Enter': {
               if (!row && !isQuery) return;
               event.preventDefault();
               event.stopPropagation();
               const selected = row || this.visibleEnabledRows()[0];
               if (selected) this.choose({ params: { index: this.rows.indexOf(selected) } });
               return;
            }
         }
      }

      moveFocus(key, row) {
         if (this.panelTarget.hidden) this.open();
         const visible = this.visibleEnabledRows();
         if (!visible.length) return;
         const index = visible.indexOf(row);
         let next;
         switch (key) {
            case 'Home':
               next = 0;
               break;
            case 'End':
               next = visible.length - 1;
               break;
            case 'ArrowDown':
               next = (index + 1) % visible.length;
               break;
            case 'ArrowUp':
               next = index < 0 ? visible.length - 1 : (index + visible.length - 1) % visible.length;
               break;
         }
         visible[next].button.focus({ preventScroll: true });
      }
   }
   window.TransformalizeReportFilterController = FilterController;
})();
