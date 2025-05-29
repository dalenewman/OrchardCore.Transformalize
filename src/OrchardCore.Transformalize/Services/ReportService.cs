using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.WebUtilities;
using OrchardCore.Alias.Models;
using OrchardCore.ContentManagement;
using OrchardCore.Data;
using OrchardCore.Title.Models;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using Transformalize.Configuration;
using Transformalize.Extensions;
using TransformalizeModule.Models;
using TransformalizeModule.Services.Contracts;
using YesSql;

namespace TransformalizeModule.Services {

   public class ReportService : IReportService {

      private readonly IArrangementLoadService _loadService;
      private readonly IArrangementStreamService _streamService;
      private readonly IArrangementService _arrangementService;
      private readonly IHttpContextAccessor _httpContextAccessor;
      private readonly ISettingsService _settings;
      private readonly ISchemaService _schemaService;
      private readonly IContentManager _contentManager;
      private readonly IDictionary<string, string> _parameters;
      private readonly IDbConnectionAccessor _dbConnectionAccessor;
      private readonly IStore _store;

      public ReportService(
         IArrangementLoadService loadService,
         IArrangementStreamService streamService,
         IArrangementService arrangementService,
         IHttpContextAccessor httpContextAccessor,
         ISettingsService settings,
         IParameterService parameterService,
         ISchemaService schemaService,
         IContentManager contentManager,
         IDbConnectionAccessor dbConnectionAccessor,
         IStore store
      ) {
         _loadService = loadService;
         _streamService = streamService;
         _arrangementService = arrangementService;
         _httpContextAccessor = httpContextAccessor;
         _settings = settings;
         _schemaService = schemaService;
         _contentManager = contentManager;
         _parameters = parameterService.GetParameters();
         _dbConnectionAccessor = dbConnectionAccessor;
         _store = store;
      }

      public async Task<bool> CanAccess(ContentItem contentItem) {
         return await _arrangementService.CanAccess(contentItem);
      }

      public async Task<ContentItem> GetByIdOrAliasAsync(string idOrAlias) {
         return await _arrangementService.GetByIdOrAliasAsync(idOrAlias);
      }

      public Process LoadForStream(ContentItem contentItem) {
         return _loadService.LoadForStream(contentItem);
      }

      public Process LoadForReport(ContentItem contentItem, string format = null) {
         return _loadService.LoadForReport(contentItem, format);
      }

      public Process LoadForBatch(ContentItem contentItem) {
         return _loadService.LoadForBatch(contentItem);
      }

      public Process LoadForMap(ContentItem contentItem) {
         return _loadService.LoadForMap(contentItem);
      }

      public Process LoadForMapStream(ContentItem contentItem) {
         return _loadService.LoadForMapStream(contentItem);
      }

      public Process LoadForCalendar(ContentItem contentItem) {
         return _loadService.LoadForCalendar(contentItem);
      }

      public Process LoadForCalendarStream(ContentItem contentItem) {
         return _loadService.LoadForCalendarStream(contentItem);
      }

      public async Task RunAsync(Process process, StreamWriter streamWriter) {
         await _streamService.RunAsync(process, streamWriter);
      }

      public void Run(Process process, StreamWriter streamWriter) {
         _streamService.Run(process, streamWriter);
      }

      public async Task<TransformalizeResponse<TransformalizeReportPart>> Validate(TransformalizeRequest request) {

         var response = new TransformalizeResponse<TransformalizeReportPart>(request.Format) {
            ContentItem = await GetByIdOrAliasAsync(request.ContentItemId)
         };

         bool authorized = false;

         if (response.ContentItem == null) {

            if (_httpContextAccessor.HttpContext!.User.IsInRole("Administrator")) {

               var req = _httpContextAccessor.HttpContext.Request;

               if (TryConnection(out Connection connection)) {

                  response.BreadCrumbs.Add(new BreadCrumb("Browse", req.Path));

                  if (TryTable(out string schema, out string table)) {
                     if (TryReturnUrl(out string returnUrl)) {
                        response.BreadCrumbs.Add(new BreadCrumb(connection.Name, returnUrl));
                     } else {
                        response.BreadCrumbs.Add(new BreadCrumb(connection.Name, QueryHelpers.AddQueryString(req.Path, "c", connection.Name)));
                     }
                     PrepareTable(response, connection, schema, table);
                     response.Editable = true;
                  } else {
                     PrepareTables(response, connection, $"{req.Path}{req.QueryString}");
                  }

               } else {
                  response.BreadCrumbs.Add(new BreadCrumb("Browse", req.Path));
                  PrepareConnections(response, $"{req.Path}{req.QueryString}");
               }

            } else {
               SetupNotFoundResponse(request, response);
               return response;
            }

         } else {

            authorized = await CanAccess(response.ContentItem);
            if (request.Secure && !authorized) {
               SetupPermissionsResponse(request, response);
               return response;
            }

            response.Part = response.ContentItem.As<TransformalizeReportPart>();
            if (response.Part == null) {
               SetupWrongTypeResponse(request, response);
               return response;
            }

         }

         // allow authorized users to mess with existing reports, but not save their edits (yet)
         if (authorized && !response.BreadCrumbs.Any() && request.Mode.In("default", "stream")) {
            response.Editable = true;
            var process = new Transformalize.ConfigurationFacade.Process(response.Part.Arrangement.Text);
            ModifyProcess(process);
            response.Part.Arrangement.Text = process.Serialize();
         }

         switch (request.Mode) {
            case "calendar":
               response.Process = LoadForCalendar(response.ContentItem);
               break;
            case "stream-calendar":
               response.Process = LoadForCalendarStream(response.ContentItem);
               break;
            case "map":
               response.Process = LoadForMap(response.ContentItem);
               break;
            case "stream-map":
               response.Process = LoadForMapStream(response.ContentItem);
               break;
            case "stream":
               response.Process = LoadForStream(response.ContentItem);
               break;
            default:
               response.Process = LoadForReport(response.ContentItem, request.Format);
               break;
         }

         if (response.Process.Status != 200) {
            SetupLoadErrorResponse(request, response);
            return response;
         }

         if (request.ValidateParameters && !response.Process.Parameters.All(p => p.Valid)) {
            SetupInvalidParametersResponse(request, response);
            return response;
         }

         response.Valid = true;
         return response;
      }

      private bool TryTable(out string schema, out string table) {
         schema = string.Empty;
         table = string.Empty;
         if (_parameters.ContainsKey("t") && !string.IsNullOrWhiteSpace(_parameters["t"])) {
            table = _parameters["t"].Trim();
            if (table.Contains('.')) {
               // handle schema.table format
               var parts = table.Split('.');
               if (parts.Length == 2) {
                  schema = parts[0].Trim(); // take the schema name
                  table = parts[1].Trim(); // take the table/view name
               }
            }
            return true;
         }
         return false;
      }

      private bool TryConnection(out Connection connection) {
         connection = new Connection();
         if (_parameters.ContainsKey("c")) {
            if (_settings.Connections.ContainsKey(_parameters["c"])) {
               connection = _settings.Connections[_parameters["c"]];
            } else if (_parameters["c"] == Common.OrchardConnectionName) {
               using (var cn = _dbConnectionAccessor.CreateConnection()) {
                  connection.ConnectionString = cn.ConnectionString;
                  connection.Name = Common.OrchardConnectionName;
                  connection.Provider = _store.Configuration.SqlDialect.Name.ToLower();
                  if (connection.Provider == "sqlite") {
                     connection.File = cn.Database;
                  } else if (connection.Provider == "postgresql") {
                     connection.Enclose = true; // orchard encloses it's objects, requiring case sensativity and double quotes
                  }
               }
            } else {
               return false;
            }
            return true;
         }
         return false;
      }

      public void SetupInvalidParametersResponse<TPart>(TransformalizeRequest request, TransformalizeResponse<TPart> response) {
         _arrangementService.SetupInvalidParametersResponse(request, response);
      }

      public void SetupPermissionsResponse<TPart>(TransformalizeRequest request, TransformalizeResponse<TPart> response) {
         _arrangementService.SetupPermissionsResponse(request, response);
      }

      public void SetupNotFoundResponse<TPart>(TransformalizeRequest request, TransformalizeResponse<TPart> response) {
         _arrangementService.SetupNotFoundResponse(request, response);
      }

      public void SetupLoadErrorResponse<TPart>(TransformalizeRequest request, TransformalizeResponse<TPart> response) {
         _arrangementService.SetupLoadErrorResponse(request, response);
      }

      public void SetupWrongTypeResponse<T1>(TransformalizeRequest request, TransformalizeResponse<T1> response) {
         _arrangementService.SetupWrongTypeResponse(request, response);
      }

      public void SetupCustomErrorResponse<TPart>(TransformalizeRequest request, TransformalizeResponse<TPart> response, string error) {
         _arrangementService.SetupCustomErrorResponse(request, response, error);
      }

      private void PrepareConnections(TransformalizeResponse<TransformalizeReportPart> response, string currentUrl) {

         // Create Connections Browser
         response.ContentItem = new ContentItem();
         response.Part = new TransformalizeReportPart();

         // Create a connections arrangement from settings
         var process = new Process { Name = "Connections", ReadOnly = true };
         process.Connections.Add(new Connection {
            Name = "input",
            Provider = "internal"
         });
         process.Connections.Add(new Connection {
            Name = "output",
            Provider = "internal"
         });
         process.Entities.Add(new Entity {
            Name = "Connections",
            Alias = "Browse",
            Input = "input",
            Fields = new List<Field> {
               new Field {
                  Name = "Name",
                  Type = "string",
                  Length= "128",
                  Sortable = "false",
                  Raw = true,
               },
               new Field {
                  Name = "Provider",
                  Type = "string",
                  Length = "128",
                  Sortable = "false"
               },
               new Field {
                  Name = "Database",
                  Label = "Database / File",
                  Type = "string",
                  Length = "128",
                  Sortable = "false"
               }
            }
         });

         process.Load();

         foreach (var c in _settings.Process.Connections.Where(c => c.Browse).OrderBy(c => c.Name)) {

            var newUrl = QueryHelpers.AddQueryString(currentUrl, "c", c.Name);

            var database = c.Provider == "sqlite" ? c.File : c.Database;
            process.Entities[0].Rows.Add(new Transformalize.Impl.CfgRow(["Name", "Provider", "Database"]) {
               Map = new Dictionary<string, short> {
                        { "Name", 0 },
                        { "Provider", 1 },
                        { "Database", 2 }
                     },
               Storage = [$"<a href=\"{newUrl}\">{c.Name}</a>", c.Provider, database]
            });
         }

         AddOrchardConnection(currentUrl, process);

         response.Part.Arrangement.Text = process.Serialize();
         response.ContentItem.Weld(response.Part);
         response.ContentItem.Weld(new TitlePart { Title = "Connections" });
      }

      private static void AddOrchardConnection(string currentUrl, Process process) {
         if (!process.Connections.Exists(c => c.Name == Common.OrchardConnectionName)) {

            var newUrl = QueryHelpers.AddQueryString(currentUrl, "c", Common.OrchardConnectionName);

            process.Entities[0].Rows.Add(new Transformalize.Impl.CfgRow(["Name", "Provider", "Database"]) {
               Map = new Dictionary<string, short> {
                        { "Name", 0 },
                        { "Provider", 1 },
                        { "Database", 2 }
                     },
               Storage = [$"<a href=\"{newUrl}\">{Common.OrchardConnectionName}</a>", Common.OrchardConnectionName, Common.OrchardConnectionName]
            });
         }
      }

      private void PrepareTables(TransformalizeResponse<TransformalizeReportPart> response, Connection connection, string currentUrl) {

         response.ContentItem = new ContentItem();
         response.Part = new TransformalizeReportPart();

         var process = new Process { Name = "Tables", ReadOnly = true };
         process.Connections.Add(connection);
         var originalName = connection.Name;
         process.Connections.First().Name = "input";

         var modifiedUrl = new Flurl.Url(currentUrl).RemoveQueryParams("size", "sort", "page").ToString();

         var separator = modifiedUrl.Contains('?') ? "&" : "?";

         if (connection.Provider.Equals("sqlite")) {
            process.Entities.Add(new Entity {
               Name = "sqlite_master",
               Alias = originalName,
               Input = "input",
               Filter = new List<Filter> {
                  new Filter {
                     Expression = "type IN ('table', 'view') AND name NOT LIKE 'sqlite_%'"
                  }
               },
               Fields = new List<Field> {
                  new Field {
                     Name = "name",
                     Label="Name",
                     Type = "string",
                     Length= "128",
                     Raw = true,
                     T = $"format(<a href=\"{modifiedUrl}{separator}t={{name}}&{Common.ReturnUrlName}={Uri.EscapeDataString(currentUrl)}\">{{name}}</a>)",
                     Parameter = "search"
                  },
                  new Field {
                     Name = "type",
                     Label="Type",
                     Type = "string",
                     Length = "128",
                     Parameter = "facet"
                  }
               }
            });

         } else {
            // PostgreSQL, MySQL, SQL Server, etc.

            process.Entities.Add(new Entity {
               Name = "tables",
               Alias = originalName,
               Schema = "information_schema",
               Input = "input",
               Filter = new List<Filter> {
               new Filter {
                  Expression = "table_type IN ('BASE TABLE','VIEW','FOREIGN', 'FOREIGN TABLE') AND table_schema NOT IN ('information_schema', 'mysql', 'performance_schema', 'pg_catalog', 'sys')"
               },
               new Filter {
                  Expression = connection.Provider.Equals("mysql") ? $"table_schema = '{connection.Database}'" : "1=1"
               }
            },
               Fields = new List<Field> {
               new Field {
                  Name = "table_schema",
                  Label="Schema",
                  Type = "string",
                  Length= "128",
                  Parameter = "facet"
               },
               new Field {
                  Name = "table_name",
                  Label="Name",
                  Type = "string",
                  Length= "128",
                  Raw = true,
                  T = $"copy(table_schema,table_name).format(<a href=\"{modifiedUrl}{separator}t={{table_schema}}.{{table_name}}&{Common.ReturnUrlName}={Uri.EscapeDataString(currentUrl)}\">{{table_name}}</a>)",
                  Parameter = "search"
               },
               new Field {
                  Name = "table_type",
                  Label="Type",
                  Type = "string",
                  Length = "128",
                  Parameter = "facet"
               }
            }
            });
         }

         process.Load();

         response.Part.Arrangement.Text = process.Serialize();
         response.ContentItem.Weld(response.Part);
         response.ContentItem.Weld(new TitlePart { Title = "Tables or Views" });
      }

      private async void PrepareTable(TransformalizeResponse<TransformalizeReportPart> response, Connection connection, string schema, string table) {

         // Create a views arrangement for connection
         var process = new Process { Name = table, ReadOnly = true };
         process.Connections.Add(connection);

         process.Entities.Add(new Entity {
            Name = table,
            Schema = schema,
            Input = connection.Name
         });

         process.Actions.Add(new Transformalize.Configuration.Action() {
            Type = "humanize-labels"
         });

         process.Load();

         if (Requested("save", "true")) {

            response.ContentItem = new ContentItem();
            ComposeArrangement(ref response, ref process, table);

            var contentItem = await _contentManager.NewAsync("TransformalizeReport");
            contentItem.Apply(new TitlePart { Title = table });
            contentItem.Apply(new AliasPart { Alias = contentItem.ContentItemId });
            await _contentManager.CreateAsync(contentItem);

            process.Name = contentItem.ContentItemId;
            ClearConnectionDetails(process);
            process.Connections.RemoveAt(1);

            foreach (var field in process.Entities[0].Fields) {
               field.Src = string.Empty;
               if(field.Input) {
                  field.Label = null;
                  field.Sortable = null;
                  field.SortField = null;
                  if (field.Alias != null && !field.Alias.EndsWith("Alias")) {
                     field.Alias = null;
                  }
               } else {
                  field.Alias = null;
                  field.Label = null;
               }
            }

            process.SearchTypes.Clear();

            string xml = RemoveXmlProperty(process.Serialize(), "provider");

            // do some fancy replacements to make the transforms more readable
            xml = Regex.Replace(xml, @" t=""(.*?)"" ", match => {
               string attributeValue = match.Groups[1].Value;

               // replace basic entities *only within the match*
               attributeValue = attributeValue.Replace("&lt;", "<").Replace("&gt;", ">").Replace("&quot;", "\"");

               return $" t='{attributeValue}' "; // switch to single quotes
            });

            contentItem.Alter<TransformalizeReportPart>(part => { part.Arrangement.Text = xml; });
            await _contentManager.UpdateAsync(contentItem);

            var editUrl = $"/Admin/Contents/ContentItems/{contentItem.ContentItemId}/Edit";
            _httpContextAccessor.HttpContext.Response.Redirect(editUrl);

         } else {
            response.ContentItem = new ContentItem();
            ComposeArrangement(ref response, ref process, table);
         }
      }

      /// <summary>
      /// Clears the connection details for the specified process, resetting all connection-specific properties to their
      /// default values.
      /// </summary>
      /// <remarks>This method resets the connection details of the first connection in the process's
      /// connection list.  All properties, such as connection string, server, port, and credentials, are set to their
      /// default or empty values.</remarks>
      /// <param name="process">The process whose connection details will be cleared. This parameter cannot be null.</param>
      private static void ClearConnectionDetails(Process process) {
         var cn = process.Connections[0];
         cn.ConnectionString = string.Empty;
         cn.Server = "localhost";
         cn.Port = 0;
         cn.Database = string.Empty;
         cn.File = string.Empty;
         cn.User = string.Empty;
         cn.Password = string.Empty;
         cn.Browse = false;
      }

      private bool Requested(string key, string value) {
         return _httpContextAccessor.HttpContext!.Request.Query.ContainsKey(key) && _httpContextAccessor.HttpContext.Request.Query[key].ToString() == value;
      }

      public void ModifyProcess(Transformalize.ConfigurationFacade.Process process) {
         var req = _httpContextAccessor.HttpContext!.Request;

         var shortened = Common.GetShortestUniqueVersions(process.Entities[0].Fields.Select(f => f.Name).ToArray());
         for (int i = 0; i < shortened.Length; i++) {
            process.Entities[0].Fields[i].Src = shortened[i];
         }

         AddSearch(process, req);
         AddSingleFacet(process, req);
         AddMultipleFacet(process, req);
         AddTimeAgo(process, req);
         AddEllipse(process, req);
         Hide(process, req);

         var order = req.Query["o"].ToString().Split('.');
         process.Entities[0].Fields = process.Entities[0].Fields.OrderBy(f => Array.IndexOf(order, f.Src)).ToList();

      }

      public void ComposeArrangement(ref TransformalizeResponse<TransformalizeReportPart> response, ref Process process, string table) {

         response.ContentItem.Weld(new TitlePart { Title = table });
         response.Part = new TransformalizeReportPart();
         response.Part.Arrangement.Text = process.Serialize();
         response.ContentItem.Weld(response.Part);

         process = _schemaService.LoadForSchema(response.ContentItem, "xml");
         process = _schemaService.GetSchemaAsync(process).Result;

         var facade = new Transformalize.ConfigurationFacade.Process(process.Serialize());
         ModifyProcess(facade);

         var xml = facade.Serialize();
         response.Part.Arrangement.Text = xml;
         response.ContentItem.Weld(response.Part);
         process = new Process(xml);
      }

      private static void AddTimeAgo(Transformalize.ConfigurationFacade.Process process, HttpRequest req) {
         foreach (var src in req.Query["ta"].ToString().Split('.', StringSplitOptions.RemoveEmptyEntries)) {
            var fieldIndex = process.Entities[0].Fields.FindIndex(f => f.Src == src);
            if (fieldIndex > -1) {
               var original = process.Entities[0].Fields[fieldIndex];
               if (original.Type == "datetime") {

                  original.Output = "false";
                  original.Export = "true";
                  original.Parameter = null;
                  original.Alias = original.Name + "Alias";

                  var timeAgo = new Transformalize.ConfigurationFacade.Field {
                     Name = original.Name,
                     Input = "false",
                     Length = "128",
                     Src = original.Src,
                     T = $"copy({original.Alias}).timeAgo().format(<span title=\"{{{original.Alias}:yyyy-MM-dd}}\">{{{original.Name}}}</span>)",
                     Raw = "true",
                     Export = "false",
                     SortField = original.Name,
                     Sortable = "true"
                  };
                  process.Entities[0].Fields.Insert(fieldIndex + 1, timeAgo);
               }
            }
         }
      }

      private static void AddEllipse(Transformalize.ConfigurationFacade.Process process, HttpRequest req) {
         foreach (var src in req.Query["e"].ToString().Split('.', StringSplitOptions.RemoveEmptyEntries)) {
            var fieldIndex = process.Entities[0].Fields.FindIndex(f => f.Src == src);
            if (fieldIndex > -1) {
               var original = process.Entities[0].Fields[fieldIndex];
               if (original.Type == "string" || original.Type == null) {
                  original.Alias = original.Name + "Alias";
                  original.Output = "false";
                  original.Export = "true";
                  if (original.Parameter != "search") {
                     original.Parameter = null;
                  }

                  var encoded = new Transformalize.ConfigurationFacade.Field {
                     Name = original.Name + "Encoded",
                     Input = "false",
                     Output = "false",
                     Length = "max",
                     Src = original.Src,
                     T = $"copy({original.Alias}).htmlEncode()"
                  };
                  process.Entities[0].Fields.Insert(fieldIndex + 1, encoded);

                  var ellipsis = new Transformalize.ConfigurationFacade.Field {
                     Name = original.Name,
                     Input = "false",
                     Length = "max",
                     Src = original.Src,
                     T = $"copy({original.Alias}).ellipsis(20).format(<span title=\"{{{encoded.Name}}}\">{{{original.Name}}}</span>)",
                     Raw = "true",
                     Export = "false",
                     SortField = original.Name,
                     Sortable = "true"
                  };
                  process.Entities[0].Fields.Insert(fieldIndex + 2, ellipsis);

               }
            }
         }
      }

      private static void Hide(Transformalize.ConfigurationFacade.Process process, HttpRequest req) {
         foreach (var src in req.Query["h"].ToString().Split('.', StringSplitOptions.RemoveEmptyEntries)) {
            var field = process.Entities[0].Fields.FirstOrDefault(f => f.Src == src);
            if (field != null) {
               field.Output = "false";
               field.Export = "false";
               field.T = string.Empty;
            }
         }
      }

      private static void AddMultipleFacet(Transformalize.ConfigurationFacade.Process process, HttpRequest req) {
         foreach (var src in req.Query["f2"].ToString().Split('.', StringSplitOptions.RemoveEmptyEntries)) {
            var field = process.Entities[0].Fields.FirstOrDefault(f => f.Src == src);
            if (field != null) {
               field.Parameter = "facets";
            }
         }
      }

      private static void AddSingleFacet(Transformalize.ConfigurationFacade.Process process, HttpRequest req) {
         foreach (var src in req.Query["f1"].ToString().Split('.', StringSplitOptions.RemoveEmptyEntries)) {
            var field = process.Entities[0].Fields.FirstOrDefault(f => f.Src == src);
            if (field != null) {
               field.Parameter = "facet";
               if (field.Type == "bool") {
                  var input = process.Entities[0].Input;
                  if (process.Connections.First(c => c.Name == input).Provider == "postgresql") {
                     field.Map = "false:No,true:Yes";
                  } else {
                     field.Map = "0:No,1:Yes";
                  }
               }
            }
         }
      }

      private static void AddSearch(Transformalize.ConfigurationFacade.Process process, HttpRequest req) {
         foreach (var src in req.Query["s"].ToString().Split('.', StringSplitOptions.RemoveEmptyEntries)) {
            var field = process.Entities[0].Fields.FirstOrDefault(f => f.Src == src);
            if (field != null) {
               field.Parameter = "search";
            }
         }
      }

      public static string RemoveXmlProperty(string xml, string property) {
         var xDocument = XDocument.Parse(xml);

         foreach (var element in xDocument.Descendants()) {
            element.Attribute(property)?.Remove();
         }

         return xDocument.ToString();
      }

      private bool TryReturnUrl(out string returnUrl) {
         returnUrl = string.Empty;
         var req = _httpContextAccessor.HttpContext.Request;

         if (req.Query.ContainsKey(Common.ReturnUrlName) && !string.IsNullOrWhiteSpace(req.Query[Common.ReturnUrlName].ToString())) {
            returnUrl = Uri.UnescapeDataString(req.Query[Common.ReturnUrlName].ToString());
            return true;
         }
         return false;
      }

   }
}
