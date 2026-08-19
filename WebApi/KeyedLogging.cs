using Microsoft.Extensions.Options;

public static class KeyedLoggingServiceCollectionExtensions
{
    public static IHostApplicationBuilder AddKeyedLogging<TKey>(this IHostApplicationBuilder builder)
        => AddKeyedLogging<TKey>(builder, (_) => { });
    
    public static IHostApplicationBuilder AddKeyedLogging<TKey>(this IHostApplicationBuilder builder, Action<ILoggingBuilder> configure)
        => AddKeyedLogging<TKey>(builder, builder.Configuration.GetSection(typeof(TKey).Name), configure);

    public static IHostApplicationBuilder AddKeyedLogging<TKey>(this IHostApplicationBuilder builder, IConfiguration configuration, Action<ILoggingBuilder> configure)
    {
        builder.Services.AddKeyedSingleton<IServiceProvider>(typeof(TKey), (_, _) => {
            var loggingServiceCollection = new ServiceCollection();
            loggingServiceCollection.AddSingleton<IConfiguration>(configuration);
            loggingServiceCollection.AddLogging(loggerBuilder =>
            {
                loggerBuilder.AddConfiguration(configuration);
                configure(loggerBuilder);
            });

            return loggingServiceCollection.BuildServiceProvider();
        });
        builder.Services.AddKeyedTransient(typeof(ILogger<>), typeof(TKey), typeof(KeyedLogger<>));
        builder.Services.AddKeyedSingleton<ILoggerFactory, LoggerFactory>(typeof(TKey), (services, key) =>
        {
            var keyedServices = services.GetRequiredKeyedService<IServiceProvider>(key);
            var providers = keyedServices.GetServices<ILoggerProvider>();
            var optionsMonitor = keyedServices.GetRequiredService<IOptionsMonitor<LoggerFilterOptions>>();

            return new LoggerFactory(providers, optionsMonitor);
        });

        return builder;
    }
}

public class KeyedLogger<TCategoryName> : ILogger<TCategoryName>
{
    private ILogger<TCategoryName> _logger;
    public KeyedLogger([ServiceKey] object key, IServiceProvider services)
    {
        _logger = services.GetRequiredKeyedService<ILoggerFactory>(key).CreateLogger<TCategoryName>();
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
