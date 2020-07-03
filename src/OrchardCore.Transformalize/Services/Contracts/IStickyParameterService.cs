using System;
using System.Collections.Generic;
using Transformalize.Configuration;

namespace OrchardCore.TransformalizeModule.Services.Contracts {

    public interface IStickyParameterService {
      public void SetStickyParameters(string contentItemId, List<Parameter> parameters);
      public void GetStickyParameters(string contentItemId, IDictionary<string, string> parameters);
      public T GetStickyParameter<T>(string contentItemId, string name, Func<T> defaultValue) where T : IConvertible;
   }
}
