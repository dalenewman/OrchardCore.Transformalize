// Compatibility adapter for the site's existing Google Places API setup.
// The SDK is shared; autocomplete instances and popup ownership are per input.
(function () {
   let loading;
   let callbackSequence = 0;
   let authenticationFailed = false;
   let watchingAuthentication = false;
   const failures = new Set();

   function watchAuthentication() {
      if (watchingAuthentication) return;
      watchingAuthentication = true;
      const previous = window.gm_authFailure;
      window.gm_authFailure = function () {
         authenticationFailed = true;
         failures.forEach(failure => failure());
         if (typeof previous === 'function') previous();
      };
   }

   function load(key) {
      watchAuthentication();
      if (authenticationFailed) return Promise.reject(new Error('Google Places authentication failed.'));
      if (window.google?.maps?.places?.Autocomplete) return Promise.resolve(window.google.maps.places);
      if (loading) return loading;
      if (window.google?.maps?.importLibrary) {
         loading = window.google.maps.importLibrary('places').catch(error => { loading = null; throw error; });
         return loading;
      }
      if (!key) return Promise.reject(new Error('Google Places is not configured.'));

      const attempt = new Promise((resolve, reject) => {
         const script = document.createElement('script');
         const callback = 'ready' + ++callbackSequence;
         let finished = false;
         const finish = error => {
            if (finished) return;
            finished = true;
            clearTimeout(timeout);
            failures.delete(failedAuthentication);
            // A late SDK callback after timeout must be harmless.
            window.TransformalizePlacesWidgets[callback] = () => {};
            if (error) { script.remove(); reject(error); }
            else resolve(window.google.maps.places);
         };
         const failedAuthentication = () => finish(new Error('Google Places authentication failed.'));
         failures.add(failedAuthentication);
         window.TransformalizePlacesWidgets[callback] = () => {
            finish(window.google?.maps?.places?.Autocomplete ? null : new Error('Google Places did not initialize.'));
         };
         const timeout = setTimeout(() => finish(new Error('Google Places loading timed out.')), 20000);
         const query = new URLSearchParams({ key, libraries: 'places', v: 'weekly', loading: 'async', callback: 'TransformalizePlacesWidgets.' + callback });
         script.src = 'https://maps.googleapis.com/maps/api/js?' + query;
         script.async = true;
         script.onerror = () => finish(new Error('Google Places could not be loaded.'));
         document.head.append(script);
      });
      loading = attempt.catch(error => { loading = null; throw error; });
      return loading;
   }

   window.TransformalizePlacesWidgets = {
      load,
      watchFailure(failure) {
         watchAuthentication();
         failures.add(failure);
         return () => failures.delete(failure);
      },
      connect(input, places, selected) {
         const before = new Set(document.querySelectorAll('.pac-container'));
         const widget = new places.Autocomplete(input, {
            componentRestrictions: { country: ['us'] },
            fields: ['address_components', 'geometry'],
            types: ['address']
         });
         // Google appends prediction popups outside the form. Remove only this
         // input's new popup, never another address control's predictions.
         const containers = new Set([...document.querySelectorAll('.pac-container')].filter(node => !before.has(node)));
         const captureLinkedPopup = () => {
            const ids = [input.getAttribute('aria-controls'), input.getAttribute('aria-owns')].filter(Boolean).join(' ').split(/\s+/);
            ids.forEach(id => {
               const node = document.getElementById(id);
               if (node?.matches('.pac-container')) containers.add(node);
            });
         };
         captureLinkedPopup();
         const observer = new MutationObserver(captureLinkedPopup);
         observer.observe(input, { attributes: true, attributeFilter: ['aria-controls', 'aria-owns'] });
         let active = true;
         const listener = widget.addListener('place_changed', () => { if (active) selected(widget.getPlace()); });
         return {
            disconnect() {
               if (!active) return;
               active = false;
               captureLinkedPopup();
               observer.disconnect();
               listener.remove();
               window.google.maps.event.clearInstanceListeners(widget);
               window.google.maps.event.clearInstanceListeners(input);
               widget.unbindAll();
               containers.forEach(node => node.remove());
            }
         };
      }
   };
})();
