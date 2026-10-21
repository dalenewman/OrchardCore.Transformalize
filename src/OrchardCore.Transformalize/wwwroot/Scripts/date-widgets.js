// The named text field belongs to Razor; the calendar owns an unnamed helper.
(function () {
   let sequence = 0;
   function isoDate(value) {
      const parts = /^(\d{4})-(\d{2})-(\d{2})$/.exec(value);
      if (!parts) return false;
      const [year, month, day] = parts.slice(1).map(Number);
      return year > 0 && month >= 1 && month <= 12 && day >= 1
         && day <= new Date(year, month, 0).getDate();
   }
   window.TransformalizeDateWidgets = {
      isoDate,
      connect(input, toggle) {
         const initialValue = input.value;
         const initialDate = input.getAttribute('data-tfl-calendar-date');
         const initialTime = input.getAttribute('data-tfl-calendar-time');
         function calendarValue() {
            if (isoDate(input.value)) return { date: input.value, time: '' };
            // Form selections retain a non-midnight time. Read it again on each
            // selection so repeated picks and edits never restore a stale time.
            if (initialTime !== null) {
               const parts = /^(\d{4}-\d{2}-\d{2})T((\d{2}):(\d{2}):(\d{2})(?:\.\d{1,7})?)$/.exec(input.value);
               if (parts && isoDate(parts[1]) && +parts[3] < 24 && +parts[4] < 60 && +parts[5] < 60) {
                  return { date: parts[1], time: /[1-9]/.test(parts[2]) ? parts[2] : '' };
               }
            }
            // Razor parses server date text using the server culture. Never use
            // these hints after the user has edited or cleared the field.
            if (input.value === initialValue && isoDate(initialDate)) return { date: initialDate, time: initialTime || '' };
            return { date: [], time: '' };
         }
         let active = true;
         let busy = false;
         const helper = document.createElement('input');
         helper.type = 'hidden'; helper.hidden = true;
         helper.setAttribute('aria-hidden', 'true');
         input.parentElement.append(helper);
         const picker = flatpickr(helper, {
            dateFormat: 'Y-m-d', disableMobile: true, clickOpens: false,
            positionElement: input,
            onChange(dates, value) {
               if (!active || busy || input.disabled || input.readOnly) return;
               const { time } = calendarValue();
               input.value = value && time ? value + 'T' + time : value;
               input.dispatchEvent(new Event('change', { bubbles: true }));
            },
            onOpen() {
               toggle.setAttribute('aria-expanded', 'true');
               const day = picker.calendarContainer.querySelector('.selected')
                  || picker.calendarContainer.querySelector('.today')
                  || picker.calendarContainer.querySelector('.flatpickr-day:not(.prevMonthDay):not(.nextMonthDay)');
               day?.focus();
            },
            onClose() {
               toggle.setAttribute('aria-expanded', 'false');
               if (active && input.isConnected) input.focus({ preventScroll: true });
            }
         });
         const calendar = picker.calendarContainer;
         calendar.id = 'tfl-calendar-' + ++sequence;
         calendar.setAttribute('role', 'dialog');
         calendar.setAttribute('aria-label', toggle.getAttribute('aria-label') || 'Choose date');
         toggle.setAttribute('aria-controls', calendar.id);
         toggle.setAttribute('aria-haspopup', 'dialog');
         toggle.setAttribute('aria-expanded', 'false');
         const click = event => {
            event.preventDefault(); event.stopPropagation();
            if (!active || busy || input.disabled || input.readOnly) return;
            if (picker.isOpen) { picker.close(); return; }
            // Opening or cancelling never normalizes/discards the user's text.
            const { date } = calendarValue();
            picker.setDate(date, false, 'Y-m-d');
            picker.open();
         };
         toggle.addEventListener('click', click);
         return {
            setBusy(value) { busy = value; toggle.disabled = busy; if (busy) picker.close(); },
            disconnect() {
               active = false;
               toggle.removeEventListener('click', click);
               picker.destroy(); helper.remove();
               ['aria-controls', 'aria-haspopup', 'aria-expanded'].forEach(name => toggle.removeAttribute(name));
            }
         };
      }
   };
})();
