import { StatusBar } from "expo-status-bar";
import { ScrollView, StyleSheet, View } from "react-native";

import { AppScreen } from "./src/components/AppScreen";
import { AppText } from "./src/components/AppText";
import { colors, spacing } from "./src/theme/tokens";

export default function App() {
  return (
    <AppScreen>
      <ScrollView
        contentContainerStyle={styles.content}
        contentInsetAdjustmentBehavior="automatic"
        showsVerticalScrollIndicator={false}
      >
        <View style={styles.identity}>
          <AppText tone="accent" variant="eyebrow">
            Fitness Coach
          </AppText>
          <AppText accessibilityRole="header" variant="display">
            Build strength that lasts.
          </AppText>
          <View accessibilityElementsHidden style={styles.accentRule} />
        </View>
      </ScrollView>
      <StatusBar style="light" />
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
  identity: {
    gap: spacing.lg,
    maxWidth: 340,
  },
  accentRule: {
    width: 48,
    height: 4,
    marginTop: spacing.sm,
    borderRadius: 2,
    backgroundColor: colors.accent,
  },
});
