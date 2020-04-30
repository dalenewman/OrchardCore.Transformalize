using Microsoft.AspNetCore.Http;
using Module.Services.Contracts;
using System;
using System.Collections.Generic;

namespace Module.Services {
   public class ParameterService : IParameterService {

      private readonly HttpRequest _request;

      public ParameterService(IHttpContextAccessor context) {
         _request = context.HttpContext.Request;
      }
      public IDictionary<string, string> GetParameters() {

         var parameters = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
         if (_request != null) {
            if (_request.QueryString != null) {
               foreach (var key in _request.Query.Keys) {
                  parameters[key] = _request.Query[key].ToString();
               }
            }
            if (_request.HasFormContentType) {
               foreach (var key in _request.Form.Keys) {
                  if (!_request.Form[key].ToString().Equals("__requestverificationtoken", StringComparison.OrdinalIgnoreCase)) {
                     parameters[key] = _request.Form[key];
                  }
               }
            }
         }

         return parameters;
      }

   }
}
