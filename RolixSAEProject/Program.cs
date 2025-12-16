using System.Globalization;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc.Razor;
using RolixSAEProject.Filters;
using RolixSAEProject.Services;

var builder = WebApplication.CreateBuilder(args);

// Localisation (RESX) — dossier Resources
builder.Services.AddLocalization(options => options.ResourcesPath = "Resources");

// Add services to the container.
builder.Services.AddControllersWithViews(options =>
{
    options.Filters.Add<CurrencyViewDataFilter>();
})
.AddViewLocalization(LanguageViewLocationExpanderFormat.Suffix); // active Views/Home/Index.fr.resx etc.

// Service Dataverse disponible en injection dans les contrôleurs
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
