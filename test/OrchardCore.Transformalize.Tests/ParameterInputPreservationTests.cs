using System.Globalization;
using Autofac;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Localization;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using OrchardCore.ContentManagement;
using OrchardCore.DisplayManagement.Notify;
using OrchardCore.Environment.Cache;
using Transformalize.Configuration;
using Transformalize.Logging;
using TransformalizeModule.Services;
using TransformalizeModule.Services.Contracts;
using TransformalizeModule.Services.Modifiers;
using TransformalizeModule.ViewModels;

namespace OrchardCore.Transformalize.Tests;

[TestClass]
public class ParameterInputPreservationTests {
   [DataTestMethod]
   [DataRow("datetime", "1/8/2006 x:00:00 AM")]
   [DataRow("date", "2026-02-30")]
   [DataRow("datetime", "")]
   [DataRow("int", "x")]
   public async Task FailedConversionPreservesInputAndErrorThroughFormConfigurationLoading(string type, string input) {
      var originalCulture = CultureInfo.CurrentCulture;
      try {
         CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("en-US");
         using var fixture = new FormConfiguration();
         var cfg = $"""
            <cfg name="Edit">
              <parameters>
                <add name="Value" type="{type}" input-type="date" prompt="true" v="required().is({type})" />
              </parameters>
            </cfg>
            """;
         for (var refresh = 0; refresh < 2; refresh++) {
            var process = await fixture.Load(cfg, input);
            var parameter = process.Parameters.Single();
            Assert.AreEqual(input, parameter.Value);
            Assert.IsFalse(parameter.Valid);
            StringAssert.Contains(parameter.Message, type == "int" ? "Must be a whole number" : "Can not convert");
            var date = new FormDateViewModel { Parameter = parameter };
            Assert.AreEqual("", date.CalendarDate);
            Assert.AreEqual("", date.CalendarTime);
         }

         // Correcting the next post uses the original arrangement and validates normally.
         var correctedInput = type == "int" ? "5" : "2006-01-08T01:00:00";
         var corrected = (await fixture.Load(cfg, correctedInput)).Parameters.Single();
         Assert.IsTrue(corrected.Valid);
         Assert.AreEqual("", corrected.Message);
         if (type == "int") Assert.AreEqual("5", corrected.Value);
         else Assert.AreEqual(new DateTime(2006, 1, 8, 1, 0, 0), DateTime.Parse(corrected.Value));
      } finally {
         CultureInfo.CurrentCulture = originalCulture;
      }
   }

   [TestMethod]
   public async Task SuccessfulConversionsAndTransformsStillReturnTheirTransformedValues() {
      using var fixture = new FormConfiguration();
      var process = await fixture.Load("""
         <cfg name="Edit">
           <parameters>
             <add name="Value" prompt="true" t="trim().toupper()" v="required().length(3)" />
           </parameters>
         </cfg>
         """, "  ab  ");
      var parameter = process.Parameters.Single();
      Assert.AreEqual("AB", parameter.Value);
      Assert.IsFalse(parameter.Valid, "A validation error must not undo successful transforms.");
      Assert.IsFalse(string.IsNullOrEmpty(parameter.Message));
      Assert.AreEqual(0, parameter.Transforms.Count);
      Assert.AreEqual(0, parameter.Validators.Count);
   }

   private sealed class FormConfiguration : IDisposable {
      private readonly MemoryCache _cache = new(new MemoryCacheOptions());
      private readonly OrchardConfigurationContainer _configuration;

      public FormConfiguration() {
         var http = new HttpContextAccessor { HttpContext = new DefaultHttpContext() };
         http.HttpContext.Request.Method = "POST";
         var container = new OrchardContainer(http, null!, null!, null!, Logger<OrchardContainer>(),
            null!, null!, _cache, Mock.Of<ISignal>());
         var modifier = new TransformalizeParametersModifier(Logger<TransferParameterModifier>(),
            Mock.Of<IHtmlLocalizer<TransferParameterModifier>>(), Mock.Of<ISettingsService>(), container, Mock.Of<INotifier>());
         _configuration = new OrchardConfigurationContainer(Logger<OrchardConfigurationContainer>(), modifier,
            Mock.Of<ILoadFormModifier>(), http);
      }

      public async Task<Process> Load(string cfg, string input) {
         await using var scope = await _configuration.CreateScopeAsync(cfg, new ContentItem { Id = 1 },
            new Dictionary<string, string> { ["Value"] = input });
         var process = scope.Resolve<Process>();
         Assert.AreEqual(0, process.Errors().Length, string.Join("; ", process.Errors()));
         return process;
      }

      public void Dispose() => _cache.Dispose();

      private static CombinedLogger<T> Logger<T>() => new(NullLogger<T>.Instance,
         new MemoryLogger(global::Transformalize.Contracts.LogLevel.Debug));
   }
}
