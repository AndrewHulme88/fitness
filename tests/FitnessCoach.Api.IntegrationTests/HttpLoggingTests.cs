using System.Collections.Concurrent;
using System.Net;

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpLogging;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FitnessCoach.Api.IntegrationTests;

public sealed class HttpLoggingTests : IClassFixture<ApiWebApplicationFactory>
{
    private const string HttpLoggingCategory =
        "Microsoft.AspNetCore.HttpLogging.HttpLoggingMiddleware";

    private readonly ApiWebApplicationFactory factory;

    public HttpLoggingTests(ApiWebApplicationFactory factory)
    {
        this.factory = factory;
    }

    [Fact]
    public void HttpLoggingConfigurationIsStructuredAndExcludesSensitiveFields()
    {
        var services = factory.Services;
        var configuration = services.GetRequiredService<IConfiguration>();
        var options = services.GetRequiredService<IOptions<HttpLoggingOptions>>().Value;

        const HttpLoggingFields expectedFields =
            HttpLoggingFields.RequestMethod
            | HttpLoggingFields.RequestPath
            | HttpLoggingFields.ResponseStatusCode
            | HttpLoggingFields.Duration;

        Assert.Equal("json", configuration["Logging:Console:FormatterName"]);
        Assert.Equal(expectedFields, options.LoggingFields);
        Assert.True(options.CombineLogs);
    }

    [Fact]
    public async Task HealthRequestIsLoggedWithoutItsQueryString()
    {
        const string syntheticSecret = "synthetic-secret-marker";
        using var logProvider = new CollectingLoggerProvider();
        using var customizedFactory = factory.WithWebHostBuilder(builder =>
            builder.ConfigureLogging(logging => logging.AddProvider(logProvider)));
        using var client = customizedFactory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            BaseAddress = new Uri("https://localhost"),
        });

        using var response = await client.GetAsync(
            $"/health?token={syntheticSecret}",
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var entry = Assert.Single(logProvider.Entries, item => item.Category == HttpLoggingCategory);
        Assert.Contains("Method: GET", entry.Message, StringComparison.Ordinal);
        Assert.Contains("Path: /health", entry.Message, StringComparison.Ordinal);
        Assert.Contains("StatusCode: 200", entry.Message, StringComparison.Ordinal);
        Assert.DoesNotContain(syntheticSecret, entry.Message, StringComparison.Ordinal);
    }

    private sealed class CollectingLoggerProvider : ILoggerProvider
    {
        private readonly ConcurrentQueue<LogEntry> entries = new();

        public IEnumerable<LogEntry> Entries => entries;

        public ILogger CreateLogger(string categoryName)
        {
            return new CollectingLogger(categoryName, entries);
        }

        public void Dispose()
        {
        }
    }

    private sealed class CollectingLogger(
        string category,
        ConcurrentQueue<LogEntry> entries) : ILogger
    {
        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull
        {
            return null;
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return true;
        }

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            entries.Enqueue(new LogEntry(category, formatter(state, exception)));
        }
    }

    private sealed record LogEntry(string Category, string Message);
}
