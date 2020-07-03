using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using OrchardCore.TransformalizeModule.Services;
using OrchardCore.TransformalizeModule.Models;
using OrchardCore.TransformalizeModule.Services.Contracts;
using Transformalize.Configuration;

namespace OrchardCore.TransformalizeModule.Controllers {
   public class ArrangementController : Controller {

      private readonly CombinedLogger<ArrangementController> _logger;
      private readonly ICommonService _commonService;
      private readonly ITransformalizeParametersModifier _modifier;

      public ArrangementController(
         CombinedLogger<ArrangementController> logger,
         ICommonService common,
         ITransformalizeParametersModifier modifier
      ) {
         _logger = logger;
         _commonService = common;
         _modifier = modifier;
      }

      [HttpGet]
      public async Task<ActionResult> TransformalizeParameters(string contentItemId) {

         if (HttpContext == null || HttpContext.User == null || HttpContext.User.Identity == null || !HttpContext.User.Identity.IsAuthenticated) {
            return Unauthorized();
         }

         var user = HttpContext.User?.Identity?.Name ?? "Anonymous";

         var request = new TransformalizeRequest(contentItemId, user) { Format = "xml" };

         var item = await _commonService.Validate(request);

         string arrangement;
         if (item.ContentItem.Has("TransformalizeTaskPart")) {
            arrangement = item.ContentItem.Content.TransformalizeTaskPart.Arrangement.Arrangement.Value;
         } else {
            arrangement = item.ContentItem.Content.TransformalizeReportPart.Arrangement.Arrangement.Value;
         }

         var response = _modifier.Modify(arrangement);

         var process = new Process(response.Arrangement);
         process.Connections.Clear();

         if (item.Fails()) {
            return item.ActionResult;
         }

         return new ContentResult() { Content = process.Serialize(), ContentType = request.ContentType };
      }

   }
}
