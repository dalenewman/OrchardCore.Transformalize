using Module.Services.Contracts;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using Transformalize.Configuration;
using System.ComponentModel;
using System.Linq;

namespace Module.Services {

   //todo: refactor this to store in a sticky parameters instance serialized as json
   public class StickyParameterService : IStickyParameterService {
      private readonly HttpContext _context;
      public StickyParameterService(IHttpContextAccessor contextAccessor) {
         _context = contextAccessor.HttpContext;
      }

      public T GetStickyParameter<T>(string contentItemId, string name, Func<T> defaultValue) where T : IConvertible {
         var tc = TypeDescriptor.GetConverter(typeof(T));

         var key = contentItemId + name;

         if (_context.Request.Query[name].ToString() != null) {
            try {
               var queryValue = (T)tc.ConvertFromString(_context.Request.Query[name].ToString());
               if (queryValue != null) {
                  if (!queryValue.Equals((T)tc.ConvertFromString(_context.Session.GetString(key)))) {
                     _context.Session.SetString(key, tc.ConvertToString(queryValue));
                  }

                  return queryValue;
               }
            } catch (Exception) {
               // Logger.Error(exception.Message);
            }
         }

         if (_context.Request.HasFormContentType && _context.Request.Form[name].ToString() != null) {
            try {
               var formValue = (T)tc.ConvertFromString(_context.Request.Form[name]);
               if (formValue != null) {
                  if (!formValue.Equals(tc.ConvertFromString(_context.Session.GetString(key)))) {
                     _context.Session.SetString(key, tc.ConvertToString(formValue));
                  }

                  return formValue;
               }
            } catch (Exception) {
               // Logger.Error(exception.Message);
            }
         }

         if (_context.Session.GetString(key) != null) {
            return (T)tc.ConvertFromString(_context.Session.GetString(key));
         }

         var value = defaultValue();
         _context.Session.SetString(key, value.ToString());
         return value;

      }

      public void GetStickyParameters(string contentItemId, IDictionary<string, string> parameters) {
         var prefix = contentItemId;
         foreach (string key in _context.Session.Keys) {
            if (key.StartsWith(prefix)) {
               var name = key.Substring(prefix.Length);
               if (!parameters.ContainsKey(name) && _context.Session.GetString(key) != null) {
                  parameters[name] = _context.Session.GetString(key);
               }
            }
         }
      }

      public void SetStickyParameters(string contentItemId, List<Parameter> parameters) {
         foreach (var parameter in parameters.Where(p => p.Sticky)) {
            var key = contentItemId + parameter.Name;
            if (string.IsNullOrEmpty(_context.Request.Query[parameter.Name].ToString())) {
               if (_context.Session.GetString(key) != null) {
                  parameter.Value = _context.Session.GetString(key);
               }
            } else {  // A parameter is set
               var value = _context.Request.Query[parameter.Name].ToString();
               if (_context.Session.GetString(key) == null) {
                  _context.Session.SetString(key, value);  // for the next time
                  parameter.Value = value; // for now
               } else {
                  if (_context.Session.GetString(key) != value) {
                     _context.Session.SetString(key, value); // for the next time
                     parameter.Value = value; // for now
                  }
               }
            }
         }
      }
   }
}
