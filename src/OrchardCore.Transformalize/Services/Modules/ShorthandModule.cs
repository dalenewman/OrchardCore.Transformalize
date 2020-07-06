﻿using System.Collections.Generic;
using Autofac;
using Cfg.Net.Contracts;
using Cfg.Net.Reader;
using Cfg.Net.Shorthand;
using Microsoft.AspNetCore.Http;
using TransformalizeModule.Transforms;
using OrchardCore.Users.Services;
using Transformalize.Configuration;
using Transformalize.Containers.Autofac;
using Transformalize.Containers.Autofac.Modules;
using Transformalize.Contracts;
using Transformalize.Providers.File.Autofac;
using Transformalize.Transforms.Humanizer.Autofac;
using Transformalize.Transforms.Jint.Autofac;
using Transformalize.Transforms.Json.Autofac;
using Transformalize.Validate.Jint.Autofac;
using Transformalize.Transforms.LambdaParser.Autofac;
using Transformalize.Transforms.Razor.Autofac;

namespace TransformalizeModule.Services.Modules {

   public class ShorthandModule : Module {

      private readonly HashSet<string> _methods = new HashSet<string>();
      private readonly ShorthandRoot _shortHand = new ShorthandRoot();
      private readonly IPipelineLogger _logger;
      private readonly IUserService _userService;
      private readonly IHttpContextAccessor _httpContext;

      public ShorthandModule(
         IPipelineLogger logger,
         IHttpContextAccessor httpContext,
         IUserService userService
      ) {
         _logger = logger;
         _userService = userService;
         _httpContext = httpContext;
      }

      protected override void Load(ContainerBuilder builder) {

         builder.Register((ctx) => _logger).As<IPipelineLogger>();

         builder.Register<IReader>(c => new DefaultReader(new FileReader(), new WebReader())).As<IReader>();

         // register short-hand for t attribute
         var tm = new TransformModule(new Process { Name = "Transform Shorthand" }, _methods, _shortHand, _logger) { Plugins = false };
         // adding additional transforms here
         tm.AddTransform(new TransformHolder((c) => new UsernameTransform(_httpContext, c), new UsernameTransform().GetSignatures()));
         tm.AddTransform(new TransformHolder((c) => new UserIdTransform(_httpContext, _userService, c), new UserIdTransform().GetSignatures()));
         tm.AddTransform(new TransformHolder((c) => new UserEmailTransform(_httpContext, _userService, c), new UserEmailTransform().GetSignatures()));
         builder.RegisterModule(tm);

         // register short-hand for v attribute
         var vm = new ValidateModule(new Process { Name = "ValidateShorthand" }, _methods, _shortHand, _logger) { Plugins = false };
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
         builder.RegisterModule(new LambdaParserModule());
         builder.RegisterModule(new RazorTransformModule());

         // register validator modules here so they can register their short-hand
         builder.RegisterModule(new JintValidateModule());

         builder.Register((c, p) => _methods).Named<HashSet<string>>("Methods").InstancePerLifetimeScope();
         builder.Register((c, p) => _shortHand).As<ShorthandRoot>().InstancePerLifetimeScope();
      }
   }
}
