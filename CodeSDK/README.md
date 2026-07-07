# CodeSDK — NhatTinLogistics.Sdk (bàn giao)

SDK C# **không chính thức** cho **Nhat Tin Logistics (NTL) Open API**, dùng để tích hợp vận chuyển Nhất Tín
vào TruePos (POS). Đây là thư viện **độc lập** (standalone .NET 6 class library), tách khỏi emulator sandbox
(`CodeSandBox`) và webhook receiver (`CodeWebHooks`) trong cùng repo.

- **Phiên bản:** 0.3.0 · **Target:** net6.0 · **Trạng thái:** đã live-verify với sandbox thật (2026-07-07).
- **Tài liệu API + ví dụ dùng chi tiết:** [src/NhatTinLogistics.Sdk/README.md](src/NhatTinLogistics.Sdk/README.md) (cũng là README gói NuGet).
- **Lịch sử thay đổi:** [src/NhatTinLogistics.Sdk/CHANGELOG.md](src/NhatTinLogistics.Sdk/CHANGELOG.md).

Tài liệu này là **điểm vào cho người tiếp nhận**: bố cục, cách build/test, cấu hình, hành vi cốt lõi, cách
chạy kiểm thử thật, và các "hố ga" đã biết của sandbox.

---

## 1. Bố cục

```
CodeSDK/
├─ NhatTinLogisticsSdk.sln
├─ src/NhatTinLogistics.Sdk/            # thư viện chính (đóng gói NuGet được)
│  ├─ Client/       Auth/Bill/Location API (interface + impl)
│  ├─ Http/         NhatTinHttpClient (auth, retry, refresh), ITokenStore, JSON, converters
│  ├─ Types/        Requests / Responses / Enums (map snake_case)
│  ├─ Webhooks/     NhatTinWebhookParser + payload
│  ├─ Extensions/   AddNhatTinLogisticsClient (DI)
│  └─ README.md · CHANGELOG.md
├─ tests/NhatTinLogistics.Sdk.Tests/    # xUnit, stub HttpMessageHandler (không gọi mạng)
└─ samples/NhatTinLogistics.Sdk.LiveSmoke/  # console drive SDK đánh vào sandbox THẬT
```

Điểm vào: `NhatTinLogisticsClient` → `.Auth`, `.Bill`, `.Location`, `.Tokens`.

## 2. Build & test

```powershell
# Build toàn solution
dotnet build CodeSDK/NhatTinLogisticsSdk.sln

# Unit test (offline, dùng stub — KHÔNG chạm mạng). Hiện: 68/68 pass.
dotnet test CodeSDK/tests/NhatTinLogistics.Sdk.Tests/NhatTinLogistics.Sdk.Tests.csproj

# Đóng gói NuGet (kèm README + symbols .snupkg)
dotnet pack CodeSDK/src/NhatTinLogistics.Sdk/NhatTinLogistics.Sdk.csproj -c Release
```

Yêu cầu: .NET SDK 6+ (đã build/test với SDK 9.0.x, target net6.0).

## 3. Dùng nhanh

Chi tiết ở [README gói](src/NhatTinLogistics.Sdk/README.md). Tối thiểu:

```csharp
using var client = new NhatTinLogisticsClient(new NhatTinLogisticsClientOptions
{
    Username    = "your_account",
    Password    = "your_password",
    Environment = NhatTinEnvironment.Sandbox, // hoặc Production
});

var fee = await client.Bill.CalcFeeAsync(req);   // partner_id tự lấy từ token sau đăng nhập
if (fee.IsSuccess) Console.WriteLine(fee.Data![0].TotalFee);
```

- Sandbox → `https://apisandbox.ntlogistics.vn`; Production → `https://apiws.ntlogistics.vn` (đặt qua `Environment`, hoặc `BaseUrl` để tự trỏ).
- DI: `builder.Services.AddNhatTinLogisticsClient(o => { ... })` rồi inject `NhatTinLogisticsClient`.

## 4. Cấu hình (`NhatTinLogisticsClientOptions`)

| Option | Mặc định | Ý nghĩa |
| --- | --- | --- |
| `Username` / `Password` | – | Tài khoản JWT (bắt buộc khi `AutoAuthenticate=true`). |
| `Environment` | `Sandbox` | Chọn host Sandbox/Production. |
| `BaseUrl` | `null` | Ghi đè host (test/self-host). |
| `PartnerId` | `null` | Mặc định cho calc-fee/update/print; **tự bắt** từ login nếu bỏ trống. |
| `TimeoutMilliseconds` | `90000` | Timeout HttpClient. |
| `AutoAuthenticate` | `true` | SDK tự sign-in + refresh. Đặt `false` để tự quản token. |
| `EnableProactiveRefresh` | `true` | Refresh **trước khi** access token hết hạn (thay vì chỉ đợi 401). |
| `TokenExpirySkew` | `60s` | Refresh sớm bao lâu trước hạn. |
| `EnableRetry` | `true` | Retry lỗi tạm thời cho lệnh **idempotent**. |
| `MaxRetries` | `3` | Số lần retry sau lần đầu (→ tối đa 4 lượt). |
| `RetryBaseDelay` | `200ms` | Base cho exponential backoff. |
| `RetryMaxDelay` | `5s` | Trần một lần backoff. |

## 5. Hành vi cốt lõi cần biết khi vận hành

- **Auth 3 lớp:** sign-in lười → **proactive refresh** trước hạn (dùng TTL `token_expires_in`/`refresh_expires_in`
  từ login) → vẫn còn **refresh reactive khi 401** làm lưới an toàn. Refresh dùng single-flight (`SemaphoreSlim`)
  chống refresh trùng khi gọi song song. Nếu TTL không đọc được → bỏ proactive, chỉ dựa 401.
- **Retry chỉ cho lệnh idempotent:** mọi `GET` + `calc-fee`/`sign-in`/`refresh-token`. **Lệnh ghi
  (create/update-shipping/destroy/revert) KHÔNG BAO GIỜ retry** — NhatTin không có idempotency key nên retry mù
  có thể tạo trùng vận đơn; chống trùng là việc của POS. Retry khi: lỗi transport, timeout, HTTP 5xx/429/408.
  **Không** retry business-error (HTTP 200 + `success:false`) vì đó là câu trả lời thật.
- **Kết quả:** mọi call trả `NhatTinResponse<T>` (`IsSuccess`, `Message`, `Data`, `HttpStatusCode`, `RawBody`).
  Business fail **không ném**; chỉ transport/JSON/auth ném `NhatTinApiException`. `.EnsureSuccess()` để đổi
  business-fail thành ném.
- **Manual token mode** (`AutoAuthenticate=false`): SDK không tự sign-in/refresh — bạn seed token qua
  `client.Tokens.SetTokens(...)` và tự refresh khi gặp 401 (xem README gói).

## 6. Kiểm thử thật với sandbox (live smoke)

Unit test dùng payload stub → chỉ chứng minh "đường dây". Muốn chứng minh **hình dạng dữ liệu thật**, chạy
console drive chính SDK đánh vào sandbox thật:

```powershell
$env:NHATTIN_USERNAME = "thaison"
$env:NHATTIN_PASSWORD = "***"          # truyền qua env var — KHÔNG hardcode/commit secret
dotnet run --project CodeSDK/samples/NhatTinLogistics.Sdk.LiveSmoke
```

9 bước: sign-in → provinces → wards → calc-fee → create → tracking → print → cancel (dọn sạch bill test) →
refresh. Chi tiết + bằng chứng: [../Tests/Results/EVIDENCE-SDK-LIVE-2026-07-07.md](../Tests/Results/EVIDENCE-SDK-LIVE-2026-07-07.md).

## 7. "Hố ga" đã biết của sandbox (đã xử lý trong SDK)

Live smoke 2026-07-07 lộ 3 lệch shape mà stub bỏ sót — đều đã fix và có regression test:

1. **calc-fee** trả `service_id: null` → `FeeOption.ServiceId` là `int?`.
2. **tracking** trộn kiểu bất nhất (`weight:"2"` chuỗi, `cod_amt:0`/`main_fee:41936` số thô, `lifting_fee:null`)
   → `TolerantStringConverter` global đọc string/số/bool/null về `string?`.
3. **destroy** trả `data` là object `{success:[...],failed:[]}` (giống revert), không phải mảng →
   `CancelResponse{Succeeded,Failed}`.

Khác cần lưu ý: **print** trả JSON envelope (HTTP 200 + `success:false`, có mã `[ERR-xxxxx]`); bill in được có thể
là HTML → `PrintAsync` trả `PrintResult` content-type-aware. **refresh token cũ** ở sandbox vẫn dùng được sau khi
xoay (JWT stateless 7d) — cần hỏi lại hành vi ở production.

## 8. Tài liệu liên quan (đọc khi cần đào sâu)

- Thiết kế SDK gốc: [../docs/superpowers/specs/2026-07-06-nhattin-logistics-sdk-design.md](../docs/superpowers/specs/2026-07-06-nhattin-logistics-sdk-design.md)
- Thiết kế nâng cấp resilience (proactive refresh + retry): [../docs/superpowers/specs/2026-07-07-nhattin-sdk-resilience-design.md](../docs/superpowers/specs/2026-07-07-nhattin-sdk-resilience-design.md)
- Đánh giá nâng cấp toàn hệ (SDK/Sandbox/Webhook): [../docs/PhanTich-ThaoTac-DongBo/08-DANH-GIA-NANG-CAP.md](../docs/PhanTich-ThaoTac-DongBo/08-DANH-GIA-NANG-CAP.md)
- Tài liệu API NhatTin: [../NhatTinAPIDocumentation/NhatTin-API-Documentation-VI.md](../NhatTinAPIDocumentation/NhatTin-API-Documentation-VI.md)

## 9. Đề xuất nâng cấp còn treo (chưa làm, ưu tiên giảm dần)

Xem đầy đủ ở doc 08; tóm tắt phần SDK còn hở:

- Bóc `ErrorCode` ra `NhatTinResponse<T>` + catalog `NhatTinErrorCodes`.
- Hook observability (`ILogger`) — log method/path/status/elapsed/ref_code, mask token.
- Money field tracking → `decimal?` (thay vì `string?`) cho đối soát tiền.
- Multi-target `net8.0` (net6 đã EOL) / `netstandard2.0` nếu client là .NET Framework.
- `ITokenStore` bản bền (file/DB) cho nhiều tiến trình / khởi động lại.

## 10. Bảo mật

- Secret (username/password/token) **truyền qua env var hoặc secret store**, không hardcode/commit vào repo.
- Token phải **mask** khi log/in ra (xem `Mask()` trong live smoke).
- Webhook NhatTin **không ký** — không có chữ ký để verify; chống trùng/đối soát là việc của phía nhận.
