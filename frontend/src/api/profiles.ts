import type { components } from "./generated/schema";
import { createApiClient } from "./client";

export type CreateTrainingProfileRequest =
  components["schemas"]["CreateTrainingProfileRequest"];
export type TrainingProfile = components["schemas"]["TrainingProfileResponse"];

type CreateTrainingProfileOptions = {
  baseUrl?: string;
  fetch?: typeof globalThis.fetch;
  signal?: AbortSignal;
};

const profileRequestTimeoutMilliseconds = 10_000;

export async function createTrainingProfile(
  request: CreateTrainingProfileRequest,
  options: CreateTrainingProfileOptions = {},
): Promise<TrainingProfile> {
  const baseUrl = options.baseUrl ?? process.env.EXPO_PUBLIC_API_URL;

  if (!baseUrl) {
    throw new Error("The API URL is not configured.");
  }

  const requestController = new AbortController();
  const abortFromCaller = () => requestController.abort();
  const timeout = setTimeout(
    () => requestController.abort(),
    profileRequestTimeoutMilliseconds,
  );

  if (options.signal?.aborted) {
    requestController.abort();
  } else {
    options.signal?.addEventListener("abort", abortFromCaller, { once: true });
  }

  try {
    const client = createApiClient({ baseUrl, fetch: options.fetch });
    const { data, error } = await client.POST("/profiles", {
      body: request,
      signal: requestController.signal,
    });

    if (error || !data) {
      throw new Error("The profile could not be saved.");
    }

    return data;
  } finally {
    clearTimeout(timeout);
    options.signal?.removeEventListener("abort", abortFromCaller);
  }
}
