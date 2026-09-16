#region license
// Transformalize
// Configurable Extract, Transform, and Load
// Copyright 2013-2019 Dale Newman
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
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Transformalize.Configuration;
using Transformalize.Context;
using Transformalize.Contracts;

namespace TransformalizeModule.Services.Writers {
   public class LogWriter : IWrite, IWriteStream {

      private readonly IField _level;
      private readonly Field _message;
      private readonly OutputContext _context;

      public LogWriter(OutputContext context) {
         _context = context;
         _level = context.OutputFields.First(f => f.Alias.Equals("level", StringComparison.OrdinalIgnoreCase));
         _message = context.OutputFields.First(f => f.Alias.Equals("message", StringComparison.OrdinalIgnoreCase));
      }

      public void Write(IEnumerable<IRow> rows) {

         foreach (var row in rows) {
            WriteRow(row);
         }
      }

      public Task WriteAsync(IEnumerable<IRow> rows, CancellationToken token = default) {
         foreach (var row in rows) {
            token.ThrowIfCancellationRequested();
            WriteRow(row);
         }
         return Task.CompletedTask;
      }

      public async Task WriteStreamAsync(IAsyncEnumerable<IRow> rows, CancellationToken token = default) {
         await foreach (var row in rows.WithCancellation(token).ConfigureAwait(false)) {
            token.ThrowIfCancellationRequested();
            WriteRow(row);
         }
      }

      private void WriteRow(IRow row) {
         var message = (string)row[_message] ?? string.Empty;
         switch (row[_level].ToString().ToLower()) {
            case "warn":
            case "warning":
               _context.Warn(message);
               break;
            case "error":
               _context.Error(message);
               break;
            case "debug":
               _context.Debug(() => message);
               break;
            default:
               _context.Info(message);
               break;
         }
      }
   }
}
