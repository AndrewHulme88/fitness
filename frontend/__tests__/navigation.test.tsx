import {
  fireEvent,
  renderRouter,
  screen,
  testRouter,
  waitFor,
} from "expo-router/testing-library";

import { createTrainingProfile, getTrainingProfile } from "../src/api/profiles";
import { listWorkouts } from "../src/api/workouts";

jest.mock("../src/api/profiles", () => ({
  createTrainingProfile: jest.fn(),
  getTrainingProfile: jest.fn(),
}));
jest.mock("../src/api/workouts", () => ({
  createWorkout: jest.fn(),
  getWorkout: jest.fn(),
  listWorkouts: jest.fn(),
  updateWorkout: jest.fn(),
  WorkoutRevisionConflictError: class WorkoutRevisionConflictError extends Error {},
}));

const createTrainingProfileMock = jest.mocked(createTrainingProfile);
const getTrainingProfileMock = jest.mocked(getTrainingProfile);
const listWorkoutsMock = jest.mocked(listWorkouts);
const profile = {
  id: "6bf68a92-f5f8-40e5-a112-5330d83e31ed",
  goals: ["buildStrength" as const],
  experience: "beginner" as const,
  availableEquipment: ["bodyweight" as const],
  unitSystem: "metric" as const,
  createdAt: "2026-08-26T03:00:00Z",
};

describe("initial navigation shell", () => {
  beforeEach(() => {
    createTrainingProfileMock.mockReset();
    getTrainingProfileMock.mockReset();
    listWorkoutsMock.mockReset();
    createTrainingProfileMock.mockResolvedValue(profile);
    getTrainingProfileMock.mockResolvedValue(profile);
    listWorkoutsMock.mockResolvedValue({ items: [], nextOffset: null });
  });

  it("moves from onboarding into workout planning with the profile context", async () => {
    const router = renderRouter("./src/app", { initialUrl: "/" });

    expect(router.getPathname()).toBe("/onboarding");
    expect(
      screen.getByRole("header", { name: "Make training fit your life." }),
    ).toBeVisible();

    fireEvent.press(screen.getByRole("checkbox", { name: "Build strength" }));
    fireEvent.press(screen.getByRole("radio", { name: "Beginner" }));
    fireEvent.press(screen.getByRole("checkbox", { name: "Bodyweight" }));
    fireEvent.press(screen.getByRole("radio", { name: "Metric" }));
    fireEvent.press(screen.getByRole("button", { name: "Save and continue" }));

    await waitFor(() => expect(router.getPathname()).toBe("/workouts"));
    expect(createTrainingProfileMock).toHaveBeenCalledWith({
      goals: ["buildStrength"],
      experience: "beginner",
      availableEquipment: ["bodyweight"],
      unitSystem: "metric",
    });
    expect(screen.getByRole("header", { name: "Your workouts" })).toBeVisible();
    expect(testRouter.canGoBack()).toBe(false);

    fireEvent.press(screen.getByRole("button", { name: "Create workout" }));

    await waitFor(() => expect(router.getPathname()).toBe("/workout/create"));
    expect(
      await screen.findByRole("header", {
        name: "Build a workout you can reuse.",
      }),
    ).toBeVisible();
    expect(getTrainingProfileMock).toHaveBeenCalledWith(
      profile.id,
      expect.objectContaining({ signal: expect.any(AbortSignal) }),
    );
  });

  it("recovers from an unavailable route", () => {
    const router = renderRouter("./src/app", { initialUrl: "/missing" });

    expect(router.getPathname()).toBe("/missing");
    expect(
      screen.getByRole("header", {
        name: "This screen isn't available",
      }),
    ).toBeVisible();

    fireEvent.press(screen.getByRole("button", { name: "Return to setup" }));

    expect(router.getPathname()).toBe("/onboarding");
    expect(testRouter.canGoBack()).toBe(false);
  });
});
