import { Pressable, StyleSheet, type PressableProps } from "react-native";

import { colors, layout, radii, spacing } from "../theme/tokens";
import { AppText } from "./AppText";

type PrimaryButtonProps = Omit<PressableProps, "children" | "style"> & {
  label: string;
};

export function PrimaryButton({ label, ...props }: PrimaryButtonProps) {
  return (
    <Pressable
      {...props}
      accessibilityRole="button"
      style={({ pressed }) => [styles.root, pressed && styles.pressed]}
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
  label: {
    color: colors.onAccent,
    textAlign: "center",
  },
});
