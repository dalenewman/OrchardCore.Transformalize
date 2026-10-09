using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using OrchardCore.ContentManagement;
using OrchardCore.FileStorage;
using TransformalizeModule.Controllers;
using TransformalizeModule.Models;
using TransformalizeModule.Services.Contracts;

namespace OrchardCore.Transformalize.Tests;

[TestClass]
public class FileUploadTests {
   [TestMethod]
   public void ClearingAnAttachmentPostsAnExplicitEmptyIdInsteadOfRestoringThePreviousId() {
      var context = new DefaultHttpContext();
      context.Request.Method = "POST";
      context.Request.ContentType = "application/x-www-form-urlencoded";
      context.Request.Form = new FormCollection(new Dictionary<string, Microsoft.Extensions.Primitives.StringValues> {
         ["Evidence_Old"] = "",
         ["OtherFile_Old"] = "retained-id"
      });
      var files = TransformalizeModule.Common.GetFileParameters(context.Request);
      Assert.IsTrue(files.ContainsKey("Evidence"), "Removal must override an existing arrangement/database value.");
      Assert.AreEqual(string.Empty, files["Evidence"]);
      Assert.AreEqual("retained-id", files["OtherFile"]);
   }

   [TestMethod]
   public async Task UploadReturnsValidJsonForQuotedAndUnicodeFilenames() {
      const string filename = "photo \"quoted\" café.jpg";
      var item = new ContentItem { ContentItemId = "stored-file" };
      item.Weld(new TransformalizeFilePart());
      var manager = new Mock<IContentManager>();
      manager.Setup(m => m.NewAsync("TransformalizeFile")).ReturnsAsync(item);
      var entry = new Mock<IFileStoreEntry>();
      entry.SetupGet(e => e.Path).Returns("test/photo.jpg");
      var store = new Mock<ICustomFileStore>();
      store.Setup(s => s.GetFileInfoAsync(It.IsAny<string>())).ReturnsAsync(entry.Object);
      var controller = new FileController(store.Object, manager.Object, null!, null!) {
         ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
      };
      controller.HttpContext.User = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.Name, "test-user") }, "test"));
      controller.Request.ContentType = "multipart/form-data";
      using var stream = new MemoryStream(new byte[] { 1, 2, 3 });
      controller.Request.Form = new FormCollection(new Dictionary<string, Microsoft.Extensions.Primitives.StringValues>(),
         new FormFileCollection { new FormFile(stream, 0, stream.Length, "Evidence", filename) });
      var result = await controller.Upload();
      using var json = JsonDocument.Parse(result.Content!);
      Assert.AreEqual("stored-file", json.RootElement.GetProperty("id").GetString());
      Assert.AreEqual(filename, json.RootElement.GetProperty("message").GetString());
      store.Verify(s => s.CreateFileFromStreamAsync(It.IsAny<string>(), It.IsAny<Stream>(), true), Times.Once);
   }
}
