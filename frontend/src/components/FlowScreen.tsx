import { ScrollView, StyleSheet, View } from "react-native";

import { layout, spacing } from "../theme/tokens";
import { AppScreen } from "./AppScreen";
import { AppText } from "./AppText";
import { PrimaryButton } from "./PrimaryButton";

type FlowScreenProps = {
  description: string;
  eyebrow: string;
  title: string;
} & (
  | {
      actionLabel: string;
      onAction: () => void;
    }
  | {
      actionLabel?: never;
      onAction?: never;
    }
);

export function FlowScreen({
  actionLabel,
  description,
  eyebrow,
  onAction,
  title,
}: FlowScreenProps) {
  return (
    <AppScreen>
      <ScrollView
        contentContainerStyle={styles.content}
        contentInsetAdjustmentBehavior="automatic"
        showsVerticalScrollIndicator={false}
      >
        <View style={styles.copy}>
          <AppText tone="accent" variant="eyebrow">
            {eyebrow}
          </AppText>
          <AppText accessibilityRole="header" variant="display">
            {title}
          </AppText>
          <AppText tone="secondary">{description}</AppText>
          {actionLabel && onAction ? (
            <PrimaryButton label={actionLabel} onPress={onAction} />
          ) : null}
        </View>
      </ScrollView>
    </AppScreen>
  );
}

const styles = StyleSheet.create({
  content: {
    flexGrow: 1,
    alignItems: "center",
    justifyContent: "center",
    paddingHorizontal: spacing.xl,
    paddingVertical: spacing.xxxl,
  },
  copy: {
    width: "100%",
    maxWidth: layout.readableContentWidth,
    gap: spacing.lg,
  },
});
