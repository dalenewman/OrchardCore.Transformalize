using Microsoft.AspNetCore.WebUtilities;

public static class QueryHelpersExt {
   public static string RemoveQueryString(string url, HashSet<string> keys) {
      var uri = new Uri(url);
      var query = QueryHelpers.ParseQuery(uri.Query);
      var queryParams = new Dictionary<string, string>(query.Count);

      foreach (var param in query) {
         if (!keys.Contains(param.Key)) {
            queryParams[param.Key] = param.Value;
         }
      }

      var uriBuilder = new UriBuilder(uri) {
         Query = QueryHelpers.AddQueryString(string.Empty, queryParams).TrimStart('?')
      };

      return uriBuilder.ToString();
   }
}
