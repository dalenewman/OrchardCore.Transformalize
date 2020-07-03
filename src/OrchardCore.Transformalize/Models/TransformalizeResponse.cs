using Microsoft.AspNetCore.Mvc;
using OrchardCore.ContentManagement;
using Transformalize.Configuration;

namespace TransformalizeModule.Models {

   public class TransformalizeResponse<T> {

      public ContentItem ContentItem { get; set; }
      public T Part { get; set; }
      public Process Process { get; set; }
      public ActionResult ActionResult { get; set; }
      public bool Valid { get; set; }
      public bool Fails() => !Valid;

      public TransformalizeResponse(string format = null) {
         // this provides a non-null Process and determines the default serializer (xml or json)
         Process = format switch
         {
            "json" => new Process("{ \"name\":\"Process\" }"),
            "xml" => new Process("<cfg name=\"Process\" />"),
            _ => new Process(),
         };
      }
   }
}
