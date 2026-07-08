# Thiết kế: Chuyển toàn bộ project về .NET 6

**Ngày:** 2026-07-08
**Branch:** `feat/nhattin-sdk`
**Trạng thái:** Đã duyệt, đang thực thi

## Mục tiêu & lý do

Đưa **toàn bộ** solution trong repo về **`net6.0`** để đồng nhất runtime với sản phẩm TruePos
(SDK `NhatTinLogistics.Sdk` — artifact thực sự ship vào TruePos — vốn đã cố ý là net6). Các công cụ
sandbox/dev xung quanh hiện đang `net8.0`; đưa về net6 để chạy cùng một runtime như production.

## Phạm vi

Chuyển `net8.0 → net6.0` cho **10 project / 3 solution**:

- `CodeSandBox/` (6 project): Domain, Application, Infrastructure, Api, AdminPortal, Tests
- `CodeWebHooks/` (2 project): Api, Tests
- `CodeMVC/` (2 project): Web, Tests

**Không đụng tới:** `CodeSDK/` (3 project) — đã là `net6.0`, đóng vai trò mẫu. Vẫn được CodeMVC tiêu
thụ dưới dạng gói `NhatTinLogistics.Sdk 0.3.0` từ `CodeMVC/local-feed/` (đã có sẵn nupkg net6).

## Kết quả khảo sát (rủi ro thực tế thấp)

- **Chỉ 1** tính năng C# 11 được dùng: raw string literal `"""` trong
  `CodeMVC/tests/NhatTinMvc.Tests/WebhookIngestServiceTests.cs`. Không có primary constructor,
  collection expression, `required`, keyed DI, `TimeProvider`, `IExceptionHandler`, `MapGroup`,
  `[AsParameters]`. ⇒ không vướng API riêng của .NET 7/8.
- EF migrations gắn `ProductVersion "8.0.10"` + `HasPrecision(18,2)` (có từ EF 5, tương thích EF 6).
- Máy chỉ cài **.NET SDK 9.0.301** (không có global.json). SDK 9 build target net6 tốt vì runtime
  6.0.36 đã có. **KHÔNG** thêm global.json ghim 6.0 (sẽ hỏng build vì thiếu SDK 6).

## Thay đổi cụ thể (áp dụng cho mỗi solution)

### 1. Target framework
Mọi `.csproj`: `<TargetFramework>net8.0</TargetFramework>` → `net6.0`. Giữ `Nullable`, `ImplicitUsings`.

### 2. Hạ package (net8.0.10 → 6.x)
| Package | Từ | Về |
|---|---|---|
| Microsoft.EntityFrameworkCore.SqlServer | 8.0.10 | **6.0.36** |
| Microsoft.EntityFrameworkCore.Design | 8.0.10 | **6.0.36** |
| Microsoft.EntityFrameworkCore.InMemory | 8.0.10 | **6.0.36** |
| Microsoft.AspNetCore.Authentication.JwtBearer (CodeSandBox) | 8.0.10 | **6.0.36** |
| Microsoft.AspNetCore.Mvc.Testing (test) | 8.0.10 | **6.0.36** |
| Microsoft.Extensions.Http | 8.0.0 | **6.0.0** |
| System.IdentityModel.Tokens.Jwt (CodeSandBox) | 8.1.2 | **dòng 6.x khớp JwtBearer 6** (xác nhận lúc restore) |

**Giữ nguyên** (không phụ thuộc framework): Swashbuckle.AspNetCore 6.6.2, xunit 2.5.3,
xunit.runner.visualstudio 2.5.3, Microsoft.NET.Test.Sdk 17.8.0, coverlet.collector 6.0.0.

### 3. Sửa C# 11 → C# 10
`WebhookIngestServiceTests.cs`: đổi raw string `"""...json..."""` thành verbatim/escaped string thường.

### 4. Regenerate EF migrations bằng dotnet-ef 6.x
Đã cài sẵn `dotnet-ef 6.0.36` làm **local tool** ở gốc repo (`.config/dotnet-tools.json`) — dùng chung,
main cài một lần để subagent không tranh chấp. Với mỗi DbContext: **sau khi đã hạ package + build**,
xóa thư mục Migrations cũ và `dotnet ef migrations add InitialCreate` (offline, diff với model) →
snapshot & ProductVersion khớp EF 6. DbContext liên quan: SandboxDbContext, WebhookDbContext, MvcDbContext.

## Chiến lược thực thi song song (không xung đột)

| Nguồn tranh chấp | Cách xử lý |
|---|---|
| File nguồn | 1 subagent = 1 solution trọn vẹn → không file nào bị 2 agent sửa |
| SQL Server dùng chung | Unit test chạy **EF InMemory**; scaffold migration **offline** → build/test không đụng DB thật |
| Git index | Subagent chỉ sửa+build+test, **không commit**; main commit tuần tự |
| NuGet cache | Restore đồng thời an toàn (NuGet khóa file) |
| dotnet-ef local tool | Main cài sẵn 1 lần trước khi tung subagent |
| Live smoke (DB thật + port) | Pha riêng sau song song, main chạy tuần tự |

### Thứ tự
1. **Pha 1 (song song):** 3 subagent, mỗi cái một solution — hạ framework+package, sửa raw-string
   (CodeMVC), regenerate migration, `dotnet build` + `dotnet test`, báo cáo (không commit).
2. **Pha 2 (main, tuần tự):** live smoke từng app trên net6 — Sandbox (5080/5090) → WebHooks (5099)
   → MVC (5110): app boot, `Migrate()` tạo DB, endpoint chính trả 200. DB cần drop 1 lần
   (vd `NhatTinWebhooks` cũ) → nhờ user nếu auto-mode chặn drop.
3. **Pha 3 (main):** commit tuần tự theo solution; cập nhật memory. (CodeMVC hiện **untracked** →
   commit lần đầu, xác nhận với user trước.)

## Tiêu chí hoàn thành

- [ ] 3 solution build sạch trên `net6.0`
- [ ] Toàn bộ test xanh (68 SDK + ~26 sandbox/mvc)
- [ ] 3 app boot & phục vụ endpoint chính trên runtime net6 (live smoke)
- [ ] Migrations regenerate theo EF 6 (ProductVersion khớp)

## Ngoài phạm vi (YAGNI)

- Central Package Management / Directory.Build.props (là refactor riêng).
- Nâng cấp logic nghiệp vụ; chỉ đổi target + version, giữ nguyên hành vi.
- Live end-to-end webhook loop xuyên app (cần bước đăng ký subscription thủ công) — chỉ smoke boot+endpoint.
