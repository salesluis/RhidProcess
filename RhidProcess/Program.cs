using RhidProcess.Browser;
using RhidProcess.Models;
using RhidProcess.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

var browserFactory = new BrowserFactory();

var service = new RepAutomationService(browserFactory);

var result = await service.ExecuteAsync(
    new UnlockRequest(
        "123456789",
        "987654"));

Console.WriteLine(result.ContraSenha);

app.Run();
