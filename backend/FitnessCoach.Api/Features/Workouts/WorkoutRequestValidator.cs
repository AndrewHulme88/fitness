using FitnessCoach.Api.Features.Exercises;

namespace FitnessCoach.Api.Features.Workouts;

internal static class WorkoutRequestValidator
{
    private const int MaximumExerciseCount = 20;
    private const int MaximumNameLength = 80;
    private const int MaximumPlannedSets = 20;
    private const int MaximumRepetitions = 1000;
    private const decimal MaximumLoadKilograms = 2000;
    private const int MaximumDurationSeconds = 86_400;
    private const decimal MaximumDistanceMetres = 1_000_000;

    public static Dictionary<string, string[]> Validate(
        string name,
        IReadOnlyList<WorkoutExerciseRequest> requests,
        IReadOnlyDictionary<Guid, Exercise> exercises,
        out WorkoutExerciseInput[] inputs)
    {
        var errors = new Dictionary<string, string[]>(StringComparer.Ordinal);

        if (string.IsNullOrWhiteSpace(name)
            || name.Length > MaximumNameLength
            || name != name.Trim())
        {
            errors["name"] =
                [$"Name must be trimmed text of at most {MaximumNameLength} characters."];
        }

        if (requests.Count is < 1 or > MaximumExerciseCount)
        {
            errors["exercises"] =
                [$"Choose between 1 and {MaximumExerciseCount} exercises."];
        }

        if (requests.Select(request => request.ExerciseId).Distinct().Count() != requests.Count)
        {
            errors["exercises"] = ["Each exercise can appear only once in a workout."];
        }

        var parsedInputs = new List<WorkoutExerciseInput>(requests.Count);
        for (var index = 0; index < requests.Count; index++)
        {
            var request = requests[index];
            var field = $"exercises[{index}]";

            if (!exercises.TryGetValue(request.ExerciseId, out var exercise))
            {
                errors[$"{field}.exerciseId"] = ["Choose a curated exercise."];
                continue;
            }

            ValidatePrescription(request, exercise.TrackingMode, field, errors);
            parsedInputs.Add(new WorkoutExerciseInput(
                request.ExerciseId,
                request.PlannedSets,
                request.MinimumRepetitions,
                request.MaximumRepetitions,
                request.TargetLoadKilograms,
                request.TargetDurationSeconds,
                request.TargetDistanceMetres));
        }

        inputs = parsedInputs.ToArray();
        return errors;
    }

    private static void ValidatePrescription(
        WorkoutExerciseRequest request,
        ExerciseTrackingMode trackingMode,
        string field,
        Dictionary<string, string[]> errors)
    {
        if (request.PlannedSets is < 1 or > MaximumPlannedSets)
        {
            errors[$"{field}.plannedSets"] =
                [$"Planned sets must be between 1 and {MaximumPlannedSets}."];
        }

        var hasValidRepetitions = request.MinimumRepetitions is >= 1 and <= MaximumRepetitions
            && request.MaximumRepetitions is >= 1 and <= MaximumRepetitions
            && request.MinimumRepetitions <= request.MaximumRepetitions;
        var hasLoad = request.TargetLoadKilograms is not null;
        var hasValidLoad = request.TargetLoadKilograms is > 0 and <= MaximumLoadKilograms
            && HasAtMostTwoDecimalPlaces(request.TargetLoadKilograms.Value);
        var hasDuration = request.TargetDurationSeconds is not null;
        var hasValidDuration = request.TargetDurationSeconds is >= 1 and <= MaximumDurationSeconds;
        var hasDistance = request.TargetDistanceMetres is not null;
        var hasValidDistance = request.TargetDistanceMetres is > 0 and <= MaximumDistanceMetres
            && HasAtMostTwoDecimalPlaces(request.TargetDistanceMetres.Value);

        if (trackingMode is ExerciseTrackingMode.Repetitions
            or ExerciseTrackingMode.RepetitionsAndLoad)
        {
            if (!hasValidRepetitions)
            {
                errors[$"{field}.repetitions"] =
                    [$"Choose a repetition range between 1 and {MaximumRepetitions}."];
            }

            if (trackingMode is ExerciseTrackingMode.Repetitions && hasLoad)
            {
                errors[$"{field}.targetLoadKilograms"] =
                    ["This exercise does not use a load target."];
            }
            else if (hasLoad && !hasValidLoad)
            {
                errors[$"{field}.targetLoadKilograms"] =
                    [$"Load must be at most {MaximumLoadKilograms} kilograms with up to two decimals."];
            }

            RejectDurationAndDistance(request, field, errors);
            return;
        }

        if (request.MinimumRepetitions is not null || request.MaximumRepetitions is not null)
        {
            errors[$"{field}.repetitions"] =
                ["This exercise does not use a repetition target."];
        }

        if (trackingMode is ExerciseTrackingMode.Duration)
        {
            if (!hasValidDuration)
            {
                errors[$"{field}.targetDurationSeconds"] =
                    [$"Duration must be between 1 and {MaximumDurationSeconds} seconds."];
            }

            if (hasDistance)
            {
                errors[$"{field}.targetDistanceMetres"] =
                    ["This exercise does not use a distance target."];
            }

            if (hasLoad)
            {
                errors[$"{field}.targetLoadKilograms"] =
                    ["This exercise does not use a load target."];
            }

            return;
        }

        if (!hasDistance && !hasDuration)
        {
            errors[$"{field}.target"] = ["Choose a distance, duration, or both."];
        }

        if (hasDistance && !hasValidDistance)
        {
            errors[$"{field}.targetDistanceMetres"] =
                [$"Distance must be at most {MaximumDistanceMetres} metres with up to two decimals."];
        }

        if (hasDuration && !hasValidDuration)
        {
            errors[$"{field}.targetDurationSeconds"] =
                [$"Duration must be between 1 and {MaximumDurationSeconds} seconds."];
        }

        if (trackingMode is ExerciseTrackingMode.DistanceAndDuration && hasLoad)
        {
            errors[$"{field}.targetLoadKilograms"] =
                ["This exercise does not use a load target."];
        }
        else if (hasLoad && !hasValidLoad)
        {
            errors[$"{field}.targetLoadKilograms"] =
                [$"Load must be at most {MaximumLoadKilograms} kilograms with up to two decimals."];
        }
    }

    private static void RejectDurationAndDistance(
        WorkoutExerciseRequest request,
        string field,
        Dictionary<string, string[]> errors)
    {
        if (request.TargetDurationSeconds is not null)
        {
            errors[$"{field}.targetDurationSeconds"] =
                ["This exercise does not use a duration target."];
        }

        if (request.TargetDistanceMetres is not null)
        {
            errors[$"{field}.targetDistanceMetres"] =
                ["This exercise does not use a distance target."];
        }
    }

    private static bool HasAtMostTwoDecimalPlaces(decimal value)
    {
        return decimal.Round(value, 2) == value;
    }
}
