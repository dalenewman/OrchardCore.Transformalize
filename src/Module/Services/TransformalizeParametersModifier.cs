﻿#region license
// Transformalize
// Configurable Extract, Transform, and Load
// Copyright 2013-2017 Dale Newman
//  
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//   
//       http://www.apache.org/licenses/LICENSE-2.0
//   
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
#endregion

using Autofac;
using Cfg.Net.Contracts;
using Fluid;
using Microsoft.AspNetCore.Http;
using Module.Models;
using Module.Services.Contracts;
using Module.Services.Modifiers;
using Module.Services.Modules;
using OrchardCore.Users.Services;
using System.Collections.Generic;
using System.Linq;
using Transformalize.Configuration;
using Transformalize.Containers.Autofac.Modules;
using Transformalize.Contracts;
using Transformalize.Impl;
using Process = Transformalize.Configuration.Process;

namespace Module.Services {

   public class TransformalizeParametersModifier : ITransformalizeParametersModifier {

      private const string _tpInput = "tp-input";
      private const string _tpOutput = "tp-output";
      private readonly ISettingsService _settings;
      private readonly CombinedLogger<TransferParameterModifier> _logger;
      private readonly IUserService _userService;
      private readonly IHttpContextAccessor _httpContext;
      private readonly IParameterService _parametersService;
      private readonly IContainer _container;

      public ISerializer Serializer { get; set; }

      public TransformalizeParametersModifier(
         CombinedLogger<TransferParameterModifier> logger,
         ISettingsService settings,
         IHttpContextAccessor httpContext,
         IUserService userService,
         IParameterService parametersService,
         IContainer container
      ) {
         _logger = logger;
         _settings = settings;
         _userService = userService;
         _httpContext = httpContext;
         _container = container;
         _parametersService = parametersService;
      }

      public ArrangementModifierResponse Modify(string cfg) {

         var response = new ArrangementModifierResponse {
            Arrangement = cfg,
            Parameters = _parametersService.GetParameters()
         };

         // using facade (which is all string properties) so things can be 
         // transformed before type checked or place-holders are replaced

         var builder = new ContainerBuilder();
         builder.RegisterModule(new ShorthandModule(_logger, _httpContext, _userService));

         //var facade = new Transformalize.ConfigurationFacade.Process(
         //   cfg,
         //   parameters: response.Parameters,
         //   dependencies: new List<IDependency> {
         //      // transfer external parameters to internal parameters
         //      new TransferParameterModifier("parameters", "name", "value"),
         //   }.ToArray()
         //);

         Transformalize.ConfigurationFacade.Process facade;

         using (var scope = builder.Build().BeginLifetimeScope()) {
            facade = new Transformalize.ConfigurationFacade.Process(
               cfg,
               parameters: response.Parameters,
               dependencies: new List<IDependency> {
                  new TransferParameterModifier("parameters", "name", "value"),
                  scope.ResolveNamed<IDependency>(TransformModule.ParametersName),
                  scope.ResolveNamed<IDependency>(ValidateModule.ParametersName)
               }.ToArray()
            );
         }

         _settings.ApplyCommonSettings(facade);

         var fields = new List<Field>();

         foreach (var pr in facade.Parameters) {
            var field = new Field {
               Name = pr.Name,
               Alias = pr.Name,
               Default = pr.Value,
               Label = pr.Label,
               PostBack = pr.PostBack,
               Transforms = pr.Transforms.Select(o => o.ToOperation()).ToList(),
               Validators = pr.Validators.Select(o => o.ToOperation()).ToList()
            };
            if (!string.IsNullOrEmpty(pr.Length)) {
               field.Length = pr.Length;
            }
            if (!string.IsNullOrEmpty(pr.Type)) {
               field.Type = pr.Type;
            }
            if (!string.IsNullOrEmpty(pr.Precision) && int.TryParse(pr.Precision, out int precision)) {
               field.Precision = precision;
            }
            if (!string.IsNullOrEmpty(pr.Scale) && int.TryParse(pr.Scale, out int scale)) {
               field.Scale = scale;
            }
            fields.Add(field);
         }

         var validatorFields = new List<Field>();

         foreach (var field in fields.Where(f => f.Validators.Any())) {

            field.ValidField = field.Name + "Valid";
            field.MessageField = field.Name + "Message";

            validatorFields.Add(new Field {
               Name = field.ValidField,
               Alias = field.ValidField,
               Input = false,
               Default = "true",
               Type = "bool"
            });
            validatorFields.Add(new Field {
               Name = field.MessageField,
               Alias = field.MessageField,
               Input = false,
               Default = string.Empty,
               Type = "string",
               Length = "255"
            });
         }

         // create an internal connection for input
         var connections = new List<Transformalize.ConfigurationFacade.Connection> {
            new Transformalize.ConfigurationFacade.Connection() { Name = _tpInput, Provider = "internal" }
         };

         // add existing connections in case maps need to be loaded
         connections.AddRange(facade.Connections);

         //create an internal connection for output
         connections.Add(new Transformalize.ConfigurationFacade.Connection() { Name = _tpOutput, Provider = "internal" });


         var fieldCount = fields.Count;

         // create entity
         var entity = new Entity {
            Name = "Parameters",
            Fields = fields,
            CalculatedFields = validatorFields,
            Input = _tpInput
         };

         // create process, adding everything previous created
         var process = new Process {
            Name = "Transformalize Parameters",
            ReadOnly = true,
            Output = _tpOutput,
            Parameters = facade.Parameters.Select(p => p.ToParameter()).ToList(),
            Entities = new List<Entity> { entity },
            Maps = facade.Maps.Select(m => m.ToMap()).ToList(), // for map transforms
            Scripts = facade.Scripts.Select(m => m.ToScript()).ToList(), // for transforms that use scripts (e.g. js)
            Connections = connections.Select(c => c.ToConnection()).ToList()
         };

         process.Load(); // very important to check after creating, as it runs validation and even modifies!

         if (!process.Errors().Any()) {

            // modification in Load() do not make it out to local variables so overwrite them
            entity = process.Entities.First();
            fields = entity.Fields;
            validatorFields = entity.CalculatedFields;

            CfgRow output;
            _container.GetReaderAlternate = (input, rowFactory) => new ParameterRowReader(input, new DefaultRowReader(input, rowFactory));
            using (var scope = _container.CreateScope(process, _logger)) {
               scope.Resolve<IProcessController>().Execute();
               output = process.Entities[0].Rows.FirstOrDefault();
            }

            for (int i = 0; i < process.Maps.Count; i++) {
               var source = process.Maps[i];
               var target = facade.Maps[i];
               if (source.Items.Any() && !target.Items.Any()) {
                  foreach (var item in source.Items) {
                     target.Items.Add(new Transformalize.ConfigurationFacade.MapItem() {
                        From = item.From.ToString(),
                        To = item.To.ToString(),
                        Parameter = item.Parameter,
                        Value = item.Value
                     });
                  }
                  target.Query = string.Empty;
               }
            }

            if (output != null) {
               foreach (var parameter in facade.Parameters.Where(p => p.Validators.Any())) {
                  var field = fields.First(f => f.Name == parameter.Name);

                  // set the transformed value
                  parameter.Value = output[field.Name].ToString();

                  // set the validation results
                  if ((bool)output[field.ValidField]) {
                     parameter.Valid = "true";
                  } else {
                     parameter.Valid = "false";
                     parameter.Message = ((string)output[field.MessageField]).TrimEnd('|');
                  }

                  parameter.T = null;
                  parameter.Transforms.Clear();
                  parameter.V = null;
                  parameter.Validators.Clear();
               }
            }

            response.Arrangement = facade.Serialize();
            return response;
         }

         foreach (var error in process.Errors()) {
            _logger.Error(() => error);
         }

         return response;
      }


   }

}
