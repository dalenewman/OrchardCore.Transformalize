using Microsoft.AspNetCore.Mvc;

namespace TransformalizeModule.ViewModels;

public class ChartViewModel
{
    public bool ChartIsEnabled { get; set; }
    public ReportViewModel? ReportViewModel { get; set; }
    public bool Failed { get; set; }
    public ActionResult? ActionResult { get; set; }
    public bool ShowLog { get; set; }
    public LogViewModel? LogViewModel { get; set; }
}
