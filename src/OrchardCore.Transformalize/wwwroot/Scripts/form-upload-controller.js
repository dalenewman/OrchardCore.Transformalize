(function () {
   class UploadController extends Stimulus.Controller {
      static targets = ['input', 'value', 'display', 'choose', 'clear', 'retry', 'progress', 'status', 'preview'];
      static values = { url: String, name: String, scan: Boolean };

      connect() {
         this.pending = false;
         this.failed = false;
         this.formBoundary = this.element.closest('[data-controller~="tfl-form"]');
         this.busy = this.formBoundary?.getAttribute('aria-busy') === 'true';
         this.busyChanged = event => { this.busy = event.detail.busy; this.render(); };
         this.formBoundary?.addEventListener('tfl-form:busy', this.busyChanged);
         this.connectPreview();
         this.connectAdapter();
         this.render();
      }

      connectAdapter() {
         this.adapter = TransformalizeUploadWidgets.connect(this.inputTarget, { url: this.urlValue, name: this.nameValue }, {
            selected: job => this.selected(job),
            progress: (job, percent) => { this.progressTarget.value = percent; },
            completed: (job, result) => this.completed(result),
            failed: (job, message) => this.failure(message)
         });
         this.render();
      }

      disconnect() {
         this.formBoundary?.removeEventListener('tfl-form:busy', this.busyChanged);
         this.adapter?.disconnect();
         if (this.previewError) this.previewElement.removeEventListener('error', this.previewError);
         this.releasePreview();
      }

      selected(job) {
         if (this.busy || this.pending) return false;
         this.job = job;
         this.preview(job.file);
         this.pending = true;
         this.failed = false;
         this.progressTarget.value = 0;
         this.statusTarget.textContent = this.scanValue ? 'Preparing image for scanning…' : 'Preparing and uploading…';
         this.state(true);
         this.render();
         return true;
      }

      completed(result) {
         if (!result || typeof result.id !== 'string' || !result.id || typeof result.message !== 'string') {
            this.failure(result && typeof result.message === 'string' ? result.message + ' Retry or clear it.' : 'The upload returned an unexpected response. Retry or clear it.');
            return;
         }
         this.pending = false;
         this.failed = false;
         this.job = null;
         this.valueTarget.value = this.scanValue ? result.message : result.id;
         if (this.hasDisplayTarget) this.displayTarget.value = result.message;
         this.inputTarget.value = '';
         this.statusTarget.textContent = this.scanValue ? 'Scan complete.' : 'Upload complete.';
         this.render();
         // Update/validate the canonical value before releasing deferred refreshes.
         this.changed();
         this.state(false);
      }

      failure(message) {
         this.pending = false;
         this.failed = true;
         this.statusTarget.textContent = this.scanValue ? message + ' You can clear the image and enter the value manually.' : message;
         this.render();
         // Keep save/refresh blocked until Retry or Clear resolves the selected image.
      }

      choose() {
         if (!this.inputTarget.disabled) this.inputTarget.click();
      }

      retry() { if (this.failed && !this.busy) this.job?.retry(); }

      clear() {
         if (this.busy) return;
         if ((this.valueTarget.value || this.pending || this.failed) && !window.confirm(this.scanValue
            ? 'Are you sure you want to clear this scan and its value?'
            : 'Are you sure you want to clear this attachment from the form? This will not delete the stored file.')) return;
         const restoreChooser = this.element.ownerDocument?.activeElement === this.clearTarget;
         if (this.pending) {
            // Ignore abort's failure callback before releasing the pending selection.
            this.adapter.disconnect();
            this.adapter = null;
         }
         this.job = null;
         this.pending = false;
         this.failed = false;
         this.valueTarget.value = '';
         this.inputTarget.value = '';
         if (this.hasDisplayTarget) this.displayTarget.value = '';
         this.releasePreview();
         if (this.hasPreviewTarget) {
            this.previewElement.removeAttribute('src');
            this.previewElement.hidden = true;
         }
         this.statusTarget.textContent = '';
         this.render();
         if (restoreChooser) (this.scanValue ? this.valueTarget : this.chooseTarget).focus({ preventScroll: true });
         this.changed();
         this.state(false);
         if (!this.adapter) this.connectAdapter();
      }

      connectPreview() {
         if (!this.hasPreviewTarget) return;
         this.previewElement = this.previewTarget;
         const src = this.previewTarget.getAttribute('src');
         if (!src) return;
         // Decorative arrangement output may already display this stored file.
         // Reuse that image (and its styling) instead of adding a second preview.
         const base = this.element.ownerDocument.baseURI;
         const existing = [...(this.formBoundary?.querySelectorAll('[data-tfl-raw-output] img[src]') || [])]
            .find(image => {
               try { return new URL(image.getAttribute('src'), base).href === new URL(src, base).href; }
               catch { return false; }
            });
         if (!existing) return;
         this.previewTarget.hidden = true;
         this.previewTarget.removeAttribute('src');
         this.previewElement = existing;
         if (!existing.alt) existing.alt = this.previewTarget.alt;
         this.previewError = event => this.previewFailed(event);
         existing.addEventListener('error', this.previewError);
      }

      preview(file) {
         if (!this.hasPreviewTarget) return;
         this.releasePreview();
         this.previewElement.removeAttribute('src');
         this.previewElement.hidden = true;
         if (!file?.type?.startsWith('image/')) return;
         this.previewUrl = URL.createObjectURL(file);
         this.previewElement.src = this.previewUrl;
         this.previewElement.alt = 'Preview of ' + file.name;
         this.previewElement.hidden = false;
      }

      releasePreview() {
         if (this.previewUrl) URL.revokeObjectURL(this.previewUrl);
         this.previewUrl = null;
      }

      previewFailed(event) {
         if (event?.target && event.target !== this.previewElement) return;
         this.releasePreview();
         this.previewElement.removeAttribute('src');
         this.previewElement.hidden = true;
      }

      state(blocked) { this.dispatch('state', { detail: { blocked }, prefix: 'tfl-upload' }); }
      changed() { this.valueTarget.dispatchEvent(new Event('change', { bubbles: true })); }

      render() {
         if (this.scanValue) this.valueTarget.readOnly = this.pending || this.failed;
         const blocked = this.pending || this.failed || (!this.scanValue && !!this.valueTarget.value);
         this.inputTarget.disabled = this.busy || blocked;
         // Validation blocks opening the picker, but must not skip this field
         // when the user tabs forward from the preceding postback field.
         this.chooseTarget.disabled = blocked;
         this.chooseTarget.setAttribute('aria-disabled', String(this.inputTarget.disabled));
         this.chooseTarget.classList.toggle('disabled', this.inputTarget.disabled);
         this.clearTarget.setAttribute('aria-disabled', String(this.busy));
         this.clearTarget.hidden = !this.valueTarget.value && !this.pending && !this.failed;
         this.retryTarget.hidden = !this.failed;
         this.retryTarget.disabled = this.busy;
         this.progressTarget.hidden = !this.pending;
         if (this.hasDisplayTarget) this.displayTarget.hidden = !this.valueTarget.value;
      }
   }
   window.TransformalizeFormUploadController = UploadController;
})();
