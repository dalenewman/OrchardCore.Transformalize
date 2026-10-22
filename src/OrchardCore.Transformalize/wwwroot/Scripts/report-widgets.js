// Shared date filters and adapters for the calendar display plugin.
(function () {
   window.TransformalizeReportWidgets = {
      calendarEvents(rows) {
         // Streams preserve arrangement alias casing (e.g. Id for Model.Id in
         // Razor). The calendar plugin requires lowercase property names.
         return rows.map(row => {
            const event = { ...row };
            const keys = Object.keys(row);
            for (const name of ['id', 'title', 'url', 'class', 'start', 'end']) {
               if (Object.hasOwn(event, name)) continue;
               const key = keys.find(key => key.toLowerCase() === name);
               if (key !== undefined) event[name] = row[key];
            }
            return event;
         });
      },
      connectCalendar(root) {
         // Capture runs before the plugin's day handler stops propagation.
         // A marker opens that day's list; title links in the list navigate.
         root.addEventListener('click', event => {
            const marker = event.target.closest?.('a.event');
            if (marker && root.contains(marker)) event.preventDefault();
         }, true);
      },
      connect(root) {
         const dates = [...root.querySelectorAll('.form-control.date')].map(input =>
            TransformalizeDateWidgets.connect(input, input.parentElement.querySelector('[data-report-date-toggle]')));
         return {
            setBusy(busy) { dates.forEach(date => date.setBusy(busy)); },
            clear() {
               root.querySelectorAll('select[name]').forEach(select => {
                  if (select.multiple) [...select.options].forEach(option => { option.selected = false; });
                  else select.value = '*';
                  select.dispatchEvent(new Event('change', { bubbles: true }));
               });
            },
            disconnect() { dates.forEach(date => date.disconnect()); }
         };
      }
   };
})();
