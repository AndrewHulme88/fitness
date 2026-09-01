import {
  act,
  fireEvent,
  render,
  screen,
  waitFor,
} from "@testing-library/react-native";

import { getCoachConversation, sendCoachMessage } from "../../api/coach";
import { CoachConversation } from "./CoachConversation";

jest.mock("../../api/coach", () => ({
  deleteCoachConversation: jest.fn(),
  getCoachConversation: jest.fn(),
  sendCoachMessage: jest.fn(),
}));

const mockGetCoachConversation = getCoachConversation as jest.MockedFunction<
  typeof getCoachConversation
>;
const mockSendCoachMessage = sendCoachMessage as jest.MockedFunction<
  typeof sendCoachMessage
>;

const profileId = "10000000-0000-0000-0000-000000000001";

describe("CoachConversation", () => {
  beforeEach(() => {
    mockGetCoachConversation.mockReset().mockResolvedValue({
      id: "20000000-0000-0000-0000-000000000002",
      messages: [],
      proposals: [],
    });
    mockSendCoachMessage.mockReset();
  });

  it("does not allow deleting a saved conversation while a reply is pending", async () => {
    let resolveSend:
      | ((value: { id: string; messages: []; proposals: [] }) => void)
      | undefined;
    mockSendCoachMessage.mockImplementation(
      () =>
        new Promise((resolve) => {
          resolveSend = resolve;
        }),
    );

    render(<CoachConversation profileId={profileId} />);

    await screen.findByText("Delete saved conversation");
    fireEvent.changeText(
      screen.getByLabelText("Question for the AI coach"),
      "What is a warm-up?",
    );
    fireEvent.press(screen.getByRole("button", { name: "Ask coach" }));

    await waitFor(() =>
      expect(
        screen.getByRole("button", { name: "Delete saved conversation" }),
      ).toBeDisabled(),
    );

    await act(async () =>
      resolveSend?.({
        id: "20000000-0000-0000-0000-000000000002",
        messages: [],
        proposals: [],
      }),
    );
  });

  it("renders a conversation returned by the previous API contract", async () => {
    mockGetCoachConversation.mockResolvedValue({
      id: "20000000-0000-0000-0000-000000000002",
      messages: [],
    } as never);

    render(<CoachConversation profileId={profileId} />);

    expect(await screen.findByText("Delete saved conversation")).toBeTruthy();
  });
});
