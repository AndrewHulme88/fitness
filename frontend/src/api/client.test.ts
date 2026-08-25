import { createApiClient } from "./client";

describe("createApiClient", () => {
  it("creates a typed client for a configured API origin", () => {
    const client = createApiClient({ baseUrl: "https://api.example.test/" });

    expect(client.GET).toEqual(expect.any(Function));
  });

  it("rejects a missing API origin", () => {
    expect(() => createApiClient({ baseUrl: "  " })).toThrow(
      "API base URL is required.",
    );
  });
});
