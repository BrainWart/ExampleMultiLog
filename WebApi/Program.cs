using Microsoft.AspNetCore.Mvc;
using OpenTelemetry.Logs;

var builder = WebApplication.CreateBuilder(args);

builder.AddKeyedLogging<AuditLogging>(loggingBuilder =>
{
    loggingBuilder.AddConsole();
});

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

var summaries = new[]
{
    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
};

app.MapGet("/weatherforecast", ([FromKeyedServices(typeof(AuditLogging))] ILogger<Program> auditLogger, [FromServices] ILogger<WeatherForecast> logger) =>
{
    auditLogger.LogInformation("Test1");
    logger.LogInformation("Additional Logging");

    var forecast =  Enumerable.Range(1, 5).Select(index =>
        new WeatherForecast
        (
            DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
            Random.Shared.Next(-20, 55),
            summaries[Random.Shared.Next(summaries.Length)]
        ))
        .ToArray();
    return forecast;
})
.WithName("GetWeatherForecast");


async Task Test()
{
    var auditLogger = app.Services.GetRequiredKeyedService<ILogger<Program>>(typeof(AuditLogging));
    using (auditLogger.BeginScope("Audit Testing"))
        auditLogger.LogInformation("Audit Started!");


    var logger = app.Services.GetRequiredService<ILogger<Program>>();
    using (logger.BeginScope("Testing"))
        logger.LogInformation("Normal Started!");
}

await Task.WhenAll(Test(), app.RunAsync());

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}

internal class AuditLogging {}
