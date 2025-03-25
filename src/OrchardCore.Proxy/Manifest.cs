using OrchardCore.Modules.Manifest;

[assembly: Module(
    Name = "OrchardCore.Proxy",
    Author = "Dale Newman",
    Website = "https://github.com/dalenewman/OrchardCore.Proxy",
    Version = "0.13.1",
    Description = "for secured access to internal resources",
    Category = "Security",
    Dependencies = new[]{
        "OrchardCoreContrib.ContentPermissions"
    }
)]
