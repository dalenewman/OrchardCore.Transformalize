(function () {
   const coordinateNames = {
      latitude: 'latitude', longitude: 'longitude', accuracy: 'accuracy',
      altitude: 'altitude', altitudeaccuracy: 'altitudeAccuracy', speed: 'speed', heading: 'heading'
   };

   class FormLocationController extends Stimulus.Controller {
      static targets = ['button', 'spinner', 'icon', 'status'];
      static values = { automatic: Boolean, highAccuracy: Boolean, maximumAge: Number, timeout: Number };

      connect() {
         this.active = true;
         this.requestId = (this.requestId || 0) + 1;
         this.setPending(false);
         this.statusTarget.textContent = '';
         if (!navigator.geolocation) {
            this.statusTarget.textContent = 'Location is unavailable in this browser.';
            this.buttonTarget.disabled = true;
         } else if (this.automaticValue) {
            this.locate();
         }
      }

      locate() {
         if (this.pending || !navigator.geolocation) return;
         const requestId = ++this.requestId;
         this.setPending(true);
         this.statusTarget.textContent = 'Finding location…';
         const current = () => this.active && this.element.isConnected && requestId === this.requestId;
         const success = position => {
            if (!current()) return;
            this.element.querySelectorAll('[data-tfl-location-coordinate]').forEach(field => {
               const property = coordinateNames[field.dataset.tflLocationCoordinate];
               if (property) field.value = position.coords[property] ?? '';
            });
            this.setPending(false);
            this.buttonTarget.classList.remove('btn-danger');
            this.buttonTarget.classList.add('btn-success');
            this.statusTarget.textContent = `Location updated (accuracy ${Math.round(position.coords.accuracy)} m).`;
            this.dispatch('changed');
         };
         const failure = error => {
            if (!current()) return;
            this.setPending(false);
            this.buttonTarget.classList.remove('btn-success');
            this.buttonTarget.classList.add('btn-danger');
            this.statusTarget.textContent = {
               1: 'Location access was blocked. You can try again after allowing access in your browser.',
               2: 'Your location could not be determined. Try again.',
               3: 'The location request timed out. Try again.'
            }[error.code] || 'The location request failed. Try again.';
         };
         try {
            navigator.geolocation.getCurrentPosition(success, failure, {
               enableHighAccuracy: this.highAccuracyValue,
               maximumAge: this.maximumAgeValue < 0 ? Infinity : this.maximumAgeValue,
               timeout: this.timeoutValue < 0 ? Infinity : this.timeoutValue
            });
         } catch (_) {
            failure({ code: 0 });
         }
      }

      setPending(pending) {
         this.pending = pending;
         this.buttonTarget.disabled = pending;
         this.buttonTarget.setAttribute('aria-busy', String(pending));
         this.spinnerTarget.hidden = !pending;
         this.iconTarget.hidden = pending;
      }

      disconnect() {
         // getCurrentPosition has no cancellation API. Ignore callbacks after a swap.
         this.active = false;
         this.requestId++;
      }
   }
   window.TransformalizeFormLocationController = FormLocationController;
})();
