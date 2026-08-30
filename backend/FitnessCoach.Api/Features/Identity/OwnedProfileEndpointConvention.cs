using FitnessCoach.Api.Features.Profiles;
using FitnessCoach.Api.Persistence;

using Microsoft.EntityFrameworkCore;

namespace FitnessCoach.Api.Features.Identity;

internal static class OwnedProfileEndpointConvention
{
    public static RouteGroupBuilder RequireOwnedProfile(this RouteGroupBuilder group)
    {
        group.AddEndpointFilter(async (context, next) =>
        {
            if (context.HttpContext.User.Identity?.IsAuthenticated != true)
            {
                return await next(context);
            }

            var routeValue = context.HttpContext.Request.RouteValues["profileId"]?.ToString();
            if (!Guid.TryParse(routeValue, out var profileId))
            {
                return Results.NotFound();
            }

            var services = context.HttpContext.RequestServices;
            var dbContext = services.GetRequiredService<FitnessCoachDbContext>();
            var timeProvider = services.GetRequiredService<TimeProvider>();
            var account = await ApplicationAccountResolver.GetOrCreateAsync(
                context.HttpContext.User,
                dbContext,
                timeProvider,
                context.HttpContext.RequestAborted);
            var ownsProfile = await dbContext.Set<TrainingProfile>()
                .AsNoTracking()
                .AnyAsync(
                    profile => profile.Id == profileId && profile.AccountId == account.Id,
                    context.HttpContext.RequestAborted);

            return ownsProfile ? await next(context) : Results.NotFound();
        });
        return group;
    }
}
