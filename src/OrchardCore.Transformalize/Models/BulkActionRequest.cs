namespace TransformalizeModule.Models {

   public class BulkActionRequest {
      public string ContentItemId { get; set; } = string.Empty;
      public string ActionName { get; set; } = string.Empty;
      public int ActionCount { get; set; }
      public IEnumerable<string> Records { get; set; } = [];
   }

}
