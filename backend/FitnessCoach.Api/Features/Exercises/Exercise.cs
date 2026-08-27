using FitnessCoach.Api.Domain;

namespace FitnessCoach.Api.Features.Exercises;

internal sealed class Exercise
{
    private Exercise()
    {
    }

    private Exercise(Guid id)
    {
        Id = id;
    }

    public Guid Id { get; private set; }

    public string Slug { get; private set; } = string.Empty;

    public string Name { get; private set; } = string.Empty;

    public ExerciseCategory Category { get; private set; }

    public ExerciseMovementPattern MovementPattern { get; private set; }

    public ExerciseTrackingMode TrackingMode { get; private set; }

    public string Setup { get; private set; } = string.Empty;

    public string Execution { get; private set; } = string.Empty;

    public string Safety { get; private set; } = string.Empty;

    public ICollection<ExerciseAlias> Aliases { get; } = [];

    public ICollection<ExerciseEquipment> RequiredEquipment { get; } = [];

    public ICollection<ExerciseMuscle> Muscles { get; } = [];

    public static Exercise Create(ExerciseManifestItem source)
    {
        var exercise = new Exercise(source.Id);
        exercise.Update(source);
        return exercise;
    }

    public void Update(ExerciseManifestItem source)
    {
        Slug = source.Slug;
        Name = source.Name;
        Category = source.Category;
        MovementPattern = source.MovementPattern;
        TrackingMode = source.TrackingMode;
        Setup = source.Setup;
        Execution = source.Execution;
        Safety = source.Safety;

        SynchronizeAliases(source.Aliases);
        SynchronizeEquipment(source.RequiredEquipment);
        SynchronizeMuscles(source.PrimaryMuscles, source.SecondaryMuscles);
    }

    private void SynchronizeAliases(IReadOnlyCollection<string> desiredAliases)
    {
        foreach (var alias in Aliases.Where(item => !desiredAliases.Contains(item.Alias)).ToArray())
        {
            Aliases.Remove(alias);
        }

        foreach (var alias in desiredAliases.Where(value => Aliases.All(item => item.Alias != value)))
        {
            Aliases.Add(new ExerciseAlias(Id, alias));
        }
    }

    private void SynchronizeEquipment(IReadOnlyCollection<EquipmentType> desiredEquipment)
    {
        foreach (var equipment in RequiredEquipment
                     .Where(item => !desiredEquipment.Contains(item.Equipment))
                     .ToArray())
        {
            RequiredEquipment.Remove(equipment);
        }

        foreach (var equipment in desiredEquipment
                     .Where(value => RequiredEquipment.All(item => item.Equipment != value)))
        {
            RequiredEquipment.Add(new ExerciseEquipment(Id, equipment));
        }
    }

    private void SynchronizeMuscles(
        IReadOnlyCollection<MuscleGroup> primaryMuscles,
        IReadOnlyCollection<MuscleGroup> secondaryMuscles)
    {
        var desiredMuscles = primaryMuscles
            .Select(muscle => new ExerciseMuscleSelection(muscle, MuscleRole.Primary))
            .Concat(secondaryMuscles.Select(muscle =>
                new ExerciseMuscleSelection(muscle, MuscleRole.Secondary)))
            .ToArray();

        foreach (var muscle in Muscles
                     .Where(item => desiredMuscles.All(desired => desired.Muscle != item.Muscle))
                     .ToArray())
        {
            Muscles.Remove(muscle);
        }

        foreach (var desired in desiredMuscles)
        {
            var existing = Muscles.SingleOrDefault(item => item.Muscle == desired.Muscle);
            if (existing is null)
            {
                Muscles.Add(new ExerciseMuscle(Id, desired.Muscle, desired.Role));
            }
            else
            {
                existing.SetRole(desired.Role);
            }
        }
    }

    private sealed record ExerciseMuscleSelection(MuscleGroup Muscle, MuscleRole Role);
}

internal sealed class ExerciseAlias(Guid exerciseId, string alias)
{
    public Guid ExerciseId { get; private set; } = exerciseId;

    public string Alias { get; private set; } = alias;
}

internal sealed class ExerciseEquipment(Guid exerciseId, EquipmentType equipment)
{
    public Guid ExerciseId { get; private set; } = exerciseId;

    public EquipmentType Equipment { get; private set; } = equipment;
}

internal sealed class ExerciseMuscle(Guid exerciseId, MuscleGroup muscle, MuscleRole role)
{
    public Guid ExerciseId { get; private set; } = exerciseId;

    public MuscleGroup Muscle { get; private set; } = muscle;

    public MuscleRole Role { get; private set; } = role;

    public void SetRole(MuscleRole role)
    {
        Role = role;
    }
}

internal sealed class ExerciseCatalogueState
{
    public const int SingletonId = 1;

    private ExerciseCatalogueState()
    {
    }

    private ExerciseCatalogueState(
        int catalogueVersion,
        string contentHash,
        ContentReviewStatus reviewStatus,
        DateTimeOffset importedAt)
    {
        Id = SingletonId;
        Update(catalogueVersion, contentHash, reviewStatus, importedAt);
    }

    public int Id { get; private set; }

    public int CatalogueVersion { get; private set; }

    public string ContentHash { get; private set; } = string.Empty;

    public ContentReviewStatus ReviewStatus { get; private set; }

    public DateTimeOffset ImportedAt { get; private set; }

    public static ExerciseCatalogueState Create(
        int catalogueVersion,
        string contentHash,
        ContentReviewStatus reviewStatus,
        DateTimeOffset importedAt)
    {
        return new ExerciseCatalogueState(
            catalogueVersion,
            contentHash,
            reviewStatus,
            importedAt);
    }

    public void Update(
        int catalogueVersion,
        string contentHash,
        ContentReviewStatus reviewStatus,
        DateTimeOffset importedAt)
    {
        CatalogueVersion = catalogueVersion;
        ContentHash = contentHash;
        ReviewStatus = reviewStatus;
        ImportedAt = importedAt;
    }
}
