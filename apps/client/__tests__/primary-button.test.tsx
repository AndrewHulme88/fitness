import { fireEvent, render, screen } from "@testing-library/react-native";

import { PrimaryButton } from "../src/components/PrimaryButton";

describe("PrimaryButton", () => {
  it("invokes its action", () => {
    const onPress = jest.fn();

    render(<PrimaryButton label="Continue" onPress={onPress} />);

    fireEvent.press(screen.getByRole("button", { name: "Continue" }));

    expect(onPress).toHaveBeenCalledTimes(1);
  });

  it("communicates and enforces its disabled state", () => {
    const onPress = jest.fn();

    render(<PrimaryButton disabled label="Continue" onPress={onPress} />);

    const button = screen.getByRole("button", { name: "Continue" });
    expect(button).toBeDisabled();
    expect(button).toHaveProp("accessibilityState", { disabled: true });
    expect(button).toHaveStyle({ opacity: 0.48 });

    fireEvent.press(button);

    expect(onPress).not.toHaveBeenCalled();
  });
});
