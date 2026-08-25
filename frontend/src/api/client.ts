import createClient from "openapi-fetch";

import type { paths } from "./generated/schema";

export type ApiClient = ReturnType<typeof createApiClient>;

export interface ApiClientOptions {
  baseUrl: string;
  fetch?: typeof globalThis.fetch;
}

export function createApiClient({ baseUrl, fetch }: ApiClientOptions) {
  if (baseUrl.trim().length === 0) {
    throw new Error("API base URL is required.");
  }

  return createClient<paths>({
    baseUrl: baseUrl.replace(/\/$/, ""),
    fetch,
  });
}
