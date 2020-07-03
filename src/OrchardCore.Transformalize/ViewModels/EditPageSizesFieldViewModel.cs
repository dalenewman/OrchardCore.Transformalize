using OrchardCore.TransformalizeModule.Fields;
using OrchardCore.ContentManagement;
using OrchardCore.ContentManagement.Metadata.Models;

namespace OrchardCore.TransformalizeModule.ViewModels {
   public class EditPageSizesFieldViewModel {
      public string PageSizes { get; set; }
      public PageSizesField Field { get; set; }
      public ContentPart Part { get; set; }
      public ContentPartFieldDefinition PartFieldDefinition { get; set; }
   }
}
