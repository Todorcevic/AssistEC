using AssistEC.Components;
using AssistEC.Services;
using AssistEC.Models;
using AssistEC.Services.Abstractions;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Configure settings
builder.Services.Configure<DocumentContextSettings>(
    builder.Configuration.GetSection("DocumentContext"));

// Add memory cache
builder.Services.AddMemoryCache();

// Register custom services
// Permitir configurar qué servicio de SharePoint usar
var useMockService = builder.Configuration.GetValue<bool>("SharePoint:UseMockService", true);

if (useMockService)
{
    builder.Services.AddScoped<ISharePointService, MockSharePointService>();
    builder.Logging.AddConsole().SetMinimumLevel(LogLevel.Information);
    Console.WriteLine("🔧 Using MockSharePointService for testing");
}
else
{
    builder.Services.AddScoped<ISharePointService, SharePointService>();
    Console.WriteLine("📡 Using real SharePointService - ensure credentials are configured");
}

// Register AI services
builder.Services.AddScoped<IEmbeddingService, EmbeddingService>();
builder.Services.AddScoped<IDocumentContextService, DocumentContextService>();
builder.Services.AddScoped<OpenAIService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
