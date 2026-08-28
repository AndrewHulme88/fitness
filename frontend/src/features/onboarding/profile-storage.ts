import Storage from "expo-sqlite/kv-store";

import type { UnitSystem } from "../sessions/session-values";

const profileKey = "local-training-profile:v1";

export type StoredProfile = {
  schemaVersion: 1;
  profileId: string;
  unitSystem: UnitSystem;
};

export async function loadStoredProfile(): Promise<StoredProfile | null> {
  const serialized = await Storage.getItemAsync(profileKey);
  if (!serialized) return null;
  try {
    const value: unknown = JSON.parse(serialized);
    if (isStoredProfile(value)) return value;
  } catch {
    // Corrupt or obsolete local state is removed rather than trusted.
  }
  await Storage.removeItemAsync(profileKey);
  return null;
}

export function saveStoredProfile(profile: StoredProfile) {
  return Storage.setItemAsync(profileKey, JSON.stringify(profile));
}

function isStoredProfile(value: unknown): value is StoredProfile {
  if (typeof value !== "object" || value === null) return false;
  const candidate = value as Record<string, unknown>;
  return (
    candidate.schemaVersion === 1 &&
    typeof candidate.profileId === "string" &&
    (candidate.unitSystem === "metric" || candidate.unitSystem === "imperial")
  );
}
