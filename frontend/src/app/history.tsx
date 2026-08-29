import { useLocalSearchParams, useRouter } from "expo-router";

import { RouteStatus } from "../components/RouteStatus";
import { WorkoutHistoryList } from "../features/history/WorkoutHistoryList";

export default function HistoryRoute() {
  const router = useRouter();
  const { profileId } = useLocalSearchParams<{ profileId?: string }>();
  if (!profileId) {
    return (
      <RouteStatus
        actionLabel="Return to setup"
        message="Complete your training setup before reviewing history."
        onAction={() => router.replace("/onboarding")}
        title="Profile required"
      />
    );
  }

  return (
    <WorkoutHistoryList
      profileId={profileId}
      onPlans={() =>
        router.replace({ pathname: "/workouts", params: { profileId } })
      }
      onProgress={() =>
        router.replace({ pathname: "/progress", params: { profileId } })
      }
      onSelect={(sessionId) =>
        router.push({
          pathname: "/workout/history-detail",
          params: { profileId, sessionId },
        })
      }
    />
  );
}
