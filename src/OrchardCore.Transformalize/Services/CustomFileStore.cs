using OrchardCore.FileStorage.FileSystem;
using TransformalizeModule.Services.Contracts;

namespace TransformalizeModule.Services {

   public class CustomFileStore : FileSystemStore, ICustomFileStore {
      public CustomFileStore(string path) : base(path) { }
   }
   
}
