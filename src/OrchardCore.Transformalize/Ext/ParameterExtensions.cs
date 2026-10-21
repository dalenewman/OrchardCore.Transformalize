using Parameter = Transformalize.Configuration.Parameter;

namespace TransformalizeModule.Ext {

   public static class ParameterExtensions {

      public static bool Readonly(this Parameter p) {
         return !p.Prompt || p.Visible == "false";
      }

      public static bool VisiblePrompt(this Parameter p) {
         return p.Prompt && p.Visible != "false";
      }

      public static string ToValidation(this Parameter f) {
         var rules = ValidationAttributes(f);
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
                  attributes["required"] = "true";
                  switch (f.InputType) {
                     case "file":
                     case "scan":
                        attributes["required-message"] = "a " + f.InputType + " is required";
                        break;
                     default:
                        break;
                  }
                  break;
               case "length":
                  attributes["length"] = string.Format("[{0}, {1}]", expression.SingleParameter, expression.SingleParameter);
                  break;
               case "numeric":
                  attributes["type"] = "number";
                  break;
               case "matches":
                  attributes["pattern"] = expression.SingleParameter;
                  break;
               case "min":
                  attributes["min"] = expression.SingleParameter;
                  break;
               case "max":
                  attributes["max"] = expression.SingleParameter;
                  break;
               case "is":
                  switch (expression.SingleParameter) {
                     case "int":
                     case "int32":
                        attributes["type"] = "integer";
                        break;
                     case "date":
                     case "datetime":
                        attributes["date"] = "true";
                        break;
                  }
                  break;
               case "alphanum":
                  attributes["type"] = "alphanum";
                  break;
               case "digits":
                  attributes["type"] = "digits";
                  break;
               case "email":
                  attributes["type"] = "email";
                  break;
               case "url":
                  attributes["type"] = "url";
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
