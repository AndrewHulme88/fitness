using System.Text.Json;
using System.Text.Json.Serialization;

using FitnessCoach.Api.Features.Profiles;
using FitnessCoach.Api.Persistence;

using Microsoft.AspNetCore.HttpLogging;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;

var builder = WebApplication.CreateBuilder(args);

const string postgresConnectionName = "Postgres";

builder.Services.AddHealthChecks();
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.Converters.Add(
        new JsonStringEnumConverter(JsonNamingPolicy.CamelCase, allowIntegerValues: false));
    options.SerializerOptions.UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow;
});
builder.Services.AddOpenApi("v1", options =>
    options.AddDocumentTransformer((document, _, _) =>
    {
        document.Info.Title = "Fitness Coach API";
        document.Info.Version = "v1";

        return Task.CompletedTask;
    }));
builder.Services.AddDbContext<FitnessCoachDbContext>((services, options) =>
{
    var configuration = services.GetRequiredService<IConfiguration>();
    var postgresConnectionString = configuration.GetConnectionString(postgresConnectionName);

    if (string.IsNullOrWhiteSpace(postgresConnectionString))
    {
        throw new InvalidOperationException(
            $"Connection string '{postgresConnectionName}' is required. "
            + "Configure it with the ConnectionStrings__Postgres environment variable.");
    }

    options.UseNpgsql(postgresConnectionString);
});
builder.Services.AddHttpLogging(options =>
{
    // Bodies, headers, and query strings may contain sensitive user data and are deliberately excluded.
    options.LoggingFields =
        HttpLoggingFields.RequestMethod
        | HttpLoggingFields.RequestPath
        | HttpLoggingFields.ResponseStatusCode
        | HttpLoggingFields.Duration;
    options.CombineLogs = true;
});

var app = builder.Build();

app.UseHttpLogging();
app.UseHttpsRedirection();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapProfileEndpoints();
}

app.MapGet("/health", GetHealthAsync)
    .WithName("GetHealth")
    .WithSummary("Check API liveness")
    .Produces<string>(StatusCodes.Status200OK, "text/plain")
    .Produces<string>(StatusCodes.Status503ServiceUnavailable, "text/plain");

app.Run();

static async Task<IResult> GetHealthAsync(
    HealthCheckService healthCheckService,
    HttpContext context,
    CancellationToken cancellationToken)
{
    var healthReport = await healthCheckService.CheckHealthAsync(cancellationToken);
    var statusCode = healthReport.Status is HealthStatus.Unhealthy
        ? StatusCodes.Status503ServiceUnavailable
        : StatusCodes.Status200OK;

    context.Response.Headers.CacheControl = "no-store, no-cache";

    return Results.Text(
        healthReport.Status.ToString(),
        contentType: "text/plain",
        statusCode: statusCode);
}

public partial class Program;
