import { Pressable, StyleSheet, type PressableProps } from "react-native";

import { colors, layout, radii, spacing } from "../theme/tokens";
import { AppText } from "./AppText";

type PrimaryButtonProps = Omit<
  PressableProps,
  "accessibilityRole" | "children" | "style"
> & {
  label: string;
};

export function PrimaryButton({
  accessibilityState,
  disabled = false,
  label,
  ...props
}: PrimaryButtonProps) {
  const isDisabled = disabled === true;

  return (
    <Pressable
      {...props}
      accessibilityRole="button"
      accessibilityState={{ ...accessibilityState, disabled: isDisabled }}
      disabled={isDisabled}
      style={({ pressed }) => [
        styles.root,
        pressed && !isDisabled && styles.pressed,
        isDisabled && styles.disabled,
      ]}
    >
      <AppText style={styles.label} variant="label">
        {label}
      </AppText>
    </Pressable>
  );
}

const styles = StyleSheet.create({
  root: {
    minHeight: Math.max(layout.minimumTouchTarget, 52),
    alignItems: "center",
    justifyContent: "center",
    paddingHorizontal: spacing.xl,
    borderRadius: radii.control,
    backgroundColor: colors.accent,
  },
  pressed: {
    opacity: 0.84,
  },
  disabled: {
    opacity: 0.48,
  },
  label: {
    color: colors.onAccent,
    textAlign: "center",
  },
});
