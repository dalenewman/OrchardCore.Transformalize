using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using TransformalizeModule.ViewModels;

namespace TransformalizeModule.Ext {
   public static class FormInteractivityExtensions {
      public static ActionResult FormFailure(this Controller controller, Transformalize.Configuration.Process process, ActionResult fallback) {
         if (controller.Request.Headers["HX-Request"] != "true" || controller.Request.Headers["HX-Target"] != "tfl-form"
            || controller.Request.Headers["HX-History-Restore-Request"] == "true") return fallback;

         var fields = process.Parameters.Where(p => p.Input && p.Type == "string" && !string.IsNullOrEmpty(p.InvalidCharacters)
               && p.Value != null)
            .Select(p => new {
               name = p.Name,
               characters = p.InvalidCharacters.Distinct().Where(c => !(c == ',' && p.Multiple) && p.Value.Contains(c)).ToArray(),
               label = string.IsNullOrEmpty(p.Label) ? p.Name : p.Label
            })
            .Where(p => p.characters.Length > 0)
            .Select(p => new { p.name, message = $"{p.label} contains a character that is not allowed: {string.Join(", ", p.characters.Select(c => $"“{c}”"))}. Remove it and try again." })
            .ToArray();
         if (fields.Length == 0) return fallback;
         controller.Response.Headers["X-Transformalize-Form-Error"] = "true";
         // Like an invalid form fragment, this is a completed validation request.
         // The marker distinguishes it from login/diagnostic HTML without a swap.
         return new JsonResult(new { message = "Please correct the highlighted fields, then try again.", fields });
      }

      public static void SetFormInteractivity(this Controller controller, FormInteractivityViewModel model) {
         controller.ViewData["FormInteractivity"] = model;
      }

      public static ActionResult FormFragment(this Controller controller, FormInteractivityViewModel model) {
         controller.SetFormInteractivity(model);
         controller.Response.Headers.Append("Vary", "HX-Request, HX-Target, HX-History-Restore-Request");
         if (model.Interactive && controller.Request.Headers["HX-Request"] == "true"
            && controller.Request.Headers["HX-Target"] == "tfl-form"
            && controller.Request.Headers["HX-History-Restore-Request"] != "true") {
            controller.Response.Headers["X-Transformalize-Form"] = "true";
            return controller.PartialView("_InteractiveForm", model);
         }
         return controller.View("Form", model.Process);
      }
   }
}
