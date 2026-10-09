// Scoped validation of Razor's parameter rules; the server remains authoritative.
(function () {
   let sequence = 0;
   const number = /^-?(\d*\.)?\d+(e[-+]?\d+)?$/i;
   const types = { number, integer: /^-?\d+$/, digits: /^\d+$/, alphanum: /^\w+$/ };

   function validDate(value) {
      const iso = /^(\d{4}-\d{2}-\d{2})(?:[ T].*)?$/.exec(value);
      if (iso && !TransformalizeDateWidgets.isoDate(iso[1])) return false;
      const local = /^(\d{1,2})\/(\d{1,2})\/(\d{4})(?:\s.*)?$/.exec(value);
      if (local && !TransformalizeDateWidgets.isoDate(local[3] + '-' + local[1].padStart(2, '0') + '-' + local[2].padStart(2, '0'))) return false;
      return Number.isFinite(Date.parse(value));
   }

   function matches(value, pattern) {
      // .NET-only syntax stays with server validation rather than blocking input.
      if (/\\[AZz]|\(\?>|\(\?[imsx-]+[:)]/.test(pattern)) return true;
      try {
         const literal = /^\/(.*)\/([gimsuy]*)$/.exec(pattern);
         const expression = literal ? new RegExp(literal[1], literal[2].replace(/[gy]/g, '')) : new RegExp('^(?:' + pattern + ')$');
         return expression.test(value);
      } catch { return true; }
   }

   function fieldValue(field, form) {
      if (field.type === 'radio' || field.type === 'checkbox') {
         return [...form.elements]
            .filter(other => other.name === field.name && other.checked && !other.disabled)
            .map(other => other.value).join(',');
      }
      if (field.multiple) return [...field.selectedOptions].map(option => option.value).join(',');
      return field.value || '';
   }

   function typeMessage(field, value, rules) {
      const type = rules.type || (['number', 'email', 'url'].includes(field.type) ? field.type : '');
      if (types[type] && !types[type].test(value)) {
         const descriptions = { digits: 'digits', alphanum: 'alphanumeric' };
         return 'This value should be ' + (descriptions[type] || 'a valid ' + type) + '.';
      }
      if (type === 'email' || type === 'url') {
         const probe = document.createElement('input');
         probe.type = type;
         // Existing URL rules permit a hostname without a scheme.
         probe.value = type === 'url' && !/^[a-z][a-z\d+.-]*:\/\//i.test(value) ? 'https://' + value : value;
         if (probe.validity.typeMismatch) return 'This value should be a valid ' + type + '.';
      }
      return '';
   }

   function formatMessage(field, value, rules) {
      if (rules.date === 'true' && !validDate(value)) return 'This value should be a date.';
      if (rules.pattern && !matches(value, rules.pattern)) return 'This value seems to be invalid.';
      if (field.getAttribute('pattern') && !matches(value, field.getAttribute('pattern'))) return 'This value seems to be invalid.';
      return '';
   }

   function lengthMessage(field, value, rules) {
      const length = rules.length ? JSON.parse(rules.length) : null;
      if (length && (value.length < Number(length[0]) || value.length > Number(length[1]))) return 'This value should have ' + length[0] + ' characters.';
      if (field.minLength > 0 && value.length < field.minLength) return 'This value should have at least ' + field.minLength + ' characters.';
      if (field.maxLength >= 0 && value.length > field.maxLength) return 'This value should have no more than ' + field.maxLength + ' characters.';
      return '';
   }

   function rangeMessage(field, value, rules) {
      const numericInput = ['number', 'range'].includes(field.type);
      const min = rules.min ?? (numericInput ? field.getAttribute('min') : null);
      const max = rules.max ?? (numericInput ? field.getAttribute('max') : null);
      if (min != null && min !== '' && (!number.test(value) || Number(value) < Number(min))) return 'This value should be greater than or equal to ' + min + '.';
      if (max != null && max !== '' && (!number.test(value) || Number(value) > Number(max))) return 'This value should be lower than or equal to ' + max + '.';
      return '';
   }

   function nativeMessage(field) {
      if (field.validity?.stepMismatch) return 'This value should match the specified numeric step.';
      if (field.validity?.typeMismatch || field.validity?.rangeOverflow || field.validity?.rangeUnderflow) return field.validationMessage || 'This value seems to be invalid.';
      return '';
   }

   function validationMessage(field, form) {
      const rules = JSON.parse(field.getAttribute('data-tfl-validation') || '{}');
      const value = fieldValue(field, form);
      if (field.validity?.badInput) return 'This value should be a valid ' + (field.type === 'number' ? 'number.' : 'value.');
      if ((rules.required === 'true' || field.required) && !/\S/.test(value)) return rules['required-message'] || 'This value is required.';
      if (!value) return '';

      // Keep rule precedence: show the first applicable error.
      return typeMessage(field, value, rules)
         || formatMessage(field, value, rules)
         || lengthMessage(field, value, rules)
         || rangeMessage(field, value, rules)
         || nativeMessage(field);
   }

   function descriptionIds(field) {
      return new Set((field.getAttribute('aria-describedby') || '').split(/\s+/).filter(Boolean));
   }

   function setDescriptionIds(field, ids) {
      if (ids.size) field.setAttribute('aria-describedby', [...ids].join(' '));
      else field.removeAttribute('aria-describedby');
   }

   function removeDescription(field, id) {
      const ids = descriptionIds(field);
      ids.delete(id);
      setDescriptionIds(field, ids);
   }

   function setValidationClasses(element, invalid) {
      element?.classList.toggle('is-invalid', invalid);
      element?.classList.toggle('is-valid', !invalid);
   }

   function serverMessages(container) {
      return [...(container?.querySelectorAll('.help-block:not([data-tfl-validation-message])') || [])];
   }

   window.TransformalizeFormWidgets = {
      connect(form) {
         const messages = new Map();
         const serverInvalid = new Map();
         const responseErrors = new Map();
         const isEligible = field => field.form === form && !!field.name && !field.disabled
            && field.matches('input, textarea, select')
            && !field.matches('[data-tfl-validation-excluded], input[type=button], input[type=submit], input[type=reset], input[type=file], input[type=hidden]:not([data-form-upload-value])');
         function fieldMembers(field) {
            if (!['radio', 'checkbox'].includes(field.type)) return [field];
            return [...form.elements].filter(other => other.name === field.name && isEligible(other));
         }

         function errorFor(field) {
            const responseError = responseErrors.get(field);
            if (responseError && responseError.value !== field.value) responseErrors.delete(field);
            return responseErrors.get(field)?.message || validationMessage(field, form);
         }

         function updateValidationState(group, members, error) {
            if (!serverInvalid.has(group)) serverInvalid.set(group, group?.classList.contains('is-invalid') === true);
            const invalid = !!error || serverInvalid.get(group);
            members.forEach(member => {
               setValidationClasses(member, invalid);
               member.setAttribute('aria-invalid', String(invalid));
            });
            setValidationClasses(group, invalid);
         }

         function getOrCreateBrowserMessage(group, container) {
            let item = messages.get(group);
            if (item) return item;
            const node = document.createElement('span');
            node.className = 'help-block';
            node.id = 'tfl-validation-' + ++sequence;
            node.setAttribute('data-tfl-validation-message', '');
            node.setAttribute('role', 'alert');
            container.append(node);
            item = { node, fields: new Set() };
            messages.set(group, item);
            return item;
         }

         function showMessage(group, members, error) {
            const container = group?.querySelector('.help-container');
            if (!container) return;
            // A local check cannot resolve cross-field/server-only rules. Keep
            // Razor's messages until another server response replaces the group.
            const existing = serverMessages(container);
            existing.forEach(node => { node.id ||= 'tfl-server-validation-' + ++sequence; });
            const item = getOrCreateBrowserMessage(group, container);
            item.node.textContent = error;
            members.forEach(member => {
               const ids = descriptionIds(member);
               if (error) ids.add(item.node.id);
               else ids.delete(item.node.id);
               existing.forEach(node => ids.add(node.id));
               setDescriptionIds(member, ids);
               item.fields.add(member);
            });
         }

         function validateField(field) {
            if (!isEligible(field)) return true;
            const error = errorFor(field);
            const group = field.closest('.form-group');
            const members = fieldMembers(field);
            updateValidationState(group, members, error);
            showMessage(group, members, error);
            return !error;
         }

         function clearServerMessages(field, group) {
            // A failed refresh can still supersede this field's older feedback.
            serverMessages(group?.querySelector('.help-container')).forEach(node => {
               removeDescription(field, node.id);
               node.remove();
            });
            serverInvalid.set(group, false);
         }

         const blur = event => { if (isEligible(event.target)) validateField(event.target); };
         const input = event => { if (isEligible(event.target) && event.target.getAttribute('aria-invalid') === 'true') validateField(event.target); };
         form.addEventListener('focusout', blur);
         form.addEventListener('input', input);
         const dates = [...form.querySelectorAll('[data-form-date]')].map(field => TransformalizeDateWidgets.connect(field, field.parentElement.querySelector('[data-form-date-toggle]')));
         const boundary = form.closest('[data-controller~="tfl-form"]');
         const busy = event => dates.forEach(date => date.setBusy(event.detail.busy));
         boundary?.addEventListener('tfl-form:busy', busy);
         return {
            validateField,
            showErrors(errors) {
               responseErrors.clear();
               errors.forEach(error => {
                  if (typeof error.name !== 'string' || typeof error.message !== 'string') return;
                  [...form.elements].filter(field => field.name === error.name && isEligible(field)).forEach(field => {
                     const group = field.closest('.form-group');
                     clearServerMessages(field, group);
                     responseErrors.set(field, { value: field.value, message: error.message });
                     validateField(field);
                  });
               });
            },
            validateForm() {
               const invalid = [...form.elements].filter(isEligible).filter(field => !validateField(field));
               const first = invalid[0];
               const focus = first?.type === 'hidden' ? first.closest('.form-group')?.querySelector('[data-tfl-upload-target~="choose"]') : first;
               focus?.focus({ preventScroll: true });
               return !invalid.length;
            },
            disconnect() {
               form.removeEventListener('focusout', blur);
               form.removeEventListener('input', input);
               boundary?.removeEventListener('tfl-form:busy', busy);
               dates.forEach(date => date.disconnect());
               messages.forEach(({ node, fields }) => {
                  fields.forEach(field => removeDescription(field, node.id));
                  node.remove();
               });
               messages.clear();
            }
         };
      }
   };
})();
