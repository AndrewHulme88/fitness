import { Pressable, StyleSheet, View } from "react-native";

import { AppText } from "../../components/AppText";
import { colors, layout, radii, spacing } from "../../theme/tokens";

type SelectionControlProps = {
  description?: string;
  disabled?: boolean;
  label: string;
  mode: "multiple" | "single";
  onPress: () => void;
  selected: boolean;
};

export function SelectionControl({
  description,
  disabled = false,
  label,
  mode,
  onPress,
  selected,
}: SelectionControlProps) {
  return (
    <Pressable
      accessibilityHint={description}
      accessibilityLabel={label}
      accessibilityRole={mode === "multiple" ? "checkbox" : "radio"}
      accessibilityState={{ checked: selected, disabled }}
      disabled={disabled}
      onPress={onPress}
      style={({ pressed }) => [
        styles.root,
        selected && styles.selected,
        pressed && !disabled && styles.pressed,
        disabled && styles.disabled,
      ]}
    >
      <View
        importantForAccessibility="no-hide-descendants"
        style={[
          styles.indicator,
          mode === "single" && styles.radioIndicator,
          selected && styles.indicatorSelected,
        ]}
      >
        {selected ? (
          <AppText style={styles.indicatorMark} variant="label">
            {mode === "multiple" ? "✓" : "•"}
          </AppText>
        ) : null}
      </View>
      <View importantForAccessibility="no-hide-descendants" style={styles.copy}>
        <AppText variant="label">{label}</AppText>
        {description ? (
          <AppText tone="secondary" variant="body">
            {description}
          </AppText>
        ) : null}
      </View>
    </Pressable>
  );
}

const styles = StyleSheet.create({
  root: {
    minHeight: Math.max(layout.minimumTouchTarget, 56),
    flexDirection: "row",
    alignItems: "center",
    gap: spacing.md,
    paddingHorizontal: spacing.lg,
    paddingVertical: spacing.md,
    borderWidth: 1,
    borderColor: colors.border,
    borderRadius: radii.control,
    backgroundColor: colors.surface,
  },
  selected: {
    borderColor: colors.accentHighlight,
    backgroundColor: colors.surfaceRaised,
  },
  pressed: {
    opacity: 0.82,
  },
  disabled: {
    opacity: 0.48,
  },
  indicator: {
    width: 24,
    height: 24,
    alignItems: "center",
    justifyContent: "center",
    borderWidth: 1,
    borderColor: colors.border,
    borderRadius: radii.subtle,
  },
  indicatorSelected: {
    borderColor: colors.accent,
    backgroundColor: colors.accent,
  },
  radioIndicator: {
    borderRadius: 12,
  },
  indicatorMark: {
    color: colors.onAccent,
    lineHeight: 18,
  },
  copy: {
    flex: 1,
    gap: spacing.xs,
  },
});
