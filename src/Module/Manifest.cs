using OrchardCore.Modules.Manifest;

[assembly: Module(
    Name = "Transformalize",
    Author = "Dale Newman",
    Website = "https://github.com/dalenewman/Transformalize",
    Version = "0.6.28",
    Description = "Transformalize",
    Category = "Content",
    Dependencies = new[]{
        "OrchardCore.Users",
        "OrchardCore.Contents",
        "OrchardCore.Title",
        "Etch.OrchardCore.ContentPermissions",
        "OrchardCore.MiniProfiler",
        "OrchardCore.Alias" // using alias until autoroute works for custom content item controllers
    }
)]
