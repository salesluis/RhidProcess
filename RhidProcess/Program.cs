using RhidProcess.Abstractions;
using RhidProcess.Auth;
using RhidProcess.Browser;
using RhidProcess.Health;
using RhidProcess.Logging;
using RhidProcess.Options;
using RhidProcess.Routes;
using RhidProcess.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.AddJsonConsole(options => options.IncludeScopes = true);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

builder.Services.Configure<RhidOptions>(
    builder.Configuration.GetSection(RhidOptions.SectionName));
builder.Services.AddSingleton<IBrowserFactory, BrowserFactory>();
builder.Services.AddSingleton<RepAutomationService>();
builder.Services.AddSingleton<ErrorFileLogger>();
builder.Services.AddSingleton<RhidHealthService>();

var app = builder.Build();

app.UseMiddleware<ErrorLoggingMiddleware>();
app.UseMiddleware<ApiKeyMiddleware>();
app.MapRhidRoute();
app.UseHttpsRedirection();

app.Run();
