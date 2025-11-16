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
    // Note: UmbracoMediaPhysicalRootPath should be relative, not absolute
    // If it's set to an absolute path in environment variables, remove it
    var mediaPathEnv = Environment.GetEnvironmentVariable("Umbraco__CMS__Global__UmbracoMediaPhysicalRootPath");
    var mediaPathEnvTruncated = Environment.GetEnvironmentVariable("Umbraco__CMS__Global__UmbracoMediaPhysic");
    
    // Remove absolute paths from environment variables
    if (!string.IsNullOrWhiteSpace(mediaPathEnv) && Path.IsPathRooted(mediaPathEnv))
    {
        Environment.SetEnvironmentVariable("Umbraco__CMS__Global__UmbracoMediaPhysicalRootPath", null);
    }
    if (!string.IsNullOrWhiteSpace(mediaPathEnvTruncated) && Path.IsPathRooted(mediaPathEnvTruncated))
    {
        Environment.SetEnvironmentVariable("Umbraco__CMS__Global__UmbracoMediaPhysic", null);
    }
    
    // Ensure the media directory exists
    var homePath = Environment.GetEnvironmentVariable("HOME");
    if (!string.IsNullOrWhiteSpace(homePath))
    {
        var mediaFullPath = Path.Combine(homePath, "site", "wwwroot", "wwwroot", "media");
        
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

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

// Double-check and ensure production settings are in configuration
if (builder.Environment.IsProduction())
{
    var productionOverrides = new Dictionary<string, string?>();
    
    // Verify ApplicationUrl is set - ALWAYS set it explicitly to ensure Umbraco reads it
    var appUrl = builder.Configuration["Umbraco:CMS:Global:ApplicationUrl"];
    var appUrlEnv = Environment.GetEnvironmentVariable("Umbraco__CMS__Global__ApplicationUrl");
    
    string? finalAppUrl = null;
    if (!string.IsNullOrWhiteSpace(appUrl))
    {
        finalAppUrl = appUrl.Trim();
    }
    else if (!string.IsNullOrWhiteSpace(appUrlEnv))
    {
        finalAppUrl = appUrlEnv.Trim();
    }
    else
    {
        var websiteHostname = Environment.GetEnvironmentVariable("WEBSITE_HOSTNAME");
        if (!string.IsNullOrWhiteSpace(websiteHostname))
        {
            finalAppUrl = $"https://{websiteHostname}";
        }
    }
    
    // Always set it in overrides to ensure it's available
    if (!string.IsNullOrWhiteSpace(finalAppUrl))
    {
        productionOverrides["Umbraco:CMS:Global:ApplicationUrl"] = finalAppUrl;
    }
    
    // Verify UseHttps is set - ALWAYS set it explicitly
    var useHttps = builder.Configuration["Umbraco:CMS:Global:UseHttps"];
    var useHttpsEnv = Environment.GetEnvironmentVariable("Umbraco__CMS__Global__UseHttps");
    var useHttpsValue = !string.IsNullOrWhiteSpace(useHttpsEnv) ? useHttpsEnv : useHttps;
    if (useHttpsValue != "true" && useHttpsValue != "True")
    {
        productionOverrides["Umbraco:CMS:Global:UseHttps"] = "true";
    }
    else
    {
        productionOverrides["Umbraco:CMS:Global:UseHttps"] = "true"; // Always set to ensure it's available
    }
    
    // Verify ModelsBuilder mode is set - ALWAYS set it explicitly
    var modelsMode = builder.Configuration["Umbraco:CMS:ModelsBuilder:ModelsMode"];
    var modelsModeEnv = Environment.GetEnvironmentVariable("Umbraco__CMS__ModelsBuilder__ModelsMode");
    var modelsModeValue = !string.IsNullOrWhiteSpace(modelsModeEnv) ? modelsModeEnv : modelsMode;
    if (modelsModeValue != "Nothing")
    {
        productionOverrides["Umbraco:CMS:ModelsBuilder:ModelsMode"] = "Nothing";
    }
    else
    {
        productionOverrides["Umbraco:CMS:ModelsBuilder:ModelsMode"] = "Nothing"; // Always set to ensure it's available
    }
    
    // CRITICAL: Always ensure UmbracoMediaPhysicalRootPath is not set to an absolute path
    // Umbraco requires relative paths - absolute paths cause "MapPathWebRoot" errors
    // Check all possible sources and clear it if it's absolute
    var mediaPath = builder.Configuration["Umbraco:CMS:Global:UmbracoMediaPhysicalRootPath"];
    var mediaPathEnv = Environment.GetEnvironmentVariable("Umbraco__CMS__Global__UmbracoMediaPhysicalRootPath");
    var mediaPathEnvTruncated = Environment.GetEnvironmentVariable("Umbraco__CMS__Global__UmbracoMediaPhysic");
    
    // Check if any source has an absolute path - if so, clear it
    bool needsClearing = false;
    if (!string.IsNullOrWhiteSpace(mediaPath) && Path.IsPathRooted(mediaPath))
    {
        needsClearing = true;
    }
    if (!string.IsNullOrWhiteSpace(mediaPathEnv) && Path.IsPathRooted(mediaPathEnv))
    {
        needsClearing = true;
    }
    if (!string.IsNullOrWhiteSpace(mediaPathEnvTruncated) && Path.IsPathRooted(mediaPathEnvTruncated))
    {
        needsClearing = true;
    }
    
    // If any source has an absolute path, clear it completely
    // Also check if it exists in configuration at all - if so, verify it's not absolute
    if (needsClearing || !string.IsNullOrWhiteSpace(mediaPath))
    {
        // Always clear it if it exists - let Umbraco use its default relative path
        // Setting to empty string will override any existing value
        productionOverrides["Umbraco:CMS:Global:UmbracoMediaPhysicalRootPath"] = string.Empty;
    }
    
    // Add overrides to configuration - insert at the beginning to ensure highest precedence
    if (productionOverrides.Count > 0)
    {
        // Insert at the beginning so it overrides environment variables and appsettings
        builder.Configuration.Sources.Insert(0, 
            new Microsoft.Extensions.Configuration.Memory.MemoryConfigurationSource 
            { 
                InitialData = productionOverrides 
            });
    }
}

var umbracoBuilder = builder.CreateUmbracoBuilder()
    .AddBackOffice()
    .AddWebsite()
    .AddComposers();

// Verify configuration is set before Umbraco builder reads it
if (builder.Environment.IsProduction())
{
    var appUrl = builder.Configuration["Umbraco:CMS:Global:ApplicationUrl"];
    if (string.IsNullOrWhiteSpace(appUrl))
    {
        // Final fallback - add to configuration sources if still missing
        var websiteHostname = Environment.GetEnvironmentVariable("WEBSITE_HOSTNAME");
        if (!string.IsNullOrWhiteSpace(websiteHostname))
        {
            builder.Configuration.Sources.Insert(0, 
                new Microsoft.Extensions.Configuration.Memory.MemoryConfigurationSource 
                { 
                    InitialData = new Dictionary<string, string?>
                    {
                        ["Umbraco:CMS:Global:ApplicationUrl"] = $"https://{websiteHostname}"
                    }
                });
        }
    }
}

umbracoBuilder.Build();

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

