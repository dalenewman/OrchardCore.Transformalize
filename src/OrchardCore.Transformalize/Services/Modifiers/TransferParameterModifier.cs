using System;
using System.Collections.Generic;
using System.Linq;
using Cfg.Net.Contracts;

namespace OrchardCore.TransformalizeModule.Services.Modifiers {

   /// <inheritdoc />
   public class TransferParameterModifier : ICustomizer {

      private class ValueAttribute : IAttribute {
         public ValueAttribute(string value) {
            Name = "value";
            Value = value;
         }

         public string Name { get; set; }
         public object Value { get; set; }
      }

      private readonly string _parametersElementName;
      private readonly string _parameterNameAttribute;
      private readonly string _parameterValueAttribute;

      /// <inheritdoc />
      public TransferParameterModifier(
          string parametersElementName,
          string parameterNameAttribute,
          string parameterValueAttribute
          ) {
         _parametersElementName = parametersElementName;
         _parameterNameAttribute = parameterNameAttribute;
         _parameterValueAttribute = parameterValueAttribute;
      }

      /// <inheritdoc />
      /// <summary>
      /// This gets called for each node, after the parameters have been merged
      /// </summary>
      /// <param name="parent"></param>
      /// <param name="node"></param>
      /// <param name="parameters"></param>
      /// <param name="logger"></param>
      public void Customize(string parent, INode node, IDictionary<string, string> parameters, ILogger logger) {
         return;
      }

      /// <summary>
      /// This gets called first to transfer the external parameters into the internal parameters
      /// </summary>
      /// <param name="root"></param>
      /// <param name="parameters"></param>
      /// <param name="logger"></param>
      public void Customize(INode root, IDictionary<string, string> parameters, ILogger logger) {

         var rootParameters = root.SubNodes.FirstOrDefault(n => n.Name.Equals(_parametersElementName, StringComparison.OrdinalIgnoreCase));
         if (rootParameters == null)
            return;
         if (rootParameters.SubNodes.Count == 0)
            return;

         TransferParameters(rootParameters.SubNodes, parameters);
      }

      private void TransferParameters(IEnumerable<INode> nodes, IDictionary<string, string> parameters) {

         foreach (var parameterNode in nodes) {

            if (parameterNode.TryAttribute(_parameterNameAttribute, out var nameAttribute)) {

               if (nameAttribute.Value != null) {

                  var name = nameAttribute.Value.ToString();
                  if (parameters.ContainsKey(name)) {

                     if (parameterNode.TryAttribute(_parameterValueAttribute, out var valueAttr)) {
                        valueAttr.Value = parameters[name];
                     } else {
                        parameterNode.Attributes.Add(new ValueAttribute(parameters[name]));
                     }

                     parameters.Remove(name);

                  }

               }

            }

         }
      
      }

   }
}