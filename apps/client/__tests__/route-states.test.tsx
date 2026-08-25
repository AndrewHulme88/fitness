import { fireEvent, render, screen } from "@testing-library/react-native";

import { ErrorBoundary, SuspenseFallback } from "../src/app/_layout";

describe("route states", () => {
  it("shows a clear loading state", () => {
    render(<SuspenseFallback />);

    expect(screen.getByLabelText("Loading")).toBeVisible();
    expect(screen.getByRole("header", { name: "Loading" })).toBeVisible();
  });

  it("offers recovery without exposing the underlying error", () => {
    const retry = jest.fn();

    render(
      <ErrorBoundary error={new Error("sensitive detail")} retry={retry} />,
    );

    expect(
      screen.getByRole("header", { name: "Something went wrong" }),
    ).toBeVisible();
    expect(screen.queryByText("sensitive detail")).not.toBeOnTheScreen();

    fireEvent.press(screen.getByRole("button", { name: "Try again" }));

    expect(retry).toHaveBeenCalledTimes(1);
  });
});
