import { render } from "@testing-library/react-native";

import App from "../App";

describe("<App />", () => {
  it("renders the application identity as a scalable accessible heading", async () => {
    const { getByRole } = await render(<App />);
    const heading = getByRole("header", {
      name: "Build strength that lasts.",
    });

    expect(heading).toBeTruthy();
    expect(heading.props.allowFontScaling).toBe(true);
  });
});
