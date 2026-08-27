using FitnessCoach.Api.Domain;

namespace FitnessCoach.Api.Features.Profiles;

public sealed record CreateTrainingProfileRequest
{
    public required IReadOnlyList<TrainingGoal> Goals { get; init; }

    public required TrainingExperience Experience { get; init; }

    public required IReadOnlyList<EquipmentType> AvailableEquipment { get; init; }

    public required UnitSystem UnitSystem { get; init; }
}

public sealed record TrainingProfileResponse(
    Guid Id,
    IReadOnlyList<TrainingGoal> Goals,
    TrainingExperience Experience,
    IReadOnlyList<EquipmentType> AvailableEquipment,
    UnitSystem UnitSystem,
    DateTimeOffset CreatedAt);
