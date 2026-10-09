// Calendars are the only report widgets outside the Stimulus filter controllers.
(function () {
   window.TransformalizeReportWidgets = {
      connect(root) {
         const dates = [...root.querySelectorAll('.form-control.date')].map(input =>
            TransformalizeDateWidgets.connect(input, input.parentElement.querySelector('[data-report-date-toggle]')));
         return {
            setBusy(busy) { dates.forEach(date => date.setBusy(busy)); },
            clear() {
               root.querySelectorAll('select').forEach(select => {
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
