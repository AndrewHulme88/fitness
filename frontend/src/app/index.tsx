import { Redirect, type Href } from "expo-router";
import { useEffect, useState } from "react";

import { getCurrentAccount } from "../api/accounts";
import { getTrainingProfile } from "../api/profiles";
import { RouteStatus } from "../components/RouteStatus";
import {
  loadStoredProfile,
  saveStoredProfile,
} from "../features/onboarding/profile-storage";
import { loadAccessToken } from "../features/auth/cognito";
import { loadStoredSession } from "../features/sessions/session-storage";

export default function IndexRoute() {
  const [destination, setDestination] = useState<Href>();
  const [restoreFailed, setRestoreFailed] = useState(false);
  const [restoreAttempt, setRestoreAttempt] = useState(0);

  useEffect(() => {
    let active = true;
    async function restore() {
      try {
        const accessToken = await loadAccessToken();
        if (!accessToken) {
          if (active) setDestination("/sign-in" as Href);
          return;
        }

        let profile = await loadStoredProfile();
        if (!profile) {
          const account = await getCurrentAccount();
          if (!account.profileId) {
            if (active) setDestination({ pathname: "/onboarding" });
            return;
          }

          const serverProfile = await getTrainingProfile(account.profileId);
          profile = {
            schemaVersion: 1,
            profileId: serverProfile.id,
            unitSystem: serverProfile.unitSystem,
          };
          await saveStoredProfile(profile);
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
      } catch {
        if (active) setRestoreFailed(true);
      }
    }
    void restore();
    return () => {
      active = false;
    };
  }, [restoreAttempt]);

  if (restoreFailed) {
    return (
      <RouteStatus
        actionLabel="Try again"
        message="We couldn’t restore your account. Check your connection and try again."
        onAction={() => {
          setRestoreFailed(false);
          setRestoreAttempt((attempt) => attempt + 1);
        }}
        title="Account unavailable"
      />
    );
  }

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
