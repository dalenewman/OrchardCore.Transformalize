using System.Collections.Generic;

namespace Module.Models {
   public class ValidateRequest {
      public string ContentItemId { get; set; } = string.Empty;
      public bool Secure { get; set; } = true;
      public string Format { get; set; } = null;
      public Dictionary<string, string> InternalParameters { get; set; } = null;

      public ValidateRequest() { }

      public ValidateRequest(string contentItemId) {
         ContentItemId = contentItemId;
      }
   }


}
