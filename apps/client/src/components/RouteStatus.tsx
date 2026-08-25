import { ActivityIndicator, ScrollView, StyleSheet, View } from "react-native";

import { colors, layout, spacing } from "../theme/tokens";
import { AppScreen } from "./AppScreen";
import { AppText } from "./AppText";
import { PrimaryButton } from "./PrimaryButton";

type RouteStatusProps = {
  busy?: boolean;
  message: string;
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

export function RouteStatus({
  actionLabel,
  busy = false,
  message,
  onAction,
  title,
}: RouteStatusProps) {
  return (
    <AppScreen>
      <ScrollView
        contentContainerStyle={styles.content}
        contentInsetAdjustmentBehavior="automatic"
        showsVerticalScrollIndicator={false}
      >
        <View style={styles.copy}>
          {busy ? (
            <ActivityIndicator
              accessibilityLabel="Loading"
              accessibilityRole="progressbar"
              color={colors.accentHighlight}
              size="large"
            />
          ) : null}
          <AppText accessibilityRole="header" variant="title">
            {title}
          </AppText>
          <AppText tone="secondary">{message}</AppText>
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
