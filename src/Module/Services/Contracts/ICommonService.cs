using Module.Models;
using OrchardCore.ContentManagement;
using System.Threading.Tasks;

namespace Module.Services.Contracts {
   public interface ICommonService : IArrangementService {
      Task<TransformalizeResponse<ContentPart>> Validate(TransformalizeRequest request);
   }

}
