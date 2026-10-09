using Parameter = Transformalize.Configuration.Parameter;

namespace TransformalizeModule.Ext {

   public static class ParameterExtensions {

      public static bool Readonly(this Parameter p) {
         return !p.Prompt || p.Visible == "false";
      }

      public static bool VisiblePrompt(this Parameter p) {
         return p.Prompt && p.Visible != "false";
      }

      public static string ToParsley(this Parameter f) => string.Join(" ", ValidationAttributes(f)
         .Select(i => $"{i.Key}=\"{System.Text.Encodings.Web.HtmlEncoder.Default.Encode(i.Value)}\""));

      public static string ToValidation(this Parameter f, bool interactive) {
         if (!interactive) return f.ToParsley();
         var rules = ValidationAttributes(f).ToDictionary(i => i.Key["data-parsley-".Length..], i => i.Value);
         if (rules.Count == 0) return string.Empty;
         var json = System.Text.Json.JsonSerializer.Serialize(rules);
         return $"data-tfl-validation=\"{System.Text.Encodings.Web.HtmlEncoder.Default.Encode(json)}\"";
      }

      private static Dictionary<string, string> ValidationAttributes(Parameter f) {
         var attributes = new Dictionary<string, string>();
         if (f.V == string.Empty)
            return attributes;

         var expressions = new Cfg.Net.Shorthand.Expressions(f.V);
         foreach (var expression in expressions) {
            switch (expression.Method) {
               case "required":
                  attributes["data-parsley-required"] = "true";
                  switch (f.InputType) {
                     case "file":
                     case "scan":
                        attributes["data-parsley-required-message"] = "a " + f.InputType + " is required";
                        break;
                     default:
                        break;
                  }
                  break;
               case "length":
                  attributes["data-parsley-length"] = string.Format("[{0}, {1}]", expression.SingleParameter, expression.SingleParameter);
                  break;
               case "numeric":
                  attributes["data-parsley-type"] = "number";
                  break;
               case "matches":
                  attributes["data-parsley-pattern"] = expression.SingleParameter;
                  break;
               case "min":
                  attributes["data-parsley-min"] = expression.SingleParameter;
                  break;
               case "max":
                  attributes["data-parsley-max"] = expression.SingleParameter;
                  break;
               case "is":
                  switch (expression.SingleParameter) {
                     case "int":
                     case "int32":
                        attributes["data-parsley-type"] = "integer";
                        break;
                     case "date":
                     case "datetime":
                        attributes["data-parsley-date"] = "true";
                        break;
                  }
                  break;
               case "alphanum":
                  attributes["data-parsley-type"] = "alphanum";
                  break;
               case "digits":
                  attributes["data-parsley-type"] = "digits";
                  break;
               case "email":
                  attributes["data-parsley-type"] = "email";
                  break;
               case "url":
                  attributes["data-parsley-type"] = "url";
                  break;
            }
         }


         return attributes;
      }

      public static bool UseTextArea(this Parameter parameter, out int length) {
         var useTextArea = parameter.Length == "max";
         length = 4000;
         if (!useTextArea) {
            if (int.TryParse(parameter.Length, out length)) {
               useTextArea = length >= 255;
            }
         }
         if (length == 0) {
            length = 4000;
         }
         return useTextArea;
      }
   }
}
