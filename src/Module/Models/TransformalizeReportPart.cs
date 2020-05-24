using OrchardCore.ContentManagement;
using Module.Fields;
using OrchardCore.ContentFields.Fields;

namespace Module.Models {
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
         PageSizes = new PageSizesField();
         BulkActions = new BooleanField();
         BulkActionValueField = new TextField();
      }
      public TransformalizeArrangementField Arrangement { get; set; }
      public PageSizesField PageSizes { get; set; }
      public BooleanField BulkActions { get; set; }
      public TextField BulkActionValueField { get; set; }

   }
}
