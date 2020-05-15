using OrchardCore.ContentManagement;
using System.Collections.Generic;
using System.Threading.Tasks;
using Transformalize.Configuration;

namespace Module.Services.Contracts {
   public interface IArrangementService {
      public bool IsMissingRequiredParameters(List<Parameter> parameters);
      public Task<ContentItem> GetByIdOrAliasAsync(string idOrAlias);
      public bool CanAccess(ContentItem contentItem);
   }


}
