using RhidProcess;
using RhidProcess.Abstractions;
using RhidProcess.Auth;
using RhidProcess.Browser;
using RhidProcess.Health;
using RhidProcess.Logging;
using RhidProcess.Monitoring;
using RhidProcess.Routes;
using RhidProcess.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

builder.Services.AddSingleton<IBrowserFactory, BrowserFactory>();
builder.Services.AddSingleton<RepAutomationService>();
builder.Services.AddSingleton<ErrorFileLogger>();
builder.Services.AddSingleton<BrowserRuntimeSettings>();
builder.Services.AddSingleton<AutomationTelemetry>();
builder.Services.AddSingleton<RhidHealthService>();
builder.Services.AddHttpClient(HttpClientNames.RhidAvailability, client =>
{
    client.Timeout = Timeout.InfiniteTimeSpan;
});

builder.Services.AddOptions<RhidOptions>()
    .Bind(builder.Configuration.GetSection(RhidOptions.SectionName))
    .ValidateDataAnnotations()
    .Validate(
        options => !string.IsNullOrWhiteSpace(options.Password),
        "A senha do RHID não foi configurada."
        )
    .Validate(
        options => options.DefaultTimeout > 0
                   && options.DefaultNavigationTimeout > 0,
        "Os timeouts do RHID devem ser maiores que zero.")
    .ValidateOnStart();

var app = builder.Build();

app.UseMiddleware<ErrorLoggingMiddleware>();
app.UseMiddleware<ApiKeyMiddleware>();
app.MapRhidRoute();
// app.UseHttpsRedirection();

app.Run();
