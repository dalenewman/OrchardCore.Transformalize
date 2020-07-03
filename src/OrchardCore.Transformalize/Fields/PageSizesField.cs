using OrchardCore.ContentManagement;
using System;
using System.Collections.Generic;

namespace TransformalizeModule.Fields {
   public class PageSizesField : ContentField {

      private List<int> _pageSizes;
      private string pageSizes;

      public string PageSizes { 
         get => pageSizes ?? string.Empty; 
         set => pageSizes = value ?? string.Empty; 
      }

      public bool Enabled() { 
         return PageSizes != "0";
      }

      public bool OverrideDefaults() {
         return PageSizes != string.Empty;
      }

      public IEnumerable<int> AsEnumerable() {
         if (_pageSizes != null) {
            return _pageSizes;
         }
         _pageSizes = new List<int>();
         foreach (var size in PageSizes.Split(',', StringSplitOptions.RemoveEmptyEntries)) {
            if (int.TryParse(size, out int result)) {
               _pageSizes.Add(result);
            }
         }
         return _pageSizes;
      }
   }
}
