A [Transformalize](https://github.com/dalenewman/Transformalize) 
module for [Orchard Core](https://github.com/OrchardCMS/OrchardCore) rc1. 

#### Features
- <strike>Code Mirror XML Editor for arrangements</strike>
- Reporting
   - <strike>Sortable, Filterable Table with pagination</strike>
   - <strike>CSV Extract</strike>
   - <strike>JSON Extract</strike>
   - Map(Box) view
   - Calendar view
- Batches / Bulk Actions <span style="color:orange;">(underway)</span>
  - <strike>Tasks (ETL Jobs) for running batches, etc</strike>
  - Forms for accepting parameters for batches
- <strike>Add Mini Profiler into ADO providers</strike>
- Settings
  - <strike>Connections</strike>
  - Actions
  - <strike>Page Sizes</strike>

#### Providers
- <strike>[Elasticsearch](https://github.com/dalenewman/Transformalize.Provider.Elasticsearch)</strike>, <strike>[SQL Server](https://github.com/dalenewman/Transformalize.Provider.SqlServer)</strike>, <strike>[PostgreSQL](https://github.com/dalenewman/Transformalize.Provider.PostgreSql)</strike>, <strike>[SQLite](https://github.com/dalenewman/Transformalize.Provider.SQLite)</strike>, <strike>[MySql](https://github.com/dalenewman/Transformalize.Provider.MySql)</strike>, <strike>[JSON](https://github.com/dalenewman/Transformalize.Provider.JSON)</strike>, <strike>[CSV](https://github.com/dalenewman/Transformalize.Provider.CsvHelper)</strike>, <strike>[Bogus](https://github.com/dalenewman/Transformalize.Provider.Bogus)</strike>
- [SOLR](https://github.com/dalenewman/Transformalize.Provider.SOLR), [GeoJson](https://github.com/dalenewman/Transformalize.Provider.GeoJson), [KML](https://github.com/dalenewman/Transformalize/tree/master/Providers/Kml), [Lucene](https://github.com/dalenewman/Transformalize.Provider.Lucene)

#### Transforms
- <strike>built-in [transforms](https://github.com/dalenewman/Transformalize/blob/master/Containers/Autofac/Transformalize.Container.Autofac.Shared/TransformBuilder.cs) and [validators](https://github.com/dalenewman/Transformalize/blob/master/Containers/Autofac/Transformalize.Container.Autofac.Shared/ValidateBuilder.cs)</strike>, <strike>[Jint](https://github.com/dalenewman/Transformalize.Transform.Jint
)</strike>, <strike>JSON</strike>, <strike>[Humanize](https://github.com/dalenewman/Transformalize.Transform.Humanizer)</strike>
- [Razor](https://github.com/dalenewman/Transformalize.Provider.Razor), [Liquid / Fluid](https://github.com/dalenewman/Transformalize.Transform.Fluid
), [Lambda Parser](https://github.com/dalenewman/Transformalize.Transform.LambdaParser)
- Orchard transforms: <strike>username()</strike>, <strike>userid()</strike>, <strike>useremail()</strike>.

#### Development Environment
- Visual Studio 2019 with ASP.NET Core related workloads:
  - ASP.NET and Web Development
  - .NET Core Cross-Platform Development
- Add nuget package source https://www.myget.org/F/transformalize/api/v3/index.json
- Set the Site as the startup project and run.
- Enable Transformalize in Dashboard > Configuration > Features
- Optionally run recipe in Dashboard > Configuration > Recipes

#### Module Dependencies
You may have to enable some of these manually.
- `OrchardCore.Users`
- `OrchardCore.Contents`
- `OrchardCore.Title`
- [`Etch.OrchardCore.ContentPermissions`](https://github.com/EtchUK/Etch.OrchardCore.ContentPermissions)
- `OrchardCore.MiniProfiler`
- `OrchardCore.Alias`
- `OrchardCore.ContentFields` <span style="color:red;">(new as of 2020-05-22)</span>

---

Using [Transformalize](https://github.com/dalenewman/Transformalize) for reporting basically means you 
read from one entity and do not specify an output.  The [bogus](https://github.com/dalenewman/Transformalize.Provider.Bogus) provider 
(see below) is fun to play with if you don't have any data sources.  To find out 
more about the providers, click on the links above.  Also, be sure to read 
the main [Transformalize](https://github.com/dalenewman/Transformalize) read me page.

![bogus report](bogus.gif)