using FitnessCoach.Api.Domain;
using FitnessCoach.Api.Features.Exercises;

namespace FitnessCoach.Api.IntegrationTests;

public sealed class ExerciseCatalogueManifestTests
{
    [Fact]
    public void EmbeddedManifestMeetsTheApprovedCataloguePolicy()
    {
        var manifest = ExerciseCatalogueManifestLoader.Load();

        var errors = ExerciseCatalogueManifestValidator.Validate(manifest);

        Assert.Empty(errors);
        Assert.Equal(1, manifest.CatalogueVersion);
        Assert.Equal(ContentReviewStatus.RequiresQualifiedReview, manifest.ReviewStatus);
        Assert.Equal(35, manifest.Exercises.Count);
        Assert.Equal(
            Enum.GetValues<EquipmentType>().Order().ToArray(),
            manifest.Exercises
                .SelectMany(exercise => exercise.RequiredEquipment)
                .Distinct()
                .Order()
                .ToArray());
        Assert.Equal(
            manifest.Exercises.Count,
            manifest.Exercises.Select(exercise => exercise.Id).Distinct().Count());
    }

    [Fact]
    public void ManifestValidatorRejectsAnUndersizedCatalogueAndDuplicateIdentity()
    {
        var manifest = ExerciseCatalogueManifestLoader.Load();
        var firstExercise = manifest.Exercises[0];
        var invalidManifest = manifest with
        {
            Exercises = manifest.Exercises.Take(28).Append(firstExercise).ToArray(),
        };

        var errors = ExerciseCatalogueManifestValidator.Validate(invalidManifest);

        Assert.Contains(errors, error => error.Contains("at least 30", StringComparison.Ordinal));
        Assert.Contains(errors, error => error.Contains("id must be non-empty and unique", StringComparison.Ordinal));
        Assert.Contains(errors, error => error.Contains("slug must be unique", StringComparison.Ordinal));
    }
}
