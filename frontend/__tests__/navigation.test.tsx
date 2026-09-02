import {
  fireEvent,
  renderRouter,
  screen,
  testRouter,
  waitFor,
} from "expo-router/testing-library";

import { createTrainingProfile, getTrainingProfile } from "../src/api/profiles";
import { getCurrentAccount } from "../src/api/accounts";
import { listWorkouts } from "../src/api/workouts";
import {
  loadStoredProfile,
  removeStoredProfile,
  saveStoredProfile,
} from "../src/features/onboarding/profile-storage";
import { loadAccessToken } from "../src/features/auth/cognito";
import { removeStoredSession } from "../src/features/sessions/session-storage";

jest.mock("../src/api/profiles", () => ({
  createTrainingProfile: jest.fn(),
  getTrainingProfile: jest.fn(),
}));
jest.mock("../src/api/accounts", () => ({ getCurrentAccount: jest.fn() }));
jest.mock("../src/api/workouts", () => ({
  createWorkout: jest.fn(),
  getWorkout: jest.fn(),
  listWorkouts: jest.fn(),
  updateWorkout: jest.fn(),
  WorkoutRevisionConflictError: class WorkoutRevisionConflictError extends Error {},
}));
jest.mock("../src/features/onboarding/profile-storage", () => ({
  loadStoredProfile: jest.fn(),
  removeStoredProfile: jest.fn(),
  saveStoredProfile: jest.fn(),
}));
jest.mock("../src/features/sessions/session-storage", () => ({
  loadStoredSession: jest.fn(),
  removeStoredSession: jest.fn(),
}));
jest.mock("../src/features/auth/cognito", () => ({
  loadAccessToken: jest.fn(),
}));

const createTrainingProfileMock = jest.mocked(createTrainingProfile);
const getCurrentAccountMock = jest.mocked(getCurrentAccount);
const getTrainingProfileMock = jest.mocked(getTrainingProfile);
const listWorkoutsMock = jest.mocked(listWorkouts);
const loadStoredProfileMock = jest.mocked(loadStoredProfile);
const removeStoredProfileMock = jest.mocked(removeStoredProfile);
const saveStoredProfileMock = jest.mocked(saveStoredProfile);
const loadAccessTokenMock = jest.mocked(loadAccessToken);
const removeStoredSessionMock = jest.mocked(removeStoredSession);
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
    getCurrentAccountMock.mockReset();
    getTrainingProfileMock.mockReset();
    listWorkoutsMock.mockReset();
    loadStoredProfileMock.mockReset();
    removeStoredProfileMock.mockReset();
    saveStoredProfileMock.mockReset();
    loadAccessTokenMock.mockReset();
    removeStoredSessionMock.mockReset();
    createTrainingProfileMock.mockResolvedValue(profile);
    getCurrentAccountMock.mockResolvedValue({ profileId: null });
    getTrainingProfileMock.mockResolvedValue(profile);
    listWorkoutsMock.mockResolvedValue({ items: [], nextOffset: null });
    loadStoredProfileMock.mockResolvedValue(null);
    removeStoredProfileMock.mockResolvedValue(true);
    saveStoredProfileMock.mockResolvedValue(undefined);
    loadAccessTokenMock.mockResolvedValue("access-token");
    removeStoredSessionMock.mockResolvedValue(true);
  });

  it("moves from onboarding into workout planning with the profile context", async () => {
    const router = renderRouter("./src/app", { initialUrl: "/" });

    await waitFor(() => expect(router.getPathname()).toBe("/onboarding"));
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
    expect(saveStoredProfileMock).toHaveBeenCalledWith({
      schemaVersion: 1,
      profileId: profile.id,
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

  it("restores an existing account profile after local sign-out", async () => {
    loadStoredProfileMock.mockResolvedValue(null);
    getCurrentAccountMock.mockResolvedValue({ profileId: profile.id });

    const router = renderRouter("./src/app", { initialUrl: "/" });

    await waitFor(() => expect(router.getPathname()).toBe("/workouts"));
    expect(getTrainingProfileMock).toHaveBeenCalledWith(profile.id);
    expect(saveStoredProfileMock).toHaveBeenCalledWith({
      schemaVersion: 1,
      profileId: profile.id,
      unitSystem: "metric",
    });
  });

  it("clears another account's local workout before restoring this account", async () => {
    const otherProfileId = "20b843e5-4194-423d-a406-0418969178f5";
    loadStoredProfileMock.mockResolvedValue({
      schemaVersion: 1,
      profileId: otherProfileId,
      unitSystem: "imperial",
    });
    getCurrentAccountMock.mockResolvedValue({ profileId: profile.id });

    const router = renderRouter("./src/app", { initialUrl: "/" });

    await waitFor(() => expect(router.getPathname()).toBe("/workouts"));
    expect(removeStoredSessionMock).toHaveBeenCalledWith(otherProfileId);
    expect(getTrainingProfileMock).toHaveBeenCalledWith(profile.id);
    expect(saveStoredProfileMock).toHaveBeenCalledWith({
      schemaVersion: 1,
      profileId: profile.id,
      unitSystem: "metric",
    });
  });

  it("clears local fitness cache when the signed-in account has no profile", async () => {
    loadStoredProfileMock.mockResolvedValue({
      schemaVersion: 1,
      profileId: profile.id,
      unitSystem: "metric",
    });
    getCurrentAccountMock.mockResolvedValue({ profileId: null });

    const router = renderRouter("./src/app", { initialUrl: "/" });

    await waitFor(() => expect(router.getPathname()).toBe("/onboarding"));
    expect(removeStoredSessionMock).toHaveBeenCalledWith(profile.id);
    expect(removeStoredProfileMock).toHaveBeenCalledTimes(1);
    expect(getTrainingProfileMock).not.toHaveBeenCalled();
  });
});
