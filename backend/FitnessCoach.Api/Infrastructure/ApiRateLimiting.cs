using System.Globalization;
using System.Threading.RateLimiting;

using Microsoft.AspNetCore.RateLimiting;

namespace FitnessCoach.Api.Infrastructure;

internal static class ApiRateLimitPolicies
{
    internal const string Standard = "standard-api";
    internal const string ActiveSessionWrites = "active-session-writes";
    internal const string CoachMessages = "coach-messages";
}

internal static class ApiRateLimiting
{
    private const string ConfigurationSection = "RateLimiting";

    internal static IServiceCollection AddFitnessCoachRateLimiting(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var settings = new ApiRateLimitSettings();
        configuration.GetSection(ConfigurationSection).Bind(settings);
        settings.Validate();

        services.AddRateLimiter(options =>
        {
            options.OnRejected = (context, _) =>
            {
                if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
                {
                    context.HttpContext.Response.Headers.RetryAfter = Math.Max(
                        1,
                        (int)Math.Ceiling(retryAfter.TotalSeconds))
                        .ToString(CultureInfo.InvariantCulture);
                }

                var response = context.HttpContext.Response;
                response.StatusCode = StatusCodes.Status429TooManyRequests;
                return new ValueTask(Results.Problem(
                    statusCode: StatusCodes.Status429TooManyRequests,
                    title: "Too many requests.",
                    type: "about:blank").ExecuteAsync(context.HttpContext));
            };
            AddFixedWindowPolicy(options, ApiRateLimitPolicies.Standard, settings.Standard);
            AddFixedWindowPolicy(
                options,
                ApiRateLimitPolicies.ActiveSessionWrites,
                settings.ActiveSessionWrites);
            AddFixedWindowPolicy(options, ApiRateLimitPolicies.CoachMessages, settings.CoachMessages);
        });

        return services;
    }

    private static void AddFixedWindowPolicy(
        RateLimiterOptions options,
        string policyName,
        RateLimitPolicySettings settings) => options.AddPolicy(
            policyName,
            context => RateLimitPartition.GetFixedWindowLimiter(
                GetPartitionKey(context),
                _ => new FixedWindowRateLimiterOptions
                {
                    AutoReplenishment = true,
                    PermitLimit = settings.PermitLimit,
                    QueueLimit = 0,
                    Window = TimeSpan.FromSeconds(settings.WindowSeconds),
                }));

    private static string GetPartitionKey(HttpContext context)
    {
        var subject = context.User.FindFirst("sub")?.Value;
        if (!string.IsNullOrWhiteSpace(subject)) return $"account:{subject}";

        return $"anonymous:{context.Connection.RemoteIpAddress?.ToString() ?? "unknown"}";
    }
}

internal sealed class ApiRateLimitSettings
{
    public RateLimitPolicySettings Standard { get; set; } = new(120, 60);

    public RateLimitPolicySettings ActiveSessionWrites { get; set; } = new(30, 60);

    public RateLimitPolicySettings CoachMessages { get; set; } = new(6, 600);

    internal void Validate()
    {
        Standard.Validate(nameof(Standard));
        ActiveSessionWrites.Validate(nameof(ActiveSessionWrites));
        CoachMessages.Validate(nameof(CoachMessages));
    }
}

internal sealed class RateLimitPolicySettings
{
    internal RateLimitPolicySettings()
    {
    }

    internal RateLimitPolicySettings(int permitLimit, int windowSeconds)
    {
        PermitLimit = permitLimit;
        WindowSeconds = windowSeconds;
    }

    public int PermitLimit { get; set; }

    public int WindowSeconds { get; set; }

    internal void Validate(string policyName)
    {
        if (PermitLimit is < 1 or > 10_000 || WindowSeconds is < 1 or > 86_400)
        {
            throw new InvalidOperationException(
                $"Rate limiting policy '{policyName}' must use a permit limit from 1 to 10,000 "
                + "and a window from 1 to 86,400 seconds.");
        }
    }
}
