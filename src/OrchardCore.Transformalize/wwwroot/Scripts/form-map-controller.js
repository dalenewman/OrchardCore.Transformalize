(function () {
   class FormMapController extends Stimulus.Controller {
      static targets = ['canvas', 'error'];
      static values = { latitude: String, longitude: String, token: String };

      connect() {
         this.form = this.element.closest('form');
         this.latitude = this.form.elements.namedItem(this.latitudeValue);
         this.longitude = this.form.elements.namedItem(this.longitudeValue);
         if (!this.latitude || !this.longitude || !this.tokenValue || !window.mapboxgl) {
            this.showError();
            return;
         }

         try {
            const position = this.readPosition() || [0, 0];
            this.map = new mapboxgl.Map({
               container: this.canvasTarget,
               accessToken: this.tokenValue,
               style: 'mapbox://styles/mapbox/streets-v11',
               center: position,
               zoom: position.some(value => value !== 0) ? 14 : 2
            });
            this.map.addControl(new mapboxgl.NavigationControl());
            this.map.addControl(new mapboxgl.GeolocateControl({ positionOptions: { enableHighAccuracy: true } }));
            this.marker = new mapboxgl.Marker({ draggable: true, color: '#e74c3c' })
               .setLngLat(position).addTo(this.map);

            this.onClick = event => this.setPosition(event.lngLat);
            this.onDrag = () => this.setPosition(this.marker.getLngLat());
            this.onLoad = () => this.map.resize();
            this.onError = () => this.showError();
            this.onFieldChange = event => {
               if (event.target !== this.latitude && event.target !== this.longitude) return;
               const coordinates = this.readPosition();
               if (!coordinates) return;
               this.marker.setLngLat(coordinates);
               this.map.flyTo({ center: coordinates });
            };
            this.map.on('click', this.onClick);
            this.map.on('load', this.onLoad);
            this.map.on('error', this.onError);
            this.marker.on('dragend', this.onDrag);
            this.form.addEventListener('change', this.onFieldChange);
         } catch (_) {
            this.disconnect();
            this.showError();
         }
      }

      readPosition() {
         const latitude = this.latitude.value.trim();
         const longitude = this.longitude.value.trim();
         if (!latitude || !longitude) return null;
         const lat = Number(latitude), lng = Number(longitude);
         return Number.isFinite(lat) && Number.isFinite(lng) && Math.abs(lat) <= 90 && Math.abs(lng) <= 180 ? [lng, lat] : null;
      }

      setPosition(position) {
         // Mapbox can return a wrapped longitude after panning across the world.
         const longitude = ((position.lng + 180) % 360 + 360) % 360 - 180;
         this.latitude.value = position.lat.toFixed(7);
         this.longitude.value = longitude.toFixed(7);
         this.marker.setLngLat([longitude, position.lat]);
         this.dispatch('changed', { detail: { fields: [this.latitude, this.longitude] } });
      }

      showError() {
         this.errorTarget.hidden = false;
      }

      disconnect() {
         if (this.onFieldChange) this.form.removeEventListener('change', this.onFieldChange);
         if (this.marker) {
            if (this.onDrag) this.marker.off('dragend', this.onDrag);
            this.marker.remove();
            this.marker = null;
         }
         if (this.map) {
            if (this.onClick) this.map.off('click', this.onClick);
            if (this.onLoad) this.map.off('load', this.onLoad);
            if (this.onError) this.map.off('error', this.onError);
            this.map.remove();
            this.map = null;
         }
      }
   }
   window.TransformalizeFormMapController = FormMapController;
})();
