using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using TransformalizeModule.Services.Contracts;
using TransformalizeModule.Services;
using TransformalizeModule.Models;
using Microsoft.AspNetCore.Authorization;

namespace TransformalizeModule.Controllers {

   [Authorize]
   public class FormController : Controller {

      private readonly CombinedLogger<FormController> _logger;
      private readonly IFormService _formService;

      public FormController(
         IFormService formService,
         CombinedLogger<FormController> logger
      ) {
         _logger = logger;
         _formService = formService;
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

   }
}
