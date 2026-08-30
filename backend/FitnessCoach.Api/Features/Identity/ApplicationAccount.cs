using FitnessCoach.Api.Features.Profiles;

namespace FitnessCoach.Api.Features.Identity;

internal sealed class ApplicationAccount
{
    private ApplicationAccount()
    {
    }

    private ApplicationAccount(Guid id, string issuer, string subject, DateTimeOffset createdAt)
    {
        Id = id;
        Issuer = issuer;
        Subject = subject;
        CreatedAt = createdAt;
    }

    public Guid Id { get; private set; }

    public string Issuer { get; private set; } = null!;

    public string Subject { get; private set; } = null!;

    public DateTimeOffset CreatedAt { get; private set; }

    public TrainingProfile? Profile { get; private set; }

    public static ApplicationAccount Create(string issuer, string subject, DateTimeOffset createdAt) =>
        new(Guid.NewGuid(), issuer, subject, createdAt);
}
