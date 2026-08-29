import { useLocalSearchParams } from "expo-router";

import { RouteStatus } from "../../components/RouteStatus";
import { ExercisePerformance } from "../../features/progress/ExercisePerformance";

export default function ExerciseProgressRoute() {
  const { profileId, exerciseId } = useLocalSearchParams<{
    profileId?: string;
    exerciseId?: string;
  }>();
  if (!profileId || !exerciseId) {
    return (
      <RouteStatus
        message="The profile or exercise identifier is missing."
        title="Performance unavailable"
      />
    );
  }
  return <ExercisePerformance exerciseId={exerciseId} profileId={profileId} />;
}
