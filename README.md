An effort to put [Transformalize](https://github.com/dalenewman/Transformalize) 
in [Orchard Core](https://github.com/OrchardCMS/OrchardCore).  Currently targets 
Orchard Core 1.0.0-rc1 Nuget packages.

### Providers
Converted to .NET Standard, Modify for Orchard Core, etc.

- <strike>Elasticsearch</strike>
- <strike>SQL Server</strike>
- <strike>PostgreSQL</strike>
- <strike>SQLite</strike>
- MySql
- <strike>JSON</strike>
- <strike>CSV</strike>
- <strike>Bogus</strike>
- SOLR

#### Transforms

Converted to .NET Standard, Modified for Orchard Core, etc.
- <strike>Jint</strike>, should tap into Orchard Core's service
- <strike>JSON</strike>
- Razor
- Humanize
- Liquid / Fluid, should tap into Orchard Core's service

#### Web Features

- <strike>Code Mirror XML Editor</strike>
- Reporting (about 50%)
- Tasks (ETL Jobs), Schedule? Background?
- <strike>CSV Extract</strike>, no more sync writes to stream
- <strike>JSON Extract</strike>, no more sync writes to stream
- Batches / Bulk Actions
- Forms (simple one page forms with transforms / validators)
- OpenId Connect / OAuth secured API to tasks
Caution:  This currently relies on Nuget packages
that are not yet published so don't bother trying
to get it running on your environment yet.