import {
  AuthenticationRequiredError,
  createTrainingProfile,
  type CreateTrainingProfileRequest,
  type TrainingProfile,
} from "./profiles";

const request: CreateTrainingProfileRequest = {
  goals: ["buildStrength"],
  experience: "beginner",
  availableEquipment: ["bodyweight"],
  unitSystem: "metric",
};

const profile: TrainingProfile = {
  id: "6bf68a92-f5f8-40e5-a112-5330d83e31ed",
  ...request,
  createdAt: "2026-08-26T03:00:00Z",
};

describe("createTrainingProfile", () => {
  it("posts the generated request shape and returns the profile", async () => {
    const fetchMock = jest.fn(
      async (_input: RequestInfo | URL, _init?: RequestInit) =>
        new Response(JSON.stringify(profile), {
          status: 201,
          headers: { "Content-Type": "application/json" },
        }),
    );
    const fetchImplementation: typeof globalThis.fetch = (input, init) =>
      fetchMock(input, init);

    const result = await createTrainingProfile(request, {
      baseUrl: "https://api.example.test",
      fetch: fetchImplementation,
    });

    expect(result).toEqual(profile);
    expect(fetchMock).toHaveBeenCalledTimes(1);
    const sentRequest = fetchMock.mock.calls[0]?.[0];
    expect(sentRequest).toBeInstanceOf(Request);

    if (!(sentRequest instanceof Request)) {
      throw new Error("Expected openapi-fetch to send a Request instance.");
    }

    expect(sentRequest.method).toBe("POST");
    expect(sentRequest.url).toBe("https://api.example.test/profiles");
    expect(sentRequest.headers.get("Content-Type")).toBe("application/json");
    await expect(sentRequest.clone().json()).resolves.toEqual(request);
  });

  it("requires an explicitly configured public API URL", async () => {
    const previousApiUrl = process.env.EXPO_PUBLIC_API_URL;
    delete process.env.EXPO_PUBLIC_API_URL;

    try {
      await expect(createTrainingProfile(request)).rejects.toThrow(
        "The API URL is not configured.",
      );
    } finally {
      process.env.EXPO_PUBLIC_API_URL = previousApiUrl;
    }
  });

  it("requires a fresh session when the API rejects authentication", async () => {
    const fetch = jest
      .fn()
      .mockResolvedValue(new Response(null, { status: 401 }));

    await expect(
      createTrainingProfile(request, {
        baseUrl: "https://api.example.test",
        fetch,
      }),
    ).rejects.toBeInstanceOf(AuthenticationRequiredError);
  });
});
