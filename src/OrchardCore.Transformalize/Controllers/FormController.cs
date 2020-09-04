using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using TransformalizeModule.Services.Contracts;
using TransformalizeModule.Services;
using TransformalizeModule.Models;
using Microsoft.AspNetCore.Authorization;
using System.IO;

namespace TransformalizeModule.Controllers {

   [Authorize]
   public class FormController : Controller {

      private readonly CombinedLogger<FormController> _logger;
      private readonly IFormService _formService;
      private readonly IFormFileStore _formFileStore;

      public FormController(
         IFormService formService,
         IFormFileStore formFileStore,
         CombinedLogger<FormController> logger
      ) {
         _logger = logger;
         _formService = formService;
         _formFileStore = formFileStore;
      }

      public async Task<ActionResult> Index(string contentItemId) {

         var form = await _formService.ValidateForm(new TransformalizeRequest(contentItemId, HttpContext.User.Identity.Name));

         if (form.Fails()) {
            return form.ActionResult;
         }

         return View(form);
      }

      public async Task<ActionResult> Form(string contentItemId) {

         var form = await _formService.ValidateForm(new TransformalizeRequest(contentItemId, HttpContext.User.Identity.Name));

         if (form.Fails()) {
            return form.ActionResult;
         }

         return View("Form", form.Process);
      }

      [HttpGet]
      public async Task<ActionResult> Run(string contentItemId, string format = "json") {

         var request = new TransformalizeRequest(contentItemId, HttpContext.User.Identity.Name) { Format = format };
         var report = await _formService.ValidateForm(request);

         if (report.Fails()) {
            return report.ActionResult;
         }

         await _formService.RunAsync(report.Process);

         report.Process.Connections.Clear();

         return new ContentResult() { Content = report.Process.Serialize(), ContentType = request.ContentType };
      }

      [HttpPost]
      public async Task<ContentResult> Upload() {

         if (!User.Identity.IsAuthenticated) {
            return GetResult(0, "Unauthorized");
         }

         if (Request.HasFormContentType && Request.Form.Files != null && Request.Form.Files.Count > 0) {
            var file = Request.Form.Files[0];
            if (file != null && file.Length > 0) {
               var filePath = Path.Combine(Common.GetSafeFilePath(HttpContext.User.Identity.Name, file.FileName));
               using (var stream = file.OpenReadStream()) {
                  await _formFileStore.CreateFileFromStreamAsync(filePath, stream, true);
               }
               var fileInfo = await _formFileStore.GetFileInfoAsync(filePath);
               return GetResult(1, file.FileName);
            }
         }

         return GetResult(0, "Error");
      }

      private static ContentResult GetResult(int id, string message) {
         var data = string.Format("{{ \"id\":{0}, \"message\":\"{1}\" }}", id, message);
         return new ContentResult {
            Content = data,
            ContentType = "text/json"
         };
      }

   }
}
