(function () {
   class PlacesController extends Stimulus.Controller {
      static targets = ['input', 'status', 'retry'];
      static values = { key: String, components: Object, postBack: Boolean };

      connect() {
         this.connected = true;
         this.form = this.inputTarget.form;
         this.boundary = this.element.closest('[data-controller~="tfl-form"]');
         this.originalReadonly = this.inputTarget.readOnly;
         this.originalDisabled = this.inputTarget.disabled;
         this.originalPlaceholder = this.inputTarget.placeholder;
         this.busyChanged = event => this.setBusy(event.detail.busy);
         this.boundary?.addEventListener('tfl-form:busy', this.busyChanged);
         this.stopWatching = TransformalizePlacesWidgets.watchFailure(() => this.failure());
         this.setBusy(this.boundary?.getAttribute('aria-busy') === 'true');
         this.initialize();
      }

      async initialize() {
         const generation = this.generation = (this.generation || 0) + 1;
         this.retryTarget.hidden = true;
         this.statusTarget.textContent = 'Loading address suggestions…';
         try {
            const places = await TransformalizePlacesWidgets.load(this.keyValue);
            if (!this.connected || generation !== this.generation) return;
            this.adapter?.disconnect();
            const adapter = TransformalizePlacesWidgets.connect(this.inputTarget, places, place => this.selected(place));
            if (!this.connected || generation !== this.generation) { adapter.disconnect(); return; }
            this.adapter = adapter;
            this.statusTarget.textContent = '';
         } catch {
            if (this.connected && generation === this.generation) this.failure();
         }
      }

      disconnect() {
         this.connected = false;
         this.generation++;
         this.boundary?.removeEventListener('tfl-form:busy', this.busyChanged);
         this.stopWatching?.();
         this.adapter?.disconnect();
         this.adapter = null;
         this.inputTarget.readOnly = this.originalReadonly;
      }

      retry() { if (!this.busy) this.initialize(); }

      failure() {
         if (!this.connected) return;
         this.generation++;
         this.adapter?.disconnect();
         this.adapter = null;
         this.inputTarget.disabled = this.originalDisabled;
         this.inputTarget.placeholder = this.originalPlaceholder;
         this.statusTarget.textContent = 'Address suggestions are unavailable. You can still enter address details manually.';
         this.retryTarget.hidden = false;
      }

      key(event) {
         // Google owns Enter selection. Keep the form from submitting or moving
         // focus while that selection is being handled by the input's listeners.
         if (event.target === this.inputTarget && event.key === 'Enter') event.preventDefault();
      }

      setBusy(busy) {
         this.busy = busy;
         this.inputTarget.readOnly = this.originalReadonly || busy;
         this.retryTarget.disabled = busy;
         if (busy) this.inputTarget.blur();
      }

      selected(place) {
         if (!this.connected || this.busy) return;
         if (!Array.isArray(place?.address_components) || !place.address_components.length) {
            this.statusTarget.textContent = 'No address details were returned. Choose another suggestion or enter the details manually.';
            return;
         }
         const values = new Map();
         place.address_components.forEach(component => {
            (component.types || []).forEach(type => values.set(type, component.long_name || ''));
         });
         const fields = new Set([this.inputTarget]);
         Object.entries(this.componentsValue).forEach(([component, name]) => {
            const field = this.form.elements.namedItem(name);
            if (!field || field.form !== this.form || field.disabled || !('value' in field)) return;
            // Missing components must not retain data from the previous address.
            field.value = values.get(component) || '';
            fields.add(field);
         });
         this.statusTarget.textContent = '';
         this.dispatch('changed', { prefix: 'tfl-places', detail: { fields: [...fields], postBack: this.postBackValue } });
      }
   }
   window.TransformalizeFormPlacesController = PlacesController;
})();
