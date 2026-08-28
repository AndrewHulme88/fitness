import Storage from "expo-sqlite/kv-store";

import type { LocalWorkoutSession } from "./session-model";

export type KeyValueStorage = Pick<
  typeof Storage,
  "getItemAsync" | "setItemAsync" | "removeItemAsync"
>;

export function sessionStorageKey(profileId: string) {
  return `active-workout-session:v1:${profileId}`;
}

export async function loadStoredSession(
  profileId: string,
  storage: KeyValueStorage = Storage,
): Promise<LocalWorkoutSession | null> {
  const key = sessionStorageKey(profileId);
  const serialized = await storage.getItemAsync(key);
  if (!serialized) return null;

  try {
    const value: unknown = JSON.parse(serialized);
    if (isStoredSession(value) && value.profileId === profileId) return value;
  } catch {
    // Corrupt or obsolete local state is removed rather than trusted.
  }

  await storage.removeItemAsync(key);
  return null;
}

export function saveStoredSession(
  session: LocalWorkoutSession,
  storage: KeyValueStorage = Storage,
) {
  return storage.setItemAsync(
    sessionStorageKey(session.profileId),
    JSON.stringify(session),
  );
}

export function removeStoredSession(
  profileId: string,
  storage: KeyValueStorage = Storage,
) {
  return storage.removeItemAsync(sessionStorageKey(profileId));
}

function isStoredSession(value: unknown): value is LocalWorkoutSession {
  if (!isRecord(value)) return false;
  return (
    value.schemaVersion === 1 &&
    typeof value.id === "string" &&
    typeof value.profileId === "string" &&
    typeof value.workoutPlanId === "string" &&
    typeof value.workoutPlanRevision === "number" &&
    typeof value.workoutName === "string" &&
    typeof value.revision === "number" &&
    (value.status === "active" || value.status === "completed") &&
    (value.syncState === "synced" ||
      value.syncState === "pending" ||
      value.syncState === "syncing" ||
      value.syncState === "conflict") &&
    typeof value.startedAt === "string" &&
    typeof value.updatedAt === "string" &&
    isNullableString(value.finishedAt) &&
    isNullableString(value.notes) &&
    isNullableString(value.mutationId) &&
    isNullableString(value.restTimerEndsAt) &&
    Array.isArray(value.exercises) &&
    value.exercises.every(isStoredExercise)
  );
}

function isStoredExercise(value: unknown) {
  if (!isRecord(value)) return false;
  return (
    typeof value.exerciseId === "string" &&
    typeof value.position === "number" &&
    typeof value.exerciseName === "string" &&
    isTrackingMode(value.trackingMode) &&
    Array.isArray(value.primaryMuscles) &&
    value.primaryMuscles.every((muscle) => typeof muscle === "string") &&
    typeof value.plannedSets === "number" &&
    isNullableNumber(value.minimumRepetitions) &&
    isNullableNumber(value.maximumRepetitions) &&
    isNullableNumber(value.targetLoadKilograms) &&
    isNullableNumber(value.targetDurationSeconds) &&
    isNullableNumber(value.targetDistanceMetres) &&
    typeof value.isSkipped === "boolean" &&
    isNullableString(value.notes) &&
    Array.isArray(value.sets) &&
    value.sets.every(isStoredSet)
  );
}

function isStoredSet(value: unknown) {
  if (!isRecord(value)) return false;
  return (
    typeof value.setId === "string" &&
    typeof value.position === "number" &&
    typeof value.isCompleted === "boolean" &&
    isNullableString(value.completedAt) &&
    isNullableNumber(value.actualRepetitions) &&
    isNullableNumber(value.actualLoadKilograms) &&
    isNullableNumber(value.actualDurationSeconds) &&
    isNullableNumber(value.actualDistanceMetres)
  );
}

function isTrackingMode(value: unknown) {
  return (
    value === "repetitions" ||
    value === "repetitionsAndLoad" ||
    value === "duration" ||
    value === "distanceAndDuration" ||
    value === "distanceDurationAndLoad"
  );
}

function isNullableNumber(value: unknown) {
  return (
    value === null || (typeof value === "number" && Number.isFinite(value))
  );
}

function isNullableString(value: unknown) {
  return value === null || typeof value === "string";
}

function isRecord(value: unknown): value is Record<string, unknown> {
  return typeof value === "object" && value !== null;
}
