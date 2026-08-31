using FitnessCoach.Api.Features.Profiles;
using FitnessCoach.Api.Persistence;

using Microsoft.EntityFrameworkCore;

namespace FitnessCoach.Api.Features.Identity;

internal static class PrototypeProfileClaimer
{
    public static async Task ClaimAsync(
        FitnessCoachDbContext db,
        Guid profileId,
        string issuer,
        string subject,
        TimeProvider timeProvider,
        CancellationToken cancellationToken)
    {
        var profile = await db.Set<TrainingProfile>()
            .SingleOrDefaultAsync(item => item.Id == profileId, cancellationToken)
            ?? throw new InvalidOperationException("Prototype profile was not found.");
        var account = await db.Set<ApplicationAccount>()
            .SingleOrDefaultAsync(item => item.Issuer == issuer && item.Subject == subject, cancellationToken);
        if (account is null)
        {
            account = ApplicationAccount.Create(issuer, subject, timeProvider.GetUtcNow());
            db.Add(account);
        }

        if (profile.AccountId == account.Id) return;
        if (profile.AccountId is not null) throw new InvalidOperationException("Prototype profile is already claimed.");

        profile.Claim(account.Id);
        await db.SaveChangesAsync(cancellationToken);
    }
}
