using FitnessCoach.Api.Domain;
using FitnessCoach.Api.Features.Identity;
using FitnessCoach.Api.Features.Profiles;
using FitnessCoach.Api.Persistence;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace FitnessCoach.Api.IntegrationTests;

public sealed class PrototypeProfileClaimerTests : IClassFixture<PostgreSqlApiFixture>
{
    private const string Issuer = "https://cognito-idp.ap-southeast-2.amazonaws.com/ap-southeast-2_test";
    private static readonly TrainingGoal[] Goals = [TrainingGoal.BuildStrength];
    private static readonly EquipmentType[] Equipment = [EquipmentType.Bodyweight];
    private readonly PostgreSqlApiFixture fixture;

    public PrototypeProfileClaimerTests(PostgreSqlApiFixture fixture) => this.fixture = fixture;

    [Fact]
    public async Task ClaimingAnUnclaimedProfileIsIdempotentAndRejectsAnotherAccount()
    {
        var profileId = await CreateUnclaimedProfileAsync();
        const string ownerSubject = "prototype-owner";

        await ClaimAsync(profileId, ownerSubject);
        await ClaimAsync(profileId, ownerSubject);

        await using var scope = fixture.Factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<FitnessCoachDbContext>();
        var profiles = await db.Set<TrainingProfile>().Where(profile => profile.Id == profileId).ToListAsync(
            TestContext.Current.CancellationToken);
        var account = await db.Set<ApplicationAccount>().SingleAsync(
            item => item.Issuer == Issuer && item.Subject == ownerSubject,
            TestContext.Current.CancellationToken);
        Assert.Single(profiles);
        Assert.Equal(account.Id, profiles[0].AccountId);

        await Assert.ThrowsAsync<InvalidOperationException>(() => ClaimAsync(profileId, "other-account"));

        await using var verificationScope = fixture.Factory.Services.CreateAsyncScope();
        var verificationDb = verificationScope.ServiceProvider.GetRequiredService<FitnessCoachDbContext>();
        var accountCount = await verificationDb.Set<ApplicationAccount>().CountAsync(
            item => item.Issuer == Issuer, TestContext.Current.CancellationToken);
        Assert.Equal(1, accountCount);
    }

    private async Task<Guid> CreateUnclaimedProfileAsync()
    {
        await using var scope = fixture.Factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<FitnessCoachDbContext>();
        var profile = TrainingProfile.Create(
            Goals,
            TrainingExperience.Beginner,
            Equipment,
            UnitSystem.Metric,
            TimeProvider.System.GetUtcNow());
        db.Add(profile);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        return profile.Id;
    }

    private async Task ClaimAsync(Guid profileId, string subject)
    {
        await using var scope = fixture.Factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<FitnessCoachDbContext>();
        await PrototypeProfileClaimer.ClaimAsync(
            db, profileId, Issuer, subject, TimeProvider.System, TestContext.Current.CancellationToken);
    }
}
