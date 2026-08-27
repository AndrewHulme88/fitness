import {
  getExercise,
  searchExercises,
  type ExerciseDetail,
  type ExerciseSearchResult,
  type ExerciseSummary,
} from "./exercises";

const exerciseId = "24836217-8e14-4d0f-aad1-8bef355ae78b";
const summary: ExerciseSummary = {
  id: exerciseId,
  slug: "barbell-back-squat",
  name: "Barbell Back Squat",
  category: "strength",
  movementPattern: "squat",
  trackingMode: "repetitionsAndLoad",
  requiredEquipment: ["barbell", "squatRack"],
  primaryMuscles: ["quadriceps", "glutes"],
};
const searchResult: ExerciseSearchResult = {
  items: [summary],
  nextOffset: null,
};
const detail: ExerciseDetail = {
  ...summary,
  secondaryMuscles: ["hamstrings", "back", "core"],
  setup: "Synthetic setup.",
  execution: "Synthetic execution.",
  safety: "Synthetic safety cue.",
};

describe("exercise catalogue API", () => {
  it("serializes typed search filters and returns generated-contract data", async () => {
    const fetchMock = createJsonFetch(searchResult);

    const result = await searchExercises(
      {
        query: "squat",
        category: "strength",
        availableEquipment: ["barbell", "squatRack"],
        limit: 20,
      },
      { baseUrl: "https://api.example.test", fetch: fetchMock.fetch },
    );

    expect(result).toEqual(searchResult);
    const request = fetchMock.request();
    const url = new URL(request.url);
    expect(request.method).toBe("GET");
    expect(url.pathname).toBe("/exercises");
    expect(url.searchParams.get("query")).toBe("squat");
    expect(url.searchParams.get("category")).toBe("strength");
    expect(url.searchParams.getAll("availableEquipment")).toEqual([
      "barbell",
      "squatRack",
    ]);
    expect(url.searchParams.get("limit")).toBe("20");
  });

  it("loads one exercise by its stable identifier", async () => {
    const fetchMock = createJsonFetch(detail);

    const result = await getExercise(exerciseId, {
      baseUrl: "https://api.example.test",
      fetch: fetchMock.fetch,
    });

    expect(result).toEqual(detail);
    expect(fetchMock.request().url).toBe(
      `https://api.example.test/exercises/${exerciseId}`,
    );
  });
});

function createJsonFetch(responseBody: ExerciseDetail | ExerciseSearchResult) {
  const mock = jest.fn(
    async (_input: RequestInfo | URL, _init?: RequestInit) =>
      new Response(JSON.stringify(responseBody), {
        status: 200,
        headers: { "Content-Type": "application/json" },
      }),
  );
  const fetch: typeof globalThis.fetch = (input, init) => mock(input, init);

  return {
    fetch,
    request() {
      const request = mock.mock.calls[0]?.[0];
      if (!(request instanceof Request)) {
        throw new Error("Expected openapi-fetch to send a Request instance.");
      }

      return request;
    },
  };
}
