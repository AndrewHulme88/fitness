using System.Text.Json;
using System.Text.Json.Serialization;

using FitnessCoach.Api.Domain;

namespace FitnessCoach.Api.Features.Exercises;

internal sealed record ExerciseCatalogueManifest
{
    public required int CatalogueVersion { get; init; }

    public required ContentReviewStatus ReviewStatus { get; init; }

    public required IReadOnlyList<ExerciseManifestItem> Exercises { get; init; }
}

internal sealed record ExerciseManifestItem
{
    public required Guid Id { get; init; }

    public required string Slug { get; init; }

    public required string Name { get; init; }

    public required IReadOnlyList<string> Aliases { get; init; }

    public required ExerciseCategory Category { get; init; }

    public required ExerciseMovementPattern MovementPattern { get; init; }

    public required ExerciseTrackingMode TrackingMode { get; init; }

    public required IReadOnlyList<EquipmentType> RequiredEquipment { get; init; }

    public required IReadOnlyList<MuscleGroup> PrimaryMuscles { get; init; }

    public required IReadOnlyList<MuscleGroup> SecondaryMuscles { get; init; }

    public required string Setup { get; init; }

    public required string Execution { get; init; }

    public required string Safety { get; init; }
}

internal static class ExerciseCatalogueManifestLoader
{
    private const string ManifestResourceName =
        "FitnessCoach.Api.Features.Exercises.Catalogue.exercise-catalogue.json";

    private static readonly JsonSerializerOptions SerializerOptions = CreateSerializerOptions();

    public static ExerciseCatalogueManifest Load()
    {
        using var stream = typeof(ExerciseCatalogueManifestLoader).Assembly
            .GetManifestResourceStream(ManifestResourceName)
            ?? throw new InvalidOperationException(
                $"Embedded exercise catalogue '{ManifestResourceName}' was not found.");

        try
        {
            return JsonSerializer.Deserialize<ExerciseCatalogueManifest>(stream, SerializerOptions)
                ?? throw new InvalidDataException("The exercise catalogue manifest is empty.");
        }
        catch (JsonException exception)
        {
            throw new InvalidDataException(
                "The exercise catalogue manifest is not valid JSON for the approved schema.",
                exception);
        }
    }

    public static byte[] SerializeCanonical(ExerciseCatalogueManifest manifest)
    {
        return JsonSerializer.SerializeToUtf8Bytes(manifest, SerializerOptions);
    }

    private static JsonSerializerOptions CreateSerializerOptions()
    {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web)
        {
            UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
        };
        options.Converters.Add(
            new JsonStringEnumConverter(JsonNamingPolicy.CamelCase, allowIntegerValues: false));

        return options;
    }
}
