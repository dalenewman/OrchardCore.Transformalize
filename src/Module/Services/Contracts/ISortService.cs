using Module.Models;
using Transformalize.Configuration;

namespace Module.Services.Contracts
{
    public interface ISortService {
        Direction Sort(int fieldNumber, string expression);
        void AddSortToEntity(TransformalizeReportPart part, Entity entity, string expression);
    }
}
