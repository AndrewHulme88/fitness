import { fireEvent, render, screen } from "@testing-library/react-native";

import { listWorkouts } from "../../api/workouts";
import { WorkoutList } from "./WorkoutList";

jest.mock("../../api/workouts", () => ({
  listWorkouts: jest.fn(),
}));

const mockListWorkouts = listWorkouts as jest.MockedFunction<
  typeof listWorkouts
>;
const profileId = "10000000-0000-0000-0000-000000000001";

describe("WorkoutList", () => {
  beforeEach(() => mockListWorkouts.mockReset());

  it("presents a focused empty state and create action", async () => {
    const onCreate = jest.fn();
    mockListWorkouts.mockResolvedValue({ items: [], nextOffset: null });

    render(
      <WorkoutList
        onCreate={onCreate}
        onEdit={jest.fn()}
        profileId={profileId}
      />,
    );

    expect(await screen.findByText("No workouts yet")).toBeVisible();
    fireEvent.press(screen.getByRole("button", { name: "Create workout" }));

    expect(onCreate).toHaveBeenCalledTimes(1);
    expect(mockListWorkouts).toHaveBeenCalledWith(
      profileId,
      { limit: 50, offset: 0 },
      expect.objectContaining({ signal: expect.any(AbortSignal) }),
    );
  });

  it("shows compact plan metadata and opens the selected workout", async () => {
    const onEdit = jest.fn();
    mockListWorkouts.mockResolvedValue({
      items: [
        {
          id: "20000000-0000-0000-0000-000000000002",
          name: "Upper strength",
          exerciseCount: 5,
          plannedSetCount: 16,
          revision: 1,
          updatedAt: "2026-08-27T00:00:00Z",
        },
      ],
      nextOffset: null,
    });

    render(
      <WorkoutList
        onCreate={jest.fn()}
        onEdit={onEdit}
        profileId={profileId}
      />,
    );

    const row = await screen.findByRole("button", { name: /Upper strength/ });
    expect(screen.getByText("5 exercises · 16 sets")).toBeVisible();
    fireEvent.press(row);

    expect(onEdit).toHaveBeenCalledWith("20000000-0000-0000-0000-000000000002");
  });

  it("offers retry without exposing transport details", async () => {
    mockListWorkouts
      .mockRejectedValueOnce(new Error("private server detail"))
      .mockResolvedValueOnce({ items: [], nextOffset: null });

    render(
      <WorkoutList
        onCreate={jest.fn()}
        onEdit={jest.fn()}
        profileId={profileId}
      />,
    );

    expect(
      await screen.findByText("Your workouts could not be loaded."),
    ).toBeVisible();
    expect(screen.queryByText("private server detail")).not.toBeOnTheScreen();
    fireEvent.press(screen.getByRole("button", { name: "Try again" }));

    expect(await screen.findByText("No workouts yet")).toBeVisible();
    expect(mockListWorkouts).toHaveBeenCalledTimes(2);
  });
});
