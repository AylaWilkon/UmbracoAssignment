// Set environment variables BEFORE creating the builder to ensure they're picked up
// This is critical for Azure App Service where environment variables might not be set correctly
var aspNetCoreEnvironment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
if (aspNetCoreEnvironment == "Production")
{
    // Ensure ApplicationUrl is set
    var appUrlEnv = Environment.GetEnvironmentVariable("Umbraco__CMS__Global__ApplicationUrl");
    if (string.IsNullOrWhiteSpace(appUrlEnv))
    {
        var websiteHostname = Environment.GetEnvironmentVariable("WEBSITE_HOSTNAME");
        if (!string.IsNullOrWhiteSpace(websiteHostname))
        {
            Environment.SetEnvironmentVariable("Umbraco__CMS__Global__ApplicationUrl", $"https://{websiteHostname}");
        }
    }
    
    // Ensure UseHttps is set
    var useHttpsEnv = Environment.GetEnvironmentVariable("Umbraco__CMS__Global__UseHttps");
    if (string.IsNullOrWhiteSpace(useHttpsEnv) || useHttpsEnv != "true")
    {
        Environment.SetEnvironmentVariable("Umbraco__CMS__Global__UseHttps", "true");
    }
    
    // Ensure ModelsBuilder mode is set
    var modelsModeEnv = Environment.GetEnvironmentVariable("Umbraco__CMS__ModelsBuilder__ModelsMode");
    if (string.IsNullOrWhiteSpace(modelsModeEnv) || modelsModeEnv != "Nothing")
    {
        Environment.SetEnvironmentVariable("Umbraco__CMS__ModelsBuilder__ModelsMode", "Nothing");
    }
    
    // Configure media path for Azure
    var mediaPathEnv = Environment.GetEnvironmentVariable("Umbraco__CMS__Global__UmbracoMediaPhysicalRootPath");
    if (string.IsNullOrWhiteSpace(mediaPathEnv))
    {
        var homePath = Environment.GetEnvironmentVariable("HOME");
        if (!string.IsNullOrWhiteSpace(homePath))
        {
            var mediaFullPath = Path.Combine(homePath, "site", "wwwroot", "wwwroot", "media");
            Environment.SetEnvironmentVariable("Umbraco__CMS__Global__UmbracoMediaPhysicalRootPath", mediaFullPath);
            
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
}

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

// Double-check and ensure production settings are in configuration
if (builder.Environment.IsProduction())
{
    var productionOverrides = new Dictionary<string, string?>();
    
    // Verify ApplicationUrl is set
    var appUrl = builder.Configuration["Umbraco:CMS:Global:ApplicationUrl"];
    if (string.IsNullOrWhiteSpace(appUrl))
    {
        var appUrlEnv = Environment.GetEnvironmentVariable("Umbraco__CMS__Global__ApplicationUrl");
        if (!string.IsNullOrWhiteSpace(appUrlEnv))
        {
            productionOverrides["Umbraco:CMS:Global:ApplicationUrl"] = appUrlEnv.Trim();
        }
        else
        {
            var websiteHostname = Environment.GetEnvironmentVariable("WEBSITE_HOSTNAME");
            if (!string.IsNullOrWhiteSpace(websiteHostname))
            {
                productionOverrides["Umbraco:CMS:Global:ApplicationUrl"] = $"https://{websiteHostname}";
            }
        }
    }
    
    // Verify UseHttps is set
    var useHttps = builder.Configuration["Umbraco:CMS:Global:UseHttps"];
    if (useHttps != "true" && useHttps != "True")
    {
        productionOverrides["Umbraco:CMS:Global:UseHttps"] = "true";
    }
    
    // Verify ModelsBuilder mode is set
    var modelsMode = builder.Configuration["Umbraco:CMS:ModelsBuilder:ModelsMode"];
    if (modelsMode != "Nothing")
    {
        productionOverrides["Umbraco:CMS:ModelsBuilder:ModelsMode"] = "Nothing";
    }
    
    // Verify media path is set
    var mediaPath = builder.Configuration["Umbraco:CMS:Global:UmbracoMediaPhysicalRootPath"];
    if (string.IsNullOrWhiteSpace(mediaPath))
    {
        var mediaPathEnv = Environment.GetEnvironmentVariable("Umbraco__CMS__Global__UmbracoMediaPhysicalRootPath");
        if (!string.IsNullOrWhiteSpace(mediaPathEnv))
        {
            productionOverrides["Umbraco:CMS:Global:UmbracoMediaPhysicalRootPath"] = mediaPathEnv;
        }
    }
    
    // Add overrides to configuration if needed
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

