import { Redirect, type Href } from "expo-router";
import { useEffect, useState } from "react";

import { RouteStatus } from "../components/RouteStatus";
import { loadStoredProfile } from "../features/onboarding/profile-storage";
import { loadStoredSession } from "../features/sessions/session-storage";

export default function IndexRoute() {
  const [destination, setDestination] = useState<Href>();

  useEffect(() => {
    let active = true;
    async function restore() {
      const profile = await loadStoredProfile();
      if (!profile) {
        if (active) setDestination({ pathname: "/onboarding" });
        return;
      }

      const session = await loadStoredSession(profile.profileId);
      if (!active) return;
      const route =
        session?.status === "active"
          ? "/workout/session"
          : session?.status === "completed"
            ? "/workout/summary"
            : "/workouts";
      setDestination({
        pathname: route,
        params: { profileId: profile.profileId },
      });
    }
    void restore();
    return () => {
      active = false;
    };
  }, []);

  return destination ? (
    <Redirect href={destination} />
  ) : (
    <RouteStatus
      busy
      message="Checking for an interrupted workout."
      title="Opening Fitness Coach"
    />
  );
}
