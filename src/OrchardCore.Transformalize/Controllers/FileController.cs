using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using TransformalizeModule.Services.Contracts;
using TransformalizeModule.Services;
using TransformalizeModule.Models;
using Microsoft.AspNetCore.Authorization;
using System.IO;
using OrchardCore.ContentManagement;

namespace TransformalizeModule.Controllers {

   [Authorize]
   public class FileController : Controller {

      private readonly CombinedLogger<FileController> _logger;
      private readonly ICustomFileStore _formFileStore;
      private readonly IFileService _fileService;
      private readonly IContentManager _contentManager;

      public FileController(
         ICustomFileStore formFileStore,
         IContentManager contentManager,
         IFileService fileService,
         CombinedLogger<FileController> logger
      ) {
         _logger = logger;
         _fileService = fileService;
         _formFileStore = formFileStore;
         _contentManager = contentManager;
      }

      public async Task<ActionResult> Index(string contentItemId) {

         var part = await _fileService.GetFilePart(contentItemId);
         if (part == null) {
            return NotFound();
         }
         var fileInfo = await _formFileStore.GetFileInfoAsync(part.FullPath.Text);

         var mimeType = part.MimeType();
         return new FileStreamResult(await _formFileStore.GetFileStreamAsync(fileInfo), mimeType);
      }


      [HttpPost]
      public async Task<ContentResult> Upload() {

         if (!User.Identity.IsAuthenticated) {
            return GetResult(string.Empty, "Unauthorized");
         }

         if (Request.HasFormContentType && Request.Form.Files != null && Request.Form.Files.Count > 0) {
            var file = Request.Form.Files[0];
            if (file != null && file.Length > 0) {

               //TODO: move to file server so parameters for task can use it too
               var filePath = Path.Combine(Common.GetSafeFilePath(HttpContext.User.Identity.Name, file.FileName));
               var contentItem = await _contentManager.NewAsync("TransformalizeFile");
               var part = contentItem.As<TransformalizeFilePart>();

               part.OriginalName.Text = file.FileName;
               using (var stream = file.OpenReadStream()) {
                  await _formFileStore.CreateFileFromStreamAsync(filePath, stream, true);
               }

               var fileInfo = await _formFileStore.GetFileInfoAsync(filePath);

               part.FullPath.Text = fileInfo.Path;

               contentItem.Apply(part);

               await _contentManager.CreateAsync(contentItem);

               return GetResult(contentItem.ContentItemId, file.FileName);
            }
         }

         return GetResult(string.Empty, "Error");
      }

      private static ContentResult GetResult(string id, string message) {
         var data = string.Format("{{ \"id\":\"{0}\", \"message\":\"{1}\" }}", id, message);
         return new ContentResult {
            Content = data,
            ContentType = "text/json"
         };
      }

   }
}
