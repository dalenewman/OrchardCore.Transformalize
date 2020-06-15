using Autofac;
using Module.Services.Contracts;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Transformalize.Configuration;
using Transformalize.Contracts;

namespace Module.Services {
   public class ArrangementSchemaService : IArrangementSchemaService {

      private readonly IContainer _container;
      private readonly CombinedLogger<ArrangementSchemaService> _logger;

      public ArrangementSchemaService(
         IContainer container,
         CombinedLogger<ArrangementSchemaService> logger
      ) {
         _container = container;
         _logger = logger;
      }

      public async Task<Process> GetSchemaAsync(Process process) {

         //TODO: make getting a schema asynchronous

         var scope = _container.CreateScope(process, _logger);
         var connectionReaders = new Dictionary<string, ISchemaReader>();
         var entityReaders = new Dictionary<Entity, ISchemaReader>();

         foreach (var connection in process.Connections) {
            if (process.Entities.Any(e => e.Connection == connection.Name) && !connectionReaders.ContainsKey(connection.Name)) {
               connectionReaders.Add(connection.Name, scope.ResolveNamed<ISchemaReader>(connection.Key));
            }
         }
         foreach (var entity in process.Entities.Where(e => connectionReaders.ContainsKey(e.Connection))) {
            entityReaders.Add(entity, connectionReaders[entity.Connection]);
         }

         var schemas = new List<Schema>();

         foreach (var reader in entityReaders) {
            schemas.Add(await Task.Run(() => reader.Value.Read(reader.Key)));
         }
         foreach (var schema in schemas) {
            foreach (var schemaEntity in schema.Entities) {
               var entity = process.Entities.FirstOrDefault(e => e.Name == schemaEntity.Name);
               if (entity != null) {
                  foreach (var schemaField in schemaEntity.Fields) {
                     if (!entity.Fields.Any(f => f.Name == schemaField.Name)) {
                        entity.Fields.Add(schemaField);
                     }
                  }
               }
            }
         }

         // remove some of the cfg-net transformations that are not meant to be seen
         foreach (var entity in process.Entities) {
            
            entity.Fields.RemoveAll(f => f.System);

            if(entity.Alias == entity.Name) {
               entity.Alias = null;
            }

            foreach(var field in entity.Fields) {
               if (field.Alias == field.Name) {
                  field.Alias = null;
               }
               if (field.Label == field.Name) {
                  field.Label = null;
               }
               if(field.SortField == field.Name) {
                  field.SortField = null;
               }
               field.Sortable = null;
            }
         }

         return process;
      }
   }
}
