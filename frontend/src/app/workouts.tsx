import { useLocalSearchParams, useRouter } from "expo-router";

import { RouteStatus } from "../components/RouteStatus";
import { WorkoutList } from "../features/workouts/WorkoutList";

export default function WorkoutsRoute() {
  const router = useRouter();
  const { profileId } = useLocalSearchParams<{ profileId?: string }>();

  if (!profileId) {
    return (
      <RouteStatus
        actionLabel="Return to setup"
        message="Complete your training setup before creating a workout."
        onAction={() => router.replace("/onboarding")}
        title="Profile required"
      />
    );
  }

  const openPlanner = (workoutId?: string) =>
    router.push({
      pathname: "/workout/create",
      params: { profileId, ...(workoutId ? { workoutId } : {}) },
    });

  return (
    <WorkoutList
      onCreate={() => openPlanner()}
      onEdit={openPlanner}
      onStart={(workoutId) =>
        router.push({
          pathname: "/workout/session",
          params: { profileId, workoutId },
        })
      }
      profileId={profileId}
    />
  );
}
