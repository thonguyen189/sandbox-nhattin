# NhatTin SDK — Resilience upgrade design (proactive token refresh + transient retry)

Ngày: 2026-07-07. Nhánh: `feat/nhattin-sdk`. Phạm vi: `CodeSDK/src/NhatTinLogistics.Sdk`.

Nguồn quyết định: đề xuất nâng cấp sau đợt live-verify SDK 2026-07-07
([../../../Tests/Results/EVIDENCE-SDK-LIVE-2026-07-07.md](../../../Tests/Results/EVIDENCE-SDK-LIVE-2026-07-07.md)),
đối chiếu [08-DANH-GIA-NANG-CAP.md](../../PhanTich-ThaoTac-DongBo/08-DANH-GIA-NANG-CAP.md).

## Mục tiêu

Hai nâng cấp "giá trị cao / rủi ro thật" cho SDK, không đổi hành vi nghiệp vụ:

- **#1 — Refresh token chủ động (proactive):** hiện SDK chỉ refresh khi dính HTTP 401
  ([NhatTinHttpClient.cs](../../../CodeSDK/src/NhatTinLogistics.Sdk/Http/NhatTinHttpClient.cs)),
  nên request đầu tiên sau khi access token hết hạn (24h) luôn tốn một vòng 401 thừa. SDK đã đọc
  `token_expires_in="24h"` / `refresh_expires_in="7d"` nhưng không dùng. Ta lưu hạn và refresh sớm trước hạn.
- **#3 — Retry + backoff cho lệnh idempotent:** hiện một blip mạng / 5xx / 429 là fail luôn ngay quầy
  thu ngân. Thêm retry tự viết (không thêm dependency) chỉ cho lệnh idempotent.

## Ngoài phạm vi (đã chốt với chủ dự án)

- **#2 chống trùng bill KHÔNG làm trong SDK.** Tài liệu API (dòng 1123 trong
  [NhatTin-API-Documentation-VI.md](../../../NhatTinAPIDocumentation/NhatTin-API-Documentation-VI.md))
  xác nhận NhatTin **không có idempotency key** và **"POS tự xử lý trùng lặp"**; không có endpoint tra
  bill theo `ref_code`. Dedupe do hệ thống POS đảm nhận. Phần duy nhất giữ lại: **lệnh ghi không bao giờ
  auto-retry** (thuộc phân loại của #3).
- Không dùng Polly. Không đổi mô hình response. Không làm persistent token store (để lần sau).

## Feature A — Proactive token refresh

### A1. `TokenTtl.Parse`
Helper `internal static class TokenTtl { public static TimeSpan? Parse(string? ttl); }`.
- Chấp nhận: `"24h"`, `"7d"`, `"3600s"`, `"900s"`, `"30m"`, và số thuần (hiểu là giây).
- Không parse được / null / rỗng → `null` (⇒ proactive tự tắt cho token đó, vẫn còn 401-fallback).

### A2. `ITokenStore` mở rộng hạn (breaking nhẹ, SDK còn pre-1.0)
```csharp
DateTimeOffset? AccessTokenExpiresAt { get; }
DateTimeOffset? RefreshTokenExpiresAt { get; }
void SetTokens(string accessToken, string refreshToken,
               DateTimeOffset? accessExpiresAt = null,
               DateTimeOffset? refreshExpiresAt = null);
```
`InMemoryTokenStore` lưu thêm 2 mốc; `Clear()` xoá cả 2. Call-site `SetTokens(a,b)` cũ vẫn biên dịch
nhờ tham số mặc định `null`.

### A3. Refresh chủ động trong `NhatTinHttpClient`
- Thêm ctor param **optional** `Func<DateTimeOffset>? clock = null` (default `() => DateTimeOffset.UtcNow`).
  Ctor của `NhatTinLogisticsClient` (standalone/DI) **không đổi**.
- Khi sign-in/refresh nội bộ lưu token: tính `ExpiresAt = clock() + TokenTtl.Parse(ttl)` (nếu parse được).
- `EnsureAuthenticatedAsync` đổi điều kiện "token còn tốt":
  có access **và** (`AccessTokenExpiresAt == null` **hoặc** `clock() < AccessTokenExpiresAt − skew`).
  Nếu không tốt → vào single-flight (`_authLock` sẵn có): còn refresh hợp lệ (chưa quá hạn) thì refresh,
  không thì sign-in lại. Nhánh 401 reactive giữ nguyên làm lưới an toàn.

## Feature B — Transient retry (idempotent-only)

### B1. Phân loại (theo method + path, allowlist tường minh)
- **Retry được:** mọi `GET`; `POST /v3/bill/calc-fee`; `POST /v1/auth/sign-in`; `POST /v1/auth/refresh-token`.
- **KHÔNG retry:** `POST` create / update-shipping / destroy / revert-bill (lệnh ghi, non-idempotent).

### B2. Điều kiện retry
- Lỗi transport (`NhatTinApiException` HttpStatusCode == 0).
- Timeout (`OperationCanceledException` khi `ct` **không** bị hủy — phân biệt với người dùng hủy).
- HTTP **5xx / 429 / 408**.
- **KHÔNG** retry business-error (HTTP 200 + `success:false`) — giữ đúng hành vi hiện tại.

### B3. Backoff
Exponential + jitter. Default `MaxRetries=3`, base `200ms` (~200/400/800ms ± jitter), cap `5s`.
Ctor thêm param **optional** `Func<TimeSpan, CancellationToken, Task>? delay = null` (default `Task.Delay`);
test truyền no-op để chạy tức thì.

### B4. Gộp chung
Tách helper `ExecuteWithRetryAsync(Func<Task<HttpResponseMessage>> sendUnit, bool idempotent, ct)`
dùng cho **cả** `SendAsync<T>` và `GetPrintAsync` (print là GET → idempotent). Nhánh
"401 → refresh → retry-once" hiện có nằm *trong* mỗi lần gửi, **không** tính vào số transient-retry.

## Options mới (default an toàn, không cần đổi call-site)

```csharp
bool     EnableProactiveRefresh { get; set; } = true;
TimeSpan TokenExpirySkew        { get; set; } = TimeSpan.FromSeconds(60);
bool     EnableRetry            { get; set; } = true;
int      MaxRetries             { get; set; } = 3;
TimeSpan RetryBaseDelay         { get; set; } = TimeSpan.FromMilliseconds(200);
TimeSpan RetryMaxDelay          { get; set; } = TimeSpan.FromSeconds(5);
```

## Error handling & tương thích

- Hết số lần retry → đưa lỗi ra y như hành vi hiện tại (không nuốt lỗi).
- TTL không parse được → proactive tự tắt cho token đó (fallback 401). An toàn khi NhatTin đổi format.
- **Breaking thật sự duy nhất:** interface `ITokenStore` (impl bên ngoài phải thêm 2 property + đổi chữ
  ký `SetTokens`). Ghi CHANGELOG, bump `0.2.0 → 0.3.0`.

## Kế hoạch test (TDD — thêm vào 42 test hiện có)

- `TokenTtl.Parse`: `"24h"/"7d"/"3600s"/"900s"/"30m"/"120"/rác/null` → giá trị/null tương ứng.
- `InMemoryTokenStore`: set kèm expiry đọc lại đúng; `Clear` xoá expiry.
- Proactive: clock giả vượt `ExpiresAt − skew` ⇒ call kế tiếp tự refresh **không** cần 401 (đếm request qua stub).
- Proactive tắt khi TTL null (không refresh sớm).
- Retry: `500→500→200` với GET ⇒ thành công sau 3 lần; `create` gặp 500 ⇒ **không** retry (đúng 1 request);
  429 có retry; business `success:false` **không** retry; hết retry ⇒ lỗi propagate. Dùng delay no-op.
- Regression: 4 test 401/concurrent hiện có phải giữ xanh.

## Bàn giao

Sau khi hoàn tất: cập nhật `CHANGELOG.md`, bump version, và tạo `CodeSDK/README.md` (handoff) mô tả kiến
trúc, cấu hình options mới, cách chạy test + live-smoke.
