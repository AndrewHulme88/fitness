using FitnessCoach.Api.Domain;

namespace FitnessCoach.Api.Features.Profiles;

internal static class ProfileRequestValidator
{
    public static Dictionary<string, string[]> Validate(CreateTrainingProfileRequest request)
    {
        return Validate(
            request.Goals,
            request.Experience,
            request.AvailableEquipment,
            request.UnitSystem);
    }

    public static Dictionary<string, string[]> Validate(UpdateTrainingProfileRequest request)
    {
        return Validate(
            request.Goals,
            request.Experience,
            request.AvailableEquipment,
            request.UnitSystem);
    }

    private static Dictionary<string, string[]> Validate(
        IReadOnlyList<TrainingGoal> goals,
        TrainingExperience experience,
        IReadOnlyList<EquipmentType> availableEquipment,
        UnitSystem unitSystem)
    {
        var errors = new Dictionary<string, string[]>(StringComparer.Ordinal);

        ValidateSelection(
            goals,
            maximumCount: Enum.GetValues<TrainingGoal>().Length,
            fieldName: "goals",
            requiredMessage: "Choose at least one training goal.",
            invalidMessage: "Choose only supported training goals.",
            duplicateMessage: "Each training goal can be selected only once.",
            errors);

        if (!Enum.IsDefined(experience))
        {
            errors["experience"] = ["Choose a supported training experience."];
        }

        ValidateSelection(
            availableEquipment,
            maximumCount: Enum.GetValues<EquipmentType>().Length,
            fieldName: "availableEquipment",
            requiredMessage: "Choose at least one available equipment option.",
            invalidMessage: "Choose only supported equipment options.",
            duplicateMessage: "Each equipment option can be selected only once.",
            errors);

        if (!Enum.IsDefined(unitSystem))
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
