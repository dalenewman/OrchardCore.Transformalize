using Microsoft.AspNetCore.Http;
using OrchardCore.Users.Models;
using OrchardCore.Users.Services;
using Transformalize;
using Transformalize.Contracts;
using Transformalize.Transforms;

namespace TransformalizeModule.Services.Transforms {
   public class UserPropertiesTransform : BaseTransform {

      private readonly string _userProperties = string.Empty;

      public UserPropertiesTransform(
         IHttpContextAccessor httpContext = null,
         IUserService userService = null,
         IContext context = null
      ) : base(context, "string") {

         if (IsMissingContext()) {
            return;
         }

         if (IsNotReceiving("string")) {
            return;
         }
         
         if (httpContext == null) {
            Run = false;
            Context.Error($"{nameof(UserPropertiesTransform)} requires an instance of IHttpContextAccessor");
         } else {
            var username = httpContext.HttpContext.User?.Identity?.Name ?? "Anonymous";
            if(username != "Anonymous") {
               if (userService.GetUserAsync(username).Result is User user) {
                  _userProperties = user.Properties.ToString();
                  if(Context.Field.Length != "max") {
                     if (int.TryParse(Context.Field.Length, out int len)) { 
                        if (len < _userProperties.Length) {
                           Context.Warn($"{nameof(UserPropertiesTransform)} properties length {_userProperties.Length} is more than your field length {len}.");
                        }
                     }

                  }
               }
            }
         }

      }
      public override IRow Operate(IRow row) {
         row[Context.Field] = _userProperties;
         return row;
      }

      public override IEnumerable<OperationSignature> GetSignatures() {
         yield return new OperationSignature("userproperties");
      }
   }
}
