using System.Collections.Generic;

namespace Module.Models {
   public class ArrangementModifierResponse {
      public string Arrangement { get; set; }
      public IDictionary<string,string> Parameters { get; set; }
   }

}
