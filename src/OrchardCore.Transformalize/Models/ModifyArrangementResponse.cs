using System.Collections.Generic;

namespace TransformalizeModule.Models {
   public class ArrangementModifierResponse {

      /// <summary>
      /// most likely an XML or JSON transformalize arrangement
      /// </summary>
      public string Arrangement { get; set; }

      /// <summary>
      /// returns what's left of the parameters, as they may have been transformed (consumed)
      /// </summary>
      public IDictionary<string,string> Parameters { get; set; }
   }

}
