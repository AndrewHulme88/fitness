import { getCurrentAccount } from "./accounts";

describe("getCurrentAccount", () => {
  it("loads the signed-in account's profile association", async () => {
    const fetch = jest.fn().mockResolvedValue(
      new Response(
        JSON.stringify({ profileId: "7c3c03b7-022e-4c6d-af2f-67fb167620bc" }),
        {
          headers: { "Content-Type": "application/json" },
          status: 200,
        },
      ),
    );

    await expect(
      getCurrentAccount({ baseUrl: "https://api.example.test", fetch }),
    ).resolves.toEqual({ profileId: "7c3c03b7-022e-4c6d-af2f-67fb167620bc" });

    const request = fetch.mock.calls[0]?.[0] as Request;
    expect(request.method).toBe("GET");
    expect(request.url).toBe("https://api.example.test/account");
  });
});
