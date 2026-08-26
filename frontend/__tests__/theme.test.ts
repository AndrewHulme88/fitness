import { colors, layout } from "../src/theme/tokens";

function relativeLuminance(hex: string) {
  const channels = hex
    .slice(1)
    .match(/.{2}/g)
    ?.map((channel) => Number.parseInt(channel, 16) / 255)
    .map((channel) =>
      channel <= 0.04045
        ? channel / 12.92
        : Math.pow((channel + 0.055) / 1.055, 2.4),
    );

  if (!channels || channels.length !== 3) {
    throw new Error(`Expected a six-digit hex color, received ${hex}`);
  }

  return channels[0] * 0.2126 + channels[1] * 0.7152 + channels[2] * 0.0722;
}

function contrastRatio(foreground: string, background: string) {
  const foregroundLuminance = relativeLuminance(foreground);
  const backgroundLuminance = relativeLuminance(background);
  const lighter = Math.max(foregroundLuminance, backgroundLuminance);
  const darker = Math.min(foregroundLuminance, backgroundLuminance);

  return (lighter + 0.05) / (darker + 0.05);
}

describe("design tokens", () => {
  it.each([
    ["primary text", colors.textPrimary, colors.canvas],
    ["secondary text", colors.textSecondary, colors.canvas],
    ["accent text", colors.accentHighlight, colors.canvas],
    ["error text", colors.statusDanger, colors.canvas],
    ["text on the accent action", colors.onAccent, colors.accent],
  ])("keeps %s at WCAG AA contrast", (_name, foreground, background) => {
    expect(contrastRatio(foreground, background)).toBeGreaterThanOrEqual(4.5);
  });

  it("uses the iOS minimum touch target", () => {
    expect(layout.minimumTouchTarget).toBeGreaterThanOrEqual(44);
  });
});
