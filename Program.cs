WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

// Configure production settings for Azure
if (builder.Environment.IsProduction())
{
    var overrides = new Dictionary<string, string?>();
    
    // ApplicationUrl - use environment variable, config, or WEBSITE_HOSTNAME
    var appUrl = builder.Configuration["Umbraco:CMS:Global:ApplicationUrl"] 
                 ?? Environment.GetEnvironmentVariable("Umbraco__CMS__Global__ApplicationUrl");
    
    if (string.IsNullOrWhiteSpace(appUrl))
    {
        var hostname = Environment.GetEnvironmentVariable("WEBSITE_HOSTNAME");
        if (!string.IsNullOrWhiteSpace(hostname))
        {
            appUrl = $"https://{hostname}";
        }
    }
    
    if (!string.IsNullOrWhiteSpace(appUrl))
    {
        overrides["Umbraco:CMS:Global:ApplicationUrl"] = appUrl.Trim();
    }
    
    // UseHttps - must be true in production
    overrides["Umbraco:CMS:Global:UseHttps"] = "true";
    
    // ModelsBuilder - must be Nothing in production
    overrides["Umbraco:CMS:ModelsBuilder:ModelsMode"] = "Nothing";
    
    // Apply overrides with highest precedence
    if (overrides.Count > 0)
    {
        builder.Configuration.Sources.Insert(0, 
            new Microsoft.Extensions.Configuration.Memory.MemoryConfigurationSource 
            { 
                InitialData = overrides 
            });
    }
    
    // DIAGNOSTIC: Log where UmbracoMediaPhysicalRootPath is coming from
    var mediaPathFromConfig = builder.Configuration["Umbraco:CMS:Global:UmbracoMediaPhysicalRootPath"];
    var mediaPathFromEnv = Environment.GetEnvironmentVariable("Umbraco__CMS__Global__UmbracoMediaPhysicalRootPath");
    var mediaPathFromEnvTruncated = Environment.GetEnvironmentVariable("Umbraco__CMS__Global__UmbracoMediaPhysic");
    
    Console.WriteLine($"[DIAGNOSTIC] UmbracoMediaPhysicalRootPath from Configuration: '{mediaPathFromConfig}'");
    Console.WriteLine($"[DIAGNOSTIC] UmbracoMediaPhysicalRootPath from Env Var (full): '{mediaPathFromEnv}'");
    Console.WriteLine($"[DIAGNOSTIC] UmbracoMediaPhysicalRootPath from Env Var (truncated): '{mediaPathFromEnvTruncated}'");
    
    // Log all Umbraco-related environment variables
    var allEnvVars = Environment.GetEnvironmentVariables();
    foreach (var key in allEnvVars.Keys)
    {
        var keyStr = key.ToString();
        if (keyStr != null && keyStr.Contains("Umbraco", StringComparison.OrdinalIgnoreCase))
        {
            Console.WriteLine($"[DIAGNOSTIC] Env Var: {keyStr} = '{allEnvVars[key]}'");
        }
    }
}

builder.CreateUmbracoBuilder()
    .AddBackOffice()
    .AddWebsite()
    .AddComposers()
    .Build();

WebApplication app = builder.Build();

await app.BootUmbracoAsync();

app.UseHttpsRedirection();

app.UseUmbraco()
    .WithMiddleware(u =>
    {
        u.UseBackOffice();
        u.UseWebsite();
    })
    .WithEndpoints(u =>
    {
        u.UseBackOfficeEndpoints();
        u.UseWebsiteEndpoints();
    });

await app.RunAsync();

