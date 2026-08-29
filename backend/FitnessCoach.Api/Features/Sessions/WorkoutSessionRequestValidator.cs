using FitnessCoach.Api.Features.Exercises;

namespace FitnessCoach.Api.Features.Sessions;

internal static class WorkoutSessionRequestValidator
{
    private const int MaximumSetsPerExercise = 20;

    public static Dictionary<string, string[]> Validate(
        WorkoutSession session,
        UpdateWorkoutSessionRequest request,
        out UpdateWorkoutSessionInput input)
    {
        var errors = new Dictionary<string, string[]>(StringComparer.Ordinal);
        var exercises = new List<WorkoutSessionExerciseInput>();

        ValidateSessionFields(session, request, errors);

        if (request.Exercises is null)
        {
            errors["exercises"] = ["Include every exercise in the session snapshot."];
        }
        else
        {
            ValidateExercises(session, request.Exercises, exercises, errors);
            if (request.Status == WorkoutSessionStatus.Completed
                && !errors.ContainsKey("exercises")
                && !request.Exercises.SelectMany(item => item.Sets ?? [])
                    .Any(item => item.IsCompleted))
            {
                errors["status"] = ["Complete at least one set before finishing the session."];
            }
        }

        input = new UpdateWorkoutSessionInput(
            request.ClientMutationId,
            request.Status,
            request.FinishedAt?.ToUniversalTime(),
            request.Notes,
            exercises);
        return errors;
    }

    public static Dictionary<string, string[]> ValidateCorrection(
        WorkoutSession session,
        CorrectWorkoutSessionRequest request,
        out CorrectWorkoutSessionInput input)
    {
        var errors = new Dictionary<string, string[]>(StringComparer.Ordinal);
        var exercises = new List<WorkoutSessionExerciseInput>();

        if (request.Notes?.Length > 2000)
        {
            errors["notes"] = ["Keep the session note to 2,000 characters or fewer."];
        }

        if (request.Exercises is null)
        {
            errors["exercises"] = ["Include every exercise and set from the completed session."];
        }
        else
        {
            ValidateExercises(session, request.Exercises, exercises, errors);
            ValidateCorrectionShape(session, request.Exercises, errors);
        }

        input = new CorrectWorkoutSessionInput(request.Notes, exercises);
        return errors;
    }

    private static void ValidateSessionFields(
        WorkoutSession session,
        UpdateWorkoutSessionRequest request,
        Dictionary<string, string[]> errors)
    {
        if (request.ClientMutationId == Guid.Empty)
        {
            errors["clientMutationId"] = ["Provide a stable mutation identifier."];
        }

        if (request.Notes?.Length > 2000)
        {
            errors["notes"] = ["Keep the session note to 2,000 characters or fewer."];
        }

        if (request.Status == WorkoutSessionStatus.Active && request.FinishedAt is not null)
        {
            errors["finishedAt"] = ["An active session cannot have a finish time."];
        }
        else if (request.Status == WorkoutSessionStatus.Completed)
        {
            if (request.FinishedAt is null)
            {
                errors["finishedAt"] = ["Provide the time the session finished."];
            }
            else if (request.FinishedAt.Value.ToUniversalTime() < session.StartedAt)
            {
                errors["finishedAt"] = ["The finish time cannot be before the session started."];
            }
        }
    }

    private static void ValidateExercises(
        WorkoutSession session,
        IReadOnlyList<WorkoutSessionExerciseRequest> requests,
        List<WorkoutSessionExerciseInput> inputs,
        Dictionary<string, string[]> errors)
    {
        var expectedIds = session.Exercises.Select(item => item.ExerciseId).ToHashSet();
        if (requests.Any(item => item is null)
            || requests.Count != expectedIds.Count
            || requests.Select(item => item.ExerciseId).Distinct().Count() != requests.Count
            || requests.Any(item => !expectedIds.Contains(item.ExerciseId)))
        {
            errors["exercises"] = ["The session exercises must match its workout snapshot."];
            return;
        }

        for (var exerciseIndex = 0; exerciseIndex < requests.Count; exerciseIndex++)
        {
            var request = requests[exerciseIndex];
            var snapshot = session.Exercises.Single(item => item.ExerciseId == request.ExerciseId);
            var prefix = $"exercises[{exerciseIndex}]";

            if (request.Notes?.Length > 1000)
            {
                errors[$"{prefix}.notes"] = ["Keep the exercise note to 1,000 characters or fewer."];
            }

            if (request.Sets is null
                || request.Sets.Count is < 1 or > MaximumSetsPerExercise)
            {
                errors[$"{prefix}.sets"] = ["Keep between 1 and 20 sets for each exercise."];
                continue;
            }

            if (request.Sets.Any(item => item is null)
                || request.Sets.Any(item => item.SetId == Guid.Empty)
                || request.Sets.Select(item => item.SetId).Distinct().Count() != request.Sets.Count)
            {
                errors[$"{prefix}.sets"] = ["Every set needs a unique stable identifier."];
                continue;
            }

            var sets = new List<WorkoutSessionSetInput>(request.Sets.Count);
            for (var setIndex = 0; setIndex < request.Sets.Count; setIndex++)
            {
                var set = request.Sets[setIndex];
                ValidateSet(snapshot.TrackingMode, set, $"{prefix}.sets[{setIndex}]", errors);
                sets.Add(new WorkoutSessionSetInput(
                    set.SetId,
                    set.IsCompleted,
                    set.CompletedAt?.ToUniversalTime(),
                    set.ActualRepetitions,
                    set.ActualLoadKilograms,
                    set.ActualDurationSeconds,
                    set.ActualDistanceMetres));
            }

            inputs.Add(new WorkoutSessionExerciseInput(
                request.ExerciseId,
                request.IsSkipped,
                request.Notes,
                sets));
        }

        if (inputs.SelectMany(item => item.Sets).Select(item => item.SetId).Distinct().Count()
            != inputs.Sum(item => item.Sets.Count))
        {
            errors["exercises"] = ["Every set in the session needs a unique stable identifier."];
        }

        var existingOwners = session.Exercises
            .SelectMany(exercise => exercise.Sets.Select(set => new
            {
                set.Id,
                exercise.ExerciseId,
            }))
            .ToDictionary(item => item.Id, item => item.ExerciseId);
        if (inputs.Any(exercise => exercise.Sets.Any(set =>
                existingOwners.TryGetValue(set.SetId, out var ownerId)
                && ownerId != exercise.ExerciseId)))
        {
            errors["exercises"] = ["An existing set cannot move to another exercise."];
        }
    }

    private static void ValidateCorrectionShape(
        WorkoutSession session,
        IReadOnlyList<WorkoutSessionExerciseRequest> requests,
        Dictionary<string, string[]> errors)
    {
        if (errors.ContainsKey("exercises"))
        {
            return;
        }

        foreach (var request in requests)
        {
            var existing = session.Exercises.Single(item => item.ExerciseId == request.ExerciseId);
            var existingSetIds = existing.Sets
                .OrderBy(item => item.Position)
                .Select(item => item.Id);
            var requestedSetIds = request.Sets.Select(item => item.SetId);
            if (!existingSetIds.SequenceEqual(requestedSetIds))
            {
                errors["exercises"] =
                    ["Corrections cannot add, remove, reorder, or move recorded sets."];
                return;
            }
        }
    }

    private static void ValidateSet(
        ExerciseTrackingMode mode,
        WorkoutSessionSetRequest set,
        string prefix,
        Dictionary<string, string[]> errors)
    {
        if (set.IsCompleted != (set.CompletedAt is not null))
        {
            errors[$"{prefix}.completedAt"] =
                ["A completed set needs a completion time; an incomplete set must not have one."];
        }

        ValidateRange(set.ActualRepetitions, 1, 1000, $"{prefix}.actualRepetitions", errors);
        ValidateRange(set.ActualLoadKilograms, 0.01m, 2000m, $"{prefix}.actualLoadKilograms", errors);
        ValidateRange(set.ActualDurationSeconds, 1, 86400, $"{prefix}.actualDurationSeconds", errors);
        ValidateRange(set.ActualDistanceMetres, 0.01m, 1_000_000m, $"{prefix}.actualDistanceMetres", errors);
        ValidateScale(set.ActualLoadKilograms, $"{prefix}.actualLoadKilograms", errors);
        ValidateScale(set.ActualDistanceMetres, $"{prefix}.actualDistanceMetres", errors);

        var allowsRepetitions = mode is ExerciseTrackingMode.Repetitions
            or ExerciseTrackingMode.RepetitionsAndLoad;
        var allowsLoad = mode is ExerciseTrackingMode.RepetitionsAndLoad
            or ExerciseTrackingMode.DistanceDurationAndLoad;
        var allowsDuration = mode is ExerciseTrackingMode.Duration
            or ExerciseTrackingMode.DistanceAndDuration
            or ExerciseTrackingMode.DistanceDurationAndLoad;
        var allowsDistance = mode is ExerciseTrackingMode.DistanceAndDuration
            or ExerciseTrackingMode.DistanceDurationAndLoad;

        RejectUnsupported(set.ActualRepetitions, allowsRepetitions, $"{prefix}.actualRepetitions", errors);
        RejectUnsupported(set.ActualLoadKilograms, allowsLoad, $"{prefix}.actualLoadKilograms", errors);
        RejectUnsupported(set.ActualDurationSeconds, allowsDuration, $"{prefix}.actualDurationSeconds", errors);
        RejectUnsupported(set.ActualDistanceMetres, allowsDistance, $"{prefix}.actualDistanceMetres", errors);

        if (!set.IsCompleted)
        {
            return;
        }

        RequireValue(set.ActualRepetitions, allowsRepetitions, $"{prefix}.actualRepetitions", errors);
        RequireValue(set.ActualLoadKilograms, allowsLoad, $"{prefix}.actualLoadKilograms", errors);
        RequireValue(set.ActualDurationSeconds, allowsDuration, $"{prefix}.actualDurationSeconds", errors);
        RequireValue(set.ActualDistanceMetres, allowsDistance, $"{prefix}.actualDistanceMetres", errors);
    }

    private static void ValidateRange<T>(
        T? value,
        T minimum,
        T maximum,
        string field,
        Dictionary<string, string[]> errors)
        where T : struct, IComparable<T>
    {
        if (value is not null
            && (value.Value.CompareTo(minimum) < 0
                || value.Value.CompareTo(maximum) > 0))
        {
            errors[field] = ["Enter a supported positive value."];
        }
    }

    private static void RejectUnsupported<T>(
        T? value,
        bool allowed,
        string field,
        Dictionary<string, string[]> errors)
        where T : struct
    {
        if (!allowed && value is not null)
        {
            errors[field] = ["This value is not used for the exercise tracking mode."];
        }
    }

    private static void ValidateScale(
        decimal? value,
        string field,
        Dictionary<string, string[]> errors)
    {
        if (value is not null && decimal.Round(value.Value, 2) != value.Value)
        {
            errors[field] = ["Use no more than two decimal places."];
        }
    }

    private static void RequireValue<T>(
        T? value,
        bool required,
        string field,
        Dictionary<string, string[]> errors)
        where T : struct
    {
        if (required && value is null)
        {
            errors[field] = ["Enter this value before completing the set."];
        }
    }
}
