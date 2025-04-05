using System.Security.Claims;

namespace TransformalizeModule.Models {
   public class TransformalizeRequest {
      private string _format = null;

      public string Mode { get; set; } = "default";
      public string ContentItemId { get; set; } = string.Empty;
      public bool Secure { get; set; } = true;
      public bool ValidateParameters { get; set; } = true;
      public string Format {
         get { return _format; }
         set {
            _format = value;
            ContentType = value switch {
               "json" => "application/json",
               "xml" => "application/xml",
               _ => "text/html",
            };
         }
      }
      public string ContentType { get; private set; } = "text/html";
      public Dictionary<string, string> InternalParameters { get; set; } = null;

      public TransformalizeRequest(string contentItemId) {
         ContentItemId = contentItemId;
      }
   }
}
