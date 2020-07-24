using OrchardCore.ContentManagement;
using TransformalizeModule.Fields;
using OrchardCore.ContentFields.Fields;

namespace TransformalizeModule.Models {
   public class TransformalizeReportPart : ContentPart {
      public TransformalizeReportPart() {
         Arrangement = new TransformalizeArrangementField() { Arrangement = @"<cfg name=""report"">
   <parameters>
   </parameters>
   <connections>
   </connections>
   <entities>
      <add name=""entity"">
         <filter>
         </filter>
         <fields>
         </fields>
      </add>
   </entities>
</cfg>" };
         PageSizes = new TextField();
         BulkActions = new BooleanField();
         BulkActionValueField = new TextField();
         BulkActionCreateTask = new TextField();
         BulkActionWriteTask = new TextField();
         BulkActionSummaryTask = new TextField();
         BulkActionRunTask = new TextField();
         BulkActionSuccessTask = new TextField();
         BulkActionFailTask = new TextField();
      }
      public TransformalizeArrangementField Arrangement { get; set; }
      public TextField PageSizes { get; set; }
      public BooleanField BulkActions { get; set; }
      public TextField BulkActionValueField { get; set; }
      public TextField BulkActionCreateTask { get; set; }
      public TextField BulkActionWriteTask { get; set; }
      public TextField BulkActionSummaryTask { get; set; }
      public TextField BulkActionRunTask { get; set; }
      public TextField BulkActionSuccessTask { get; set; }
      public TextField BulkActionFailTask { get; set; }

   }
}
