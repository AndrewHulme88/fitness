import type {
  ActualSetValues,
  SessionExercise,
  SessionSet,
} from "./session-model";

export type UnitSystem = "metric" | "imperial";

const poundsToKilograms = 0.45359237;
const milesToMetres = 1_609.344;
const feetToMetres = 0.3048;

export type SetField = {
  key: keyof ActualSetValues;
  label: string;
  integer?: boolean;
};

export function fieldsFor(
  exercise: SessionExercise,
  unitSystem: UnitSystem,
): SetField[] {
  const fields: SetField[] = [];
  if (
    exercise.trackingMode === "repetitions" ||
    exercise.trackingMode === "repetitionsAndLoad"
  ) {
    fields.push({
      key: "actualRepetitions",
      label: "Repetitions",
      integer: true,
    });
  }
  if (
    exercise.trackingMode === "repetitionsAndLoad" ||
    exercise.trackingMode === "distanceDurationAndLoad"
  ) {
    fields.push({
      key: "actualLoadKilograms",
      label: `Load (${unitSystem === "metric" ? "kg" : "lb"})`,
    });
  }
  if (
    exercise.trackingMode === "duration" ||
    exercise.trackingMode === "distanceAndDuration" ||
    exercise.trackingMode === "distanceDurationAndLoad"
  ) {
    fields.push({
      key: "actualDurationSeconds",
      label: exercise.trackingMode === "duration" ? "Seconds" : "Minutes",
    });
  }
  if (
    exercise.trackingMode === "distanceAndDuration" ||
    exercise.trackingMode === "distanceDurationAndLoad"
  ) {
    fields.push({
      key: "actualDistanceMetres",
      label:
        exercise.trackingMode === "distanceAndDuration"
          ? `Distance (${unitSystem === "metric" ? "km" : "mi"})`
          : `Distance (${unitSystem === "metric" ? "m" : "ft"})`,
    });
  }
  return fields;
}

export function toDisplayValue(
  exercise: SessionExercise,
  key: keyof ActualSetValues,
  value: number | null,
  unitSystem: UnitSystem,
) {
  if (value === null) return "";
  if (key === "actualLoadKilograms" && unitSystem === "imperial") {
    return formatNumber(value / poundsToKilograms);
  }
  if (key === "actualDurationSeconds" && exercise.trackingMode !== "duration") {
    return formatNumber(value / 60);
  }
  if (key === "actualDistanceMetres") {
    if (exercise.trackingMode === "distanceAndDuration") {
      return formatNumber(
        value / (unitSystem === "metric" ? 1_000 : milesToMetres),
      );
    }
    if (unitSystem === "imperial") return formatNumber(value / feetToMetres);
  }
  return formatNumber(value);
}

export function fromDisplayValue(
  exercise: SessionExercise,
  key: keyof ActualSetValues,
  input: string,
  unitSystem: UnitSystem,
): number | null {
  const value = Number(input.trim());
  if (!Number.isFinite(value) || value <= 0) return null;
  if (key === "actualRepetitions")
    return Number.isInteger(value) ? value : null;
  if (key === "actualLoadKilograms") {
    return round(value * (unitSystem === "metric" ? 1 : poundsToKilograms));
  }
  if (key === "actualDurationSeconds") {
    const seconds = exercise.trackingMode === "duration" ? value : value * 60;
    return Number.isInteger(seconds) ? seconds : null;
  }
  if (key === "actualDistanceMetres") {
    if (exercise.trackingMode === "distanceAndDuration") {
      return round(value * (unitSystem === "metric" ? 1_000 : milesToMetres));
    }
    return round(value * (unitSystem === "metric" ? 1 : feetToMetres));
  }
  return value;
}

export function formatSetActual(
  exercise: SessionExercise,
  set: SessionSet,
  unitSystem: UnitSystem,
) {
  if (!set.isCompleted) return "Tap to log";
  return fieldsFor(exercise, unitSystem)
    .map((field) => {
      const value = toDisplayValue(
        exercise,
        field.key,
        set[field.key],
        unitSystem,
      );
      return `${value} ${shortUnit(field.label)}`.trim();
    })
    .join(" · ");
}

export function formatPlan(exercise: SessionExercise, unitSystem: UnitSystem) {
  const parts: string[] = [];
  if (exercise.minimumRepetitions && exercise.maximumRepetitions) {
    parts.push(
      `${exercise.minimumRepetitions}–${exercise.maximumRepetitions} reps`,
    );
  }
  if (exercise.targetLoadKilograms) {
    parts.push(
      `${toDisplayValue(exercise, "actualLoadKilograms", exercise.targetLoadKilograms, unitSystem)} ${unitSystem === "metric" ? "kg" : "lb"}`,
    );
  }
  if (exercise.targetDurationSeconds) {
    parts.push(
      `${toDisplayValue(exercise, "actualDurationSeconds", exercise.targetDurationSeconds, unitSystem)} ${exercise.trackingMode === "duration" ? "sec" : "min"}`,
    );
  }
  if (exercise.targetDistanceMetres) {
    const unit =
      exercise.trackingMode === "distanceAndDuration"
        ? unitSystem === "metric"
          ? "km"
          : "mi"
        : unitSystem === "metric"
          ? "m"
          : "ft";
    parts.push(
      `${toDisplayValue(exercise, "actualDistanceMetres", exercise.targetDistanceMetres, unitSystem)} ${unit}`,
    );
  }
  return parts.join(" · ");
}

function shortUnit(label: string) {
  const match = /\(([^)]+)\)/.exec(label);
  if (match) return match[1];
  if (label === "Repetitions") return "reps";
  if (label === "Seconds") return "sec";
  if (label === "Minutes") return "min";
  return "";
}

function formatNumber(value: number) {
  return Number(value.toFixed(2)).toString();
}

function round(value: number) {
  return Number(value.toFixed(2));
}
