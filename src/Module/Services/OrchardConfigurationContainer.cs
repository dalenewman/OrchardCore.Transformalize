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

      public ISerializer Serializer { get; set; }

      public OrchardConfigurationContainer(
                  IHttpContextAccessor httpContext,
         IUserService userService) {
         _userService = userService;
         _httpContext = httpContext;
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
         var tm = new TransformModule(new Process { Name = "TransformShorthand" }, _methods, _shortHand, logger);
         // adding additional transforms here
         tm.AddTransform(new TransformHolder((c) => new UsernameTransform(_httpContext, c), new UsernameTransform().GetSignatures()));
         tm.AddTransform(new TransformHolder((c) => new UserIdTransform(_httpContext, _userService, c), new UserIdTransform().GetSignatures()));
         tm.AddTransform(new TransformHolder((c) => new UserEmailTransform(_httpContext, _userService, c), new UserEmailTransform().GetSignatures()));
         builder.RegisterModule(tm);

         // register short-hand for v attribute
         var vm = new ValidateModule(new Process { Name = "ValidateShorthand" }, _methods, _shortHand, logger);
         // adding additional validators here
         builder.RegisterModule(vm);

         // register the validator short hand
         builder.Register((c, p) => _shortHand).Named<ShorthandRoot>(ValidateModule.Name).InstancePerLifetimeScope();
         builder.Register((c, p) => new ShorthandCustomizer(c.ResolveNamed<ShorthandRoot>(ValidateModule.Name), new[] { "fields", "calculated-fields", "calculatedfields" }, "v", "validators", "method")).Named<IDependency>(ValidateModule.Name).InstancePerLifetimeScope();

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

            var transformed = TransformParameters(ctx, cfg);

            var dependancies = new List<IDependency>();
            dependancies.Add(ctx.Resolve<IReader>());
            dependancies.Add(new ReportParameterModifier());
            if (Serializer != null) {
               dependancies.Add(Serializer);
            }
            dependancies.Add(new ParameterModifier(new PlaceHolderReplacer(placeHolderStyle[0], placeHolderStyle[1], placeHolderStyle[2]), "parameters", "name", "value"));
            dependancies.Add(ctx.ResolveNamed<IDependency>(TransformModule.FieldsName));
            dependancies.Add(ctx.ResolveNamed<IDependency>(TransformModule.ParametersName));
            dependancies.Add(ctx.ResolveNamed<IDependency>(ValidateModule.Name));

            var process = new Process(transformed ?? cfg, parameters, dependancies.ToArray());

            if (process.Errors().Any()) {
               var c = new PipelineContext(logger, new Process() { Name = "Errors" });
               c.Error("The configuration has errors.");
               foreach (var error in process.Errors()) {
                  c.Error(error);
               }
            }

            return process;
         }).As<Process>().InstancePerDependency();
         return builder.Build().BeginLifetimeScope();
      }

      private static string TransformParameters(IComponentContext ctx, string cfg) {

         var preProcess = new Transformalize.ConfigurationFacade.Process(
            cfg,
            new Dictionary<string, string>(),
            new List<IDependency> {
               ctx.Resolve<IReader>(),
               new DateMathModifier(),
               new ParameterModifier(new NullPlaceHolderReplacer()),
               ctx.ResolveNamed<IDependency>(TransformModule.ParametersName)
         }.ToArray());

         if (!preProcess.Parameters.Any(pr => pr.Transforms.Any()))
            return null;

         var fields = preProcess.Parameters.Select(pr => new Field {
            Name = pr.Name,
            Alias = pr.Name,
            Default = pr.Value,
            Type = pr.Type,
            Transforms = pr.Transforms.Select(o => o.ToOperation()).ToList()
         }).ToList();
         var len = fields.Count;
         var entity = new Entity { Name = "Parameters", Alias = "Parameters", Fields = fields };
         var mini = new Process {
            Name = "ParameterTransform",
            ReadOnly = true,
            Entities = new List<Entity> { entity },
            Maps = preProcess.Maps.Select(m => m.ToMap()).ToList(), // for map transforms
            Scripts = preProcess.Scripts.Select(s => s.ToScript()).ToList() // for transforms that use scripts (e.g. js)
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

            // make an input out of the parameters
            var input = new List<IRow>();
            var row = new Transformalize.MasterRow(len);
            for (var i = 0; i < len; i++) {
               row[fields[i]] = preProcess.Parameters[i].Value;
            }

            input.Add(row);

            var output = transforms.Aggregate(input.AsEnumerable(), (rows, t) => t.Operate(rows)).ToList().First();

            for (var i = 0; i < len; i++) {
               var parameter = preProcess.Parameters[i];
               parameter.Value = output[fields[i]].ToString();
               parameter.T = string.Empty;
               parameter.Transforms.Clear();
            }

            return preProcess.Serialize();
         }

         var context = new PipelineContext(ctx.Resolve<IPipelineLogger>(), mini, entity);
         foreach (var error in mini.Errors()) {
            context.Error(error);
         }

         return null;
      }

   }

}
