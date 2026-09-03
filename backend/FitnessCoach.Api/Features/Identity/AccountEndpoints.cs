using FitnessCoach.Api.Persistence;

using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.RateLimiting;

using FitnessCoach.Api.Infrastructure;

namespace FitnessCoach.Api.Features.Identity;

internal static class AccountEndpoints
{
    public static IEndpointRouteBuilder MapAccountEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/account", GetAccountAsync)
            .RequireAuthorization()
            .RequireRateLimiting(ApiRateLimitPolicies.Standard)
            .ProducesProblem(StatusCodes.Status429TooManyRequests)
            .WithTags("Account")
            .WithName("GetCurrentAccount")
            .WithSummary("Get the authenticated account and its training profile")
            .Produces<CurrentAccountResponse>();
        return endpoints;
    }

    private static async Task<Ok<CurrentAccountResponse>> GetAccountAsync(
        HttpContext context,
        FitnessCoachDbContext dbContext,
        TimeProvider timeProvider,
        CancellationToken cancellationToken)
    {
        var account = await ApplicationAccountResolver.GetOrCreateAsync(
            context.User, dbContext, timeProvider, cancellationToken);
        var profileId = await dbContext.Set<Profiles.TrainingProfile>()
            .Where(profile => profile.AccountId == account.Id)
            .Select(profile => (Guid?)profile.Id)
            .SingleOrDefaultAsync(cancellationToken);
        return TypedResults.Ok(new CurrentAccountResponse(profileId));
    }
}

public sealed record CurrentAccountResponse(Guid? ProfileId);
