using System.Collections.Generic;

namespace Module.Services.Contracts {
   public interface IParameterService {
      public IDictionary<string, string> GetParameters();
   }
}
