import type { ExerciseSearchFilters } from "../../api/exercises";

export type ExerciseBodyPart =
  "all" | "arms" | "back" | "cardio" | "chest" | "core" | "legs" | "shoulders";

type ExerciseBodyPartOption = {
  label: string;
  value: ExerciseBodyPart;
};

const armMuscles = ["biceps", "triceps", "forearms"] as const;
const legMuscles = ["quadriceps", "hamstrings", "glutes", "calves"] as const;

export const exerciseBodyPartOptions = [
  { label: "All", value: "all" },
  { label: "Arms", value: "arms" },
  { label: "Back", value: "back" },
  { label: "Cardio", value: "cardio" },
  { label: "Chest", value: "chest" },
  { label: "Core", value: "core" },
  { label: "Legs", value: "legs" },
  { label: "Shoulders", value: "shoulders" },
] as const satisfies readonly ExerciseBodyPartOption[];

export function getBodyPartSearchFilters(
  bodyPart: ExerciseBodyPart,
): ExerciseSearchFilters[] {
  switch (bodyPart) {
    case "arms":
      return armMuscles.map((primaryMuscle) => ({
        primaryMuscle,
      }));
    case "back":
    case "chest":
    case "core":
    case "shoulders":
      return [{ primaryMuscle: bodyPart }];
    case "cardio":
      return [{ category: "cardio" }];
    case "legs":
      return legMuscles.map((primaryMuscle) => ({ primaryMuscle }));
    case "all":
      return [{}];
  }
}
