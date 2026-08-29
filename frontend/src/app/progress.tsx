import { useLocalSearchParams, useRouter } from "expo-router";

import { RouteStatus } from "../components/RouteStatus";
import { ProgressOverview } from "../features/progress/ProgressOverview";

export default function ProgressRoute() {
  const router = useRouter();
  const { profileId } = useLocalSearchParams<{ profileId?: string }>();
  if (!profileId) {
    return (
      <RouteStatus
        actionLabel="Return to setup"
        message="Complete your training setup before reviewing progress."
        onAction={() => router.replace("/onboarding")}
        title="Profile required"
      />
    );
  }
  return (
    <ProgressOverview
      profileId={profileId}
      onHistory={() =>
        router.replace({ pathname: "/history", params: { profileId } })
      }
      onPlans={() =>
        router.replace({ pathname: "/workouts", params: { profileId } })
      }
      onSelectExercise={(exerciseId) =>
        router.push({
          pathname: "/workout/exercise-progress",
          params: { profileId, exerciseId },
        })
      }
    />
  );
}
