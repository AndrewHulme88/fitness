import { useLocalSearchParams, useRouter } from "expo-router";

import { RouteStatus } from "../components/RouteStatus";
import { CoachConversation } from "../features/coach/CoachConversation";

export default function CoachRoute() {
  const router = useRouter();
  const { profileId } = useLocalSearchParams<{ profileId?: string }>();
  if (!profileId)
    return (
      <RouteStatus
        actionLabel="Return to setup"
        message="Complete your training setup before using the coach."
        onAction={() => router.replace("/onboarding")}
        title="Profile required"
      />
    );
  return <CoachConversation profileId={profileId} />;
}
