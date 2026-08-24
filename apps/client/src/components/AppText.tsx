import { StyleSheet, Text, type TextProps } from "react-native";

import { colors, typography, type TextVariant } from "../theme/tokens";

type TextTone = "primary" | "secondary" | "accent";

type AppTextProps = Omit<TextProps, "allowFontScaling"> & {
  tone?: TextTone;
  variant?: TextVariant;
};

const toneStyles = StyleSheet.create({
  primary: {
    color: colors.textPrimary,
  },
  secondary: {
    color: colors.textSecondary,
  },
  accent: {
    color: colors.accentHighlight,
  },
});

export function AppText({
  children,
  style,
  tone = "primary",
  variant = "body",
  ...props
}: AppTextProps) {
  return (
    <Text
      {...props}
      allowFontScaling
      style={[typography[variant], toneStyles[tone], style]}
    >
      {children}
    </Text>
  );
}
