namespace TransformalizeModule.Models {
   public class BreadCrumb {
      public string Title { get; set; }
      public string Url { get; set; }

      public BreadCrumb(string title, string url) {
         Title = title ?? throw new ArgumentNullException(nameof(title));
         Url = url ?? throw new ArgumentNullException(nameof(url));
      }

      public override string ToString() {
         return $"{Title} ({Url})";
      }
   }
}
