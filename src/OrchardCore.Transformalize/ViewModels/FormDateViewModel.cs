using System.Globalization;
using Transformalize.Configuration;

namespace TransformalizeModule.ViewModels {
   public class FormDateViewModel {
      public required Parameter Parameter { get; init; }
      public bool Valid { get; init; }
      // Match the culture used to serialize loaded/validated DateTime values.
      // This is only a calendar hint; the named field keeps the original text.
      public string CalendarDate => DateTime.TryParse(Parameter.Value, CultureInfo.CurrentCulture, DateTimeStyles.None, out var date)
         ? date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)
         : string.Empty;
      public string CalendarTime => DateTime.TryParse(Parameter.Value, CultureInfo.CurrentCulture, DateTimeStyles.None, out var date)
         && date.TimeOfDay != TimeSpan.Zero
         ? date.ToString("HH:mm:ss.FFFFFFF", CultureInfo.InvariantCulture)
         : string.Empty;
   }
}
