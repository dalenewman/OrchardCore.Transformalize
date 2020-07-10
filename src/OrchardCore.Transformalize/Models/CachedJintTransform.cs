using Transformalize.Configuration;
using Esprima.Ast;

namespace TransformalizeModule.Models {
   public class CachedJintTransform {
      public Program Program { get; set; }
      public Field[] Input;
   }

}