// Report basemaps and the report overlay share a map, but load independently.
(function () {
   const styles = [
      ['Streets', 'streets-v11'],
      ['Satellite + streets', 'satellite-streets-v12'],
      ['Satellite', 'satellite-v9'],
      ['Outdoors', 'outdoors-v12'],
      ['Light', 'light-v11'],
      ['Dark', 'dark-v11']
   ];

   function connect(map, layer) {
      let data, fitPending = false, styleReady = map.isStyleLoaded();
      const control = {
         onAdd() {
            this.container = document.createElement('div');
            this.container.className = 'mapboxgl-ctrl mapboxgl-ctrl-group tfl-map-style-control';
            const label = document.createElement('label');
            const caption = document.createElement('span');
            caption.textContent = 'Map style';
            const select = document.createElement('select');
            select.className = 'form-select form-select-sm';
            for (const [name, id] of styles) {
               const option = document.createElement('option');
               option.value = 'mapbox://styles/mapbox/' + id;
               option.textContent = name;
               select.append(option);
            }
            select.addEventListener('change', () => {
               // A native select in the report form must not trigger report filtering.
               styleReady = false;
               map.setStyle(select.value);
            });
            this.container.addEventListener('change', event => event.stopPropagation());
            this.container.addEventListener('keydown', event => event.stopPropagation());
            label.append(caption, select);
            this.container.append(label);
            return this.container;
         },
         onRemove() { this.container.remove(); }
      };

      function restore() {
         if (!styleReady || !data) return;
         const source = map.getSource(layer.source);
         if (source) source.setData(data);
         else map.addSource(layer.source, { type: 'geojson', data });
         if (!map.getLayer(layer.id)) map.addLayer(layer);
         if (fitPending && data.features.length) {
            const bounds = new mapboxgl.LngLatBounds();
            data.features.forEach(feature => bounds.extend(feature.geometry.coordinates));
            map.fitBounds(bounds, { padding: 100 });
         }
         fitPending = false;
      }

      map.on('style.load', () => {
         styleReady = true;
         restore();
      });
      // Bind once: replacing the basemap does not remove delegated map listeners.
      map.on('click', layer.id, event => {
         const coordinates = event.features[0].geometry.coordinates.slice();
         while (Math.abs(event.lngLat.lng - coordinates[0]) > 180) {
            coordinates[0] += event.lngLat.lng > coordinates[0] ? 360 : -360;
         }
         new mapboxgl.Popup().setLngLat(coordinates)
            .setHTML(event.features[0].properties.description).addTo(map);
      });
      map.on('mouseenter', layer.id, () => { map.getCanvas().style.cursor = 'pointer'; });
      map.on('mouseleave', layer.id, () => { map.getCanvas().style.cursor = ''; });
      map.addControl(control, 'top-left');

      return {
         setData(value, fitBounds) {
            data = value;
            fitPending = fitPending || fitBounds;
            restore();
         }
      };
   }

   window.TransformalizeReportMap = { connect };
})();
