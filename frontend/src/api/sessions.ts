import type { components } from "./generated/schema";
import { executeApiRequest, type ApiRequestOptions } from "./request";

export type StartWorkoutSessionRequest =
  components["schemas"]["StartWorkoutSessionRequest"];
export type UpdateWorkoutSessionRequest =
  components["schemas"]["UpdateWorkoutSessionRequest"];
export type WorkoutSession = components["schemas"]["WorkoutSessionResponse"];

export class WorkoutSessionConflictError extends Error {
  constructor() {
    super("The workout session changed after it was loaded.");
    this.name = "WorkoutSessionConflictError";
  }
}

export class ActiveWorkoutExistsError extends Error {
  constructor() {
    super("A workout is already active.");
    this.name = "ActiveWorkoutExistsError";
  }
}

export async function startWorkoutSession(
  profileId: string,
  request: StartWorkoutSessionRequest,
  options: ApiRequestOptions = {},
): Promise<WorkoutSession> {
  return executeApiRequest(options, async (client, signal) => {
    const { data, error, response } = await client.POST(
      "/profiles/{profileId}/workout-sessions",
      {
        params: { path: { profileId } },
        body: request,
        signal,
      },
    );

    if (response.status === 409) throw new ActiveWorkoutExistsError();
    if (error || !data) throw new Error("The workout could not be started.");
    return data;
  });
}

export async function getActiveWorkoutSession(
  profileId: string,
  options: ApiRequestOptions = {},
): Promise<WorkoutSession | null> {
  return executeApiRequest(options, async (client, signal) => {
    const { data, error, response } = await client.GET(
      "/profiles/{profileId}/workout-sessions/active",
      { params: { path: { profileId } }, signal },
    );

    if (response.status === 404) return null;
    if (error || !data)
      throw new Error("The active workout could not be loaded.");
    return data;
  });
}

export async function updateWorkoutSession(
  profileId: string,
  sessionId: string,
  request: UpdateWorkoutSessionRequest,
  options: ApiRequestOptions = {},
): Promise<WorkoutSession> {
  return executeApiRequest(options, async (client, signal) => {
    const { data, error, response } = await client.PUT(
      "/profiles/{profileId}/workout-sessions/{sessionId}",
      {
        params: { path: { profileId, sessionId } },
        body: request,
        signal,
      },
    );

    if (response.status === 409) throw new WorkoutSessionConflictError();
    if (error || !data)
      throw new Error("The workout changes could not be synchronized.");
    return data;
  });
}

export async function discardWorkoutSession(
  profileId: string,
  sessionId: string,
  options: ApiRequestOptions = {},
): Promise<void> {
  return executeApiRequest(options, async (client, signal) => {
    const { error, response } = await client.DELETE(
      "/profiles/{profileId}/workout-sessions/{sessionId}",
      { params: { path: { profileId, sessionId } }, signal },
    );

    if (response.status === 409) throw new WorkoutSessionConflictError();
    if (error || response.status !== 204) {
      throw new Error("The workout could not be discarded.");
    }
  });
}
