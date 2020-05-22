using OrchardCore.Modules.Manifest;

[assembly: Module(
    Name = "Transformalize",
    Author = "Dale Newman",
    Website = "https://github.com/dalenewman/Transformalize",
    Version = "0.7.8",
    Description = "Transformalize",
    Category = "Reporting",
    Dependencies = new[]{
        "OrchardCore.Users",
        "OrchardCore.Users.Core",
        "OrchardCore.Contents",
        "OrchardCore.ContentFields",
        "OrchardCore.Title",
        "Etch.OrchardCore.ContentPermissions",
        "OrchardCore.MiniProfiler",
        "OrchardCore.Alias" // using alias until autoroute works for custom content item controllers
    }
)]
