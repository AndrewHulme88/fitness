import {
  fireEvent,
  render,
  screen,
  waitFor,
} from "@testing-library/react-native";

import { OnboardingForm } from "./OnboardingForm";

describe("OnboardingForm", () => {
  it("requires every onboarding section before submission", () => {
    const onSubmit = jest.fn();
    render(<OnboardingForm onSubmit={onSubmit} />);

    fireEvent.press(screen.getByRole("button", { name: "Save and continue" }));

    expect(screen.getByText("Choose at least one goal.")).toBeVisible();
    expect(screen.getByText("Choose your experience level.")).toBeVisible();
    expect(
      screen.getByText("Choose at least one equipment option."),
    ).toBeVisible();
    expect(screen.getByText("Choose your preferred units.")).toBeVisible();
    expect(onSubmit).not.toHaveBeenCalled();
  });

  it("submits the selected generated-contract values", async () => {
    const onSubmit = jest.fn().mockResolvedValue(undefined);
    render(<OnboardingForm onSubmit={onSubmit} />);

    selectRequiredOptions();
    fireEvent.press(screen.getByRole("checkbox", { name: "Build muscle" }));
    fireEvent.press(screen.getByRole("button", { name: "Save and continue" }));

    await waitFor(() =>
      expect(onSubmit).toHaveBeenCalledWith({
        goals: ["buildStrength", "buildMuscle"],
        experience: "beginner",
        availableEquipment: ["bodyweight"],
        unitSystem: "metric",
      }),
    );
  });

  it("keeps selections and offers a safe retry after submission fails", async () => {
    const onSubmit = jest
      .fn()
      .mockRejectedValueOnce(new Error("sensitive server detail"))
      .mockResolvedValueOnce(undefined);
    render(<OnboardingForm onSubmit={onSubmit} />);

    selectRequiredOptions();
    fireEvent.press(screen.getByRole("button", { name: "Save and continue" }));

    expect(
      await screen.findByRole("alert", {
        name: "We couldn’t save your setup. Check your connection and try again.",
      }),
    ).toBeVisible();
    expect(screen.queryByText("sensitive server detail")).not.toBeOnTheScreen();
    expect(
      screen.getByRole("checkbox", { name: "Build strength" }),
    ).toBeChecked();

    fireEvent.press(screen.getByRole("button", { name: "Save and continue" }));

    await waitFor(() => expect(onSubmit).toHaveBeenCalledTimes(2));
  });

  it("disables the form and communicates progress while saving", async () => {
    let finishSubmission: (() => void) | undefined;
    const onSubmit = jest.fn(
      () =>
        new Promise<void>((resolve) => {
          finishSubmission = resolve;
        }),
    );
    render(<OnboardingForm onSubmit={onSubmit} />);

    selectRequiredOptions();
    fireEvent.press(screen.getByRole("button", { name: "Save and continue" }));

    expect(
      screen.getByRole("button", { name: "Saving setup…" }),
    ).toBeDisabled();
    expect(
      screen.getByRole("checkbox", { name: "Build strength" }),
    ).toBeDisabled();

    finishSubmission?.();
    await waitFor(() =>
      expect(
        screen.getByRole("button", { name: "Save and continue" }),
      ).toBeEnabled(),
    );
  });
});

function selectRequiredOptions() {
  fireEvent.press(screen.getByRole("checkbox", { name: "Build strength" }));
  fireEvent.press(screen.getByRole("radio", { name: "Beginner" }));
  fireEvent.press(screen.getByRole("checkbox", { name: "Bodyweight" }));
  fireEvent.press(screen.getByRole("radio", { name: "Metric" }));
}
