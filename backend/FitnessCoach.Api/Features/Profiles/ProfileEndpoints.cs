using FitnessCoach.Api.Persistence;

using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace FitnessCoach.Api.Features.Profiles;

internal static class ProfileEndpoints
{
    public static IEndpointRouteBuilder MapProfileEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var profiles = endpoints.MapGroup("/profiles").WithTags("Profiles");

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

        dbContext.Add(profile);
        await dbContext.SaveChangesAsync(cancellationToken);

        var response = MapResponse(profile);
        return TypedResults.Created($"/profiles/{profile.Id}", response);
    }

    private static async Task<Results<Ok<TrainingProfileResponse>, NotFound>> GetProfileAsync(
        Guid profileId,
        FitnessCoachDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var profile = await dbContext.Set<TrainingProfile>()
            .AsNoTracking()
            .Include(item => item.Goals)
            .Include(item => item.AvailableEquipment)
            .SingleOrDefaultAsync(item => item.Id == profileId, cancellationToken);

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
