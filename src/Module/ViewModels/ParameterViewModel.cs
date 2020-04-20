using System.Collections.Generic;
using Transformalize.Configuration;

namespace Module.ViewModels {
    public class ParameterViewModel {
        public IEnumerable<Map> Maps { get; set; }
        public Parameter Parameter { get; set; }
    }
}
