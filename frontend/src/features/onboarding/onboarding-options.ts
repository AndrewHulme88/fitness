import type { components } from "../../api/generated/schema";
import type { CreateTrainingProfileRequest } from "../../api/profiles";

export type TrainingGoal = components["schemas"]["TrainingGoal"];
export type TrainingExperience = components["schemas"]["TrainingExperience"];
export type EquipmentType = components["schemas"]["EquipmentType"];
export type UnitSystem = components["schemas"]["UnitSystem"];
export type OnboardingSubmission = CreateTrainingProfileRequest;

type Option<T extends string> = {
  description?: string;
  label: string;
  value: T;
};

export const goalOptions = [
  { label: "Build strength", value: "buildStrength" },
  { label: "Build muscle", value: "buildMuscle" },
  { label: "Improve general fitness", value: "generalFitness" },
] as const satisfies readonly Option<TrainingGoal>[];

export const experienceOptions = [
  {
    description: "New to structured training or returning after a long break.",
    label: "Beginner",
    value: "beginner",
  },
  {
    description: "Comfortable with common exercises and regular training.",
    label: "Intermediate",
    value: "intermediate",
  },
  {
    description: "Experienced with structured programs and load progression.",
    label: "Advanced",
    value: "advanced",
  },
] as const satisfies readonly Option<TrainingExperience>[];

export const equipmentOptions = [
  { label: "Bodyweight", value: "bodyweight" },
  { label: "Dumbbells", value: "dumbbells" },
  { label: "Barbell", value: "barbell" },
  { label: "Bench", value: "bench" },
  { label: "Squat rack", value: "squatRack" },
  { label: "Cable machine", value: "cableMachine" },
  { label: "Resistance bands", value: "resistanceBands" },
  { label: "Cardio equipment", value: "cardioEquipment" },
] as const satisfies readonly Option<EquipmentType>[];

export const unitOptions = [
  {
    description: "Kilograms and centimetres",
    label: "Metric",
    value: "metric",
  },
  {
    description: "Pounds and inches",
    label: "Imperial",
    value: "imperial",
  },
] as const satisfies readonly Option<UnitSystem>[];
