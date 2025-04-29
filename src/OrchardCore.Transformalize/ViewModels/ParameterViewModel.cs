using Transformalize.Configuration;

namespace TransformalizeModule.ViewModels {
   public class ParameterViewModel {
      public IEnumerable<Map> Maps { get; set; }
      public Parameter Parameter { get; set; }
      public Field Field { get; set; }

      /// <summary>
      /// The only use case I can think of for this is to make some modifications to the automatic facet(s)
      /// For example, if it's a bool facet, then you can change True/true to Yes/On and False/false to No/Off or whatever
      /// </summary>
      /// <param name="map"></param>
      /// <param name="item"></param>
      public void ModifyMapItem(Map? map, MapItem item) {
         if (map != null) {
            var from = item.From.ToString() ?? string.Empty;
            var mappedItem = map.Items.FirstOrDefault(i => from.StartsWith(i.From + " ("));
            if (mappedItem != null) {
               item.From = from.Replace(mappedItem.From + " (", mappedItem.To + " (");
            }
         }
      }
   }
}
