import type { WorkoutSession } from "../../api/sessions";
import {
  addSet,
  editSession,
  sessionFromApi,
  sessionToUpdateRequest,
  suggestedValues,
  updateSet,
} from "./session-model";
import { loadStoredSession, saveStoredSession } from "./session-storage";

const remoteSession: WorkoutSession = {
  id: "10000000-0000-0000-0000-000000000001",
  profileId: "10000000-0000-0000-0000-000000000002",
  workoutPlanId: "10000000-0000-0000-0000-000000000003",
  workoutPlanRevision: "1",
  workoutName: "Strength",
  revision: "1",
  status: "active",
  startedAt: "2026-08-28T00:00:00Z",
  updatedAt: "2026-08-28T00:00:00Z",
  finishedAt: null,
  correctedAt: null,
  notes: null,
  exercises: [
    {
      exerciseId: "10000000-0000-0000-0000-000000000004",
      position: 0,
      exerciseName: "Bench press",
      trackingMode: "repetitionsAndLoad",
      primaryMuscles: ["chest"],
      plannedSets: 2,
      minimumRepetitions: 8,
      maximumRepetitions: 10,
      targetLoadKilograms: "60",
      targetDurationSeconds: null,
      targetDistanceMetres: null,
      isSkipped: false,
      notes: null,
      sets: [
        {
          setId: "10000000-0000-0000-0000-000000000005",
          position: 0,
          isCompleted: false,
          completedAt: null,
          actualRepetitions: null,
          actualLoadKilograms: null,
          actualDurationSeconds: null,
          actualDistanceMetres: null,
        },
      ],
    },
  ],
};

describe("session model", () => {
  it("normalizes transport numbers and keeps plans separate from actuals", () => {
    const session = sessionFromApi(remoteSession);
    const exercise = session.exercises[0];
    const set = exercise.sets[0];

    expect(session.revision).toBe(1);
    expect(set.actualLoadKilograms).toBeNull();
    expect(suggestedValues(exercise, set)).toEqual({
      actualRepetitions: 10,
      actualLoadKilograms: 60,
      actualDurationSeconds: null,
      actualDistanceMetres: null,
    });
  });

  it("marks edits pending and inherits completed values for an added set", () => {
    const initial = sessionFromApi(remoteSession);
    const completed = editSession(initial, "mutation-1", (session) =>
      updateSet(
        session,
        initial.exercises[0].exerciseId,
        initial.exercises[0].sets[0].setId,
        {
          actualRepetitions: 9,
          actualLoadKilograms: 62.5,
          actualDurationSeconds: null,
          actualDistanceMetres: null,
        },
        "2026-08-28T00:01:00Z",
      ),
    );
    const added = editSession(completed, "mutation-2", (session) =>
      addSet(session, initial.exercises[0].exerciseId, "new-set"),
    );

    expect(added.syncState).toBe("pending");
    expect(added.exercises[0].sets[1]).toMatchObject({
      setId: "new-set",
      actualRepetitions: 9,
      actualLoadKilograms: 62.5,
    });
    expect(sessionToUpdateRequest(added).clientMutationId).toBe("mutation-2");
  });

  it("round trips valid state and removes corrupt local state", async () => {
    const values = new Map<string, string>();
    const storage = {
      getItemAsync: async (key: string) => values.get(key) ?? null,
      setItemAsync: async (
        key: string,
        value: string | ((prior: string | null) => string),
      ) => {
        values.set(
          key,
          typeof value === "function" ? value(values.get(key) ?? null) : value,
        );
      },
      removeItemAsync: async (key: string) => values.delete(key),
    };
    const session = sessionFromApi(remoteSession);

    await saveStoredSession(session, storage);
    await expect(
      loadStoredSession(session.profileId, storage),
    ).resolves.toEqual(session);
    values.set(
      `active-workout-session:v1:${session.profileId}`,
      JSON.stringify({ ...session, exercises: [null] }),
    );
    await expect(
      loadStoredSession(session.profileId, storage),
    ).resolves.toBeNull();
    values.set(`active-workout-session:v1:${session.profileId}`, "{bad json");
    await expect(
      loadStoredSession(session.profileId, storage),
    ).resolves.toBeNull();
    expect(values.size).toBe(0);
  });
});
