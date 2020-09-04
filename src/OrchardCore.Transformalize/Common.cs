using System;
using System.Collections.Generic;
using System.IO;

namespace TransformalizeModule {
   public static class Common {

      public const int IdLength = 26;
      public const string KeySuffix = ":TFL";
      public const string ModuleName = "OrchardCore.Transformalize";
      public const string ReturnUrlName = "ReturnUrl";
      public const string SettingsGroupId = "Transformalize";
      public const string CodeMirrorVersion = "5.52.2";
      public const string InvalidParametersMessage = "Parameter Validation Failed";
      public const string InvalidContentTypeMessage = "Invalid Content Type";

      public const string TaskReferrer = "TaskReferrer";
      public const string TaskContentItemId = "TaskContentItemId";
      public const string ReportContentItemId = "ReportContentItemId";
      public static HashSet<string> SystemFields = new HashSet<string>(3, StringComparer.OrdinalIgnoreCase) { TaskContentItemId, TaskReferrer, ReportContentItemId };

      public const string BulkActionCreateTaskHint = "Default task to create and return an identifier for a bulk action batch.";
      public const string BulkActionWriteTaskHint = "Default task to write the bulk action' values for a bulk action batch.";
      public const string BulkActionSummaryTaskHint = "Default task to gather review and result summary for a bulk action.";
      public const string BulkActionRunTaskHint = "Default task to indicate the task is running.";
      public const string BulkActionSuccessTaskHint = "Default task to indicate the task succeeded.";
      public const string BulkActionFailTaskHint = "Default task to indicate the task failed.";

      public static string GetCacheKey(int contentItemId) {
         return contentItemId + KeySuffix;
      }

      public static string GetSafeFilePath(string user, string name) {
         var now = DateTime.UtcNow;
         var path = Path.Combine(now.Year.ToString(), now.ToString("MM-MMM").ToUpper(), now.ToString("dd"));
         return Path.Combine(path, string.Format("{0}-{1:yyyy-MM-dd-HH-mm-ss}-{2}", user, now, name));
      }

   }
}
