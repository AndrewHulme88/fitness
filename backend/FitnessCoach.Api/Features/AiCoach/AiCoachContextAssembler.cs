using FitnessCoach.Api.Features.Exercises;
using FitnessCoach.Api.Features.Profiles;
using FitnessCoach.Api.Features.Sessions;
using FitnessCoach.Api.Features.Workouts;
using FitnessCoach.Api.Persistence;

using Microsoft.EntityFrameworkCore;

namespace FitnessCoach.Api.Features.AiCoach;

internal sealed class AiCoachContextAssembler(FitnessCoachDbContext dbContext) : IAiCoachContextAssembler
{
    public async Task<AiCoachApprovedContext?> AssembleAsync(
        Guid profileId,
        string question,
        Guid? workoutId,
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

        if (ShouldIncludeHistoryContext(question))
        {
            var completed = await dbContext.Set<WorkoutSession>()
                .AsNoTracking()
                .Where(item => item.ProfileId == profileId && item.Status == WorkoutSessionStatus.Completed)
                .OrderByDescending(item => item.FinishedAt)
                .Take(5)
                .Select(item => new { item.WorkoutName, item.FinishedAt })
                .ToListAsync(cancellationToken);
            if (completed.Count > 0)
            {
                facts.Add(new AiCoachContextFact(
                    "Recent completed workouts",
                    string.Join("; ", completed.Select(item => $"{item.WorkoutName} on {item.FinishedAt:yyyy-MM-dd}"))));
            }
        }

        return new AiCoachApprovedContext(
            profile.Goals.Select(goal => goal.Goal).Order().ToArray(),
            profile.Experience,
            profile.AvailableEquipment.Select(equipment => equipment.Equipment).Order().ToArray(),
            profile.UnitSystem,
            facts,
            workout);
    }

    private static bool ShouldIncludeWorkoutContext(string question) => ContainsAny(
        question, "workout", "plan", "exercise", "routine", "session");

    private static bool ShouldIncludeHistoryContext(string question) => ContainsAny(
        question, "recent", "history", "progress", "last", "volume", "performance");

    private static bool ContainsAny(string value, params string[] terms) => terms.Any(
        term => value.Contains(term, StringComparison.OrdinalIgnoreCase));
}
