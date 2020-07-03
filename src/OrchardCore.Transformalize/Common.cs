using System;
using System.Collections.Generic;

namespace OrchardCore.TransformalizeModule {
   public static class Common {

      public const int IdLength = 26;
      public const string ModuleName = "OrchardCore.Transformalize";
      public const string ReturnUrlName = "ReturnUrl";
      public const string SettingsGroupId = "Transformalize";
      public const string CodeMirrorVersion = "5.52.2";
      public const string InvalidParametersMessage = "Parameter Validation Failed";
      public const string InvalidContentTypeMessage = "Invalid Content Type";
      public static HashSet<string> SystemFields = new HashSet<string>(2, StringComparer.OrdinalIgnoreCase) { "TaskContentItemId", "ReportContentItemId" };

   }
}
