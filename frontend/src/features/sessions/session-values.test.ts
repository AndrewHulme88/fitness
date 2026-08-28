import type { SessionExercise } from "./session-model";
import {
  fieldsFor,
  formatPlan,
  fromDisplayValue,
  toDisplayValue,
} from "./session-values";

const carry: SessionExercise = {
  exerciseId: "exercise-id",
  position: 0,
  exerciseName: "Farmer carry",
  trackingMode: "distanceDurationAndLoad",
  primaryMuscles: ["forearms"],
  plannedSets: 1,
  minimumRepetitions: null,
  maximumRepetitions: null,
  targetLoadKilograms: 45.36,
  targetDurationSeconds: 120,
  targetDistanceMetres: 30.48,
  isSkipped: false,
  notes: null,
  sets: [],
};

describe("session display values", () => {
  it("converts imperial entry back to bounded canonical values", () => {
    expect(
      fromDisplayValue(carry, "actualLoadKilograms", "100", "imperial"),
    ).toBe(45.36);
    expect(
      fromDisplayValue(carry, "actualDistanceMetres", "100", "imperial"),
    ).toBe(30.48);
    expect(
      fromDisplayValue(carry, "actualDurationSeconds", "2", "imperial"),
    ).toBe(120);
    expect(
      toDisplayValue(carry, "actualLoadKilograms", 45.36, "imperial"),
    ).toBe("100");
  });

  it("shows only the values required by the tracking mode", () => {
    expect(fieldsFor(carry, "metric").map((field) => field.key)).toEqual([
      "actualLoadKilograms",
      "actualDurationSeconds",
      "actualDistanceMetres",
    ]);
    expect(formatPlan(carry, "imperial")).toBe("100 lb · 2 min · 100 ft");
  });

  it("rejects non-positive values and fractional repetitions", () => {
    const repetitions = { ...carry, trackingMode: "repetitions" as const };
    expect(
      fromDisplayValue(repetitions, "actualRepetitions", "8.5", "metric"),
    ).toBeNull();
    expect(
      fromDisplayValue(repetitions, "actualRepetitions", "0", "metric"),
    ).toBeNull();
  });
});
