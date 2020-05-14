using System;
using System.Collections.Generic;

namespace Module.Models {
   public class TransformalizeSettings {

      private List<int> _pageSizes;
      private string defaultPageSizes;
      private string commonArrangement;

      public string CommonArrangement {
         get => string.IsNullOrWhiteSpace(commonArrangement) ? string.Empty : commonArrangement;
         set => commonArrangement = string.IsNullOrWhiteSpace(value) ? string.Empty : value;
      }
      public string DefaultPageSizes {
         get => string.IsNullOrEmpty(defaultPageSizes) ? "20,50,100" : defaultPageSizes;
         set => defaultPageSizes = string.IsNullOrEmpty(value) ? "20,50,100" : value;
      }

      public IEnumerable<int> DefaultPageSizesAsEnumerable() {
         if (_pageSizes != null) {
            return _pageSizes;
         }
         _pageSizes = new List<int>();
         foreach (var size in DefaultPageSizes.Split(',', StringSplitOptions.RemoveEmptyEntries)) {
            if (int.TryParse(size, out int result)) {
               _pageSizes.Add(result);
            }
         }
         return _pageSizes;
      }
   }
}
