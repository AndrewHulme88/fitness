using FitnessCoach.Api.Features.Profiles;
using FitnessCoach.Api.Persistence;

using Microsoft.EntityFrameworkCore;

namespace FitnessCoach.Api.Features.AiCoach;

internal sealed class AiCoachContextAssembler(FitnessCoachDbContext dbContext) : IAiCoachContextAssembler
{
    public async Task<AiCoachApprovedContext?> AssembleAsync(
        Guid profileId,
        CancellationToken cancellationToken)
    {
        return await dbContext.Set<TrainingProfile>()
            .AsNoTracking()
            .Include(profile => profile.Goals)
            .Include(profile => profile.AvailableEquipment)
            .AsSplitQuery()
            .Where(profile => profile.Id == profileId)
            .Select(profile => new AiCoachApprovedContext(
                profile.Goals.Select(goal => goal.Goal).Order().ToArray(),
                profile.Experience,
                profile.AvailableEquipment.Select(equipment => equipment.Equipment).Order().ToArray(),
                profile.UnitSystem))
            .SingleOrDefaultAsync(cancellationToken);
    }
}
