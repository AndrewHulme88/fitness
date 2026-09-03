import * as Sentry from "@sentry/react-native";

const sentryDsn = process.env.EXPO_PUBLIC_SENTRY_DSN;

export function initializeCrashReporting() {
  if (!sentryDsn || __DEV__) return;

  Sentry.init({
    beforeBreadcrumb: () => null,
    beforeSend: (event) => {
      delete event.breadcrumbs;
      delete event.contexts;
      delete event.extra;
      delete event.request;
      delete event.user;
      return event;
    },
    dsn: sentryDsn,
    enableAutoSessionTracking: false,
    sendDefaultPii: false,
    tracesSampleRate: 0,
  });
}
