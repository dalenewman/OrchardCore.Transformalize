(function () {
   const application = Stimulus.Application.start();
   application.register('tfl-form', window.TransformalizeFormController);
   application.register('tfl-map', window.TransformalizeFormMapController);
   application.register('tfl-places', window.TransformalizeFormPlacesController);
   application.register('tfl-location', window.TransformalizeFormLocationController);
   if (window.TransformalizeFormUploadController) application.register('tfl-upload', window.TransformalizeFormUploadController);
})();
