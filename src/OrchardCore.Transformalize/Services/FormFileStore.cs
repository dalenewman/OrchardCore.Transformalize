using OrchardCore.FileStorage.FileSystem;
using TransformalizeModule.Services.Contracts;

namespace TransformalizeModule.Services {
   public class FormFileStore : FileSystemStore, IFormFileStore {
      public FormFileStore(string fileSystemPath) : base(fileSystemPath) {
      }
   }
}
