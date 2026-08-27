import {
  createWorkout,
  getWorkout,
  listWorkouts,
  updateWorkout,
  WorkoutRevisionConflictError,
  type WorkoutExerciseRequest,
} from "./workouts";

const profileId = "10000000-0000-0000-0000-000000000001";
const workoutId = "20000000-0000-0000-0000-000000000002";
const exerciseId = "30000000-0000-0000-0000-000000000003";

const exercise: WorkoutExerciseRequest = {
  exerciseId,
  plannedSets: 3,
  minimumRepetitions: 8,
  maximumRepetitions: 10,
  targetLoadKilograms: 50,
  targetDurationSeconds: null,
  targetDistanceMetres: null,
};

describe("workout API", () => {
  it("serializes bounded list pagination", async () => {
    const fetchMock = createFetch({ items: [], nextOffset: null });

    await listWorkouts(
      profileId,
      { limit: 20, offset: 0 },
      requestOptions(fetchMock.fetch),
    );

    const request = fetchMock.request();
    const url = new URL(request.url);
    expect(request.method).toBe("GET");
    expect(url.pathname).toBe(`/profiles/${profileId}/workouts`);
    expect(url.searchParams.get("limit")).toBe("20");
    expect(url.searchParams.get("offset")).toBe("0");
  });

  it("uses the generated request contract for create and update", async () => {
    const fetchMock = createFetchSequence([
      workoutDocument(),
      workoutDocument({ revision: 2 }),
    ]);

    await createWorkout(
      profileId,
      { name: "Upper strength", exercises: [exercise] },
      requestOptions(fetchMock.fetch),
    );
    await updateWorkout(
      profileId,
      workoutId,
      { name: "Upper strength", expectedRevision: 1, exercises: [exercise] },
      requestOptions(fetchMock.fetch),
    );

    expect(fetchMock.request(0).method).toBe("POST");
    expect(fetchMock.request(0).url).toBe(
      `http://api.test/profiles/${profileId}/workouts`,
    );
    await expect(fetchMock.request(0).clone().json()).resolves.toEqual({
      name: "Upper strength",
      exercises: [exercise],
    });
    expect(fetchMock.request(1).method).toBe("PUT");
    expect(fetchMock.request(1).url).toBe(
      `http://api.test/profiles/${profileId}/workouts/${workoutId}`,
    );
  });

  it("loads a workout by its stable identifiers", async () => {
    const fetchMock = createFetch(workoutDocument());

    await getWorkout(profileId, workoutId, requestOptions(fetchMock.fetch));

    expect(fetchMock.request().url).toBe(
      `http://api.test/profiles/${profileId}/workouts/${workoutId}`,
    );
    expect(fetchMock.request().method).toBe("GET");
  });

  it("surfaces revision conflicts distinctly", async () => {
    const fetchMock = createFetch({ title: "Conflict" }, 409);

    await expect(
      updateWorkout(
        profileId,
        workoutId,
        { name: "Upper strength", expectedRevision: 1, exercises: [exercise] },
        requestOptions(fetchMock.fetch),
      ),
    ).rejects.toBeInstanceOf(WorkoutRevisionConflictError);
  });
});

function requestOptions(fetch: typeof globalThis.fetch) {
  return { baseUrl: "http://api.test", fetch };
}

function jsonResponse(body: unknown, status = 200) {
  return new Response(JSON.stringify(body), {
    status,
    headers: { "Content-Type": "application/json" },
  });
}

function createFetch(body: unknown, status = 200) {
  return createFetchSequence([body], [status]);
}

function createFetchSequence(bodies: unknown[], statuses: number[] = []) {
  let responseIndex = 0;
  const mock = jest.fn(
    async (_input: RequestInfo | URL, _init?: RequestInit) => {
      const currentIndex = responseIndex;
      responseIndex += 1;
      return jsonResponse(bodies[currentIndex], statuses[currentIndex] ?? 200);
    },
  );
  const fetch: typeof globalThis.fetch = (input, init) => mock(input, init);

  return {
    fetch,
    request(index = 0) {
      const request = mock.mock.calls[index]?.[0];
      if (!(request instanceof Request)) {
        throw new Error("Expected openapi-fetch to send a Request instance.");
      }
      return request;
    },
  };
}

function workoutDocument(overrides: { revision?: number } = {}) {
  return {
    id: workoutId,
    profileId,
    name: "Upper strength",
    revision: overrides.revision ?? 1,
    exercises: [],
    createdAt: "2026-08-27T00:00:00Z",
    updatedAt: "2026-08-27T00:00:00Z",
  };
}
