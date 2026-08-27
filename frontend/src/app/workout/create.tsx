import { useLocalSearchParams, useRouter } from "expo-router";

import { RouteStatus } from "../../components/RouteStatus";
import { WorkoutPlanner } from "../../features/workouts/WorkoutPlanner";

export default function CreateWorkoutRoute() {
  const router = useRouter();
  const { profileId, workoutId } = useLocalSearchParams<{
    profileId?: string;
    workoutId?: string;
  }>();

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

  return (
    <WorkoutPlanner
      onSaved={() =>
        router.replace({ pathname: "/workouts", params: { profileId } })
      }
      profileId={profileId}
      workoutId={workoutId}
    />
  );
}
