using Transformalize.Configuration;

namespace Module.ViewModels {
   public class BulkActionViewModel {
      public Process Summary { get; set; }
      public Process Task { get; set; }

      public BulkActionViewModel(Process summary, Process task) {
         Summary = summary;
         Task = task;
      }
   }
}
