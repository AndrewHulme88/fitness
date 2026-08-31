import * as AuthSession from "expo-auth-session";
import * as SecureStore from "expo-secure-store";

const sessionKey = "cognito-session.v1";

export type CognitoConfiguration = {
  appClientId: string;
  domain: string;
  scope: string;
};

export type CognitoSession = Pick<
  AuthSession.TokenResponse,
  "accessToken" | "refreshToken" | "expiresIn" | "issuedAt"
>;

export function getCognitoConfiguration(): CognitoConfiguration {
  const domain = process.env.EXPO_PUBLIC_COGNITO_DOMAIN;
  const appClientId = process.env.EXPO_PUBLIC_COGNITO_APP_CLIENT_ID;
  const scope = process.env.EXPO_PUBLIC_COGNITO_SCOPE;
  if (!domain || !appClientId || !scope) {
    throw new Error("Cognito is not configured.");
  }
  return { appClientId, domain, scope };
}

export function getRedirectUri() {
  return AuthSession.makeRedirectUri({
    path: "auth/callback",
    scheme: "fitness-coach",
  });
}

export function getDiscovery(domain: string) {
  const normalizedDomain = domain.replace(/^https:\/\//, "").replace(/\/$/, "");
  const origin = `https://${normalizedDomain}`;

  return {
    authorizationEndpoint: `${origin}/oauth2/authorize`,
    tokenEndpoint: `${origin}/oauth2/token`,
    revocationEndpoint: `${origin}/oauth2/revoke`,
  };
}

export async function saveSession(session: CognitoSession) {
  await SecureStore.setItemAsync(sessionKey, JSON.stringify(session));
}

export async function clearSession() {
  await SecureStore.deleteItemAsync(sessionKey);
}

export async function loadAccessToken(): Promise<string | null> {
  const serialized = await SecureStore.getItemAsync(sessionKey);
  if (!serialized) return null;
  try {
    const session = JSON.parse(serialized) as CognitoSession;
    if (typeof session.accessToken !== "string") return null;
    if (AuthSession.TokenResponse.isTokenFresh(session))
      return session.accessToken;
    if (!session.refreshToken) {
      await clearSession();
      return null;
    }

    const configuration = getCognitoConfiguration();
    const refreshed = await AuthSession.refreshAsync(
      {
        clientId: configuration.appClientId,
        refreshToken: session.refreshToken,
      },
      getDiscovery(configuration.domain),
    );
    await saveSession(refreshed);
    return refreshed.accessToken;
  } catch {
    await clearSession();
    return null;
  }
}

export async function signOut() {
  const serialized = await SecureStore.getItemAsync(sessionKey);
  try {
    const session = serialized
      ? (JSON.parse(serialized) as CognitoSession)
      : null;
    if (session?.refreshToken) {
      const configuration = getCognitoConfiguration();
      await AuthSession.revokeAsync(
        { clientId: configuration.appClientId, token: session.refreshToken },
        getDiscovery(configuration.domain),
      );
    }
  } catch {
    // Local credential removal still completes if the network or revocation endpoint is unavailable.
  } finally {
    await clearSession();
  }
}

export function createAuthorizationRequest(
  configuration: CognitoConfiguration,
) {
  return new AuthSession.AuthRequest({
    clientId: configuration.appClientId,
    codeChallengeMethod: AuthSession.CodeChallengeMethod.S256,
    redirectUri: getRedirectUri(),
    responseType: AuthSession.ResponseType.Code,
    scopes: ["openid", configuration.scope],
  });
}
