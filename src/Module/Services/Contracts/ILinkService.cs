using Microsoft.AspNetCore.Html;

namespace Module.Services.Contracts {
    public interface ILinkService {
        HtmlString Create(string contentItemId, string actionUrl, bool everything);
    }
}
