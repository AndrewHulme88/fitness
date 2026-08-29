import {
  ActiveWorkoutExistsError,
  correctWorkoutSession,
  getActiveWorkoutSession,
  getWorkoutSession,
  listWorkoutHistory,
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
  correctedAt: null,
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
    await expect(
      correctWorkoutSession(
        session.profileId,
        session.id,
        { expectedRevision: 2, notes: null, exercises: [] },
        { baseUrl: "https://api.example.test", fetch },
      ),
    ).rejects.toBeInstanceOf(WorkoutSessionConflictError);
  });

  it("loads bounded history and a completed session detail", async () => {
    const fetch = jest
      .fn()
      .mockResolvedValueOnce(jsonResponse({ items: [], nextOffset: null }))
      .mockResolvedValueOnce(jsonResponse(session));

    await listWorkoutHistory(
      session.profileId,
      { limit: 20, offset: 0 },
      { baseUrl: "https://api.example.test", fetch },
    );
    await getWorkoutSession(session.profileId, session.id, {
      baseUrl: "https://api.example.test",
      fetch,
    });

    const historyRequest = fetch.mock.calls[0]?.[0] as Request;
    const detailRequest = fetch.mock.calls[1]?.[0] as Request;
    expect(historyRequest.url).toContain(
      `/profiles/${session.profileId}/workout-sessions/history?limit=20&offset=0`,
    );
    expect(detailRequest.url).toContain(
      `/profiles/${session.profileId}/workout-sessions/${session.id}`,
    );
  });
});

function jsonResponse(body: unknown, status = 200) {
  return new Response(JSON.stringify(body), {
    status,
    headers: { "Content-Type": "application/json" },
  });
}
