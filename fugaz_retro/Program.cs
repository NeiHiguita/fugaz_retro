using fugaz_retro.Models;
using fugaz_retro.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Localization;
using System.Globalization;
using Pomelo.EntityFrameworkCore.MySql;

var builder = WebApplication.CreateBuilder(args);

// Agregar la configuración de la sesión
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30); // Define el tiempo de espera de la sesión
});

// Configuración de localización
builder.Services.AddLocalization(options => options.ResourcesPath = "Resources");

builder.Services.AddControllersWithViews()
    .AddDataAnnotationsLocalization()
    .AddViewLocalization()
    .AddNewtonsoftJson(options =>
    {
        options.SerializerSettings.ReferenceLoopHandling = Newtonsoft.Json.ReferenceLoopHandling.Ignore;
    });

builder.Services.Configure<RequestLocalizationOptions>(options =>
{
    var supportedCultures = new[] { new CultureInfo("es-CO") };
    options.DefaultRequestCulture = new RequestCulture("es-CO");
    options.SupportedCultures = supportedCultures;
    options.SupportedUICultures = supportedCultures;

    // Configuración para que el separador decimal sea el punto y la coma sea el separador de miles
    options.RequestCultureProviders.Clear();
    options.RequestCultureProviders.Add(new CustomRequestCultureProvider(context =>
    {
        var defaultCulture = new CultureInfo("es-CO");
        defaultCulture.NumberFormat.CurrencyDecimalSeparator = ".";
        defaultCulture.NumberFormat.CurrencyGroupSeparator = ",";
        defaultCulture.NumberFormat.NumberDecimalSeparator = ".";
        defaultCulture.NumberFormat.NumberGroupSeparator = ",";
        return Task.FromResult(new ProviderCultureResult("es-CO"));
    }));
});

builder.Services.AddDbContext<FugazContext>(options =>
    options.UseMySql(builder.Configuration.GetConnectionString("conexion"), new MySqlServerVersion(new Version(5, 7))));

builder.Services.AddDefaultIdentity<IdentityUser>(options => options.SignIn.RequireConfirmedAccount = true)
    .AddEntityFrameworkStores<FugazContext>();

builder.Services.AddScoped<IEmailService, EmailService>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
}

app.UseStaticFiles();

app.UseRouting();

app.UseRequestLocalization(); 

app.UseSession();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapRazorPages();

app.Run();
