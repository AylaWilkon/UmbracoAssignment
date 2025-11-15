WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

// Ensure production settings are configured for Azure
if (builder.Environment.IsProduction())
{
    var config = builder.Configuration;
    
    // Build a dictionary of settings to override if not already set
    var productionSettings = new Dictionary<string, string?>();
    
    // Set ApplicationUrl if not already configured (use Azure's WEBSITE_HOSTNAME)
    var applicationUrl = config["Umbraco:CMS:Global:ApplicationUrl"];
    if (string.IsNullOrWhiteSpace(applicationUrl))
    {
        var websiteHostname = Environment.GetEnvironmentVariable("WEBSITE_HOSTNAME");
        if (!string.IsNullOrWhiteSpace(websiteHostname))
        {
            var useHttps = config["Umbraco:CMS:Global:UseHttps"] == "true";
            var scheme = useHttps ? "https" : "http";
            applicationUrl = $"{scheme}://{websiteHostname}";
            productionSettings["Umbraco:CMS:Global:ApplicationUrl"] = applicationUrl;
        }
    }
    
    // Ensure UseHttps is set to true in production
    if (config["Umbraco:CMS:Global:UseHttps"] != "true")
    {
        productionSettings["Umbraco:CMS:Global:UseHttps"] = "true";
    }
    
    // Ensure ModelsBuilder mode is set to Nothing in production
    if (config["Umbraco:CMS:ModelsBuilder:ModelsMode"] != "Nothing")
    {
        productionSettings["Umbraco:CMS:ModelsBuilder:ModelsMode"] = "Nothing";
    }
    
    // Add the production settings to configuration if any were set
    if (productionSettings.Count > 0)
    {
        builder.Configuration.AddInMemoryCollection(productionSettings);
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
