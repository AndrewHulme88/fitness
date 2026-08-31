import { usePathname, useRouter } from "expo-router";
import { Alert, Pressable, StyleSheet, View } from "react-native";
import { SafeAreaView } from "react-native-safe-area-context";

import { clearSession } from "../features/auth/cognito";
import {
  loadStoredProfile,
  removeStoredProfile,
} from "../features/onboarding/profile-storage";
import { removeStoredSession } from "../features/sessions/session-storage";
import { colors, radii, spacing } from "../theme/tokens";
import { AppText } from "./AppText";

const routesWithoutSignOut = new Set(["/", "/+not-found", "/sign-in"]);

export function SignOutControl() {
  const pathname = usePathname();
  const router = useRouter();

  if (routesWithoutSignOut.has(pathname)) return null;

  const signOut = () => {
    Alert.alert(
      "Sign out?",
      "This removes your account session and locally saved workout data from this device. Your synced data stays in your account.",
      [
        { style: "cancel", text: "Cancel" },
        {
          style: "destructive",
          text: "Sign out",
          onPress: () => {
            void signOutLocally(router.replace);
          },
        },
      ],
    );
  };

  return (
    <View pointerEvents="box-none" style={styles.overlay}>
      <SafeAreaView pointerEvents="box-none" style={styles.safeArea}>
        <Pressable
          accessibilityLabel="Sign out"
          accessibilityRole="button"
          onPress={signOut}
          style={({ pressed }) => [styles.button, pressed && styles.pressed]}
        >
          <AppText style={styles.label} variant="label">
            Sign out
          </AppText>
        </Pressable>
      </SafeAreaView>
    </View>
  );
}

async function signOutLocally(replace: (href: "/sign-in") => void) {
  const profile = await loadStoredProfile();
  if (profile) {
    await removeStoredSession(profile.profileId);
    await removeStoredProfile();
  }
  await clearSession();
  replace("/sign-in");
}

const styles = StyleSheet.create({
  overlay: {
    position: "absolute",
    top: 0,
    right: 0,
    bottom: 0,
    left: 0,
    alignItems: "flex-end",
    justifyContent: "flex-start",
  },
  safeArea: {
    alignItems: "flex-end",
    paddingRight: spacing.lg,
  },
  button: {
    minHeight: 44,
    alignItems: "center",
    justifyContent: "center",
    paddingHorizontal: spacing.md,
    borderWidth: StyleSheet.hairlineWidth,
    borderColor: colors.border,
    borderRadius: radii.control,
    backgroundColor: colors.canvas,
  },
  pressed: { opacity: 0.7 },
  label: { color: colors.textPrimary },
});
