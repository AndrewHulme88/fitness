using Microsoft.AspNetCore.HttpLogging;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHealthChecks();
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
