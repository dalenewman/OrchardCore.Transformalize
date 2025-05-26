using TransformalizeModule.Models;
using Transformalize.Configuration;

namespace TransformalizeModule.Services.Contracts
{
    public interface ISortService {
        Direction Sort(string src, string expression);
        void AddSortToEntity(TransformalizeReportPart part, Entity entity, string expression);
    }
}
