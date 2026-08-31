using System.Security.Claims;

using FitnessCoach.Api.Persistence;

using Microsoft.EntityFrameworkCore;

namespace FitnessCoach.Api.Features.Identity;

internal static class ApplicationAccountResolver
{
    public static async Task<ApplicationAccount> GetOrCreateAsync(
        ClaimsPrincipal principal,
        FitnessCoachDbContext dbContext,
        TimeProvider timeProvider,
        CancellationToken cancellationToken)
    {
        var subjectClaim = principal.FindFirst("sub")
            ?? principal.FindFirst(ClaimTypes.NameIdentifier);
        var issuer = principal.FindFirst("iss")?.Value ?? subjectClaim?.Issuer;
        var subject = subjectClaim?.Value;
        if (string.IsNullOrWhiteSpace(issuer) || string.IsNullOrWhiteSpace(subject))
        {
            throw new UnauthorizedAccessException("The access token has no stable account identity.");
        }

        var account = await dbContext.Set<ApplicationAccount>()
            .SingleOrDefaultAsync(item => item.Issuer == issuer && item.Subject == subject, cancellationToken);
        if (account is not null) return account;

        account = ApplicationAccount.Create(issuer, subject, timeProvider.GetUtcNow());
        dbContext.Add(account);
        await dbContext.SaveChangesAsync(cancellationToken);
        return account;
    }
}
