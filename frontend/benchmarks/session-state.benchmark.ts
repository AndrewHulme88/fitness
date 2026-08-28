import {
  editSession,
  type LocalWorkoutSession,
  updateSet,
} from "../src/features/sessions/session-model";

describe("active-session state benchmark", () => {
  it("records max-shape edit and durable-serialization latency", () => {
    const batches = 100;
    const operationsPerBatch = 100;
    const iterations = batches * operationsPerBatch;
    const samples: number[] = [];
    let session = maximumSession();

    for (let batch = 0; batch < batches; batch += 1) {
      const startedAt = performance.now();
      for (let operation = 0; operation < operationsPerBatch; operation += 1) {
        const index = batch * operationsPerBatch + operation;
        session = editSession(session, `mutation-${index}`, (current) =>
          updateSet(
            current,
            current.exercises[19].exerciseId,
            current.exercises[19].sets[19].setId,
            {
              actualRepetitions: 10,
              actualLoadKilograms: 60,
              actualDurationSeconds: null,
              actualDistanceMetres: null,
            },
            "2026-08-28T01:00:00Z",
          ),
        );
        JSON.stringify(session);
      }
      samples.push((performance.now() - startedAt) / operationsPerBatch);
    }

    samples.sort((left, right) => left - right);
    const median = percentile(samples, 0.5);
    const p95 = percentile(samples, 0.95);
    const minimum = samples[0];
    const maximum = samples.at(-1);

    console.info(
      `session edit + JSON serialization (${iterations} iterations, 20 exercises × 20 sets): ` +
        `median ${median.toFixed(3)} ms, p95 ${p95.toFixed(3)} ms, ` +
        `min ${minimum.toFixed(3)} ms, max ${maximum?.toFixed(3)} ms`,
    );

    expect(session.exercises[19].sets[19].actualLoadKilograms).toBe(60);
    expect(samples).toHaveLength(batches);
  });
});

function percentile(values: readonly number[], percentileRank: number) {
  return values[Math.floor((values.length - 1) * percentileRank)];
}

function maximumSession(): LocalWorkoutSession {
  return {
    schemaVersion: 1,
    id: "session-id",
    profileId: "profile-id",
    workoutPlanId: "plan-id",
    workoutPlanRevision: 1,
    workoutName: "Maximum session",
    revision: 1,
    status: "active",
    startedAt: "2026-08-28T00:00:00Z",
    updatedAt: "2026-08-28T00:00:00Z",
    finishedAt: null,
    notes: null,
    syncState: "synced",
    mutationId: null,
    restTimerEndsAt: null,
    exercises: Array.from({ length: 20 }, (_, exerciseIndex) => ({
      exerciseId: `exercise-${exerciseIndex}`,
      position: exerciseIndex,
      exerciseName: `Exercise ${exerciseIndex}`,
      trackingMode: "repetitionsAndLoad" as const,
      primaryMuscles: ["chest" as const],
      plannedSets: 20,
      minimumRepetitions: 8,
      maximumRepetitions: 10,
      targetLoadKilograms: 60,
      targetDurationSeconds: null,
      targetDistanceMetres: null,
      isSkipped: false,
      notes: null,
      sets: Array.from({ length: 20 }, (_, setIndex) => ({
        setId: `set-${exerciseIndex}-${setIndex}`,
        position: setIndex,
        isCompleted: false,
        completedAt: null,
        actualRepetitions: null,
        actualLoadKilograms: null,
        actualDurationSeconds: null,
        actualDistanceMetres: null,
      })),
    })),
  };
}
