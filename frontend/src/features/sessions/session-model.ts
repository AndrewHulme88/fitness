import type { components } from "../../api/generated/schema";
import type {
  UpdateWorkoutSessionRequest,
  WorkoutSession,
} from "../../api/sessions";

export type TrackingMode = components["schemas"]["ExerciseTrackingMode"];
export type SyncState = "synced" | "pending" | "syncing" | "conflict";

export type SessionSet = {
  setId: string;
  position: number;
  isCompleted: boolean;
  completedAt: string | null;
  actualRepetitions: number | null;
  actualLoadKilograms: number | null;
  actualDurationSeconds: number | null;
  actualDistanceMetres: number | null;
};

export type SessionExercise = {
  exerciseId: string;
  position: number;
  exerciseName: string;
  trackingMode: TrackingMode;
  primaryMuscles: components["schemas"]["MuscleGroup"][];
  plannedSets: number;
  minimumRepetitions: number | null;
  maximumRepetitions: number | null;
  targetLoadKilograms: number | null;
  targetDurationSeconds: number | null;
  targetDistanceMetres: number | null;
  isSkipped: boolean;
  notes: string | null;
  sets: SessionSet[];
};

export type LocalWorkoutSession = {
  schemaVersion: 1;
  id: string;
  profileId: string;
  workoutPlanId: string;
  workoutPlanRevision: number;
  workoutName: string;
  revision: number;
  status: "active" | "completed";
  startedAt: string;
  updatedAt: string;
  finishedAt: string | null;
  notes: string | null;
  exercises: SessionExercise[];
  syncState: SyncState;
  mutationId: string | null;
  restTimerEndsAt: string | null;
};

export type ActualSetValues = Pick<
  SessionSet,
  | "actualRepetitions"
  | "actualLoadKilograms"
  | "actualDurationSeconds"
  | "actualDistanceMetres"
>;

export function sessionFromApi(session: WorkoutSession): LocalWorkoutSession {
  return {
    schemaVersion: 1,
    id: session.id,
    profileId: session.profileId,
    workoutPlanId: session.workoutPlanId,
    workoutPlanRevision: toNumber(session.workoutPlanRevision),
    workoutName: session.workoutName,
    revision: toNumber(session.revision),
    status: session.status,
    startedAt: session.startedAt,
    updatedAt: session.updatedAt,
    finishedAt: session.finishedAt,
    notes: session.notes,
    exercises: session.exercises.map((exercise) => ({
      exerciseId: exercise.exerciseId,
      position: toNumber(exercise.position),
      exerciseName: exercise.exerciseName,
      trackingMode: exercise.trackingMode,
      primaryMuscles: exercise.primaryMuscles,
      plannedSets: toNumber(exercise.plannedSets),
      minimumRepetitions: toNullableNumber(exercise.minimumRepetitions),
      maximumRepetitions: toNullableNumber(exercise.maximumRepetitions),
      targetLoadKilograms: toNullableNumber(exercise.targetLoadKilograms),
      targetDurationSeconds: toNullableNumber(exercise.targetDurationSeconds),
      targetDistanceMetres: toNullableNumber(exercise.targetDistanceMetres),
      isSkipped: exercise.isSkipped,
      notes: exercise.notes,
      sets: exercise.sets.map((set) => ({
        setId: set.setId,
        position: toNumber(set.position),
        isCompleted: set.isCompleted,
        completedAt: set.completedAt,
        actualRepetitions: toNullableNumber(set.actualRepetitions),
        actualLoadKilograms: toNullableNumber(set.actualLoadKilograms),
        actualDurationSeconds: toNullableNumber(set.actualDurationSeconds),
        actualDistanceMetres: toNullableNumber(set.actualDistanceMetres),
      })),
    })),
    syncState: "synced",
    mutationId: null,
    restTimerEndsAt: null,
  };
}

export function sessionToUpdateRequest(
  session: LocalWorkoutSession,
): UpdateWorkoutSessionRequest {
  if (!session.mutationId) {
    throw new Error("A pending session mutation needs an identifier.");
  }

  return {
    expectedRevision: session.revision,
    clientMutationId: session.mutationId,
    status: session.status,
    finishedAt: session.finishedAt,
    notes: session.notes,
    exercises: session.exercises.map((exercise) => ({
      exerciseId: exercise.exerciseId,
      isSkipped: exercise.isSkipped,
      notes: exercise.notes,
      sets: exercise.sets.map((set) => ({
        setId: set.setId,
        isCompleted: set.isCompleted,
        completedAt: set.completedAt,
        actualRepetitions: set.actualRepetitions,
        actualLoadKilograms: set.actualLoadKilograms,
        actualDurationSeconds: set.actualDurationSeconds,
        actualDistanceMetres: set.actualDistanceMetres,
      })),
    })),
  };
}

export function editSession(
  session: LocalWorkoutSession,
  mutationId: string,
  change: (draft: LocalWorkoutSession) => LocalWorkoutSession,
): LocalWorkoutSession {
  const changed = change(session);
  return {
    ...changed,
    syncState: "pending",
    mutationId,
    updatedAt: new Date().toISOString(),
  };
}

export function updateSet(
  session: LocalWorkoutSession,
  exerciseId: string,
  setId: string,
  values: ActualSetValues,
  completedAt: string | null,
): LocalWorkoutSession {
  return updateExercise(session, exerciseId, (exercise) => ({
    ...exercise,
    isSkipped: completedAt ? false : exercise.isSkipped,
    sets: exercise.sets.map((set) =>
      set.setId === setId
        ? {
            ...set,
            ...values,
            isCompleted: completedAt !== null,
            completedAt,
          }
        : set,
    ),
  }));
}

export function addSet(
  session: LocalWorkoutSession,
  exerciseId: string,
  setId: string,
): LocalWorkoutSession {
  return updateExercise(session, exerciseId, (exercise) => {
    if (exercise.sets.length >= 20) return exercise;
    const previous = [...exercise.sets]
      .reverse()
      .find((set) => set.isCompleted);
    return {
      ...exercise,
      sets: [
        ...exercise.sets,
        {
          setId,
          position: exercise.sets.length,
          isCompleted: false,
          completedAt: null,
          actualRepetitions: previous?.actualRepetitions ?? null,
          actualLoadKilograms: previous?.actualLoadKilograms ?? null,
          actualDurationSeconds: previous?.actualDurationSeconds ?? null,
          actualDistanceMetres: previous?.actualDistanceMetres ?? null,
        },
      ],
    };
  });
}

export function removeSet(
  session: LocalWorkoutSession,
  exerciseId: string,
  setId: string,
): LocalWorkoutSession {
  return updateExercise(session, exerciseId, (exercise) => {
    if (exercise.sets.length <= 1) return exercise;
    return {
      ...exercise,
      sets: exercise.sets
        .filter((set) => set.setId !== setId)
        .map((set, position) => ({ ...set, position })),
    };
  });
}

export function suggestedValues(
  exercise: SessionExercise,
  set: SessionSet,
): ActualSetValues {
  const priorCompleted = exercise.sets
    .slice(0, set.position)
    .reverse()
    .find((candidate) => candidate.isCompleted);
  return {
    actualRepetitions:
      set.actualRepetitions ??
      priorCompleted?.actualRepetitions ??
      exercise.maximumRepetitions,
    actualLoadKilograms:
      set.actualLoadKilograms ??
      priorCompleted?.actualLoadKilograms ??
      exercise.targetLoadKilograms,
    actualDurationSeconds:
      set.actualDurationSeconds ??
      priorCompleted?.actualDurationSeconds ??
      exercise.targetDurationSeconds,
    actualDistanceMetres:
      set.actualDistanceMetres ??
      priorCompleted?.actualDistanceMetres ??
      exercise.targetDistanceMetres,
  };
}

function updateExercise(
  session: LocalWorkoutSession,
  exerciseId: string,
  change: (exercise: SessionExercise) => SessionExercise,
): LocalWorkoutSession {
  return {
    ...session,
    exercises: session.exercises.map((exercise) =>
      exercise.exerciseId === exerciseId ? change(exercise) : exercise,
    ),
  };
}

function toNumber(value: number | string) {
  return typeof value === "number" ? value : Number(value);
}

function toNullableNumber(value: null | number | string) {
  return value === null ? null : toNumber(value);
}
