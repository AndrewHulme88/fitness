import {
  fireEvent,
  render,
  screen,
  waitFor,
} from "@testing-library/react-native";

import { getTrainingProfile, type TrainingProfile } from "../../api/profiles";
import {
  createWorkout,
  getWorkout,
  updateWorkout,
  WorkoutRevisionConflictError,
  type WorkoutDetail,
} from "../../api/workouts";
import { WorkoutPlanner } from "./WorkoutPlanner";

jest.mock("../../api/profiles", () => ({
  ...jest.requireActual("../../api/profiles"),
  getTrainingProfile: jest.fn(),
}));
jest.mock("../../api/workouts", () => ({
  ...jest.requireActual("../../api/workouts"),
  createWorkout: jest.fn(),
  getWorkout: jest.fn(),
  updateWorkout: jest.fn(),
}));

const mockGetTrainingProfile = getTrainingProfile as jest.MockedFunction<
  typeof getTrainingProfile
>;
const mockCreateWorkout = createWorkout as jest.MockedFunction<
  typeof createWorkout
>;
const mockGetWorkout = getWorkout as jest.MockedFunction<typeof getWorkout>;
const mockUpdateWorkout = updateWorkout as jest.MockedFunction<
  typeof updateWorkout
>;

const profileId = "10000000-0000-0000-0000-000000000001";
const workoutId = "20000000-0000-0000-0000-000000000002";
const exerciseId = "30000000-0000-0000-0000-000000000003";
const profile: TrainingProfile = {
  id: profileId,
  goals: ["buildStrength"],
  experience: "beginner",
  availableEquipment: ["barbell"],
  unitSystem: "metric",
  createdAt: "2026-08-27T00:00:00Z",
};
const workout: WorkoutDetail = {
  id: workoutId,
  profileId,
  name: "Upper strength",
  revision: 4,
  exercises: [
    {
      exerciseId,
      position: 0,
      exerciseName: "Bench Press",
      trackingMode: "repetitionsAndLoad",
      primaryMuscles: ["chest"],
      plannedSets: 3,
      minimumRepetitions: 8,
      maximumRepetitions: 10,
      targetLoadKilograms: 50,
      targetDurationSeconds: null,
      targetDistanceMetres: null,
    },
  ],
  createdAt: "2026-08-27T00:00:00Z",
  updatedAt: "2026-08-27T00:00:00Z",
};

describe("WorkoutPlanner", () => {
  beforeEach(() => {
    mockGetTrainingProfile.mockReset().mockResolvedValue(profile);
    mockGetWorkout.mockReset().mockResolvedValue(workout);
    mockCreateWorkout.mockReset();
    mockUpdateWorkout
      .mockReset()
      .mockResolvedValue({ ...workout, revision: 5 });
  });

  it("loads an existing plan and saves against its revision", async () => {
    const onSaved = jest.fn();
    render(
      <WorkoutPlanner
        onSaved={onSaved}
        profileId={profileId}
        workoutId={workoutId}
      />,
    );

    expect(await screen.findByDisplayValue("Upper strength")).toBeVisible();
    expect(screen.getByText("3 sets · 8–10 reps · 50 kg")).toBeVisible();
    fireEvent.press(screen.getByRole("button", { name: "Save workout" }));

    await waitFor(() =>
      expect(mockUpdateWorkout).toHaveBeenCalledWith(profileId, workoutId, {
        name: "Upper strength",
        expectedRevision: 4,
        exercises: [
          {
            exerciseId,
            plannedSets: 3,
            minimumRepetitions: 8,
            maximumRepetitions: 10,
            targetLoadKilograms: 50,
            targetDurationSeconds: null,
            targetDistanceMetres: null,
          },
        ],
      }),
    );
    expect(onSaved).toHaveBeenCalledTimes(1);
  });

  it("keeps the editor open and explains a concurrent update conflict", async () => {
    mockUpdateWorkout.mockRejectedValue(new WorkoutRevisionConflictError());
    render(
      <WorkoutPlanner
        onSaved={jest.fn()}
        profileId={profileId}
        workoutId={workoutId}
      />,
    );

    await screen.findByDisplayValue("Upper strength");
    fireEvent.press(screen.getByRole("button", { name: "Save workout" }));

    expect(
      await screen.findByText(
        "This workout changed elsewhere. Reload it before saving again.",
      ),
    ).toBeVisible();
  });

  it("validates a new workout before sending it", async () => {
    render(<WorkoutPlanner onSaved={jest.fn()} profileId={profileId} />);

    await screen.findByPlaceholderText("e.g. Upper strength");
    fireEvent.press(screen.getByRole("button", { name: "Save workout" }));

    expect(
      screen.getByText("Enter a workout name of no more than 80 characters."),
    ).toBeVisible();
    expect(
      screen.getByText("Choose between 1 and 20 exercises."),
    ).toBeVisible();
    expect(mockCreateWorkout).not.toHaveBeenCalled();
  });
});
