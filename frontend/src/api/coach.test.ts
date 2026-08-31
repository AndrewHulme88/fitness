import {
  deleteCoachConversation,
  getCoachConversation,
  sendCoachMessage,
} from "./coach";

jest.mock("../features/auth/cognito", () => ({ loadAccessToken: jest.fn() }));

const profileId = "10000000-0000-0000-0000-000000000001";
const conversation = {
  id: "20000000-0000-0000-0000-000000000002",
  messages: [],
};

describe("coach API", () => {
  it("loads a missing conversation without treating it as an error", async () => {
    const fetch = jest
      .fn()
      .mockResolvedValue(new Response(null, { status: 404 }));

    await expect(
      getCoachConversation(profileId, { baseUrl: "https://api.test", fetch }),
    ).resolves.toBeUndefined();
  });

  it("sends a bounded question and deletes the retained conversation", async () => {
    const fetch = jest
      .fn()
      .mockResolvedValueOnce(Response.json(conversation))
      .mockResolvedValueOnce(new Response(null, { status: 204 }));

    await expect(
      sendCoachMessage(profileId, "What is a warm-up?", {
        baseUrl: "https://api.test",
        fetch,
      }),
    ).resolves.toEqual(conversation);
    await deleteCoachConversation(profileId, {
      baseUrl: "https://api.test",
      fetch,
    });

    const sendRequest = fetch.mock.calls[0]?.[0] as Request;
    const deleteRequest = fetch.mock.calls[1]?.[0] as Request;
    expect(sendRequest.url).toBe(
      `https://api.test/profiles/${profileId}/coach/conversation/messages`,
    );
    await expect(sendRequest.json()).resolves.toEqual({
      question: "What is a warm-up?",
    });
    expect(deleteRequest.method).toBe("DELETE");
  });
});
