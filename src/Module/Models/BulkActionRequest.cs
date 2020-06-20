﻿using System.Collections.Generic;

namespace Module.Models {

   public class BulkActionRequest {
      public string ContentItemId { get; set; }
      public string ActionName { get; set; }
      public int ActionCount { get; set; }
      public IEnumerable<string> Records { get; set; }
   }

}
