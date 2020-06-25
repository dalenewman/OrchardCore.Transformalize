using OrchardCore.Modules.Manifest;

[assembly: Module(
    Name = "Transformalize",
    Author = "Dale Newman",
    Website = "https://github.com/dalenewman/Transformalize",
    Version = "0.8.15",
    Description = "Transformalize",
    Category = "ETL",
    Dependencies = new[]{
        "OrchardCore.Users",
        "OrchardCore.Contents",
        "OrchardCore.ContentFields",
        "OrchardCore.Title",
        "Etch.OrchardCore.ContentPermissions",
        "OrchardCore.MiniProfiler",
        "OrchardCore.Alias" // using alias until autoroute works for custom content item controllers
    }
)]
