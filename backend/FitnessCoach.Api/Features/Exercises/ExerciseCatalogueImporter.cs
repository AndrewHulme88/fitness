using System.Security.Cryptography;

using FitnessCoach.Api.Persistence;

using Microsoft.EntityFrameworkCore;

namespace FitnessCoach.Api.Features.Exercises;

internal sealed class ExerciseCatalogueImporter(
    FitnessCoachDbContext dbContext,
    TimeProvider timeProvider)
{
    public async Task<ExerciseCatalogueImportResult> ImportAsync(
        CancellationToken cancellationToken)
    {
        var manifest = ExerciseCatalogueManifestLoader.Load();
        var validationErrors = ExerciseCatalogueManifestValidator.Validate(manifest);
        if (validationErrors.Count > 0)
        {
            throw new InvalidDataException(
                "The exercise catalogue manifest failed validation: "
                + string.Join(" ", validationErrors));
        }

        var contentHash = Convert.ToHexStringLower(
            SHA256.HashData(ExerciseCatalogueManifestLoader.SerializeCanonical(manifest)));

        await using var transaction = await dbContext.Database.BeginTransactionAsync(
            cancellationToken);
        var state = await dbContext.Set<ExerciseCatalogueState>()
            .SingleOrDefaultAsync(cancellationToken);
        var exercises = await dbContext.Set<Exercise>()
            .Include(exercise => exercise.Aliases)
            .Include(exercise => exercise.RequiredEquipment)
            .Include(exercise => exercise.Muscles)
            .AsSplitQuery()
            .ToListAsync(cancellationToken);

        ValidateImportProgression(manifest, contentHash, state, exercises);

        var existingById = exercises.ToDictionary(exercise => exercise.Id);
        var addedCount = 0;

        foreach (var source in manifest.Exercises)
        {
            if (existingById.TryGetValue(source.Id, out var exercise))
            {
                exercise.Update(source);
            }
            else
            {
                dbContext.Add(Exercise.Create(source));
                addedCount++;
            }
        }

        if (state is null)
        {
            state = ExerciseCatalogueState.Create(
                manifest.CatalogueVersion,
                contentHash,
                manifest.ReviewStatus,
                timeProvider.GetUtcNow());
            dbContext.Add(state);
        }
        else if (state.CatalogueVersion != manifest.CatalogueVersion
                 || state.ContentHash != contentHash
                 || state.ReviewStatus != manifest.ReviewStatus)
        {
            state.Update(
                manifest.CatalogueVersion,
                contentHash,
                manifest.ReviewStatus,
                timeProvider.GetUtcNow());
        }

        var hasChanges = dbContext.ChangeTracker.HasChanges();
        if (hasChanges)
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        await transaction.CommitAsync(cancellationToken);

        return new ExerciseCatalogueImportResult(
            manifest.CatalogueVersion,
            manifest.Exercises.Count,
            addedCount,
            hasChanges ? ExerciseCatalogueImportStatus.Updated : ExerciseCatalogueImportStatus.Unchanged);
    }

    private static void ValidateImportProgression(
        ExerciseCatalogueManifest manifest,
        string contentHash,
        ExerciseCatalogueState? state,
        List<Exercise> existingExercises)
    {
        if (state is null && existingExercises.Count > 0)
        {
            throw new InvalidOperationException(
                "Exercise rows exist without catalogue state; refusing an ambiguous import.");
        }

        if (state is not null
            && manifest.CatalogueVersion < state.CatalogueVersion)
        {
            throw new InvalidOperationException(
                $"Catalogue version {manifest.CatalogueVersion} cannot replace newer version "
                + $"{state.CatalogueVersion}.");
        }

        if (state is not null
            && manifest.CatalogueVersion == state.CatalogueVersion
            && contentHash != state.ContentHash)
        {
            throw new InvalidOperationException(
                "Catalogue content changed without incrementing catalogueVersion.");
        }

        var manifestIds = manifest.Exercises.Select(exercise => exercise.Id).ToHashSet();
        var removedExercise = existingExercises.FirstOrDefault(exercise => !manifestIds.Contains(exercise.Id));
        if (removedExercise is not null)
        {
            throw new InvalidOperationException(
                $"Exercise '{removedExercise.Slug}' is absent from the manifest. "
                + "Exercise retirement requires an explicit lifecycle design.");
        }

        var manifestIdBySlug = manifest.Exercises.ToDictionary(
            exercise => exercise.Slug,
            exercise => exercise.Id,
            StringComparer.Ordinal);
        var conflictingExercise = existingExercises.FirstOrDefault(exercise =>
            manifestIdBySlug.TryGetValue(exercise.Slug, out var manifestId)
            && manifestId != exercise.Id);
        if (conflictingExercise is not null)
        {
            throw new InvalidOperationException(
                $"Slug '{conflictingExercise.Slug}' cannot be reassigned to a different identifier.");
        }
    }
}

internal enum ExerciseCatalogueImportStatus
{
    Updated,
    Unchanged,
}

internal sealed record ExerciseCatalogueImportResult(
    int CatalogueVersion,
    int ExerciseCount,
    int AddedCount,
    ExerciseCatalogueImportStatus Status);
