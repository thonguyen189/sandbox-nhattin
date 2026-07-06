# Nhất Tín Sandbox + Webhook Receiver Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Build a faithful .NET emulator of the Nhất Tín Logistics partner API (`CodeSandBox`) plus a webhook receiver referee (`CodeWebHooks`), both backed by SQLite, so TruePos can integrate/test/demo without depending on the real API.

**Architecture:** Two independent .NET 8 solutions. `CodeSandBox` uses Clean Architecture (Domain → Application → Infrastructure → Api/AdminPortal) and emulates auth, location, and bill endpoints; changing a bill's status dispatches a webhook. `CodeWebHooks` is a minimal Web API + Razor Pages that receives callbacks, stores raw evidence, and ACKs. All behavior traces to `NhatTinAPIDocumentation/vi/`.

**Tech Stack:** .NET 8 (TFM `net8.0`, built with installed SDK 9.0.301), ASP.NET Core Web API + Razor Pages, EF Core 8 + SQLite, JWT Bearer auth, xUnit, Swashbuckle (Swagger), PowerShell smoke test.

## Global Constraints

- Target framework: `net8.0` for every project (LTS; SDK 9.0.301 builds it fine). Do NOT target `net9.0`.
- All EF Core / ASP.NET packages pinned to `8.0.*`.
- Response envelope for every emulated Nhất Tín endpoint: JSON object `{ "success": bool, "message": string, "data": object|array }`. Field casing in `data` follows the docs (snake_case like `bill_code`, `jwt_token`).
- Storage is SQLite only. Sandbox DB file: `CodeSandBox/src/NhatTinSandbox.Api/App_Data/nhattin-sandbox.db`. Webhook DB file: `CodeWebHooks/src/NhatTinWebhookReceiver.Api/App_Data/nhattin-webhooks.db`.
- Ports: Sandbox Api `5080` (http) / `7080` (https); Sandbox AdminPortal `5090` / `7090`; Webhook Receiver `5099` (http).
- No hardcoded real credentials or tokens anywhere. The only seeded account is a demo account (`sandbox` / `sandbox123`) clearly marked as a sandbox demo.
- Every route/field must match the corresponding file under `NhatTinAPIDocumentation/vi/`. Do not invent fields the docs don't mention (except the internal `/sandbox/*` helper routes).
- Unconfirmed behavior (real token TTL, webhook retry policy, print response format, full location/master-data catalogs) must stay explicitly approximated — never presented as authoritative. Keep the approximations listed in section 9 of the design spec.

**Reference spec:** `docs/superpowers/specs/2026-07-06-nhattin-sandbox-webhook-design.md`

---

## File Structure

### CodeSandBox (solution `NhatTinSandbox.sln`)

| File | Responsibility |
| --- | --- |
| `src/NhatTinSandbox.Domain/Enums/BillStatus.cs` | `status_id` catalog from `00-thong-tin-ket-noi.md`. |
| `src/NhatTinSandbox.Domain/Entities/Bill.cs` | Bill entity — all create/update fields. |
| `src/NhatTinSandbox.Domain/Entities/BillStatusHistory.cs` | Status change log rows. |
| `src/NhatTinSandbox.Domain/Entities/PartnerAccount.cs` | Sandbox login account. |
| `src/NhatTinSandbox.Domain/Entities/RefreshToken.cs` | Refresh token lifecycle. |
| `src/NhatTinSandbox.Domain/Entities/WebhookSubscription.cs` | Registered callback URLs. |
| `src/NhatTinSandbox.Domain/Entities/WebhookDeliveryLog.cs` | Outbound webhook attempts. |
| `src/NhatTinSandbox.Domain/Entities/Location.cs` | Province/District/Ward rows. |
| `src/NhatTinSandbox.Domain/Entities/MasterDataItem.cs` | Service/payment/cargo/status catalog rows. |
| `src/NhatTinSandbox.Application/Common/ApiResult.cs` | `{success,message,data}` envelope helper. |
| `src/NhatTinSandbox.Application/Auth/*` | Auth DTOs + `IAuthTokenService`. |
| `src/NhatTinSandbox.Application/Locations/*` | Location DTOs + `ILocationCatalog`. |
| `src/NhatTinSandbox.Application/Bills/*` | Bill DTOs + `IBillService`. |
| `src/NhatTinSandbox.Application/Webhooks/*` | Webhook payload DTO + `IWebhookDispatcher`. |
| `src/NhatTinSandbox.Infrastructure/Persistence/SandboxDbContext.cs` | EF Core context. |
| `src/NhatTinSandbox.Infrastructure/Persistence/SeedData.cs` | Seed accounts/locations/master data/subscription. |
| `src/NhatTinSandbox.Infrastructure/Auth/JwtAuthTokenService.cs` | JWT issue/refresh. |
| `src/NhatTinSandbox.Infrastructure/Locations/LocationCatalog.cs` | Location queries. |
| `src/NhatTinSandbox.Infrastructure/Bills/BillService.cs` | Bill use cases + fee calc. |
| `src/NhatTinSandbox.Infrastructure/Webhooks/HttpWebhookDispatcher.cs` | POST webhook + log. |
| `src/NhatTinSandbox.Api/Program.cs` | API host, DI, JWT bearer, Swagger, migrate+seed. |
| `src/NhatTinSandbox.Api/Controllers/*` | Auth/Location/Bill/Sandbox controllers. |
| `src/NhatTinSandbox.AdminPortal/*` | Razor Pages admin UI. |
| `tests/NhatTinSandbox.Tests/*` | xUnit tests. |

### CodeWebHooks (solution `NhatTinWebhookReceiver.sln`)

| File | Responsibility |
| --- | --- |
| `src/NhatTinWebhookReceiver.Api/Domain/ReceivedWebhook.cs` | Raw evidence entity. |
| `src/NhatTinWebhookReceiver.Api/Persistence/WebhookDbContext.cs` | EF Core context. |
| `src/NhatTinWebhookReceiver.Api/Controllers/WebhookController.cs` | Receive + ACK. |
| `src/NhatTinWebhookReceiver.Api/Pages/*` | Razor Pages log viewer. |
| `src/NhatTinWebhookReceiver.Api/Program.cs` | Host, DI, migrate. |
| `tests/NhatTinWebhookReceiver.Tests/*` | xUnit tests. |

### Repo root

| File | Responsibility |
| --- | --- |
| `.gitignore` | .NET ignore rules. |
| `Tests/run-nhattin-cycle.ps1` | End-to-end smoke test. |
| `README.md` | How to run both solutions. |

---

### Task 0: Repo init and CodeSandBox solution scaffold

**Files:**
- Create: `.gitignore`
- Create: `CodeSandBox/NhatTinSandbox.sln` and all `src/*` + `tests/*` project files

- [ ] **Step 1: Initialize git and .gitignore**

Run from repo root (`NhatTin-logistics-sandbox`):

```powershell
git init
dotnet new gitignore
```

Then append App_Data DB files to `.gitignore`:

```
# SQLite runtime data
**/App_Data/*.db
**/App_Data/*.db-shm
**/App_Data/*.db-wal
```

- [ ] **Step 2: Create the solution and projects**

```powershell
dotnet new sln -n NhatTinSandbox -o CodeSandBox
dotnet new classlib -n NhatTinSandbox.Domain -o CodeSandBox\src\NhatTinSandbox.Domain -f net8.0
dotnet new classlib -n NhatTinSandbox.Application -o CodeSandBox\src\NhatTinSandbox.Application -f net8.0
dotnet new classlib -n NhatTinSandbox.Infrastructure -o CodeSandBox\src\NhatTinSandbox.Infrastructure -f net8.0
dotnet new webapi -n NhatTinSandbox.Api -o CodeSandBox\src\NhatTinSandbox.Api -f net8.0 --use-controllers
dotnet new webapp -n NhatTinSandbox.AdminPortal -o CodeSandBox\src\NhatTinSandbox.AdminPortal -f net8.0
dotnet new xunit -n NhatTinSandbox.Tests -o CodeSandBox\tests\NhatTinSandbox.Tests -f net8.0
```

- [ ] **Step 3: Delete template placeholder files**

```powershell
Remove-Item CodeSandBox\src\NhatTinSandbox.Domain\Class1.cs
Remove-Item CodeSandBox\src\NhatTinSandbox.Application\Class1.cs
Remove-Item CodeSandBox\src\NhatTinSandbox.Infrastructure\Class1.cs
Remove-Item CodeSandBox\tests\NhatTinSandbox.Tests\UnitTest1.cs
Remove-Item CodeSandBox\src\NhatTinSandbox.Api\WeatherForecast.cs -ErrorAction SilentlyContinue
Remove-Item CodeSandBox\src\NhatTinSandbox.Api\Controllers\WeatherForecastController.cs -ErrorAction SilentlyContinue
```

- [ ] **Step 4: Add projects to solution and wire references**

```powershell
dotnet sln CodeSandBox\NhatTinSandbox.sln add `
  CodeSandBox\src\NhatTinSandbox.Domain\NhatTinSandbox.Domain.csproj `
  CodeSandBox\src\NhatTinSandbox.Application\NhatTinSandbox.Application.csproj `
  CodeSandBox\src\NhatTinSandbox.Infrastructure\NhatTinSandbox.Infrastructure.csproj `
  CodeSandBox\src\NhatTinSandbox.Api\NhatTinSandbox.Api.csproj `
  CodeSandBox\src\NhatTinSandbox.AdminPortal\NhatTinSandbox.AdminPortal.csproj `
  CodeSandBox\tests\NhatTinSandbox.Tests\NhatTinSandbox.Tests.csproj

dotnet add CodeSandBox\src\NhatTinSandbox.Application reference CodeSandBox\src\NhatTinSandbox.Domain
dotnet add CodeSandBox\src\NhatTinSandbox.Infrastructure reference CodeSandBox\src\NhatTinSandbox.Application
dotnet add CodeSandBox\src\NhatTinSandbox.Api reference CodeSandBox\src\NhatTinSandbox.Infrastructure
dotnet add CodeSandBox\src\NhatTinSandbox.AdminPortal reference CodeSandBox\src\NhatTinSandbox.Infrastructure
dotnet add CodeSandBox\tests\NhatTinSandbox.Tests reference CodeSandBox\src\NhatTinSandbox.Infrastructure
```

- [ ] **Step 5: Add NuGet packages**

```powershell
dotnet add CodeSandBox\src\NhatTinSandbox.Infrastructure package Microsoft.EntityFrameworkCore.Sqlite -v 8.0.10
dotnet add CodeSandBox\src\NhatTinSandbox.Infrastructure package Microsoft.EntityFrameworkCore.Design -v 8.0.10
dotnet add CodeSandBox\src\NhatTinSandbox.Infrastructure package System.IdentityModel.Tokens.Jwt -v 8.1.2
dotnet add CodeSandBox\src\NhatTinSandbox.Api package Microsoft.AspNetCore.Authentication.JwtBearer -v 8.0.10
dotnet add CodeSandBox\src\NhatTinSandbox.Api package Microsoft.EntityFrameworkCore.Design -v 8.0.10
dotnet add CodeSandBox\tests\NhatTinSandbox.Tests package Microsoft.EntityFrameworkCore.Sqlite -v 8.0.10
```

- [ ] **Step 6: Build the empty solution**

Run: `dotnet build CodeSandBox\NhatTinSandbox.sln`
Expected: Build succeeded, 0 errors.

- [ ] **Step 7: Commit**

```powershell
git add .gitignore CodeSandBox
git commit -m "chore: scaffold CodeSandBox clean-architecture solution"
```

---

### Task 1: Domain entities and enums

**Files:**
- Create: `CodeSandBox/src/NhatTinSandbox.Domain/Enums/BillStatus.cs`
- Create: `CodeSandBox/src/NhatTinSandbox.Domain/Entities/Bill.cs`
- Create: `CodeSandBox/src/NhatTinSandbox.Domain/Entities/BillStatusHistory.cs`
- Create: `CodeSandBox/src/NhatTinSandbox.Domain/Entities/PartnerAccount.cs`
- Create: `CodeSandBox/src/NhatTinSandbox.Domain/Entities/RefreshToken.cs`
- Create: `CodeSandBox/src/NhatTinSandbox.Domain/Entities/WebhookSubscription.cs`
- Create: `CodeSandBox/src/NhatTinSandbox.Domain/Entities/WebhookDeliveryLog.cs`
- Create: `CodeSandBox/src/NhatTinSandbox.Domain/Entities/Location.cs`
- Create: `CodeSandBox/src/NhatTinSandbox.Domain/Entities/MasterDataItem.cs`

**Interfaces:**
- Produces: `Bill`, `BillStatusHistory`, `PartnerAccount`, `RefreshToken`, `WebhookSubscription`, `WebhookDeliveryLog`, `Location`, `MasterDataItem` entities; enum `BillStatusId`.

- [ ] **Step 1: Create the status enum**

`CodeSandBox/src/NhatTinSandbox.Domain/Enums/BillStatus.cs`:

```csharp
namespace NhatTinSandbox.Domain.Enums;

// status_id catalog from NhatTinAPIDocumentation/vi/00-thong-tin-ket-noi.md.
// Known-incomplete: only documented ids are listed.
public enum BillStatusId
{
    ChuaThanhCong = 1,     // Waiting
    ChoLayHang = 2,        // Waiting
    DaLayHang = 3,         // KCB
    DaGiaoHang = 4,        // FBC
    Huy = 6,               // GBV
    KhongPhatDuoc = 7,     // FUD
    DangChuyenHoan = 9,    // NRT
    DaChuyenHoan = 10,     // MRC
    SuCoGiaoHang = 11,     // QIU
    VanDonNhap = 12,       // DRF
    DangGiaoHang = 13,     // DEL
    DangVanChuyen = 15,
    DangGiaoHangHoan = 16,
    LoiLayHang = 17
}
```

- [ ] **Step 2: Create the Bill entity**

`CodeSandBox/src/NhatTinSandbox.Domain/Entities/Bill.cs`:

```csharp
namespace NhatTinSandbox.Domain.Entities;

// Fields mirror NhatTinAPIDocumentation/vi/bill/createbill.md and updatebill.md.
public class Bill
{
    public int Id { get; set; }
    public string BillCode { get; set; } = string.Empty; // Nhất Tín "bill_code" e.g. CP...
    public string? RefCode { get; set; }
    public int PackageNo { get; set; } = 1;
    public double Weight { get; set; }
    public double Width { get; set; }
    public double Length { get; set; }
    public double Height { get; set; }
    public string? CargoContent { get; set; }
    public int ServiceId { get; set; }
    public int PaymentMethodId { get; set; }
    public int IsReturnDoc { get; set; }
    public decimal CodAmount { get; set; }
    public string? Note { get; set; }
    public double CargoValue { get; set; }
    public int CargoTypeId { get; set; }

    public string SenderName { get; set; } = string.Empty;
    public string SenderPhone { get; set; } = string.Empty;
    public string SenderAddress { get; set; } = string.Empty;
    public string SenderProvinceCode { get; set; } = string.Empty;
    public string SenderWardCode { get; set; } = string.Empty;

    public int IsReturnOrg { get; set; }
    public string? ReturnName { get; set; }
    public string? ReturnPhone { get; set; }
    public string? ReturnAddress { get; set; }
    public string? ReturnProvinceCode { get; set; }
    public string? ReturnWardCode { get; set; }

    public string ReceiverName { get; set; } = string.Empty;
    public string ReceiverPhone { get; set; } = string.Empty;
    public string ReceiverAddress { get; set; } = string.Empty;
    public string ReceiverProvinceCode { get; set; } = string.Empty;
    public string ReceiverWardCode { get; set; } = string.Empty;

    public int IsDraft { get; set; }
    public decimal OtherFee { get; set; }
    public int IsInstallation { get; set; }
    public int BillType { get; set; } = 1;
    public string? BillReturn { get; set; }

    public int StatusId { get; set; } = 2; // default Chờ lấy hàng
    public decimal MainFee { get; set; }
    public decimal TotalFee { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? ExpectedAt { get; set; }

    public List<BillStatusHistory> Histories { get; set; } = new();
}
```

- [ ] **Step 3: Create the remaining entities**

`CodeSandBox/src/NhatTinSandbox.Domain/Entities/BillStatusHistory.cs`:

```csharp
namespace NhatTinSandbox.Domain.Entities;

public class BillStatusHistory
{
    public int Id { get; set; }
    public int BillId { get; set; }
    public int StatusId { get; set; }
    public string StatusName { get; set; } = string.Empty;
    public string? Reason { get; set; }
    public decimal ShippingFee { get; set; }
    public DateTimeOffset ChangedAt { get; set; } = DateTimeOffset.UtcNow;
    public Bill? Bill { get; set; }
}
```

`CodeSandBox/src/NhatTinSandbox.Domain/Entities/PartnerAccount.cs`:

```csharp
namespace NhatTinSandbox.Domain.Entities;

public class PartnerAccount
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public int PartnerId { get; set; }
    public bool IsActive { get; set; } = true;
}
```

`CodeSandBox/src/NhatTinSandbox.Domain/Entities/RefreshToken.cs`:

```csharp
namespace NhatTinSandbox.Domain.Entities;

public class RefreshToken
{
    public int Id { get; set; }
    public int AccountId { get; set; }
    public string TokenHash { get; set; } = string.Empty;
    public DateTimeOffset ExpiresAt { get; set; }
    public bool IsRevoked { get; set; }
}
```

`CodeSandBox/src/NhatTinSandbox.Domain/Entities/WebhookSubscription.cs`:

```csharp
namespace NhatTinSandbox.Domain.Entities;

public class WebhookSubscription
{
    public int Id { get; set; }
    public int PartnerId { get; set; }
    public string CallbackUrl { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
}
```

`CodeSandBox/src/NhatTinSandbox.Domain/Entities/WebhookDeliveryLog.cs`:

```csharp
namespace NhatTinSandbox.Domain.Entities;

public class WebhookDeliveryLog
{
    public int Id { get; set; }
    public int BillId { get; set; }
    public string BillCode { get; set; } = string.Empty;
    public int SubscriptionId { get; set; }
    public string CallbackUrl { get; set; } = string.Empty;
    public string PayloadJson { get; set; } = string.Empty;
    public int? HttpStatusCode { get; set; }
    public bool Success { get; set; }
    public string? ResponseBody { get; set; }
    public DateTimeOffset AttemptedAt { get; set; } = DateTimeOffset.UtcNow;
}
```

`CodeSandBox/src/NhatTinSandbox.Domain/Entities/Location.cs`:

```csharp
namespace NhatTinSandbox.Domain.Entities;

public enum LocationKind { Province = 1, District = 2, Ward = 3 }

public class Location
{
    public int Id { get; set; }
    public LocationKind Kind { get; set; }
    public string Code { get; set; } = string.Empty; // province/district/ward id used by API
    public string Name { get; set; } = string.Empty;
    public string? ParentCode { get; set; }          // province code for districts/wards
    public string? DistrictCode { get; set; }        // district code for old-unit wards
    public bool IsNew { get; set; }                  // true => "Y", false => "N"
}
```

`CodeSandBox/src/NhatTinSandbox.Domain/Entities/MasterDataItem.cs`:

```csharp
namespace NhatTinSandbox.Domain.Entities;

public enum MasterDataKind { Service = 1, PaymentMethod = 2, CargoType = 3, BillStatus = 4 }

public class MasterDataItem
{
    public int Id { get; set; }
    public MasterDataKind Kind { get; set; }
    public int Code { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? StatusCode { get; set; } // for BillStatus rows (e.g. KCB, FBC)
}
```

- [ ] **Step 4: Build**

Run: `dotnet build CodeSandBox\src\NhatTinSandbox.Domain`
Expected: Build succeeded.

- [ ] **Step 5: Commit**

```powershell
git add CodeSandBox\src\NhatTinSandbox.Domain
git commit -m "feat(domain): add Nhất Tín sandbox entities and status enum"
```

---

### Task 2: ApiResult envelope and Application contracts

**Files:**
- Create: `CodeSandBox/src/NhatTinSandbox.Application/Common/ApiResult.cs`
- Create: `CodeSandBox/src/NhatTinSandbox.Application/Auth/AuthContracts.cs`
- Create: `CodeSandBox/src/NhatTinSandbox.Application/Locations/LocationContracts.cs`
- Create: `CodeSandBox/src/NhatTinSandbox.Application/Bills/BillContracts.cs`
- Create: `CodeSandBox/src/NhatTinSandbox.Application/Webhooks/WebhookContracts.cs`

**Interfaces:**
- Produces:
  - `ApiResult.Ok(data, message)` / `ApiResult.Fail(message)` returning an object serializable to `{success,message,data}`.
  - `record SignInInput(string Username, string Password)`, `record AuthTokenResult(string JwtToken, string TokenType, int TokenExpiresInSeconds, string RefreshToken, int RefreshExpiresInSeconds)`, `interface IAuthTokenService { Task<AuthTokenResult?> SignInAsync(string,string,CancellationToken); Task<AuthTokenResult?> RefreshAsync(string,CancellationToken); }`.
  - `record ProvinceDto(string Id, string ProvinceName, string IsNew)`, `record DistrictDto(string Id, string DistrictName, string IsNew)`, `record WardDto(string Id, string WardName, string IsNew)`, `interface ILocationCatalog`.
  - `record CreateBillInput(...)`, `record BillSummary(...)`, `record CalcFeeInput(...)`, `record FeeOption(...)`, `interface IBillService`.
  - `record WebhookPayload(...)`, `interface IWebhookDispatcher { Task DispatchAsync(int billId, CancellationToken); }`.

- [ ] **Step 1: Create ApiResult**

`CodeSandBox/src/NhatTinSandbox.Application/Common/ApiResult.cs`:

```csharp
namespace NhatTinSandbox.Application.Common;

public static class ApiResult
{
    public static object Ok(object? data, string message = "")
        => new { success = true, message, data };

    public static object Fail(string message, object? data = null)
        => new { success = false, message, data = data ?? new { } };
}
```

- [ ] **Step 2: Create auth contracts**

`CodeSandBox/src/NhatTinSandbox.Application/Auth/AuthContracts.cs`:

```csharp
namespace NhatTinSandbox.Application.Auth;

public sealed record SignInInput(string Username, string Password);

public sealed record AuthTokenResult(
    string JwtToken,
    string TokenType,
    int TokenExpiresInSeconds,
    string RefreshToken,
    int RefreshExpiresInSeconds);

public interface IAuthTokenService
{
    // Returns null when credentials are invalid.
    Task<AuthTokenResult?> SignInAsync(string username, string password, CancellationToken ct);
    // Returns null when the refresh token is missing/expired/revoked.
    Task<AuthTokenResult?> RefreshAsync(string refreshToken, CancellationToken ct);
}
```

- [ ] **Step 3: Create location contracts**

`CodeSandBox/src/NhatTinSandbox.Application/Locations/LocationContracts.cs`:

```csharp
namespace NhatTinSandbox.Application.Locations;

public sealed record ProvinceDto(string Id, string ProvinceName, string IsNew);
public sealed record DistrictDto(string Id, string DistrictName, string IsNew);
public sealed record WardDto(string Id, string WardName, string IsNew);

public interface ILocationCatalog
{
    Task<IReadOnlyList<ProvinceDto>> GetProvincesAsync(bool isNew, CancellationToken ct);
    Task<IReadOnlyList<DistrictDto>> GetDistrictsAsync(string provinceId, CancellationToken ct);
    Task<IReadOnlyList<WardDto>> GetWardsAsync(string? districtId, string? provinceId, bool isNew, CancellationToken ct);
}
```

- [ ] **Step 4: Create bill contracts**

`CodeSandBox/src/NhatTinSandbox.Application/Bills/BillContracts.cs`:

```csharp
namespace NhatTinSandbox.Application.Bills;

public sealed record CreateBillInput(
    string? RefCode,
    int? PackageNo,
    double Weight,
    double Width,
    double Length,
    double Height,
    string? CargoContent,
    int ServiceId,
    int PaymentMethodId,
    int? IsReturnDoc,
    decimal? CodAmount,
    string? Note,
    double? CargoValue,
    int CargoTypeId,
    string SName,
    string SPhone,
    string SAddress,
    string SProvinceCode,
    string SWardCode,
    string RName,
    string RPhone,
    string RAddress,
    string RProvinceCode,
    string RWardCode,
    int? IsDraft,
    decimal? OtherFee,
    int? IsInstallation,
    int? BillType,
    string? BillReturn);

public sealed record UpdateBillInput(
    int PartnerId,
    string BillCode,
    decimal? CodAmount,
    double? CargoValue,
    double? Weight,
    double? Length,
    double? Height,
    double? Width,
    string? CargoContent,
    string? ReceiverPhone,
    string? ReceiverName,
    string? ReceiverAddress,
    int? PackageNo,
    int? IsReturnDoc,
    string? Note,
    int? IsInstallation);

public sealed record BillSummary(
    int BillId,
    string BillCode,
    string? RefCode,
    int StatusId,
    decimal CodAmount,
    int ServiceId,
    int PaymentMethod,
    DateTimeOffset CreatedAt,
    decimal MainFee,
    decimal TotalFee,
    string ReceiverName,
    string ReceiverPhone,
    string ReceiverAddress,
    int PackageNo,
    double Weight,
    string? CargoContent,
    double CargoValue,
    string? Note);

public sealed record CalcFeeInput(
    int? PartnerId,
    double Weight,
    double? Width,
    double? Length,
    double? Height,
    int? ServiceId,
    int PaymentMethodId,
    decimal? CodAmount,
    double? CargoValue,
    string? SProvinceId,
    string? SWardId,
    string? RProvinceId,
    string? RWardId);

public sealed record FeeOption(
    double Weight,
    decimal TotalFee,
    decimal MainFee,
    decimal InsurFee,
    decimal RemoteFee,
    decimal CodFee,
    int ServiceId,
    string ServiceName,
    string LeadTime);

public sealed record CancelResult(string DoCode, string Message);
public sealed record RevertResult(IReadOnlyList<string> Success, IReadOnlyList<string> Failed);

public interface IBillService
{
    Task<BillSummary> CreateAsync(CreateBillInput input, CancellationToken ct);
    Task<BillSummary?> UpdateAsync(UpdateBillInput input, CancellationToken ct);
    Task<BillSummary?> GetByCodeAsync(string billCode, CancellationToken ct);
    Task<IReadOnlyList<FeeOption>> CalcFeeAsync(CalcFeeInput input, CancellationToken ct);
    Task<IReadOnlyList<CancelResult>> CancelAsync(IReadOnlyList<string> billCodes, CancellationToken ct);
    Task<RevertResult> RevertAsync(IReadOnlyList<string> billCodes, CancellationToken ct);
    // Used by AdminPortal + /sandbox route; returns null if bill not found.
    Task<BillSummary?> SetStatusAsync(string billCode, int statusId, string? reason, CancellationToken ct);
}
```

- [ ] **Step 5: Create webhook contracts**

`CodeSandBox/src/NhatTinSandbox.Application/Webhooks/WebhookContracts.cs`:

```csharp
namespace NhatTinSandbox.Application.Webhooks;

// Field set from NhatTinAPIDocumentation/vi/bill/webhook.md.
public sealed record WebhookPayload(
    double Weight,
    string BillNo,
    long StatusTime,
    decimal ShippingFee,
    int IsPartial,
    string StatusName,
    int StatusId,
    double DimensionWeight,
    double Length,
    double Width,
    double Height,
    long PushTime,
    string? RefCode,
    string ExpectedAt,
    string? Reason);

public interface IWebhookDispatcher
{
    // Sends the current status of the bill to all active subscriptions and logs each attempt.
    Task DispatchAsync(int billId, CancellationToken ct);
}
```

- [ ] **Step 6: Build and commit**

Run: `dotnet build CodeSandBox\src\NhatTinSandbox.Application`
Expected: Build succeeded.

```powershell
git add CodeSandBox\src\NhatTinSandbox.Application
git commit -m "feat(application): add envelope and service contracts"
```

---

### Task 3: EF Core context, SQLite, seed data, migration

**Files:**
- Create: `CodeSandBox/src/NhatTinSandbox.Infrastructure/Persistence/SandboxDbContext.cs`
- Create: `CodeSandBox/src/NhatTinSandbox.Infrastructure/Persistence/SeedData.cs`
- Create: `CodeSandBox/src/NhatTinSandbox.Infrastructure/Persistence/SandboxDbContextFactory.cs`
- Test: `CodeSandBox/tests/NhatTinSandbox.Tests/SeedDataTests.cs`

**Interfaces:**
- Produces: `SandboxDbContext` with `DbSet`s (`Bills`, `BillStatusHistories`, `PartnerAccounts`, `RefreshTokens`, `WebhookSubscriptions`, `WebhookDeliveryLogs`, `Locations`, `MasterData`); `SeedData.EnsureSeeded(SandboxDbContext)`.

- [ ] **Step 1: Create the DbContext**

`CodeSandBox/src/NhatTinSandbox.Infrastructure/Persistence/SandboxDbContext.cs`:

```csharp
using Microsoft.EntityFrameworkCore;
using NhatTinSandbox.Domain.Entities;

namespace NhatTinSandbox.Infrastructure.Persistence;

public class SandboxDbContext : DbContext
{
    public SandboxDbContext(DbContextOptions<SandboxDbContext> options) : base(options) { }

    public DbSet<Bill> Bills => Set<Bill>();
    public DbSet<BillStatusHistory> BillStatusHistories => Set<BillStatusHistory>();
    public DbSet<PartnerAccount> PartnerAccounts => Set<PartnerAccount>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<WebhookSubscription> WebhookSubscriptions => Set<WebhookSubscription>();
    public DbSet<WebhookDeliveryLog> WebhookDeliveryLogs => Set<WebhookDeliveryLog>();
    public DbSet<Location> Locations => Set<Location>();
    public DbSet<MasterDataItem> MasterData => Set<MasterDataItem>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<Bill>().HasIndex(x => x.BillCode).IsUnique();
        b.Entity<Bill>().Property(x => x.CodAmount).HasConversion<double>();
        b.Entity<Bill>().Property(x => x.CargoValue);
        b.Entity<Bill>().Property(x => x.MainFee).HasConversion<double>();
        b.Entity<Bill>().Property(x => x.TotalFee).HasConversion<double>();
        b.Entity<Bill>().Property(x => x.OtherFee).HasConversion<double>();
        b.Entity<BillStatusHistory>().Property(x => x.ShippingFee).HasConversion<double>();
        b.Entity<WebhookDeliveryLog>().HasIndex(x => x.BillCode);
        b.Entity<Location>().HasIndex(x => new { x.Kind, x.Code });
        b.Entity<MasterDataItem>().HasIndex(x => new { x.Kind, x.Code });
    }
}
```

> Note: SQLite has no native `decimal`; the `HasConversion<double>()` calls store money/fee as doubles. Acceptable for a sandbox.

- [ ] **Step 2: Create the design-time factory (for migrations)**

`CodeSandBox/src/NhatTinSandbox.Infrastructure/Persistence/SandboxDbContextFactory.cs`:

```csharp
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace NhatTinSandbox.Infrastructure.Persistence;

public class SandboxDbContextFactory : IDesignTimeDbContextFactory<SandboxDbContext>
{
    public SandboxDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<SandboxDbContext>()
            .UseSqlite("Data Source=design-time.db")
            .Options;
        return new SandboxDbContext(options);
    }
}
```

- [ ] **Step 3: Create seed data**

`CodeSandBox/src/NhatTinSandbox.Infrastructure/Persistence/SeedData.cs`:

```csharp
using System.Security.Cryptography;
using System.Text;
using NhatTinSandbox.Domain.Entities;

namespace NhatTinSandbox.Infrastructure.Persistence;

public static class SeedData
{
    // Deterministic demo password hash helper (SHA-256). Sandbox only.
    public static string Hash(string value)
        => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)));

    public static void EnsureSeeded(SandboxDbContext db)
    {
        if (!db.PartnerAccounts.Any())
        {
            db.PartnerAccounts.Add(new PartnerAccount
            {
                Username = "sandbox",
                PasswordHash = Hash("sandbox123"),
                PartnerId = 123736,
                IsActive = true
            });
        }

        if (!db.WebhookSubscriptions.Any())
        {
            db.WebhookSubscriptions.Add(new WebhookSubscription
            {
                PartnerId = 123736,
                CallbackUrl = "http://localhost:5099/webhooks/nhattin/status",
                IsActive = true
            });
        }

        if (!db.MasterData.Any())
        {
            db.MasterData.AddRange(
                new MasterDataItem { Kind = MasterDataKind.Service, Code = 90, Name = "Giao hàng nhanh (CPN)" },
                new MasterDataItem { Kind = MasterDataKind.Service, Code = 81, Name = "Hỏa tốc" },
                new MasterDataItem { Kind = MasterDataKind.Service, Code = 91, Name = "Tiết kiệm" },
                new MasterDataItem { Kind = MasterDataKind.Service, Code = 21, Name = "Hỗn hợp MES" },
                new MasterDataItem { Kind = MasterDataKind.PaymentMethod, Code = 10, Name = "Người gửi thanh toán ngay" },
                new MasterDataItem { Kind = MasterDataKind.PaymentMethod, Code = 11, Name = "Người gửi thanh toán sau" },
                new MasterDataItem { Kind = MasterDataKind.PaymentMethod, Code = 20, Name = "Người nhận thanh toán ngay" },
                new MasterDataItem { Kind = MasterDataKind.CargoType, Code = 1, Name = "Chứng từ" },
                new MasterDataItem { Kind = MasterDataKind.CargoType, Code = 2, Name = "Hàng hóa" },
                new MasterDataItem { Kind = MasterDataKind.CargoType, Code = 3, Name = "Hàng lạnh" },
                new MasterDataItem { Kind = MasterDataKind.CargoType, Code = 4, Name = "Sinh phẩm" },
                new MasterDataItem { Kind = MasterDataKind.CargoType, Code = 5, Name = "Mẫu bệnh phẩm" },
                new MasterDataItem { Kind = MasterDataKind.BillStatus, Code = 1, Name = "Chưa thành công", StatusCode = "Waiting" },
                new MasterDataItem { Kind = MasterDataKind.BillStatus, Code = 2, Name = "Chờ lấy hàng", StatusCode = "Waiting" },
                new MasterDataItem { Kind = MasterDataKind.BillStatus, Code = 3, Name = "Đã lấy hàng", StatusCode = "KCB" },
                new MasterDataItem { Kind = MasterDataKind.BillStatus, Code = 4, Name = "Đã giao hàng", StatusCode = "FBC" },
                new MasterDataItem { Kind = MasterDataKind.BillStatus, Code = 6, Name = "Hủy", StatusCode = "GBV" },
                new MasterDataItem { Kind = MasterDataKind.BillStatus, Code = 7, Name = "Không phát được", StatusCode = "FUD" },
                new MasterDataItem { Kind = MasterDataKind.BillStatus, Code = 9, Name = "Đang chuyển hoàn", StatusCode = "NRT" },
                new MasterDataItem { Kind = MasterDataKind.BillStatus, Code = 10, Name = "Đã chuyển hoàn", StatusCode = "MRC" },
                new MasterDataItem { Kind = MasterDataKind.BillStatus, Code = 11, Name = "Sự cố giao hàng", StatusCode = "QIU" },
                new MasterDataItem { Kind = MasterDataKind.BillStatus, Code = 12, Name = "Vận đơn nháp", StatusCode = "DRF" },
                new MasterDataItem { Kind = MasterDataKind.BillStatus, Code = 13, Name = "Đang giao hàng", StatusCode = "DEL" },
                new MasterDataItem { Kind = MasterDataKind.BillStatus, Code = 15, Name = "Đang vận chuyển" },
                new MasterDataItem { Kind = MasterDataKind.BillStatus, Code = 16, Name = "Đang giao hàng hoàn" },
                new MasterDataItem { Kind = MasterDataKind.BillStatus, Code = 17, Name = "Lỗi lấy hàng" });
        }

        if (!db.Locations.Any())
        {
            // Representative sample only (not the full national catalog).
            // Codes chosen from doc examples: provinces 01/79/11, wards 00004/25750/27007.
            db.Locations.AddRange(
                new Location { Kind = LocationKind.Province, Code = "01", Name = "Hà Nội", IsNew = true },
                new Location { Kind = LocationKind.Province, Code = "79", Name = "Hồ Chí Minh", IsNew = true },
                new Location { Kind = LocationKind.Province, Code = "11", Name = "Cao Bằng", IsNew = true },
                new Location { Kind = LocationKind.District, Code = "0101", Name = "Quận Ba Đình", ParentCode = "01", IsNew = false },
                new Location { Kind = LocationKind.District, Code = "7901", Name = "Quận 1", ParentCode = "79", IsNew = false },
                new Location { Kind = LocationKind.District, Code = "1101", Name = "TP.Cao Bằng", ParentCode = "11", IsNew = false },
                new Location { Kind = LocationKind.Ward, Code = "00004", Name = "Phường Ba Đình", ParentCode = "01", DistrictCode = "0101", IsNew = true },
                new Location { Kind = LocationKind.Ward, Code = "27007", Name = "Phường Bến Nghé", ParentCode = "79", DistrictCode = "7901", IsNew = true },
                new Location { Kind = LocationKind.Ward, Code = "25750", Name = "Phường Sài Gòn", ParentCode = "79", DistrictCode = "7901", IsNew = true });
        }

        db.SaveChanges();
    }
}
```

- [ ] **Step 4: Write the seed test**

`CodeSandBox/tests/NhatTinSandbox.Tests/SeedDataTests.cs`:

```csharp
using Microsoft.EntityFrameworkCore;
using NhatTinSandbox.Domain.Entities;
using NhatTinSandbox.Infrastructure.Persistence;
using Xunit;

namespace NhatTinSandbox.Tests;

public sealed class SeedDataTests
{
    private static SandboxDbContext NewInMemorySqlite()
    {
        var conn = new Microsoft.Data.Sqlite.SqliteConnection("Data Source=:memory:");
        conn.Open();
        var options = new DbContextOptionsBuilder<SandboxDbContext>().UseSqlite(conn).Options;
        var db = new SandboxDbContext(options);
        db.Database.EnsureCreated();
        return db;
    }

    [Fact]
    public void EnsureSeeded_IsIdempotent_AndSeedsDemoAccount()
    {
        using var db = NewInMemorySqlite();

        SeedData.EnsureSeeded(db);
        SeedData.EnsureSeeded(db); // second call must not duplicate

        Assert.Single(db.PartnerAccounts);
        Assert.Equal("sandbox", db.PartnerAccounts.Single().Username);
        Assert.Single(db.WebhookSubscriptions);
        Assert.Contains(db.Locations, l => l.Kind == LocationKind.Province && l.Code == "79");
        Assert.Contains(db.MasterData, m => m.Kind == MasterDataKind.Service && m.Code == 91);
    }
}
```

- [ ] **Step 5: Run test to verify it fails**

Run: `dotnet test CodeSandBox\tests\NhatTinSandbox.Tests --filter SeedDataTests`
Expected: FAIL (or compile error) because `SandboxDbContext`/`SeedData` are not yet referenced by the test project... they are — so it should compile and PASS. If it PASSES immediately that's acceptable (this task's code already exists before the test). If red, fix and continue.

> TDD note: because seed + context are created together here, run the test after Step 4; treat PASS as the gate.

- [ ] **Step 6: Create the initial migration**

```powershell
dotnet tool install --global dotnet-ef --version 8.0.10
dotnet ef migrations add InitialCreate `
  --project CodeSandBox\src\NhatTinSandbox.Infrastructure `
  --startup-project CodeSandBox\src\NhatTinSandbox.Infrastructure `
  --output-dir Persistence\Migrations
```

Expected: A migration is generated under `Persistence/Migrations`.

- [ ] **Step 7: Run test to verify it passes**

Run: `dotnet test CodeSandBox\tests\NhatTinSandbox.Tests --filter SeedDataTests`
Expected: PASS.

- [ ] **Step 8: Commit**

```powershell
git add CodeSandBox
git commit -m "feat(infra): add EF Core SQLite context, seed data, initial migration"
```

---

### Task 4: JWT auth token service (TDD)

**Files:**
- Create: `CodeSandBox/src/NhatTinSandbox.Infrastructure/Auth/JwtOptions.cs`
- Create: `CodeSandBox/src/NhatTinSandbox.Infrastructure/Auth/JwtAuthTokenService.cs`
- Test: `CodeSandBox/tests/NhatTinSandbox.Tests/AuthTokenServiceTests.cs`

**Interfaces:**
- Consumes: `IAuthTokenService`, `AuthTokenResult`, `SandboxDbContext`, `SeedData.Hash`.
- Produces: `JwtOptions { string Issuer; string Audience; string SigningKey; int AccessTtlSeconds; int RefreshTtlSeconds; }`; `JwtAuthTokenService : IAuthTokenService`.

- [ ] **Step 1: Write the failing test**

`CodeSandBox/tests/NhatTinSandbox.Tests/AuthTokenServiceTests.cs`:

```csharp
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using NhatTinSandbox.Infrastructure.Auth;
using NhatTinSandbox.Infrastructure.Persistence;
using Xunit;

namespace NhatTinSandbox.Tests;

public sealed class AuthTokenServiceTests
{
    private static SandboxDbContext NewDb()
    {
        var conn = new Microsoft.Data.Sqlite.SqliteConnection("Data Source=:memory:");
        conn.Open();
        var options = new DbContextOptionsBuilder<SandboxDbContext>().UseSqlite(conn).Options;
        var db = new SandboxDbContext(options);
        db.Database.EnsureCreated();
        SeedData.EnsureSeeded(db);
        return db;
    }

    private static JwtAuthTokenService NewService(SandboxDbContext db)
    {
        var opt = Options.Create(new JwtOptions
        {
            Issuer = "nhattin-sandbox",
            Audience = "nhattin-sandbox",
            SigningKey = "sandbox-signing-key-please-change-0123456789",
            AccessTtlSeconds = 900,
            RefreshTtlSeconds = 3600
        });
        return new JwtAuthTokenService(db, opt);
    }

    [Fact]
    public async Task SignIn_WithSeededDemoAccount_ReturnsBearerTokenPair()
    {
        using var db = NewDb();
        var svc = NewService(db);

        var result = await svc.SignInAsync("sandbox", "sandbox123", CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal("Bearer", result!.TokenType);
        Assert.False(string.IsNullOrWhiteSpace(result.JwtToken));
        Assert.False(string.IsNullOrWhiteSpace(result.RefreshToken));
        Assert.Equal(900, result.TokenExpiresInSeconds);
        Assert.True(result.RefreshExpiresInSeconds > result.TokenExpiresInSeconds);
    }

    [Fact]
    public async Task SignIn_WithWrongPassword_ReturnsNull()
    {
        using var db = NewDb();
        var svc = NewService(db);

        var result = await svc.SignInAsync("sandbox", "wrong", CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task Refresh_WithIssuedToken_ReturnsNewPair()
    {
        using var db = NewDb();
        var svc = NewService(db);
        var first = await svc.SignInAsync("sandbox", "sandbox123", CancellationToken.None);

        var refreshed = await svc.RefreshAsync(first!.RefreshToken, CancellationToken.None);

        Assert.NotNull(refreshed);
        Assert.False(string.IsNullOrWhiteSpace(refreshed!.JwtToken));
    }

    [Fact]
    public async Task Refresh_WithUnknownToken_ReturnsNull()
    {
        using var db = NewDb();
        var svc = NewService(db);

        var refreshed = await svc.RefreshAsync("not-a-real-token", CancellationToken.None);

        Assert.Null(refreshed);
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test CodeSandBox\tests\NhatTinSandbox.Tests --filter AuthTokenServiceTests`
Expected: FAIL — `JwtOptions`/`JwtAuthTokenService` do not exist.

- [ ] **Step 3: Create JwtOptions**

`CodeSandBox/src/NhatTinSandbox.Infrastructure/Auth/JwtOptions.cs`:

```csharp
namespace NhatTinSandbox.Infrastructure.Auth;

public sealed class JwtOptions
{
    public const string SectionName = "Jwt";
    public string Issuer { get; set; } = "nhattin-sandbox";
    public string Audience { get; set; } = "nhattin-sandbox";
    public string SigningKey { get; set; } = "";
    public int AccessTtlSeconds { get; set; } = 900;
    public int RefreshTtlSeconds { get; set; } = 3600;
}
```

- [ ] **Step 4: Implement JwtAuthTokenService**

`CodeSandBox/src/NhatTinSandbox.Infrastructure/Auth/JwtAuthTokenService.cs`:

```csharp
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using NhatTinSandbox.Application.Auth;
using NhatTinSandbox.Domain.Entities;
using NhatTinSandbox.Infrastructure.Persistence;

namespace NhatTinSandbox.Infrastructure.Auth;

public sealed class JwtAuthTokenService : IAuthTokenService
{
    private readonly SandboxDbContext _db;
    private readonly JwtOptions _opt;

    public JwtAuthTokenService(SandboxDbContext db, IOptions<JwtOptions> opt)
    {
        _db = db;
        _opt = opt.Value;
    }

    public async Task<AuthTokenResult?> SignInAsync(string username, string password, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            return null;

        var hash = SeedData.Hash(password);
        var account = await _db.PartnerAccounts
            .FirstOrDefaultAsync(a => a.Username == username && a.IsActive, ct);

        if (account is null || account.PasswordHash != hash)
            return null;

        return await IssueAsync(account, ct);
    }

    public async Task<AuthTokenResult?> RefreshAsync(string refreshToken, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(refreshToken))
            return null;

        var tokenHash = SeedData.Hash(refreshToken);
        var stored = await _db.RefreshTokens
            .FirstOrDefaultAsync(t => t.TokenHash == tokenHash && !t.IsRevoked, ct);

        if (stored is null || stored.ExpiresAt <= DateTimeOffset.UtcNow)
            return null;

        stored.IsRevoked = true; // rotate
        var account = await _db.PartnerAccounts.FirstAsync(a => a.Id == stored.AccountId, ct);
        return await IssueAsync(account, ct);
    }

    private async Task<AuthTokenResult> IssueAsync(PartnerAccount account, CancellationToken ct)
    {
        var jwt = BuildJwt(account);
        var refresh = Convert.ToBase64String(RandomNumberGenerator.GetBytes(48));

        _db.RefreshTokens.Add(new RefreshToken
        {
            AccountId = account.Id,
            TokenHash = SeedData.Hash(refresh),
            ExpiresAt = DateTimeOffset.UtcNow.AddSeconds(_opt.RefreshTtlSeconds),
            IsRevoked = false
        });
        await _db.SaveChangesAsync(ct);

        return new AuthTokenResult(
            JwtToken: jwt,
            TokenType: "Bearer",
            TokenExpiresInSeconds: _opt.AccessTtlSeconds,
            RefreshToken: refresh,
            RefreshExpiresInSeconds: _opt.RefreshTtlSeconds);
    }

    private string BuildJwt(PartnerAccount account)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_opt.SigningKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, account.Id.ToString()),
            new Claim("partner_id", account.PartnerId.ToString()),
            new Claim("username", account.Username)
        };
        var token = new JwtSecurityToken(
            issuer: _opt.Issuer,
            audience: _opt.Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddSeconds(_opt.AccessTtlSeconds),
            signingCredentials: creds);
        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
```

- [ ] **Step 5: Run test to verify it passes**

Run: `dotnet test CodeSandBox\tests\NhatTinSandbox.Tests --filter AuthTokenServiceTests`
Expected: PASS (4 tests).

- [ ] **Step 6: Commit**

```powershell
git add CodeSandBox
git commit -m "feat(infra): JWT auth token service with rotation"
```

---

### Task 5: Location catalog service (TDD)

**Files:**
- Create: `CodeSandBox/src/NhatTinSandbox.Infrastructure/Locations/LocationCatalog.cs`
- Test: `CodeSandBox/tests/NhatTinSandbox.Tests/LocationCatalogTests.cs`

**Interfaces:**
- Consumes: `ILocationCatalog`, `ProvinceDto`, `DistrictDto`, `WardDto`, `SandboxDbContext`.
- Produces: `LocationCatalog : ILocationCatalog`.

- [ ] **Step 1: Write the failing test**

`CodeSandBox/tests/NhatTinSandbox.Tests/LocationCatalogTests.cs`:

```csharp
using Microsoft.EntityFrameworkCore;
using NhatTinSandbox.Infrastructure.Locations;
using NhatTinSandbox.Infrastructure.Persistence;
using Xunit;

namespace NhatTinSandbox.Tests;

public sealed class LocationCatalogTests
{
    private static SandboxDbContext NewDb()
    {
        var conn = new Microsoft.Data.Sqlite.SqliteConnection("Data Source=:memory:");
        conn.Open();
        var options = new DbContextOptionsBuilder<SandboxDbContext>().UseSqlite(conn).Options;
        var db = new SandboxDbContext(options);
        db.Database.EnsureCreated();
        SeedData.EnsureSeeded(db);
        return db;
    }

    [Fact]
    public async Task GetProvinces_ReturnsSeededProvinces_WithIsNewFlag()
    {
        using var db = NewDb();
        var catalog = new LocationCatalog(db);

        var provinces = await catalog.GetProvincesAsync(isNew: true, CancellationToken.None);

        Assert.Contains(provinces, p => p.Id == "79" && p.ProvinceName == "Hồ Chí Minh" && p.IsNew == "Y");
    }

    [Fact]
    public async Task GetWards_ByProvince_FiltersByProvinceCode()
    {
        using var db = NewDb();
        var catalog = new LocationCatalog(db);

        var wards = await catalog.GetWardsAsync(districtId: null, provinceId: "79", isNew: true, CancellationToken.None);

        Assert.All(wards, w => Assert.False(string.IsNullOrEmpty(w.WardName)));
        Assert.Contains(wards, w => w.Id == "27007");
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test CodeSandBox\tests\NhatTinSandbox.Tests --filter LocationCatalogTests`
Expected: FAIL — `LocationCatalog` does not exist.

- [ ] **Step 3: Implement LocationCatalog**

`CodeSandBox/src/NhatTinSandbox.Infrastructure/Locations/LocationCatalog.cs`:

```csharp
using Microsoft.EntityFrameworkCore;
using NhatTinSandbox.Application.Locations;
using NhatTinSandbox.Domain.Entities;
using NhatTinSandbox.Infrastructure.Persistence;

namespace NhatTinSandbox.Infrastructure.Locations;

public sealed class LocationCatalog : ILocationCatalog
{
    private readonly SandboxDbContext _db;
    public LocationCatalog(SandboxDbContext db) => _db = db;

    private static string Flag(bool isNew) => isNew ? "Y" : "N";

    public async Task<IReadOnlyList<ProvinceDto>> GetProvincesAsync(bool isNew, CancellationToken ct)
    {
        var rows = await _db.Locations
            .Where(l => l.Kind == LocationKind.Province && l.IsNew == isNew)
            .OrderBy(l => l.Code)
            .ToListAsync(ct);
        return rows.Select(l => new ProvinceDto(l.Code, l.Name, Flag(l.IsNew))).ToList();
    }

    public async Task<IReadOnlyList<DistrictDto>> GetDistrictsAsync(string provinceId, CancellationToken ct)
    {
        var rows = await _db.Locations
            .Where(l => l.Kind == LocationKind.District && l.ParentCode == provinceId)
            .OrderBy(l => l.Code)
            .ToListAsync(ct);
        return rows.Select(l => new DistrictDto(l.Code, l.Name, Flag(l.IsNew))).ToList();
    }

    public async Task<IReadOnlyList<WardDto>> GetWardsAsync(string? districtId, string? provinceId, bool isNew, CancellationToken ct)
    {
        var query = _db.Locations.Where(l => l.Kind == LocationKind.Ward);
        if (!string.IsNullOrEmpty(districtId))
            query = query.Where(l => l.DistrictCode == districtId);
        if (!string.IsNullOrEmpty(provinceId))
            query = query.Where(l => l.ParentCode == provinceId);

        var rows = await query.OrderBy(l => l.Code).ToListAsync(ct);
        return rows.Select(l => new WardDto(l.Code, l.Name, Flag(l.IsNew))).ToList();
    }
}
```

- [ ] **Step 4: Run test to verify it passes**

Run: `dotnet test CodeSandBox\tests\NhatTinSandbox.Tests --filter LocationCatalogTests`
Expected: PASS.

- [ ] **Step 5: Commit**

```powershell
git add CodeSandBox
git commit -m "feat(infra): location catalog over seeded sample data"
```

---

### Task 6: Bill service — create, get, set-status (TDD)

**Files:**
- Create: `CodeSandBox/src/NhatTinSandbox.Infrastructure/Bills/BillService.cs`
- Test: `CodeSandBox/tests/NhatTinSandbox.Tests/BillServiceTests.cs`

**Interfaces:**
- Consumes: `IBillService`, `CreateBillInput`, `UpdateBillInput`, `BillSummary`, `CalcFeeInput`, `FeeOption`, `CancelResult`, `RevertResult`, `SandboxDbContext`, `BillStatusId`.
- Produces: `BillService : IBillService`. This task implements `CreateAsync`, `GetByCodeAsync`, `SetStatusAsync`; Task 7 fills `CalcFeeAsync`, `UpdateAsync`, `CancelAsync`, `RevertAsync` on the same class.

- [ ] **Step 1: Write the failing test**

`CodeSandBox/tests/NhatTinSandbox.Tests/BillServiceTests.cs`:

```csharp
using Microsoft.EntityFrameworkCore;
using NhatTinSandbox.Application.Bills;
using NhatTinSandbox.Infrastructure.Bills;
using NhatTinSandbox.Infrastructure.Persistence;
using Xunit;

namespace NhatTinSandbox.Tests;

public sealed class BillServiceTests
{
    private static SandboxDbContext NewDb()
    {
        var conn = new Microsoft.Data.Sqlite.SqliteConnection("Data Source=:memory:");
        conn.Open();
        var options = new DbContextOptionsBuilder<SandboxDbContext>().UseSqlite(conn).Options;
        var db = new SandboxDbContext(options);
        db.Database.EnsureCreated();
        SeedData.EnsureSeeded(db);
        return db;
    }

    private static CreateBillInput SampleInput() => new(
        RefCode: "TP-001", PackageNo: 1, Weight: 2, Width: 0, Length: 0, Height: 0,
        CargoContent: "Hàng dễ vỡ", ServiceId: 91, PaymentMethodId: 10, IsReturnDoc: 0,
        CodAmount: 120000, Note: null, CargoValue: 0, CargoTypeId: 2,
        SName: "TruePos", SPhone: "0333333333", SAddress: "số 10", SProvinceCode: "01", SWardCode: "00004",
        RName: "Nguyễn Văn A", RPhone: "0911111111", RAddress: "123", RProvinceCode: "79", RWardCode: "27007",
        IsDraft: 0, OtherFee: 0, IsInstallation: 0, BillType: 1, BillReturn: null);

    [Fact]
    public async Task Create_AssignsBillCodeStartingWithCP_AndStatus2()
    {
        using var db = NewDb();
        var svc = new BillService(db);

        var summary = await svc.CreateAsync(SampleInput(), CancellationToken.None);

        Assert.StartsWith("CP", summary.BillCode);
        Assert.Equal(2, summary.StatusId);
        Assert.Equal("TP-001", summary.RefCode);
        Assert.True(summary.TotalFee > 0);
    }

    [Fact]
    public async Task Create_DraftBill_StartsAtStatus12()
    {
        using var db = NewDb();
        var svc = new BillService(db);

        var draft = SampleInput() with { IsDraft = 1 };
        var summary = await svc.CreateAsync(draft, CancellationToken.None);

        Assert.Equal(12, summary.StatusId);
    }

    [Fact]
    public async Task GetByCode_ReturnsCreatedBill()
    {
        using var db = NewDb();
        var svc = new BillService(db);
        var created = await svc.CreateAsync(SampleInput(), CancellationToken.None);

        var found = await svc.GetByCodeAsync(created.BillCode, CancellationToken.None);

        Assert.NotNull(found);
        Assert.Equal(created.BillCode, found!.BillCode);
    }

    [Fact]
    public async Task SetStatus_ChangesStatus_AndRecordsHistory()
    {
        using var db = NewDb();
        var svc = new BillService(db);
        var created = await svc.CreateAsync(SampleInput(), CancellationToken.None);

        var updated = await svc.SetStatusAsync(created.BillCode, 3, "Đã lấy hàng", CancellationToken.None);

        Assert.NotNull(updated);
        Assert.Equal(3, updated!.StatusId);
        var bill = db.Bills.Single(x => x.BillCode == created.BillCode);
        Assert.Contains(db.BillStatusHistories, h => h.BillId == bill.Id && h.StatusId == 3);
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test CodeSandBox\tests\NhatTinSandbox.Tests --filter BillServiceTests`
Expected: FAIL — `BillService` does not exist.

- [ ] **Step 3: Implement BillService (create/get/set-status + fee helper)**

`CodeSandBox/src/NhatTinSandbox.Infrastructure/Bills/BillService.cs`:

```csharp
using Microsoft.EntityFrameworkCore;
using NhatTinSandbox.Application.Bills;
using NhatTinSandbox.Domain.Entities;
using NhatTinSandbox.Infrastructure.Persistence;

namespace NhatTinSandbox.Infrastructure.Bills;

public sealed class BillService : IBillService
{
    private readonly SandboxDbContext _db;
    public BillService(SandboxDbContext db) => _db = db;

    // Deterministic, made-up sandbox fee formula (NOT the real Nhất Tín price table).
    internal static decimal CalcMainFee(double weight, decimal codAmount)
    {
        var weightUnits = Math.Ceiling(weight <= 0 ? 1 : weight);
        var codSurcharge = codAmount > 0 ? 5000m : 0m;
        return 18000m + (decimal)weightUnits * 3000m + codSurcharge;
    }

    private static string NewBillCode()
        => "CP" + DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString();

    private static BillSummary ToSummary(Bill b) => new(
        BillId: b.Id, BillCode: b.BillCode, RefCode: b.RefCode, StatusId: b.StatusId,
        CodAmount: b.CodAmount, ServiceId: b.ServiceId, PaymentMethod: b.PaymentMethodId,
        CreatedAt: b.CreatedAt, MainFee: b.MainFee, TotalFee: b.TotalFee,
        ReceiverName: b.ReceiverName, ReceiverPhone: b.ReceiverPhone, ReceiverAddress: b.ReceiverAddress,
        PackageNo: b.PackageNo, Weight: b.Weight, CargoContent: b.CargoContent,
        CargoValue: b.CargoValue, Note: b.Note);

    public async Task<BillSummary> CreateAsync(CreateBillInput input, CancellationToken ct)
    {
        var mainFee = CalcMainFee(input.Weight, input.CodAmount ?? 0);
        var otherFee = input.OtherFee ?? 0;
        var bill = new Bill
        {
            BillCode = NewBillCode(),
            RefCode = input.RefCode,
            PackageNo = input.PackageNo ?? 1,
            Weight = input.Weight,
            Width = input.Width,
            Length = input.Length,
            Height = input.Height,
            CargoContent = input.CargoContent,
            ServiceId = input.ServiceId,
            PaymentMethodId = input.PaymentMethodId,
            IsReturnDoc = input.IsReturnDoc ?? 0,
            CodAmount = input.CodAmount ?? 0,
            Note = input.Note,
            CargoValue = input.CargoValue ?? 0,
            CargoTypeId = input.CargoTypeId,
            SenderName = input.SName,
            SenderPhone = input.SPhone,
            SenderAddress = input.SAddress,
            SenderProvinceCode = input.SProvinceCode,
            SenderWardCode = input.SWardCode,
            ReceiverName = input.RName,
            ReceiverPhone = input.RPhone,
            ReceiverAddress = input.RAddress,
            ReceiverProvinceCode = input.RProvinceCode,
            ReceiverWardCode = input.RWardCode,
            IsDraft = input.IsDraft ?? 0,
            OtherFee = otherFee,
            IsInstallation = input.IsInstallation ?? 0,
            BillType = input.BillType ?? 1,
            BillReturn = input.BillReturn,
            StatusId = (input.IsDraft ?? 0) == 1 ? (int)BillStatusId.VanDonNhap : (int)BillStatusId.ChoLayHang,
            MainFee = mainFee,
            TotalFee = mainFee + otherFee,
            CreatedAt = DateTimeOffset.UtcNow,
            ExpectedAt = DateTimeOffset.UtcNow.AddDays(3)
        };

        _db.Bills.Add(bill);
        await _db.SaveChangesAsync(ct);

        _db.BillStatusHistories.Add(new BillStatusHistory
        {
            BillId = bill.Id,
            StatusId = bill.StatusId,
            StatusName = StatusName(bill.StatusId),
            ShippingFee = bill.TotalFee,
            ChangedAt = DateTimeOffset.UtcNow
        });
        await _db.SaveChangesAsync(ct);

        return ToSummary(bill);
    }

    public async Task<BillSummary?> GetByCodeAsync(string billCode, CancellationToken ct)
    {
        var bill = await _db.Bills.FirstOrDefaultAsync(b => b.BillCode == billCode, ct);
        return bill is null ? null : ToSummary(bill);
    }

    public async Task<BillSummary?> SetStatusAsync(string billCode, int statusId, string? reason, CancellationToken ct)
    {
        var bill = await _db.Bills.FirstOrDefaultAsync(b => b.BillCode == billCode, ct);
        if (bill is null) return null;

        bill.StatusId = statusId;
        _db.BillStatusHistories.Add(new BillStatusHistory
        {
            BillId = bill.Id,
            StatusId = statusId,
            StatusName = StatusName(statusId),
            Reason = reason,
            ShippingFee = bill.TotalFee,
            ChangedAt = DateTimeOffset.UtcNow
        });
        await _db.SaveChangesAsync(ct);
        return ToSummary(bill);
    }

    private string StatusName(int statusId)
        => _db.MasterData
            .Where(m => m.Kind == MasterDataKind.BillStatus && m.Code == statusId)
            .Select(m => m.Name)
            .FirstOrDefault() ?? $"Status {statusId}";

    // ---- Task 7 fills these ----
    public Task<BillSummary?> UpdateAsync(UpdateBillInput input, CancellationToken ct)
        => throw new NotImplementedException();
    public Task<IReadOnlyList<FeeOption>> CalcFeeAsync(CalcFeeInput input, CancellationToken ct)
        => throw new NotImplementedException();
    public Task<IReadOnlyList<CancelResult>> CancelAsync(IReadOnlyList<string> billCodes, CancellationToken ct)
        => throw new NotImplementedException();
    public Task<RevertResult> RevertAsync(IReadOnlyList<string> billCodes, CancellationToken ct)
        => throw new NotImplementedException();
}
```

- [ ] **Step 4: Run test to verify it passes**

Run: `dotnet test CodeSandBox\tests\NhatTinSandbox.Tests --filter BillServiceTests`
Expected: PASS (4 tests).

- [ ] **Step 5: Commit**

```powershell
git add CodeSandBox
git commit -m "feat(infra): bill create/get/set-status with history"
```

---

### Task 7: Bill service — calc-fee, update, cancel, revert (TDD)

**Files:**
- Modify: `CodeSandBox/src/NhatTinSandbox.Infrastructure/Bills/BillService.cs` (replace the 4 `NotImplementedException` methods)
- Test: `CodeSandBox/tests/NhatTinSandbox.Tests/BillServiceFeeAndOpsTests.cs`

**Interfaces:**
- Consumes: everything from Task 6.
- Produces: working `CalcFeeAsync`, `UpdateAsync`, `CancelAsync`, `RevertAsync`.

- [ ] **Step 1: Write the failing test**

`CodeSandBox/tests/NhatTinSandbox.Tests/BillServiceFeeAndOpsTests.cs`:

```csharp
using Microsoft.EntityFrameworkCore;
using NhatTinSandbox.Application.Bills;
using NhatTinSandbox.Infrastructure.Bills;
using NhatTinSandbox.Infrastructure.Persistence;
using Xunit;

namespace NhatTinSandbox.Tests;

public sealed class BillServiceFeeAndOpsTests
{
    private static SandboxDbContext NewDb()
    {
        var conn = new Microsoft.Data.Sqlite.SqliteConnection("Data Source=:memory:");
        conn.Open();
        var options = new DbContextOptionsBuilder<SandboxDbContext>().UseSqlite(conn).Options;
        var db = new SandboxDbContext(options);
        db.Database.EnsureCreated();
        SeedData.EnsureSeeded(db);
        return db;
    }

    private static CreateBillInput SampleInput(string refCode = "TP-001") => new(
        RefCode: refCode, PackageNo: 1, Weight: 2, Width: 0, Length: 0, Height: 0,
        CargoContent: "Hàng", ServiceId: 91, PaymentMethodId: 10, IsReturnDoc: 0,
        CodAmount: 0, Note: null, CargoValue: 0, CargoTypeId: 2,
        SName: "TruePos", SPhone: "0333333333", SAddress: "số 10", SProvinceCode: "01", SWardCode: "00004",
        RName: "A", RPhone: "0911111111", RAddress: "123", RProvinceCode: "79", RWardCode: "27007",
        IsDraft: 0, OtherFee: 0, IsInstallation: 0, BillType: 1, BillReturn: null);

    [Fact]
    public async Task CalcFee_ReturnsAtLeastOneOption_WithPositiveFee()
    {
        using var db = NewDb();
        var svc = new BillService(db);
        var input = new CalcFeeInput(
            PartnerId: 123736, Weight: 1.3, Width: 0, Length: 0, Height: 0,
            ServiceId: null, PaymentMethodId: 10, CodAmount: 120000, CargoValue: 2000000,
            SProvinceId: "79", SWardId: "27007", RProvinceId: "01", RWardId: "00004");

        var options = await svc.CalcFeeAsync(input, CancellationToken.None);

        Assert.NotEmpty(options);
        Assert.All(options, o => Assert.True(o.TotalFee > 0));
    }

    [Fact]
    public async Task Update_ChangesCod_ReturnsUpdatedSummary()
    {
        using var db = NewDb();
        var svc = new BillService(db);
        var created = await svc.CreateAsync(SampleInput(), CancellationToken.None);

        var updated = await svc.UpdateAsync(new UpdateBillInput(
            PartnerId: 123736, BillCode: created.BillCode, CodAmount: 200000,
            CargoValue: null, Weight: null, Length: null, Height: null, Width: null,
            CargoContent: null, ReceiverPhone: null, ReceiverName: null, ReceiverAddress: null,
            PackageNo: null, IsReturnDoc: 0, Note: "updated", IsInstallation: null), CancellationToken.None);

        Assert.NotNull(updated);
        Assert.Equal(200000, updated!.CodAmount);
    }

    [Fact]
    public async Task Cancel_ReturnsPerCodeResult()
    {
        using var db = NewDb();
        var svc = new BillService(db);
        var created = await svc.CreateAsync(SampleInput(), CancellationToken.None);

        var result = await svc.CancelAsync(new[] { created.BillCode }, CancellationToken.None);

        Assert.Single(result);
        Assert.Equal(created.BillCode, result[0].DoCode);
        var bill = db.Bills.Single(b => b.BillCode == created.BillCode);
        Assert.Equal(6, bill.StatusId); // Hủy
    }

    [Fact]
    public async Task Revert_SplitsSuccessAndFailed()
    {
        using var db = NewDb();
        var svc = new BillService(db);
        var created = await svc.CreateAsync(SampleInput(), CancellationToken.None);
        await svc.SetStatusAsync(created.BillCode, 3, "picked", CancellationToken.None); // eligible

        var result = await svc.RevertAsync(new[] { created.BillCode, "CP-UNKNOWN" }, CancellationToken.None);

        Assert.Contains(created.BillCode, result.Success);
        Assert.Contains("CP-UNKNOWN", result.Failed);
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test CodeSandBox\tests\NhatTinSandbox.Tests --filter BillServiceFeeAndOpsTests`
Expected: FAIL — methods throw `NotImplementedException`.

- [ ] **Step 3: Replace the four stub methods**

In `CodeSandBox/src/NhatTinSandbox.Infrastructure/Bills/BillService.cs`, replace the `// ---- Task 7 fills these ----` block with:

```csharp
    public async Task<BillSummary?> UpdateAsync(UpdateBillInput input, CancellationToken ct)
    {
        var bill = await _db.Bills.FirstOrDefaultAsync(b => b.BillCode == input.BillCode, ct);
        if (bill is null) return null;

        if (input.CodAmount.HasValue) bill.CodAmount = input.CodAmount.Value;
        if (input.CargoValue.HasValue) bill.CargoValue = input.CargoValue.Value;
        if (input.Weight.HasValue) bill.Weight = input.Weight.Value;
        if (input.Length.HasValue) bill.Length = input.Length.Value;
        if (input.Height.HasValue) bill.Height = input.Height.Value;
        if (input.Width.HasValue) bill.Width = input.Width.Value;
        if (input.CargoContent is not null) bill.CargoContent = input.CargoContent;
        if (input.ReceiverPhone is not null) bill.ReceiverPhone = input.ReceiverPhone;
        if (input.ReceiverName is not null) bill.ReceiverName = input.ReceiverName;
        if (input.ReceiverAddress is not null) bill.ReceiverAddress = input.ReceiverAddress;
        if (input.PackageNo.HasValue) bill.PackageNo = input.PackageNo.Value;
        if (input.IsReturnDoc.HasValue) bill.IsReturnDoc = input.IsReturnDoc.Value;
        if (input.Note is not null) bill.Note = input.Note;
        if (input.IsInstallation.HasValue) bill.IsInstallation = input.IsInstallation.Value;

        bill.MainFee = CalcMainFee(bill.Weight, bill.CodAmount);
        bill.TotalFee = bill.MainFee + bill.OtherFee;
        await _db.SaveChangesAsync(ct);
        return ToSummary(bill);
    }

    public async Task<IReadOnlyList<FeeOption>> CalcFeeAsync(CalcFeeInput input, CancellationToken ct)
    {
        // Sandbox approximation: build one option per known service. Not the real price table.
        var services = await _db.MasterData
            .Where(m => m.Kind == MasterDataKind.Service)
            .OrderBy(m => m.Code)
            .ToListAsync(ct);

        var cod = input.CodAmount ?? 0;
        var lead = DateTimeOffset.UtcNow.AddDays(2).ToString("dd/MM/yyyy HH:mm");
        var options = new List<FeeOption>();
        foreach (var s in services)
        {
            if (input.ServiceId.HasValue && input.ServiceId.Value != s.Code) continue;
            var main = CalcMainFee(input.Weight, cod);
            options.Add(new FeeOption(
                Weight: input.Weight,
                TotalFee: main,
                MainFee: main,
                InsurFee: 0,
                RemoteFee: 0,
                CodFee: cod > 0 ? 5000 : 0,
                ServiceId: s.Code,
                ServiceName: s.Name,
                LeadTime: lead));
        }
        return options;
    }

    public async Task<IReadOnlyList<CancelResult>> CancelAsync(IReadOnlyList<string> billCodes, CancellationToken ct)
    {
        var results = new List<CancelResult>();
        foreach (var code in billCodes)
        {
            var bill = await _db.Bills.FirstOrDefaultAsync(b => b.BillCode == code, ct);
            if (bill is null)
            {
                results.Add(new CancelResult(code, $"Bill {code} not found"));
                continue;
            }
            // Doc: cancel allowed for status 1 (Chưa thành công) and 2 (Chờ lấy hàng).
            if (bill.StatusId is 1 or 2)
            {
                bill.StatusId = (int)Domain.Enums.BillStatusId.Huy;
                _db.BillStatusHistories.Add(new BillStatusHistory
                {
                    BillId = bill.Id, StatusId = bill.StatusId, StatusName = StatusName(bill.StatusId),
                    ShippingFee = bill.TotalFee, ChangedAt = DateTimeOffset.UtcNow
                });
                results.Add(new CancelResult(code, $"Bill {code} has canceled successful"));
            }
            else
            {
                results.Add(new CancelResult(code, $"Bill {code} cannot be canceled at status {bill.StatusId}"));
            }
        }
        await _db.SaveChangesAsync(ct);
        return results;
    }

    public async Task<RevertResult> RevertAsync(IReadOnlyList<string> billCodes, CancellationToken ct)
    {
        var success = new List<string>();
        var failed = new List<string>();
        foreach (var code in billCodes)
        {
            var bill = await _db.Bills.FirstOrDefaultAsync(b => b.BillCode == code, ct);
            // Doc: revert allowed for status 3 (Đã lấy hàng), 15 (Đang vận chuyển), 7 (Không phát được).
            if (bill is not null && bill.StatusId is 3 or 15 or 7)
            {
                bill.StatusId = (int)Domain.Enums.BillStatusId.DangChuyenHoan;
                _db.BillStatusHistories.Add(new BillStatusHistory
                {
                    BillId = bill.Id, StatusId = bill.StatusId, StatusName = StatusName(bill.StatusId),
                    ShippingFee = bill.TotalFee, ChangedAt = DateTimeOffset.UtcNow
                });
                success.Add(code);
            }
            else
            {
                failed.Add(code);
            }
        }
        await _db.SaveChangesAsync(ct);
        return new RevertResult(success, failed);
    }
```

- [ ] **Step 4: Run test to verify it passes**

Run: `dotnet test CodeSandBox\tests\NhatTinSandbox.Tests --filter BillServiceFeeAndOpsTests`
Expected: PASS (4 tests).

- [ ] **Step 5: Run the whole test project**

Run: `dotnet test CodeSandBox\tests\NhatTinSandbox.Tests`
Expected: all tests PASS.

- [ ] **Step 6: Commit**

```powershell
git add CodeSandBox
git commit -m "feat(infra): calc-fee/update/cancel/revert bill operations"
```

---

### Task 8: Webhook dispatcher (TDD)

**Files:**
- Create: `CodeSandBox/src/NhatTinSandbox.Infrastructure/Webhooks/HttpWebhookDispatcher.cs`
- Test: `CodeSandBox/tests/NhatTinSandbox.Tests/WebhookDispatcherTests.cs`

**Interfaces:**
- Consumes: `IWebhookDispatcher`, `WebhookPayload`, `SandboxDbContext`, `IHttpClientFactory`.
- Produces: `HttpWebhookDispatcher : IWebhookDispatcher` — reads the bill + latest history, builds a `WebhookPayload` matching `webhook.md`, POSTs JSON to each active subscription, writes a `WebhookDeliveryLog` per attempt.

- [ ] **Step 1: Write the failing test**

`CodeSandBox/tests/NhatTinSandbox.Tests/WebhookDispatcherTests.cs`:

```csharp
using System.Net;
using Microsoft.EntityFrameworkCore;
using NhatTinSandbox.Application.Bills;
using NhatTinSandbox.Infrastructure.Bills;
using NhatTinSandbox.Infrastructure.Persistence;
using NhatTinSandbox.Infrastructure.Webhooks;
using Xunit;

namespace NhatTinSandbox.Tests;

public sealed class WebhookDispatcherTests
{
    private sealed class StubHandler : HttpMessageHandler
    {
        public string? LastBody;
        public HttpStatusCode Status = HttpStatusCode.OK;
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            LastBody = request.Content is null ? null : await request.Content.ReadAsStringAsync(ct);
            return new HttpResponseMessage(Status) { Content = new StringContent("{\"success\":true}") };
        }
    }

    private sealed class StubFactory : IHttpClientFactory
    {
        private readonly HttpMessageHandler _handler;
        public StubFactory(HttpMessageHandler h) => _handler = h;
        public HttpClient CreateClient(string name) => new(_handler, disposeHandler: false);
    }

    private static SandboxDbContext NewDb()
    {
        var conn = new Microsoft.Data.Sqlite.SqliteConnection("Data Source=:memory:");
        conn.Open();
        var options = new DbContextOptionsBuilder<SandboxDbContext>().UseSqlite(conn).Options;
        var db = new SandboxDbContext(options);
        db.Database.EnsureCreated();
        SeedData.EnsureSeeded(db);
        return db;
    }

    private static CreateBillInput SampleInput() => new(
        RefCode: "TP-001", PackageNo: 1, Weight: 2, Width: 1, Length: 1, Height: 1,
        CargoContent: "Hàng", ServiceId: 91, PaymentMethodId: 10, IsReturnDoc: 0,
        CodAmount: 0, Note: null, CargoValue: 0, CargoTypeId: 2,
        SName: "TruePos", SPhone: "0333333333", SAddress: "số 10", SProvinceCode: "01", SWardCode: "00004",
        RName: "A", RPhone: "0911111111", RAddress: "123", RProvinceCode: "79", RWardCode: "27007",
        IsDraft: 0, OtherFee: 0, IsInstallation: 0, BillType: 1, BillReturn: null);

    [Fact]
    public async Task Dispatch_PostsPayloadWithBillNo_AndLogsSuccess()
    {
        using var db = NewDb();
        var billSvc = new BillService(db);
        var bill = await billSvc.CreateAsync(SampleInput(), CancellationToken.None);
        await billSvc.SetStatusAsync(bill.BillCode, 3, "Đã lấy hàng", CancellationToken.None);
        var billEntity = db.Bills.Single(b => b.BillCode == bill.BillCode);

        var handler = new StubHandler();
        var dispatcher = new HttpWebhookDispatcher(db, new StubFactory(handler));

        await dispatcher.DispatchAsync(billEntity.Id, CancellationToken.None);

        Assert.NotNull(handler.LastBody);
        Assert.Contains("\"bill_no\"", handler.LastBody);
        Assert.Contains(bill.BillCode, handler.LastBody);
        Assert.Contains(db.WebhookDeliveryLogs, l => l.BillCode == bill.BillCode && l.Success);
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test CodeSandBox\tests\NhatTinSandbox.Tests --filter WebhookDispatcherTests`
Expected: FAIL — `HttpWebhookDispatcher` does not exist.

- [ ] **Step 3: Implement the dispatcher**

`CodeSandBox/src/NhatTinSandbox.Infrastructure/Webhooks/HttpWebhookDispatcher.cs`:

```csharp
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using NhatTinSandbox.Application.Webhooks;
using NhatTinSandbox.Domain.Entities;
using NhatTinSandbox.Infrastructure.Persistence;

namespace NhatTinSandbox.Infrastructure.Webhooks;

public sealed class HttpWebhookDispatcher : IWebhookDispatcher
{
    private readonly SandboxDbContext _db;
    private readonly IHttpClientFactory _httpFactory;

    public HttpWebhookDispatcher(SandboxDbContext db, IHttpClientFactory httpFactory)
    {
        _db = db;
        _httpFactory = httpFactory;
    }

    public async Task DispatchAsync(int billId, CancellationToken ct)
    {
        var bill = await _db.Bills.FirstOrDefaultAsync(b => b.Id == billId, ct);
        if (bill is null) return;

        var latest = await _db.BillStatusHistories
            .Where(h => h.BillId == billId)
            .OrderByDescending(h => h.Id)
            .FirstOrDefaultAsync(ct);

        var now = DateTimeOffset.UtcNow;
        var payload = new
        {
            weight = bill.Weight,
            bill_no = bill.BillCode,
            status_time = ((DateTimeOffset)(latest?.ChangedAt ?? now)).ToUnixTimeSeconds(),
            shipping_fee = (double)bill.TotalFee,
            is_partial = 0,
            status_name = latest?.StatusName ?? $"Status {bill.StatusId}",
            status_id = bill.StatusId,
            dimension_weight = 0d,
            length = bill.Length,
            width = bill.Width,
            height = bill.Height,
            push_time = now.ToUnixTimeSeconds(),
            ref_code = bill.RefCode,
            expected_at = (bill.ExpectedAt ?? now).ToString("yyyy-MM-dd HH:mm:ss"),
            reason = latest?.Reason
        };
        var json = JsonSerializer.Serialize(payload);

        var subs = await _db.WebhookSubscriptions
            .Where(s => s.IsActive && s.PartnerId == 123736)
            .ToListAsync(ct);

        var client = _httpFactory.CreateClient("webhook");
        foreach (var sub in subs)
        {
            var log = new WebhookDeliveryLog
            {
                BillId = bill.Id,
                BillCode = bill.BillCode,
                SubscriptionId = sub.Id,
                CallbackUrl = sub.CallbackUrl,
                PayloadJson = json,
                AttemptedAt = now
            };
            try
            {
                using var content = new StringContent(json, Encoding.UTF8, "application/json");
                using var resp = await client.PostAsync(sub.CallbackUrl, content, ct);
                log.HttpStatusCode = (int)resp.StatusCode;
                log.Success = resp.IsSuccessStatusCode;
                log.ResponseBody = await resp.Content.ReadAsStringAsync(ct);
            }
            catch (Exception ex)
            {
                log.Success = false;
                log.ResponseBody = ex.Message;
            }
            _db.WebhookDeliveryLogs.Add(log);
        }
        await _db.SaveChangesAsync(ct);
    }
}
```

- [ ] **Step 4: Run test to verify it passes**

Run: `dotnet test CodeSandBox\tests\NhatTinSandbox.Tests --filter WebhookDispatcherTests`
Expected: PASS.

- [ ] **Step 5: Commit**

```powershell
git add CodeSandBox
git commit -m "feat(infra): HTTP webhook dispatcher with delivery logging"
```

---

### Task 9: API host wiring — Program.cs, DI, JWT bearer, Swagger, controllers

**Files:**
- Create: `CodeSandBox/src/NhatTinSandbox.Api/Extensions/InfrastructureRegistration.cs`
- Modify: `CodeSandBox/src/NhatTinSandbox.Api/Program.cs`
- Create: `CodeSandBox/src/NhatTinSandbox.Api/appsettings.json` (overwrite)
- Create: `CodeSandBox/src/NhatTinSandbox.Api/Properties/launchSettings.json` (overwrite)
- Create: `CodeSandBox/src/NhatTinSandbox.Api/Controllers/AuthController.cs`
- Create: `CodeSandBox/src/NhatTinSandbox.Api/Controllers/LocationController.cs`
- Create: `CodeSandBox/src/NhatTinSandbox.Api/Controllers/BillController.cs`
- Create: `CodeSandBox/src/NhatTinSandbox.Api/Controllers/SandboxController.cs`

**Interfaces:**
- Consumes: all Application interfaces and Infrastructure implementations.
- Produces: running API on port 5080 with the documented routes.

- [ ] **Step 1: Create the DI registration extension**

`CodeSandBox/src/NhatTinSandbox.Api/Extensions/InfrastructureRegistration.cs`:

```csharp
using Microsoft.EntityFrameworkCore;
using NhatTinSandbox.Application.Auth;
using NhatTinSandbox.Application.Bills;
using NhatTinSandbox.Application.Locations;
using NhatTinSandbox.Application.Webhooks;
using NhatTinSandbox.Infrastructure.Auth;
using NhatTinSandbox.Infrastructure.Bills;
using NhatTinSandbox.Infrastructure.Locations;
using NhatTinSandbox.Infrastructure.Persistence;
using NhatTinSandbox.Infrastructure.Webhooks;

namespace NhatTinSandbox.Api.Extensions;

public static class InfrastructureRegistration
{
    public static IServiceCollection AddSandboxInfrastructure(this IServiceCollection services, IConfiguration config)
    {
        services.Configure<JwtOptions>(config.GetSection(JwtOptions.SectionName));

        var cs = config.GetConnectionString("Sandbox")
                 ?? "Data Source=App_Data/nhattin-sandbox.db";
        services.AddDbContext<SandboxDbContext>(o => o.UseSqlite(cs));

        services.AddScoped<IAuthTokenService, JwtAuthTokenService>();
        services.AddScoped<ILocationCatalog, LocationCatalog>();
        services.AddScoped<IBillService, BillService>();
        services.AddScoped<IWebhookDispatcher, HttpWebhookDispatcher>();
        services.AddHttpClient("webhook");
        return services;
    }
}
```

- [ ] **Step 2: Overwrite appsettings.json**

`CodeSandBox/src/NhatTinSandbox.Api/appsettings.json`:

```json
{
  "Logging": {
    "LogLevel": { "Default": "Information", "Microsoft.AspNetCore": "Warning" }
  },
  "AllowedHosts": "*",
  "ConnectionStrings": {
    "Sandbox": "Data Source=App_Data/nhattin-sandbox.db"
  },
  "Jwt": {
    "Issuer": "nhattin-sandbox",
    "Audience": "nhattin-sandbox",
    "SigningKey": "sandbox-signing-key-change-me-please-0123456789abcdef",
    "AccessTtlSeconds": 900,
    "RefreshTtlSeconds": 3600
  }
}
```

> The signing key here is a non-secret sandbox placeholder; production keys must come from user-secrets/env, never this file.

- [ ] **Step 3: Overwrite launchSettings.json for fixed ports**

`CodeSandBox/src/NhatTinSandbox.Api/Properties/launchSettings.json`:

```json
{
  "$schema": "https://json.schemastore.org/launchsettings.json",
  "profiles": {
    "http": {
      "commandName": "Project",
      "dotnetRunMessages": true,
      "launchBrowser": true,
      "launchUrl": "swagger",
      "applicationUrl": "http://localhost:5080",
      "environmentVariables": { "ASPNETCORE_ENVIRONMENT": "Development" }
    },
    "https": {
      "commandName": "Project",
      "dotnetRunMessages": true,
      "launchBrowser": true,
      "launchUrl": "swagger",
      "applicationUrl": "https://localhost:7080;http://localhost:5080",
      "environmentVariables": { "ASPNETCORE_ENVIRONMENT": "Development" }
    }
  }
}
```

- [ ] **Step 4: Rewrite Program.cs**

`CodeSandBox/src/NhatTinSandbox.Api/Program.cs`:

```csharp
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using NhatTinSandbox.Api.Extensions;
using NhatTinSandbox.Infrastructure.Auth;
using NhatTinSandbox.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

// snake_case for BOTH inbound binding (s_name -> SName) and outbound JSON,
// matching NhatTinAPIDocumentation/vi/ field names. .NET 8 built-in policy.
builder.Services.AddControllers().AddJsonOptions(o =>
{
    o.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower;
    o.JsonSerializerOptions.DictionaryKeyPolicy = JsonNamingPolicy.SnakeCaseLower;
});
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddSandboxInfrastructure(builder.Configuration);

var jwt = builder.Configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>()!;
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(o =>
    {
        o.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwt.Issuer,
            ValidAudience = jwt.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.SigningKey))
        };
    });
builder.Services.AddAuthorization();

var app = builder.Build();

// Ensure App_Data exists, migrate, seed.
Directory.CreateDirectory(Path.Combine(app.Environment.ContentRootPath, "App_Data"));
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<SandboxDbContext>();
    db.Database.Migrate();
    SeedData.EnsureSeeded(db);
}

app.UseSwagger();
app.UseSwaggerUI();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();

public partial class Program { }
```

> `public partial class Program { }` enables `WebApplicationFactory<Program>` integration tests later if wanted.

- [ ] **Step 5: Create AuthController**

`CodeSandBox/src/NhatTinSandbox.Api/Controllers/AuthController.cs`:

```csharp
using Microsoft.AspNetCore.Mvc;
using NhatTinSandbox.Application.Auth;
using NhatTinSandbox.Application.Common;

namespace NhatTinSandbox.Api.Controllers;

[ApiController]
public sealed class AuthController : ControllerBase
{
    private readonly IAuthTokenService _tokens;
    public AuthController(IAuthTokenService tokens) => _tokens = tokens;

    public sealed record SignInBody(string username, string password);
    public sealed record RefreshBody(string refresh_token);

    [HttpPost("/v1/auth/sign-in")]
    public async Task<IActionResult> SignIn([FromBody] SignInBody body, CancellationToken ct)
    {
        var result = await _tokens.SignInAsync(body.username, body.password, ct);
        if (result is null)
            return Unauthorized(ApiResult.Fail("Xác thực thất bại"));
        return Ok(ApiResult.Ok(ToData(result), "Sign in successfully"));
    }

    [HttpPost("/v1/auth/refresh-token")]
    public async Task<IActionResult> Refresh([FromBody] RefreshBody body, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(body.refresh_token))
            return BadRequest(ApiResult.Fail("Thiếu refresh token"));
        var result = await _tokens.RefreshAsync(body.refresh_token, ct);
        if (result is null)
            return Unauthorized(ApiResult.Fail("Token không hợp lệ"));
        return Ok(ApiResult.Ok(ToData(result), "Refresh token successfully"));
    }

    private static object ToData(AuthTokenResult r) => new
    {
        jwt_token = r.JwtToken,
        token_type = r.TokenType,
        token_expires_in = $"{r.TokenExpiresInSeconds}s",
        refresh_token = r.RefreshToken,
        refresh_expires_in = $"{r.RefreshExpiresInSeconds}s"
    };
}
```

- [ ] **Step 6: Create LocationController**

`CodeSandBox/src/NhatTinSandbox.Api/Controllers/LocationController.cs`:

```csharp
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NhatTinSandbox.Application.Common;
using NhatTinSandbox.Application.Locations;

namespace NhatTinSandbox.Api.Controllers;

[ApiController]
[Authorize]
public sealed class LocationController : ControllerBase
{
    private readonly ILocationCatalog _catalog;
    public LocationController(ILocationCatalog catalog) => _catalog = catalog;

    [HttpGet("/v3/loc/provinces")]
    public async Task<IActionResult> Provinces([FromQuery(Name = "is_new")] int? isNew, CancellationToken ct)
        => Ok(ApiResult.Ok(await _catalog.GetProvincesAsync(isNew == 1, ct)));

    [HttpGet("/v3/loc/districts")]
    public async Task<IActionResult> Districts([FromQuery(Name = "province_id")] string provinceId, CancellationToken ct)
        => Ok(ApiResult.Ok(await _catalog.GetDistrictsAsync(provinceId, ct)));

    [HttpGet("/v3/loc/wards")]
    public async Task<IActionResult> Wards(
        [FromQuery(Name = "district_id")] string? districtId,
        [FromQuery(Name = "province_id")] string? provinceId,
        [FromQuery(Name = "is_new")] int? isNew,
        CancellationToken ct)
        => Ok(ApiResult.Ok(await _catalog.GetWardsAsync(districtId, provinceId, isNew == 1, ct)));
}
```

> Note on field names: the global `SnakeCaseLower` policy added in Step 4 maps every C# property to its documented snake_case name automatically — `ProvinceName` → `province_name`, `IsNew` → `is_new`, `Id` → `id`, and on the bill input records `SName` → `s_name`, `ServiceId` → `service_id`, etc. No per-DTO attributes are needed; Task 2's plain records stay as written.

- [ ] **Step 7: Verify snake_case mapping works (no code change)**

The location DTOs and bill input records need no `[JsonPropertyName]` attributes — the global policy from Step 4 handles casing. Confirm after Step 10's smoke run that a `GET /v3/loc/provinces?is_new=1` response body contains `province_name` and `is_new` (not `ProvinceName`/`IsNew`), and that `POST /v3/bill/create` correctly reads `s_name`/`service_id` (the created bill's `receiver_name` is non-empty and `service_id` matches the request). If a field comes back PascalCase or a created bill has empty sender/receiver, the JSON policy in Step 4 was not applied — fix that before continuing.

- [ ] **Step 8: Create BillController**

`CodeSandBox/src/NhatTinSandbox.Api/Controllers/BillController.cs`:

```csharp
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NhatTinSandbox.Application.Bills;
using NhatTinSandbox.Application.Common;

namespace NhatTinSandbox.Api.Controllers;

[ApiController]
[Authorize]
public sealed class BillController : ControllerBase
{
    private readonly IBillService _bills;
    public BillController(IBillService bills) => _bills = bills;

    [HttpPost("/v3/bill/create")]
    public async Task<IActionResult> Create([FromBody] CreateBillInput input, CancellationToken ct)
    {
        var bill = await _bills.CreateAsync(input, ct);
        return Ok(ApiResult.Ok(ToBillData(bill), "Create bill successfully"));
    }

    [HttpPost("/v3/bill/update-shipping")]
    public async Task<IActionResult> Update([FromBody] UpdateBillInput input, CancellationToken ct)
    {
        var bill = await _bills.UpdateAsync(input, ct);
        if (bill is null) return Ok(ApiResult.Fail("Bill not found"));
        return Ok(ApiResult.Ok(ToBillData(bill), "Update successful"));
    }

    [HttpPost("/v3/bill/calc-fee")]
    public async Task<IActionResult> CalcFee([FromBody] CalcFeeInput input, CancellationToken ct)
    {
        var options = await _bills.CalcFeeAsync(input, ct);
        return Ok(ApiResult.Ok(options.Select(ToFeeData)));
    }

    public sealed record BillCodeList(List<string> bill_code);

    [HttpPost("/v3/bill/destroy")]
    public async Task<IActionResult> Destroy([FromBody] BillCodeList body, CancellationToken ct)
    {
        var results = await _bills.CancelAsync(body.bill_code, ct);
        return Ok(ApiResult.Ok(results.Select(r => new { doCode = r.DoCode, message = r.Message })));
    }

    [HttpPost("/v3/bill/revert-bill")]
    public async Task<IActionResult> Revert([FromBody] BillCodeList body, CancellationToken ct)
    {
        var result = await _bills.RevertAsync(body.bill_code, ct);
        return Ok(ApiResult.Ok(new { success = result.Success, failed = result.Failed }));
    }

    [HttpGet("/v3/bill/tracking")]
    public async Task<IActionResult> Tracking([FromQuery(Name = "bill_code")] string billCode, CancellationToken ct)
    {
        var bill = await _bills.GetByCodeAsync(billCode, ct);
        if (bill is null) return Ok(ApiResult.Fail("Bill not found"));
        return Ok(ApiResult.Ok(new[] { ToTrackingData(bill) }, "Tracking successfully"));
    }

    [HttpGet("/v3/bill/print")]
    public IActionResult Print([FromQuery(Name = "do_code")] string doCode, [FromQuery(Name = "partner_id")] int partnerId)
    {
        // Real print response format is unconfirmed; sandbox returns a placeholder link.
        var url = $"http://localhost:5080/sandbox/labels/{doCode}.html";
        return Ok(ApiResult.Ok(new { do_code = doCode, partner_id = partnerId, print_url = url }, "Sandbox print placeholder"));
    }

    private static object ToBillData(BillSummary b) => new
    {
        bill_id = b.BillId,
        bill_code = b.BillCode,
        ref_code = b.RefCode ?? "",
        status_id = b.StatusId,
        cod_amount = b.CodAmount,
        service_id = b.ServiceId,
        payment_method = b.PaymentMethod,
        created_at = b.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss"),
        main_fee = b.MainFee,
        total_fee = b.TotalFee,
        receiver_name = b.ReceiverName,
        receiver_phone = b.ReceiverPhone,
        receiver_address = b.ReceiverAddress,
        package_no = b.PackageNo,
        weight = b.Weight,
        cargo_content = b.CargoContent ?? "",
        cargo_value = b.CargoValue,
        note = b.Note ?? ""
    };

    private static object ToFeeData(FeeOption f) => new
    {
        weight = f.Weight,
        total_fee = f.TotalFee,
        main_fee = f.MainFee,
        insur_fee = f.InsurFee,
        remote_fee = f.RemoteFee,
        cod_fee = f.CodFee,
        service_id = f.ServiceId,
        service_name = f.ServiceName,
        lead_time = f.LeadTime
    };

    private static object ToTrackingData(BillSummary b) => new
    {
        bill_code = b.BillCode,
        ref_code = b.RefCode ?? "",
        weight = b.Weight.ToString("0.00"),
        bill_status_id = b.StatusId,
        cod_amt = b.CodAmount.ToString("0.00"),
        total_fee = b.TotalFee.ToString("0"),
        receiver_name = b.ReceiverName,
        receiver_phone = b.ReceiverPhone,
        receiver_address = b.ReceiverAddress,
        note = b.Note ?? ""
    };
}
```

- [ ] **Step 9: Create SandboxController (internal helpers, incl. auto-webhook)**

`CodeSandBox/src/NhatTinSandbox.Api/Controllers/SandboxController.cs`:

```csharp
using Microsoft.AspNetCore.Mvc;
using NhatTinSandbox.Application.Bills;
using NhatTinSandbox.Application.Common;
using NhatTinSandbox.Application.Webhooks;

namespace NhatTinSandbox.Api.Controllers;

// Internal sandbox helpers — NOT part of the real Nhất Tín API surface.
[ApiController]
[Route("sandbox")]
public sealed class SandboxController : ControllerBase
{
    private readonly IBillService _bills;
    private readonly IWebhookDispatcher _dispatcher;

    public SandboxController(IBillService bills, IWebhookDispatcher dispatcher)
    {
        _bills = bills;
        _dispatcher = dispatcher;
    }

    public sealed record SimulateStatusBody(int status_id, string? reason);

    [HttpPost("bills/{billCode}/simulate-status")]
    public async Task<IActionResult> SimulateStatus(string billCode, [FromBody] SimulateStatusBody body, CancellationToken ct)
    {
        var updated = await _bills.SetStatusAsync(billCode, body.status_id, body.reason, ct);
        if (updated is null) return NotFound(ApiResult.Fail("Bill not found"));
        await _dispatcher.DispatchAsync(updated.BillId, ct);
        return Ok(ApiResult.Ok(new { bill_code = updated.BillCode, status_id = updated.StatusId }, "Status simulated and webhook dispatched"));
    }
}
```

- [ ] **Step 10: Build and run smoke-check**

Run: `dotnet build CodeSandBox\NhatTinSandbox.sln`
Expected: Build succeeded.

Then run manually (in a background terminal): `dotnet run --project CodeSandBox\src\NhatTinSandbox.Api`
Expected: listening on `http://localhost:5080`; `App_Data/nhattin-sandbox.db` created; Swagger reachable at `/swagger`.

- [ ] **Step 11: Commit**

```powershell
git add CodeSandBox
git commit -m "feat(api): controllers, JWT bearer, Swagger, migrate+seed on boot"
```

---

### Task 10: AdminPortal (Razor Pages)

**Files:**
- Modify: `CodeSandBox/src/NhatTinSandbox.AdminPortal/Program.cs`
- Create: `CodeSandBox/src/NhatTinSandbox.AdminPortal/appsettings.json` (overwrite)
- Create: `CodeSandBox/src/NhatTinSandbox.AdminPortal/Properties/launchSettings.json` (overwrite)
- Create: `CodeSandBox/src/NhatTinSandbox.AdminPortal/Pages/Bills/Index.cshtml` (+ `.cs`)
- Create: `CodeSandBox/src/NhatTinSandbox.AdminPortal/Pages/Bills/Details.cshtml` (+ `.cs`)
- Create: `CodeSandBox/src/NhatTinSandbox.AdminPortal/Pages/Subscriptions/Index.cshtml` (+ `.cs`)
- Create: `CodeSandBox/src/NhatTinSandbox.AdminPortal/Pages/Deliveries/Index.cshtml` (+ `.cs`)

**Interfaces:**
- Consumes: `SandboxDbContext`, `IBillService`, `IWebhookDispatcher` (registered via a shared registration helper).

- [ ] **Step 1: Overwrite AdminPortal appsettings.json**

`CodeSandBox/src/NhatTinSandbox.AdminPortal/appsettings.json`:

```json
{
  "Logging": { "LogLevel": { "Default": "Information", "Microsoft.AspNetCore": "Warning" } },
  "AllowedHosts": "*",
  "ConnectionStrings": {
    "Sandbox": "Data Source=../NhatTinSandbox.Api/App_Data/nhattin-sandbox.db"
  },
  "Jwt": {
    "Issuer": "nhattin-sandbox",
    "Audience": "nhattin-sandbox",
    "SigningKey": "sandbox-signing-key-change-me-please-0123456789abcdef",
    "AccessTtlSeconds": 900,
    "RefreshTtlSeconds": 3600
  }
}
```

> AdminPortal shares the same SQLite file as the Api so they see the same bills. Relative path resolves from the AdminPortal content root.

- [ ] **Step 2: Overwrite AdminPortal launchSettings.json**

`CodeSandBox/src/NhatTinSandbox.AdminPortal/Properties/launchSettings.json`:

```json
{
  "$schema": "https://json.schemastore.org/launchsettings.json",
  "profiles": {
    "http": {
      "commandName": "Project",
      "launchBrowser": true,
      "applicationUrl": "http://localhost:5090",
      "environmentVariables": { "ASPNETCORE_ENVIRONMENT": "Development" }
    },
    "https": {
      "commandName": "Project",
      "launchBrowser": true,
      "applicationUrl": "https://localhost:7090;http://localhost:5090",
      "environmentVariables": { "ASPNETCORE_ENVIRONMENT": "Development" }
    }
  }
}
```

- [ ] **Step 3: Wire AdminPortal Program.cs**

`CodeSandBox/src/NhatTinSandbox.AdminPortal/Program.cs`:

```csharp
using Microsoft.EntityFrameworkCore;
using NhatTinSandbox.Application.Bills;
using NhatTinSandbox.Application.Webhooks;
using NhatTinSandbox.Infrastructure.Bills;
using NhatTinSandbox.Infrastructure.Persistence;
using NhatTinSandbox.Infrastructure.Webhooks;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddRazorPages();

var cs = builder.Configuration.GetConnectionString("Sandbox")
         ?? "Data Source=../NhatTinSandbox.Api/App_Data/nhattin-sandbox.db";
builder.Services.AddDbContext<SandboxDbContext>(o => o.UseSqlite(cs));
builder.Services.AddScoped<IBillService, BillService>();
builder.Services.AddScoped<IWebhookDispatcher, HttpWebhookDispatcher>();
builder.Services.AddHttpClient("webhook");

var app = builder.Build();
app.UseStaticFiles();
app.UseRouting();
app.MapRazorPages();
app.MapGet("/", () => Results.Redirect("/Bills"));
app.Run();
```

- [ ] **Step 4: Bills list page**

`CodeSandBox/src/NhatTinSandbox.AdminPortal/Pages/Bills/Index.cshtml.cs`:

```csharp
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using NhatTinSandbox.Domain.Entities;
using NhatTinSandbox.Infrastructure.Persistence;

namespace NhatTinSandbox.AdminPortal.Pages.Bills;

public class IndexModel : PageModel
{
    private readonly SandboxDbContext _db;
    public IndexModel(SandboxDbContext db) => _db = db;

    public List<Bill> Bills { get; private set; } = new();

    public async Task OnGetAsync()
        => Bills = await _db.Bills.OrderByDescending(b => b.Id).Take(100).ToListAsync();
}
```

`CodeSandBox/src/NhatTinSandbox.AdminPortal/Pages/Bills/Index.cshtml`:

```html
@page
@model NhatTinSandbox.AdminPortal.Pages.Bills.IndexModel
@{ ViewData["Title"] = "Vận đơn"; }
<h1>Vận đơn Sandbox</h1>
<p><a href="/Subscriptions">Webhook Subscriptions</a> | <a href="/Deliveries">Webhook Delivery Logs</a></p>
<table class="table" border="1" cellpadding="6">
  <thead><tr><th>Bill Code</th><th>Ref</th><th>Status</th><th>COD</th><th>Total Fee</th><th></th></tr></thead>
  <tbody>
  @foreach (var b in Model.Bills)
  {
    <tr>
      <td>@b.BillCode</td><td>@b.RefCode</td><td>@b.StatusId</td>
      <td>@b.CodAmount</td><td>@b.TotalFee</td>
      <td><a href="/Bills/Details?billCode=@b.BillCode">Chi tiết / Đổi trạng thái</a></td>
    </tr>
  }
  </tbody>
</table>
```

- [ ] **Step 5: Bill details + change status page**

`CodeSandBox/src/NhatTinSandbox.AdminPortal/Pages/Bills/Details.cshtml.cs`:

```csharp
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using NhatTinSandbox.Application.Bills;
using NhatTinSandbox.Application.Webhooks;
using NhatTinSandbox.Domain.Entities;
using NhatTinSandbox.Infrastructure.Persistence;

namespace NhatTinSandbox.AdminPortal.Pages.Bills;

public class DetailsModel : PageModel
{
    private readonly SandboxDbContext _db;
    private readonly IBillService _bills;
    private readonly IWebhookDispatcher _dispatcher;

    public DetailsModel(SandboxDbContext db, IBillService bills, IWebhookDispatcher dispatcher)
    {
        _db = db; _bills = bills; _dispatcher = dispatcher;
    }

    public Bill? Bill { get; private set; }
    public List<MasterDataItem> Statuses { get; private set; } = new();
    public List<BillStatusHistory> Histories { get; private set; } = new();

    [BindProperty(SupportsGet = true)] public string BillCode { get; set; } = "";
    [BindProperty] public int NewStatusId { get; set; }
    [BindProperty] public string? Reason { get; set; }

    public async Task OnGetAsync() => await LoadAsync();

    public async Task<IActionResult> OnPostAsync()
    {
        var updated = await _bills.SetStatusAsync(BillCode, NewStatusId, Reason, HttpContext.RequestAborted);
        if (updated is not null)
            await _dispatcher.DispatchAsync(updated.BillId, HttpContext.RequestAborted);
        return RedirectToPage(new { billCode = BillCode });
    }

    private async Task LoadAsync()
    {
        Bill = await _db.Bills.FirstOrDefaultAsync(b => b.BillCode == BillCode);
        Statuses = await _db.MasterData.Where(m => m.Kind == MasterDataKind.BillStatus).OrderBy(m => m.Code).ToListAsync();
        if (Bill is not null)
            Histories = await _db.BillStatusHistories.Where(h => h.BillId == Bill.Id).OrderByDescending(h => h.Id).ToListAsync();
    }
}
```

`CodeSandBox/src/NhatTinSandbox.AdminPortal/Pages/Bills/Details.cshtml`:

```html
@page
@model NhatTinSandbox.AdminPortal.Pages.Bills.DetailsModel
@{ ViewData["Title"] = "Chi tiết vận đơn"; }
<p><a href="/Bills">&larr; Danh sách</a></p>
@if (Model.Bill is null)
{
  <p>Không tìm thấy vận đơn <b>@Model.BillCode</b>.</p>
}
else
{
  <h1>@Model.Bill.BillCode</h1>
  <ul>
    <li>Ref: @Model.Bill.RefCode</li>
    <li>Trạng thái hiện tại: <b>@Model.Bill.StatusId</b></li>
    <li>Người nhận: @Model.Bill.ReceiverName - @Model.Bill.ReceiverPhone</li>
    <li>COD: @Model.Bill.CodAmount | Tổng phí: @Model.Bill.TotalFee</li>
  </ul>

  <form method="post">
    <input type="hidden" name="BillCode" value="@Model.BillCode" />
    <label>Đổi trạng thái:</label>
    <select name="NewStatusId">
      @foreach (var s in Model.Statuses)
      {
        <option value="@s.Code">@s.Code - @s.Name</option>
      }
    </select>
    <input type="text" name="Reason" placeholder="Lý do (tùy chọn)" />
    <button type="submit">Cập nhật & gửi webhook</button>
  </form>

  <h3>Lịch sử trạng thái</h3>
  <table border="1" cellpadding="6">
    <thead><tr><th>Status</th><th>Tên</th><th>Lý do</th><th>Thời điểm</th></tr></thead>
    <tbody>
    @foreach (var h in Model.Histories)
    {
      <tr><td>@h.StatusId</td><td>@h.StatusName</td><td>@h.Reason</td><td>@h.ChangedAt</td></tr>
    }
    </tbody>
  </table>
}
```

- [ ] **Step 6: Subscriptions page**

`CodeSandBox/src/NhatTinSandbox.AdminPortal/Pages/Subscriptions/Index.cshtml.cs`:

```csharp
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using NhatTinSandbox.Domain.Entities;
using NhatTinSandbox.Infrastructure.Persistence;

namespace NhatTinSandbox.AdminPortal.Pages.Subscriptions;

public class IndexModel : PageModel
{
    private readonly SandboxDbContext _db;
    public IndexModel(SandboxDbContext db) => _db = db;

    public List<WebhookSubscription> Subscriptions { get; private set; } = new();
    [BindProperty] public string CallbackUrl { get; set; } = "";
    [BindProperty] public int PartnerId { get; set; } = 123736;

    public async Task OnGetAsync()
        => Subscriptions = await _db.WebhookSubscriptions.OrderBy(s => s.Id).ToListAsync();

    public async Task<IActionResult> OnPostAddAsync()
    {
        if (!string.IsNullOrWhiteSpace(CallbackUrl))
        {
            _db.WebhookSubscriptions.Add(new WebhookSubscription { CallbackUrl = CallbackUrl, PartnerId = PartnerId, IsActive = true });
            await _db.SaveChangesAsync();
        }
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostToggleAsync(int id)
    {
        var sub = await _db.WebhookSubscriptions.FindAsync(id);
        if (sub is not null) { sub.IsActive = !sub.IsActive; await _db.SaveChangesAsync(); }
        return RedirectToPage();
    }
}
```

`CodeSandBox/src/NhatTinSandbox.AdminPortal/Pages/Subscriptions/Index.cshtml`:

```html
@page
@model NhatTinSandbox.AdminPortal.Pages.Subscriptions.IndexModel
@{ ViewData["Title"] = "Webhook Subscriptions"; }
<p><a href="/Bills">&larr; Vận đơn</a></p>
<h1>Webhook Subscriptions</h1>
<form method="post" asp-page-handler="Add">
  <input name="PartnerId" value="123736" />
  <input name="CallbackUrl" placeholder="http://localhost:5099/webhooks/nhattin/status" size="60" />
  <button type="submit">Thêm</button>
</form>
<table border="1" cellpadding="6">
  <thead><tr><th>ID</th><th>Partner</th><th>Callback URL</th><th>Active</th><th></th></tr></thead>
  <tbody>
  @foreach (var s in Model.Subscriptions)
  {
    <tr>
      <td>@s.Id</td><td>@s.PartnerId</td><td>@s.CallbackUrl</td><td>@s.IsActive</td>
      <td>
        <form method="post" asp-page-handler="Toggle" asp-route-id="@s.Id" style="display:inline">
          <button type="submit">Bật/Tắt</button>
        </form>
      </td>
    </tr>
  }
  </tbody>
</table>
```

- [ ] **Step 7: Delivery logs page (with manual resend)**

`CodeSandBox/src/NhatTinSandbox.AdminPortal/Pages/Deliveries/Index.cshtml.cs`:

```csharp
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using NhatTinSandbox.Application.Webhooks;
using NhatTinSandbox.Domain.Entities;
using NhatTinSandbox.Infrastructure.Persistence;

namespace NhatTinSandbox.AdminPortal.Pages.Deliveries;

public class IndexModel : PageModel
{
    private readonly SandboxDbContext _db;
    private readonly IWebhookDispatcher _dispatcher;
    public IndexModel(SandboxDbContext db, IWebhookDispatcher dispatcher) { _db = db; _dispatcher = dispatcher; }

    public List<WebhookDeliveryLog> Logs { get; private set; } = new();

    public async Task OnGetAsync()
        => Logs = await _db.WebhookDeliveryLogs.OrderByDescending(l => l.Id).Take(100).ToListAsync();

    public async Task<IActionResult> OnPostResendAsync(int billId)
    {
        await _dispatcher.DispatchAsync(billId, HttpContext.RequestAborted);
        return RedirectToPage();
    }
}
```

`CodeSandBox/src/NhatTinSandbox.AdminPortal/Pages/Deliveries/Index.cshtml`:

```html
@page
@model NhatTinSandbox.AdminPortal.Pages.Deliveries.IndexModel
@{ ViewData["Title"] = "Webhook Delivery Logs"; }
<p><a href="/Bills">&larr; Vận đơn</a></p>
<h1>Webhook Delivery Logs</h1>
<table border="1" cellpadding="6">
  <thead><tr><th>ID</th><th>Bill</th><th>URL</th><th>HTTP</th><th>Success</th><th>Time</th><th></th></tr></thead>
  <tbody>
  @foreach (var l in Model.Logs)
  {
    <tr>
      <td>@l.Id</td><td>@l.BillCode</td><td>@l.CallbackUrl</td>
      <td>@l.HttpStatusCode</td><td>@l.Success</td><td>@l.AttemptedAt</td>
      <td>
        <form method="post" asp-page-handler="Resend" asp-route-billId="@l.BillId" style="display:inline">
          <button type="submit">Gửi lại</button>
        </form>
      </td>
    </tr>
  }
  </tbody>
</table>
```

- [ ] **Step 8: Build**

Run: `dotnet build CodeSandBox\src\NhatTinSandbox.AdminPortal`
Expected: Build succeeded.

- [ ] **Step 9: Commit**

```powershell
git add CodeSandBox
git commit -m "feat(admin): Razor portal for bills, status change, subscriptions, delivery logs"
```

---

### Task 11: CodeWebHooks receiver solution

**Files:**
- Create: `CodeWebHooks/NhatTinWebhookReceiver.sln` + `src/*` + `tests/*` projects
- Create: `CodeWebHooks/src/NhatTinWebhookReceiver.Api/Domain/ReceivedWebhook.cs`
- Create: `CodeWebHooks/src/NhatTinWebhookReceiver.Api/Persistence/WebhookDbContext.cs`
- Create: `CodeWebHooks/src/NhatTinWebhookReceiver.Api/Persistence/WebhookDbContextFactory.cs`
- Create: `CodeWebHooks/src/NhatTinWebhookReceiver.Api/Controllers/WebhookController.cs`
- Create: `CodeWebHooks/src/NhatTinWebhookReceiver.Api/Pages/Index.cshtml` (+ `.cs`)
- Modify: `CodeWebHooks/src/NhatTinWebhookReceiver.Api/Program.cs`
- Overwrite: `appsettings.json`, `Properties/launchSettings.json`
- Test: `CodeWebHooks/tests/NhatTinWebhookReceiver.Tests/WebhookControllerTests.cs`

**Interfaces:**
- Produces: receiver listening on 5099 at `/webhooks/nhattin/status`, storing raw evidence.

- [ ] **Step 1: Scaffold the solution**

```powershell
dotnet new sln -n NhatTinWebhookReceiver -o CodeWebHooks
dotnet new webapi -n NhatTinWebhookReceiver.Api -o CodeWebHooks\src\NhatTinWebhookReceiver.Api -f net8.0 --use-controllers
dotnet new xunit -n NhatTinWebhookReceiver.Tests -o CodeWebHooks\tests\NhatTinWebhookReceiver.Tests -f net8.0

Remove-Item CodeWebHooks\src\NhatTinWebhookReceiver.Api\WeatherForecast.cs -ErrorAction SilentlyContinue
Remove-Item CodeWebHooks\src\NhatTinWebhookReceiver.Api\Controllers\WeatherForecastController.cs -ErrorAction SilentlyContinue
Remove-Item CodeWebHooks\tests\NhatTinWebhookReceiver.Tests\UnitTest1.cs

dotnet sln CodeWebHooks\NhatTinWebhookReceiver.sln add `
  CodeWebHooks\src\NhatTinWebhookReceiver.Api\NhatTinWebhookReceiver.Api.csproj `
  CodeWebHooks\tests\NhatTinWebhookReceiver.Tests\NhatTinWebhookReceiver.Tests.csproj

dotnet add CodeWebHooks\tests\NhatTinWebhookReceiver.Tests reference CodeWebHooks\src\NhatTinWebhookReceiver.Api

dotnet add CodeWebHooks\src\NhatTinWebhookReceiver.Api package Microsoft.EntityFrameworkCore.Sqlite -v 8.0.10
dotnet add CodeWebHooks\src\NhatTinWebhookReceiver.Api package Microsoft.EntityFrameworkCore.Design -v 8.0.10
dotnet add CodeWebHooks\tests\NhatTinWebhookReceiver.Tests package Microsoft.EntityFrameworkCore.Sqlite -v 8.0.10
dotnet add CodeWebHooks\tests\NhatTinWebhookReceiver.Tests package Microsoft.AspNetCore.Mvc.Testing -v 8.0.10
```

- [ ] **Step 2: Create the entity**

`CodeWebHooks/src/NhatTinWebhookReceiver.Api/Domain/ReceivedWebhook.cs`:

```csharp
namespace NhatTinWebhookReceiver.Api.Domain;

public class ReceivedWebhook
{
    public int Id { get; set; }
    public DateTimeOffset ReceivedAt { get; set; } = DateTimeOffset.UtcNow;
    public string HttpMethod { get; set; } = string.Empty;
    public string HeadersJson { get; set; } = string.Empty;
    public string RawBody { get; set; } = string.Empty;
    public bool IsValidPayload { get; set; }
    public string? BillNo { get; set; }
    public int? StatusId { get; set; }
    public string? StatusName { get; set; }
    public string? RefCode { get; set; }
}
```

- [ ] **Step 3: Create the DbContext + factory**

`CodeWebHooks/src/NhatTinWebhookReceiver.Api/Persistence/WebhookDbContext.cs`:

```csharp
using Microsoft.EntityFrameworkCore;
using NhatTinWebhookReceiver.Api.Domain;

namespace NhatTinWebhookReceiver.Api.Persistence;

public class WebhookDbContext : DbContext
{
    public WebhookDbContext(DbContextOptions<WebhookDbContext> options) : base(options) { }
    public DbSet<ReceivedWebhook> ReceivedWebhooks => Set<ReceivedWebhook>();
}
```

`CodeWebHooks/src/NhatTinWebhookReceiver.Api/Persistence/WebhookDbContextFactory.cs`:

```csharp
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace NhatTinWebhookReceiver.Api.Persistence;

public class WebhookDbContextFactory : IDesignTimeDbContextFactory<WebhookDbContext>
{
    public WebhookDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<WebhookDbContext>()
            .UseSqlite("Data Source=design-time.db").Options;
        return new WebhookDbContext(options);
    }
}
```

- [ ] **Step 4: Create the controller**

`CodeWebHooks/src/NhatTinWebhookReceiver.Api/Controllers/WebhookController.cs`:

```csharp
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using NhatTinWebhookReceiver.Api.Domain;
using NhatTinWebhookReceiver.Api.Persistence;

namespace NhatTinWebhookReceiver.Api.Controllers;

[ApiController]
public sealed class WebhookController : ControllerBase
{
    private readonly WebhookDbContext _db;
    public WebhookController(WebhookDbContext db) => _db = db;

    // Docs list GET/POST/PUT. Accept all; always store raw evidence first.
    [HttpPost("/webhooks/nhattin/status")]
    [HttpPut("/webhooks/nhattin/status")]
    [HttpGet("/webhooks/nhattin/status")]
    public async Task<IActionResult> Receive(CancellationToken ct)
    {
        string body = "";
        if (Request.ContentLength is > 0)
        {
            using var reader = new StreamReader(Request.Body, Encoding.UTF8);
            body = await reader.ReadToEndAsync(ct);
        }

        var record = new ReceivedWebhook
        {
            ReceivedAt = DateTimeOffset.UtcNow,
            HttpMethod = Request.Method,
            HeadersJson = JsonSerializer.Serialize(Request.Headers.ToDictionary(h => h.Key, h => h.Value.ToString())),
            RawBody = body
        };

        TryParse(body, record);

        _db.ReceivedWebhooks.Add(record);
        await _db.SaveChangesAsync(ct);

        return Ok(new { success = true, message = "ACK", data = new { received_at = record.ReceivedAt } });
    }

    private static void TryParse(string body, ReceivedWebhook record)
    {
        if (string.IsNullOrWhiteSpace(body)) return;
        try
        {
            using var doc = JsonDocument.Parse(body);
            var root = doc.RootElement;
            if (root.TryGetProperty("bill_no", out var billNo)) record.BillNo = billNo.GetString();
            if (root.TryGetProperty("status_id", out var sid) && sid.TryGetInt32(out var sidVal)) record.StatusId = sidVal;
            if (root.TryGetProperty("status_name", out var sname)) record.StatusName = sname.GetString();
            if (root.TryGetProperty("ref_code", out var rc)) record.RefCode = rc.GetString();
            record.IsValidPayload = record.BillNo is not null && record.StatusId is not null;
        }
        catch (JsonException)
        {
            record.IsValidPayload = false; // store raw anyway; never "fix" it
        }
    }
}
```

- [ ] **Step 5: Create the log viewer Razor page**

`CodeWebHooks/src/NhatTinWebhookReceiver.Api/Pages/Index.cshtml.cs`:

```csharp
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using NhatTinWebhookReceiver.Api.Domain;
using NhatTinWebhookReceiver.Api.Persistence;

namespace NhatTinWebhookReceiver.Api.Pages;

public class IndexModel : PageModel
{
    private readonly WebhookDbContext _db;
    public IndexModel(WebhookDbContext db) => _db = db;

    public List<ReceivedWebhook> Items { get; private set; } = new();

    public async Task OnGetAsync()
        => Items = await _db.ReceivedWebhooks.OrderByDescending(x => x.Id).Take(100).ToListAsync();
}
```

`CodeWebHooks/src/NhatTinWebhookReceiver.Api/Pages/Index.cshtml`:

```html
@page
@model NhatTinWebhookReceiver.Api.Pages.IndexModel
@{ ViewData["Title"] = "Webhook nhận được"; }
<h1>Webhook nhận được (referee)</h1>
<table border="1" cellpadding="6">
  <thead><tr><th>ID</th><th>Thời điểm</th><th>Method</th><th>Bill</th><th>Status</th><th>Valid?</th><th>Raw body</th></tr></thead>
  <tbody>
  @foreach (var w in Model.Items)
  {
    <tr>
      <td>@w.Id</td><td>@w.ReceivedAt</td><td>@w.HttpMethod</td>
      <td>@w.BillNo</td><td>@w.StatusId - @w.StatusName</td><td>@w.IsValidPayload</td>
      <td><pre style="max-width:480px;white-space:pre-wrap">@w.RawBody</pre></td>
    </tr>
  }
  </tbody>
</table>
```

`CodeWebHooks/src/NhatTinWebhookReceiver.Api/Pages/_ViewImports.cshtml`:

```cshtml
@namespace NhatTinWebhookReceiver.Api.Pages
@addTagHelper *, Microsoft.AspNetCore.Mvc.TagHelpers
```

- [ ] **Step 6: Wire Program.cs**

`CodeWebHooks/src/NhatTinWebhookReceiver.Api/Program.cs`:

```csharp
using Microsoft.EntityFrameworkCore;
using NhatTinWebhookReceiver.Api.Persistence;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllers();
builder.Services.AddRazorPages();

var cs = builder.Configuration.GetConnectionString("Webhooks")
         ?? "Data Source=App_Data/nhattin-webhooks.db";
builder.Services.AddDbContext<WebhookDbContext>(o => o.UseSqlite(cs));

var app = builder.Build();

Directory.CreateDirectory(Path.Combine(app.Environment.ContentRootPath, "App_Data"));
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<WebhookDbContext>();
    db.Database.Migrate();
}

app.UseStaticFiles();
app.UseRouting();
app.MapControllers();
app.MapRazorPages();
app.Run();

public partial class Program { }
```

- [ ] **Step 7: Overwrite appsettings.json and launchSettings.json**

`CodeWebHooks/src/NhatTinWebhookReceiver.Api/appsettings.json`:

```json
{
  "Logging": { "LogLevel": { "Default": "Information", "Microsoft.AspNetCore": "Warning" } },
  "AllowedHosts": "*",
  "ConnectionStrings": { "Webhooks": "Data Source=App_Data/nhattin-webhooks.db" }
}
```

`CodeWebHooks/src/NhatTinWebhookReceiver.Api/Properties/launchSettings.json`:

```json
{
  "$schema": "https://json.schemastore.org/launchsettings.json",
  "profiles": {
    "http": {
      "commandName": "Project",
      "launchBrowser": true,
      "applicationUrl": "http://localhost:5099",
      "environmentVariables": { "ASPNETCORE_ENVIRONMENT": "Development" }
    }
  }
}
```

- [ ] **Step 8: Add the initial migration**

```powershell
dotnet ef migrations add InitialCreate `
  --project CodeWebHooks\src\NhatTinWebhookReceiver.Api `
  --startup-project CodeWebHooks\src\NhatTinWebhookReceiver.Api `
  --output-dir Persistence\Migrations
```

Expected: migration generated.

- [ ] **Step 9: Write the integration test**

`CodeWebHooks/tests/NhatTinWebhookReceiver.Tests/WebhookControllerTests.cs`:

```csharp
using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace NhatTinWebhookReceiver.Tests;

public sealed class WebhookControllerTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    public WebhookControllerTests(WebApplicationFactory<Program> factory) => _factory = factory;

    [Fact]
    public async Task PostStatus_ReturnsAck()
    {
        var client = _factory.CreateClient();
        var payload = new
        {
            bill_no = "CP16658276R",
            ref_code = "40724974",
            status_id = 3,
            status_name = "Đã lấy hàng",
            status_time = 1681382601,
            push_time = 1681382738
        };

        var resp = await client.PostAsJsonAsync("/webhooks/nhattin/status", payload);

        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        var json = await resp.Content.ReadAsStringAsync();
        Assert.Contains("ACK", json);
    }
}
```

- [ ] **Step 10: Run tests**

Run: `dotnet test CodeWebHooks\tests\NhatTinWebhookReceiver.Tests`
Expected: PASS. (The factory boots the app, migrates a SQLite file, records the webhook, ACKs.)

- [ ] **Step 11: Commit**

```powershell
git add CodeWebHooks
git commit -m "feat(webhooks): receiver with raw evidence store, ACK, log viewer"
```

---

### Task 12: End-to-end smoke test script + README

**Files:**
- Create: `Tests/run-nhattin-cycle.ps1`
- Create: `README.md`

**Interfaces:**
- Consumes: running Sandbox Api (5080) and Webhook Receiver (5099).

- [ ] **Step 1: Write the cycle script**

`Tests/run-nhattin-cycle.ps1`:

```powershell
param(
    [string]$SandboxBaseUrl = "http://localhost:5080",
    [string]$WebhookBaseUrl = "http://localhost:5099"
)
$ErrorActionPreference = "Stop"

Write-Host "1) Sign in..."
$login = Invoke-RestMethod -Method Post -Uri "$SandboxBaseUrl/v1/auth/sign-in" `
    -ContentType "application/json" `
    -Body (@{ username = "sandbox"; password = "sandbox123" } | ConvertTo-Json)
$token = $login.data.jwt_token
$headers = @{ Authorization = "Bearer $token" }
Write-Host "   login success = $($login.success)"

Write-Host "2) Get provinces..."
$provinces = Invoke-RestMethod -Method Get -Uri "$SandboxBaseUrl/v3/loc/provinces?is_new=1" -Headers $headers

Write-Host "3) Create bill..."
$billBody = @{
    ref_code = "TP-CYCLE-001"; weight = 2; service_id = 91; payment_method_id = 10; cargo_type_id = 2
    s_name = "TruePos"; s_phone = "0333333333"; s_address = "so 10"; s_province_code = "01"; s_ward_code = "00004"
    r_name = "Nguyen Van A"; r_phone = "0911111111"; r_address = "123"; r_province_code = "79"; r_ward_code = "27007"
    cod_amount = 120000
} | ConvertTo-Json
$bill = Invoke-RestMethod -Method Post -Uri "$SandboxBaseUrl/v3/bill/create" -Headers $headers -ContentType "application/json" -Body $billBody
$billCode = $bill.data.bill_code
Write-Host "   bill_code = $billCode (status $($bill.data.status_id))"

Write-Host "4) Simulate status change -> webhook..."
$sim = Invoke-RestMethod -Method Post -Uri "$SandboxBaseUrl/sandbox/bills/$billCode/simulate-status" `
    -ContentType "application/json" -Body (@{ status_id = 3; reason = "Da lay hang" } | ConvertTo-Json)

Write-Host "5) Tracking..."
$tracking = Invoke-RestMethod -Method Get -Uri "$SandboxBaseUrl/v3/bill/tracking?bill_code=$billCode" -Headers $headers

Write-Host "6) Verify webhook received (query receiver DB via its page is manual; here we just report)."
[PSCustomObject]@{
    LoginSuccess    = $login.success
    ProvinceCount   = @($provinces.data).Count
    BillCode        = $billCode
    SimulateSuccess = $sim.success
    TrackingStatus  = $tracking.data[0].bill_status_id
} | ConvertTo-Json
```

- [ ] **Step 2: Write README**

`README.md`:

```markdown
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
```

- [ ] **Step 3: Run the full build + test sweep**

```powershell
dotnet build CodeSandBox\NhatTinSandbox.sln
dotnet build CodeWebHooks\NhatTinWebhookReceiver.sln
dotnet test CodeSandBox\NhatTinSandbox.sln
dotnet test CodeWebHooks\NhatTinWebhookReceiver.sln
```

Expected: all builds and tests pass.

- [ ] **Step 4: Manual end-to-end verification**

Start receiver (5099) then sandbox (5080) in background terminals, then:

```powershell
pwsh Tests\run-nhattin-cycle.ps1
```

Expected: JSON report with `LoginSuccess=True`, a `BillCode`, `SimulateSuccess=True`, `TrackingStatus=3`. Then open `http://localhost:5099` and confirm a row with `bill_no` = that bill code and `status_id = 3`.

- [ ] **Step 5: Secret scan**

```powershell
rg -n "password|jwt_token|refresh_token|SigningKey" CodeSandBox CodeWebHooks Tests -g "!**/Migrations/**"
```

Expected: only the sandbox demo password (`sandbox123`), setting keys, and the placeholder signing key appear — no real Nhất Tín credentials or tokens.

- [ ] **Step 6: Commit**

```powershell
git add Tests README.md
git commit -m "test: end-to-end cycle script and run instructions"
```

---

## Execution Order

1. Task 0 — repo init + CodeSandBox scaffold
2. Task 1 — domain entities
3. Task 2 — application contracts + envelope
4. Task 3 — EF Core context + seed + migration
5. Task 4 — JWT auth service
6. Task 5 — location catalog
7. Task 6 — bill create/get/set-status
8. Task 7 — bill calc-fee/update/cancel/revert
9. Task 8 — webhook dispatcher
10. Task 9 — API host + controllers
11. Task 10 — admin portal
12. Task 11 — webhook receiver solution
13. Task 12 — smoke test + README

## Deferred (out of scope, matches design spec section 10)

- `CodeMVC` real POS client integration and POS system-configuration binding.
- Cross-verification against the real Nhất Tín sandbox with live credentials.
- SQL Server backing store (architecture already provider-swappable via `UseSqlite` → `UseSqlServer`).
- Real print response format, webhook retry/signature policy, full national location catalog, full master-data catalog.
