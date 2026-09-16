using Autofac;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Transformalize.Configuration;
using Transformalize.Context;
using Transformalize.Contracts;
using Transformalize.Logging;
using Transformalize.Providers.CsvHelper;
using Transformalize.Providers.CsvHelper.Autofac;
using Transformalize.Transforms;
using TransformalizeModule.Services;
using TransformalizeModule.Services.Transforms;
using TransformalizeModule.Services.Writers;

namespace OrchardCore.Transformalize.Tests;

[TestClass]
public class StreamingTests {

   [TestMethod]
   public void CustomSequenceTransformsProvideNativeStreamingImplementations() {
      var transformTypes = typeof(OrchardJintTransform).Assembly
         .GetTypes()
         .Where(type => !type.IsAbstract && typeof(BaseTransform).IsAssignableFrom(type));

      foreach (var type in transformTypes) {
         var sequenceMethod = type.GetMethod(nameof(BaseTransform.Operate), [typeof(IEnumerable<IRow>)]);
         if (sequenceMethod?.DeclaringType != type) {
            continue;
         }

         var streamMethod = type.GetMethod(
            nameof(IOperateStream.OperateStreamAsync),
            [typeof(IAsyncEnumerable<IRow>), typeof(CancellationToken)]);

         Assert.AreEqual(
            type,
            streamMethod?.DeclaringType,
            $"{type.Name} overrides Operate(IEnumerable<IRow>) and would buffer unless it also overrides OperateStreamAsync.");
      }
   }

   [TestMethod]
   public void CustomLogWriterSupportsStreaming() {
      Assert.IsTrue(typeof(IWriteStream).IsAssignableFrom(typeof(LogWriter)));
   }

   [TestMethod]
   public async Task CallerOwnedCsvWriterIsNotSynchronouslyDisposedWithScope() {
      var process = new Process("""
         <cfg name="csv" output="output">
           <connections>
             <add name="input" provider="internal" />
             <add name="output" provider="file" file="ignored.csv" delimiter="," stream="true" />
           </connections>
           <entities>
             <add name="rows" input="input">
               <fields>
                 <add name="id" type="int" />
               </fields>
             </add>
           </entities>
         </cfg>
         """);
      Assert.IsFalse(process.Errors().Any(), string.Join(System.Environment.NewLine, process.Errors()));

      var entity = process.Entities.Single();
      var outputContext = new OutputContext(new PipelineContext(
         new MemoryLogger(LogLevel.Debug), process, entity));
      var responseBody = new AsyncOnlyStream();
      await using var streamWriter = new StreamWriter(responseBody);
      var builder = new ContainerBuilder();
      builder.RegisterModule(new CsvHelperProviderModule(process, streamWriter));
      builder.RegisterInstance(outputContext).Named<OutputContext>(entity.Key);

      var container = builder.Build();
      var scope = container.BeginLifetimeScope();
      var writer = scope.ResolveNamed<IWrite>(entity.Key);

      Assert.IsInstanceOfType<CsvHelperStreamWriter>(writer);
      await scope.DisposeAsync();
      await streamWriter.FlushAsync();
      Assert.IsTrue(responseBody.CanWrite);
      container.Dispose();
   }

   private sealed class AsyncOnlyStream : MemoryStream {
      public override void Flush() =>
         throw new InvalidOperationException("Synchronous operations are disallowed.");

      public override Task FlushAsync(CancellationToken cancellationToken) => Task.CompletedTask;
   }
}
