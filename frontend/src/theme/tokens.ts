import type { TextStyle } from "react-native";

export const colors = {
  canvas: "#111224",
  surface: "#191A2C",
  surfaceRaised: "#22233A",
  textPrimary: "#F4F0E7",
  textSecondary: "#BAB9C7",
  accent: "#D46A48",
  accentHighlight: "#E48B6E",
  onAccent: "#1A1310",
  border: "#36374F",
  focus: "#F0A083",
} as const;

export const spacing = {
  xs: 4,
  sm: 8,
  md: 12,
  lg: 16,
  xl: 24,
  xxl: 32,
  xxxl: 48,
} as const;

export const typography = {
  display: {
    fontSize: 38,
    fontWeight: "700",
    letterSpacing: -1.25,
  },
  title: {
    fontSize: 28,
    fontWeight: "700",
    letterSpacing: -0.6,
  },
  body: {
    fontSize: 17,
    fontWeight: "400",
  },
  label: {
    fontSize: 15,
    fontWeight: "600",
  },
  eyebrow: {
    fontSize: 12,
    fontWeight: "700",
    letterSpacing: 1.1,
    textTransform: "uppercase",
  },
} as const satisfies Record<string, TextStyle>;

export const radii = {
  subtle: 4,
  control: 14,
  panel: 20,
} as const;

export const motion = {
  quick: 120,
  standard: 220,
  deliberate: 320,
} as const;

export const layout = {
  minimumTouchTarget: 44,
  readableContentWidth: 480,
} as const;

export type TextVariant = keyof typeof typography;
