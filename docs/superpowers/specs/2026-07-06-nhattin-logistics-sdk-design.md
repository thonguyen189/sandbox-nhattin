# Thiết kế: NhatTinLogistics.Sdk (SDK C# xử lý dữ liệu Nhất Tín)

- **Ngày:** 2026-07-06
- **Phạm vi:** `CodeSDK/` — một class library .NET 6 độc lập (`NhatTinLogistics.Sdk`) + project test.
- **Ngoài phạm vi:** Sửa đổi `CodeSandBox/`, `CodeWebHooks/`. SDK **không** phụ thuộc bất kỳ project sandbox nào.
- **Mẫu tham chiếu:** `https://github.com/tingeehub/tingee-csharp` (Tingee.Sdk).

## 1. Bối cảnh & mục tiêu

Đối tác tích hợp Nhất Tín (ví dụ TruePos/POS) cần một thư viện C# gói lại toàn bộ việc "xử lý dữ liệu" hai chiều với Nhất Tín Logistics:

- **Gửi đi:** đăng nhập JWT, tạo/cập nhật/hủy/chuyển hoàn vận đơn, tính giá, tra cứu, in vận đơn, tra địa danh.
- **Nhận về:** parse payload webhook trạng thái đơn thành object có kiểu.

SDK đóng vai trò như `Tingee.Sdk` nhưng cho Nhất Tín, giúp lập trình viên tích hợp không phải tự dựng HTTP, tự quản token, hay tự map field snake_case.

**Nguồn sự thật cho hành vi API:** `NhatTinAPIDocumentation/vi/` (bản offline của `https://docs.ntlogistics.vn/docs/vi`).

### Khác biệt cốt lõi so với Tingee.Sdk (ảnh hưởng thiết kế)

| Khía cạnh | Tingee | Nhất Tín |
| --- | --- | --- |
| Xác thực | SecretKey + ClientId (HMAC ký request) | **JWT username/password + refresh token** |
| Envelope | `{ code, message, data }` | `{ success, message, data }` (`success` là boolean) |
| Webhook | Có chữ ký `x-signature` + `x-request-timestamp` | **Không ký số** — chỉ là JSON POST trần |

→ SDK **không** có `VerifyWebhookSignature`; thay vào đó cung cấp parser payload có kiểu. Điều này được ghi rõ trong README để tránh hiểu nhầm.

## 2. Kiến trúc tổng thể

Một solution `.NET 6` độc lập, layout lấy cảm hứng từ tingee-csharp (`Client/ Http/ Types/`) nhưng theo quy ước `src/ + tests/` của repo này để nhất quán với `CodeSandBox/` và `CodeWebHooks/`.

```
CodeSDK/
  NhatTinLogisticsSdk.sln
  src/NhatTinLogistics.Sdk/
    NhatTinLogistics.Sdk.csproj          # net6.0, nullable enable, packable → NuGet
    NhatTinLogisticsClient.cs            # entry point: .Auth .Bill .Location + Webhook helpers
    NhatTinLogisticsClientOptions.cs     # cấu hình
    NhatTinEnvironment.cs                # enum Sandbox | Production
    SdkVersion.cs                        # hằng version SDK (đọc runtime)
    Client/
      IAuthApi.cs      / AuthApi.cs      # SignInAsync, RefreshTokenAsync
      IBillApi.cs      / BillApi.cs      # Create/Update/Cancel/CalcFee/Revert/Tracking/Print
      ILocationApi.cs  / LocationApi.cs  # Provinces/Districts/Wards
    Http/
      NhatTinHttpClient.cs               # tầng gửi thấp: token, snake_case, envelope, retry-401
      NhatTinResponse.cs                 # envelope: Success, Message, Data<T>, IsSuccess, EnsureSuccess()
      NhatTinApiException.cs             # lỗi transport/parse/auth
      NhatTinJson.cs                     # JsonSerializerOptions dùng chung (snake_case)
      ITokenStore.cs / InMemoryTokenStore.cs   # lưu jwt/refresh/expiry, thread-safe
    Webhooks/
      WebhookPayload.cs                  # model payload webhook có kiểu
      NhatTinWebhookParser.cs            # Parse / TryParse raw JSON → WebhookPayload
    Types/
      Requests/    # *Request DTO ([JsonPropertyName] snake_case)
      Responses/   # *Result DTO
      Enums/       # ServiceType, PaymentMethod, CargoType, BillStatus
    Extensions/
      ServiceCollectionExtensions.cs     # AddNhatTinLogisticsClient(...)
    README.md
    CHANGELOG.md
  tests/NhatTinLogistics.Sdk.Tests/
    NhatTinLogistics.Sdk.Tests.csproj    # xUnit, net6.0
    Infrastructure/StubHttpMessageHandler.cs
    ...*Tests.cs
```

**Tech stack:** .NET 6 (`net6.0`), `System.Text.Json` (in-box), `Microsoft.Extensions.Http` + `Microsoft.Extensions.DependencyInjection.Abstractions` (cho DI extension), xUnit cho test.

> Ghi chú build: máy hiện có SDK .NET 9 (dùng cho sandbox .NET 8). SDK .NET 9 build được target `net6.0` không cần cài thêm. `global.json` (nếu có) không được ghim SDK version chặn net6.

## 3. Public API surface

### 3.1 Khởi tạo standalone (giống Tingee)

```csharp
var client = new NhatTinLogisticsClient(new NhatTinLogisticsClientOptions
{
    Username    = Environment.GetEnvironmentVariable("NTL_USERNAME")!,
    Password    = Environment.GetEnvironmentVariable("NTL_PASSWORD")!,
    Environment = NhatTinEnvironment.Sandbox,   // hoặc Production
    PartnerId   = 123736,                        // dùng cho calc-fee/update/print
});

NhatTinResponse<BillResult> res = await client.Bill.CreateAsync(new CreateBillRequest
{
    Weight = 2, ServiceId = 91, PaymentMethodId = 10, CargoTypeId = 2,
    SName = "TEST", SPhone = "0333333333", SAddress = "số 10",
    SProvinceCode = "01", SWardCode = "00004",
    RName = "TEST", RPhone = "0333333333", RAddress = "123",
    RProvinceCode = "79", RWardCode = "25750",
});

if (res.IsSuccess) Console.WriteLine(res.Data!.BillCode);
else               Console.WriteLine(res.Message);
```

### 3.2 Đăng ký qua DI (ASP.NET Core)

```csharp
builder.Services.AddNhatTinLogisticsClient(o =>
{
    o.Username = builder.Configuration["Ntl:Username"]!;
    o.Password = builder.Configuration["Ntl:Password"]!;
    o.Environment = NhatTinEnvironment.Sandbox;
    o.PartnerId = 123736;
});
// rồi inject NhatTinLogisticsClient vào controller/service
```

Extension đăng ký một typed `HttpClient` (qua `IHttpClientFactory`) cho `NhatTinHttpClient` và `NhatTinLogisticsClient` (scoped). `ITokenStore` mặc định là singleton in-memory để chia sẻ token giữa các request.

### 3.3 Xử lý webhook (bên nhận)

```csharp
// Trong controller nhận callback từ Nhất Tín
string raw = await new StreamReader(Request.Body).ReadToEndAsync();
if (NhatTinWebhookParser.TryParse(raw, out WebhookPayload payload))
{
    BillStatus status = payload.Status;      // status_id → enum (giữ raw nếu lạ)
    // ... cập nhật đơn theo payload.BillNo, status, payload.ShippingFee ...
}
```

### 3.4 Bảng nhóm method

| Nhóm | Method | HTTP thực tế |
| --- | --- | --- |
| `client.Auth` | `SignInAsync(username, password, ct)` | `POST /v1/auth/sign-in` |
| | `RefreshTokenAsync(refreshToken, ct)` | `POST /v1/auth/refresh-token` |
| `client.Bill` | `CreateAsync(CreateBillRequest, ct)` | `POST /v3/bill/create` |
| | `UpdateAsync(UpdateBillRequest, ct)` | `POST /v3/bill/update-shipping` |
| | `CancelAsync(IEnumerable<string> billCodes, ct)` | `POST /v3/bill/destroy` |
| | `CalcFeeAsync(CalcFeeRequest, ct)` | `POST /v3/bill/calc-fee` |
| | `RevertAsync(IEnumerable<string> billCodes, ct)` | `POST /v3/bill/revert-bill` |
| | `TrackingAsync(string billCode, ct)` | `GET /v3/bill/tracking?bill_code=` |
| | `GetPrintUrl(billCode, partnerId?)` / `PrintAsync(...)` | `GET /v3/bill/print?do_code=&partner_id=` |
| `client.Location` | `GetProvincesAsync(bool isNew, ct)` | `GET /v3/loc/provinces?is_new=` |
| | `GetDistrictsAsync(string provinceId, ct)` | `GET /v3/loc/districts?...` |
| | `GetWardsAsync(string? districtId, string? provinceId, bool isNew, ct)` | `GET /v3/loc/wards?...` |

## 4. Cấu hình (`NhatTinLogisticsClientOptions`)

| Thuộc tính | Kiểu | Mặc định | Mô tả |
| --- | --- | --- | --- |
| `Username` | `string` | (bắt buộc\*) | Tài khoản đăng nhập JWT |
| `Password` | `string` | (bắt buộc\*) | Mật khẩu |
| `Environment` | `NhatTinEnvironment` | `Sandbox` | `Sandbox` → `https://apisandbox.ntlogistics.vn`, `Production` → `https://apiws.ntlogistics.vn` |
| `BaseUrl` | `string?` | `null` | Override host (test/self-host). Nếu set thì bỏ qua `Environment` |
| `PartnerId` | `int?` | `null` | Mặc định cho calc-fee/update-shipping/print khi request không truyền |
| `TimeoutMilliseconds` | `int` | `90000` | Timeout HTTP (giống Tingee) |
| `AutoAuthenticate` | `bool` | `true` | Tự sign-in lazily khi chưa có token |

\* Bắt buộc khi `AutoAuthenticate = true`. Constructor **validate** và ném `ArgumentException` nếu thiếu Username/Password mà vẫn bật auto-auth.

## 5. Quản lý token & luồng auth (giá trị cốt lõi)

`NhatTinHttpClient` chịu trách nhiệm token, ẩn hoàn toàn với người dùng SDK:

1. Trước mỗi request cần auth: lấy access token từ `ITokenStore`. Nếu chưa có (và `AutoAuthenticate`), gọi `/v1/auth/sign-in` bằng Username/Password để lấy `jwt_token` + `refresh_token`, lưu vào store.
2. Gắn header `Authorization: Bearer <jwt_token>`.
3. Nếu nhận **401**: gọi `/v1/auth/refresh-token` với refresh token → cập nhật store → **retry request đúng 1 lần**. Nếu 401 lần nữa (hoặc refresh cũng 401): sign-in lại một lần; nếu vẫn hỏng → ném `NhatTinApiException` (auth failed).
4. Chống refresh trùng khi gọi song song: dùng `SemaphoreSlim` bao quanh thao tác refresh/sign-in; các luồng khác chờ rồi dùng token mới.

`token_expires_in`/`refresh_expires_in` trả về dạng chuỗi mềm ("1m", "2m"). SDK **không** dựa vào việc parse chuỗi này để hết hạn chủ động (không đáng tin); cơ chế chính là **phản ứng theo 401**. (Có thể parse best-effort để refresh sớm, nhưng đó là tối ưu tùy chọn, không bắt buộc.)

Endpoint auth (`sign-in`, `refresh-token`) tự chúng **không** đính token và **không** kích hoạt vòng retry-401 (tránh đệ quy).

## 6. Envelope & xử lý lỗi

Mọi call API trả `NhatTinResponse<T>`:

```csharp
public sealed class NhatTinResponse<T>
{
    public bool    Success { get; init; }
    public string? Message { get; init; }
    public T?      Data    { get; init; }
    public int     HttpStatusCode { get; init; }
    public string  RawBody { get; init; } = "";
    public bool IsSuccess => Success;               // alias đọc dễ
    public NhatTinResponse<T> EnsureSuccess();      // ném NhatTinApiException nếu !Success
}
```

Nguyên tắc:

- **`success:false`** (lỗi nghiệp vụ) → trả envelope `IsSuccess=false`, `Message` set, `Data=default`. **Không ném.** Người dùng chủ động kiểm tra hoặc gọi `EnsureSuccess()`.
- **Lỗi transport/timeout/HTTP 5xx/JSON hỏng** → ném `NhatTinApiException` (kèm `HttpStatusCode`, `RawBody`, inner exception).
- **Auth thất bại sau retry** → `NhatTinApiException` với thông điệp rõ ràng.

## 7. Serialization & wire format

- Một `JsonSerializerOptions` dùng chung trong `NhatTinJson`: `PropertyNameCaseInsensitive = true`, bỏ qua null khi ghi (`DefaultIgnoreCondition = WhenWritingNull`), encoder cho phép ký tự Unicode (tiếng Việt).
- DTO dùng `[JsonPropertyName("snake_case")]` **tường minh** cho từng field theo đúng tài liệu — không phụ thuộc naming policy tự động, để khớp chính xác các tên "khó" (`s_province_code`, `payment_method_id`, `is_return_doc`, `do_code`...).
- **Cạm bẫy kiểu số dạng chuỗi:** response `tracking` trả số dưới dạng chuỗi (`"weight": "1.00"`, `"total_fee": "20000"`). DTO tracking để các field đó là `string?` (giữ nguyên) thay vì ép `double`, tránh lỗi deserialize. (Có thể thêm property tính toán parse an toàn nếu cần.)
- **`doCode` (camelCase) trong response hủy đơn** giữ đúng casing theo tài liệu — đã có tiền lệ ở sandbox (commit `5bf5ae6`).

## 8. Enums / Master Data (`Types/Enums`)

Định nghĩa theo bảng Master Data trong `00-thong-tin-ket-noi.md`. Lưu **giá trị số thật**; cung cấp helper map an toàn (giá trị lạ → giữ số, không ném).

- `ServiceType`: `GiaoHangNhanh=90 (CPN)`, `HoaToc=81`, `TietKiem=91`, `HonHopMES=21`.
- `PaymentMethod`: `SenderPayNow=10`, `SenderPayLater=11`, `ReceiverPayNow=20`.
- `CargoType`: `ChungTu=1`, `HangHoa=2`, `HangLanh=3`, `SinhPham=4`, `MauBenhPham=5`.
- `BillStatus`: các mã `1..17` (`WaitingFail=1`, `WaitingPickup=2`, `PickedUp=3`, `Delivered=4`, `Cancelled=6`, `FailedDelivery=7`, `Returning=9`, `Returned=10`, `DeliveryIncident=11`, `Draft=12`, `Delivering=13`, `InTransit=15`, `ReturnDelivering=16`, `PickupError=17`).

Trong DTO request, các field id giữ kiểu `int` (khớp wire), nhưng có overload/constructor tiện dụng nhận enum. `WebhookPayload.Status` phơi ra `BillStatus` (map từ `status_id`), đồng thời giữ `StatusId` số gốc.

## 9. Xử lý webhook (`Webhooks/`)

`WebhookPayload` — record khớp payload webhook Nhất Tín (`bill/webhook.md`):

| JSON | Property | Kiểu |
| --- | --- | --- |
| `bill_no` | `BillNo` | `string` |
| `ref_code` | `RefCode` | `string?` |
| `status_id` | `StatusId` | `int` (+ `Status` → `BillStatus`) |
| `status_name` | `StatusName` | `string` |
| `status_time` | `StatusTime` | `long` (unix) + `StatusTimeUtc` (`DateTimeOffset`) |
| `push_time` | `PushTime` | `long` (unix) + `PushTimeUtc` |
| `shipping_fee` | `ShippingFee` | `decimal` |
| `is_partial` | `IsPartial` | `int` (+ `IsPartialReturn` bool) |
| `reason` | `Reason` | `string?` |
| `weight`,`dimension_weight`,`length`,`width`,`height` | tương ứng | `double` |
| `expected_at` | `ExpectedAt` | `string` (+ helper parse `DateTime?` theo `yyyy-MM-dd HH:mm:ss`) |

`NhatTinWebhookParser`:
- `Parse(string json)` → `WebhookPayload` (ném `NhatTinApiException` nếu JSON hỏng).
- `TryParse(string json, out WebhookPayload payload)` → `bool`.
- Dùng chung `NhatTinJson` options, không phụ thuộc HTTP client → gọi được độc lập, không cần khởi tạo `NhatTinLogisticsClient`.

## 10. Print (`GET /v3/bill/print`) — điểm cần lưu ý

Tài liệu `printbill.md` mâu thuẫn: mô tả trả `{success,message,data}` nhưng sample lại là URL trên host in riêng (`printdev/printdigi.ntlogistics.vn`). Do hành vi chưa xác nhận:

- SDK cung cấp `Bill.GetPrintUrl(string billCode, int? partnerId = null)` → dựng URL in (dùng `PartnerId` mặc định nếu không truyền). Đây là phần chắc chắn, không gọi mạng.
- `Bill.PrintAsync(...)` (tùy chọn) GET URL đó và trả `byte[]`/stream + content-type — đánh dấu "hành vi phụ thuộc phía Nhất Tín" trong XML-doc.

Đây là **điểm mở** (mục 13), không chặn phần còn lại của SDK.

## 11. Tích hợp DI (`Extensions/ServiceCollectionExtensions.cs`)

`AddNhatTinLogisticsClient(this IServiceCollection, Action<NhatTinLogisticsClientOptions>)`:
- Đăng ký options, validate khi resolve.
- Đăng ký typed `HttpClient` (đặt `BaseAddress`, `Timeout`) cho `NhatTinHttpClient`.
- `ITokenStore` → `InMemoryTokenStore` singleton (chia sẻ token toàn app).
- `NhatTinLogisticsClient` → scoped, nhận `NhatTinHttpClient`.

Không ép người dùng DI: constructor standalone tự tạo `HttpClient` nội bộ (và `HttpMessageHandler` inject được để test).

## 12. Chiến lược test (TDD, `NhatTinLogistics.Sdk.Tests`)

Dùng `StubHttpMessageHandler` (nhận lambda `(HttpRequestMessage) → HttpResponseMessage`) để giả lập Nhất Tín, **không gọi mạng thật**. Viết test trước, code sau. Các nhóm test tối thiểu:

1. **Auth:** `SignInAsync` map đúng `jwt_token/refresh_token`; token được lưu store.
2. **Retry-401:** request đầu 401 → SDK gọi refresh → retry → thành công; xác nhận đúng 1 lần refresh, không vòng lặp vô hạn.
3. **Refresh đồng thời:** N request song song cùng gặp 401 chỉ refresh **một** lần (SemaphoreSlim).
4. **Serialize create bill:** body gửi lên chứa đúng field snake_case theo `createbill.md` (assert JSON).
5. **Envelope:** `success:true` → `IsSuccess`, `Data` map; `success:false` → `IsSuccess=false`, `Message` set, không ném; `EnsureSuccess()` ném khi false.
6. **Cancel/Revert:** map `data` mảng `[{doCode,message}]` và `{success[],failed[]}` đúng.
7. **Tracking số-dạng-chuỗi:** deserialize không lỗi khi field số là chuỗi.
8. **Webhook parse:** `Parse`/`TryParse` từ payload mẫu → field đúng; `status_id → BillStatus`; JSON hỏng → `TryParse=false`.
9. **DI:** `AddNhatTinLogisticsClient` + `BuildServiceProvider` resolve được `NhatTinLogisticsClient`.
10. **Enum map an toàn:** `status_id` lạ (vd 99) không ném, giữ số gốc.

## 13. Đóng gói (NuGet)

`NhatTinLogistics.Sdk.csproj`:
- `<TargetFramework>net6.0</TargetFramework>`, `<Nullable>enable</Nullable>`, `<LangVersion>latest</LangVersion>`, `<ImplicitUsings>enable</ImplicitUsings>`.
- Metadata: `PackageId=NhatTinLogistics.Sdk`, `Version` (đồng bộ `SdkVersion.cs`), `Authors`, `Description`, `PackageTags`, `RepositoryUrl`, `README.md` (packed), `PackageLicenseExpression`.
- Dependencies: `Microsoft.Extensions.Http`, `Microsoft.Extensions.DependencyInjection.Abstractions`. (`System.Text.Json` in-box net6.)
- `README.md` (hướng dẫn dùng standalone + DI + webhook, nêu rõ webhook không ký số) và `CHANGELOG.md` (bản `0.1.0`).

## 14. Điểm mở / rủi ro (không tự suy diễn thành đặc tả)

- **Print API:** host/định dạng trả về chưa xác nhận → chỉ đảm bảo `GetPrintUrl`; `PrintAsync` best-effort (mục 10).
- **Lỗi chính tả trong tài liệu:** `"messsage"` (cancel), `insurr_fee`/`insur_fee` (create vs calc-fee) không đồng nhất. DTO khớp field như tài liệu ghi; `PropertyNameCaseInsensitive` + map cả hai biến thể fee khi cần; ghi chú lại điểm này.
- **`partner_id`:** một số endpoint bắt buộc; nguồn giá trị là cấu hình tài khoản đối tác. Đưa vào `Options.PartnerId`, cho phép override theo request.
- **Định dạng `token_expires_in` mềm ("1m"):** không dùng làm cơ chế hết hạn chính (đã xử lý ở mục 5).
- **Districts (địa danh cũ):** tham số chưa rõ đầy đủ; giữ chữ ký linh hoạt, không cứng hoá.

## 15. Trình tự triển khai (build sequence)

1. Scaffold solution `CodeSDK/` + 2 project (`net6.0`), thêm vào (một `.sln` riêng của SDK).
2. `NhatTinJson`, `NhatTinResponse<T>`, `NhatTinApiException`, `ITokenStore/InMemoryTokenStore`, enums — nền tảng thuần, test được ngay.
3. `NhatTinHttpClient` (token, envelope, retry-401) — TDD với stub handler.
4. `AuthApi` → `BillApi` → `LocationApi` + các DTO Requests/Responses — mỗi nhóm kèm test serialize/deserialize.
5. `Webhooks` (payload + parser) — TDD.
6. `NhatTinLogisticsClient` (ghép nhóm) + `ServiceCollectionExtensions` (DI) — test resolve.
7. `README.md`, `CHANGELOG.md`, metadata NuGet, `SdkVersion.cs`.
8. `dotnet build` + `dotnet test` toàn solution xanh; đóng gói thử `dotnet pack`.
