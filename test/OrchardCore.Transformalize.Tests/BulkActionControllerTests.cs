using Microsoft.VisualStudio.TestTools.UnitTesting;
using Transformalize.Configuration;
using Transformalize.Impl;
using TransformalizeModule;
using TransformalizeModule.Controllers;
using TransformalizeModule.Models;
using TflAction = Transformalize.Configuration.Action;

namespace OrchardCore.Transformalize.Tests {

   [TestClass]
   public class BulkActionControllerTests {

      // real-looking 26-char content item IDs and the report route
      private const string ReportId = "4p2x8wvq1nfjm3k7c6d9hy0zre";
      private const string TaskId   = "7r9tqnvb2mjhc5ld0ew8fk3xps";
      private const string Referrer = "https://example.com/t/report/" + ReportId;

      // ── GetFieldFromSummary ──────────────────────────────────────────────

      [TestMethod]
      public void GetFieldFromSummary_ExtractsBatchIdFromSummaryResult() {
         Assert.AreEqual("42", BulkActionController.GetFieldFromSummary(BuildBatchProcess("BatchId", "42"), "BatchId"));
      }

      // ── TransferRequiredParameters ───────────────────────────────────────

      [TestMethod]
      public void TransferRequiredParameters_AddsMissingParamsFromRequest() {
         var request = new BulkActionReviewRequest {
            TaskReferrer     = Referrer,
            TaskContentItemId   = TaskId,
            ReportContentItemId = ReportId
         };
         var response = new TransformalizeResponse<TransformalizeTaskPart>();

         var returned = BulkActionController.TransferRequiredParameters(request, response);

         Assert.AreSame(response, returned, "should return the same response object");
         Assert.AreEqual(Referrer, returned.Process.Parameters.First(p => p.Name == Common.TaskReferrer).Value);
         Assert.AreEqual(TaskId,   returned.Process.Parameters.First(p => p.Name == Common.TaskContentItemId).Value);
         Assert.AreEqual(ReportId, returned.Process.Parameters.First(p => p.Name == Common.ReportContentItemId).Value);
      }

      [TestMethod]
      public void TransferRequiredParameters_DoesNotOverwriteExistingParameters() {
         var request = new BulkActionReviewRequest {
            TaskReferrer        = "https://example.com/t/report/new",
            TaskContentItemId   = "new-task-id-here-000000000",
            ReportContentItemId = ReportId
         };
         var response = new TransformalizeResponse<TransformalizeTaskPart>();
         response.Process.Parameters.Add(new Parameter { Name = Common.TaskReferrer,      Value = Referrer });
         response.Process.Parameters.Add(new Parameter { Name = Common.TaskContentItemId, Value = TaskId });

         BulkActionController.TransferRequiredParameters(request, response);

         Assert.AreEqual(Referrer, response.Process.Parameters.First(p => p.Name == Common.TaskReferrer).Value, "existing TaskReferrer must not be overwritten");
         Assert.AreEqual(TaskId,   response.Process.Parameters.First(p => p.Name == Common.TaskContentItemId).Value, "existing TaskContentItemId must not be overwritten");
         Assert.AreEqual(ReportId, response.Process.Parameters.First(p => p.Name == Common.ReportContentItemId).Value, "missing ReportContentItemId should be added from request");
      }

      // ── ParametersToRouteValues ──────────────────────────────────────────

      [TestMethod]
      public void ParametersToRouteValues_Dict_MapsParametersAndModal() {
         var parameters = new Dictionary<string, string> {
            { Common.ReportContentItemId, ReportId },
            { Common.TaskContentItemId,   TaskId }
         };

         var withoutModal = (IDictionary<string, object?>)BulkActionController.ParametersToRouteValues(parameters, modal: false);
         Assert.IsFalse(withoutModal.ContainsKey("modal"), "modal key must not appear when not in a modal");
         Assert.AreEqual(ReportId, withoutModal[Common.ReportContentItemId]);

         var withModal = (IDictionary<string, object?>)BulkActionController.ParametersToRouteValues(parameters, modal: true);
         Assert.AreEqual("1", withModal["modal"], "modal=1 must be added when request is modal");
      }

      [TestMethod]
      public void ParametersToRouteValues_Parameters_MapsParametersAndModal() {
         var parameters = new List<Parameter> {
            new Parameter { Name = Common.ReportContentItemId, Value = ReportId },
            new Parameter { Name = "RecordsAffected",          Value = "12" }
         };

         var withoutModal = (IDictionary<string, object?>)BulkActionController.ParametersToRouteValues(parameters, modal: false);
         Assert.IsFalse(withoutModal.ContainsKey("modal"));
         Assert.AreEqual(ReportId, withoutModal[Common.ReportContentItemId]);

         var withModal = (IDictionary<string, object?>)BulkActionController.ParametersToRouteValues(parameters, modal: true);
         Assert.AreEqual("1", withModal["modal"]);
         Assert.AreEqual("12", withModal["RecordsAffected"]);
      }

      // ── CountAffectedRecords ─────────────────────────────────────────────

      [DataTestMethod]
      [DataRow(5, 0, 0, 5,  DisplayName = "action RowCount only")]
      [DataRow(0, 7, 0, 7,  DisplayName = "entity Hits only")]
      [DataRow(0, 0, 3, 3,  DisplayName = "entity Rows only")]
      [DataRow(4, 6, 1, 11, DisplayName = "all three sources combined")]
      public void CountAffectedRecords_SumsAllSources(int actionRows, int entityHits, int entityRows, int expected) {
         var process = new Process();
         if (actionRows > 0) process.Actions.Add(new TflAction { RowCount = actionRows });
         var entity = new Entity { Name = "Batch", Hits = entityHits };
         for (int i = 0; i < entityRows; i++) entity.Rows.Add(new CfgRow(new[] { "batchvalue" }));
         process.Entities.Add(entity);

         Assert.AreEqual(expected, BulkActionController.CountAffectedRecords(process));
      }

      // ── helpers ───────────────────────────────────────────────────────────

      private static Process BuildBatchProcess(string fieldAlias, object value) {
         var process = new Process();
         var entity = new Entity { Name = "Batch" };
         entity.Fields.Add(new Field { Name = fieldAlias, Alias = fieldAlias, Output = true });
         var row = new CfgRow(new[] { fieldAlias });
         row[fieldAlias] = value;
         entity.Rows.Add(row);
         process.Entities.Add(entity);
         return process;
      }
   }
}
