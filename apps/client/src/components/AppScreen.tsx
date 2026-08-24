import type { PropsWithChildren } from "react";
import { StyleSheet, View, type ViewProps } from "react-native";

import { colors } from "../theme/tokens";

type AppScreenProps = PropsWithChildren<ViewProps>;

export function AppScreen({ children, style, ...props }: AppScreenProps) {
  return (
    <View {...props} style={[styles.root, style]}>
      {children}
    </View>
  );
}

const styles = StyleSheet.create({
  root: {
    flex: 1,
    backgroundColor: colors.canvas,
  },
});
