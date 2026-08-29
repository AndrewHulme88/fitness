import { fireEvent, render, screen } from "@testing-library/react-native";

import { listWorkoutHistory } from "../../api/sessions";
import { WorkoutHistoryList } from "./WorkoutHistoryList";

jest.mock("../../api/sessions", () => ({ listWorkoutHistory: jest.fn() }));

const mockList = listWorkoutHistory as jest.MockedFunction<
  typeof listWorkoutHistory
>;

describe("WorkoutHistoryList", () => {
  beforeEach(() => mockList.mockReset());

  it("shows an honest empty state", async () => {
    mockList.mockResolvedValue({ items: [], nextOffset: null });
    render(<List />);

    expect(await screen.findByText("No completed workouts")).toBeVisible();
    expect(
      screen.getByRole("tab", { name: "History" }).props.accessibilityState,
    ).toEqual({ selected: true });
  });

  it("shows recorded totals and opens a workout", async () => {
    const onSelect = jest.fn();
    mockList.mockResolvedValue({
      items: [historyItem],
      nextOffset: null,
    });
    render(<List onSelect={onSelect} />);

    expect(await screen.findByText("Upper strength")).toBeVisible();
    expect(screen.getByText("3/5 sets · 45 min")).toBeVisible();
    expect(screen.getByText("1 skipped exercise · Corrected")).toBeVisible();
    fireEvent.press(screen.getByRole("button", { name: /Upper strength/ }));
    expect(onSelect).toHaveBeenCalledWith(historyItem.id);
  });

  it("loads an earlier bounded page on request", async () => {
    mockList
      .mockResolvedValueOnce({ items: [historyItem], nextOffset: 20 })
      .mockResolvedValueOnce({
        items: [{ ...historyItem, id: "session-2", workoutName: "Lower" }],
        nextOffset: null,
      });
    render(<List />);

    fireEvent.press(
      await screen.findByRole("button", { name: "Load earlier workouts" }),
    );
    expect(await screen.findByText("Lower")).toBeVisible();
    expect(mockList).toHaveBeenLastCalledWith("profile-1", {
      limit: 20,
      offset: 20,
    });
  });
});

function List({ onSelect = jest.fn() }: { onSelect?: (id: string) => void }) {
  return (
    <WorkoutHistoryList
      onPlans={jest.fn()}
      onProgress={jest.fn()}
      onSelect={onSelect}
      profileId="profile-1"
    />
  );
}

const historyItem = {
  id: "session-1",
  workoutName: "Upper strength",
  startedAt: "2026-08-28T23:00:00Z",
  finishedAt: "2026-08-28T23:45:00Z",
  durationSeconds: 2_700,
  completedSetCount: 3,
  totalSetCount: 5,
  skippedExerciseCount: 1,
  correctedAt: "2026-08-29T00:00:00Z",
};
