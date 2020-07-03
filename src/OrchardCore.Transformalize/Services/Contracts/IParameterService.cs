using System.Collections.Generic;

namespace OrchardCore.TransformalizeModule.Services.Contracts {
   public interface IParameterService {
      public IDictionary<string, string> GetParameters();
   }
}
