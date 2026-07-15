# Frontend Technical Specification

## AI-Powered Predictive Shopping List App — .NET MAUI Client

| | |
|---|---|
| **Document Version** | 1.3 |
| **Date** | July 10, 2026 (v1.3 — updated assuming Backend API Change Request v1.0 is fully implemented) |
| **Status** | Draft |
| **Prepared For** | Mobile Engineering |
| **Source Documents** | Business Requirements Document (BRD) v1.0, June 28, 2026; CartSmart.Api OpenAPI spec v1.0.0; Backend API Change Request v1.0 (assumed implemented — see Section 6 note) |
| **Platforms** | iOS, Android (via .NET MAUI) |

---

## 1. Purpose & Scope

This document translates the BRD's functional and non-functional requirements into a technical specification for the mobile client, to be built with **.NET MAUI** and to run on top of a backend API.

It covers app architecture, screen-level requirements, the local (on-device) data and prediction layer, the API integration surface, and offline/sync behavior. It does not redefine business objectives or scope decisions — those remain governed by the BRD. Where this spec makes a technical assumption not stated in the BRD, it is called out explicitly in Section 11.

**Key architectural carry-over from the BRD:** the prediction engine (FR-2.x) is on-device only in Phase 1. The API is used for account/auth and cross-device list sync — **not** for prediction. This is a hard constraint (NFR-2, NFR-3, FR-5.3), not a client-side implementation choice, and it shapes the layering in Section 4.

## 2. Technology Stack

| Layer | Choice | Notes |
|---|---|---|
| Framework | .NET MAUI (.NET 10) | Single codebase targeting iOS and Android per BRD Platforms requirement |
| UI pattern | MVVM | `CommunityToolkit.Mvvm` for `ObservableObject`, `RelayCommand`, source generators |
| Local storage | SQLite (`sqlite-net-pcl` or EF Core with Sqlite provider) | Stores purchase history, list items, model state (Section 5) |
| API client | `HttpClient` + `Refit`, one typed interface per API tag (`IAuthApi`, `IDeviceApi`, `IListApi`, `ISyncApi`, `IAccountApi`, `IReferenceApi`) | Mirrors the `CartSmart.Api` OpenAPI tags exactly — see Section 6. A shared `Refit` error handler parses the `{ code, message }` error envelope (now implemented per Backend API Change Request Section 1.2) into typed client exceptions |
| Local notifications | `Plugin.LocalNotification` or MAUI Essentials-based scheduler | Delivers FR-3.1 "due soon" alerts entirely on-device |
| On-device ML/stats | Custom C# statistical layer (rolling average, exponential smoothing) | Per BRD Section 9 assumption: no heavy ML/deep learning needed; avoids a hard dependency on Core ML/TFLite bindings for Phase 1 |
| Secure token storage | `SecureStorage` (MAUI Essentials) | Auth tokens only — never purchase data (aligns with FR-5.3 / Data Requirements table) |
| Dependency injection | `Microsoft.Extensions.DependencyInjection` via `MauiProgram` | Standard MAUI host builder pattern |

## 3. App Architecture Overview

```
[Views (XAML)]
      ↕ data binding
[ViewModels] ──uses──> [Domain Services]
                              ├─ ListService          (FR-1.x) ──> [Local SQLite: list_items]
                              ├─ PredictionService     (FR-2.x) ──> [Local SQLite: purchase_events, model_state]
                              ├─ NotificationService   (FR-3.x)
                              ├─ ShoppingModeService   (FR-4.x)
                              └─ SyncService (facade)  (FR-5.x)
                                     ├─ IAuthApi        (register, login, google, apple, refresh, logout)
                                     ├─ IDeviceApi      (register/list/remove device)
                                     ├─ IListApi        (upsert/delete list, upsert/delete item)
                                     ├─ ISyncApi        (GET /sync?since= — pull changes)
                                     ├─ IAccountApi     (GET account, DELETE account, GET export)
                                     └─ IReferenceApi   (GET reference/version, GET reference/products)
                                              ↕
                                        Backend API (CartSmart.Api) ↔ PostgreSQL
```

- **ViewModels** never call any `I*Api` interface directly — everything goes through `SyncService`, which owns retry/backoff, auth-token attachment, and offline queueing. This keeps the "no purchase data leaves the device" rule enforceable in exactly one place, even though the API itself is now split across six typed clients rather than one.
- **PredictionService** has no reference to any `I*Api` interface in Phase 1. Per BRD Section 7.1, it must expose a pluggable interface (`ISuggestionSignalProvider` or similar) so a future server-signal provider (Phase 2, Option A) can be injected later without touching Phase 1 code.
- **SyncService** is the only component allowed to call the backend API, and only for: authentication, device registration, list/item CRUD + pull-sync, account view/delete/export, and reference data — never purchase history, never model state. This is unchanged from the original intent; only the internal decomposition into six Refit clients is new, driven by the real API's tag structure.

## 4. Data Layer Mapping (Client-Side)

| Data | Local Table/Store | Synced via API? | Requirement Reference |
|---|---|---|---|
| Shopping lists (name) | `lists` (SQLite) | Yes — `PUT/DELETE /api/v1/lists/{listId}` | FR-1.4, FR-5.2 |
| Shopping list items (name, quantity, unit, category, isChecked) | `list_items` (SQLite) | Yes — `PUT/DELETE /api/v1/lists/{listId}/items/{itemId}` | FR-1.1–1.3, FR-5.2 |
| Purchase history (item, date, qty) | `purchase_events` (SQLite) | **No** | FR-2.1, NFR-2 |
| Prediction model state (intervals, weights) | `model_state` (SQLite) | **No** (must not be *required* to sync — FR-5.3) | FR-2.2, FR-5.3, NFR-6 |
| Registered devices (this device's `clientDeviceId`, platform, display name) | `device_registration` (SQLite, single row) | Yes — `POST/GET/DELETE /api/v1/devices` | FR-5.2 (new — not previously specified, added after schema review) |
| User account/profile | Not stored raw locally beyond session | Yes — `GET/DELETE /api/v1/account`, `GET /api/v1/account/export` | FR-5.1 |
| Auth tokens (access + refresh) | `SecureStorage` | N/A (received from API) | FR-5.1 |
| Product/category reference list | `reference_products` (SQLite), cached with a `reference_version` marker | Yes — `GET /api/v1/reference/version`, `GET /api/v1/reference/products` | BRD Data Requirements — **revised**: see note below |

**Correction from the previous draft:** the reference list is not purely bundled with app releases. The API exposes `GET /reference/version` and `GET /reference/products`, so the intended design is a cache-and-check pattern — ship a seed copy for first-run/offline use, then check `reference/version` on each launch (when online) and refresh `reference/products` if it's changed. This still keeps autocomplete working offline; it just means the reference data can update independently of app store releases.

`lists` and `list_items` should each carry client-only bookkeeping columns not present server-side: `is_dirty` (pending push) and `client_device_id` (which device made the last local edit) — needed for the offline queue in Section 7. They should also store `server_updated_at`, populated from the `updatedAt` field the API now returns on `PUT` responses and on `GET /sync` (per Backend API Change Request Section 2, now implemented) — this is what makes real conflict *detection* possible, distinct from the client-only `is_dirty` flag. All local tables should also include a `schema_version` column per BRD Section 7.1 ("data schema... should be versioned from the start").

**Assumption carried over from the change request's open questions:** the backend chose a timestamp (`updatedAt`) rather than a monotonic version integer for conflict detection. Either works for the client, but this spec assumes timestamp since it's consistent with the rest of the API's date-time conventions (e.g. the `since` parameter on `/sync`). Confirm this against the actual implementation once available.

## 5. Screens & UI Requirements

### 5.1 Onboarding & Account

| ID | Requirement | Priority | Notes |
|---|---|---|---|
| FE-0.1 | Sign-in/sign-up screen supporting email, Apple, and Google sign-in | Must | Maps to FR-5.1; use platform-native auth SDKs where available |
| FE-0.2 | First-run explainer screen stating data stays on-device (privacy positioning) | Should | Supports BO-3; mitigates Risk "users assume cross-user smart features" (BRD Section 11) |
| FE-0.3 | Cold-start messaging component, reusable across list and suggestion views | Must | Maps to FR-2.7; must clearly indicate insufficient history state, not just hide the suggestion |
| FE-0.4 | "Forgot password" flow: request-reset screen (`POST /auth/password/forgot`) and a reset-completion screen (`POST /auth/password/reset`) | Must | New — enabled by Backend API Change Request Section 3, now implemented. Only shown for email/password accounts; hidden for Apple/Google sign-in, using `authProvider` from `GET /account` |

### 5.2 List Management

| ID | Requirement | Priority | Notes |
|---|---|---|---|
| FE-1.1 | List view with add/edit/delete, swipe-to-check-off | Must | FR-1.1, FR-1.3 |
| FE-1.2 | Item detail sheet: quantity, unit, category | Must | FR-1.2 |
| FE-1.3 | Multi-list picker/tab UI | Should | FR-1.4 |
| FE-1.4 | Autocomplete text field bound to local reference list | Should | FR-1.5 |
| FE-1.5 | Voice input button on add-item field | Could | FR-1.6; platform speech-to-text API |
| FE-1.6 | Barcode scan button + camera scan view | Could | FR-1.7 |

### 5.3 Suggestions & Prediction Feedback

| ID | Requirement | Priority | Notes |
|---|---|---|---|
| FE-2.1 | "Suggested items" section on list screen with accept/reject/snooze actions | Must | FR-2.4; actions must call `PredictionService` synchronously, not the API |
| FE-2.2 | Visual indicator (badge/label) for "due soon" vs. "cold-start / not enough data" items | Must | FR-2.3, FR-2.7 |
| FE-2.3 | "Vacation/pause" date range picker in settings | Should | FR-2.6 |

### 5.4 Notifications

| ID | Requirement | Priority | Notes |
|---|---|---|---|
| FE-3.1 | Local push notification for "running low" items | Must | FR-3.1; scheduled on-device, no server push needed |
| FE-3.2 | Notification settings screen: frequency, quiet hours | Should | FR-3.2 |

### 5.5 Shopping Mode

| ID | Requirement | Priority | Notes |
|---|---|---|---|
| FE-4.1 | Large-tap, high-contrast checklist view, toggled from main list | Should | FR-4.1 |
| FE-4.2 | Category/aisle sort toggle within Shopping Mode | Could | FR-4.2 |
| FE-4.3 | Shopping Mode must render and function with `SyncService` fully offline (no network calls triggered) | Must | FR-4.3, NFR-3 |

### 5.6 Settings & Sync

| ID | Requirement | Priority | Notes |
|---|---|---|---|
| FE-5.1 | Account settings screen (sign out, linked sign-in provider) | Must | FR-5.1 |
| FE-5.2 | Sync status indicator (last synced, sync now, error state) | Should | FR-5.2 |
| FE-5.3 | Explicit UI copy/tooltip confirming prediction data is not synced | Could | Reinforces FR-5.3 and BO-3 |
| FE-5.4 | "Manage devices" screen: list registered devices (from `GET /devices`), remove a device (`DELETE /devices/{deviceId}`) | Should | New — required by the Device endpoints; supports FR-5.2 multi-device sync and lets a user revoke a lost/old device |
| FE-5.5 | "Export my data" action in settings, calling `GET /account/export` and sharing/saving the result via the platform share sheet | Should | New — required by NFR-7 (GDPR/CCPA data portability); not explicit in BRD text but implied by compliance requirement and now confirmed by the API |

## 6. API Integration Surface

Endpoints below are taken directly from the `CartSmart.Api` OpenAPI spec v1.0.0 (base path `/api/v1`), **updated to assume Backend API Change Request v1.0 is fully implemented**: response schemas, error schemas, `updatedAt` fields on lists/items, the `GET /sync` response shape, and the password-reset endpoints. The client now has 22 endpoints available (up from 19), across 6 tags. Anywhere this spec had to guess at a backend decision left open in the change request (e.g., timestamp vs. version-int), the assumption made is called out inline and in Section 11.

### 6.1 `IAuthApi` (9 endpoints)

| Endpoint | Request DTO | Response DTO | Client Method |
|---|---|---|---|
| `POST /auth/register` | `RegisterRequest { email, password }` | `{ accessToken, refreshToken, expiresIn, userId }` | `SyncService.RegisterAsync` |
| `POST /auth/login` | `LoginRequest { email, password }` | `{ accessToken, refreshToken, expiresIn, userId }` | `SyncService.LoginAsync` |
| `POST /auth/google` | `ExternalLoginRequest { idToken }` | `{ accessToken, refreshToken, expiresIn, userId }` | `SyncService.LoginWithGoogleAsync` |
| `POST /auth/apple` | `ExternalLoginRequest { idToken }` | `{ accessToken, refreshToken, expiresIn, userId }` | `SyncService.LoginWithAppleAsync` |
| `POST /auth/refresh` | `RefreshRequest { refreshToken }` | `{ accessToken, refreshToken, expiresIn }` | Called transparently by a `Refit`/`HttpClient` auth-delegating handler on 401 |
| `POST /auth/logout` | `LogoutRequest { refreshToken }` | `204 No Content` | `SyncService.LogoutAsync` — must also clear `SecureStorage` and local session state |
| `POST /auth/password/forgot` *(new)* | `{ email }` | `204 No Content` (returned regardless of whether the email exists, per change request's anti-enumeration note) | `SyncService.RequestPasswordResetAsync` — powers FE-0.4 |
| `POST /auth/password/reset` *(new)* | `{ resetToken, newPassword }` | `204 No Content` | `SyncService.CompletePasswordResetAsync` — powers FE-0.4 |
| `POST /auth/password/change` *(new, optional per change request)* | `{ currentPassword, newPassword }` | `204 No Content` | `SyncService.ChangePasswordAsync` — logged-in flow, not currently mapped to a screen in Section 5; add if Product wants proactive password change in settings |

### 6.2 `IDeviceApi` (3 endpoints)

| Endpoint | Request DTO | Response DTO | Client Method |
|---|---|---|---|
| `POST /devices` | `RegisterDeviceRequest { clientDeviceId, platform, displayName }` | `{ deviceId, clientDeviceId, platform, displayName, registeredAt }` | `SyncService.RegisterDeviceAsync` — called once per install, right after first successful login |
| `GET /devices` | — | `[{ deviceId, clientDeviceId, platform, displayName, lastSyncAt }]` | `SyncService.GetDevicesAsync` — powers FE-5.4 |
| `DELETE /devices/{deviceId}` | — (path param) | `204 No Content` | `SyncService.RemoveDeviceAsync` — powers FE-5.4 |

`clientDeviceId` should be a stable, locally-generated GUID persisted in `SecureStorage` (not the OS device identifier, which can change), and `platform` should be `"iOS"` or `"Android"`.

### 6.3 `IListApi` (4 endpoints)

| Endpoint | Request DTO | Response DTO | Client Method |
|---|---|---|---|
| `PUT /lists/{listId}` | `UpsertListRequest { name }` | `{ listId, name, updatedAt }` | `ListService` → `SyncService.UpsertListAsync` (create and rename both use this) |
| `DELETE /lists/{listId}` | — | `204 No Content` | `SyncService.DeleteListAsync` |
| `PUT /lists/{listId}/items/{itemId}` | `UpsertListItemRequest { name, quantity, unit, category, isChecked }` | `{ itemId, listId, name, quantity, unit, category, isChecked, updatedAt }` | `ListService` → `SyncService.UpsertItemAsync` (create, edit, and check-off all use this) |
| `DELETE /lists/{listId}/items/{itemId}` | — | `204 No Content` | `SyncService.DeleteItemAsync` |

The `updatedAt` now returned on every upsert (per Backend API Change Request Section 1.1/2) is written straight into the local `server_updated_at` column (Section 4) — this is what lets the client detect, after the fact, whether a write it just made was immediately stale relative to another device.

Note: there is still **no bulk push endpoint**. Each list/item change is an individual `PUT`/`DELETE`. The offline queue (Section 7) needs to replay these one at a time in order, not as a single batch payload.

### 6.4 `ISyncApi` (1 endpoint)

| Endpoint | Params | Response DTO | Client Method |
|---|---|---|---|
| `GET /sync?since={timestamp}` | `since`: ISO 8601 date-time | See below | `SyncService.PullChangesAsync` — the only pull mechanism; drives incremental refresh after reconnect or on a timer |

Response shape (per Backend API Change Request Section 2, now implemented):

```json
{
  "serverTimestamp": "2026-07-10T12:00:00Z",
  "lists": [ { "listId": "...", "name": "...", "updatedAt": "..." } ],
  "items": [ { "itemId": "...", "listId": "...", "name": "...", "quantity": 2, "unit": "...", "category": "...", "isChecked": false, "updatedAt": "..." } ],
  "deletedListIds": ["..."],
  "deletedItemIds": ["..."]
}
```

Two behavior changes this drives on the client, both replacing earlier assumptions:

- **`serverTimestamp` is now the client's `since` cursor for the next call** — not the device's own clock. `SyncService` must persist this value locally and pass it back verbatim, rather than using `DateTime.UtcNow`. This removes the clock-drift risk noted in the original open question.
- **Deletes are now explicit** (`deletedListIds`/`deletedItemIds`), so the client applies additions/updates and deletions from a single response rather than inferring deletion from absence.

**Not yet confirmed:** pagination. The change request flagged this as an open question for the backend team; this spec assumes an unbounded response for now (acceptable at Phase 1/MVP data volumes) and flags it as a forward-looking risk if account history grows large — revisit before Phase 2 if so.

### 6.5 `IAccountApi` (3 endpoints)

| Endpoint | Response DTO | Client Method |
|---|---|---|
| `GET /account` | `{ id, email, authProvider, createdAt }` | `SyncService.GetAccountAsync` — `authProvider` now drives FE-0.4 (hide "forgot password" for Apple/Google accounts) |
| `DELETE /account` | `204 No Content` | `SyncService.DeleteAccountAsync` |
| `GET /account/export` | Assumed inline JSON body (change request left export format as an open question; this spec assumes the simpler option — confirm before implementation, especially for large accounts) | `SyncService.ExportAccountDataAsync` |

### 6.6 `IReferenceApi` (2 endpoints)

| Endpoint | Response DTO | Client Method |
|---|---|---|
| `GET /reference/version` | `{ version }` | `SyncService.GetReferenceVersionAsync` — compare to locally cached `reference_version` |
| `GET /reference/products` | `[{ productId, name, category, defaultUnit }]` | `SyncService.GetReferenceProductsAsync` — only called when version check indicates a change |

### 6.7 Error Handling

All endpoints now return a consistent error envelope on failure (per Backend API Change Request Section 1.2):

```json
{ "code": "INVALID_CREDENTIALS", "message": "Email or password is incorrect." }
```

`SyncService` should map this into a small set of typed client exceptions (e.g., `AuthenticationException`, `ConflictException`, `NotFoundException`, `ValidationException`) based on HTTP status + `code`, so ViewModels catch specific exception types rather than parsing strings. This replaces the earlier per-endpoint guesswork the client would otherwise have needed for 400/401/404/409/422 responses.

**Explicitly out of scope for this API client in Phase 1:** the schema still has no endpoints for purchase history upload, prediction model upload/download, or cold-start priors — confirming the BRD's on-device-only boundary is reflected on the backend too. Nothing in Section 3's `SyncService` should ever be extended to call such an endpoint in Phase 1, even speculatively; that hook is reserved for the Phase 2 `ISuggestionSignalProvider`.

## 7. Offline & Sync Behavior

| ID | Requirement | Priority | Notes |
|---|---|---|---|
| FE-6.1 | All screens except sign-in and explicit "sync now" must render and remain interactive with no network connection | Must | NFR-3 |
| FE-6.2 | List/item changes made offline are queued locally (in `is_dirty` rows) and replayed as individual `PUT`/`DELETE` calls, in order, on next connectivity. Pull-sync uses the server-issued `serverTimestamp` (Section 6.4) as the `since` cursor, not the device clock | Must | Supports FR-5.2 without blocking core usage; revised now that the real `/sync` response is known |
| FE-6.3 | Conflict resolution: `PUT` is still a full-object upsert with no field-level merge — the last successful `PUT` wins for that item as a whole. But the client can now **detect** this: compare the `updatedAt` returned by a `PUT` (or seen in a subsequent `/sync` pull) against the local `server_updated_at` recorded before the edit; if the server's value is newer than what the client last saw, surface "updated on another device" instead of assuming the write succeeded cleanly | Should | Upgraded from "silent last-write-wins" to "detected last-write-wins" now that `updatedAt` exists (Backend API Change Request Section 2) |
| FE-6.4 | Sync failures must fail silently to a status indicator (FE-5.2), never block list or suggestion interaction. Failures now parse the `{ code, message }` error envelope (Section 6.7) into a typed exception before updating the indicator | Must | Keeps predictive features fully decoupled from network state |
| FE-6.5 | On first login after install, client must call `POST /devices` before any list sync, so subsequent `PUT`/`GET /sync` calls are attributable to a registered device | Must | Required by the Device endpoints |

## 8. Non-Functional Requirements (Client-Specific)

| ID | Category | Requirement | Source |
|---|---|---|---|
| FE-NFR-1 | Performance | Prediction computation on the UI thread must not block rendering; run on a background thread/task and marshal results back | NFR-1 |
| FE-NFR-2 | Platform support | Minimum OS versions: current and prior major iOS and Android releases at time of launch, per .NET MAUI's supported target framework moniker (TFM) for each release | NFR-4 |
| FE-NFR-3 | Data durability | SQLite writes for purchase events and model state must be transactional; app must recover cleanly from an interrupted write | NFR-5 |
| FE-NFR-4 | Accessibility | All screens must support platform accessibility (Dynamic Type/font scaling, screen reader labels) — not explicit in BRD but standard for iOS/Android store guidelines | Assumption |
| FE-NFR-5 | Extensibility | `PredictionService` and `SyncService` must be separately mockable/injectable to keep the pluggable suggestion-ranker boundary enforceable in code review | NFR-6 |

## 9. Out of Scope for This Client Spec

Per BRD Section 3.2, the following remain out of scope for Phase 1 and are not addressed by this frontend spec: receipt OCR/scanning, grocery delivery integrations, voice assistant integrations (Siri/Google Assistant — distinct from the in-app voice *input* in FE-1.5), smartwatch companion apps, and analytics dashboards.

## 10. Phase 2 Readiness

This section assesses, honestly, how much Phase 1 actually de-risks Phase 2 rather than just gesturing at it — per BRD Section 7.2 and the Risk in Section 11 ("architecture isn't actually extensible when Phase 2 begins").

### 10.1 Already in place

| Item | Why it helps Phase 2 |
|---|---|
| `ISuggestionSignalProvider` interface boundary on `PredictionService` | Phase 2 Option A (server cold-start priors) can be added as a second implementation, without rewriting the ranking logic itself |
| `schema_version` column on all local tables | New Phase 2 local tables (e.g., cached server priors) are a migration, not a redesign |
| Full account/device/list/sync infrastructure (Sections 6.1–6.5) | None of this needs to change for Phase 2 — Option A only touches the prediction path, not auth, sync, or devices |
| Cold-start UI (FE-0.3, FR-2.7) | Phase 2 priors change *what fills* the cold-start state, not the UI concept itself |

### 10.2 Gaps — not actually ready yet

| Gap | Detail | Action needed |
|---|---|---|
| **No backend endpoint exists for Phase 2** | The `CartSmart.Api` v1 OpenAPI spec (Section 6) has no endpoint for server-supplied priors or any cross-user aggregate data. `reference/products` is category metadata, not prediction priors | A new backend endpoint must be designed and built before the client-side hook has anything to call — this is a backend workstream, not just a client one |
| **Interface discipline is a process risk, not a technical one** | The `ISuggestionSignalProvider` boundary existing in the spec doesn't guarantee `PredictionService` is actually implemented against it rather than hardcoded inline | Enforce in code review from day one (FE-NFR-5); flagged in BRD Section 11 as a named risk, not a hypothetical |
| **Migration path is unproven** | A `schema_version` column is necessary but not sufficient — no real schema migration will have been exercised until Phase 2 forces one | Run a trivial "dry-run" migration during Phase 1 (even a no-op version bump) so the mechanism is proven before it's load-bearing |

### 10.3 Recommended acceptance test before calling Phase 1 "done"

Before Phase 1 sign-off, build a second, throwaway `ISuggestionSignalProvider` implementation (it can return a hardcoded value) and confirm it can be swapped in via DI without touching `PredictionService`'s internals. If that swap is awkward, the interface boundary is in the wrong place, and it's better to find that out now than mid-Phase-2.

**Bottom line:** Phase 1's *design* is Phase-2-ready. Phase 1's *implementation* will only be Phase-2-ready if the interface discipline above is actually enforced and the acceptance test in 10.3 passes — neither is automatic just because this spec calls for them.

## 11. Open Questions / Assumptions Requiring Confirmation

### 11.1 Resolved

1. ~~API contract~~ — **Resolved.** Section 6 reflects the real `CartSmart.Api` v1 OpenAPI spec.
2. ~~Cross-platform ML approach~~ — **Resolved: custom C# statistical layer** (rolling average, exponential smoothing), not Core ML/TFLite. BRD Section 9 confirms Phase 1 needs no deep learning, so there's no technical justification for per-platform ML bridging complexity. No further sign-off needed — this is a client architecture decision within scope of this spec.
3. ~~Push notification delivery~~ — **Resolved: fully local/on-device scheduling.** The API has no push-infrastructure endpoints at all (no device push token registration, no APNs/FCM relay), so server-triggered push isn't available even as an option in Phase 1 — this isn't a preference, it's the only mechanism the backend supports today.
4. ~~Account update/profile editing~~ — **Resolved: confirmed out of scope for Phase 1.** The API exposes no `PATCH`/`PUT /account`; this is an intentional API-side omission, not a gap in this spec.
5. ~~Response schemas were undefined~~ — **Resolved, assuming Backend API Change Request v1.0 is implemented.** Section 6 now documents response and error DTOs for every endpoint.
6. ~~`GET /sync` response shape was unknown~~ — **Resolved, assuming Backend API Change Request v1.0 is implemented.** Section 6.4 documents the full shape, including the `serverTimestamp` cursor and explicit delete lists.
7. ~~No password-reset endpoint existed~~ — **Resolved, assuming Backend API Change Request v1.0 is implemented.** `POST /auth/password/forgot` and `/reset` are in Section 6.1, with FE-0.4 covering the UI.

### 11.2 Proposed default — needs Product Owner sign-off, not a technical blocker

8. **Reference data caching policy:** proposed default — check `GET /reference/version` once per app launch when online, capped at once per 24 hours, plus a manual "refresh" action in settings. Reasonable default balancing freshness against battery/data use; needs PO confirmation rather than further engineering.

### 11.3 Residual assumptions — this spec picked a default where the change request left the backend's actual choice open; confirm once implementation is inspected

9. **Conflict field type:** this spec assumes the backend implemented `updatedAt` as an ISO 8601 timestamp rather than a monotonic version integer (Section 4). Functionally equivalent for the client's purposes (Section 7, FE-6.3), but the exact field name/type needs to match whatever the backend actually shipped.
10. **`GET /account/export` format:** this spec assumes an inline JSON response body (Section 6.5). If the backend instead returns a signed download URL (the other option the change request raised, likely for large accounts), FE-5.5's implementation needs a download/polling step rather than a direct parse.
11. **`/sync` pagination:** this spec assumes no pagination is implemented yet, i.e. `GET /sync` returns the full change set in one response (Section 6.4). Fine at Phase 1 data volumes; revisit if the backend adds a continuation token before Phase 2.
12. **`POST /auth/password/change` (proactive, logged-in password change):** marked optional in the change request; this spec lists the endpoint (Section 6.1) but doesn't currently wire it to a settings screen. Add a UI entry point if Product wants it in Phase 1, or confirm it's deferred.

---

*End of Document*
