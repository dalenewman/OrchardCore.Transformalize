using OrchardCore.Modules.Manifest;

[assembly: Module(
    Name = "OrchardCore.Transformalize",
    Author = "Dale Newman",
    Website = "https://github.com/dalenewman/OrchardCore.Transformalize",
    Version = "0.8.20",
    Description = "An OrchardCore module that wants to Transformalize everything",
    Category = "ETL",
    Dependencies = new[]{
        "Etch.OrchardCore.ContentPermissions",
        "OrchardCore.Alias",
        "OrchardCore.ContentFields",
        "OrchardCore.Contents",
        "OrchardCore.MiniProfiler",
        "OrchardCore.Title",
        "OrchardCore.Users",
        "OrchardCore.Workflows"
    }
)]
