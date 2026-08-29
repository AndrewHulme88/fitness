import { useLocalSearchParams } from "expo-router";

import { RouteStatus } from "../../components/RouteStatus";
import { WorkoutHistoryDetail } from "../../features/history/WorkoutHistoryDetail";

export default function WorkoutHistoryDetailRoute() {
  const { profileId, sessionId } = useLocalSearchParams<{
    profileId?: string;
    sessionId?: string;
  }>();
  if (!profileId || !sessionId) {
    return (
      <RouteStatus
        message="The profile or workout record identifier is missing."
        title="Record unavailable"
      />
    );
  }
  return <WorkoutHistoryDetail profileId={profileId} sessionId={sessionId} />;
}
