using OrchardCore.ContentManagement;
using OrchardCore.TransformalizeModule.Models;
using System.Threading.Tasks;

namespace OrchardCore.TransformalizeModule.Services.Contracts {
   public interface ICommonService : IArrangementService {
      Task<TransformalizeResponse<ContentPart>> Validate(TransformalizeRequest request);
   }

}
