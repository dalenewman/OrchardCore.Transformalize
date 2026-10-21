// One application; each controller owns one part of the Razor-rendered report.
(function () {
   // Controllers handle events; htmx never needs to evaluate JavaScript strings.
   htmx.config.allowEval = false;
   const application = Stimulus.Application.start();
   application.register('report-filter', window.TransformalizeReportFilterController);
   Object.entries(window.TransformalizeReportControllers).forEach(([name, controller]) => application.register(name, controller));
})();
