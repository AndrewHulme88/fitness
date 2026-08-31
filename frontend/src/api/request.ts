import { createApiClient, type ApiClient } from "./client";
import { loadAccessToken } from "../features/auth/cognito";

export type ApiRequestOptions = {
  baseUrl?: string;
  fetch?: typeof globalThis.fetch;
  signal?: AbortSignal;
};

const apiRequestTimeoutMilliseconds = 10_000;

export async function executeApiRequest<T>(
  options: ApiRequestOptions,
  operation: (client: ApiClient, signal: AbortSignal) => Promise<T>,
): Promise<T> {
  const baseUrl = options.baseUrl ?? process.env.EXPO_PUBLIC_API_URL;

  if (!baseUrl) {
    throw new Error("The API URL is not configured.");
  }

  const requestController = new AbortController();
  const abortFromCaller = () => requestController.abort();
  const timeout = setTimeout(
    () => requestController.abort(),
    apiRequestTimeoutMilliseconds,
  );

  if (options.signal?.aborted) {
    requestController.abort();
  } else {
    options.signal?.addEventListener("abort", abortFromCaller, { once: true });
  }

  try {
    const baseFetch = options.fetch ?? globalThis.fetch;
    const accessToken = await loadAccessToken();
    const authenticatedFetch: typeof globalThis.fetch = (input, init) => {
      const headers = new Headers(
        input instanceof Request ? input.headers : undefined,
      );
      new Headers(init?.headers).forEach((value, name) =>
        headers.set(name, value),
      );
      if (accessToken) headers.set("Authorization", `Bearer ${accessToken}`);
      return baseFetch(input, { ...init, headers });
    };
    const client = createApiClient({ baseUrl, fetch: authenticatedFetch });
    return await operation(client, requestController.signal);
  } finally {
    clearTimeout(timeout);
    options.signal?.removeEventListener("abort", abortFromCaller);
  }
}
