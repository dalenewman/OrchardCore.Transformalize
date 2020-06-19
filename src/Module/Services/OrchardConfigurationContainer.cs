#region license
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
using Cfg.Net.Environment;
using Cfg.Net.Reader;
using Cfg.Net.Shorthand;
using Microsoft.AspNetCore.Http;
using Module.Services.Contracts;
using Module.Services.Modifiers;
using Module.Transforms;
using OrchardCore.Users.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using Transformalize.Configuration;
using Transformalize.Containers.Autofac;
using Transformalize.Containers.Autofac.Modules;
using Transformalize.Context;
using Transformalize.Contracts;
using Transformalize.Impl;
using Transformalize.Providers.File.Autofac;
using Transformalize.Transforms.Humanizer.Autofac;
using Transformalize.Transforms.Jint.Autofac;
using Transformalize.Transforms.Json.Autofac;
using Transformalize.Validate.Jint.Autofac;
using Process = Transformalize.Configuration.Process;

namespace Module.Services {

   /// <summary>
   /// This container deals with the arrangement and how it becomes a process.
   /// Transforms and Validators are registered here as well because their 
   /// short-hand is expanded in the arrangement by customizers before it becomes a process.
   /// </summary>
   public class OrchardConfigurationContainer : IConfigurationContainer {

      private const string _tpInput = "tp-input";
      private const string _tpOutput = "tp-output";
      private readonly HashSet<string> _methods = new HashSet<string>();
      private readonly ShorthandRoot _shortHand = new ShorthandRoot();
      private readonly IUserService _userService;
      private readonly IHttpContextAccessor _httpContext;
      private readonly ISettingsService _settings;
      private readonly CombinedLogger<OrchardConfigurationContainer> _logger;
      private readonly IContainer _container;

      public ISerializer Serializer { get; set; }

      public OrchardConfigurationContainer(
         IHttpContextAccessor httpContext,
         CombinedLogger<OrchardConfigurationContainer> logger,
         ISettingsService settings,
         IUserService userService,
         IContainer container
      ) {
         _userService = userService;
         _httpContext = httpContext;
         _logger = logger;
         _settings = settings;
         _container = container;
      }

      public ILifetimeScope CreateScope(string cfg, IPipelineLogger logger, IDictionary<string, string> parameters = null) {

         var placeHolderStyle = "@[]";

         if (parameters == null) {
            parameters = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
         }

         var builder = new ContainerBuilder();

         builder.Register((ctx) => logger).As<IPipelineLogger>();

         builder.Register<IReader>(c => new DefaultReader(new FileReader(), new WebReader())).As<IReader>();

         // register short-hand for t attribute
         var tm = new TransformModule(new Process { Name = "TransformShorthand" }, _methods, _shortHand, logger) { Plugins = false };
         // adding additional transforms here
         tm.AddTransform(new TransformHolder((c) => new UsernameTransform(_httpContext, c), new UsernameTransform().GetSignatures()));
         tm.AddTransform(new TransformHolder((c) => new UserIdTransform(_httpContext, _userService, c), new UserIdTransform().GetSignatures()));
         tm.AddTransform(new TransformHolder((c) => new UserEmailTransform(_httpContext, _userService, c), new UserEmailTransform().GetSignatures()));
         builder.RegisterModule(tm);

         // register short-hand for v attribute
         var vm = new ValidateModule(new Process { Name = "ValidateShorthand" }, _methods, _shortHand, logger) { Plugins = false };
         // adding additional validators here
         builder.RegisterModule(vm);

         // register the validator short hand
         builder.Register((c, p) => _shortHand).Named<ShorthandRoot>(ValidateModule.FieldsName).InstancePerLifetimeScope();
         builder.Register((c, p) => _shortHand).Named<ShorthandRoot>(ValidateModule.ParametersName).InstancePerLifetimeScope();
         builder.Register((c, p) => new ShorthandCustomizer(c.ResolveNamed<ShorthandRoot>(ValidateModule.FieldsName), new[] { "fields", "calculated-fields", "calculatedfields" }, "v", "validators", "method")).Named<IDependency>(ValidateModule.FieldsName).InstancePerLifetimeScope();
         builder.Register((c, p) => new ShorthandCustomizer(c.ResolveNamed<ShorthandRoot>(ValidateModule.ParametersName), new[] { "parameters" }, "v", "validators", "method")).Named<IDependency>(ValidateModule.ParametersName).InstancePerLifetimeScope();

         // register the transform short hand
         builder.Register((c, p) => _shortHand).Named<ShorthandRoot>(TransformModule.FieldsName).InstancePerLifetimeScope();
         builder.Register((c, p) => _shortHand).Named<ShorthandRoot>(TransformModule.ParametersName).InstancePerLifetimeScope();
         builder.Register((c, p) => new ShorthandCustomizer(c.ResolveNamed<ShorthandRoot>(TransformModule.FieldsName), new[] { "fields", "calculated-fields", "calculatedfields" }, "t", "transforms", "method")).Named<IDependency>(TransformModule.FieldsName).InstancePerLifetimeScope();
         builder.Register((c, p) => new ShorthandCustomizer(c.ResolveNamed<ShorthandRoot>(TransformModule.ParametersName), new[] { "parameters" }, "t", "transforms", "method")).Named<IDependency>(TransformModule.ParametersName).InstancePerLifetimeScope();

         // the shorthand registrations are stored in the builder's properties for plugins to add to
         builder.Properties["ShortHand"] = _shortHand;
         builder.Properties["Methods"] = _methods;

         // register transform modules here so they can add their shorthand
         builder.RegisterModule(new JintTransformModule());
         builder.RegisterModule(new JsonTransformModule());
         builder.RegisterModule(new HumanizeModule());
         builder.RegisterModule(new FileModule());

         // register validator modules here so they can register their short-hand
         builder.RegisterModule(new JintValidateModule());

         builder.Register((c, p) => _methods).Named<HashSet<string>>("Methods").InstancePerLifetimeScope();
         builder.Register((c, p) => _shortHand).As<ShorthandRoot>().InstancePerLifetimeScope();

         builder.Register(ctx => {

            // parameter values are only used if outside parameters don't exist so
            // outside parameters will over-write or set the value before transforms / validators run
            // if outside parameters are used here, and possibly transformed, they will move on to the
            // real process as the transformed value

            // THE REASON WHY YOU NEED FACADE IS SO PLACE-HOLDERS (always strings like @[Seed]) AREN'T LOST IN OTHER TYPES OF FIELDS (i.e. ints).
            var pre = new Transformalize.ConfigurationFacade.Process(
               cfg,
               //parameters: null,
               parameters: parameters,
               dependencies: new List<IDependency> {
                  // added parameter modifier
                  new ParameterModifier(new NullPlaceHolderReplacer(), "parameters", "name", "value"),
                  ctx.Resolve<IReader>(),
                  ctx.ResolveNamed<IDependency>(TransformModule.ParametersName),
                  ctx.ResolveNamed<IDependency>(ValidateModule.ParametersName)
               }.ToArray()
            );

            if (pre.Parameters.Any(pr => pr.Transforms.Any() || pr.Validators.Any())) {
               _settings.ApplyCommonSettings(pre);
               cfg = TransformalizeParameters(ctx, pre, parameters);
            }

            var dependancies = new List<IDependency>();
            dependancies.Add(ctx.Resolve<IReader>());
            dependancies.Add(new ReportParameterModifier());
            if (Serializer != null) {
               dependancies.Add(Serializer);
            }
            dependancies.Add(new ParameterModifier(new PlaceHolderReplacer(placeHolderStyle[0], placeHolderStyle[1], placeHolderStyle[2]), "parameters", "name", "value"));
            dependancies.Add(ctx.ResolveNamed<IDependency>(TransformModule.FieldsName));
            dependancies.Add(ctx.ResolveNamed<IDependency>(TransformModule.ParametersName));
            dependancies.Add(ctx.ResolveNamed<IDependency>(ValidateModule.FieldsName));
            dependancies.Add(ctx.ResolveNamed<IDependency>(ValidateModule.ParametersName));

            var process = new Process(cfg, parameters, dependancies.ToArray());

            if (process.Errors().Any()) {
               _logger.Error(() => "The configuration has errors.");
               foreach (var error in process.Errors()) {
                  _logger.Error(() => error);
               }
            }

            return process;
         }).As<Process>().InstancePerDependency();
         return builder.Build().BeginLifetimeScope();
      }

      private string TransformalizeParameters(IComponentContext ctx, Transformalize.ConfigurationFacade.Process process, IDictionary<string, string> parameters) {

         var fields = new List<Field>();

         foreach (var pr in process.Parameters) {
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

         if (fields.Any(f => f.Validators.Any())) {

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

            fields.AddRange(validatorFields);
         }

         // sandwich connections with internal input and output
         var connections = new List<Transformalize.ConfigurationFacade.Connection>();
         connections.Add(new Transformalize.ConfigurationFacade.Connection() { Name = _tpInput, Provider = "internal" });
         connections.AddRange(process.Connections);
         connections.Add(new Transformalize.ConfigurationFacade.Connection() { Name = _tpOutput, Provider = "internal" });

         var fieldCount = fields.Count;
         var entity = new Entity { Name = "Parameters", Alias = "Parameters", Fields = fields, Input = _tpInput };
         var mini = new Process {
            Name = "TransformalizeParameters",
            ReadOnly = true,
            Output = _tpOutput,
            Parameters = process.Parameters.Select(p=>p.ToParameter()).ToList(),
            Entities = new List<Entity> { entity },
            Maps = process.Maps.Select(m=>m.ToMap()).ToList() , // for map transforms
            Scripts = process.Scripts.Select(m=>m.ToScript()).ToList() , // for transforms that use scripts (e.g. js)
            Connections = connections.Select(c=>c.ToConnection()).ToList()
         };

         mini.Load(); // very important to check after creating, as it runs validation and even modifies!

         if (!mini.Errors().Any()) {

            // modification in Load() do not make it out to local variables so overwrite them
            fields = mini.Entities.First().Fields;
            entity = mini.Entities.First();

            CfgRow output;
            _container.ParametersForInternalReader = parameters;
            using (var scope = _container.CreateScope(mini, _logger)) {
               scope.Resolve<IProcessController>().Execute();
               output = mini.Entities[0].Rows.FirstOrDefault();
            }
            _container.ParametersForInternalReader = null;

            for (int i = 0; i < mini.Maps.Count; i++) {
               var source = mini.Maps[i];
               var target = process.Maps[i];
               if(source.Items.Any() && !target.Items.Any()) {
                  foreach(var item in source.Items) {
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

            if(output != null) {
               for (var i = 0; i < process.Parameters.Count; i++) {

                  var field = fields[i];
                  var parameter = process.Parameters[i];

                  // set the transformed value
                  parameter.Value = output[field.Name].ToString();

                  // set the validation results
                  if (field.ValidField == string.Empty) {
                     parameter.Valid = "true";
                  } else {
                     if ((bool)output[field.ValidField]) {
                        parameter.Valid = "true";
                     } else {
                        parameter.Valid = "false";
                        parameter.Message = ((string)output[field.MessageField]).TrimEnd('|');
                     }
                  }

                  parameter.Transforms.Clear();
                  parameter.Validators.Clear();
               }

            }

            return process.Serialize();
         }

         var context = new PipelineContext(ctx.Resolve<IPipelineLogger>(), mini, entity);
         foreach (var error in mini.Errors()) {
            context.Error(error);
         }

         return null;
      }

   }

}
