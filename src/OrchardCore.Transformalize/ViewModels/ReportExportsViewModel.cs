namespace TransformalizeModule.ViewModels {
   public class ReportExportsViewModel {
      public bool Interactive { get; set; }
      public bool Disabled { get; set; }
      public required string CsvUrl { get; set; }
      public required string JsonUrl { get; set; }
      public string? GeoJsonUrl { get; set; }
      public required string TableId { get; set; }
   }
}
