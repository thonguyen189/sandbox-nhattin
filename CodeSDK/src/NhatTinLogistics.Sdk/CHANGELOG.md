# Changelog

## 0.2.0

- **Breaking:** `IBillApi.PrintAsync` / `BillApi.PrintAsync` now return `PrintResult` instead of `byte[]`. Live verification showed print returns a `{success,message,data}` JSON envelope (HTTP 200 + `success:false` on error, with `[ERR-xxxxx]` codes); a successful label may be HTML. `PrintResult` exposes `Success`, `IsJson`/`IsHtml`, `ContentType`, `Content`, `AsText()`, `Message`, `ErrorCode`, and refreshes the token on 401 like the other calls.
- `partner_id` is now auto-captured from the sign-in response (`data.partner_id`) into `Options.PartnerId` when not set explicitly.

## 0.1.0

- Initial release.
- JWT auth with lazy sign-in and refresh-on-401.
- Bill API: create, update-shipping, destroy (cancel), calc-fee, revert-bill, tracking, print URL.
- Location API: provinces, districts, wards.
- Typed webhook payload parser (`NhatTinWebhookParser`).
- Standalone and DI (`AddNhatTinLogisticsClient`) usage.
