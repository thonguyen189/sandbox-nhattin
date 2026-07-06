# NhatTin Logistics Sandbox

Emulator (`CodeSandBox`) + webhook receiver (`CodeWebHooks`) for the Nhất Tín Logistics partner API. All behavior traces to `NhatTinAPIDocumentation/vi/`. See `docs/superpowers/specs/2026-07-06-nhattin-sandbox-webhook-design.md` for design and known-unconfirmed items.

## Prerequisites
- .NET 8 runtime/SDK (builds with installed SDK 9.0.301)
- SQLite (bundled via EF Core provider; no server needed)

## Run

Terminal A — webhook receiver (start first so the sandbox can reach it):
```
dotnet run --project CodeWebHooks/src/NhatTinWebhookReceiver.Api
```
Receiver: http://localhost:5099  (log viewer at `/`, endpoint at `/webhooks/nhattin/status`)

Terminal B — sandbox API:
```
dotnet run --project CodeSandBox/src/NhatTinSandbox.Api
```
API + Swagger: http://localhost:5080/swagger

Terminal C — admin portal (optional):
```
dotnet run --project CodeSandBox/src/NhatTinSandbox.AdminPortal
```
Admin: http://localhost:5090

## Demo credentials (sandbox only)
- username: `sandbox`
- password: `sandbox123`

## End-to-end smoke test
With receiver (5099) and sandbox (5080) running:
```
pwsh Tests/run-nhattin-cycle.ps1
```

## Known unconfirmed (approximated) behavior
Token TTL, webhook retry/ACK policy, print response format, and full location/master-data catalogs are approximated. See the design spec section 9.
