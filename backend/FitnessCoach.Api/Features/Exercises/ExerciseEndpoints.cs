using System.Globalization;
using System.Text.Json;

using FitnessCoach.Api.Domain;
using FitnessCoach.Api.Persistence;

using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi;
using Microsoft.AspNetCore.RateLimiting;

using FitnessCoach.Api.Infrastructure;

namespace FitnessCoach.Api.Features.Exercises;

internal static class ExerciseEndpoints
{
    private const int DefaultLimit = 20;
    private const int MaximumLimit = 50;
    private const int MaximumOffset = 10_000;
    private const int MaximumSearchLength = 100;

    public static IEndpointRouteBuilder MapExerciseEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var exercises = endpoints.MapGroup("/exercises")
            .WithTags("Exercises")
            .RequireRateLimiting(ApiRateLimitPolicies.Standard);
        exercises.ProducesProblem(StatusCodes.Status429TooManyRequests);
        if (endpoints.ServiceProvider.GetRequiredService<IConfiguration>().GetSection("Cognito").Exists())
        {
            exercises.RequireAuthorization();
        }

        exercises.MapGet("/", SearchExercisesAsync)
            .WithName("SearchExercises")
            .WithSummary("Search and filter the curated exercise catalogue")
            .AddOpenApiOperationTransformer(ConfigureSearchContractAsync);
        exercises.MapGet("/{exerciseId:guid}", GetExerciseAsync)
            .WithName("GetExercise")
            .WithSummary("Get one curated exercise");

        return endpoints;
    }

    private static async Task<Results<Ok<ExerciseSearchResponse>, ValidationProblem>>
        SearchExercisesAsync(
            FitnessCoachDbContext dbContext,
            string? query = null,
            string? category = null,
            string? movementPattern = null,
            string? trackingMode = null,
            string[]? availableEquipment = null,
            string? primaryMuscle = null,
            string? limit = null,
            string? offset = null,
            CancellationToken cancellationToken = default)
    {
        var validationErrors = ValidateSearch(
            query,
            category,
            movementPattern,
            trackingMode,
            availableEquipment,
            primaryMuscle,
            limit,
            offset,
            out var filters);
        if (validationErrors.Count > 0)
        {
            return TypedResults.ValidationProblem(validationErrors);
        }

        var exercisesQuery = dbContext.Set<Exercise>().AsNoTracking().AsQueryable();
        var trimmedQuery = query?.Trim();

        if (!string.IsNullOrEmpty(trimmedQuery))
        {
            var searchPattern = $"%{EscapeLikePattern(trimmedQuery)}%";
            exercisesQuery = exercisesQuery.Where(exercise =>
                EF.Functions.ILike(exercise.Name, searchPattern, "\\")
                || exercise.Aliases.Any(alias =>
                    EF.Functions.ILike(alias.Alias, searchPattern, "\\")));
        }

        if (filters.Category is not null)
        {
            exercisesQuery = exercisesQuery.Where(exercise =>
                exercise.Category == filters.Category);
        }

        if (filters.MovementPattern is not null)
        {
            exercisesQuery = exercisesQuery.Where(exercise =>
                exercise.MovementPattern == filters.MovementPattern);
        }

        if (filters.TrackingMode is not null)
        {
            exercisesQuery = exercisesQuery.Where(exercise =>
                exercise.TrackingMode == filters.TrackingMode);
        }

        if (filters.AvailableEquipment is { Length: > 0 })
        {
            exercisesQuery = exercisesQuery.Where(exercise =>
                exercise.RequiredEquipment.All(required =>
                    filters.AvailableEquipment.Contains(required.Equipment)));
        }

        if (filters.PrimaryMuscle is not null)
        {
            exercisesQuery = exercisesQuery.Where(exercise =>
                exercise.Muscles.Any(muscle =>
                    muscle.Role == MuscleRole.Primary
                    && muscle.Muscle == filters.PrimaryMuscle));
        }

        var matches = await exercisesQuery
            .OrderBy(exercise => exercise.Name)
            .ThenBy(exercise => exercise.Id)
            .Skip(filters.Offset)
            .Take(filters.Limit + 1)
            .Include(exercise => exercise.RequiredEquipment)
            .Include(exercise => exercise.Muscles)
            .AsSplitQuery()
            .ToListAsync(cancellationToken);

        var hasMore = matches.Count > filters.Limit;
        var items = matches.Take(filters.Limit).Select(MapSummary).ToArray();

        return TypedResults.Ok(new ExerciseSearchResponse(
            items,
            hasMore ? filters.Offset + filters.Limit : null));
    }

    private static async Task<Results<Ok<ExerciseDetailResponse>, NotFound>> GetExerciseAsync(
        Guid exerciseId,
        FitnessCoachDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var exercise = await dbContext.Set<Exercise>()
            .AsNoTracking()
            .Include(item => item.RequiredEquipment)
            .Include(item => item.Muscles)
            .AsSplitQuery()
            .SingleOrDefaultAsync(item => item.Id == exerciseId, cancellationToken);

        return exercise is null
            ? TypedResults.NotFound()
            : TypedResults.Ok(MapDetail(exercise));
    }

    private static async Task ConfigureSearchContractAsync(
        OpenApiOperation operation,
        OpenApiOperationTransformerContext context,
        CancellationToken cancellationToken)
    {
        var parameterTypes = new Dictionary<string, Type>(StringComparer.Ordinal)
        {
            ["category"] = typeof(ExerciseCategory),
            ["movementPattern"] = typeof(ExerciseMovementPattern),
            ["trackingMode"] = typeof(ExerciseTrackingMode),
            ["availableEquipment"] = typeof(EquipmentType[]),
            ["primaryMuscle"] = typeof(MuscleGroup),
        };

        foreach (var parameter in operation.Parameters ?? [])
        {
            if (parameter is not OpenApiParameter concreteParameter
                || concreteParameter.Name is null)
            {
                continue;
            }

            if (concreteParameter.Name is "limit" or "offset")
            {
                concreteParameter.Schema = new OpenApiSchema
                {
                    Type = JsonSchemaType.Integer,
                    Format = "int32",
                    Minimum = concreteParameter.Name == "limit" ? "1" : "0",
                    Maximum = concreteParameter.Name == "limit"
                        ? MaximumLimit.ToString(CultureInfo.InvariantCulture)
                        : MaximumOffset.ToString(CultureInfo.InvariantCulture),
                };
            }
            else if (parameterTypes.TryGetValue(concreteParameter.Name, out var parameterType))
            {
                concreteParameter.Schema = await context.GetOrCreateSchemaAsync(
                    parameterType,
                    parameterDescription: null,
                    cancellationToken);
            }
        }
    }

    private static Dictionary<string, string[]> ValidateSearch(
        string? query,
        string? category,
        string? movementPattern,
        string? trackingMode,
        string[]? availableEquipment,
        string? primaryMuscle,
        string? limit,
        string? offset,
        out ExerciseSearchFilters filters)
    {
        var errors = new Dictionary<string, string[]>(StringComparer.Ordinal);

        if (query?.Length > MaximumSearchLength)
        {
            errors["query"] = [$"Search text cannot exceed {MaximumSearchLength} characters."];
        }

        var parsedCategory = ParseOptionalEnum<ExerciseCategory>(category, "category", errors);
        var parsedMovementPattern = ParseOptionalEnum<ExerciseMovementPattern>(
            movementPattern,
            "movementPattern",
            errors);
        var parsedTrackingMode = ParseOptionalEnum<ExerciseTrackingMode>(
            trackingMode,
            "trackingMode",
            errors);
        var parsedPrimaryMuscle = ParseOptionalEnum<MuscleGroup>(
            primaryMuscle,
            "primaryMuscle",
            errors);
        var parsedEquipment = ParseEquipment(availableEquipment, errors);
        var parsedLimit = ParseBoundedInteger(
            limit,
            DefaultLimit,
            1,
            MaximumLimit,
            "limit",
            errors);
        var parsedOffset = ParseBoundedInteger(
            offset,
            0,
            0,
            MaximumOffset,
            "offset",
            errors);

        if (availableEquipment is not null
            && (availableEquipment.Length > Enum.GetValues<EquipmentType>().Length
                || availableEquipment.Distinct(StringComparer.Ordinal).Count()
                != availableEquipment.Length))
        {
            errors["availableEquipment"] =
                ["Available equipment must contain unique supported values."];
        }

        filters = new ExerciseSearchFilters(
            parsedCategory,
            parsedMovementPattern,
            parsedTrackingMode,
            parsedEquipment,
            parsedPrimaryMuscle,
            parsedLimit,
            parsedOffset);

        return errors;
    }

    private static T? ParseOptionalEnum<T>(
        string? value,
        string fieldName,
        Dictionary<string, string[]> errors)
        where T : struct, Enum
    {
        if (value is null)
        {
            return null;
        }

        foreach (var candidate in Enum.GetValues<T>())
        {
            var contractValue = JsonNamingPolicy.CamelCase.ConvertName(candidate.ToString());
            if (string.Equals(value, contractValue, StringComparison.Ordinal))
            {
                return candidate;
            }
        }

        errors[fieldName] = ["Choose a supported value."];
        return null;
    }

    private static EquipmentType[] ParseEquipment(
        string[]? values,
        Dictionary<string, string[]> errors)
    {
        if (values is null)
        {
            return [];
        }

        var parsed = new List<EquipmentType>(values.Length);
        foreach (var value in values)
        {
            var equipment = ParseOptionalEnum<EquipmentType>(
                value,
                "availableEquipment",
                errors);
            if (equipment is not null)
            {
                parsed.Add(equipment.Value);
            }
        }

        return parsed.ToArray();
    }

    private static int ParseBoundedInteger(
        string? value,
        int defaultValue,
        int minimum,
        int maximum,
        string fieldName,
        Dictionary<string, string[]> errors)
    {
        if (value is null)
        {
            return defaultValue;
        }

        if (int.TryParse(
                value,
                NumberStyles.None,
                CultureInfo.InvariantCulture,
                out var parsed)
            && parsed >= minimum
            && parsed <= maximum)
        {
            return parsed;
        }

        errors[fieldName] = [$"Value must be between {minimum} and {maximum}."];
        return defaultValue;
    }

    private static string EscapeLikePattern(string value)
    {
        return value
            .Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace("%", "\\%", StringComparison.Ordinal)
            .Replace("_", "\\_", StringComparison.Ordinal);
    }

    private static ExerciseSummaryResponse MapSummary(Exercise exercise)
    {
        return new ExerciseSummaryResponse(
            exercise.Id,
            exercise.Slug,
            exercise.Name,
            exercise.Category,
            exercise.MovementPattern,
            exercise.TrackingMode,
            exercise.RequiredEquipment
                .Select(item => item.Equipment)
                .Order()
                .ToArray(),
            exercise.Muscles
                .Where(item => item.Role == MuscleRole.Primary)
                .Select(item => item.Muscle)
                .Order()
                .ToArray());
    }

    private static ExerciseDetailResponse MapDetail(Exercise exercise)
    {
        var summary = MapSummary(exercise);

        return new ExerciseDetailResponse(
            summary.Id,
            summary.Slug,
            summary.Name,
            summary.Category,
            summary.MovementPattern,
            summary.TrackingMode,
            summary.RequiredEquipment,
            summary.PrimaryMuscles,
            exercise.Muscles
                .Where(item => item.Role == MuscleRole.Secondary)
                .Select(item => item.Muscle)
                .Order()
                .ToArray(),
            exercise.Setup,
            exercise.Execution,
            exercise.Safety);
    }

    private sealed record ExerciseSearchFilters(
        ExerciseCategory? Category,
        ExerciseMovementPattern? MovementPattern,
        ExerciseTrackingMode? TrackingMode,
        EquipmentType[] AvailableEquipment,
        MuscleGroup? PrimaryMuscle,
        int Limit,
        int Offset);
}
