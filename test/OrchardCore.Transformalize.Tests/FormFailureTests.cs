using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Transformalize.Configuration;
using TransformalizeModule.Controllers;
using TransformalizeModule.Ext;

namespace OrchardCore.Transformalize.Tests;

[TestClass]
public class FormFailureTests {
   private static FormController Controller(bool fragment = true) {
      var controller = new FormController(null!, null!, null!, null!, null!, null!) {
         ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
      };
      if (fragment) {
         controller.Request.Headers["HX-Request"] = "true";
         controller.Request.Headers["HX-Target"] = "tfl-form";
      }
      return controller;
   }

   [TestMethod]
   public void ARejectedApostropheGetsAFieldErrorInsteadOfDiagnosticHtml() {
      var process = new Process();
      process.Parameters.Add(new Parameter { Name = "OtherReason", Label = "Other Reason", Value = "I don't want to say" });
      var controller = Controller();
      var result = (JsonResult)controller.FormFailure(process, new BadRequestResult());
      var json = JsonSerializer.SerializeToElement(result.Value);
      Assert.AreEqual("OtherReason", json.GetProperty("fields")[0].GetProperty("name").GetString());
      StringAssert.Contains(json.GetProperty("fields")[0].GetProperty("message").GetString(), "Other Reason");
      StringAssert.Contains(json.GetProperty("fields")[0].GetProperty("message").GetString(), "“'”");
      Assert.AreEqual("true", controller.Response.Headers["X-Transformalize-Form-Error"].ToString());
      Assert.AreEqual(";'`", process.Parameters[0].InvalidCharacters, "Keep the arrangement's restriction.");
   }

   [TestMethod]
   public void ExplicitAllowedCharactersAndNativeOrHistoryRequestsKeepTheirExistingResult() {
      var process = new Process();
      var parameter = new Parameter { Name = "OtherReason", Value = "I don't want to say", InvalidCharacters = "" };
      process.Parameters.Add(parameter);
      var fallback = new BadRequestResult();
      Assert.AreSame(fallback, Controller().FormFailure(process, fallback));
      parameter.InvalidCharacters = "'";
      Assert.AreSame(fallback, Controller(false).FormFailure(process, fallback));
      var history = Controller();
      history.Request.Headers["HX-History-Restore-Request"] = "true";
      Assert.AreSame(fallback, history.FormFailure(process, fallback));
   }

   [TestMethod]
   public void OnlyInputStringFieldsUseCharacterRestrictionsAndRepeatedCharactersAppearOnce() {
      var process = new Process();
      process.Parameters.Add(new Parameter { Name = "OtherReason", Value = "bad!", InvalidCharacters = "!!" });
      process.Parameters.Add(new Parameter { Name = "Raw", Input = false, Value = "<img src='file'>", InvalidCharacters = "'" });
      var result = (JsonResult)Controller().FormFailure(process, new BadRequestResult());
      var fields = JsonSerializer.SerializeToElement(result.Value).GetProperty("fields");
      Assert.AreEqual(1, fields.GetArrayLength());
      StringAssert.Contains(fields[0].GetProperty("message").GetString(), "“!”");
   }
}
