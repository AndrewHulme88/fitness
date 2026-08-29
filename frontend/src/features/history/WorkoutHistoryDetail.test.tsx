import {
  fireEvent,
  render,
  screen,
  waitFor,
} from "@testing-library/react-native";

import { getTrainingProfile } from "../../api/profiles";
import { correctWorkoutSession, getWorkoutSession } from "../../api/sessions";
import { WorkoutHistoryDetail } from "./WorkoutHistoryDetail";

jest.mock("../../api/profiles", () => ({ getTrainingProfile: jest.fn() }));
jest.mock("../../api/sessions", () => ({
  correctWorkoutSession: jest.fn(),
  getWorkoutSession: jest.fn(),
  WorkoutSessionConflictError: class extends Error {},
}));

const mockProfile = getTrainingProfile as jest.MockedFunction<
  typeof getTrainingProfile
>;
const mockSession = getWorkoutSession as jest.MockedFunction<
  typeof getWorkoutSession
>;
const mockCorrection = correctWorkoutSession as jest.MockedFunction<
  typeof correctWorkoutSession
>;

describe("WorkoutHistoryDetail", () => {
  beforeEach(() => {
    mockProfile.mockReset();
    mockSession.mockReset();
    mockCorrection.mockReset();
    mockProfile.mockResolvedValue({
      id: "profile-1",
      goals: ["buildStrength"],
      experience: "intermediate",
      availableEquipment: ["barbell"],
      unitSystem: "metric",
      createdAt: "2026-08-01T00:00:00Z",
    });
    mockSession.mockResolvedValue(session);
    mockCorrection.mockResolvedValue({
      ...session,
      revision: 3,
      correctedAt: "2026-08-29T01:00:00Z",
    });
  });

  it("keeps history read-only until explicit correction mode", async () => {
    render(
      <WorkoutHistoryDetail profileId="profile-1" sessionId="session-1" />,
    );

    expect(await screen.findByText("8 reps · 50 kg")).toBeVisible();
    expect(screen.queryByText("Mark skipped")).not.toBeOnTheScreen();
    fireEvent.press(
      screen.getByRole("button", { name: "Correct this record" }),
    );
    fireEvent.press(screen.getByRole("button", { name: "Mark skipped" }));
    fireEvent.press(screen.getByRole("button", { name: "Save correction" }));

    await waitFor(() => expect(mockCorrection).toHaveBeenCalledTimes(1));
    expect(mockCorrection.mock.calls[0]?.[2]).toEqual(
      expect.objectContaining({
        expectedRevision: 2,
        exercises: [expect.objectContaining({ isSkipped: true })],
      }),
    );
    expect(await screen.findByText("Correction saved.")).toBeVisible();
  });
});

const session = {
  id: "session-1",
  profileId: "profile-1",
  workoutPlanId: "workout-1",
  workoutPlanRevision: 1,
  workoutName: "Upper strength",
  revision: 2,
  status: "completed" as const,
  startedAt: "2026-08-28T23:00:00Z",
  updatedAt: "2026-08-29T00:00:00Z",
  finishedAt: "2026-08-28T23:45:00Z",
  correctedAt: null,
  notes: null,
  exercises: [
    {
      exerciseId: "exercise-1",
      position: 0,
      exerciseName: "Barbell bench press",
      trackingMode: "repetitionsAndLoad" as const,
      primaryMuscles: ["chest" as const],
      plannedSets: 1,
      minimumRepetitions: 8,
      maximumRepetitions: 10,
      targetLoadKilograms: 50,
      targetDurationSeconds: null,
      targetDistanceMetres: null,
      isSkipped: false,
      notes: null,
      sets: [
        {
          setId: "set-1",
          position: 0,
          isCompleted: true,
          completedAt: "2026-08-28T23:10:00Z",
          actualRepetitions: 8,
          actualLoadKilograms: 50,
          actualDurationSeconds: null,
          actualDistanceMetres: null,
        },
      ],
    },
  ],
};
