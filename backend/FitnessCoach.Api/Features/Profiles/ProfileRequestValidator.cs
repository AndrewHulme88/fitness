namespace FitnessCoach.Api.Features.Profiles;

internal static class ProfileRequestValidator
{
    public static Dictionary<string, string[]> Validate(CreateTrainingProfileRequest request)
    {
        var errors = new Dictionary<string, string[]>(StringComparer.Ordinal);

        ValidateSelection(
            request.Goals,
            maximumCount: Enum.GetValues<TrainingGoal>().Length,
            fieldName: "goals",
            requiredMessage: "Choose at least one training goal.",
            invalidMessage: "Choose only supported training goals.",
            duplicateMessage: "Each training goal can be selected only once.",
            errors);

        if (!Enum.IsDefined(request.Experience))
        {
            errors["experience"] = ["Choose a supported training experience."];
        }

        ValidateSelection(
            request.AvailableEquipment,
            maximumCount: Enum.GetValues<EquipmentType>().Length,
            fieldName: "availableEquipment",
            requiredMessage: "Choose at least one available equipment option.",
            invalidMessage: "Choose only supported equipment options.",
            duplicateMessage: "Each equipment option can be selected only once.",
            errors);

        if (!Enum.IsDefined(request.UnitSystem))
        {
            errors["unitSystem"] = ["Choose a supported unit system."];
        }

        return errors;
    }

    private static void ValidateSelection<T>(
        IReadOnlyList<T>? selection,
        int maximumCount,
        string fieldName,
        string requiredMessage,
        string invalidMessage,
        string duplicateMessage,
        Dictionary<string, string[]> errors)
        where T : struct, Enum
    {
        if (selection is not { Count: > 0 })
        {
            errors[fieldName] = [requiredMessage];
            return;
        }

        if (selection.Count > maximumCount || selection.Any(value => !Enum.IsDefined(value)))
        {
            errors[fieldName] = [invalidMessage];
            return;
        }

        if (selection.Distinct().Count() != selection.Count)
        {
            errors[fieldName] = [duplicateMessage];
        }
    }
}
