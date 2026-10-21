using Transformalize.Configuration;

namespace TransformalizeModule.Models;

public static class CalendarConfiguration {
   // Use the same validation when saving and when loading existing reports.
   public static bool TryCreateFieldMap(TransformalizeReportPart part, Process process,
      out Dictionary<string, string> fields, out List<string> errors) {
      fields = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
      errors = new List<string>();
      var available = process.Entities.FirstOrDefault()?.GetAllFields()
         .Select(field => field.Alias).ToHashSet(StringComparer.OrdinalIgnoreCase) ?? [];
      var mappings = new (string Label, string? Field, string Output)[] {
         ("id", part.CalendarIdField.Text, "id"),
         ("title", part.CalendarTitleField.Text, "title"),
         ("URL", part.CalendarUrlField.Text, "url"),
         ("class", part.CalendarClassField.Text, "class"),
         ("start", part.CalendarStartField.Text, "start"),
         ("end", part.CalendarEndField.Text, "end")
      };
      foreach (var (label, field, output) in mappings) {
         if (string.IsNullOrWhiteSpace(field)) {
            errors.Add($"Set the calendar {label} field to an arrangement field alias.");
            continue;
         }
         if (!fields.TryAdd(field, output)) {
            errors.Add($"The field '{field}' is mapped to more than one calendar value. Use a distinct field alias for calendar {label}.");
         }
         if (!available.Contains(field)) {
            errors.Add($"The field '{field}' used for calendar {label} does not exist.");
         }
      }
      return errors.Count == 0;
   }
}
