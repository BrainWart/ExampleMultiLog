using Microsoft.AspNetCore.Mvc;
using OpenTelemetry.Logs;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.AddOpenTelemetry(o =>
{
    o.AddOtlpExporter();
});

builder.AddAuditLogging(loggingBuilder =>
{
    loggingBuilder.AddOpenTelemetry(o =>
    {
        o.AddOtlpExporter();
    });
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

app.MapGet("/weatherforecast", ([FromKeyedServices(typeof(AuditKey))] ILogger<Program> auditLogger, [FromServices] ILogger<WeatherForecast> logger) =>
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

var auditLogger = app.Services.GetRequiredKeyedService<ILogger<Program>>(typeof(AuditKey));
auditLogger.LogInformation("Started!");

app.Run();

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
