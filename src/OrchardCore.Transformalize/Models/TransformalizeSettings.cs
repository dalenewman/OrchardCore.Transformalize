using System;
using System.Collections.Generic;

namespace TransformalizeModule.Models {
   public class TransformalizeSettings {

      private List<int> _pageSizes;
      private string _defaultPageSizes;
      private string _commonArrangement;
      private string _mapBoxToken;

      public string CommonArrangement {
         get => string.IsNullOrWhiteSpace(_commonArrangement) ? string.Empty : _commonArrangement;
         set => _commonArrangement = string.IsNullOrWhiteSpace(value) ? string.Empty : value;
      }
      public string DefaultPageSizes {
         get => string.IsNullOrEmpty(_defaultPageSizes) ? "20,50,100" : _defaultPageSizes;
         set => _defaultPageSizes = string.IsNullOrEmpty(value) ? "20,50,100" : value;
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

      public string MapBoxToken {
         get => string.IsNullOrWhiteSpace(_mapBoxToken) ? string.Empty : _mapBoxToken;
         set => _mapBoxToken = string.IsNullOrWhiteSpace(value) ? string.Empty : value;
      }

   }
}
