namespace FitnessCoach.Api.Features.Identity;

internal static class PrototypeProfileClaimCommand
{
    private const string ClaimArgument = "--claim-prototype-profile";
    private const string SubjectArgument = "--cognito-sub";

    public static async Task<bool> TryRunAsync(WebApplication app, string[] arguments)
    {
        var claimIndex = arguments.IndexOf(ClaimArgument);
        if (claimIndex < 0) return false;
        if (!app.Environment.IsDevelopment())
        {
            throw new InvalidOperationException("Prototype profile claims are Development-only.");
        }

        if (claimIndex + 1 >= arguments.Length || !Guid.TryParse(arguments[claimIndex + 1], out var profileId))
        {
            throw new ArgumentException("--claim-prototype-profile requires a profile UUID.");
        }

        var subjectIndex = arguments.IndexOf(SubjectArgument);
        if (subjectIndex < 0 || subjectIndex + 1 >= arguments.Length
            || string.IsNullOrWhiteSpace(arguments[subjectIndex + 1]))
        {
            throw new ArgumentException("--cognito-sub requires the authenticated Cognito subject.");
        }

        var cognito = app.Configuration.GetRequiredSection("Cognito").Get<CognitoConfiguration>()
            ?? throw new InvalidOperationException("Cognito configuration is required.");
        cognito.Validate();
        var issuer = $"https://cognito-idp.{cognito.Region}.amazonaws.com/{cognito.UserPoolId}";

        await using var scope = app.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<Persistence.FitnessCoachDbContext>();
        await PrototypeProfileClaimer.ClaimAsync(
            db, profileId, issuer, arguments[subjectIndex + 1], TimeProvider.System, CancellationToken.None);
        return true;
    }
}
