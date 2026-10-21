// Validation requests replace Razor fields. Submit/Run remains a native POST.
(function () {
   // A replacement controller receives only focus state, never old field values.
   const focusTransfers = new WeakMap();

   function focusName(field) {
      return field.dataset?.tflFocusName || field.name;
   }

   function isVisibleField(field) {
      return !field.disabled && field.type !== 'hidden' && field.offsetParent !== null;
   }

   function isEditableField(field) {
      return isVisibleField(field) && !field.readOnly && field.tabIndex !== -1;
   }

   class FormController extends Stimulus.Controller {
      static targets = ['form', 'submit', 'status', 'error', 'errorMessage'];
      static values = { dirty: Boolean, close: Boolean };

      connect() {
         this.uploads = new Set();
         this.refreshQueued = false;
         this.submitting = false;
         this.busy = false;
         this.editRevision = 0;
         this.keepFocus = false;
         if (this.closeValue) {
            window.parent.postMessage({ action: 'closeModal', reason: 'confirmed' }, window.location.origin);
            return;
         }
         this.widgets = TransformalizeFormWidgets.connect(this.formTarget);
         this.setBusy(false);
         this.restoreFocus();
      }

      disconnect() { this.widgets?.disconnect(); }

      get hasBlockingUploads() {
         return !!this.uploads?.size;
      }

      hasEditsSinceRequest() {
         return (this.editRevision || 0) !== this.requestRevision;
      }

      edited(event) {
         const isLocationChange = event.type === 'tfl-location:changed';
         if (isLocationChange || (event.target.form === this.formTarget && event.target.name)) this.recordEdit();
      }

      recordEdit() {
         this.dirtyValue = true;
         this.editRevision = (this.editRevision || 0) + 1;
         if (this.busy) this.keepFocus = true;
      }

      // Browser checks are advisory: the server can transform any posted value.
      // Map/address selections update a group before one configured postback.
      groupChanged(event) {
         const fields = event.detail.fields.filter(field => field.form === this.formTarget);
         if (!fields.length) return;
         this.recordEdit();
         fields.forEach(field => this.widgets.validateField(field));
         if (event.detail.postBack || fields.some(field => field.dataset.tflPostBack === 'true')) this.validate();
      }

      changed(event) {
         const field = event.target;
         if (field.form !== this.formTarget || !field.matches('[data-tfl-post-back]')) return;
         this.recordEdit();
         this.widgets.validateField(field);
         if (field.dataset.tflPostBack === 'true') this.validate();
      }

      uploadState(event) {
         this.uploads ??= new Set();
         this.recordEdit();
         if (event.detail.blocked) this.uploads.add(event.target);
         else this.uploads.delete(event.target);
         this.setBusy(this.busy);
         if (!this.busy) this.validateQueued();
      }

      validate() {
         if (this.submitting) return;
         if (this.hasBlockingUploads) {
            this.refreshQueued = true;
            return;
         }
         htmx.trigger(this.formTarget, 'tfl:validate');
      }

      validateQueued() {
         if (!this.refreshQueued || this.hasBlockingUploads) return;
         this.refreshQueued = false;
         this.validate();
      }

      focus(event) {
         const name = focusName(event.target);
         if (event.target.form === this.formTarget && name) {
            if (this.busy && event.target !== this.requestFocus) this.keepFocus = true;
            this.formTarget.elements.namedItem('Orchard.Focus').value = name;
         }
      }

      modalFocus() {
         // Opening the modal must not undo a field the user already selected.
         const active = this.element.ownerDocument.activeElement;
         if (active?.form !== this.formTarget) this.restoreFocus();
      }

      restoreFocus() {
         if (this.restoreTransferredFocus()) return;
         const name = this.formTarget.elements.namedItem('Orchard.Focus').value;
         const fields = [...this.formTarget.elements].filter(isEditableField);
         const named = fields.filter(field => focusName(field) === name);
         const target = fields.find(field => field.hasAttribute('autofocus'))
            || named.find(field => field.checked)
            || named[0]
            || fields.find(field => this.isDefaultFocusField(field));
         target?.focus({ preventScroll: true });
         if (target?.matches('input[type="text"], input[type="email"], textarea')) target.select();
      }

      isDefaultFocusField(field) {
         return field.matches('input:not([type="button"]):not([type="submit"]):not([type="reset"]), select, textarea')
            || (field.dataset?.tflFocusName && field.matches('button'));
      }

      restoreTransferredFocus() {
         const document = this.element.ownerDocument;
         const transfer = document && focusTransfers.get(document);
         if (!transfer || transfer.source.isConnected) return false;

         focusTransfers.delete(document);
         const fields = [...this.formTarget.elements];
         const target = fields.find(field => transfer.id && field.id === transfer.id)
            || fields.find(field => this.matchesTransferredFocus(field, transfer));
         if (!target || !isVisibleField(target)) return false;

         target.focus({ preventScroll: true });
         if (transfer.start != null && target.setSelectionRange) {
            try {
               target.setSelectionRange(transfer.start, transfer.end, transfer.direction);
            } catch { /* Non-text fields have no caret. */ }
         }
         return true;
      }

      matchesTransferredFocus(field, transfer) {
         if (!transfer.name || focusName(field) !== transfer.name || field.type !== transfer.type) return false;
         if (['radio', 'checkbox'].includes(field.type)) return field.value === transfer.value;
         return true;
      }

      rememberFocus() {
         const document = this.element.ownerDocument;
         const active = document?.activeElement;
         if (active?.form !== this.formTarget) return;
         if (!this.keepFocus && active === this.requestFocus) return;

         focusTransfers.set(document, {
            source: this.element,
            id: active.id,
            name: focusName(active),
            type: active.type,
            value: active.value,
            start: active.selectionStart,
            end: active.selectionEnd,
            direction: active.selectionDirection
         });
      }

      advance(event) {
         if (event.key !== 'Enter' || event.target.form !== this.formTarget
            || event.target.matches('[data-tfl-places-target="input"]')
            || !event.target.matches('input:not([type="submit"]), select')) return;
         event.preventDefault();
         const fields = [...this.formTarget.elements].filter(isEditableField);
         fields[fields.indexOf(event.target) + 1]?.focus();
      }

      submit(event) {
         if (event.target !== this.formTarget) return;
         if (this.busy || this.hasBlockingUploads || this.submitting) {
            event.preventDefault();
            return;
         }
         this.widgets.validateForm();
         // Do not disable the submitter: its name/value belong in the native POST.
         this.submitting = true;
         this.formTarget.dataset.submitting = 'true';
         this.element.setAttribute('aria-busy', 'true');
         this.statusTarget.textContent = 'Submitting…';
      }

      cancel() {
         if (this.dirtyValue && !window.confirm('Lose unsaved changes?')) return;
         this.dirtyValue = false;
         window.parent.postMessage({ action: 'closeModal', reason: 'cancel' }, window.location.origin);
      }

      leaving(event) {
         if (!this.dirtyValue || this.submitting) return;
         event.preventDefault();
         event.returnValue = '';
      }

      beforeRequest(event) {
         if (event.detail.elt !== this.formTarget) return;
         if (this.hasBlockingUploads) {
            event.preventDefault();
            this.refreshQueued = true;
            return;
         }
         this.invalidResponse = false;
         this.responseErrorMessage = null;
         this.errorTarget.hidden = true;
         this.requestRevision = this.editRevision || 0;
         this.requestFocus = this.element.ownerDocument?.activeElement;
         this.setBusy(true);
      }

      beforeSwap(event) {
         if (event.detail.target !== this.element) return;
         if (this.hasBlockingUploads) {
            event.detail.shouldSwap = false;
            this.refreshQueued = true;
            return;
         }
         const xhr = event.detail.xhr;
         if (xhr.getResponseHeader('X-Transformalize-Form-Error') === 'true') {
            event.detail.shouldSwap = false;
            if (this.discardOutdatedResponse(event.detail)) return;
            this.showResponseErrors(xhr);
            return;
         }
         const isFormFragment = xhr.getResponseHeader('X-Transformalize-Form') === 'true';
         const isSuccessfulResponse = xhr.status >= 200 && xhr.status < 300;
         if (isSuccessfulResponse && !isFormFragment) {
            event.detail.shouldSwap = false;
            this.handleUnexpectedResponse(xhr);
            return;
         }
         if (!event.detail.shouldSwap || !isFormFragment) return;
         if (this.discardOutdatedResponse(event.detail)) return;
         this.rememberFocus();
      }

      discardOutdatedResponse(detail) {
         if (!this.hasEditsSinceRequest()) return false;
         // Fields stay editable. Validate the latest values together, including
         // fields with postback disabled, instead of replacing them with old edits.
         detail.shouldSwap = false;
         this.refreshQueued = true;
         this.keepFocus = true;
         return true;
      }

      showResponseErrors(xhr) {
         this.invalidResponse = true;
         try {
            const result = JSON.parse(xhr.responseText);
            if (Array.isArray(result.fields)) this.widgets.showErrors(result.fields);
            if (typeof result.message === 'string') this.responseErrorMessage = result.message;
         } catch { /* Keep the entered form even if the error response is malformed. */ }
         this.error();
      }

      handleUnexpectedResponse(xhr) {
         const requestedUrl = new URL(this.formTarget.getAttribute('hx-post'), window.location.origin);
         const responseUrl = new URL(xhr.responseURL || requestedUrl.href, window.location.origin);
         if (responseUrl.href !== requestedUrl.href) {
            // Authentication redirects lead to a complete page. The validation
            // endpoint itself returns bare markup and must never be navigated to.
            this.submitting = true;
            window.location.assign(responseUrl.href);
            return;
         }
         this.invalidResponse = true;
         this.error();
      }

      afterRequest(event) {
         if (event.detail.elt !== this.formTarget) return;
         this.setBusy(false);
         if (event.detail.failed || this.invalidResponse) this.error();
         else this.validateQueued();
      }

      error() {
         this.setBusy(false);
         this.errorTarget.hidden = false;
         const message = this.responseErrorMessage || 'The form could not be refreshed. Your entries are still here.';
         if (this.hasErrorMessageTarget) this.errorMessageTarget.textContent = message;
         this.statusTarget.textContent = message;
      }

      setBusy(busy) {
         this.busy = busy;
         this.element.setAttribute('aria-busy', String(busy));
         if (this.hasSubmitTarget) {
            // Keep the action in the Tab order while validation is pending.
            // submit() still prevents execution until the response is current.
            this.submitTarget.disabled = this.hasBlockingUploads;
            this.submitTarget.setAttribute('aria-disabled', String(busy || this.hasBlockingUploads));
         }
         this.statusTarget.textContent = this.statusMessage();
         this.element.dispatchEvent(new CustomEvent('tfl-form:busy', { detail: { busy } }));
      }

      statusMessage() {
         if (this.busy) return 'Refreshing form…';
         if (this.hasBlockingUploads) return 'Finish or clear the selected upload before continuing.';
         return 'Form ready.';
      }
   }
   window.TransformalizeFormController = FormController;
})();
