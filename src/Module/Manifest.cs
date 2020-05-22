using OrchardCore.Modules.Manifest;

[assembly: Module(
    Name = "Transformalize",
    Author = "Dale Newman",
    Website = "https://github.com/dalenewman/Transformalize",
    Version = "0.7.7",
    Description = "Transformalize",
    Category = "Reporting",
    Dependencies = new[]{
        "OrchardCore.Users",
        "OrchardCore.Contents",
        "OrchardCore.Title",
        "Etch.OrchardCore.ContentPermissions",
        "OrchardCore.MiniProfiler",
        "OrchardCore.Alias" // using alias until autoroute works for custom content item controllers
    }
)]
