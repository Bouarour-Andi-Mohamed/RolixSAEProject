using System.Globalization;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.PowerPlatform.Dataverse.Client;
using RolixSAEProject.Filters;
using RolixSAEProject.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<CustomerAuthService>();

// Localisation (RESX) — dossier Resources
builder.Services.AddLocalization(options => options.ResourcesPath = "Resources");

// MVC + filtre devise
builder.Services.AddControllersWithViews(options =>
{
    options.Filters.Add<CurrencyViewDataFilter>();
})
.AddViewLocalization(LanguageViewLocationExpanderFormat.Suffix);

// ✅ SESSION (obligatoire si tu fais app.UseSession())
builder.Services.AddDistributedMemoryCache(); // fournit IDistributedCache -> ISessionStore
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromHours(2);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;   // utile si tu as un consentement cookies
    options.Cookie.SameSite = SameSiteMode.Lax;
});

// UN SEUL ServiceClient (partagé par tout le site)
builder.Services.AddSingleton<ServiceClient>(sp =>
{
    var cfg = sp.GetRequiredService<IConfiguration>();

    var url = cfg["Dataverse:Url"];
    var clientId = cfg["Dataverse:ClientId"];
    var clientSecret = cfg["Dataverse:ClientSecret"];
    var tenantId = cfg["Dataverse:TenantId"];

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

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

// ✅ Session doit être avant Authorization / endpoints
app.UseSession();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
