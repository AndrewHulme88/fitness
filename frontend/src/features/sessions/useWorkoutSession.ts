import { randomUUID } from "expo-crypto";
import { useCallback, useEffect, useRef, useState } from "react";
import { AppState } from "react-native";

import {
  ActiveWorkoutExistsError,
  discardWorkoutSession,
  getActiveWorkoutSession,
  startWorkoutSession,
  updateWorkoutSession,
  WorkoutSessionConflictError,
} from "../../api/sessions";
import {
  editSession,
  type LocalWorkoutSession,
  sessionFromApi,
  sessionToUpdateRequest,
} from "./session-model";
import {
  loadStoredSession,
  removeStoredSession,
  saveStoredSession,
} from "./session-storage";

type LoadState = "loading" | "ready" | "error";

export function useWorkoutSession(profileId: string, workoutPlanId?: string) {
  const [session, setSessionState] = useState<LocalWorkoutSession | null>(null);
  const [loadState, setLoadState] = useState<LoadState>("loading");
  const [message, setMessage] = useState<string>();
  const sessionRef = useRef<LocalWorkoutSession | null>(null);
  const mountedRef = useRef(true);
  const retryWhenActiveRef = useRef(false);
  const synchronizingRef = useRef(false);
  const persistQueueRef = useRef(Promise.resolve());

  const setSession = useCallback((value: LocalWorkoutSession | null) => {
    sessionRef.current = value;
    if (mountedRef.current) setSessionState(value);
  }, []);

  const persist = useCallback((value: LocalWorkoutSession) => {
    persistQueueRef.current = persistQueueRef.current
      .catch(() => undefined)
      .then(() => saveStoredSession(value));
    return persistQueueRef.current;
  }, []);

  const synchronize = useCallback(async () => {
    if (synchronizingRef.current) return;
    const pending = sessionRef.current;
    if (!pending || pending.syncState !== "pending" || !pending.mutationId)
      return;

    synchronizingRef.current = true;
    const sentMutationId = pending.mutationId;
    retryWhenActiveRef.current = false;
    setMessage(undefined);
    setSession({ ...pending, syncState: "syncing" });

    try {
      const remote = await updateWorkoutSession(
        pending.profileId,
        pending.id,
        sessionToUpdateRequest(pending),
      );
      const current = sessionRef.current;
      if (!current) return;

      if (current.mutationId === sentMutationId) {
        const synced = {
          ...sessionFromApi(remote),
          restTimerEndsAt: current.restTimerEndsAt,
        };
        setSession(synced);
        await persist(synced);
      } else {
        const rebased = {
          ...current,
          revision: Number(remote.revision),
          syncState: "pending" as const,
        };
        setSession(rebased);
        await persist(rebased);
      }
    } catch (error) {
      const current = sessionRef.current;
      if (!current || current.mutationId !== sentMutationId) return;

      if (error instanceof WorkoutSessionConflictError) {
        const conflicted = { ...current, syncState: "conflict" as const };
        setSession(conflicted);
        await persist(conflicted);
        setMessage(
          "This session also changed elsewhere. Your device copy is still safe.",
        );
      } else {
        const waiting = { ...current, syncState: "pending" as const };
        retryWhenActiveRef.current = true;
        setSession(waiting);
        await persist(waiting);
        setMessage("Saved on this device. Waiting to synchronize.");
      }
    } finally {
      synchronizingRef.current = false;
    }
  }, [persist, setSession]);

  useEffect(() => {
    mountedRef.current = true;
    const controller = new AbortController();

    async function load() {
      try {
        const stored = await loadStoredSession(profileId);
        if (
          stored &&
          stored.status === "completed" &&
          stored.syncState === "synced" &&
          workoutPlanId
        ) {
          await removeStoredSession(profileId);
        } else if (stored) {
          const recoverable =
            stored.syncState === "syncing"
              ? { ...stored, syncState: "pending" as const }
              : stored;
          setSession(recoverable);
          setLoadState("ready");
          if (recoverable.syncState === "pending") void synchronize();
          return;
        }

        let remote;
        if (workoutPlanId) {
          try {
            remote = await startWorkoutSession(
              profileId,
              { sessionId: randomUUID(), workoutPlanId },
              { signal: controller.signal },
            );
          } catch (error) {
            if (!(error instanceof ActiveWorkoutExistsError)) throw error;
            remote = await getActiveWorkoutSession(profileId, {
              signal: controller.signal,
            });
          }
        } else {
          remote = await getActiveWorkoutSession(profileId, {
            signal: controller.signal,
          });
        }

        if (controller.signal.aborted) return;
        if (!remote) {
          setMessage("Start a saved workout before opening the logger.");
          setLoadState("error");
          return;
        }

        const local = sessionFromApi(remote);
        setSession(local);
        await persist(local);
        setLoadState("ready");
      } catch {
        if (!controller.signal.aborted) {
          setMessage(
            "The workout could not be started. Check the API connection and try again.",
          );
          setLoadState("error");
        }
      }
    }

    void load();
    return () => {
      mountedRef.current = false;
      controller.abort();
    };
  }, [persist, profileId, setSession, synchronize, workoutPlanId]);

  useEffect(() => {
    if (session?.syncState !== "pending" || retryWhenActiveRef.current) return;
    const timer = setTimeout(() => void synchronize(), 500);
    return () => clearTimeout(timer);
  }, [session?.mutationId, session?.syncState, synchronize]);

  useEffect(() => {
    const subscription = AppState.addEventListener("change", (state) => {
      if (state === "active" && sessionRef.current?.syncState === "pending") {
        retryWhenActiveRef.current = false;
        void synchronize();
      }
    });
    return () => subscription.remove();
  }, [synchronize]);

  const mutate = useCallback(
    (change: (current: LocalWorkoutSession) => LocalWorkoutSession) => {
      const current = sessionRef.current;
      if (!current || current.status !== "active") return;
      retryWhenActiveRef.current = false;
      const next = editSession(current, randomUUID(), change);
      setSession(next);
      return persist(next);
    },
    [persist, setSession],
  );

  const setRestTimer = useCallback(
    (endsAt: string | null) => {
      const current = sessionRef.current;
      if (!current) return;
      const next = { ...current, restTimerEndsAt: endsAt };
      setSession(next);
      void persist(next);
    },
    [persist, setSession],
  );

  const retry = useCallback(() => {
    retryWhenActiveRef.current = false;
    if (sessionRef.current?.syncState === "conflict") return;
    void synchronize();
  }, [synchronize]);

  const reloadServerVersion = useCallback(async () => {
    const remote = await getActiveWorkoutSession(profileId);
    if (!remote)
      throw new Error("The active workout no longer exists on the server.");
    const local = sessionFromApi(remote);
    setSession(local);
    setMessage(undefined);
    await persist(local);
  }, [persist, profileId, setSession]);

  const discard = useCallback(async () => {
    const current = sessionRef.current;
    if (!current) return;
    await discardWorkoutSession(profileId, current.id);
    await removeStoredSession(profileId);
    setSession(null);
  }, [profileId, setSession]);

  const clearCompleted = useCallback(async () => {
    const current = sessionRef.current;
    if (current?.status !== "completed" || current.syncState !== "synced") {
      return false;
    }
    await removeStoredSession(profileId);
    setSession(null);
    return true;
  }, [profileId, setSession]);

  return {
    session,
    loadState,
    message,
    mutate,
    setRestTimer,
    retry,
    reloadServerVersion,
    discard,
    clearCompleted,
  };
}
