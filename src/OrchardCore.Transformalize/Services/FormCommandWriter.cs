﻿using System.Collections.Generic;
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

         if (_context.Connection.Table == Common.Default) {
            return new AdoFormCommands();
         }

         return new AdoFormCommands {
            Create = SqlCreate(),
            Insert = SqlInsert(),
            Update = SqlUpdate(),
            Select = SqlSelect()
         };
      }

      public static bool ShouldSelect(Parameter p) {
         return !string.IsNullOrEmpty(p.Name) && p.Input && p.Output && p.Scope == Common.Default;
      }
      public string SqlSelect() {

         var fields = _context.Process.Parameters.Where(ShouldSelect).OrderBy(f => f.Sequence).ToList();
         var columns = string.Join(", ", fields.Where(p => !p.PrimaryKey).Select(f => _factory.Enclose(f.Name)));
         var criteria = string.Join(" AND ", fields.Where(f => f.PrimaryKey).Select(f => $"{_factory.Enclose(f.Name)} = {GetParameter(f)}"));
         return $"SELECT {columns} FROM {_factory.Enclose(_context.Connection.Table)} WHERE {criteria}{_factory.Terminator}";
      }

      /// <summary>
      /// this assumes sql server is the provider (currently)
      /// </summary>
      /// <param name="c"></param>
      /// <param name="cf"></param>
      /// <returns></returns>
      private string SqlCreate() {

         var definitions = new List<string>();
         static bool ShouldCreate(Parameter p) {
            return !string.IsNullOrEmpty(p.Name) && p.Input || p.Scope != Common.Default;
         }

         foreach (var parameter in _context.Process.Parameters.Where(ShouldCreate)) {
            if (parameter.PrimaryKey) {
               switch (_factory.AdoProvider) {
                  case AdoProvider.PostgreSql:
                     definitions.Add(_factory.Enclose(parameter.Name) + " SERIAL NOT NULL PRIMARY KEY");
                     break;
                  case AdoProvider.SqLite:
                     definitions.Add(_factory.Enclose(parameter.Name) + " INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT");
                     break;
                  default:
                     definitions.Add(_factory.Enclose(parameter.Name) + " INT NOT NULL PRIMARY KEY IDENTITY(1,1)");
                     break;
               }
            } else {
               var field = new Field { Name = parameter.Name, Alias = parameter.Name, Type = parameter.Type, Precision = parameter.Precision, Scale = parameter.Scale, Unicode = parameter.Unicode, VariableLength = parameter.VariableLength };
               var notNull = parameter.Scope == "update" ? string.Empty : " NOT NULL";
               definitions.Add(_factory.Enclose(parameter.Name) + " " + _factory.SqlDataType(field) + notNull);
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
         static bool ShouldInsert(Parameter p) {
            return !string.IsNullOrEmpty(p.Name) && !p.PrimaryKey && (p.Input || p.Scope == "insert");
         }

         var fields = _context.Process.Parameters.Where(ShouldInsert).ToList();
         var fieldNames = string.Join(",", fields.Select(f => _factory.Enclose(f.Name)));
         var parameters = _factory.AdoProvider == AdoProvider.Access ? string.Join(",", fields.Select(f => "?")) : string.Join(",", fields.Select(f => "@" + f.Name));
         return $"INSERT INTO {_factory.Enclose(_context.Connection.Table)}({fieldNames}) VALUES({parameters}){_factory.Terminator}";
      }

      private string SqlUpdate() {

         static bool ShouldUpdate(Parameter p) {
            return !string.IsNullOrEmpty(p.Name) && (p.Input || p.Scope == "update");
         }

         var fields = _context.Process.Parameters.Where(ShouldUpdate).ToList();

         var sets = fields.Where(p => !p.PrimaryKey && p.InputType != "file").Select(f => $"{_factory.Enclose(f.Name)} = {GetParameter(f)}");
         var fileSets = fields.Where(p => !p.PrimaryKey && p.InputType == "file").Select(f => $"{_factory.Enclose(f.Name)} = CASE {GetParameter(f)} WHEN '' THEN {_factory.Enclose(f.Name)} ELSE {GetParameter(f)} END"); // for now
         var combinedSets = string.Join(",", sets.Union(fileSets));

         var criteria = string.Join(" AND ", fields.Where(f => f.PrimaryKey).OrderBy(f => f.Sequence).Select(f => $"{_factory.Enclose(f.Name)} = {GetParameter(f)}"));
         return $"UPDATE {_factory.Enclose(_context.Connection.Table)} SET {combinedSets} WHERE {criteria}{_factory.Terminator}";

      }

      private string GetParameter(Parameter p) {
         return _factory.AdoProvider == AdoProvider.Access ? "?" : "@" + p.Name;
      }

   }
}
