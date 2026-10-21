using Microsoft.AspNetCore.Html;
using TransformalizeModule.Models;

namespace TransformalizeModule.Services.Contracts {
    public interface ILinkService {
        HtmlString Create(string contentItemId, string actionUrl, bool everything, ReportPaging? paging = null);
    }
}
