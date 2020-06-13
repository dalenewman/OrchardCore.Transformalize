using Module.Models;
using OrchardCore.ContentManagement;
using System.Threading.Tasks;
using Transformalize.Configuration;

namespace Module.Services.Contracts {
   public interface ISchemaService: IArrangementService, IArrangementSchemaService {
      Process LoadForSchema(ContentItem contentItem, string format);
      Task<TransformalizeResponse<ContentPart>> Validate(TransformalizeRequest request);
   }
}
