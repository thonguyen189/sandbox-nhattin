# Changelog

## 0.3.0

- **Proactive token refresh.** The SDK now refreshes the access token just before it expires (using the
  `token_expires_in` / `refresh_expires_in` TTL from the sign-in/refresh response) instead of only reacting
  to a 401. This removes the wasted 401 round-trip that a long-running client hit on its first call after the
  24h access token lapsed. If the TTL is unknown/unparseable, proactive refresh is skipped and the reactive
  401 path still applies. When the refresh token itself has expired, the SDK does a full sign-in. New options:
  `EnableProactiveRefresh` (default true) and `TokenExpirySkew` (default 60s).
- **Transient-fault retry with backoff.** Idempotent calls — every `GET`, plus `POST /v3/bill/calc-fee`,
  `/v1/auth/sign-in`, `/v1/auth/refresh-token` — are now retried on transport errors, timeouts, and HTTP
  5xx/429/408 with exponential backoff + jitter. **Write calls (create, update-shipping, destroy, revert-bill)
  are never retried**, so a network blip can't duplicate a shipment (NhatTin has no idempotency key; dedupe is
  the POS's responsibility). HTTP 200 business errors (`success:false`) are treated as real answers and not
  retried. New options: `EnableRetry` (default true), `MaxRetries` (default 3), `RetryBaseDelay` (default 200ms),
  `RetryMaxDelay` (default 5s).
- **Breaking:** `ITokenStore` gained `AccessTokenExpiresAt` / `RefreshTokenExpiresAt` and `SetTokens` now takes
  optional `accessExpiresAt` / `refreshExpiresAt` arguments. Existing two-argument `SetTokens(access, refresh)`
  calls still compile; only custom `ITokenStore` implementations must add the two properties and widen the method
  signature. `InMemoryTokenStore` is updated.
- Design spec: `docs/superpowers/specs/2026-07-07-nhattin-sdk-resilience-design.md`.

## 0.2.0

- **Breaking:** `IBillApi.CancelAsync` / `BillApi.CancelAsync` now return `NhatTinResponse<CancelResponse>` instead of `NhatTinResponse<List<CancelResult>>`. Live SDK smoke (2026-07-07) showed `/v3/bill/destroy` returns `data` as an **object** `{ "success": [{doCode,message}], "failed": [] }`, not a bare array; the old shape threw a JSON-parse error. `CancelResponse` exposes `Succeeded` and `Failed` (both `List<CancelResult>`).
- **Breaking:** `FeeOption.ServiceId` is now `int?`. Live calc-fee returns `service_id: null`, which threw on the previous non-nullable `int`.
- Tracking (and all string fields) are now tolerant of the sandbox's inconsistent value types. A new global `TolerantStringConverter` reads a `string` property from a JSON string, a **raw number** (e.g. `"cod_amt":0`, `"main_fee":41936`), a boolean, or null — the sandbox mixes these unpredictably and the strict `string?` mapping previously threw on `/v3/bill/tracking`.
- **Breaking:** `IBillApi.PrintAsync` / `BillApi.PrintAsync` now return `PrintResult` instead of `byte[]`. Live verification showed print returns a `{success,message,data}` JSON envelope (HTTP 200 + `success:false` on error, with `[ERR-xxxxx]` codes); a successful label may be HTML. `PrintResult` exposes `Success`, `IsJson`/`IsHtml`, `ContentType`, `Content`, `AsText()`, `Message`, `ErrorCode`, and refreshes the token on 401 like the other calls.
- `partner_id` is now auto-captured from the sign-in response (`data.partner_id`) into `Options.PartnerId` when not set explicitly.

## 0.1.0

- Initial release.
- JWT auth with lazy sign-in and refresh-on-401.
- Bill API: create, update-shipping, destroy (cancel), calc-fee, revert-bill, tracking, print URL.
- Location API: provinces, districts, wards.
- Typed webhook payload parser (`NhatTinWebhookParser`).
- Standalone and DI (`AddNhatTinLogisticsClient`) usage.
