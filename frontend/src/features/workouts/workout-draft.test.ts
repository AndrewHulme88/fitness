import type { ExerciseSummary } from "../../api/exercises";
import {
  buildWorkoutRequest,
  createExerciseDraft,
  formatPrescription,
} from "./workout-draft";

const repetitionsExercise = exercise("repetitionsAndLoad");
const cardioExercise = exercise("distanceAndDuration");

describe("workout draft", () => {
  it("does not invent a prescription when an exercise is added", () => {
    expect(createExerciseDraft(repetitionsExercise)).toEqual({
      exercise: repetitionsExercise,
      plannedSets: "1",
      minimumRepetitions: "",
      maximumRepetitions: "",
      targetLoad: "",
      targetDuration: "",
      targetDistance: "",
    });
  });

  it("builds a repetition prescription in canonical kilograms", () => {
    const draft = {
      ...createExerciseDraft(repetitionsExercise),
      plannedSets: "3",
      minimumRepetitions: "8",
      maximumRepetitions: "10",
      targetLoad: "100",
    };

    const result = buildWorkoutRequest(" Upper strength ", [draft], "imperial");

    expect(result.errors).toBeNull();
    expect(result.request).toEqual({
      name: "Upper strength",
      exercises: [
        expect.objectContaining({
          plannedSets: 3,
          minimumRepetitions: 8,
          maximumRepetitions: 10,
          targetLoadKilograms: 45.36,
        }),
      ],
    });
  });

  it("converts cardio minutes and display distance to canonical values", () => {
    const draft = {
      ...createExerciseDraft(cardioExercise),
      targetDuration: "30",
      targetDistance: "3.1",
    };

    const result = buildWorkoutRequest("Cardio", [draft], "imperial");

    expect(result.errors).toBeNull();
    expect(result.request?.exercises[0]).toEqual(
      expect.objectContaining({
        targetDurationSeconds: 1_800,
        targetDistanceMetres: 4_988.97,
      }),
    );
  });

  it("rejects missing tracking-specific targets", () => {
    const result = buildWorkoutRequest(
      "Incomplete",
      [createExerciseDraft(repetitionsExercise)],
      "metric",
    );

    expect(result.request).toBeNull();
    expect(result.errors?.byExercise[repetitionsExercise.id]).toBe(
      "Choose a valid repetition range.",
    );
  });

  it("formats compact plan rows in the profile unit system", () => {
    const draft = {
      ...createExerciseDraft(repetitionsExercise),
      plannedSets: "3",
      minimumRepetitions: "8",
      maximumRepetitions: "10",
      targetLoad: "100",
    };

    expect(formatPrescription(draft, "imperial")).toBe(
      "3 sets · 8–10 reps · 100 lb",
    );
  });

  it("rejects duplicate exercises and display values outside API bounds", () => {
    const draft = {
      ...createExerciseDraft(repetitionsExercise),
      minimumRepetitions: "8",
      maximumRepetitions: "10",
      targetLoad: "5000",
    };

    const duplicateResult = buildWorkoutRequest(
      "Duplicate",
      [draft, draft],
      "metric",
    );
    const boundedResult = buildWorkoutRequest("Too heavy", [draft], "metric");

    expect(duplicateResult.errors?.exercises).toBe(
      "Each exercise can appear only once in a workout.",
    );
    expect(boundedResult.errors?.byExercise[repetitionsExercise.id]).toBe(
      "Enter a valid positive load or leave it blank.",
    );
  });
});

function exercise(
  trackingMode: ExerciseSummary["trackingMode"],
): ExerciseSummary {
  return {
    id: `00000000-0000-0000-0000-${trackingMode.padEnd(12, "0").slice(0, 12)}`,
    slug: "synthetic-exercise",
    name: "Synthetic Exercise",
    category: trackingMode === "distanceAndDuration" ? "cardio" : "strength",
    movementPattern:
      trackingMode === "distanceAndDuration" ? "locomotion" : "horizontalPush",
    trackingMode,
    requiredEquipment: ["bodyweight"],
    primaryMuscles: ["chest"],
  };
}
