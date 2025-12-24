using CarvedRockFitness.Components;
using CarvedRockFitness.Services;
using CarvedRockFitness.Repositories;
using Microsoft.Extensions.Logging.AzureAppServices;

var builder = WebApplication.CreateBuilder(args);

// Razor / Blazor
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Session
builder.Services.AddHttpContextAccessor();
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.Cookie.Name = "CarvedRockFitness.Session";
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// --------------------
// LOGGING (CORRECTO)
// --------------------
builder.Logging.ClearProviders();
builder.Logging.AddAzureWebAppDiagnostics();

// Nivel global
builder.Logging.SetMinimumLevel(LogLevel.Information);

// Filtro explícito para App Service providers
builder.Logging.AddFilter<AzureFileLoggerProvider>(null, LogLevel.Information);
builder.Logging.AddFilter<AzureBlobLoggerProvider>(null, LogLevel.Information);

// Reduce ruido
builder.Logging.AddFilter("Microsoft", LogLevel.Warning);
builder.Logging.AddFilter("System", LogLevel.Warning);

// --------------------
// Repositorios
// --------------------
string connectionString =
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

// Pipeline HTTP
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

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
