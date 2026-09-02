using FitnessCoach.Api.Features.Exercises;
using FitnessCoach.Api.Features.Profiles;
using FitnessCoach.Api.Features.Sessions;
using FitnessCoach.Api.Features.Workouts;
using FitnessCoach.Api.Persistence;

using Microsoft.EntityFrameworkCore;

namespace FitnessCoach.Api.Features.AiCoach;

internal sealed class AiCoachContextAssembler(
    FitnessCoachDbContext dbContext,
    TimeProvider timeProvider) : IAiCoachContextAssembler
{
    public async Task<AiCoachApprovedContext?> AssembleAsync(
        Guid profileId,
        string question,
        Guid? workoutId,
        Guid? progressExerciseId,
        int? progressPeriodDays,
        CancellationToken cancellationToken)
    {
        var profile = await dbContext.Set<TrainingProfile>()
            .AsNoTracking()
            .Include(profile => profile.Goals)
            .Include(profile => profile.AvailableEquipment)
            .AsSplitQuery()
            .Where(profile => profile.Id == profileId)
            .SingleOrDefaultAsync(cancellationToken);
        if (profile is null) return null;

        var facts = new List<AiCoachContextFact>
        {
            new("Training profile", $"Goals: {string.Join(", ", profile.Goals.Select(item => item.Goal))}; experience: {profile.Experience}; equipment: {string.Join(", ", profile.AvailableEquipment.Select(item => item.Equipment))}; units: {profile.UnitSystem}."),
        };

        AiCoachWorkoutSnapshot? workout = null;
        if (workoutId is not null)
        {
            var selectedWorkout = await dbContext.Set<WorkoutPlan>()
                .AsNoTracking()
                .Include(item => item.Exercises)
                .AsSplitQuery()
                .SingleOrDefaultAsync(
                    item => item.Id == workoutId && item.ProfileId == profileId,
                    cancellationToken);
            if (selectedWorkout is null) return null;

            var exerciseIds = selectedWorkout.Exercises.Select(item => item.ExerciseId).ToArray();
            var catalogue = await dbContext.Set<Exercise>()
                .AsNoTracking()
                .Where(item => exerciseIds.Contains(item.Id))
                .ToDictionaryAsync(item => item.Id, cancellationToken);
            workout = new AiCoachWorkoutSnapshot(
                selectedWorkout.Id, selectedWorkout.Revision, selectedWorkout.Name,
                selectedWorkout.Exercises.OrderBy(item => item.Position).Select(item =>
                {
                    var exercise = catalogue[item.ExerciseId];
                    return new AiCoachWorkoutSnapshotExercise(
                        item.ExerciseId, exercise.Name, exercise.TrackingMode.ToString(), item.PlannedSets,
                        item.MinimumRepetitions, item.MaximumRepetitions, item.TargetLoadKilograms,
                        item.TargetDurationSeconds, item.TargetDistanceMetres);
                }).ToArray());
            facts.Add(new AiCoachContextFact("Selected workout", $"{workout.Name} (revision {workout.Revision})."));
        }
        else if (ShouldIncludeWorkoutContext(question))
        {
            var plans = await dbContext.Set<WorkoutPlan>()
                .AsNoTracking()
                .Where(item => item.ProfileId == profileId)
                .OrderByDescending(item => item.UpdatedAt)
                .Take(5)
                .Select(item => new { item.Name, ExerciseCount = item.Exercises.Count })
                .ToListAsync(cancellationToken);
            if (plans.Count > 0)
            {
                facts.Add(new AiCoachContextFact(
                    "Current workout plans",
                    string.Join("; ", plans.Select(item => $"{item.Name} ({item.ExerciseCount} exercises)"))));
            }

        }

        var progress = await AssembleProgressAsync(
            profileId, progressExerciseId, progressPeriodDays, cancellationToken);
        if (progress is not null)
        {
            facts.Add(new AiCoachContextFact("Recorded progress", progress.Scope));
        }

        return new AiCoachApprovedContext(
            profile.Goals.Select(goal => goal.Goal).Order().ToArray(),
            profile.Experience,
            profile.AvailableEquipment.Select(equipment => equipment.Equipment).Order().ToArray(),
            profile.UnitSystem,
            facts,
            workout,
            progress);
    }

    private static bool ShouldIncludeWorkoutContext(string question) => ContainsAny(
        question, "workout", "plan", "exercise", "routine", "session");

    private static bool ContainsAny(string value, params string[] terms) => terms.Any(
        term => value.Contains(term, StringComparison.OrdinalIgnoreCase));

    private async Task<AiCoachProgressSnapshot?> AssembleProgressAsync(
        Guid profileId,
        Guid? exerciseId,
        int? periodDays,
        CancellationToken cancellationToken)
    {
        if (exerciseId is not null)
        {
            var appearances = await dbContext.Set<WorkoutSession>()
                .AsNoTracking()
                .Where(session => session.ProfileId == profileId
                    && session.Status == WorkoutSessionStatus.Completed)
                .SelectMany(session => session.Exercises
                    .Where(exercise => exercise.ExerciseId == exerciseId
                        && exercise.Sets.Any(set => set.IsCompleted))
                    .Select(exercise => new
                    {
                        SessionId = session.Id,
                        session.WorkoutName,
                        PerformedAt = session.FinishedAt!.Value,
                        exercise.ExerciseName,
                        exercise.TrackingMode,
                    }))
                .OrderByDescending(item => item.PerformedAt)
                .ThenByDescending(item => item.SessionId)
                .Take(12)
                .ToListAsync(cancellationToken);
            if (appearances.Count == 0) return null;

            var sessionIds = appearances.Select(item => item.SessionId).ToArray();
            var sets = await dbContext.Set<WorkoutSessionSet>()
                .AsNoTracking()
                .Where(set => sessionIds.Contains(set.WorkoutSessionId)
                    && set.ExerciseId == exerciseId && set.IsCompleted)
                .OrderBy(set => set.WorkoutSessionId).ThenBy(set => set.Position)
                .Select(set => new
                {
                    set.WorkoutSessionId,
                    Set = new AiCoachRecordedSetSnapshot(set.Position, set.ActualRepetitions,
                        set.ActualLoadKilograms, set.ActualDurationSeconds, set.ActualDistanceMetres),
                })
                .ToListAsync(cancellationToken);
            var setsBySession = sets.GroupBy(item => item.WorkoutSessionId).ToDictionary(
                group => group.Key,
                group => (IReadOnlyList<AiCoachRecordedSetSnapshot>)group.Select(item => item.Set).ToArray());
            var first = appearances[0];
            var exercise = new AiCoachExerciseProgressSnapshot(
                exerciseId.Value, first.ExerciseName, first.TrackingMode.ToString(),
                appearances.Select(item => new AiCoachExerciseAppearanceSnapshot(
                    item.SessionId, item.WorkoutName, item.PerformedAt, setsBySession[item.SessionId])).ToArray());
            return new AiCoachProgressSnapshot(
                $"Recorded completed sets for {first.ExerciseName} from its {appearances.Count} most recent appearance(s).",
                appearances.Min(item => item.PerformedAt), appearances.Max(item => item.PerformedAt), Exercise: exercise);
        }

        if (periodDays is null) return null;
        var periodEnd = timeProvider.GetUtcNow();
        var periodStart = periodEnd.AddDays(-periodDays.Value);
        var sessions = dbContext.Set<WorkoutSession>().AsNoTracking().Where(item =>
            item.ProfileId == profileId && item.Status == WorkoutSessionStatus.Completed
            && item.FinishedAt >= periodStart && item.FinishedAt <= periodEnd);
        var completedWorkoutCount = await sessions.CountAsync(cancellationToken);
        var completedSetCount = await sessions.SelectMany(session => session.Exercises)
            .SelectMany(exercise => exercise.Sets).CountAsync(set => set.IsCompleted, cancellationToken);
        var duration = await sessions.SumAsync(session =>
            (session.FinishedAt!.Value - session.StartedAt).TotalSeconds, cancellationToken);
        return new AiCoachProgressSnapshot(
            $"Recorded completed-workout totals for the most recent {periodDays} days.", periodStart, periodEnd,
            completedWorkoutCount, completedSetCount, (int)Math.Max(0, Math.Min(duration, int.MaxValue)));
    }
}
