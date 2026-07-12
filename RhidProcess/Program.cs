using RhidProcess.Abstractions;
using RhidProcess.Auth;
using RhidProcess.Browser;
using RhidProcess.Logging;
using RhidProcess.Routes;
using RhidProcess.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

builder.Services.AddSingleton<IBrowserFactory, BrowserFactory>();
builder.Services.AddSingleton<RepAutomationService>();
builder.Services.AddSingleton<ErrorFileLogger>();

var app = builder.Build();

app.UseMiddleware<ErrorLoggingMiddleware>();
app.UseMiddleware<ApiKeyMiddleware>();
app.MapRhidRoute();
// app.UseHttpsRedirection();

app.Run();
