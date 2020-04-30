using Module.Models;
using System.Collections.Generic;
using Transformalize.Configuration;

namespace Module.Services.Contracts {
   public interface ISettingsService {
      public Dictionary<string,Connection> Connections { get; }
      public TransformalizeSettings TransformalizeSettings { get; }
      public Process Process { get; set; }
   }
}
