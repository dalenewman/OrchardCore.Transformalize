(function () {
   class ModalController extends Stimulus.Controller {
      static targets = ['dialog', 'frame', 'loading', 'title'];

      connect() {
         // Existing arrangements emit these raw HTML links. Adapt that exact
         // legacy contract without loading report.js or exposing a page global.
         this.element.querySelectorAll('a[data-url][onclick], button[data-url][onclick]').forEach(link => {
            if (!/^\s*(?:return\s+)?loadModal\s*\(\s*this\s*\)\s*;?\s*$/.test(link.getAttribute('onclick'))) return;
            link.removeAttribute('onclick');
            if (link.tagName === 'A') link.setAttribute('href', link.getAttribute('data-url'));
            const actions = new Set((link.getAttribute('data-action') || '').split(/\s+/).filter(Boolean));
            actions.add('click->modal#open');
            link.setAttribute('data-action', [...actions].join(' '));
         });
         this.instance = new bootstrap.Modal(this.dialogTarget, { backdrop: 'static', keyboard: false });
         this.active = false;
      }

      disconnect() {
         // End transitions synchronously when htmx removes the owning report.
         this.dialogTarget.classList.remove('fade');
         this.instance.hide();
         this.instance.dispose();
      }

      show(title, opener) {
         this.opener = opener;
         this.titleTarget.textContent = title || 'Report action';
         this.frameTarget.title = this.titleTarget.textContent;
         this.loadingTarget.hidden = false;
         this.frameTarget.setAttribute('aria-busy', 'true');
         this.active = true;
         this.visible = false;
         this.frameLoaded = false;
         this.confirmed = false;
         this.instance.show();
      }

      open(event) {
         if (event.ctrlKey || event.metaKey || event.shiftKey || event.altKey || event.button > 0) return;
         event.preventDefault();
         this.show(event.currentTarget.title, event.currentTarget);
         this.frameTarget.src = event.currentTarget.getAttribute('data-url') || event.currentTarget.href;
      }

      submit(event) {
         const form = event.detail.form;
         this.show(event.detail.title, document.activeElement);
         form.target = this.frameTarget.name;
         document.body.append(form);
         form.submit();
         form.remove();
      }

      loaded() {
         if (!this.active) return;
         this.frameLoaded = true;
         this.loadingTarget.hidden = true;
         this.frameTarget.setAttribute('aria-busy', 'false');
         this.focusFrame();
      }

      shown() {
         this.visible = true;
         this.focusFrame();
      }

      focusFrame() {
         if (!this.active || !this.visible || !this.frameLoaded) return;
         // Bootstrap's opening transition can focus the dialog after the form
         // has initialized. Its field may already have lost focus, or the form
         // may have initialized while the dialog was still hidden.
         try {
            this.frameTarget.focus({ preventScroll: true });
            this.frameTarget.contentWindow.dispatchEvent(new CustomEvent('tfl-form:focus'));
         } catch { /* A cross-origin action retains its browser-managed focus. */ }
      }

      message(event) {
         if (!this.active || event.origin !== window.location.origin || event.source !== this.frameTarget.contentWindow) return;
         if (event.data?.action !== 'closeModal') return;
         this.confirmed = event.data.reason === 'confirmed';
         this.instance.hide();
      }

      hidden() {
         this.active = false;
         this.visible = false;
         this.frameLoaded = false;
         this.frameTarget.src = 'about:blank';
         this.opener?.focus({ preventScroll: true });
         if (this.confirmed) this.dispatch('confirmed');
      }
   }
   window.TransformalizeReportControllers.modal = ModalController;
})();
