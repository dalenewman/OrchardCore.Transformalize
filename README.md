A [Transformalize](https://github.com/dalenewman/Transformalize) 
module for [Orchard Core](https://github.com/OrchardCMS/OrchardCore) rc2. 

#### Features
- <strike>Code Mirror XML Editor for arrangements</strike>
- Reporting
   - <strike>Sortable, Filterable Table with pagination</strike>
   - <strike>CSV Extract</strike>
   - <strike>JSON Extract</strike>
   - Calendar view
- Map(Box) view
  - <strike>js libraries</strike>
  - <strike>MapBox Key</strike>
  - <strike>Dot Description Value/Field</strike>
  - <strike>Dot Color Value/Field</strike>
  - <strike>Dot Opacity Value/Field</strike>
  - <strike>Dot Size Value/Field</strike>
  - <strike>Paging (extended sizes or no paging)</strike>
- <strike>Batches / Bulk Actions</strike>
  - <strike>Tasks (ETL Jobs) for running batches, etc</strike>
  - <strike>Forms for accepting parameters for batches</strike>
  - <strike>Bulk Action Settings</strike>
    - <strike>Common</strike>
    - <strike>Part</strike>
  - Workflow: Still learning, but bulk operations / batches might be helped by the Workflows module.
- <strike>Add Mini Profiler into ADO providers</strike>
- <strike>Common Arrangement Settings</strike>
  - <strike>Parameters</strike>
  - <strike>Connections</strike>
  - <strike>Actions</strike>
  - <strike>Page Sizes</strike>
  - <strike>Fields</strike> (no short-hand yet)

#### Providers
- <strike>[Elasticsearch](https://github.com/dalenewman/Transformalize.Provider.Elasticsearch)</strike>, <strike>[SQL Server](https://github.com/dalenewman/Transformalize.Provider.SqlServer)</strike>, <strike>[PostgreSQL](https://github.com/dalenewman/Transformalize.Provider.PostgreSql)</strike>, <strike>[SQLite](https://github.com/dalenewman/Transformalize.Provider.SQLite)</strike>, <strike>[MySql](https://github.com/dalenewman/Transformalize.Provider.MySql)</strike>, <strike>[JSON](https://github.com/dalenewman/Transformalize.Provider.JSON)</strike>, <strike>[CSV](https://github.com/dalenewman/Transformalize.Provider.CsvHelper)</strike>, <strike>[Bogus](https://github.com/dalenewman/Transformalize.Provider.Bogus)</strike>, <strike>[GeoJson](https://github.com/dalenewman/Transformalize.Provider.GeoJson)</strike>
- [SOLR](https://github.com/dalenewman/Transformalize.Provider.SOLR), [KML](https://github.com/dalenewman/Transformalize/tree/master/Providers/Kml), [Lucene](https://github.com/dalenewman/Transformalize.Provider.Lucene)
- <strike>Update SQL Server provider to use [Microsoft.Data.SqlClient](https://www.nuget.org/packages/Microsoft.Data.SqlClient/).</strike>

#### Transforms
- <strike>built-in [transforms](https://github.com/dalenewman/Transformalize/blob/master/Containers/Autofac/Transformalize.Container.Autofac.Shared/TransformBuilder.cs) and [validators](https://github.com/dalenewman/Transformalize/blob/master/Containers/Autofac/Transformalize.Container.Autofac.Shared/ValidateBuilder.cs)</strike>, <strike>[Jint](https://github.com/dalenewman/Transformalize.Transform.Jint
)</strike>, <strike>JSON</strike>, <strike>[Humanize](https://github.com/dalenewman/Transformalize.Transform.Humanizer)</strike>
- <strike>[Razor](https://github.com/dalenewman/Transformalize.Provider.Razor)</strike>, <strike>[Liquid / Fluid](https://github.com/dalenewman/Transformalize.Transform.Fluid
)</strike>, <strike>[Lambda Parser](https://github.com/dalenewman/Transformalize.Transform.LambdaParser)</strike>
- Orchard transforms: <strike>username()</strike>, <strike>userid()</strike>, <strike>useremail()</strike>.
- <strike>file related (get file name, file path, file extension)</strike>
- <strike>IMemoryCache, ISignal Caching for Expensive Transforms</strike>
  - <strike>Razor: Cache RazorTemplate</strike>
  - <strike>Fluid: Cache `FluidTemplate`</strike>
  - <strike>Jint: Combine script and cache result of `JavaScriptParser.Parse`</strike>

#### Actions
- <strike>file related (copy, move, archive, replace)</strike>
- <strike>nested tasks</strike>
- <strike>log</strike>

#### Development Environment
- Visual Studio 2019 with ASP.NET Core related workloads:
  - ASP.NET and Web Development
  - .NET Core Cross-Platform Development
- <strike>Add nuget package source https://www.myget.org/F/transformalize/api/v3/index.json</strike>
- Set the Site as the startup project and run.
- Enable Transformalize
- Optionally run Transformalize recipes

#### Module Dependencies
You may have to enable some of these manually if you're not installing from scratch.
- `OrchardCore.Users`
- `OrchardCore.Contents`
- `OrchardCore.Title`
- [`Etch.OrchardCore.ContentPermissions`](https://github.com/EtchUK/Etch.OrchardCore.ContentPermissions)
- `OrchardCore.MiniProfiler` (not required anymore)
- `OrchardCore.Alias`
- `OrchardCore.ContentFields`
- `OrchardCore.Workflows`

#### Issues
- Tabs in part editors break on ModelState error

---

#### GIF
![bogus report](src/Site/App_Data/samples/sacramento-crime/criminal-bulk-actions.gif)

> Prerequisite Reading: [Transformalize](https://github.com/dalenewman/Transformalize) read me page.

#### Reporting
Using [Transformalize](https://github.com/dalenewman/Transformalize) for reporting basically means you 
read from one entity and do not specify an output.  To find out 
more about the providers, click on the links above.

#### Tasks
Tasks can transform, validate, or de-normalize data.
They can prompt the user for and validate input. They 
may be used to modify data.

#### Bulk Actions
When you enable Bulk Actions on a report, you designate a field or calculated field 
in your arrangement as the `Bulk Action Value Field`.  Each row's value from 
this field is sent to the bulk action.

Bulk actions are processed by tasks (above).  Tasks are added to 
an arrangement like this:

```xml
<cfg name="report">
   <actions>
      <add name="the-alias-of-your-task" description="seen in the bulk action dropdown" />
   </actions>
   <connections/>
   <entities/>
</cfg>
```

Five tasks must be defined in order to run bulk actions:

1. `batch-create`: create and return a batch identifier
1. `batch-write`: write batch values.
1. `batch-summary`: gather review and result summary for a batch.
1. `batch-run`: indicate the task is running (not yet implemented)
1. `batch-success`: indicate the task succeeded
1. `batch-fail`: indicate the task failed

A recipe named "Transformalize Batches SQLite" provides a 
set of SQLite batch tasks.

