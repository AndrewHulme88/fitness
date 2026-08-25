import {
  fireEvent,
  renderRouter,
  screen,
  testRouter,
} from "expo-router/testing-library";

describe("initial navigation shell", () => {
  it("moves through onboarding and the initial workout flow", () => {
    const router = renderRouter("./src/app", { initialUrl: "/" });

    expect(router.getPathname()).toBe("/onboarding");
    expect(
      screen.getByRole("header", { name: "Make training fit your life." }),
    ).toBeVisible();

    fireEvent.press(screen.getByRole("button", { name: "Continue" }));

    expect(router.getPathname()).toBe("/workout/create");
    expect(
      screen.getByRole("header", { name: "Create your first workout." }),
    ).toBeVisible();

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
