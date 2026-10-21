const test = require('node:test');
const assert = require('node:assert/strict');
const fs = require('node:fs');
const path = require('node:path');
const vm = require('node:vm');

for (const entry of ['form', 'report']) {
   test(entry + ' disables htmx expression evaluation before starting controllers', () => {
      const registrations = [];
      const htmx = { config: { allowEval: true } };
      const context = {
         htmx,
         window: { TransformalizeReportControllers: { report: {} } },
         Stimulus: { Application: { start() {
            assert.equal(htmx.config.allowEval, false);
            return { register: name => registrations.push(name) };
         } } }
      };
      vm.runInNewContext(fs.readFileSync(path.join(__dirname,
         '../../src/OrchardCore.Transformalize/wwwroot/Scripts/' + entry + '-interactivity.js'), 'utf8'), context);
      assert.ok(registrations.includes(entry === 'form' ? 'tfl-form' : 'report'));
   });
}
