using System.Text.Json;
using System.Text.Json.Serialization;

using FitnessCoach.Api.Features.Exercises;
using FitnessCoach.Api.Features.AiCoach;
using FitnessCoach.Api.Features.Identity;
using FitnessCoach.Api.Features.Profiles;
using FitnessCoach.Api.Features.Progress;
using FitnessCoach.Api.Features.Sessions;
using FitnessCoach.Api.Features.Workouts;
using FitnessCoach.Api.Infrastructure;
using FitnessCoach.Api.Persistence;

using Microsoft.AspNetCore.HttpLogging;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;

var builder = WebApplication.CreateBuilder(args);

const string postgresConnectionName = "Postgres";
const string cognitoConfigurationSection = "Cognito";
const string openAiConfigurationSection = "OpenAi";

var cognitoConfiguration = builder.Configuration.GetSection(cognitoConfigurationSection).Get<CognitoConfiguration>();
var openAiConfiguration = builder.Configuration.GetSection(openAiConfigurationSection).Get<OpenAiConfiguration>()
    ?? new OpenAiConfiguration();
builder.Services.AddAuthentication();
builder.Services.AddAuthorization();
if (cognitoConfiguration is not null)
{
    cognitoConfiguration.Validate();
    var issuer = $"https://cognito-idp.{cognitoConfiguration.Region}.amazonaws.com/{cognitoConfiguration.UserPoolId}";

    builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(options =>
        {
            options.Authority = issuer;
            options.MapInboundClaims = false;
            options.TokenValidationParameters.ValidateAudience = false;
            options.Events = new JwtBearerEvents
            {
                OnTokenValidated = context =>
                {
                    var tokenUse = context.Principal?.FindFirst("token_use")?.Value;
                    var clientId = context.Principal?.FindFirst("client_id")?.Value;
                    var scopes = context.Principal?.FindFirst("scope")?.Value?.Split(' ', StringSplitOptions.RemoveEmptyEntries)
                        ?? [];

                    if (tokenUse != "access"
                        || clientId != cognitoConfiguration.AppClientId
                        || !scopes.Contains(cognitoConfiguration.RequiredScope, StringComparer.Ordinal))
                    {
                        context.Fail("The access token is not authorized for Fitness Coach.");
                    }

                    return Task.CompletedTask;
                },
            };
        });
}

builder.Services.AddHealthChecks();
builder.Services.AddFitnessCoachRateLimiting(builder.Configuration);
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddScoped<ExerciseCatalogueImporter>();
builder.Services.AddScoped<AiCoachService>();
builder.Services.AddScoped<IAiCoachContextAssembler, AiCoachContextAssembler>();
builder.Services.AddSingleton(openAiConfiguration);
if (openAiConfiguration.IsConfigured)
{
    builder.Services.AddHttpClient("OpenAI", client => client.BaseAddress = new Uri("https://api.openai.com/"));
    builder.Services.AddSingleton<IAiCoachProvider, OpenAiAiCoachProvider>();
}
else if (builder.Environment.IsDevelopment())
{
    builder.Services.AddSingleton<IAiCoachProvider, DevelopmentFakeAiCoachProvider>();
}
else
{
    builder.Services.AddSingleton<IAiCoachProvider, UnavailableAiCoachProvider>();
}
builder.Services.AddSingleton<IAiCoachUsageRecorder, LoggerAiCoachUsageRecorder>();
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

if (await ExerciseCatalogueImportCommand.TryRunAsync(app, args))
{
    return;
}

if (await PrototypeProfileClaimCommand.TryRunAsync(app, args))
{
    return;
}

app.UseHttpLogging();
app.UseHttpsRedirection();

app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
if (!app.Environment.IsDevelopment()) app.UseRateLimiter();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

if (cognitoConfiguration is not null
    || app.Environment.IsDevelopment()
    || app.Environment.IsEnvironment("Testing"))
{
    app.MapAccountEndpoints();
    app.MapCoachConversationEndpoints();
    app.MapExerciseEndpoints();
    app.MapProfileEndpoints();
    app.MapProgressEndpoints();
    app.MapWorkoutSessionEndpoints();
    app.MapWorkoutEndpoints();
}

app.MapGet("/health", GetHealthAsync)
    .WithName("GetHealth")
    .WithSummary("Check API liveness")
    .Produces<string>(StatusCodes.Status200OK, "text/plain")
    .Produces<string>(StatusCodes.Status503ServiceUnavailable, "text/plain");
app.MapGet("/health/ready", GetReadinessAsync)
    .WithName("GetReadiness")
    .WithSummary("Check database readiness")
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

static async Task<IResult> GetReadinessAsync(
    IServiceProvider services,
    HttpContext context,
    CancellationToken cancellationToken)
{
    try
    {
        await using var scope = services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<FitnessCoachDbContext>();
        var isReady = await dbContext.Database.CanConnectAsync(cancellationToken);
        context.Response.Headers.CacheControl = "no-store, no-cache";
        return Results.Text(
            isReady ? "Ready" : "Unavailable",
            contentType: "text/plain",
            statusCode: isReady
                ? StatusCodes.Status200OK
                : StatusCodes.Status503ServiceUnavailable);
    }
    catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
    {
        throw;
    }
    catch (Exception)
    {
        context.Response.Headers.CacheControl = "no-store, no-cache";
        return Results.Text(
            "Unavailable",
            contentType: "text/plain",
            statusCode: StatusCodes.Status503ServiceUnavailable);
    }
}

public partial class Program;

internal sealed class CognitoConfiguration
{
    public string? Region { get; init; }

    public string? UserPoolId { get; init; }

    public string? AppClientId { get; init; }

    public string? RequiredScope { get; init; }

    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(Region)
            || string.IsNullOrWhiteSpace(UserPoolId)
            || string.IsNullOrWhiteSpace(AppClientId)
            || string.IsNullOrWhiteSpace(RequiredScope))
        {
            throw new InvalidOperationException(
                "Cognito configuration requires Region, UserPoolId, AppClientId, and RequiredScope.");
        }
    }
}
