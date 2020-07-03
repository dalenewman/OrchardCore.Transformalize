using Microsoft.AspNetCore.Html;

namespace OrchardCore.TransformalizeModule.Services.Contracts {
    public interface ILinkService {
        HtmlString Create(string contentItemId, string actionUrl, bool everything);
    }
}
