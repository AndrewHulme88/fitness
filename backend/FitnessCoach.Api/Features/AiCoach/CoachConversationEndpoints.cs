using FitnessCoach.Api.Features.Identity;
using FitnessCoach.Api.Persistence;

using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace FitnessCoach.Api.Features.AiCoach;

internal static class CoachConversationEndpoints
{
    private const int ConversationContextLimit = 8;

    public static IEndpointRouteBuilder MapCoachConversationEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var coach = endpoints.MapGroup("/profiles/{profileId:guid}/coach/conversation")
            .WithTags("AI coach")
            .RequireOwnedProfile()
            .RequireAuthorization();
        coach.MapGet("/", GetAsync).WithName("GetCoachConversation").WithSummary("Get the retained coach conversation");
        coach.MapPost("/messages", SendAsync).WithName("SendCoachMessage").WithSummary("Ask the read-only AI coach");
        coach.MapDelete("/", DeleteAsync).WithName("DeleteCoachConversation").WithSummary("Delete the retained coach conversation");
        return endpoints;
    }

    private static async Task<Results<Ok<CoachConversationResponse>, NotFound>> GetAsync(
        Guid profileId, FitnessCoachDbContext dbContext, CancellationToken cancellationToken)
    {
        var conversation = await LoadAsync(profileId, dbContext, cancellationToken);
        return conversation is null ? TypedResults.NotFound() : TypedResults.Ok(Map(conversation));
    }

    private static async Task<Results<Ok<CoachConversationResponse>, ValidationProblem>> SendAsync(
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
        conversation.AddMessage(CoachMessageRole.User, question, null, [], now);
        var answer = await coachService.AskAsync(profileId, request, contextTurns, cancellationToken);
        conversation.AddMessage(
            CoachMessageRole.Coach,
            answer.Message,
            answer.Kind,
            answer.ContextSources ?? [],
            timeProvider.GetUtcNow());
        await dbContext.SaveChangesAsync(cancellationToken);
        return TypedResults.Ok(Map(conversation));
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

    private static Task<CoachConversation?> LoadAsync(
        Guid profileId, FitnessCoachDbContext dbContext, CancellationToken cancellationToken) => dbContext.Set<CoachConversation>()
            .Include(item => item.Messages)
            .SingleOrDefaultAsync(item => item.ProfileId == profileId, cancellationToken);

    private static CoachConversationResponse Map(CoachConversation conversation) => new(
        conversation.Id,
        conversation.Messages.OrderBy(item => item.CreatedAt).Select(item => new CoachMessageResponse(
            item.Id,
            item.Role == CoachMessageRole.User ? CoachMessageRoleResponse.User : CoachMessageRoleResponse.Coach,
            item.Content,
            item.ResponseKind,
            item.ContextSources,
            item.CreatedAt)).ToArray());
}
