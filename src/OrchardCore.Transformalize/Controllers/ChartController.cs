using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TransformalizeModule.Services.Contracts;

namespace TransformalizeModule.Controllers {

   [Authorize]
   public class ChartController : Controller {

      private readonly IChartService _chartService;

      public ChartController(IChartService chartService) =>
         _chartService = chartService;

      [HttpGet]
      public async Task<ActionResult> Index(string contentItemId) {

         var viewModel = await _chartService.GetReportViewModelAsync(contentItemId);

         if (viewModel.Failed) {
            return viewModel.ActionResult!;
         }

         if (viewModel.ShowLog) {
            return View("Log", viewModel.LogViewModel);
         }

         return View(viewModel.ReportViewModel);
      }
   }
}
