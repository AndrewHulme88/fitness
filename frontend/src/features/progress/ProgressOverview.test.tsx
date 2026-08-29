import { fireEvent, render, screen } from "@testing-library/react-native";

import { getProgressOverview } from "../../api/progress";
import { ProgressOverview } from "./ProgressOverview";

jest.mock("../../api/progress", () => ({ getProgressOverview: jest.fn() }));

const mockOverview = getProgressOverview as jest.MockedFunction<
  typeof getProgressOverview
>;

describe("ProgressOverview", () => {
  beforeEach(() => mockOverview.mockReset());

  it("shows explainable totals without an invented score", async () => {
    const onSelectExercise = jest.fn();
    mockOverview.mockResolvedValue({
      periodStart: "2026-08-01T00:00:00Z",
      periodEnd: "2026-08-29T00:00:00Z",
      completedWorkoutCount: 4,
      completedSetCount: 32,
      totalWorkoutDurationSeconds: 9_000,
      recordedExercises: [
        {
          exerciseId: "exercise-1",
          exerciseName: "Barbell bench press",
          trackingMode: "repetitionsAndLoad",
          appearanceCount: 4,
          lastPerformedAt: "2026-08-28T00:00:00Z",
        },
      ],
    });
    render(
      <ProgressOverview
        onHistory={jest.fn()}
        onPlans={jest.fn()}
        onSelectExercise={onSelectExercise}
        profileId="profile-1"
      />,
    );

    expect(await screen.findByText("2h 30m")).toBeVisible();
    expect(screen.getByText("32")).toBeVisible();
    expect(screen.getByText(/They are not a score/)).toBeVisible();
    fireEvent.press(
      screen.getByRole("button", { name: /Barbell bench press/ }),
    );
    expect(onSelectExercise).toHaveBeenCalledWith("exercise-1");
  });

  it("handles a profile with no completed sets", async () => {
    mockOverview.mockResolvedValue({
      periodStart: "2026-08-01T00:00:00Z",
      periodEnd: "2026-08-29T00:00:00Z",
      completedWorkoutCount: 0,
      completedSetCount: 0,
      totalWorkoutDurationSeconds: 0,
      recordedExercises: [],
    });
    render(
      <ProgressOverview
        onHistory={jest.fn()}
        onPlans={jest.fn()}
        onSelectExercise={jest.fn()}
        profileId="profile-1"
      />,
    );
    expect(
      await screen.findByText("No recorded performance yet"),
    ).toBeVisible();
  });
});
