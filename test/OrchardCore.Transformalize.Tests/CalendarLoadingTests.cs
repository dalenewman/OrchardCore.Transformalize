using Autofac;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Primitives;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using OrchardCore.ContentManagement;
using OrchardCore.Environment.Cache;
using Transformalize.Configuration;
using Transformalize.Logging;
using TransformalizeModule;
using TransformalizeModule.Models;
using TransformalizeModule.Services;
using TransformalizeModule.Services.Contracts;

namespace OrchardCore.Transformalize.Tests;

[TestClass]
public class CalendarLoadingTests {
   // Preserve the reported false dependency: url mentions crime-edit, while
   // edit contains data-url. No SQL Server is needed to exercise the loader.
   private const string Arrangement = """
      <cfg name="report" output="output">
        <connections>
          <add name="input" provider="internal" />
          <add name="output" provider="json" />
        </connections>
        <entities>
          <add name="SacramentoCrime" input="input">
            <rows><add Id="123" crimedescr="Test crime" cdatetime="2006-01-08T01:00:00" /></rows>
            <fields>
              <add name="Id" type="int" primary-key="true" />
              <add name="crimedescr" />
              <add name="cdatetime" type="datetime" />
              <add name="Unused" />
            </fields>
            <calculated-fields>
              <add name="edit" raw="true"
                t='copy(Id).razor(&lt;a onclick="loadModal(this)" data-url="/t/form/crime-edit?modal=1&amp;Id=@Model.Id"&gt;Edit&lt;/a&gt;)' />
              <add name="url" t='copy(Id).razor(/t/form/crime-edit?modal=1&amp;Id=@Model.Id)' />
              <add name="start" type="long" t="copy(cdatetime).specifykind(utc).tounixtime(ms)" />
              <add name="end" type="long" t="copy(cdatetime).dateadd(15,minute).specifykind(utc).tounixtime(ms)" />
              <add name="class" default="event-info" />
            </calculated-fields>
          </add>
        </entities>
      </cfg>
      """;

   [DataTestMethod]
   [DataRow(false)]
   [DataRow(true)]
   public async Task CrimeEditLinksLoadAndStreamWithoutRecursiveDependencyFailure(bool intermediateDates) {
      await AssertCalendarStream(intermediateDates);
   }

   [TestMethod]
   public async Task IntermediateCalendarDatesStayCorrectAcrossRepeatedRequests() {
      using var cache = new MemoryCache(new MemoryCacheOptions());
      var signal = CreateSignal();
      await AssertCalendarStream(true, cache, signal);
      await AssertCalendarStream(true, cache, signal);
   }

   [TestMethod]
   public async Task FieldMatchersAreReusedAndInvalidatedByTheReportSignal() {
      using var cache = new MemoryCache(new MemoryCacheOptions());
      var signal = CreateSignal();
      var loader = Loader(Mock.Of<IConfigurationContainer>(), cache: cache, signal: signal);
      const string pattern = @"\b(?:Id|start|end)\b";
      const string changedPattern = @"\b(?:Id|begin|end)\b";
      var original = loader.GetFieldMatcher(1, pattern);
      var changed = loader.GetFieldMatcher(1, changedPattern);

      Assert.AreSame(original, loader.GetFieldMatcher(1, pattern));
      Assert.AreNotSame(original, changed, "A same-count field rename must use its new field names immediately.");
      Assert.IsTrue(changed.IsMatch("begin"));
      Assert.IsFalse(changed.IsMatch("start"));

      await signal.SignalTokenAsync(Common.GetCacheKey(2));
      Assert.AreSame(original, loader.GetFieldMatcher(1, pattern), "Saving another report must not evict this matcher.");

      await signal.SignalTokenAsync(Common.GetCacheKey(1));
      Assert.AreNotSame(original, loader.GetFieldMatcher(1, pattern));
      Assert.AreNotSame(changed, loader.GetFieldMatcher(1, changedPattern));
   }

   private static async Task AssertCalendarStream(bool intermediateDates, IMemoryCache? matcherCache = null, ISignal? matcherSignal = null) {
      using var requestCache = matcherCache is null ? new MemoryCache(new MemoryCacheOptions()) : null;
      var arrangement = intermediateDates ? Arrangement
         .Replace("<add name=\"start\" type=\"long\"", """
            <add name="startdate" type="datetime" t="copy(cdatetime).datemath(+20y).specifykind(utc)" />
            <add name="enddate" type="datetime" t="copy(startdate).datemath(+15m).specifykind(utc)" />
            <add name="start" type="long"
            """)
         .Replace("copy(cdatetime).specifykind(utc).tounixtime(ms)", "copy(startdate).tounixtime(ms)")
         .Replace("copy(cdatetime).dateadd(15,minute).specifykind(utc).tounixtime(ms)", "copy(enddate).tounixtime(ms)")
         : Arrangement;
      var item = Item(arrangement);
      var http = new HttpContextAccessor { HttpContext = new DefaultHttpContext() };
      var configuration = new OrchardConfigurationContainer(Logger<OrchardConfigurationContainer>(), null!, null!, http);
      await using var configured = await configuration.CreateScopeAsync(arrangement, item, new Dictionary<string, string>(), false);
      var original = configured.Resolve<Process>();
      Assert.AreEqual(0, original.Errors().Length, string.Join("; ", original.Errors()));
      var entity = original.Entities.Single();
      CollectionAssert.Contains(entity.GetFieldMatches(entity.GetField("url").Transforms.Last().Template).Select(f => f.Name).ToArray(), "edit");
      CollectionAssert.Contains(entity.GetFieldMatches(entity.GetField("edit").Transforms.Last().Template).Select(f => f.Name).ToArray(), "url");

      var configurationMock = new Mock<IConfigurationContainer>();
      configurationMock.Setup(c => c.CreateScopeAsync(It.IsAny<string>(), item, It.IsAny<IDictionary<string, string>>(), true))
         .ReturnsAsync(configured);
      var loader = Loader(configurationMock.Object, cache: matcherCache ?? requestCache!, signal: matcherSignal);
      var process = await loader.LoadForCalendarStreamAsync(item);

      Assert.AreEqual(200, process.Status);
      Assert.AreEqual("stream-calendar", process.Mode);
      entity = process.Entities.Single();
      Assert.IsTrue(entity.GetField("cdatetime").Input);
      Assert.IsFalse(entity.GetField("Unused").Input);
      Assert.AreEqual("Id", entity.GetField("Id").Alias, "Keep existing alias casing used by Razor's dynamic Model.Id.");
      if (intermediateDates) {
         Assert.IsTrue(entity.GetField("startdate").Transforms.Any(), "Keep the intermediate start date's transformations.");
         Assert.IsTrue(entity.GetField("enddate").Transforms.Any(), "Keep the intermediate end date's transformations.");
      }

      using var cache = new MemoryCache(new MemoryCacheOptions());
      var signal = new Mock<ISignal>();
      signal.Setup(s => s.GetToken(It.IsAny<string>())).Returns(new CancellationChangeToken(CancellationToken.None));
      var container = new OrchardContainer(http, null!, null!, null!, Logger<OrchardContainer>(), null!, null!, cache, signal.Object);
      var streamLogger = Logger<ArrangementStreamService>();
      var stream = new ArrangementStreamService(container, streamLogger);
      using var buffer = new MemoryStream();
      await using var writer = new StreamWriter(buffer, leaveOpen: true);

      await stream.RunAsync(process, writer);
      await writer.FlushAsync();

      Assert.AreEqual(200, process.Status, string.Join("; ", streamLogger.Log.Select(entry => entry.Message)));
      using var json = System.Text.Json.JsonDocument.Parse(buffer.ToArray());
      var row = json.RootElement[0];
      Assert.AreEqual("Test crime", row.GetProperty("title").GetString());
      Assert.AreEqual("/t/form/crime-edit?modal=1&Id=123", row.GetProperty("url").GetString());
      Assert.AreEqual("event-info", row.GetProperty("class").GetString());
      Assert.AreEqual(new DateTimeOffset(intermediateDates ? 2026 : 2006, 1, 8, 1, 0, 0, TimeSpan.Zero).ToUnixTimeMilliseconds(), row.GetProperty("start").GetInt64());
      Assert.AreEqual(15 * 60 * 1000, row.GetProperty("end").GetInt64() - row.GetProperty("start").GetInt64());
   }

   [DataTestMethod]
   [DataRow("missing")]
   [DataRow("blank")]
   [DataRow("duplicate")]
   public async Task InvalidCalendarMappingsReturnDiagnosticsForPageAndStream(string kind) {
      foreach (var streaming in new[] { false, true }) {
         var item = Item();
         var part = item.GetOrCreate<TransformalizeReportPart>();
         part.CalendarEndField.Text = kind switch { "blank" => "", "duplicate" => "start", _ => "missing" };
         item.Apply(part);
         var configuration = new OrchardConfigurationContainer(Logger<OrchardConfigurationContainer>(), null!, null!,
            new HttpContextAccessor { HttpContext = new DefaultHttpContext() });
         await using var scope = await configuration.CreateScopeAsync(Arrangement, item, new Dictionary<string, string>(), false);
         var mock = new Mock<IConfigurationContainer>();
         mock.Setup(c => c.CreateScopeAsync(It.IsAny<string>(), item, It.IsAny<IDictionary<string, string>>(), true)).ReturnsAsync(scope);
         var logger = Logger<ArrangementLoadService>();
         var loader = Loader(mock.Object, logger);

         var process = streaming ? await loader.LoadForCalendarStreamAsync(item) : await loader.LoadForCalendarAsync(item);

         Assert.AreEqual(500, process.Status);
         StringAssert.Contains(process.Message, "Calendar configuration");
         Assert.IsTrue(logger.Log.Any(entry => entry.Message.Contains("calendar end")));
         Assert.AreEqual("crimedescr", process.Entities.Single().GetField("crimedescr").Alias,
            "Invalid mapping must be reported before fields or output providers are modified.");
      }
   }

   private static ContentItem Item(string arrangement = Arrangement) {
      var item = new ContentItem { Id = 1, ContentType = "TransformalizeReport" };
      var part = new TransformalizeReportPart();
      part.Arrangement.Text = arrangement;
      part.Calendar.Value = true;
      part.CalendarIdField.Text = "Id";
      part.CalendarTitleField.Text = "crimedescr";
      part.PageSizesExtended.Text = "0";
      item.Weld(part);
      return item;
   }

   private static ArrangementLoadService Loader(IConfigurationContainer configuration, CombinedLogger<ArrangementLoadService>? logger = null,
      IMemoryCache? cache = null, ISignal? signal = null) {
      var parameters = new Mock<IParameterService>();
      parameters.Setup(p => p.GetParameters()).Returns(new Dictionary<string, string>());
      var settings = new Mock<ISettingsService>();
      settings.SetupGet(s => s.Settings).Returns(new TransformalizeSettings());
      return new ArrangementLoadService(null!, parameters.Object, Mock.Of<ISortService>(), settings.Object,
         configuration, Mock.Of<IFileService>(), logger ?? Logger<ArrangementLoadService>(),
         cache ?? new MemoryCache(new MemoryCacheOptions()), signal ?? CreateSignal());
   }

   private static ISignal CreateSignal() {
      var sources = new Dictionary<string, CancellationTokenSource>();
      var signal = new Mock<ISignal>();
      signal.Setup(s => s.GetToken(It.IsAny<string>())).Returns((string key) => {
         if (!sources.TryGetValue(key, out var source)) sources[key] = source = new CancellationTokenSource();
         return new CancellationChangeToken(source.Token);
      });
      signal.Setup(s => s.SignalTokenAsync(It.IsAny<string>())).Returns((string key) => {
         if (sources.Remove(key, out var source)) {
            source.Cancel();
            source.Dispose();
         }
         return Task.CompletedTask;
      });
      return signal.Object;
   }

   private static CombinedLogger<T> Logger<T>() => new(NullLogger<T>.Instance,
      new MemoryLogger(global::Transformalize.Contracts.LogLevel.Debug));
}
