A [Transformalize](https://github.com/dalenewman/Transformalize) 
module for [Orchard Core](https://github.com/OrchardCMS/OrchardCore). 

#### Features
- <strike>Code Mirror XML Editor</strike>
- Reporting
   - <strike>Sortable, Filterable Table with pagination</strike>
   - <strike>CSV Extract</strike>
   - <strike>JSON Extract</strike>
   - Map(Box) view
   - Calendar view
- Batches / Bulk Actions
  - Tasks (ETL Jobs) for running batches, etc
  - Forms for accepting parameters for batches
- <strike>Add Mini Profiler into ADO providers</strike>

#### Providers
- <strike>[Elasticsearch](https://github.com/dalenewman/Transformalize.Provider.Elasticsearch)</strike>, <strike>[SQL Server](https://github.com/dalenewman/Transformalize.Provider.SqlServer)</strike>, <strike>[PostgreSQL](https://github.com/dalenewman/Transformalize.Provider.PostgreSql)</strike>, <strike>[SQLite](https://github.com/dalenewman/Transformalize.Provider.SQLite)</strike>, <strike>[MySql](https://github.com/dalenewman/Transformalize.Provider.MySql)</strike>, <strike>[JSON](https://github.com/dalenewman/Transformalize.Provider.JSON)</strike>, <strike>[CSV](https://github.com/dalenewman/Transformalize.Provider.CsvHelper)</strike>, <strike>[Bogus](https://github.com/dalenewman/Transformalize.Provider.Bogus)</strike>
- [SOLR](https://github.com/dalenewman/Transformalize.Provider.SOLR), [GeoJson](https://github.com/dalenewman/Transformalize.Provider.GeoJson), [KML](https://github.com/dalenewman/Transformalize/tree/master/Providers/Kml), [Lucene](https://github.com/dalenewman/Transformalize.Provider.Lucene)

#### Transforms
- <strike>built-in [transforms](https://github.com/dalenewman/Transformalize/blob/master/Containers/Autofac/Transformalize.Container.Autofac.Shared/TransformBuilder.cs) and [validators](https://github.com/dalenewman/Transformalize/blob/master/Containers/Autofac/Transformalize.Container.Autofac.Shared/ValidateBuilder.cs)</strike>, <strike>[Jint](https://github.com/dalenewman/Transformalize.Transform.Jint
)</strike>, <strike>JSON</strike>, <strike>[Humanize](https://github.com/dalenewman/Transformalize.Transform.Humanizer)</strike>
- [Razor](https://github.com/dalenewman/Transformalize.Provider.Razor), [Liquid / Fluid](https://github.com/dalenewman/Transformalize.Transform.Fluid
), [Lambda Parser](https://github.com/dalenewman/Transformalize.Transform.LambdaParser)

#### Development Environment
- Visual Studio 2019 with .NET Core cross-platform development workload
- Add nuget package source https://www.myget.org/F/transformalize/api/v3/index.json
- Set the Site as the startup project.
- Goto the Dashboard > Configuration > Features, search for Transformalize and enable it.

---

Using [Transformalize](https://github.com/dalenewman/Transformalize) for reporting means you 
read from one entity.  The [bogus](https://github.com/dalenewman/Transformalize.Provider.Bogus) provider 
(see below) is fun to play with if you don't have any data sources. 

![bogus report](bogus.gif)