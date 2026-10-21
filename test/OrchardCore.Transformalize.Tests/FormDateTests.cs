using System.Globalization;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Transformalize.Configuration;
using TransformalizeModule.ViewModels;

namespace OrchardCore.Transformalize.Tests;

[TestClass]
public class FormDateTests {
   [DataTestMethod]
   [DataRow("en-US", "1/31/2006 11:50:00\u202fPM", "2006-01-31", "23:50:00")]
   [DataRow("en-GB", "31/01/2006 23:50:00", "2006-01-31", "23:50:00")]
   [DataRow("en-GB", "01/02/2006 23:50:00", "2006-02-01", "23:50:00")]
   [DataRow("de-DE", "31.01.2006 23:50:00", "2006-01-31", "23:50:00")]
   [DataRow("en-US", "1/31/2006 12:00:00\u202fAM", "2006-01-31", "")]
   [DataRow("en-US", "1/31/2006 12:00:00\u202fPM", "2006-01-31", "12:00:00")]
   [DataRow("en-US", "2006-01-31T00:00:00.0000001", "2006-01-31", "00:00:00.0000001")]
   [DataRow("en-US", "2006-01-31T23:50:01.1234567", "2006-01-31", "23:50:01.1234567")]
   [DataRow("en-US", "2024-02-29", "2024-02-29", "")]
   [DataRow("en-US", "2026-02-30", "", "")]
   [DataRow("en-US", "not a date", "", "")]
   [DataRow("en-US", "", "", "")]
   [DataRow("en-US", null, "", "")]
   public void CalendarHintUsesServerCultureAndPreservesParameterValue(string culture, string? value, string expected, string expectedTime) {
      var originalCulture = CultureInfo.CurrentCulture;
      try {
         CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo(culture);
         var parameter = new Parameter { Name = "EventDate", Type = "datetime", InputType = "date", Value = value };
         var model = new FormDateViewModel { Parameter = parameter };
         Assert.AreEqual(expected, model.CalendarDate);
         Assert.AreEqual(expectedTime, model.CalendarTime);
         Assert.AreEqual(value, parameter.Value);
         if (expectedTime != string.Empty) {
            // The new calendar selection must round-trip through the same
            // typed parameter conversion used by the form's server validation.
            var selected = (DateTime)global::Transformalize.Constants.ConversionMap["datetime"]("2006-02-02T" + model.CalendarTime);
            Assert.AreEqual(new DateTime(2006, 2, 2), selected.Date);
            Assert.AreEqual(DateTime.Parse(value!, CultureInfo.CurrentCulture).TimeOfDay, selected.TimeOfDay);
         }
      } finally {
         CultureInfo.CurrentCulture = originalCulture;
      }
   }
}
