using Microsoft.AspNetCore.Http;
using TransformalizeModule.Models;
using TransformalizeModule.Services.Contracts;
using OrchardCore.ContentManagement;
using Transformalize.Configuration;
using OrchardCore.Title.Models;
using OrchardCore.Alias.Models;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.AspNetCore.Routing;
using System.Xml.Linq;

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

      public ReportService(
         IArrangementLoadService loadService,
         IArrangementStreamService streamService,
         IArrangementService arrangementService,
         IHttpContextAccessor httpContextAccessor,
         ISettingsService settings,
         IParameterService parameterService,
         ISchemaService schemaService,
         IContentManager contentManager
      ) {
         _loadService = loadService;
         _streamService = streamService;
         _arrangementService = arrangementService;
         _httpContextAccessor = httpContextAccessor;
         _settings = settings;
         _schemaService = schemaService;
         _contentManager = contentManager;
         _parameters = parameterService.GetParameters();
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

         if (response.ContentItem == null) {

            if (_httpContextAccessor.HttpContext.User.IsInRole("Administrator")) {

               var req = _httpContextAccessor.HttpContext.Request;

               if (TryConnection(out Connection connection)) {

                  response.BreadCrumbs.Add(new BreadCrumb("Browse", req.Path));
                  if (TryTable(out string schema, out string table)) {
                     response.BreadCrumbs.Add(new BreadCrumb(connection.Name, QueryHelpers.AddQueryString(req.Path, "c", connection.Name)));
                     PrepareTable(response, connection, schema, table);
                  } else {
                     PrepareTables(response, connection, $"{req.Scheme}://{req.Host}{req.Path}{req.QueryString}");
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

            var authorized = await CanAccess(response.ContentItem);
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
         if (_parameters.ContainsKey("c") && _settings.Connections.ContainsKey(_parameters["c"])) {
            connection = _settings.Connections[_parameters["c"]];
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
               }
            }
         });

         process.Load();

         foreach (var connection in _settings.Process.Connections.Where(c => c.Browse).OrderBy(c => c.Name)) {

            var newUrl = QueryHelpers.AddQueryString(currentUrl, "c", connection.Name);

            process.Entities[0].Rows.Add(new Transformalize.Impl.CfgRow(["Name", "Provider"]) {
               Map = new Dictionary<string, short> {
                        { "Name", 0 },
                        { "Provider", 1 }
                     },
               Storage = [$"<a href=\"{newUrl}\">{connection.Name}</a>", connection.Provider]
            });
         }

         response.Part.Arrangement.Text = process.Serialize();
         response.ContentItem.Weld(response.Part);
         response.ContentItem.Weld(new TitlePart { Title = "Connections" });
      }

      private void PrepareTables(TransformalizeResponse<TransformalizeReportPart> response, Connection connection, string currentUrl) {

         response.ContentItem = new ContentItem();
         response.Part = new TransformalizeReportPart();

         var process = new Process { Name = "Tables", ReadOnly = true };
         process.Connections.Add(connection);
         var originalName = connection.Name;
         process.Connections.First().Name = "input";

         currentUrl = new Flurl.Url(currentUrl).RemoveQueryParams("size", "sort").ToString();

         var separator = currentUrl.Contains('?') ? "&" : "?";

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
                     T = $"format(<a href=\"{currentUrl}{separator}t={{name}}\">{{name}}</a>)"
                  },
                  new Field {
                     Name = "type",
                     Label="Type",
                     Type = "string",
                     Length = "128"
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
                  Length= "128"
               },
               new Field {
                  Name = "table_name",
                  Label="Name",
                  Type = "string",
                  Length= "128",
                  Raw = true,
                  T = $"copy(table_schema,table_name).format(<a href=\"{currentUrl}{separator}t={{table_schema}}.{{table_name}}\">{{table_name}}</a>)"
               },
               new Field {
                  Name = "table_type",
                  Label="Type",
                  Type = "string",
                  Length = "128"
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
         process.Connections.Add(new Connection { Name = connection.Name });

         process.Entities.Add(new Entity {
            Name = table,
            Schema = schema,
            Input = connection.Name
         });

         process.Actions.Add(new Transformalize.Configuration.Action() {
            Type = "humanize-labels"
         });

         process.Load();

         if (_httpContextAccessor.HttpContext.Request.Query.ContainsKey("save") && _httpContextAccessor.HttpContext.Request.Query["save"].ToString() == "true") {

            response.ContentItem = new ContentItem();
            FigureShitOut(ref response, ref process, table);

            var contentItem = await _contentManager.NewAsync("TransformalizeReport");
            contentItem.Apply(new TitlePart { Title = table });
            contentItem.Apply(new AliasPart { Alias = contentItem.ContentItemId });
            await _contentManager.CreateAsync(contentItem);

            process.Name = contentItem.ContentItemId;
            process.Connections[0].ConnectionString = string.Empty;
            process.Connections[0].Database = string.Empty;
            process.Connections[0].User = string.Empty;
            process.Connections[0].Password = string.Empty;
            process.Connections[0].Browse = false;
            process.Connections.RemoveAt(1);
            contentItem.Alter<TransformalizeReportPart>(part => { part.Arrangement.Text = RemoveXmlProperty(process.Serialize(),"provider"); });
            await _contentManager.UpdateAsync(contentItem);

            var editUrl = $"/Admin/Contents/ContentItems/{contentItem.ContentItemId}/Edit";
            _httpContextAccessor.HttpContext.Response.Redirect(editUrl);

         } else {
            response.ContentItem = new ContentItem();
            FigureShitOut(ref response, ref process, table);
         }
      }

      public void FigureShitOut(ref TransformalizeResponse<TransformalizeReportPart> response, ref Process process, string table) {

         response.ContentItem.Weld(new TitlePart { Title = table });
         response.Part = new TransformalizeReportPart();
         response.Part.Arrangement.Text = process.Serialize();
         response.ContentItem.Weld(response.Part);

         process = _schemaService.LoadForSchema(response.ContentItem, "xml");
         process.Load();
         process = _schemaService.GetSchemaAsync(process).Result;
         foreach (var hiddenField in _httpContextAccessor.HttpContext.Request.Query["hide"].ToString().Split('.', StringSplitOptions.RemoveEmptyEntries)) {
            foreach (var field in process.Entities[0].Fields.Where(f => f.Name.Equals(hiddenField))) {
               field.Output = false;
            }
         }
         response.Part.Arrangement.Text = process.Serialize();
         response.ContentItem.Weld(response.Part);
      }

      public static string RemoveXmlProperty(string xml, string property) {
         var xDocument = XDocument.Parse(xml);

         foreach (var element in xDocument.Descendants()) {
            element.Attribute(property)?.Remove();
         }

         return xDocument.ToString();
      }

   }
}
