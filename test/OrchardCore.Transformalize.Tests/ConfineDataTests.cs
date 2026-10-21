using Microsoft.VisualStudio.TestTools.UnitTesting;
using Transformalize.Configuration;
using TransformalizeModule.Services;

namespace OrchardCore.Transformalize.Tests;

[TestClass]
public class ConfineDataTests {
   [DataTestMethod]
   [DataRow("parameter")]
   [DataRow("map")]
   [DataRow("text")]
   public void CircularDependenciesAreVisitedOnceAndKept(string kind) {
      var process = CreateProcess("A", "B", "Unused");
      var entity = process.Entities.Single();
      var a = entity.GetField("A");
      var b = entity.GetField("B");
      if (kind == "parameter") {
         a.Transforms.Add(Copy("B"));
      } else if (kind == "map") {
         process.Maps.Add(new Map {
            Name = "lookup", Items = [new MapItem { From = "key", Parameter = "B" }]
         });
         a.Transforms.Add(new Operation { Method = "map", Map = "lookup" });
      } else {
         a.Transforms.Add(new Operation { Method = "format", Template = "{B}" });
      }
      b.Transforms.Add(Copy("A"));

      ArrangementLoadService.ConfineData(process, new Dictionary<string, string> { ["A"] = "title" });

      Assert.AreEqual("title", a.Alias);
      Assert.IsTrue(a.Output);
      Assert.IsTrue(b.Input, "The circular dependency is still required to compute the output.");
      Assert.AreEqual(1, b.Transforms.Count);
      Assert.IsFalse(entity.GetField("Unused").Input);
   }

   [TestMethod]
   public void DependenciesAreResolvedBeforeCalendarAliasesReplaceSourceNames() {
      var process = CreateProcess("start", "CalendarStart", "Unused");
      var entity = process.Entities.Single();
      var source = entity.GetField("start");
      source.Output = false;
      var calendarStart = entity.GetField("CalendarStart");
      calendarStart.Transforms.Add(Copy("start"));

      ArrangementLoadService.ConfineData(process, new Dictionary<string, string> { ["CalendarStart"] = "start" });

      Assert.AreEqual("start", calendarStart.Alias);
      Assert.IsTrue(calendarStart.Output);
      Assert.IsTrue(source.Input, "Renaming the output must not prune its original input.");
      Assert.IsFalse(source.Output);
      Assert.AreSame(source, calendarStart.Transforms.Single().Parameters.Single().AsField(entity));
   }

   [TestMethod]
   public void RepeatedCalendarRequestsKeepTheirOwnIntermediateDateDependencies() {
      for (var request = 0; request < 3; request++) {
         var process = CreateProcess("cdatetime", "startdate", "enddate", "start", "end", "Unused");
         var entity = process.Entities.Single();
         foreach (var (field, source) in new[] {
            ("startdate", "cdatetime"), ("enddate", "startdate"), ("start", "startdate"), ("end", "enddate")
         }) {
            // copy(field) is stored in Operation.Value until transforms are built.
            entity.GetField(field).Transforms.Add(new Operation { Method = "copy", Value = source });
         }

         ArrangementLoadService.ConfineData(process, new Dictionary<string, string> { ["start"] = "start", ["end"] = "end" });

         Assert.IsTrue(entity.GetField("cdatetime").Input, $"Request {request}: read the source date from the database.");
         Assert.AreEqual(1, entity.GetField("startdate").Transforms.Count, $"Request {request}: compute the intermediate start date.");
         Assert.AreEqual(1, entity.GetField("enddate").Transforms.Count, $"Request {request}: compute the intermediate end date.");
         Assert.IsFalse(entity.GetField("Unused").Input);
      }
   }

   [TestMethod]
   public void LongDependencyChainsDoNotUseTheCallStack() {
      var process = CreateProcess("Value");
      var entity = process.Entities.Single();
      const int count = 5000;
      for (var i = 0; i < count; i++) {
         entity.CalculatedFields.Add(new Field {
            Name = $"F{i}", Alias = $"F{i}", Input = false,
            Transforms = [Copy(i == 0 ? "Value" : $"F{i - 1}")]
         });
      }

      ArrangementLoadService.ConfineData(process, new Dictionary<string, string> { [$"F{count - 1}"] = "title" });

      Assert.IsTrue(entity.GetField("Value").Input);
      Assert.IsTrue(entity.CalculatedFields.All(field => field.Transforms.Count == 1));
   }

   [TestMethod]
   public void ExplicitQueriesKeepTheirFieldsAndTransforms() {
      var process = CreateProcess("A", "B");
      var entity = process.Entities.Single();
      entity.Query = "select A, B from Events";
      entity.GetField("B").Transforms.Add(Copy("A"));

      ArrangementLoadService.ConfineData(process, new Dictionary<string, string> { ["A"] = "id" });

      Assert.IsTrue(entity.GetField("B").Input);
      Assert.AreEqual(1, entity.GetField("B").Transforms.Count);
      Assert.AreEqual("id", entity.GetField("A").Alias);
   }

   private static Operation Copy(string field) => new() {
      Method = "copy", Parameters = [new Parameter { Field = field }]
   };

   private static Process CreateProcess(params string[] fields) => new($"""
      <cfg name="Calendar">
         <entities>
            <add name="Events">
               <fields>{string.Join("", fields.Select(name => $"<add name='{name}' />"))}</fields>
            </add>
         </entities>
      </cfg>
      """);
}
