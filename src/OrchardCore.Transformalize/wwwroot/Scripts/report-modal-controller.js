(function () {
   class ModalController extends Stimulus.Controller {
      static targets = ['dialog', 'frame', 'loading', 'title'];
      static values = { formPath: String, taskPath: String };

      connect() {
         this.adaptLinks(this.element);
         // Map popups and custom widgets insert action links after initial connection.
         this.linkObserver = new MutationObserver(records => records.forEach(record => {
            record.addedNodes.forEach(node => { if (node.nodeType === 1) this.adaptLinks(node); });
         }));
         this.linkObserver.observe(this.element, { childList: true, subtree: true });
         this.instance = new bootstrap.Modal(this.dialogTarget, { backdrop: 'static', keyboard: false });
         this.active = false;
      }

      adaptLinks(root) {
         // Existing arrangements emit these raw HTML links. Adapt that exact
         // legacy contract without loading report.js or exposing a page global.
         const selector = 'a[data-url][onclick], button[data-url][onclick]';
         const links = [...root.querySelectorAll(selector)];
         if (root.matches?.(selector)) links.unshift(root);
         links.forEach(link => {
            if (!/^\s*(?:return\s+)?loadModal\s*\(\s*this\s*\)\s*;?\s*$/.test(link.getAttribute('onclick'))) return;
            link.removeAttribute('onclick');
            if (link.tagName === 'A') link.setAttribute('href', link.getAttribute('data-url'));
            const actions = new Set((link.getAttribute('data-action') || '').split(/\s+/).filter(Boolean));
            actions.add('click->modal#open');
            link.setAttribute('data-action', [...actions].join(' '));
         });
      }

      disconnect() {
         this.linkObserver.disconnect();
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

      openField(event) {
         // Delegate from the field wrapper so links inserted later work too.
         // Explicit modal actions and other handlers may already own the click.
         if (event.defaultPrevented) return;
         const link = event.target.closest?.('a[href]');
         if (!link || !event.currentTarget.contains(link) || link.hasAttribute('download')) return;
         const url = this.fieldUrl(link);
         if (url) this.open(event, link, url.href);
      }

      fieldUrl(link) {
         const href = link.getAttribute('href');
         if (!href || href.trim().startsWith('#')) return null;
         let url;
         try { url = new URL(link.href, window.location.href); } catch { return null; }
         if (!/^https?:$/.test(url.protocol) || url.origin !== window.location.origin) return null;
         // Only full form/task pages in this tenant. Fragment, run and API routes
         // have additional path segments and retain their normal navigation.
         const path = url.pathname.toLowerCase();
         const page = [this.formPathValue, this.taskPathValue].some(prefix => {
            if (!prefix || !path.startsWith(prefix.toLowerCase())) return false;
            const id = path.slice(prefix.length).replace(/\/$/, '');
            return id.length > 0 && !id.includes('/');
         });
         if (!page) return null;
         url.searchParams.set('modal', '1');
         return url;
      }

      open(event, link = event.currentTarget, url = link.getAttribute('data-url') || link.href) {
         if (event.ctrlKey || event.metaKey || event.shiftKey || event.altKey || event.button > 0) return;
         event.preventDefault();
         this.show(link.title, link);
         this.frameTarget.src = url;
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
