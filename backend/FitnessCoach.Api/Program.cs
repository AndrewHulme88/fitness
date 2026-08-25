using FitnessCoach.Api.Persistence;

using Microsoft.AspNetCore.HttpLogging;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

const string postgresConnectionName = "Postgres";

builder.Services.AddHealthChecks();
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

app.MapHealthChecks("/health");

app.Run();

public partial class Program;
