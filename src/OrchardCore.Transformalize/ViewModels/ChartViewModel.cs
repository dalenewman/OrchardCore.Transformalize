using Microsoft.AspNetCore.Mvc;

namespace TransformalizeModule.ViewModels;

public class ChartViewModel
{
    public ReportViewModel? ReportViewModel { get; set; }
    public bool Failed { get; set; }
    public ActionResult? ActionResult { get; set; }
    public bool ShowLog { get; set; }
    public LogViewModel? LogViewModel { get; set; }
}
