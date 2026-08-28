import {
  ActiveWorkoutExistsError,
  getActiveWorkoutSession,
  startWorkoutSession,
  updateWorkoutSession,
  WorkoutSessionConflictError,
} from "./sessions";

const session = {
  id: "10000000-0000-0000-0000-000000000001",
  profileId: "10000000-0000-0000-0000-000000000002",
  workoutPlanId: "10000000-0000-0000-0000-000000000003",
  workoutPlanRevision: 1,
  workoutName: "Strength",
  revision: 1,
  status: "active" as const,
  startedAt: "2026-08-28T00:00:00Z",
  updatedAt: "2026-08-28T00:00:00Z",
  finishedAt: null,
  notes: null,
  exercises: [],
};

describe("workout session API", () => {
  it("starts from the generated request contract", async () => {
    const fetch = jest.fn().mockResolvedValue(
      new Response(JSON.stringify(session), {
        status: 201,
        headers: { "Content-Type": "application/json" },
      }),
    );

    await expect(
      startWorkoutSession(
        session.profileId,
        { sessionId: session.id, workoutPlanId: session.workoutPlanId },
        { baseUrl: "https://api.example.test", fetch },
      ),
    ).resolves.toEqual(session);
    const request = fetch.mock.calls[0]?.[0];
    expect(request).toBeInstanceOf(Request);
    expect((request as Request).url).toContain(
      `/profiles/${session.profileId}/workout-sessions`,
    );
    expect((request as Request).method).toBe("POST");
  });

  it("represents a missing active session as null", async () => {
    const fetch = jest
      .fn()
      .mockResolvedValue(new Response(null, { status: 404 }));
    await expect(
      getActiveWorkoutSession(session.profileId, {
        baseUrl: "https://api.example.test",
        fetch,
      }),
    ).resolves.toBeNull();
  });

  it("distinguishes start and revision conflicts", async () => {
    const fetch = jest.fn().mockImplementation(
      async () =>
        new Response(JSON.stringify({ title: "Conflict" }), {
          status: 409,
          headers: { "Content-Type": "application/problem+json" },
        }),
    );

    await expect(
      startWorkoutSession(
        session.profileId,
        { sessionId: session.id, workoutPlanId: session.workoutPlanId },
        { baseUrl: "https://api.example.test", fetch },
      ),
    ).rejects.toBeInstanceOf(ActiveWorkoutExistsError);
    await expect(
      updateWorkoutSession(
        session.profileId,
        session.id,
        {
          expectedRevision: 1,
          clientMutationId: "10000000-0000-0000-0000-000000000004",
          status: "active",
          finishedAt: null,
          notes: null,
          exercises: [],
        },
        { baseUrl: "https://api.example.test", fetch },
      ),
    ).rejects.toBeInstanceOf(WorkoutSessionConflictError);
  });
});
