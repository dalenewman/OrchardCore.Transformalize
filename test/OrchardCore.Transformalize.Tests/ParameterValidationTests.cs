using System.Net;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Transformalize.Configuration;
using TransformalizeModule.Ext;

namespace OrchardCore.Transformalize.Tests;

[TestClass]
public class ParameterValidationTests {
   [TestMethod]
   public void ModernRulesPreserveExistingParameterValidationMappings() {
      var parameter = new Parameter { V = "required().length(4).numeric().min(1).max(9).is(int)", InputType = "file" };
      var attribute = parameter.ToValidation();
      Assert.IsTrue(attribute.StartsWith("data-tfl-validation=\""));
      var json = WebUtility.HtmlDecode(attribute["data-tfl-validation=\"".Length..^1]);
      var rules = JsonSerializer.Deserialize<Dictionary<string, string>>(json)!;
      Assert.AreEqual("true", rules["required"]);
      Assert.AreEqual("a file is required", rules["required-message"]);
      Assert.AreEqual("[4, 4]", rules["length"]);
      Assert.AreEqual("integer", rules["type"]);
      Assert.AreEqual("1", rules["min"]);
      Assert.AreEqual("9", rules["max"]);
      Assert.IsFalse(attribute.Contains("data-parsley"));
   }

   [TestMethod]
   public void PatternAttributesCannotBreakOutOfTheirHtmlAttribute() {
      var parameter = new Parameter { V = "matches(^[\"<>&]+$).is(date)" };
      var modern = parameter.ToValidation();
      var rules = JsonSerializer.Deserialize<Dictionary<string, string>>(WebUtility.HtmlDecode(modern["data-tfl-validation=\"".Length..^1]))!;
      Assert.AreEqual("^[\"<>&]+$", rules["pattern"]);
      Assert.AreEqual("true", rules["date"]);
      Assert.IsFalse(modern.Contains("<"));
      Assert.IsFalse(modern.Contains("pattern=\"^[\""));
      Assert.IsTrue(modern.Contains("&quot;"));
   }

   [TestMethod]
   public void ServerOnlyRulesAndEmptyValidationDoNotInventClientConstraints() {
      Assert.AreEqual("", new Parameter().ToValidation());
      Assert.AreEqual("", new Parameter { V = "map(Options)" }.ToValidation());
   }
}
