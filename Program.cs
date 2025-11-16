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

