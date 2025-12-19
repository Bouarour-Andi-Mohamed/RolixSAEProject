using System.Globalization;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.PowerPlatform.Dataverse.Client;
using RolixSAEProject.Filters;
using RolixSAEProject.Services;

var builder = WebApplication.CreateBuilder(args);

// Localisation (RESX) — dossier Resources
builder.Services.AddLocalization(options => options.ResourcesPath = "Resources");

// MVC + filtre devise
builder.Services.AddControllersWithViews(options =>
{
    options.Filters.Add<CurrencyViewDataFilter>();
})
.AddViewLocalization(LanguageViewLocationExpanderFormat.Suffix);

// UN SEUL ServiceClient (partagé par tout le site)
builder.Services.AddSingleton<ServiceClient>(sp =>
{
    var cfg = sp.GetRequiredService<IConfiguration>();

    var url = cfg["Dataverse:Url"];
    var clientId = cfg["Dataverse:ClientId"];
    var clientSecret = cfg["Dataverse:ClientSecret"];
    var tenantId = cfg["Dataverse:TenantId"];

    // Si ClientSecret est renseigné => pas de popup login (le plus clean)
    var hasClientSecret =
        !string.IsNullOrWhiteSpace(clientId) && clientId != "<A REMPLACER PLUS TARD>" &&
        !string.IsNullOrWhiteSpace(clientSecret) && clientSecret != "<A REMPLACER PLUS TARD>";

    string connStr;

    if (hasClientSecret)
    {
        connStr =
            $"AuthType=ClientSecret;" +
            $"Url={url};" +
            $"ClientId={clientId};" +
            $"ClientSecret={clientSecret};" +
            (!string.IsNullOrWhiteSpace(tenantId) && tenantId != "<A REMPLACER PLUS TARD>" ? $"TenantId={tenantId};" : "");
    }
    else
    {
        // Fallback DEV: OAuth interactif (comme ton code actuel)
        connStr =
            $"AuthType=OAuth;" +
            $"Url={url};" +
            $"AppId=51f81489-12ee-4a9e-aaae-a2591f45987d;" +
            $"RedirectUri=http://localhost;" +
            $"LoginPrompt=Auto;";
    }

    return new ServiceClient(connStr);
});

// Services qui utilisent le même client
builder.Services.AddSingleton<DataverseService>();
builder.Services.AddSingleton<SiteContentService>();

var app = builder.Build();

// Localisation middleware (FR par défaut)
var supportedCultures = new[] { new CultureInfo("fr"), new CultureInfo("en") };
app.UseRequestLocalization(new RequestLocalizationOptions
{
    DefaultRequestCulture = new RequestCulture("fr"),
    SupportedCultures = supportedCultures,
    SupportedUICultures = supportedCultures
});

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
