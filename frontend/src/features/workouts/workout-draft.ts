import type { ExerciseSummary } from "../../api/exercises";
import type { TrainingProfile } from "../../api/profiles";
import type {
  CreateWorkoutRequest,
  WorkoutDetail,
  WorkoutExerciseRequest,
} from "../../api/workouts";

export type UnitSystem = TrainingProfile["unitSystem"];

export type PlannerExercise = Pick<
  ExerciseSummary,
  "id" | "name" | "trackingMode" | "primaryMuscles"
>;

export type WorkoutExerciseDraft = {
  exercise: PlannerExercise;
  plannedSets: string;
  minimumRepetitions: string;
  maximumRepetitions: string;
  targetLoad: string;
  targetDuration: string;
  targetDistance: string;
};

export type WorkoutDraftErrors = {
  name?: string;
  exercises?: string;
  byExercise: Record<string, string>;
};

type WorkoutDraftResult =
  | { request: CreateWorkoutRequest; errors: null }
  | { request: null; errors: WorkoutDraftErrors };

const poundsToKilograms = 0.45359237;
const milesToMetres = 1_609.344;
const feetToMetres = 0.3048;
const maximumLoadKilograms = 2_000;
const maximumDurationSeconds = 86_400;
const maximumDistanceMetres = 1_000_000;

export function createExerciseDraft(
  exercise: PlannerExercise,
): WorkoutExerciseDraft {
  return {
    exercise,
    plannedSets: "1",
    minimumRepetitions: "",
    maximumRepetitions: "",
    targetLoad: "",
    targetDuration: "",
    targetDistance: "",
  };
}

export function createDraftFromWorkoutExercise(
  exercise: WorkoutDetail["exercises"][number],
  unitSystem: UnitSystem,
): WorkoutExerciseDraft {
  const trackingMode = exercise.trackingMode;
  const loadKilograms = nullableNumber(exercise.targetLoadKilograms);
  const durationSeconds = nullableNumber(exercise.targetDurationSeconds);
  const distanceMetres = nullableNumber(exercise.targetDistanceMetres);

  return {
    exercise: {
      id: exercise.exerciseId,
      name: exercise.exerciseName,
      trackingMode,
      primaryMuscles: exercise.primaryMuscles,
    },
    plannedSets: String(exercise.plannedSets),
    minimumRepetitions: nullableString(exercise.minimumRepetitions),
    maximumRepetitions: nullableString(exercise.maximumRepetitions),
    targetLoad: loadKilograms
      ? formatNumber(
          unitSystem === "metric"
            ? loadKilograms
            : loadKilograms / poundsToKilograms,
        )
      : "",
    targetDuration: durationSeconds
      ? formatNumber(
          trackingMode === "duration" ? durationSeconds : durationSeconds / 60,
        )
      : "",
    targetDistance: distanceMetres
      ? formatNumber(
          trackingMode === "distanceAndDuration"
            ? unitSystem === "metric"
              ? distanceMetres / 1_000
              : distanceMetres / milesToMetres
            : unitSystem === "metric"
              ? distanceMetres
              : distanceMetres / feetToMetres,
        )
      : "",
  };
}

export function buildWorkoutRequest(
  name: string,
  drafts: readonly WorkoutExerciseDraft[],
  unitSystem: UnitSystem,
): WorkoutDraftResult {
  const errors: WorkoutDraftErrors = { byExercise: {} };
  const trimmedName = name.trim();

  if (trimmedName.length === 0 || trimmedName.length > 80) {
    errors.name = "Enter a workout name of no more than 80 characters.";
  }

  if (drafts.length === 0 || drafts.length > 20) {
    errors.exercises = "Choose between 1 and 20 exercises.";
  } else if (
    new Set(drafts.map((draft) => draft.exercise.id)).size !== drafts.length
  ) {
    errors.exercises = "Each exercise can appear only once in a workout.";
  }

  const exercises = drafts.map((draft) =>
    buildExerciseRequest(draft, unitSystem, errors.byExercise),
  );

  if (
    errors.name ||
    errors.exercises ||
    Object.keys(errors.byExercise).length > 0
  ) {
    return { request: null, errors };
  }

  return {
    request: {
      name: trimmedName,
      exercises: exercises as WorkoutExerciseRequest[],
    },
    errors: null,
  };
}

export function formatPrescription(
  draft: WorkoutExerciseDraft,
  unitSystem: UnitSystem,
): string {
  const sets = parseInteger(draft.plannedSets);
  const prefix = sets
    ? `${sets} ${sets === 1 ? "set" : "sets"}`
    : "Set targets";
  const trackingMode = draft.exercise.trackingMode;

  if (trackingMode === "repetitions" || trackingMode === "repetitionsAndLoad") {
    const minimum = parseInteger(draft.minimumRepetitions);
    const maximum = parseInteger(draft.maximumRepetitions);
    if (!minimum || !maximum) return prefix;

    const load = parseDecimal(draft.targetLoad);
    const loadText =
      trackingMode === "repetitionsAndLoad" && load
        ? ` · ${formatNumber(load)} ${unitSystem === "metric" ? "kg" : "lb"}`
        : "";
    return `${prefix} · ${minimum}–${maximum} reps${loadText}`;
  }

  const duration = parseDecimal(draft.targetDuration);
  const distance = parseDecimal(draft.targetDistance);
  const targets: string[] = [];

  if (duration) {
    targets.push(
      trackingMode === "duration"
        ? `${formatNumber(duration)} sec`
        : `${formatNumber(duration)} min`,
    );
  }

  if (distance) {
    targets.push(
      trackingMode === "distanceAndDuration"
        ? `${formatNumber(distance)} ${unitSystem === "metric" ? "km" : "mi"}`
        : `${formatNumber(distance)} ${unitSystem === "metric" ? "m" : "ft"}`,
    );
  }

  return targets.length > 0 ? `${prefix} · ${targets.join(" · ")}` : prefix;
}

export function targetLabels(
  trackingMode: ExerciseSummary["trackingMode"],
  unitSystem: UnitSystem,
) {
  return {
    duration:
      trackingMode === "duration" ? "Duration (seconds)" : "Duration (minutes)",
    distance:
      trackingMode === "distanceAndDuration"
        ? `Distance (${unitSystem === "metric" ? "kilometres" : "miles"})`
        : `Distance (${unitSystem === "metric" ? "metres" : "feet"})`,
    load: `Optional load (${unitSystem === "metric" ? "kg" : "lb"})`,
  };
}

function buildExerciseRequest(
  draft: WorkoutExerciseDraft,
  unitSystem: UnitSystem,
  errors: Record<string, string>,
): WorkoutExerciseRequest | null {
  const id = draft.exercise.id;
  const plannedSets = parseInteger(draft.plannedSets);
  if (!plannedSets || plannedSets > 20) {
    errors[id] = "Choose between 1 and 20 planned sets.";
    return null;
  }

  const trackingMode = draft.exercise.trackingMode;
  const minimumRepetitions = parseInteger(draft.minimumRepetitions);
  const maximumRepetitions = parseInteger(draft.maximumRepetitions);
  const load = parseDecimal(draft.targetLoad);
  const duration = parseDecimal(draft.targetDuration);
  const distance = parseDecimal(draft.targetDistance);

  if (trackingMode === "repetitions" || trackingMode === "repetitionsAndLoad") {
    if (
      !minimumRepetitions ||
      !maximumRepetitions ||
      minimumRepetitions > maximumRepetitions ||
      maximumRepetitions > 1_000
    ) {
      errors[id] = "Choose a valid repetition range.";
      return null;
    }

    const loadKilograms = load
      ? unitSystem === "metric"
        ? load
        : load * poundsToKilograms
      : null;
    if (
      draft.targetLoad.length > 0 &&
      (!loadKilograms || loadKilograms > maximumLoadKilograms)
    ) {
      errors[id] = "Enter a valid positive load or leave it blank.";
      return null;
    }

    return {
      exerciseId: id,
      plannedSets,
      minimumRepetitions,
      maximumRepetitions,
      targetLoadKilograms:
        trackingMode === "repetitionsAndLoad" && load
          ? roundCanonical(loadKilograms as number)
          : null,
      targetDurationSeconds: null,
      targetDistanceMetres: null,
    };
  }

  if (draft.targetDuration.length > 0 && (!duration || duration <= 0)) {
    errors[id] = "Enter a valid positive duration or leave it blank.";
    return null;
  }

  if (draft.targetDistance.length > 0 && (!distance || distance <= 0)) {
    errors[id] = "Enter a valid positive distance or leave it blank.";
    return null;
  }

  if (trackingMode === "duration" && !duration) {
    errors[id] = "Enter a duration target.";
    return null;
  }

  if (trackingMode !== "duration" && !duration && !distance) {
    errors[id] = "Enter a distance, duration, or both.";
    return null;
  }

  if (draft.targetLoad.length > 0 && (!load || load <= 0)) {
    errors[id] = "Enter a valid positive load or leave it blank.";
    return null;
  }

  const durationSeconds = duration
    ? trackingMode === "duration"
      ? duration
      : duration * 60
    : null;
  if (
    durationSeconds !== null &&
    (!Number.isInteger(durationSeconds) ||
      durationSeconds > maximumDurationSeconds)
  ) {
    errors[id] = "Enter a duration of no more than 24 hours in whole seconds.";
    return null;
  }

  const distanceMetres = distance
    ? trackingMode === "distanceAndDuration"
      ? unitSystem === "metric"
        ? distance * 1_000
        : distance * milesToMetres
      : unitSystem === "metric"
        ? distance
        : distance * feetToMetres
    : null;
  if (distanceMetres !== null && distanceMetres > maximumDistanceMetres) {
    errors[id] = "Enter a shorter distance target.";
    return null;
  }

  const loadKilograms = load
    ? unitSystem === "metric"
      ? load
      : load * poundsToKilograms
    : null;
  if (loadKilograms !== null && loadKilograms > maximumLoadKilograms) {
    errors[id] = "Enter a valid positive load or leave it blank.";
    return null;
  }

  return {
    exerciseId: id,
    plannedSets,
    minimumRepetitions: null,
    maximumRepetitions: null,
    targetLoadKilograms:
      trackingMode === "distanceDurationAndLoad" && load
        ? roundCanonical(loadKilograms as number)
        : null,
    targetDurationSeconds: durationSeconds,
    targetDistanceMetres: distanceMetres
      ? roundCanonical(distanceMetres)
      : null,
  };
}

function parseInteger(value: string): number | null {
  if (!/^\d+$/.test(value)) return null;
  const parsed = Number(value);
  return Number.isSafeInteger(parsed) && parsed > 0 ? parsed : null;
}

function parseDecimal(value: string): number | null {
  if (!/^\d+(?:\.\d{1,2})?$/.test(value)) return null;
  const parsed = Number(value);
  return Number.isFinite(parsed) && parsed > 0 ? parsed : null;
}

function roundCanonical(value: number): number {
  return Math.round(value * 100) / 100;
}

function formatNumber(value: number): string {
  return Number.isInteger(value)
    ? value.toString()
    : value.toFixed(2).replace(/0+$/, "");
}

function nullableNumber(value: null | number | string): number | null {
  return value === null ? null : Number(value);
}

function nullableString(value: null | number | string): string {
  return value === null ? "" : String(value);
}
