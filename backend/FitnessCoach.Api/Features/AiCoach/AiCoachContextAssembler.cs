using System.Text.Json;

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

        if (ShouldIncludeWorkoutContext(question))
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

            if (ShouldIncludeProposalContext(question))
            {
                var proposalPlans = await dbContext.Set<WorkoutPlan>()
                    .AsNoTracking()
                    .Include(item => item.Exercises)
                    .Where(item => item.ProfileId == profileId)
                    .OrderByDescending(item => item.UpdatedAt)
                    .Take(5)
                    .ToListAsync(cancellationToken);
                if (proposalPlans.Count > 0)
                {
                    facts.Add(new AiCoachContextFact(
                        "Proposal-ready workouts",
                        JsonSerializer.Serialize(proposalPlans.Select(item => new
                        {
                            item.Id,
                            item.Revision,
                            item.Name,
                            Exercises = item.Exercises.OrderBy(exercise => exercise.Position).Select(exercise => new
                            {
                                exercise.ExerciseId,
                                exercise.PlannedSets,
                                exercise.MinimumRepetitions,
                                exercise.MaximumRepetitions,
                                exercise.TargetLoadKilograms,
                                exercise.TargetDurationSeconds,
                                exercise.TargetDistanceMetres,
                            }),
                        }))));
                }
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
            facts);
    }

    private static bool ShouldIncludeWorkoutContext(string question) => ContainsAny(
        question, "workout", "plan", "exercise", "routine", "session");

    private static bool ShouldIncludeHistoryContext(string question) => ContainsAny(
        question, "recent", "history", "progress", "last", "volume", "performance");

    private static bool ShouldIncludeProposalContext(string question) => ContainsAny(
        question, "change", "adjust", "update", "modify", "swap", "replace", "proposal");

    private static bool ContainsAny(string value, params string[] terms) => terms.Any(
        term => value.Contains(term, StringComparison.OrdinalIgnoreCase));
}
