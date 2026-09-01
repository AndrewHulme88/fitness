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
        onCoach={jest.fn()}
        onEdit={jest.fn()}
        onHistory={jest.fn()}
        onProgress={jest.fn()}
        onStart={jest.fn()}
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

  it("shows compact metadata with separate start and edit actions", async () => {
    const onCoach = jest.fn();
    const onEdit = jest.fn();
    const onStart = jest.fn();
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
        onCoach={onCoach}
        onEdit={onEdit}
        onHistory={jest.fn()}
        onProgress={jest.fn()}
        onStart={onStart}
        profileId={profileId}
      />,
    );

    expect(await screen.findByText("Upper strength")).toBeVisible();
    expect(screen.getByText("5 exercises · 16 sets")).toBeVisible();
    fireEvent.press(screen.getByRole("button", { name: "Start" }));
    fireEvent.press(screen.getByRole("button", { name: "Edit" }));
    fireEvent.press(screen.getByRole("button", { name: "Review" }));

    expect(onStart).toHaveBeenCalledWith(
      "20000000-0000-0000-0000-000000000002",
    );
    expect(onEdit).toHaveBeenCalledWith("20000000-0000-0000-0000-000000000002");
    expect(onCoach).toHaveBeenCalledWith(
      "20000000-0000-0000-0000-000000000002",
    );
  });

  it("offers retry without exposing transport details", async () => {
    mockListWorkouts
      .mockRejectedValueOnce(new Error("private server detail"))
      .mockResolvedValueOnce({ items: [], nextOffset: null });

    render(
      <WorkoutList
        onCreate={jest.fn()}
        onCoach={jest.fn()}
        onEdit={jest.fn()}
        onHistory={jest.fn()}
        onProgress={jest.fn()}
        onStart={jest.fn()}
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
