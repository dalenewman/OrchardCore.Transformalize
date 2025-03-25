using Jint;
using Transformalize.Configuration;

namespace TransformalizeModule.Models {
   public class CachedJintTransform {
      public Prepared<Acornima.Ast.Script> Script { get; set; }
      public Field[] Input;
   }

}
