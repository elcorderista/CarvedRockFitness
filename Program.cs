using CarvedRockFitness.Components;
using CarvedRockFitness.Services;
using CarvedRockFitness.Repositories;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

var builder = WebApplication.CreateBuilder(args);

// ===============================
// Razor / Blazor Server
// ===============================
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// ===============================
// Session (Blazor Server = stateful)
// ===============================
builder.Services.AddHttpContextAccessor();
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.Cookie.Name = "CarvedRockFitness.Session";
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// ===============================
// LOGGING (FORMA SOPORTADA .NET 8)
// ===============================
builder.Logging.ClearProviders();

// Conecta ILogger con Azure App Service (stdout / log streaming)
builder.Logging.AddAzureWebAppDiagnostics();

// Nivel global
builder.Logging.SetMinimumLevel(LogLevel.Information);

// Filtro por categoría de la app (CLAVE)
builder.Logging.AddFilter("CarvedRockFitness", LogLevel.Information);

// Reduce ruido del framework
builder.Logging.AddFilter("Microsoft", LogLevel.Warning);
builder.Logging.AddFilter("System", LogLevel.Warning);

// ===============================
// Repositorios y servicios
// ===============================
string? connectionString =
    builder.Configuration.GetConnectionString("DefaultConnection")
    ?? Environment.GetEnvironmentVariable("DefaultConnection");

if (!string.IsNullOrEmpty(connectionString))
{
    builder.Services.AddScoped<ICartRepository, SqlCartRepository>();
}
else
{
    builder.Services.AddScoped<ICartRepository, InMemoryCartRepository>();
}

builder.Services.AddScoped<ShoppingCartService>();
builder.Services.AddSingleton<CartEventService>();
builder.Services.AddScoped<IProductRepository, ProductRepository>();

var app = builder.Build();

// ===============================
// Pipeline HTTP
// ===============================
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseStaticFiles();
app.UseRouting();
app.UseSession();
app.UseAntiforgery();

// ===============================
// Blazor endpoints
// ===============================
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
