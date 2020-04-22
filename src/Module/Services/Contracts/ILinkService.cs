using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Http;

namespace Module.Services.Contracts {
    public interface ILinkService {
        HtmlString Create(string contentItemId, string actionUrl, string type, bool everything);
    }
}
