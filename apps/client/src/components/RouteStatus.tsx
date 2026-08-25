import { ActivityIndicator, ScrollView, StyleSheet, View } from "react-native";

import { colors, spacing } from "../theme/tokens";
import { AppScreen } from "./AppScreen";
import { AppText } from "./AppText";
import { PrimaryButton } from "./PrimaryButton";

type RouteStatusProps = {
  actionLabel?: string;
  busy?: boolean;
  message: string;
  onAction?: () => void;
  title: string;
};

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
    justifyContent: "center",
    paddingHorizontal: spacing.xl,
    paddingVertical: spacing.xxxl,
  },
  copy: {
    width: "100%",
    maxWidth: 380,
    gap: spacing.lg,
  },
});
