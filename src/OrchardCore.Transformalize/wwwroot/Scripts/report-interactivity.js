// One application; each controller owns one part of the Razor-rendered report.
(function () {
   const application = Stimulus.Application.start();
   application.register('report-filter', window.TransformalizeReportFilterController);
   Object.entries(window.TransformalizeReportControllers).forEach(([name, controller]) => application.register(name, controller));
})();
