using OrchardCore.ContentFields.Fields;
using OrchardCore.ContentManagement;

namespace TransformalizeModule.Models {
   public class TransformalizeReportPart : ContentPart {
      public TransformalizeReportPart() {

         Arrangement = new TextField() { Text = @"<cfg name=""report"">
   <parameters>
   </parameters>
   <connections>
      <add name=""input"" provider="""" />
   </connections>
   <entities>
      <add name=""entity"">
         <fields>
         </fields>
      </add>
   </entities>
</cfg>" };
         PageSizes = new TextField();
         PageSizesExtended = new TextField();

         BulkActions = new BooleanField();
         BulkActionValueField = new TextField();
         BulkActionCreateTask = new TextField();
         BulkActionWriteTask = new TextField();
         BulkActionSummaryTask = new TextField();
         BulkActionRunTask = new TextField();
         BulkActionSuccessTask = new TextField();
         BulkActionFailTask = new TextField();

         Map = new BooleanField();
         MapDescriptionField = new TextField { Text = "geojson-description" };
         MapLatitudeField = new TextField { Text = "latitude" };
         MapLongitudeField = new TextField { Text = "longitude" };
         MapColorField = new TextField { Text = "geojson-color" };
         MapRadiusField = new TextField { Text = "7" };
         MapOpacityField = new TextField { Text = "0.8" };

         Calendar = new BooleanField();
         CalendarIdField = new TextField { Text = "id" };
         CalendarTitleField = new TextField { Text = "title" };
         CalendarUrlField = new TextField { Text = "url" };
         CalendarClassField = new TextField { Text = "class" };
         CalendarStartField = new TextField { Text = "start" };
         CalendarEndField = new TextField { Text = "end" };

         Chart = new BooleanField();
         ChartField1 = new TextField();
         ChartField2 = new TextField();
         ChartField3 = new TextField();
         ChartType = new TextField { Text = "doughnut" };
         ChartTitle = new TextField();
         ChartDisplayLegend = new BooleanField { Value = true };
         ChartShowPercentage = new BooleanField();
         ChartColorPaletteMap = new TextField();
         ChartTitleLink = new LinkField();
         ChartUseRawData = new BooleanField();
         ChartRawDataLabelField = new TextField();

      }
      public TextField Arrangement { get; set; }
      public TextField PageSizes { get; set; }
      public TextField PageSizesExtended { get; set; }
      
      public BooleanField BulkActions { get; set; }
      public TextField BulkActionValueField { get; set; }
      public TextField BulkActionCreateTask { get; set; }
      public TextField BulkActionWriteTask { get; set; }
      public TextField BulkActionSummaryTask { get; set; }
      public TextField BulkActionRunTask { get; set; }
      public TextField BulkActionSuccessTask { get; set; }
      public TextField BulkActionFailTask { get; set; }

      public BooleanField Map { get; set; }
      public TextField MapDescriptionField { get; set; }
      public TextField MapLatitudeField { get; set; }
      public TextField MapLongitudeField { get; set; }
      public TextField MapColorField { get; set; }
      public TextField MapRadiusField { get; set; }
      public TextField MapOpacityField { get; set; }

      public BooleanField Calendar { get; set; }
      public TextField CalendarIdField { get; set; }
      public TextField CalendarTitleField { get; set; }
      public TextField CalendarUrlField { get; set; }
      public TextField CalendarClassField { get; set; }
      public TextField CalendarStartField { get; set; }
      public TextField CalendarEndField { get; set; }

      public BooleanField Chart { get; set; }
      public TextField ChartField1 { get; set; }
      public TextField ChartField2 { get; set; }
      public TextField ChartField3 { get; set; }
      public TextField ChartType { get; set; }
      public TextField ChartTitle { get; set; }
      public BooleanField ChartDisplayLegend { get; set; }
      public BooleanField ChartShowPercentage { get; set; }
      public TextField ChartColorPaletteMap { get; set; }
      public LinkField ChartTitleLink { get; set; }
      public BooleanField ChartUseRawData { get; set; }
      public TextField ChartRawDataLabelField { get; set; }

   }
}
