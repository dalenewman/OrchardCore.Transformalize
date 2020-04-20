using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Localization;
using Microsoft.Extensions.Localization;
using OrchardCore.DisplayManagement.Notify;
using System.ComponentModel;
using Transformalize.Configuration;

namespace Module.Controllers {
   public class BaseController : Controller {
      public readonly IStringLocalizer<BaseController> S;
      public readonly IHtmlLocalizer<BaseController> H;

      public BaseController(
          IStringLocalizer<BaseController> stringLocalizer,
          IHtmlLocalizer<BaseController> htmlLocalizer
          ) {
         S = stringLocalizer;
         H = htmlLocalizer;
      }

      public bool IsMissingRequiredParameters(List<global::Transformalize.Configuration.Parameter> parameters, INotifier notifier) {

         var hasRequiredParameters = true;
         foreach (var parameter in parameters.Where(p => p.Required)) {

            var value = Request.Query[parameter.Name].ToString();
            if (value != null && value != "*") {
               continue;
            }

            if (parameter.Sticky && parameter.Value != "*") {
               continue;
            }

            notifier.Add(NotifyType.Warning, H["{0} is required. To continue, please choose a {0}.", parameter.Label]);
            if (hasRequiredParameters) {
               hasRequiredParameters = false;
            }
         }

         return !hasRequiredParameters;
      }

      public void SetStickyParameters(string contentItemId, List<global::Transformalize.Configuration.Parameter> parameters) {
         foreach (var parameter in parameters.Where(p => p.Sticky)) {
            var key = contentItemId + parameter.Name;
            if (string.IsNullOrEmpty(Request.Query[parameter.Name].ToString())) {
               if (HttpContext.Session.GetString(key) != null) {
                  parameter.Value = HttpContext.Session.GetString(key);
               }
            } else {  // A parameter is set
               var value = Request.Query[parameter.Name].ToString();
               if (HttpContext.Session.GetString(key) == null) {
                  HttpContext.Session.SetString(key, value);  // for the next time
                  parameter.Value = value; // for now
               } else {
                  if (HttpContext.Session.GetString(key) != value) {
                     HttpContext.Session.SetString(key, value); // for the next time
                     parameter.Value = value; // for now
                  }
               }
            }
         }
      }

      public void GetStickyParameters(string contentItemId, IDictionary<string, string> parameters) {
         var prefix = contentItemId;
         foreach (string key in HttpContext.Session.Keys) {
            if (key.StartsWith(prefix)) {
               var name = key.Substring(prefix.Length);
               if (!parameters.ContainsKey(name) && HttpContext.Session.GetString(key) != null) {
                  parameters[name] = HttpContext.Session.GetString(key);
               }
            }
         }
      }

      public IDictionary<string, string> GetParameters() {

         var parameters = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
         if (Request != null) {
            if (Request.QueryString != null) {
               foreach (var key in Request.Query.Keys) {
                  parameters[key] = Request.Query[key].ToString();
               }
            }
            if (Request.HasFormContentType) {
               foreach (var key in Request.Form.Keys) {
                  if (!Request.Form[key].ToString().Equals("__requestverificationtoken", StringComparison.OrdinalIgnoreCase)) {
                     parameters[key] = Request.Form[key];
                  }
               }
            }
         }

         return parameters;
      }

      public static void SetPageSize(Process process, IDictionary<string, string> parameters, int min, int stickySize, int max) {

         foreach (var entity in process.Entities) {
            if (entity.Page <= 0) {
               continue;  // This entity isn't intended to be paged
            }

            // parse out a page number
            int page;
            if (parameters.ContainsKey("page")) {
               if (!int.TryParse(parameters["page"], out page)) {
                  page = 1;
               }
            } else {
               page = 1;
            }

            entity.Page = page;

            var size = stickySize;
            if (parameters.ContainsKey("size")) {
               int.TryParse(parameters["size"], out size);
            }

            if (size == 0 && min > 0) {
               size = min;
            }
            entity.Size = max > 0 && size > max ? max : size;

         }

      }

      public T GetStickyParameter<T>(string contentItemId, string name, Func<T> defaultValue) where T : IConvertible {

         var tc = TypeDescriptor.GetConverter(typeof(T));

         var key = contentItemId + name;

         if (Request.Query[name].ToString() != null) {
            try {
               var queryValue = (T)tc.ConvertFromString(Request.Query[name].ToString());
               if (queryValue != null) {
                  if (!queryValue.Equals((T)tc.ConvertFromString(HttpContext.Session.GetString(key)))) {
                     HttpContext.Session.SetString(key, tc.ConvertToString(queryValue));
                  }

                  return queryValue;
               }
            } catch (Exception) {
               // Logger.Error(exception.Message);
            }
         }

         if (Request.HasFormContentType && Request.Form[name].ToString() != null) {
            try {
               var formValue = (T)tc.ConvertFromString(Request.Form[name]);
               if (formValue != null) {
                  if (!formValue.Equals(tc.ConvertFromString(HttpContext.Session.GetString(key)))) {
                     HttpContext.Session.SetString(key, tc.ConvertToString(formValue));
                  }

                  return formValue;
               }
            } catch (Exception) {
               // Logger.Error(exception.Message);
            }
         }

         if (HttpContext.Session.GetString(key) != null) {
            return (T)tc.ConvertFromString(HttpContext.Session.GetString(key));
         }

         var value = defaultValue();
         HttpContext.Session.SetString(key, value.ToString());
         return value;

      }




   }
}
