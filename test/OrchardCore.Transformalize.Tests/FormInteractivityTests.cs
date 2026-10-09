using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Mvc.Localization;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using OrchardCore.DisplayManagement.Notify;
using Transformalize.Configuration;
using TransformalizeModule.Controllers;
using TransformalizeModule.Ext;
using TransformalizeModule.Models;
using TransformalizeModule.Services.Contracts;
using TransformalizeModule.ViewModels;

namespace OrchardCore.Transformalize.Tests;

[TestClass]
public class FormInteractivityTests {
   [TestMethod]
   public async Task NativeFormSubmissionKeepsServerErrorsAndDoesNotSaveInvalidParameters() {
      var process = new Process();
      process.Parameters.Add(new Parameter { Name = "Teammate", Value = "Scott", Valid = false, Message = "Scott is remote." });
      var response = new TransformalizeResponse<TransformalizeFormPart> { Valid = true, Process = process };
      var service = new Mock<IFormService>();
      service.Setup(s => s.ValidateForm(It.IsAny<TransformalizeRequest>())).ReturnsAsync(response);
      var localizer = new Mock<IHtmlLocalizer<FormController>>();
      localizer.Setup(l => l[It.IsAny<string>()]).Returns((string message) => new LocalizedHtmlString(message, message));
      var accessor = new HttpContextAccessor();
      var container = new Mock<TransformalizeModule.Services.Contracts.IContainer>();
      var controller = Setup(new FormController(service.Object, new Mock<INotifier>().Object,
         container.Object, accessor, localizer.Object, null!));
      accessor.HttpContext = controller.HttpContext;

      var result = (ViewResult)await controller.Index("got-up-form");
      Assert.AreSame(response, result.Model);
      Assert.AreEqual("Scott is remote.", response.Process.Parameters[0].Message);
      service.Verify(s => s.ValidateForm(It.IsAny<TransformalizeRequest>()), Times.Once);
      service.Verify(s => s.RunAsync(It.IsAny<Process>(), It.IsAny<CancellationToken>()), Times.Never);
      container.VerifyNoOtherCalls();
   }

   [DataTestMethod]
   [DataRow("text", true)]
   [DataRow("date", true)]
   [DataRow("time", true)]
   [DataRow("file", true)]
   [DataRow("scan", true)]
   [DataRow("location", true)]
   [DataRow("map", true)]
   [DataRow("google-places-autocomplete", true)]
   public void WidgetEligibility(string inputType, bool supported) {
      var process = new Process();
      process.Parameters.Add(new Parameter { Name = "Value", InputType = inputType, Prompt = true });
      Assert.AreEqual(supported, FormInteractivityViewModel.Supports(process));
   }

   [TestMethod]
   public void RawMarkupAndGlobalScriptsUseLegacyFlow() {
      var process = new Process();
      process.Parameters.Add(new Parameter { Name = "Value", Raw = true, Value = "<script>custom()</script>" });
      Assert.IsFalse(FormInteractivityViewModel.Supports(process));
      process.Parameters.Clear();
      process.Scripts.Add(new Script { Global = true, Language = "js", Content = "custom()" });
      Assert.IsFalse(FormInteractivityViewModel.Supports(process));
      process.Scripts.Clear();
      process.Scripts.Add(new Script { Global = true, Language = "default", File = "custom.js" });
      Assert.IsFalse(FormInteractivityViewModel.Supports(process));
   }

   [TestMethod]
   public void DecorativeRawMarkupCanRefreshWithTheForm() {
      var process = new Process();
      process.Parameters.Add(new Parameter { Name = "Preview", Raw = true, Value = "<span class='badge'>Preview</span>" });
      Assert.IsTrue(FormInteractivityViewModel.Supports(process));
      process.Parameters[0].Value = "<button onclick='custom()'>Preview</button>";
      Assert.IsFalse(FormInteractivityViewModel.Supports(process));
   }

   [TestMethod]
   public void OnlyValidTaskParametersSelectTheRunAction() {
      var process = new Process();
      process.Parameters.Add(new Parameter { Name = "Value", Valid = false });
      var model = Model(process);
      Assert.AreEqual("/review", model.ActionUrl);
      process.Parameters[0].Valid = true;
      Assert.AreEqual("/run", model.ActionUrl);
      var form = new FormInteractivityViewModel { Process = process, ValidationUrl = "/validate", SubmitUrl = "/save" };
      Assert.AreEqual("/save", form.ActionUrl);
   }

   [DataTestMethod]
   [DataRow(false, false, false, false)]
   [DataRow(true, false, false, true)]
   [DataRow(true, true, false, false)]
   [DataRow(true, false, true, false)]
   public void FragmentRoutingHonorsTargetAndHistory(bool hx, bool history, bool otherTarget, bool partial) {
      var controller = Setup(new TaskController(null!, null!, null!));
      var request = controller.Request;
      if (hx) request.Headers["HX-Request"] = "true";
      if (history) request.Headers["HX-History-Restore-Request"] = "true";
      request.Headers["HX-Target"] = otherTarget ? "other" : "tfl-form";
      var model = Model(new Process());
      var result = controller.FormFragment(model);
      if (partial) {
         Assert.IsInstanceOfType<PartialViewResult>(result);
         Assert.AreEqual("_InteractiveForm", ((PartialViewResult)result).ViewName);
         Assert.AreSame(model, ((PartialViewResult)result).Model);
         Assert.AreEqual("true", controller.Response.Headers["X-Transformalize-Form"].ToString());
      } else {
         Assert.IsInstanceOfType<ViewResult>(result);
         Assert.AreEqual("Form", ((ViewResult)result).ViewName);
      }
      Assert.IsTrue(controller.Response.Headers.Vary.ToString().Contains("HX-Target"));
   }

   [TestMethod]
   public void UnsupportedFragmentRequestsUseTheLegacyView() {
      var process = new Process();
      process.Parameters.Add(new Parameter { Name = "Custom", Raw = true, Value = "<script>custom()</script>" });
      var controller = Setup(new TaskController(null!, null!, null!));
      controller.Request.Headers["HX-Request"] = "true";
      controller.Request.Headers["HX-Target"] = "tfl-form";
      Assert.IsInstanceOfType<ViewResult>(controller.FormFragment(Model(process)));
      Assert.IsFalse(controller.Response.Headers.ContainsKey("X-Transformalize-Form"));
   }

   [TestMethod]
   public async Task FormValidationRefreshDoesNotSaveOrRun() {
      var service = new Mock<IFormService>();
      service.Setup(s => s.ValidateForm(It.IsAny<TransformalizeRequest>())).ReturnsAsync(new TransformalizeResponse<TransformalizeFormPart> { Valid = true, Process = new Process() });
      var controller = Setup(new FormController(service.Object, null!, null!, null!, null!, null!));
      controller.Request.Headers["HX-Request"] = "true";
      controller.Request.Headers["HX-Target"] = "tfl-form";
      var result = await controller.Form("demo");
      Assert.IsInstanceOfType<PartialViewResult>(result);
      service.Verify(s => s.RunAsync(It.IsAny<Process>(), It.IsAny<CancellationToken>()), Times.Never);
   }

   [TestMethod]
   public async Task LocationRefreshRetainsConfiguredAccuracyAndTimeout() {
      var process = new Process();
      process.Parameters.Add(new Parameter { Name = "Latitude", InputType = "location", InputCapture = "latitude" });
      var part = new TransformalizeFormPart();
      part.LocationEnableHighAccuracy.Value = true;
      part.LocationMaximumAge.Value = 15000;
      part.LocationTimeout.Value = 5000;
      var service = new Mock<IFormService>();
      service.Setup(s => s.ValidateForm(It.IsAny<TransformalizeRequest>())).ReturnsAsync(new TransformalizeResponse<TransformalizeFormPart> {
         Valid = true, Process = process, Part = part
      });
      var controller = Setup(new FormController(service.Object, null!, null!, null!, null!, null!));
      controller.Request.Headers["HX-Request"] = "true";
      controller.Request.Headers["HX-Target"] = "tfl-form";
      var result = (PartialViewResult)await controller.Form("demo");
      var model = (FormInteractivityViewModel)result.Model!;
      Assert.IsTrue(model.HasLocation);
      Assert.IsTrue(model.LocationEnableHighAccuracy);
      Assert.AreEqual(15000, model.LocationMaximumAge);
      Assert.AreEqual(5000, model.LocationTimeout);
   }

   [TestMethod]
   public async Task TaskRefreshRetainsInvalidParametersWithoutRunningTheTask() {
      var service = new Mock<IFormService>();
      var tasks = new Mock<ITaskService>();
      var process = new Process();
      process.Parameters.Add(new Parameter { Name = "Value", Prompt = true, Valid = false });
      service.Setup(s => s.ValidateParameters(It.IsAny<TransformalizeRequest>())).ReturnsAsync(new TransformalizeResponse<TransformalizeTaskPart> { Valid = true, Process = process });
      var controller = Setup(new TaskController(tasks.Object, service.Object, null!));
      controller.Request.Headers["HX-Request"] = "true";
      controller.Request.Headers["HX-Target"] = "tfl-form";
      var result = await controller.Form("demo");
      var model = (FormInteractivityViewModel)((PartialViewResult)result).Model!;
      Assert.AreEqual("/Task/Review", model.ActionUrl);
      Assert.IsFalse(process.Parameters[0].Valid);
      tasks.Verify(s => s.RunAsync(It.IsAny<Process>(), It.IsAny<CancellationToken>()), Times.Never);
   }

   [TestMethod]
   public async Task ValidationFailureKeepsTheOriginalAuthorizationResult() {
      var service = new Mock<IFormService>();
      var forbidden = new ForbidResult();
      service.Setup(s => s.ValidateForm(It.IsAny<TransformalizeRequest>())).ReturnsAsync(new TransformalizeResponse<TransformalizeFormPart> { Valid = false, ActionResult = forbidden });
      var controller = Setup(new FormController(service.Object, null!, null!, null!, null!, null!));
      Assert.AreSame(forbidden, await controller.Form("demo"));
   }

   private static T Setup<T>(T controller) where T : Controller {
      controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };
      controller.Request.Method = "POST";
      controller.Request.ContentType = "application/x-www-form-urlencoded";
      controller.Request.Form = new FormCollection(new Dictionary<string, Microsoft.Extensions.Primitives.StringValues>());
      var urls = new Mock<IUrlHelper>();
      urls.Setup(u => u.Action(It.IsAny<UrlActionContext>())).Returns((UrlActionContext c) => $"/{c.Controller}/{c.Action}");
      controller.Url = urls.Object;
      return controller;
   }

   private static FormInteractivityViewModel Model(Process process) => new() { Process = process, ValidationUrl = "/validate", SubmitUrl = "/review", RunUrl = "/run" };
}
