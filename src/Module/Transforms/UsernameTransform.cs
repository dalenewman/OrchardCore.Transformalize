using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using Transformalize;
using Transformalize.Contracts;
using Transformalize.Transforms;

namespace Module.Transforms {
   public class UsernameTransform : BaseTransform {

      private readonly IHttpContextAccessor _httpContext;

      public UsernameTransform(IHttpContextAccessor httpContext = null, IContext context = null) : base(context, "string") {
         if (IsMissingContext()) {
            return;
         }

         if (IsNotReceiving("string")) {
            return;
         }
         
         if (httpContext == null) {
            Run = false;
            Context.Error("Username transform requires an instance of IHttpContextAccessor");
         } else {
            _httpContext = httpContext;
         }

      }
      public override IRow Operate(IRow row) {
         throw new NotImplementedException();
      }

      public override IEnumerable<IRow> Operate(IEnumerable<IRow> rows) {
         var userName = _httpContext.HttpContext.User?.Identity?.Name ?? "Anonymous";
         foreach (var row in rows) {
            row[Context.Field] = userName;
            yield return row;
         }
      }

      public override IEnumerable<OperationSignature> GetSignatures() {
         yield return new OperationSignature("username");
      }
   }
}
