## Transformalize in Orchard Core

This is an [Orchard Core](https://github.com/OrchardCMS/OrchardCore) module 
that uses [Transformalize](https://github.com/dalenewman/Transformalize) 
arrangements to create:

- **Reports**: Browse and Search for data.
- **Tasks**: Validate & transform parameters for use in data modification statements.
- **Forms**: Validate & transform form data for data collection. 
- **Bulk Actions**: Tasks run on records selected from reports.

### Reports
Reports read, filter, search, export, and page over data. 
You may transform records using [Razor](https://docs.microsoft.com/en-us/aspnet/core/mvc/views/razor?view=aspnetcore-3.1) 
, [Liquid](https://shopify.github.io/liquid), and built-in transforms. 
If the records have coordinates, you may display them on a map.

`TODO: Explain example arrangement and show GIF.`

### Tasks

Tasks accept, transform, and validate parameters before running 
traditional [transformalize](https://github.com/dalenewman/Transformalize/blob/master/README.md) 
or simple data modification processes.

`TODO: Explain example arrangement and show GIF.`


### Forms
Building off the ability to validate parameters, 
forms collect valid user supplied input and store it 
in a relational provider (a specified table).

`TODO: Explain example arrangement and show GIF.`

### Bulk Actions
Bulk Actions combine reports and tasks. You may 
select records on a report, and send them to a 
task for processing.

<!--Actions are added to report arrangements like this:

```xml
<cfg name="report">
   <actions>
      <add name="task-alias" description="a description" />
   </actions>
</cfg>
```
-->

Five configurable tasks must be defined in order to run bulk actions:

1. `batch-create`: create and return a batch identifier
1. `batch-write`: write batch values.
1. `batch-summary`: gather review and result summary for a batch.
1. `batch-run`: indicate the task is running (not yet implemented)
1. `batch-success`: indicate the task succeeded
1. `batch-fail`: indicate the task failed

A recipe named "Transformalize Batches SQLite" provides an 
example set of the above tasks.

---

Putting it all together, here is a GIF showing a report (with map) that has a bulk 
action to change color associated with the record.

![bogus report](src/Site/App_Data/samples/sacramento-crime/criminal-bulk-actions.gif)

`TODO: Link to arrangement here.`

---

### Development
- Visual Studio 2019 with ASP.NET Core related workloads:
  - ASP.NET and Web Development
  - .NET Core Cross-Platform Development
- Relies on nuget source https://www.myget.org/F/transformalize/api/v3/index.json
- **Caution**: This project is still under development.