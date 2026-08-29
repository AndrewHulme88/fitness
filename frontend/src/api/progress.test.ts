import { getExercisePerformance, getProgressOverview } from "./progress";

const profileId = "10000000-0000-0000-0000-000000000001";
const exerciseId = "20000000-0000-0000-0000-000000000002";

describe("progress API", () => {
  it("loads the overview and bounds exercise appearances", async () => {
    const fetch = jest
      .fn()
      .mockResolvedValueOnce(jsonResponse(overview))
      .mockResolvedValueOnce(jsonResponse(performance));

    await getProgressOverview(profileId, options(fetch));
    await getExercisePerformance(
      profileId,
      exerciseId,
      { limit: 12 },
      options(fetch),
    );

    expect((fetch.mock.calls[0]?.[0] as Request).url).toBe(
      `https://api.example.test/profiles/${profileId}/progress`,
    );
    expect((fetch.mock.calls[1]?.[0] as Request).url).toBe(
      `https://api.example.test/profiles/${profileId}/progress/exercises/${exerciseId}?limit=12`,
    );
  });

  it("does not expose transport details in errors", async () => {
    const fetch = jest.fn().mockResolvedValue(jsonResponse("private", 500));
    await expect(
      getProgressOverview(profileId, options(fetch)),
    ).rejects.toThrow("Progress could not be loaded.");
  });
});

function options(fetch: typeof globalThis.fetch) {
  return { baseUrl: "https://api.example.test", fetch };
}

function jsonResponse(body: unknown, status = 200) {
  return new Response(JSON.stringify(body), {
    status,
    headers: { "Content-Type": "application/json" },
  });
}

const overview = {
  periodStart: "2026-08-01T00:00:00Z",
  periodEnd: "2026-08-29T00:00:00Z",
  completedWorkoutCount: 2,
  completedSetCount: 12,
  totalWorkoutDurationSeconds: 3_600,
  recordedExercises: [],
};

const performance = {
  exerciseId,
  exerciseName: "Bench press",
  trackingMode: "repetitionsAndLoad",
  appearances: [],
};
