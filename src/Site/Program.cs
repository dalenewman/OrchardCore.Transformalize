using Microsoft.AspNetCore.HttpOverrides;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((hostingContext, configBuilder) => {
   configBuilder.ReadFrom.Configuration(hostingContext.Configuration)
   .Enrich.FromLogContext();
});

var orchardBuilder = builder.Services.AddOrchardCms();

// Only use database shells if the configuration is provided
var shellDbConfig = builder.Configuration.GetSection("OrchardCore:OrchardCore_Shells_Database");
if (shellDbConfig.Exists() && !string.IsNullOrEmpty(shellDbConfig["ConnectionString"]))
{
    orchardBuilder.AddDatabaseShellsConfiguration();
}

orchardBuilder.AddSetupFeatures("OrchardCore.AutoSetup");

// // Orchard Specific Pipeline
// .ConfigureServices( services => {
// })
// .Configure( (app, routes, services) => {
// })

var app = builder.Build();

if (!app.Environment.IsDevelopment()) {
   app.UseExceptionHandler("/Error");
   // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
   app.UseHsts();
}

// Trust X-Forwarded-For and X-Forwarded-Proto headers so that generated links use the correct
// scheme (https) when the app runs in a private subnet behind a public-facing reverse proxy
// (e.g. AWS ALB, Azure Application Gateway). Without this, the container only sees http and
// generates http:// links, which can cause auth cookies to be stripped on redirect and break
// downloads. KnownIPNetworks/KnownProxies are cleared so any upstream proxy is trusted —
// this is safe when the container is not directly reachable from the internet, but would allow
// header spoofing if the app were exposed publicly without a proxy in front of it.
var forwardedOptions = new ForwardedHeadersOptions {
   ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
};
forwardedOptions.KnownIPNetworks.Clear();
forwardedOptions.KnownProxies.Clear();
app.UseForwardedHeaders(forwardedOptions);

// app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseOrchardCore(c => c.UseSerilogRequestLogging());

app.Run();
