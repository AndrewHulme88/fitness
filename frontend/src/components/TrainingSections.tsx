import { Pressable, StyleSheet, View } from "react-native";

import { colors, spacing } from "../theme/tokens";
import { AppText } from "./AppText";

type Section = "plans" | "history" | "progress";

type Props = {
  active: Section;
  onHistory: () => void;
  onPlans: () => void;
  onProgress: () => void;
};

export function TrainingSections({
  active,
  onHistory,
  onPlans,
  onProgress,
}: Props) {
  return (
    <View accessibilityRole="tablist" style={styles.root}>
      <SectionButton
        active={active === "plans"}
        label="Plans"
        onPress={onPlans}
      />
      <SectionButton
        active={active === "history"}
        label="History"
        onPress={onHistory}
      />
      <SectionButton
        active={active === "progress"}
        label="Progress"
        onPress={onProgress}
      />
    </View>
  );
}

function SectionButton({
  active,
  label,
  onPress,
}: {
  active: boolean;
  label: string;
  onPress: () => void;
}) {
  return (
    <Pressable
      accessibilityRole="tab"
      accessibilityState={{ selected: active }}
      onPress={onPress}
      style={[styles.button, active && styles.activeButton]}
    >
      <AppText tone={active ? "primary" : "secondary"} variant="label">
        {label}
      </AppText>
    </Pressable>
  );
}

const styles = StyleSheet.create({
  root: {
    flexDirection: "row",
    borderBottomWidth: StyleSheet.hairlineWidth,
    borderBottomColor: colors.border,
  },
  button: {
    minHeight: 48,
    flex: 1,
    alignItems: "center",
    justifyContent: "center",
    borderBottomWidth: 2,
    borderBottomColor: "transparent",
    paddingHorizontal: spacing.sm,
  },
  activeButton: { borderBottomColor: colors.accentHighlight },
});
