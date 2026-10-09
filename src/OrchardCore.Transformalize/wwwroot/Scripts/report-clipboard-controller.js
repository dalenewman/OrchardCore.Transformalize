(function () {
   const escape = text => text.trim().replace(/\|/g, '\\|').replace(/\s+/g, ' ');

   function cellMarkdown(cell, type) {
      if (type === 'bool') return cell.querySelector('[aria-label]')?.getAttribute('aria-label') === 'Yes' ? '✅' : '❌';
      const clone = cell.cloneNode(true);
      if (type === 'string') clone.querySelectorAll('a[href]').forEach(link => {
         const text = link.textContent.trim();
         link.replaceWith(link.protocol === 'javascript:' ? text : `[${text}](${link.href})`);
      });
      return escape(clone.textContent);
   }

   function tableMarkdown(table) {
      const headers = [...table.tHead.rows[0].cells];
      const columns = headers.map((cell, index) => ({ cell, index }))
         .filter(({ cell }) => !cell.querySelector('input[type="checkbox"]'));
      const lines = [
         '| ' + columns.map(({ cell }) => escape(cell.textContent)).join(' | ') + ' |',
         '| ' + columns.map(() => '---').join(' | ') + ' |'
      ];
      [...table.tBodies].forEach(body => [...body.rows].forEach(row => {
         lines.push('| ' + columns.map(({ cell, index }) => cellMarkdown(row.cells[index], cell.dataset.type)).join(' | ') + ' |');
      }));
      return lines.join('\n');
   }

   class ClipboardController extends Stimulus.Controller {
      static targets = ['status'];

      async copy(event) {
         event.preventDefault();
         const table = this.element.querySelector(`[id="${event.params.table}"]`);
         if (!table?.tHead?.rows.length) return;
         try {
            const text = tableMarkdown(table);
            if (navigator.clipboard?.writeText) await navigator.clipboard.writeText(text);
            else this.fallback(text);
            this.statusTarget.textContent = 'Table copied as Markdown.';
         } catch {
            this.statusTarget.textContent = 'Could not copy the table. Allow clipboard access and try again.';
         }
      }

      fallback(text) {
         const focused = document.activeElement;
         const input = document.createElement('textarea');
         input.value = text;
         input.style.cssText = 'position:fixed;opacity:0';
         document.body.append(input);
         try {
            input.select();
            if (!document.execCommand('copy')) throw new Error('Copy failed');
         } finally {
            input.remove();
            focused?.focus({ preventScroll: true });
         }
      }
   }
   window.TransformalizeReportControllers.clipboard = ClipboardController;
})();
