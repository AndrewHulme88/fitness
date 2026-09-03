jest.mock("react-native-reanimated", () =>
  require("react-native-reanimated/mock"),
);

jest.mock("@sentry/react-native", () => ({ init: jest.fn() }));
