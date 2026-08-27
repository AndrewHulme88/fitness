import { Stack, type ErrorBoundaryProps } from "expo-router";
import { StatusBar } from "expo-status-bar";
import { GestureHandlerRootView } from "react-native-gesture-handler";

import { RouteStatus } from "../components/RouteStatus";
import { colors } from "../theme/tokens";

export function SuspenseFallback() {
  return (
    <RouteStatus
      busy
      message="Your screen will be ready in a moment."
      title="Loading"
    />
  );
}

export function ErrorBoundary({ retry }: ErrorBoundaryProps) {
  return (
    <RouteStatus
      actionLabel="Try again"
      message="We couldn't open this screen. Please try again."
      onAction={retry}
      title="Something went wrong"
    />
  );
}

export default function RootLayout() {
  return (
    <GestureHandlerRootView style={{ flex: 1 }}>
      <Stack
        screenOptions={{
          contentStyle: { backgroundColor: colors.canvas },
          headerBackButtonDisplayMode: "minimal",
          headerShadowVisible: false,
          headerStyle: { backgroundColor: colors.canvas },
          headerTintColor: colors.textPrimary,
        }}
      >
        <Stack.Screen name="index" options={{ headerShown: false }} />
        <Stack.Screen name="onboarding" options={{ headerShown: false }} />
        <Stack.Screen name="workouts" options={{ headerShown: false }} />
        <Stack.Screen
          name="workout/create"
          options={{ title: "Create workout" }}
        />
        <Stack.Screen name="workout/session" options={{ title: "Workout" }} />
        <Stack.Screen name="workout/summary" options={{ title: "Summary" }} />
        <Stack.Screen name="+not-found" options={{ title: "Unavailable" }} />
      </Stack>
      <StatusBar style="light" />
    </GestureHandlerRootView>
  );
}
