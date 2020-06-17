using Module.Models;
using System.Collections.Generic;
using Transformalize.Configuration;

namespace Module.Services.Contracts {
   public interface ISettingsService {
      public Dictionary<string, Connection> Connections { get; }
      public Dictionary<string, Map> Maps { get; }
      public TransformalizeSettings Settings { get; }
      public Process Process { get; set; }
      public IEnumerable<int> GetPageSizes(TransformalizeReportPart part);
   }
}
