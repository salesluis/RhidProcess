using RhidProcess.Browser;
using RhidProcess.Models;
using RhidProcess.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

builder.Services.AddSingleton<BrowserFactory>();
builder.Services.AddSingleton<RepAutomationService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapRhidRoutes();
app.UseHttpsRedirection();

app.Run();
