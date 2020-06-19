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

      private readonly HashSet<string> _methods = new HashSet<string>();
      private readonly ShorthandRoot _shortHand = new ShorthandRoot();
      private readonly IUserService _userService;
      private readonly IHttpContextAccessor _httpContext;
      private readonly ISettingsService _settings;
      private readonly CombinedLogger<OrchardConfigurationContainer> _logger;

      public ISerializer Serializer { get; set; }

      public OrchardConfigurationContainer(
         IHttpContextAccessor httpContext,
         CombinedLogger<OrchardConfigurationContainer> logger,
         ISettingsService settings,
         IUserService userService) 
      {
         _userService = userService;
         _httpContext = httpContext;
         _logger = logger;
         _settings = settings;
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
         var tm = new TransformModule(new Process { Name = "TransformShorthand" }, _methods, _shortHand, logger) { Plugins = false};
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

            var pre = new Process(
               cfg,
               parameters: null,
               enabled: false,
               dependencies: new List<IDependency> {
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
               _logger.Error(()=>"The configuration has errors.");
               foreach (var error in process.Errors()) {
                  _logger.Error(() => error);
               }
            }

            return process;
         }).As<Process>().InstancePerDependency();
         return builder.Build().BeginLifetimeScope();
      }

      private string TransformalizeParameters(IComponentContext ctx, Process process, IDictionary<string, string> parameters) {

         var fields = new List<Field>();

         foreach (var pr in process.Parameters) {
            var field = new Field {
               Name = pr.Name,
               Alias = pr.Name,
               Default = pr.Value,
               Label = pr.Label,
               PostBack = pr.PostBack,
               Transforms = pr.Transforms,
               Validators = pr.Validators,
               Length = pr.Length,
               Type = pr.Type,
               Precision = pr.Precision,
               Scale = pr.Scale
            };
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

         var fieldCount = fields.Count;
         var entity = new Entity { Name = "Parameters", Alias = "Parameters", Fields = fields };
         var mini = new Process {
            Name = "TransformalizeParameters",
            ReadOnly = true,
            Entities = new List<Entity> { entity },
            Maps = process.Maps, // for map transforms
            Scripts = process.Scripts // for transforms that use scripts (e.g. js)
         };

         mini.Load(); // very important to check after creating, as it runs validation and even modifies!

         if (!mini.Errors().Any()) {

            // modification in Load() do not make it out to local variables so overwrite them
            fields = mini.Entities.First().Fields;
            entity = mini.Entities.First();

            var c = new PipelineContext(ctx.Resolve<IPipelineLogger>(), mini, entity);

            var transforms = new List<ITransform> {
               new Transformalize.Transforms.System.DefaultTransform(c, fields)
            };
            transforms.AddRange(TransformFactory.GetTransforms(ctx, c, fields));

            var validators = ValidateFactory.GetValidators(ctx, c, fields);

            // make an input out of the parameters
            var input = new List<IRow>();
            var row = new Transformalize.MasterRow(fieldCount);
            for (var i = 0; i < process.Parameters.Count; i++) {
               var parameter = process.Parameters[i];
               var field = fields[i];
               if (parameters.ContainsKey(parameter.Name)) {
                  row[field] = parameters[parameter.Name];
                  parameters.Remove(parameter.Name); // since parameter may be transformed and placed in value, we discard the original
               } else {
                  row[field] = parameter.Value;
               }
               if (parameter.Type != null && parameter.Type != "string" && row[field] != null) {
                  try {
                     if (row[field] is string str) {
                        row[field] = Transformalize.Constants.ConversionMap[parameter.Type](str);
                     } else {
                        row[field] = Transformalize.Constants.ObjectConversionMap[parameter.Type](row[field]);
                     }
                  } catch (FormatException ex) {
                     _logger.Error(()=>$"Could not convert '{row[field]}' to {parameter.Type}. {ex.Message}");
                     row[field] = Transformalize.Constants.TypeDefaults()[parameter.Type];
                  }
               }
            }
            input.Add(row);

            var output = validators.Aggregate(transforms.Aggregate(input.AsEnumerable(), (rows, t) => t.Operate(rows)), (rows, v) => v.Operate(rows)).ToList().First();

            for (var i = 0; i < process.Parameters.Count; i++) {

               var field = fields[i];
               var parameter = process.Parameters[i];

               // set the transformed value
               parameter.Value = output[field].ToString();

               // set the validation results
               if (field.ValidField != string.Empty) {
                  parameter.Valid = (bool)output[fields.First(f => f.Name == field.ValidField)];
                  parameter.Message = ((string)output[fields.First(f => f.Name == field.MessageField)]).TrimEnd('|');
               } else {
                  parameter.Valid = true;
               }

               parameter.Transforms.Clear();
               parameter.Validators.Clear();
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
