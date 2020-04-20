using Transformalize.Configuration;

namespace Module.Services.Contracts
{
    public interface ISortService {
        Direction Sort(int fieldNumber, string expression);
        void AddSortToEntity(Entity entity, string expression);
    }
}
