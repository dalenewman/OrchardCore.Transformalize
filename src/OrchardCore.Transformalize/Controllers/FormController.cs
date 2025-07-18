using Autofac;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Localization;
using Microsoft.Extensions.Primitives;
using OrchardCore.DisplayManagement.Notify;
using TransformalizeModule.Models;
using TransformalizeModule.Services;
using TransformalizeModule.Services.Contracts;
using IContainer = TransformalizeModule.Services.Contracts.IContainer;

namespace TransformalizeModule.Controllers {

   [Authorize]
   public class FormController : Controller {

      private readonly CombinedLogger<FormController> _logger;
      private readonly IFormService _formService;
      private readonly INotifier _notifier;
      private readonly IHtmlLocalizer<FormController> H;
      private readonly IHttpContextAccessor _httpContext;
      private readonly IContainer _container;

      public FormController(
         IFormService formService,
         INotifier notifier,
         IContainer container,
         IHttpContextAccessor httpContext,
         IHtmlLocalizer<FormController> htmlLocalizer,
         CombinedLogger<FormController> logger
      ) {
         _logger = logger;
         _formService = formService;
         _httpContext = httpContext;
         _container = container;
         _notifier = notifier;
         H = htmlLocalizer;
      }

      public async Task<ActionResult> Index(string contentItemId) {

         var form = await _formService.ValidateForm(
            new TransformalizeRequest(contentItemId) {
               InternalParameters = Common.GetFileParameters(Request)
            }
         );

         if (form.Fails()) {
            return form.ActionResult;
         }

         if (Request.Method == "POST" && _httpContext.HttpContext.Request.HasFormContentType) {

            if (!form.Process.Parameters.All(p => p.Valid)) {
               await _notifier.ErrorAsync(H["The form did not pass validation.  Please correct it and re-submit."]);
               return View(form);
            }

            var scope = _container.CreateScope(form.Process, _logger, null);

            if (!scope.IsRegistered<AdoFormCommandWriter>()) {
               _logger.Error(() => "The form command writer isn't registered.");
               return View(form);
            }

            if (!form.Process.Parameters.Any(p => p.PrimaryKey)) {
               await _notifier.ErrorAsync(H["To save the form, you must specify one of the parameters as the primary key."]);
               return View(form);
            }

            var commands = scope.Resolve<AdoFormCommandWriter>().Write();
            var key = form.Process.Parameters.First(k => k.PrimaryKey);
            var insert = key.Value == Transformalize.Constants.TypeDefaults()[key.Type].ToString();

            // reset, modify for actual insert, and execute
            form.Process.Actions.Add(new Transformalize.Configuration.Action {
               After = true,
               Before = false,
               Type = "run",
               Connection = form.Process.Connections.First().Name,
               Command = insert ? commands.Insert : commands.Update,
               Key = Guid.NewGuid().ToString(),
               ErrorMode = "exception"
            });

            try {
               _formService.Run(form.Process);
               await _notifier.InformationAsync(insert ? H["{0} inserted", form.Process.Name] : H["{0} updated", form.Process.Name]);
               if (Request.Form["modal"] == "1") {
                  var formUrl = Url.Action("Index", "Form", new { Area = Common.ModuleName, ContentItemId = contentItemId, modal = 1, close = 1 });
                  return Redirect(formUrl);
               } else if (Request.Form[Common.ReturnUrlName] != StringValues.Empty) {
                  return Redirect(Request.Form[Common.ReturnUrlName].ToString());
               }
            } catch (Exception ex) {
               if (ex.Message.Contains("duplicate")) {
                  await _notifier.ErrorAsync(H["The {0} save failed: {1}", form.Process.Name, "The database has rejected this update due to a unique constraint violation."]);
               } else {
                  await _notifier.ErrorAsync(H["The {0} save failed: {1}", form.Process.Name, ex.Message]);
               }
               _logger.Error(ex, () => ex.Message);
            }

         }

         return View(form);
      }

      public async Task<ActionResult> Form(string contentItemId) {

         var form = await _formService.ValidateForm(
            new TransformalizeRequest(contentItemId) {
               InternalParameters = Common.GetFileParameters(Request)
            }
         );

         if (form.Fails()) {
            return form.ActionResult;
         }

         return View("Form", form.Process);
      }

      [HttpGet]
      public async Task<ActionResult> Run(string contentItemId, string format = "json") {

         var request = new TransformalizeRequest(contentItemId) { 
            Format = format,
            InternalParameters = Common.GetFileParameters(Request)
         };
         var report = await _formService.ValidateForm(request);

         if (report.Fails()) {
            return report.ActionResult;
         }

         await _formService.RunAsync(report.Process);

         report.Process.Connections.Clear();

         return new ContentResult() { Content = report.Process.Serialize(), ContentType = request.ContentType };
      }

   }
}
