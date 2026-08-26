import {
  fireEvent,
  renderRouter,
  screen,
  testRouter,
  waitFor,
} from "expo-router/testing-library";

import { createTrainingProfile } from "../src/api/profiles";

jest.mock("../src/api/profiles", () => ({
  createTrainingProfile: jest.fn(),
}));

const createTrainingProfileMock = jest.mocked(createTrainingProfile);

describe("initial navigation shell", () => {
  beforeEach(() => {
    createTrainingProfileMock.mockReset();
    createTrainingProfileMock.mockResolvedValue({
      id: "6bf68a92-f5f8-40e5-a112-5330d83e31ed",
      goals: ["buildStrength"],
      experience: "beginner",
      availableEquipment: ["bodyweight"],
      unitSystem: "metric",
      createdAt: "2026-08-26T03:00:00Z",
    });
  });

  it("moves through onboarding and the initial workout flow", async () => {
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

    await waitFor(() => expect(router.getPathname()).toBe("/workout/create"));
    expect(createTrainingProfileMock).toHaveBeenCalledWith({
      goals: ["buildStrength"],
      experience: "beginner",
      availableEquipment: ["bodyweight"],
      unitSystem: "metric",
    });
    expect(
      screen.getByRole("header", { name: "Create your first workout." }),
    ).toBeVisible();
    expect(testRouter.canGoBack()).toBe(false);

    fireEvent.press(screen.getByRole("button", { name: "Start workout" }));

    expect(router.getPathname()).toBe("/workout/session");
    expect(screen.getByRole("header", { name: "Your workout." })).toBeVisible();

    fireEvent.press(screen.getByRole("button", { name: "Finish workout" }));

    expect(router.getPathname()).toBe("/workout/summary");
    expect(
      screen.getByRole("header", { name: "Review your session." }),
    ).toBeVisible();

    testRouter.back("/workout/session");

    expect(screen.getByRole("header", { name: "Your workout." })).toBeVisible();
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
