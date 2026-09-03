using FitnessCoach.Api.Features.Identity;
using FitnessCoach.Api.Features.Exercises;
using FitnessCoach.Api.Features.Workouts;
using FitnessCoach.Api.Persistence;

using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.RateLimiting;

using Npgsql;

using FitnessCoach.Api.Infrastructure;

namespace FitnessCoach.Api.Features.AiCoach;

internal static class CoachConversationEndpoints
{
    private const int ConversationContextLimit = 8;

    public static IEndpointRouteBuilder MapCoachConversationEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var coach = endpoints.MapGroup("/profiles/{profileId:guid}/coach/conversation")
            .WithTags("AI coach")
            .RequireOwnedProfile()
            .RequireAuthorization()
            .RequireRateLimiting(ApiRateLimitPolicies.Standard);
        coach.ProducesProblem(StatusCodes.Status429TooManyRequests);
        coach.MapGet("/", GetAsync).WithName("GetCoachConversation").WithSummary("Get the retained coach conversation");
        coach.MapPost("/messages", SendAsync)
            .WithName("SendCoachMessage")
            .WithSummary("Ask the read-only AI coach")
            .RequireRateLimiting(ApiRateLimitPolicies.CoachMessages)
            .ProducesProblem(StatusCodes.Status409Conflict);
        coach.MapPost("/proposals/{proposalId:guid}/confirm", ConfirmProposalAsync)
            .WithName("ConfirmCoachWorkoutProposal")
            .WithSummary("Confirm a validated coach workout proposal")
            .ProducesProblem(StatusCodes.Status409Conflict);
        coach.MapDelete("/", DeleteAsync).WithName("DeleteCoachConversation").WithSummary("Delete the retained coach conversation");
        return endpoints;
    }

    private static async Task<Results<Ok<CoachConversationResponse>, NotFound>> GetAsync(
        Guid profileId, FitnessCoachDbContext dbContext, CancellationToken cancellationToken)
    {
        var conversation = await LoadAsync(profileId, dbContext, cancellationToken);
        return conversation is null
            ? TypedResults.NotFound()
            : TypedResults.Ok(Map(conversation, await LoadPendingProposalsAsync(profileId, dbContext, cancellationToken)));
    }

    private static async Task<Results<
        Ok<CoachConversationResponse>,
        ValidationProblem,
        ProblemHttpResult>> SendAsync(
        Guid profileId,
        AskAiCoachRequest request,
        AiCoachService coachService,
        FitnessCoachDbContext dbContext,
        TimeProvider timeProvider,
        CancellationToken cancellationToken)
    {
        var question = request.Question?.Trim();
        if (string.IsNullOrWhiteSpace(question) || question.Length > 1_000)
        {
            return TypedResults.ValidationProblem(new Dictionary<string, string[]>
            {
                ["question"] = ["Ask a fitness question of 1,000 characters or fewer."],
            });
        }
        if (request.WorkoutId is not null
            && (request.ProgressExerciseId is not null || request.ProgressPeriodDays is not null))
        {
            return TypedResults.ValidationProblem(new Dictionary<string, string[]>
            {
                ["review"] = ["Review either one workout or one progress scope at a time."],
            });
        }
        if (request.ProgressExerciseId is not null && request.ProgressPeriodDays is not null)
        {
            return TypedResults.ValidationProblem(new Dictionary<string, string[]>
            {
                ["progress"] = ["Review either one recorded exercise or one recent period at a time."],
            });
        }
        if (request.ProgressPeriodDays is not null && request.ProgressPeriodDays is not (7 or 28))
        {
            return TypedResults.ValidationProblem(new Dictionary<string, string[]>
            {
                ["progressPeriodDays"] = ["Choose a recent 7- or 28-day period."],
            });
        }

        var now = timeProvider.GetUtcNow();
        var conversation = await LoadAsync(profileId, dbContext, cancellationToken)
            ?? CoachConversation.Create(profileId, now);
        if (dbContext.Entry(conversation).State == EntityState.Detached) dbContext.Add(conversation);

        var contextTurns = conversation.Messages
            .OrderByDescending(item => item.CreatedAt)
            .Take(ConversationContextLimit)
            .OrderBy(item => item.CreatedAt)
            .Select(item => new AiCoachConversationTurn(item.Role.ToString(), item.Content))
            .ToArray();
        var userMessage = conversation.AddMessage(CoachMessageRole.User, question, null, [], now);
        dbContext.Entry(userMessage).State = EntityState.Added;
        var answer = await coachService.AskAsync(profileId, request, contextTurns, cancellationToken);
        var coachMessage = conversation.AddMessage(
            CoachMessageRole.Coach,
            answer.Message,
            answer.Kind,
            answer.ContextSources ?? [],
            timeProvider.GetUtcNow());
        dbContext.Entry(coachMessage).State = EntityState.Added;
        var proposal = await CreateValidatedProposalAsync(
            profileId, request.WorkoutId, answer.Proposal, dbContext, timeProvider.GetUtcNow(), cancellationToken);
        if (proposal is not null) dbContext.Add(proposal);
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            return ConversationChangedConflict();
        }
        catch (DbUpdateException exception) when (IsConversationDeleted(exception))
        {
            return ConversationChangedConflict();
        }

        var proposals = await LoadPendingProposalsAsync(profileId, dbContext, cancellationToken);
        return TypedResults.Ok(Map(conversation, proposals));
    }

    private static async Task<Results<NoContent, NotFound>> DeleteAsync(
        Guid profileId, FitnessCoachDbContext dbContext, CancellationToken cancellationToken)
    {
        var conversation = await LoadAsync(profileId, dbContext, cancellationToken);
        if (conversation is null) return TypedResults.NotFound();
        dbContext.Remove(conversation);
        await dbContext.SaveChangesAsync(cancellationToken);
        return TypedResults.NoContent();
    }

    private static async Task<Results<NoContent, NotFound, ValidationProblem, ProblemHttpResult>> ConfirmProposalAsync(
        Guid profileId,
        Guid proposalId,
        FitnessCoachDbContext dbContext,
        TimeProvider timeProvider,
        CancellationToken cancellationToken)
    {
        var proposal = await dbContext.Set<CoachWorkoutProposal>()
            .SingleOrDefaultAsync(item => item.Id == proposalId && item.ProfileId == profileId, cancellationToken);
        if (proposal is null) return TypedResults.NotFound();
        if (proposal.IsConfirmed) return ProposalChangedConflict();

        var workout = await dbContext.Set<WorkoutPlan>()
            .Include(item => item.Exercises)
            .SingleOrDefaultAsync(item => item.Id == proposal.WorkoutId && item.ProfileId == profileId, cancellationToken);
        if (workout is null || workout.Revision != proposal.ExpectedRevision) return ProposalChangedConflict();

        var exercises = await LoadExercisesAsync(proposal.Exercises, dbContext, cancellationToken);
        var errors = WorkoutRequestValidator.Validate(proposal.Name, proposal.Exercises, exercises, out var inputs);
        if (errors.Count > 0) return TypedResults.ValidationProblem(errors);

        workout.Update(proposal.Name, inputs, timeProvider.GetUtcNow());
        proposal.Confirm(timeProvider.GetUtcNow());
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            return ProposalChangedConflict();
        }

        return TypedResults.NoContent();
    }

    private static Task<CoachConversation?> LoadAsync(
        Guid profileId, FitnessCoachDbContext dbContext, CancellationToken cancellationToken) => dbContext.Set<CoachConversation>()
            .Include(item => item.Messages)
            .SingleOrDefaultAsync(item => item.ProfileId == profileId, cancellationToken);

    private static CoachConversationResponse Map(
        CoachConversation conversation,
        IReadOnlyList<AiCoachProposalResponse> proposals) => new(
        conversation.Id,
        conversation.Messages.OrderBy(item => item.CreatedAt).Select(item => new CoachMessageResponse(
            item.Id,
            item.Role == CoachMessageRole.User ? CoachMessageRoleResponse.User : CoachMessageRoleResponse.Coach,
            item.Content,
            item.ResponseKind,
            item.ContextSources,
            item.CreatedAt)).ToArray(),
        proposals);

    private static async Task<CoachWorkoutProposal?> CreateValidatedProposalAsync(
        Guid profileId,
        Guid? selectedWorkoutId,
        AiCoachWorkoutProposal? proposal,
        FitnessCoachDbContext dbContext,
        DateTimeOffset createdAt,
        CancellationToken cancellationToken)
    {
        if (selectedWorkoutId is null
            || proposal is null
            || proposal.WorkoutId != selectedWorkoutId
            || string.IsNullOrWhiteSpace(proposal.Rationale)
            || proposal.Rationale.Length > 600
            || proposal.Rationale != proposal.Rationale.Trim()
            || proposal.Exercises is null)
        {
            return null;
        }

        var workout = await dbContext.Set<WorkoutPlan>()
            .AsNoTracking()
            .SingleOrDefaultAsync(
                item => item.Id == proposal.WorkoutId && item.ProfileId == profileId,
                cancellationToken);
        if (workout is null || workout.Revision != proposal.ExpectedRevision) return null;

        var exercises = await LoadExercisesAsync(proposal.Exercises, dbContext, cancellationToken);
        var errors = WorkoutRequestValidator.Validate(proposal.Name, proposal.Exercises, exercises, out _);
        return errors.Count == 0 ? new CoachWorkoutProposal(profileId, proposal, createdAt) : null;
    }

    private static Task<Dictionary<Guid, Exercise>> LoadExercisesAsync(
        IEnumerable<WorkoutExerciseRequest> requests,
        FitnessCoachDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var ids = requests.Select(item => item.ExerciseId).Distinct().ToArray();
        return dbContext.Set<Exercise>()
            .AsNoTracking()
            .Where(item => ids.Contains(item.Id))
            .Include(item => item.Muscles)
            .AsSplitQuery()
            .ToDictionaryAsync(item => item.Id, cancellationToken);
    }

    private static async Task<IReadOnlyList<AiCoachProposalResponse>> LoadPendingProposalsAsync(
        Guid profileId,
        FitnessCoachDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var proposals = await dbContext.Set<CoachWorkoutProposal>()
            .AsNoTracking()
            .Where(item => item.ProfileId == profileId && item.ConfirmedAt == null)
            .OrderByDescending(item => item.CreatedAt)
            .Take(5)
            .ToListAsync(cancellationToken);
        if (proposals.Count == 0) return [];

        var workoutIds = proposals.Select(item => item.WorkoutId).Distinct().ToArray();
        var workouts = await dbContext.Set<WorkoutPlan>()
            .AsNoTracking()
            .Include(item => item.Exercises)
            .Where(item => workoutIds.Contains(item.Id) && item.ProfileId == profileId)
            .ToDictionaryAsync(item => item.Id, cancellationToken);
        var exerciseIds = proposals.SelectMany(item => item.Exercises.Select(exercise => exercise.ExerciseId))
            .Concat(workouts.Values.SelectMany(item => item.Exercises.Select(exercise => exercise.ExerciseId)))
            .Distinct().ToArray();
        var catalogue = await dbContext.Set<Exercise>()
            .AsNoTracking()
            .Where(item => exerciseIds.Contains(item.Id))
            .ToDictionaryAsync(item => item.Id, cancellationToken);

        return proposals.Select(proposal => new AiCoachProposalResponse(
            proposal.Id, proposal.WorkoutId, proposal.ExpectedRevision, proposal.Rationale,
            proposal.Name, proposal.Exercises,
            workouts.TryGetValue(proposal.WorkoutId, out var workout)
                ? BuildDiff(workout, proposal.Exercises, catalogue) : [],
            proposal.CreatedAt)).ToArray();
    }

    private static List<AiCoachProposalChangeResponse> BuildDiff(
        WorkoutPlan current,
        IReadOnlyList<WorkoutExerciseRequest> proposed,
        IReadOnlyDictionary<Guid, Exercise> catalogue)
    {
        var currentExercises = current.Exercises.OrderBy(item => item.Position).ToArray();
        var currentIds = currentExercises.Select(item => item.ExerciseId).ToHashSet();
        var proposedIds = proposed.Select(item => item.ExerciseId).ToHashSet();
        var changes = new List<AiCoachProposalChangeResponse>();
        foreach (var item in currentExercises.Where(item => proposedIds.Contains(item.ExerciseId)))
        {
            var replacement = proposed.Single(candidate => candidate.ExerciseId == item.ExerciseId);
            if (!HasSamePrescription(item, replacement)) changes.Add(new(
                AiCoachProposalChangeKind.PrescriptionChange,
                MapExercise(item, catalogue), MapExercise(replacement, catalogue)));
        }

        var removals = currentExercises.Where(item => !proposedIds.Contains(item.ExerciseId)).ToArray();
        var additions = proposed.Where(item => !currentIds.Contains(item.ExerciseId)).ToArray();
        var substitutions = Math.Min(removals.Length, additions.Length);
        for (var index = 0; index < substitutions; index++) changes.Add(new(
            AiCoachProposalChangeKind.Substitution,
            MapExercise(removals[index], catalogue), MapExercise(additions[index], catalogue)));
        changes.AddRange(removals.Skip(substitutions).Select(item => new AiCoachProposalChangeResponse(
            AiCoachProposalChangeKind.Removal, MapExercise(item, catalogue), null)));
        changes.AddRange(additions.Skip(substitutions).Select(item => new AiCoachProposalChangeResponse(
            AiCoachProposalChangeKind.Addition, null, MapExercise(item, catalogue))));
        return changes;
    }

    private static bool HasSamePrescription(WorkoutPlanExercise current, WorkoutExerciseRequest proposed) =>
        current.PlannedSets == proposed.PlannedSets
        && current.MinimumRepetitions == proposed.MinimumRepetitions
        && current.MaximumRepetitions == proposed.MaximumRepetitions
        && current.TargetLoadKilograms == proposed.TargetLoadKilograms
        && current.TargetDurationSeconds == proposed.TargetDurationSeconds
        && current.TargetDistanceMetres == proposed.TargetDistanceMetres;

    private static AiCoachProposalExerciseResponse MapExercise(
        WorkoutPlanExercise exercise, IReadOnlyDictionary<Guid, Exercise> catalogue) => new(
        exercise.ExerciseId, catalogue[exercise.ExerciseId].Name, catalogue[exercise.ExerciseId].TrackingMode,
        exercise.PlannedSets, exercise.MinimumRepetitions, exercise.MaximumRepetitions,
        exercise.TargetLoadKilograms, exercise.TargetDurationSeconds, exercise.TargetDistanceMetres);

    private static AiCoachProposalExerciseResponse MapExercise(
        WorkoutExerciseRequest exercise, IReadOnlyDictionary<Guid, Exercise> catalogue) => new(
        exercise.ExerciseId, catalogue[exercise.ExerciseId].Name, catalogue[exercise.ExerciseId].TrackingMode,
        exercise.PlannedSets, exercise.MinimumRepetitions, exercise.MaximumRepetitions,
        exercise.TargetLoadKilograms, exercise.TargetDurationSeconds, exercise.TargetDistanceMetres);

    private static ProblemHttpResult ConversationChangedConflict() => TypedResults.Problem(
        detail: "The saved conversation changed while the coach was preparing a reply. Reload it before trying again.",
        statusCode: StatusCodes.Status409Conflict,
        title: "The coach conversation changed.");

    private static ProblemHttpResult ProposalChangedConflict() => TypedResults.Problem(
        detail: "The workout or proposal changed before confirmation. Review the current workout before trying again.",
        statusCode: StatusCodes.Status409Conflict,
        title: "The proposal can no longer be applied.");

    private static bool IsConversationDeleted(DbUpdateException exception) =>
        exception.InnerException is PostgresException
        {
            SqlState: PostgresErrorCodes.ForeignKeyViolation,
        };
}
