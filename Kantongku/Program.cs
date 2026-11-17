using System.Globalization;
using Kantongku.Data;
using Kantongku.Helpers;
using Kantongku.Models;
using Kantongku.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MySqlConnector;

var builder = WebApplication.CreateBuilder(args);

// ---------------------------------------------------------------------
// Database (MySQL / MariaDB XAMPP via Pomelo)
// ---------------------------------------------------------------------
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("ConnectionStrings:DefaultConnection belum diatur di appsettings.json.");

var serverVersion = ResolveServerVersion(connectionString);

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseMySql(connectionString, serverVersion));

// ---------------------------------------------------------------------
// ASP.NET Core Identity
// ---------------------------------------------------------------------
builder.Services
    .AddIdentity<ApplicationUser, IdentityRole>(options =>
    {
        options.User.RequireUniqueEmail = true;
        options.Password.RequiredLength = 6;
        options.Password.RequireDigit = false;
        options.Password.RequireLowercase = false;
        options.Password.RequireUppercase = false;
        options.Password.RequireNonAlphanumeric = false;
        options.SignIn.RequireConfirmedAccount = false;
        options.Lockout.MaxFailedAccessAttempts = 8;
        options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
    })
    .AddEntityFrameworkStores<AppDbContext>()
    .AddDefaultTokenProviders()
    .AddErrorDescriber<IndonesianIdentityErrorDescriber>()
    .AddClaimsPrincipalFactory<AppClaimsPrincipalFactory>();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.AccessDeniedPath = "/Account/AccessDenied";
    options.Cookie.Name = "Kantongku.Auth";
    options.Cookie.HttpOnly = true;
    options.ExpireTimeSpan = TimeSpan.FromDays(14);
    options.SlidingExpiration = true;
});

// Semua endpoint wajib login, kecuali yang ditandai [AllowAnonymous].
builder.Services.AddAuthorization(options =>
{
    options.FallbackPolicy = new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build();
});

// ---------------------------------------------------------------------
// MVC + lokalisasi id-ID
// ---------------------------------------------------------------------
builder.Services.AddControllersWithViews(options =>
{
    options.SuppressImplicitRequiredAttributeForNonNullableReferenceTypes = true;
    options.Filters.Add(new AutoValidateAntiforgeryTokenAttribute());

    var messages = options.ModelBindingMessageProvider;
    messages.SetValueMustNotBeNullAccessor(_ => "Kolom ini wajib diisi.");
    messages.SetMissingBindRequiredValueAccessor(name => $"Nilai {name} wajib diisi.");
    messages.SetAttemptedValueIsInvalidAccessor((value, field) => $"Nilai '{value}' tidak valid untuk {field}.");
    messages.SetUnknownValueIsInvalidAccessor(field => $"Nilai tidak valid untuk {field}.");
    messages.SetValueIsInvalidAccessor(value => $"Nilai '{value}' tidak valid.");
    messages.SetValueMustBeANumberAccessor(field => $"{field} harus berupa angka.");
});

builder.Services.AddScoped<IPocketService, PocketService>();
builder.Services.AddScoped<ICategoryService, CategoryService>();

var culture = Formatting.Culture;
CultureInfo.DefaultThreadCurrentCulture = culture;
CultureInfo.DefaultThreadCurrentUICulture = culture;

var app = builder.Build();

try
{
    using var scope = app.Services.CreateScope();
    await DbInitializer.InitializeAsync(scope.ServiceProvider);
}
catch (Exception ex)
{
    app.Logger.LogCritical(ex, "Database tidak dapat dibuat. Periksa connection string dan pastikan MySQL XAMPP berjalan.");
    throw;
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
    app.UseHttpsRedirection();
}

app.UseStatusCodePagesWithReExecute("/Home/Status/{0}");
app.UseStaticFiles();

var localization = new RequestLocalizationOptions
{
    DefaultRequestCulture = new RequestCulture(culture),
    SupportedCultures = new List<CultureInfo> { culture },
    SupportedUICultures = new List<CultureInfo> { culture }
};
localization.RequestCultureProviders.Clear();
app.UseRequestLocalization(localization);

app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();

static ServerVersion ResolveServerVersion(string connectionString)
{
    try
    {
        // Hubungkan tanpa nama database (database belum tentu sudah ada pada run pertama).
        var builder = new MySqlConnectionStringBuilder(connectionString) { Database = string.Empty };
        return ServerVersion.AutoDetect(builder.ConnectionString);
    }
    catch
    {
        // Default XAMPP modern: MariaDB 10.4+. Dipakai bila server belum dapat dihubungi saat startup.
        return new MariaDbServerVersion(new Version(10, 4, 32));
    }
}
