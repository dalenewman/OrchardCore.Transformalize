using TransformalizeModule.Models;
using TransformalizeModule.Services.Contracts;
using Transformalize.Configuration;

namespace TransformalizeModule.Services {

    public class SortService : ISortService {

        private readonly Dictionary<string, char> _cache = null;

        private static Dictionary<string, char> ProcessExpression(string expression) {
            var order = expression ?? string.Empty;
            var orderLookup = order.Split(new[] { "." }, StringSplitOptions.RemoveEmptyEntries);
            var dict = new Dictionary<string, char>();
            foreach (var item in orderLookup) {
                var direction = item.EndsWith("d") ? 'd' : 'a';
                var value = item.Substring(0,item.Length-1);
            dict[value] = direction;
         }
            return dict;
        }

        public Direction Sort(string src, string expression) {
            var lookup = _cache ?? ProcessExpression(expression);

            if (lookup.ContainsKey(src)) {
                return lookup[src] == 'a' ? Direction.Asc : Direction.Desc;
            }

            return Direction.None;
        }

        public void AddSortToEntity(TransformalizeReportPart part, Entity entity, string expression) {
            string orderBy = null;
            var fields = entity.GetAllOutputFields().Where(f=>!f.System && f.Alias != part.BulkActionValueField.Text).ToArray();
            foreach (var field in fields) {
                if (field.Sortable == "false") {
                    continue;
                }
                var sort = Sort(field.Src, expression);
                if (sort != Direction.None) {
                    if (string.IsNullOrEmpty(entity.Query)) {
                        entity.Order.Add(new Order { Field = field.SortField, Sort = sort == Direction.Asc ? "asc" : "desc" });
                    } else {
                        if (orderBy == null) {
                            entity.Query = entity.Query.TrimEnd(';');
                            orderBy = " ORDER BY ";
                        }
                        orderBy += " [" + field.SortField + "] " + (sort == Direction.Asc ? "ASC" : "DESC") + ",";
                    }
                }
            }

            if (!string.IsNullOrEmpty(orderBy)) {
                entity.Query += orderBy.TrimEnd(',');
            }

        }
    }
}
