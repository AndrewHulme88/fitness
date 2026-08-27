namespace FitnessCoach.Api.Features.Exercises;

internal static class ExerciseCatalogueImportCommand
{
    private const string ImportArgument = "--import-exercise-catalogue";
    private static readonly Action<ILogger, ExerciseCatalogueImportStatus, int, int, int, Exception?>
        LogImportCompleted = LoggerMessage.Define<ExerciseCatalogueImportStatus, int, int, int>(
            LogLevel.Information,
            new EventId(1, "ExerciseCatalogueImportCompleted"),
            "Exercise catalogue import {ImportStatus}. Version: {CatalogueVersion}; "
            + "exercises: {ExerciseCount}; added: {AddedCount}");

    public static async Task<bool> TryRunAsync(
        WebApplication app,
        IReadOnlyCollection<string> arguments)
    {
        if (!arguments.Contains(ImportArgument, StringComparer.Ordinal))
        {
            return false;
        }

        await using var scope = app.Services.CreateAsyncScope();
        var importer = scope.ServiceProvider.GetRequiredService<ExerciseCatalogueImporter>();
        var result = await importer.ImportAsync(CancellationToken.None);

        LogImportCompleted(
            app.Logger,
            result.Status,
            result.CatalogueVersion,
            result.ExerciseCount,
            result.AddedCount,
            null);

        return true;
    }
}
