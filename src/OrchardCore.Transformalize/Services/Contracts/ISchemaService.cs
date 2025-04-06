using TransformalizeModule.Models;
using OrchardCore.ContentManagement;
using Transformalize.Configuration;

namespace TransformalizeModule.Services.Contracts {
   public interface ISchemaService: IArrangementService, IArrangementSchemaService {
      Process LoadForSchema(ContentItem contentItem, string format);
      Task<TransformalizeResponse<ContentPart>> Validate(TransformalizeRequest request);
   }
}
