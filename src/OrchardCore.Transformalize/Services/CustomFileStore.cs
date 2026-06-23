using Microsoft.Extensions.Logging;
using OrchardCore.FileStorage.FileSystem;
using TransformalizeModule.Services.Contracts;

namespace TransformalizeModule.Services {

   public class CustomFileStore : FileSystemStore, ICustomFileStore {

      public string Path { get; set; }
      // OrchardCore 3.0.0 added a required ILogger<FileSystemStore> parameter to the base ctor.
      public CustomFileStore(string path, ILogger<FileSystemStore> logger) : base(path, logger) {
         Path = path;
      }

   }

}
