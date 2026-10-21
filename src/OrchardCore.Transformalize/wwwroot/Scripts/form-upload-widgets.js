// Keep Blueimp's image/EXIF pipeline until it can be replaced independently.
// All plugin state belongs to one chooser and is torn down with its fragment.
(function () {
   window.TransformalizeUploadWidgets = {
      connect(input, options, callbacks) {
         const $input = $(input);
         let disposed = false;
         let active;
         const notify = (job, method, ...args) => {
            if (!disposed && active === job && !job.finished) callbacks[method](job, ...args);
         };
         const finish = (job, method, result) => {
            if (disposed || active !== job || job.finished) return;
            job.finished = true;
            callbacks[method](job, result);
         };
         $input.fileupload({
            url: options.url, paramName: options.name, dataType: 'json',
            fileInput: $input, replaceFileInput: false, dropZone: null, pasteZone: null,
            singleFileUploads: true, maxNumberOfFiles: 1,
            disableImageResize: false, imageMaxWidth: 1920, imageMaxHeight: 1080,
            imageCrop: false, imageOrientation: true,
            add(event, data) {
               const originalFiles = [...data.files];
               const job = {
                  finished: false,
                  file: originalFiles[0],
                  abort: () => data.abort(),
                  retry: () => $input.fileupload('add', { files: originalFiles })
               };
               if (!callbacks.selected(job)) return false;
               active = job;
               data.tflJob = job;
               data.process(() => $input.fileupload('process', data))
                  .done(() => { if (!disposed && active === job && !job.finished) data.submit(); })
                  .fail(() => finish(job, 'failed', 'The image could not be prepared. Retry or clear it.'));
            },
            progress(event, data) { notify(data.tflJob, 'progress', data.total ? Math.round(data.loaded / data.total * 100) : 0); },
            done(event, data) { finish(data.tflJob, 'completed', data.result); },
            fail(event, data) { finish(data.tflJob, 'failed', 'The upload failed. Retry or clear it.'); }
         });
         return {
            disconnect() {
               disposed = true;
               if (active && !active.finished) active.abort();
               $input.fileupload('destroy');
            }
         };
      }
   };
})();
