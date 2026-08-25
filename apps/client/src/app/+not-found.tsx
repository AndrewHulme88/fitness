import { useRouter } from "expo-router";

import { RouteStatus } from "../components/RouteStatus";

export default function NotFoundRoute() {
  const router = useRouter();

  return (
    <RouteStatus
      actionLabel="Return to setup"
      message="The destination may have moved or is not part of this version of the app."
      onAction={() => router.replace("/onboarding")}
      title="This screen isn't available"
    />
  );
}
