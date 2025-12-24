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
// LOGGING (MEJORADO PARA AZURE)
// ===============================
builder.Logging.ClearProviders();

// Console para Azure App Service Log Stream
builder.Logging.AddConsole();

// Azure Web App Diagnostics (clave para Azure)
builder.Logging.AddAzureWebAppDiagnostics();

// Debug en desarrollo local
if (builder.Environment.IsDevelopment())
{
    builder.Logging.AddDebug();
}

// Nivel global
builder.Logging.SetMinimumLevel(LogLevel.Information);

// Filtros por categoría
builder.Logging.AddFilter("CarvedRockFitness", LogLevel.Information);
builder.Logging.AddFilter("Microsoft", LogLevel.Warning);
builder.Logging.AddFilter("System", LogLevel.Warning);
builder.Logging.AddFilter("Microsoft.AspNetCore.Hosting", LogLevel.Information); // Ver startup

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
// LOGS DE INICIO (PARA VERIFICAR)
// ===============================
var logger = app.Services.GetRequiredService<ILogger<Program>>();
logger.LogInformation("========================================");
logger.LogInformation("CarvedRockFitness Application Starting");
logger.LogInformation("Environment: {Environment}", app.Environment.EnvironmentName);
logger.LogInformation("Connection String Configured: {HasConnection}", 
    !string.IsNullOrEmpty(connectionString));
logger.LogInformation("Repository: {RepositoryType}", 
    string.IsNullOrEmpty(connectionString) ? "InMemory" : "SQL");
logger.LogInformation("========================================");

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

logger.LogInformation("Application configured successfully. Starting web host...");

app.Run();