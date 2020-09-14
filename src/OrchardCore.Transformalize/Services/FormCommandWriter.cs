using System.Collections.Generic;
using System.Linq;
using Transformalize.Configuration;
using Transformalize.Contracts;
using Transformalize.Providers.Ado;

namespace TransformalizeModule.Services {

   public class AdoFormCommandWriter {

      private readonly IConnectionContext _context;
      private readonly IConnectionFactory _factory;

      public AdoFormCommandWriter(IConnectionContext context, IConnectionFactory factory) {
         _context = context;
         _factory = factory;
      }

      public AdoFormCommands Write() {

         if (_context.Connection.Table == "[default]") {
            return new AdoFormCommands();
         }

         return new AdoFormCommands {
            Create = SqlCreate(),
            Insert = SqlInsert(),
            Update = SqlUpdate()
         };
      }

      /// <summary>
      /// this assumes sql server is the provider (currently)
      /// </summary>
      /// <param name="c"></param>
      /// <param name="cf"></param>
      /// <returns></returns>
      private string SqlCreate() {
         var definitions = new List<string>();

         foreach (var parameter in _context.Process.Parameters.Where(p => !string.IsNullOrEmpty(p.Name) && p.Output)) {
            if (parameter.PrimaryKey) {
               definitions.Add(_factory.Enclose(parameter.Name) + " INT NOT NULL PRIMARY KEY IDENTITY(1,1)"); // for now
            } else {
               var field = new Field { Name = parameter.Name, Alias = parameter.Name, Type = parameter.Type, Precision = parameter.Precision, Scale = parameter.Scale, Unicode = parameter.Unicode, VariableLength = parameter.VariableLength };
               definitions.Add(_factory.Enclose(parameter.Name) + " " + _factory.SqlDataType(field) + " NOT NULL");
            }
         }

         return $"CREATE TABLE {_factory.Enclose(_context.Connection.Table)} ({string.Join(",", definitions)}){_factory.Terminator}";
      }

      /// <summary>
      /// this assumes primary key is automatically generated (e.g. SQL Server Identity)
      /// </summary>
      /// <param name="c"></param>
      /// <param name="cf"></param>
      /// <returns></returns>
      private string SqlInsert() {
         var fields = _context.Process.Parameters.Where(p => !string.IsNullOrEmpty(p.Name) && p.Output && !p.PrimaryKey && p.Scope != "update").ToList();
         var fieldNames = string.Join(",", fields.Select(f => _factory.Enclose(f.Name)));
         var parameters = _factory.AdoProvider == AdoProvider.Access ? string.Join(",", fields.Select(f => "?")) : string.Join(",", fields.Select(f => "@" + f.Name));
         return $"INSERT INTO {_factory.Enclose(_context.Connection.Table)}({fieldNames}) VALUES({parameters}){_factory.Terminator}";
      }

      private string SqlUpdate() {

         var fields = _context.Process.Parameters.Where(p => !string.IsNullOrEmpty(p.Name) && p.Output && p.Scope != "insert").ToList();

         var sets = fields.Where(p => !p.PrimaryKey && p.InputType != "file").Select(f => $"{_factory.Enclose(f.Name)} = {GetParameter(f)}");
         var fileSets = fields.Where(p => !p.PrimaryKey && p.InputType == "file").Select(f => $"{_factory.Enclose(f.Name)} = CASE WHEN {GetParameter(f)} = '' THEN {_factory.Enclose(f.Name)} ELSE {GetParameter(f)} END"); // for now
         var combinedSets = string.Join(",", sets.Union(fileSets));

         var criteria = string.Join(" AND ", fields.Where(f => f.PrimaryKey).OrderBy(f => f.Sequence).Select(f => $"{_factory.Enclose(f.Name)} = {GetParameter(f)}"));
         return $"UPDATE {_factory.Enclose(_context.Connection.Table)} SET {combinedSets} WHERE {criteria}{_factory.Terminator}";
      }

      private string GetParameter(Parameter p) {
         return _factory.AdoProvider == AdoProvider.Access ? "?" : "@" + p.Name;
      }

   }
}
