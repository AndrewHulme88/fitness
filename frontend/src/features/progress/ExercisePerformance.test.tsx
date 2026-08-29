import { render, screen } from "@testing-library/react-native";

import { getExercisePerformance } from "../../api/progress";
import { getTrainingProfile } from "../../api/profiles";
import { ExercisePerformance } from "./ExercisePerformance";

jest.mock("../../api/progress", () => ({ getExercisePerformance: jest.fn() }));
jest.mock("../../api/profiles", () => ({ getTrainingProfile: jest.fn() }));

const mockPerformance = getExercisePerformance as jest.MockedFunction<
  typeof getExercisePerformance
>;
const mockProfile = getTrainingProfile as jest.MockedFunction<
  typeof getTrainingProfile
>;

it("labels a single record as insufficient for a trend", async () => {
  mockProfile.mockResolvedValue({
    id: "profile-1",
    goals: ["buildStrength"],
    experience: "intermediate",
    availableEquipment: ["barbell"],
    unitSystem: "metric",
    createdAt: "2026-08-01T00:00:00Z",
  });
  mockPerformance.mockResolvedValue({
    exerciseId: "exercise-1",
    exerciseName: "Barbell bench press",
    trackingMode: "repetitionsAndLoad",
    appearances: [
      {
        sessionId: "session-1",
        workoutName: "Upper strength",
        performedAt: "2026-08-28T00:00:00Z",
        sets: [
          {
            position: 0,
            actualRepetitions: 8,
            actualLoadKilograms: 50,
            actualDurationSeconds: null,
            actualDistanceMetres: null,
          },
        ],
      },
    ],
  });

  render(<ExercisePerformance exerciseId="exercise-1" profileId="profile-1" />);

  expect(await screen.findByText("8 reps · 50 kg")).toBeVisible();
  expect(screen.getByText("One recorded appearance")).toBeVisible();
  expect(
    screen.getByText(
      "More completed workouts will add recorded comparisons here.",
    ),
  ).toBeVisible();
});
