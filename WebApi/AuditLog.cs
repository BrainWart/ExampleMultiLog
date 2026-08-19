using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

public static class AuditServiceCollectionExtensions
{
    public static IHostApplicationBuilder AddAuditLogging(this IHostApplicationBuilder builder)
        => AddAuditLogging(builder, (builder) => { });
    
    public static IHostApplicationBuilder AddAuditLogging(this IHostApplicationBuilder builder, Action<ILoggingBuilder> configure)
    {
        var auditServiceCollection = new ServiceCollection();
        auditServiceCollection.AddSingleton<IConfiguration>(builder.Configuration.GetSection("Audit"));
        auditServiceCollection.AddLogging(loggerBuilder =>
        {
            loggerBuilder.AddConfiguration(builder.Configuration.GetSection("Audit"));
            configure(loggerBuilder);
        });
        var auditServices = auditServiceCollection.BuildServiceProvider();

        builder.Services.AddKeyedTransient(typeof(ILogger<>), AuditKeyedLogger.AuditKey, typeof(AuditKeyedLogger<>));
        builder.Services.AddKeyedSingleton<ILoggerFactory, LoggerFactory>(AuditKeyedLogger.AuditKey, (services, _) =>
        {
            var providers = auditServices.GetServices<ILoggerProvider>();
            var options = auditServices.GetRequiredService<IOptions<LoggerFilterOptions>>();

            return new LoggerFactory(providers, options.Value);
        });

        return builder;
    }
}

public static class AuditKeyedLogger
{
    public const string AuditKey = "AUDIT";
}

public class AuditKeyedLogger<T> : ILogger<T>
{
    private ILogger<T> _logger;
    public AuditKeyedLogger([FromKeyedServices(AuditKeyedLogger.AuditKey)] ILoggerFactory factory)
    {
        _logger = factory.CreateLogger<T>();
    }

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull
    {
        return _logger.BeginScope(state);
    }

    public bool IsEnabled(LogLevel logLevel)
    {
        return _logger.IsEnabled(logLevel);
    }

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        _logger.Log(logLevel, eventId, state, exception, formatter);
    }
}