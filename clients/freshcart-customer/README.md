# FreshCart customer storefront

The Angular 20 storefront for FreshCart. It is a zoneless, signal-based single-page
application that talks only to the YARP gateway through the dev proxy. The browser holds an
HttpOnly session cookie issued by Identity; no access tokens ever reach JavaScript. State
changing requests carry the XSRF double-submit header.

## Run it

The storefront expects the gateway (and the services behind it) to be running. See the repo
root [`README.md`](../../README.md) for booting the platform with Docker Compose and the Aspire
AppHost.

```bash
npm ci
npm start
```

`npm start` runs `ng serve` with `proxy.conf.json`, which forwards `/api` and `/hubs` to the
gateway on `https://localhost:7100`. Open http://localhost:4200.

```bash
npm run build   # production bundle
npm test        # Karma + Jasmine unit tests (ChromeHeadless)
npm run lint    # eslint + angular template rules
```

## Architecture

```
src/app/
  core/        cross-cutting singletons: auth store, HTTP interceptors, error handler,
               toast service, basket store, realtime (SignalR) connection + notifications store,
               api-routes registry, injectable clock and timing tokens, title strategy
  shared/      reusable presentational pieces: pipes, directives, dialog service,
               loading/empty/not-found/rating components
  layout/      app shell, header, footer, toast host, notification bell
  features/    lazy-loaded routes: home, catalog, basket, checkout, orders, account,
               dashboard, auth, support
```

State lives in one store per concern. `AuthStore`, `BasketStore`, `NotificationsStore` and
`SupportChatStore` are root `@ngrx/signals` stores; feature-local state such as the product
list uses a feature-scoped signal store. Synchronous state is held in signals; RxJS is used only
for genuinely asynchronous streams (HTTP, SignalR) and is terminated at the component boundary.

### Realtime

`SignalrConnectionFactory` builds a `HubConnection` per hub path with cookie credentials,
automatic reconnect, and warning-level logging. `RealtimeConnection` wraps it, exposing the
connection state as a signal and serialising start/stop.

`NotificationsStore` opens `/hubs/notifications` when `AuthStore` reports an authenticated
session and stops, deregisters every handler, and clears its state on sign-out. Incoming
notifications prepend to the list and raise a toast for order-status types; the
`salesDashboardUpdated` broadcast increments a tick that the dashboard observes for a coalesced
refresh. The notification bell renders the unread count and a reconnecting indicator.

`SupportChatStore` opens `/hubs/support` lazily on the first widget open. It resumes an active
session on reopen, appends messages only on the server echo (no optimistic insert), and reports
typing as an immediate `true` followed by a debounced trailing `false`. The agent console
(`/support/console`, role SupportAgent) reuses the same `ChatThreadComponent` over a multi
session store. The order-confirmation page consumes notifications for live status updates and
keeps a bounded polling fallback for the unauthenticated-hub case.

All hub method and event names are pinned in dedicated constant files so they match the server
contract exactly.
