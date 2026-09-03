using FitnessCoach.Api.Persistence;
using FitnessCoach.Api.Features.Identity;

using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.RateLimiting;

using FitnessCoach.Api.Infrastructure;

namespace FitnessCoach.Api.Features.Profiles;

internal static class ProfileEndpoints
{
    public static IEndpointRouteBuilder MapProfileEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var profiles = endpoints.MapGroup("/profiles")
            .WithTags("Profiles")
            .RequireRateLimiting(ApiRateLimitPolicies.Standard);
        profiles.ProducesProblem(StatusCodes.Status429TooManyRequests);
        if (endpoints.ServiceProvider.GetRequiredService<IConfiguration>().GetSection("Cognito").Exists())
        {
            profiles.RequireAuthorization();
        }

        profiles.MapPost("/", CreateProfileAsync)
            .WithName("CreateTrainingProfile")
            .WithSummary("Create a training profile from onboarding choices");
        profiles.MapGet("/{profileId:guid}", GetProfileAsync)
            .WithName("GetTrainingProfile")
            .WithSummary("Get a training profile");

        return endpoints;
    }

    private static async Task<Results<Created<TrainingProfileResponse>, ValidationProblem>>
        CreateProfileAsync(
            CreateTrainingProfileRequest request,
            HttpContext context,
            FitnessCoachDbContext dbContext,
            TimeProvider timeProvider,
            CancellationToken cancellationToken)
    {
        var validationErrors = ProfileRequestValidator.Validate(request);
        if (validationErrors.Count > 0)
        {
            return TypedResults.ValidationProblem(validationErrors);
        }

        var profile = TrainingProfile.Create(
            request.Goals,
            request.Experience,
            request.AvailableEquipment,
            request.UnitSystem,
            timeProvider.GetUtcNow());
        if (context.User.Identity?.IsAuthenticated == true)
        {
            var account = await ApplicationAccountResolver.GetOrCreateAsync(
                context.User, dbContext, timeProvider, cancellationToken);
            profile.Claim(account.Id);
        }

        dbContext.Add(profile);
        await dbContext.SaveChangesAsync(cancellationToken);

        var response = MapResponse(profile);
        return TypedResults.Created($"/profiles/{profile.Id}", response);
    }

    private static async Task<Results<Ok<TrainingProfileResponse>, NotFound>> GetProfileAsync(
        Guid profileId,
        HttpContext context,
        FitnessCoachDbContext dbContext,
        TimeProvider timeProvider,
        CancellationToken cancellationToken)
    {
        var account = context.User.Identity?.IsAuthenticated == true
            ? await ApplicationAccountResolver.GetOrCreateAsync(
                context.User, dbContext, timeProvider, cancellationToken)
            : null;
        var profile = await dbContext.Set<TrainingProfile>()
            .AsNoTracking()
            .AsSplitQuery()
            .Include(item => item.Goals)
            .Include(item => item.AvailableEquipment)
            .SingleOrDefaultAsync(
                item => item.Id == profileId && (account == null || item.AccountId == account.Id),
                cancellationToken);

        return profile is null
            ? TypedResults.NotFound()
            : TypedResults.Ok(MapResponse(profile));
    }

    private static TrainingProfileResponse MapResponse(TrainingProfile profile)
    {
        return new TrainingProfileResponse(
            profile.Id,
            profile.Goals.Select(selection => selection.Goal).Order().ToArray(),
            profile.Experience,
            profile.AvailableEquipment
                .Select(selection => selection.Equipment)
                .Order()
                .ToArray(),
            profile.UnitSystem,
            profile.CreatedAt);
    }
}
