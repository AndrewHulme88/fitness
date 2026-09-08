import { act, fireEvent, render, screen } from "@testing-library/react-native";

import { searchExercises } from "../../api/exercises";
import type { TrainingProfile } from "../../api/profiles";
import { ExercisePicker } from "./ExercisePicker";

jest.mock("../../api/exercises", () => ({
  getExercise: jest.fn(),
  searchExercises: jest.fn(),
}));

const mockSearchExercises = searchExercises as jest.MockedFunction<
  typeof searchExercises
>;
const profile: TrainingProfile = {
  id: "10000000-0000-0000-0000-000000000001",
  goals: ["buildStrength"],
  experience: "beginner",
  availableEquipment: ["bodyweight", "cardioEquipment"],
  unitSystem: "metric",
  createdAt: "2026-09-08T00:00:00Z",
};

describe("ExercisePicker", () => {
  beforeEach(() => {
    jest.useFakeTimers();
    mockSearchExercises.mockReset().mockResolvedValue({
      items: [],
      nextOffset: null,
    });
  });

  afterEach(() => {
    jest.useRealTimers();
  });

  it("runs a search for each leg muscle", async () => {
    render(
      <ExercisePicker
        excludedExerciseIds={new Set()}
        onClose={jest.fn()}
        onSelect={jest.fn()}
        profile={profile}
        visible
      />,
    );

    await flushSearch();
    mockSearchExercises.mockClear();

    fireEvent.press(screen.getByRole("radio", { name: "Legs" }));
    await flushSearch();

    expect(mockSearchExercises).toHaveBeenCalledTimes(4);
    expect(mockSearchExercises).toHaveBeenCalledWith(
      expect.objectContaining({ primaryMuscle: "quadriceps" }),
      expect.anything(),
    );
    expect(mockSearchExercises).toHaveBeenCalledWith(
      expect.objectContaining({ primaryMuscle: "hamstrings" }),
      expect.anything(),
    );
    expect(mockSearchExercises).toHaveBeenCalledWith(
      expect.objectContaining({ primaryMuscle: "glutes" }),
      expect.anything(),
    );
    expect(mockSearchExercises).toHaveBeenCalledWith(
      expect.objectContaining({ primaryMuscle: "calves" }),
      expect.anything(),
    );
  });

  it("uses the cardio category filter", async () => {
    render(
      <ExercisePicker
        excludedExerciseIds={new Set()}
        onClose={jest.fn()}
        onSelect={jest.fn()}
        profile={profile}
        visible
      />,
    );

    await flushSearch();
    mockSearchExercises.mockClear();

    fireEvent.press(screen.getByRole("radio", { name: "Cardio" }));
    await flushSearch();

    expect(mockSearchExercises).toHaveBeenCalledWith(
      expect.objectContaining({ category: "cardio" }),
      expect.anything(),
    );
  });
});

async function flushSearch() {
  await act(async () => {
    jest.advanceTimersByTime(250);
    await Promise.resolve();
  });
}
