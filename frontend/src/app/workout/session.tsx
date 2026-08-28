import { useLocalSearchParams, useRouter } from "expo-router";
import { useEffect, useState } from "react";

import { getTrainingProfile } from "../../api/profiles";
import { RouteStatus } from "../../components/RouteStatus";
import { ActiveWorkout } from "../../features/sessions/ActiveWorkout";
import type { UnitSystem } from "../../features/sessions/session-values";
import { loadStoredProfile } from "../../features/onboarding/profile-storage";

export default function WorkoutSessionRoute() {
  const router = useRouter();
  const { profileId, workoutId } = useLocalSearchParams<{
    profileId?: string;
    workoutId?: string;
  }>();
  const [unitSystem, setUnitSystem] = useState<UnitSystem>();
  const [profileError, setProfileError] = useState(false);

  useEffect(() => {
    if (!profileId) return;
    const controller = new AbortController();
    async function loadUnits() {
      const stored = await loadStoredProfile();
      if (stored && stored.profileId === profileId) {
        setUnitSystem(stored.unitSystem);
      }
      try {
        const profile = await getTrainingProfile(profileId as string, {
          signal: controller.signal,
        });
        setUnitSystem(profile.unitSystem);
      } catch {
        if (!controller.signal.aborted && stored?.profileId !== profileId) {
          setProfileError(true);
        }
      }
    }
    void loadUnits();
    return () => controller.abort();
  }, [profileId]);

  if (!profileId) {
    return (
      <RouteStatus
        actionLabel="Return to setup"
        message="A training profile is required to log a workout."
        onAction={() => router.replace("/onboarding")}
        title="Profile required"
      />
    );
  }

  if (profileError) {
    return (
      <RouteStatus
        actionLabel="Return to workouts"
        message="Your unit preference could not be loaded safely."
        onAction={() =>
          router.replace({ pathname: "/workouts", params: { profileId } })
        }
        title="Profile unavailable"
      />
    );
  }

  if (!unitSystem) {
    return (
      <RouteStatus
        busy
        message="Loading your unit preference."
        title="Preparing workout"
      />
    );
  }

  return (
    <ActiveWorkout
      profileId={profileId}
      workoutPlanId={workoutId}
      unitSystem={unitSystem}
      onExit={() =>
        router.replace({ pathname: "/workouts", params: { profileId } })
      }
      onFinished={() =>
        router.replace({ pathname: "/workout/summary", params: { profileId } })
      }
    />
  );
}
