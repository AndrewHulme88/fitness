import { render } from "@testing-library/react-native";

import App from "../App";

describe("<App />", () => {
  it("renders the application identity as an accessible heading", async () => {
    const { getByRole } = await render(<App />);

    expect(getByRole("header", { name: "Fitness Coach" })).toBeTruthy();
  });
});
