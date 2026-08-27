using FitnessCoach.Api.Domain;

namespace FitnessCoach.Api.Features.Profiles;

internal sealed class TrainingProfile
{
    private TrainingProfile()
    {
    }

    private TrainingProfile(
        Guid id,
        TrainingExperience experience,
        UnitSystem unitSystem,
        DateTimeOffset createdAt)
    {
        Id = id;
        Experience = experience;
        UnitSystem = unitSystem;
        CreatedAt = createdAt;
    }

    public Guid Id { get; private set; }

    public TrainingExperience Experience { get; private set; }

    public UnitSystem UnitSystem { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public ICollection<TrainingProfileGoal> Goals { get; } = [];

    public ICollection<TrainingProfileEquipment> AvailableEquipment { get; } = [];

    public static TrainingProfile Create(
        IReadOnlyCollection<TrainingGoal> goals,
        TrainingExperience experience,
        IReadOnlyCollection<EquipmentType> availableEquipment,
        UnitSystem unitSystem,
        DateTimeOffset createdAt)
    {
        var profile = new TrainingProfile(Guid.NewGuid(), experience, unitSystem, createdAt);

        foreach (var goal in goals)
        {
            profile.Goals.Add(new TrainingProfileGoal(profile.Id, goal));
        }

        foreach (var equipment in availableEquipment)
        {
            profile.AvailableEquipment.Add(new TrainingProfileEquipment(profile.Id, equipment));
        }

        return profile;
    }
}

internal sealed class TrainingProfileGoal(Guid profileId, TrainingGoal goal)
{
    public Guid ProfileId { get; private set; } = profileId;

    public TrainingGoal Goal { get; private set; } = goal;
}

internal sealed class TrainingProfileEquipment(Guid profileId, EquipmentType equipment)
{
    public Guid ProfileId { get; private set; } = profileId;

    public EquipmentType Equipment { get; private set; } = equipment;
}
