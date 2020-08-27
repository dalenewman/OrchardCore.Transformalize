using OrchardCore.Modules.Manifest;

[assembly: Module(
    Name = "Transformalize",
    Author = "Dale Newman",
    Website = "https://github.com/dalenewman/OrchardCore.Transformalize",
    Version = "0.8.21",
    Description = "This module wants to Transformalize everything.",
    Category = "ETL",
    Dependencies = new[]{
        "Etch.OrchardCore.ContentPermissions",
        "OrchardCore.Alias",
        "OrchardCore.ContentFields",
        "OrchardCore.Contents",
        "OrchardCore.Title",
        "OrchardCore.Users",
        "OrchardCore.Workflows"
    }
)]