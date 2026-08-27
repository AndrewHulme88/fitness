using System.Text.RegularExpressions;

using FitnessCoach.Api.Domain;

namespace FitnessCoach.Api.Features.Exercises;

internal static partial class ExerciseCatalogueManifestValidator
{
    private const int MinimumExerciseCount = 30;
    private const int InitialMaximumExerciseCount = 35;
    private const int AbsoluteMaximumExerciseCount = 500;

    public static IReadOnlyList<string> Validate(ExerciseCatalogueManifest manifest)
    {
        var errors = new List<string>();

        if (manifest.CatalogueVersion <= 0)
        {
            errors.Add("catalogueVersion must be greater than zero.");
        }

        if (!Enum.IsDefined(manifest.ReviewStatus))
        {
            errors.Add("reviewStatus must be supported.");
        }

        if (manifest.Exercises.Count is < MinimumExerciseCount or > AbsoluteMaximumExerciseCount)
        {
            errors.Add(
                $"exercises must contain at least {MinimumExerciseCount} and no more than "
                + $"{AbsoluteMaximumExerciseCount} curated entries.");
        }

        if (manifest.CatalogueVersion == 1
            && manifest.Exercises.Count > InitialMaximumExerciseCount)
        {
            errors.Add(
                $"Catalogue version 1 cannot exceed {InitialMaximumExerciseCount} entries.");
        }

        var identifiers = new HashSet<Guid>();
        var slugs = new HashSet<string>(StringComparer.Ordinal);
        var searchableNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var exercise in manifest.Exercises)
        {
            ValidateExercise(exercise, identifiers, slugs, searchableNames, errors);
        }

        var representedEquipment = manifest.Exercises
            .SelectMany(exercise => exercise.RequiredEquipment)
            .ToHashSet();
        foreach (var equipment in Enum.GetValues<EquipmentType>())
        {
            if (!representedEquipment.Contains(equipment))
            {
                errors.Add($"The catalogue does not represent equipment '{equipment}'.");
            }
        }

        return errors;
    }

    private static void ValidateExercise(
        ExerciseManifestItem exercise,
        HashSet<Guid> identifiers,
        HashSet<string> slugs,
        HashSet<string> searchableNames,
        List<string> errors)
    {
        var location = exercise.Id == Guid.Empty ? "exercise with an empty id" : exercise.Id.ToString();

        if (exercise.Id == Guid.Empty || !identifiers.Add(exercise.Id))
        {
            errors.Add($"{location}: id must be non-empty and unique.");
        }

        if (string.IsNullOrWhiteSpace(exercise.Slug)
            || exercise.Slug.Length > 80
            || !SlugPattern().IsMatch(exercise.Slug)
            || !slugs.Add(exercise.Slug))
        {
            errors.Add($"{location}: slug must be unique kebab-case text of at most 80 characters.");
        }

        ValidateSearchableText(exercise.Name, 120, "name", location, searchableNames, errors);

        if (exercise.Aliases.Count > 8)
        {
            errors.Add($"{location}: no more than eight aliases are allowed.");
        }

        foreach (var alias in exercise.Aliases)
        {
            ValidateSearchableText(alias, 100, "alias", location, searchableNames, errors);
        }

        ValidateEnum(exercise.Category, "category", location, errors);
        ValidateEnum(exercise.MovementPattern, "movementPattern", location, errors);
        ValidateEnum(exercise.TrackingMode, "trackingMode", location, errors);
        ValidateSelection(exercise.RequiredEquipment, "requiredEquipment", location, errors);
        ValidateSelection(exercise.PrimaryMuscles, "primaryMuscles", location, errors);
        ValidateSelection(
            exercise.SecondaryMuscles,
            "secondaryMuscles",
            location,
            errors,
            allowEmpty: true);

        if (exercise.PrimaryMuscles.Intersect(exercise.SecondaryMuscles).Any())
        {
            errors.Add($"{location}: a muscle cannot be both primary and secondary.");
        }

        ValidateBoundedText(exercise.Setup, 500, "setup", location, errors);
        ValidateBoundedText(exercise.Execution, 700, "execution", location, errors);
        ValidateBoundedText(exercise.Safety, 500, "safety", location, errors);

        if (exercise.Category is ExerciseCategory.Cardio
            && (exercise.MovementPattern is not ExerciseMovementPattern.Locomotion
                || exercise.TrackingMode is not ExerciseTrackingMode.DistanceAndDuration))
        {
            errors.Add(
                $"{location}: cardio exercises must use locomotion and distance-and-duration tracking.");
        }

        if (exercise.Category is ExerciseCategory.Strength
            && exercise.TrackingMode is ExerciseTrackingMode.DistanceAndDuration)
        {
            errors.Add($"{location}: strength exercises cannot use cardio-only tracking.");
        }

        if (exercise.MovementPattern is ExerciseMovementPattern.Carry
            && exercise.TrackingMode is not ExerciseTrackingMode.DistanceDurationAndLoad)
        {
            errors.Add($"{location}: carries must track distance, duration, and load.");
        }
    }

    private static void ValidateSearchableText(
        string value,
        int maximumLength,
        string fieldName,
        string location,
        HashSet<string> searchableNames,
        List<string> errors)
    {
        ValidateBoundedText(value, maximumLength, fieldName, location, errors);

        if (!string.IsNullOrWhiteSpace(value) && !searchableNames.Add(value.Trim()))
        {
            errors.Add($"{location}: {fieldName} '{value}' duplicates another name or alias.");
        }
    }

    private static void ValidateBoundedText(
        string value,
        int maximumLength,
        string fieldName,
        string location,
        List<string> errors)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Length > maximumLength || value != value.Trim())
        {
            errors.Add(
                $"{location}: {fieldName} must be trimmed, non-empty text of at most "
                + $"{maximumLength} characters.");
        }
    }

    private static void ValidateSelection<T>(
        IReadOnlyCollection<T> values,
        string fieldName,
        string location,
        List<string> errors,
        bool allowEmpty = false)
        where T : struct, Enum
    {
        if ((!allowEmpty && values.Count == 0)
            || values.Count > Enum.GetValues<T>().Length
            || values.Any(value => !Enum.IsDefined(value))
            || values.Distinct().Count() != values.Count)
        {
            errors.Add($"{location}: {fieldName} must contain unique supported values.");
        }
    }

    private static void ValidateEnum<T>(
        T value,
        string fieldName,
        string location,
        List<string> errors)
        where T : struct, Enum
    {
        if (!Enum.IsDefined(value))
        {
            errors.Add($"{location}: {fieldName} must be supported.");
        }
    }

    [GeneratedRegex("^[a-z0-9]+(?:-[a-z0-9]+)*$", RegexOptions.CultureInvariant)]
    private static partial Regex SlugPattern();
}
