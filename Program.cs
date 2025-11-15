WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

// Ensure production settings are configured for Azure
if (builder.Environment.IsProduction())
{
    var productionOverrides = new Dictionary<string, string?>();
    
    // Check if ApplicationUrl is missing or empty (environment variables override JSON, so check both)
    var appUrl = builder.Configuration["Umbraco:CMS:Global:ApplicationUrl"];
    var appUrlEnv = Environment.GetEnvironmentVariable("Umbraco__CMS__Global__ApplicationUrl");
    
    // Log for debugging (remove in production if needed)
    System.Diagnostics.Debug.WriteLine($"ApplicationUrl from config: {appUrl}");
    System.Diagnostics.Debug.WriteLine($"ApplicationUrl from env var: {appUrlEnv}");
    
    // Use environment variable if available, otherwise try WEBSITE_HOSTNAME, otherwise use config value
    if (!string.IsNullOrWhiteSpace(appUrlEnv))
    {
        productionOverrides["Umbraco:CMS:Global:ApplicationUrl"] = appUrlEnv.Trim();
    }
    else if (string.IsNullOrWhiteSpace(appUrl))
    {
        // Try to get from WEBSITE_HOSTNAME (Azure provides this automatically)
        var websiteHostname = Environment.GetEnvironmentVariable("WEBSITE_HOSTNAME");
        if (!string.IsNullOrWhiteSpace(websiteHostname))
        {
            productionOverrides["Umbraco:CMS:Global:ApplicationUrl"] = $"https://{websiteHostname}";
        }
    }
    else
    {
        // Use the value from config (appsettings.Production.json)
        productionOverrides["Umbraco:CMS:Global:ApplicationUrl"] = appUrl.Trim();
    }
    
    // Ensure UseHttps is set to true (check as string "true")
    var useHttps = builder.Configuration["Umbraco:CMS:Global:UseHttps"];
    var useHttpsEnv = Environment.GetEnvironmentVariable("Umbraco__CMS__Global__UseHttps");
    var useHttpsValue = !string.IsNullOrWhiteSpace(useHttpsEnv) ? useHttpsEnv : useHttps;
    if (useHttpsValue != "true" && useHttpsValue != "True")
    {
        productionOverrides["Umbraco:CMS:Global:UseHttps"] = "true";
    }
    
    // Ensure ModelsBuilder mode is set to Nothing
    var modelsMode = builder.Configuration["Umbraco:CMS:ModelsBuilder:ModelsMode"];
    var modelsModeEnv = Environment.GetEnvironmentVariable("Umbraco__CMS__ModelsBuilder__ModelsMode");
    var modelsModeValue = !string.IsNullOrWhiteSpace(modelsModeEnv) ? modelsModeEnv : modelsMode;
    if (modelsModeValue != "Nothing")
    {
        productionOverrides["Umbraco:CMS:ModelsBuilder:ModelsMode"] = "Nothing";
    }
    
    // Configure media path for Azure (fix the doubled wwwroot issue)
    var mediaPath = builder.Configuration["Umbraco:CMS:Global:UmbracoMediaPhysicalRootPath"];
    if (string.IsNullOrWhiteSpace(mediaPath))
    {
        var homePath = Environment.GetEnvironmentVariable("HOME");
        if (!string.IsNullOrWhiteSpace(homePath))
        {
            // Azure App Service: media folder is at wwwroot/wwwroot/media when deployed
            var mediaFullPath = Path.Combine(homePath, "site", "wwwroot", "wwwroot", "media");
            productionOverrides["Umbraco:CMS:Global:UmbracoMediaPhysicalRootPath"] = mediaFullPath;
            
            // Ensure the media directory exists
            try
            {
                if (!Directory.Exists(mediaFullPath))
                {
                    Directory.CreateDirectory(mediaFullPath);
                }
            }
            catch
            {
                // If we can't create it, Umbraco will handle the error
            }
        }
    }
    
    // Add production overrides to configuration sources (these take highest precedence)
    if (productionOverrides.Count > 0)
    {
        builder.Configuration.AddInMemoryCollection(productionOverrides);
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

