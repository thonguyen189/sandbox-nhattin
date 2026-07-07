# CodeMVC — POS reference demo cho NhatTin Logistics (thiết kế)

- **Ngày:** 2026-07-07
- **Trạng thái:** đã duyệt, đang triển khai
- **Liên quan:** [[nhattin-sandbox-project]] · SDK design `2026-07-06-nhattin-logistics-sdk-design.md` · SQL migration `2026-07-07-nhattin-sqlserver-migration-design.md`

## 1. Mục tiêu

`CodeMVC` là **web app demo trực quan trọn luồng** đóng vai POS-client: dùng **SDK (đóng gói DLL/NuGet)** gọi
sandbox nội bộ (`CodeSandBox`), tự nhận webhook đổi trạng thái, và hiển thị trọn vòng đời một vận đơn **live**
trong một UI. Không phải reference-grade template, không phải test-harness — ưu tiên nhìn thấy luồng chạy
end-to-end.

Ba yêu cầu gốc: (1) dùng lại DLL build từ SDK; (2) nối thông luồng với sandbox + webhook, đồng nhất luồng dữ
liệu; (3) có các chức năng tái hiện kết nối sandbox với nghiệp vụ API tương ứng.

## 2. Quyết định chốt

| # | Quyết định | Chốt |
|---|---|---|
| 1 | Mục đích | Demo trực quan trọn luồng |
| 2 | Webhook vào MVC | MVC tự nhận qua endpoint riêng, parse bằng `NhatTinWebhookParser` của SDK |
| 3 | Tham chiếu SDK | Artifact đóng gói: `dotnet pack` → `local-feed` → `PackageReference` |
| 4 | Phạm vi nghiệp vụ | Toàn bộ: Auth, Location, CalcFee, Create, Update, Tracking, Cancel, Revert, Print |
| 5 | Lưu trữ | SQL Server DB riêng `NhatTinMvc` (EF Core) |
| 6 | Live update | SignalR real-time push |
| 7 | Kiến trúc | Single-project thực dụng (Controllers → Services → DbContext) |
| 8 | Cổng MVC | `:5110` |
| 9 | Auth | Manual token mode (`AutoAuthenticate=false`), login seed token + nút Refresh |
| 10 | Test | Test project nhẹ (EF InMemory + SDK stub) |
| 11 | CodeWebHooks | Vẫn tồn tại song song làm referee/evidence — MVC không thay thế |

## 3. Kiến trúc & luồng dữ liệu

```
Browser (Razor+Bootstrap) ──HTTP form/AJAX──▶ NhatTinMvc.Web (net8, :5110)
        ▲  SignalR push                          │  Controllers → Services(SDK) → MvcDbContext(SQL)
        └────────────────────────────────────────┤  Hubs/BillStatusHub
                                                  │  /webhooks/nhattin/status  ◀── sandbox dispatch
   SDK.dll(net6) ─ Auth/Bill/Location ───────────┼──HTTP──▶ CodeSandBox API (:5080)
   SandboxControlClient ─ simulate-status ────────┘         AdminPortal (:5090) [đăng ký subscription 1 lần]
                                                            SQL Server: NhatTinMvc
```

**Vòng chảy một vận đơn:**
1. Login → `Auth.SignInAsync` → seed `ITokenStore` (access+refresh+expiry), bắt `partner_id`.
2. Dựng địa chỉ → `Location.GetProvinces/Districts/Wards` (dropdown phụ thuộc, AJAX).
3. Tính phí → `Bill.CalcFeeAsync` → bảng dịch vụ + phí.
4. Tạo đơn → `Bill.CreateAsync` → lưu `TrackedBill`.
5. *(1 lần)* Đăng ký subscription `CallbackUrl=http://localhost:5110/webhooks/nhattin/status` qua AdminPortal.
6. Giả lập trạng thái ngay trong MVC → `SandboxControlClient` gọi `POST /sandbox/bills/{code}/simulate-status`
   → sandbox `HttpWebhookDispatcher` bắn webhook về MVC.
7. `WebhooksController` nhận → `WebhookIngestService`: lưu raw → `NhatTinWebhookParser.TryParse` → DedupeKey →
   nếu mới: lưu `BillStatusEvent`, update `TrackedBill`, `Clients.All.BillStatusChanged(...)` → UI đổi badge live.
8. Song song: `Tracking`/`Update`/`Cancel`/`Revert`/`Print`.

## 4. Bố cục

```
CodeMVC/
├─ NhatTinMvc.sln
├─ nuget.config                         # source: ./local-feed + nuget.org
├─ local-feed/                          # NhatTinLogistics.Sdk.0.3.0.nupkg (dotnet pack)
├─ src/NhatTinMvc.Web/                  # net8.0
│  ├─ Controllers/  Account · Location · Fee · Bills · Webhooks
│  ├─ Services/     IShippingService/ShippingService · ISandboxControl/SandboxControlClient
│  │               IWebhookIngestService/WebhookIngestService
│  ├─ Data/         MvcDbContext · Entities/{TrackedBill,BillStatusEvent} · Migrations/
│  ├─ Hubs/         BillStatusHub
│  ├─ Models/       ViewModels (Login, Fee, CreateBill, BillList, BillDetail, ...)
│  ├─ Views/        Account · Fee · Bills · Shared/_Layout
│  ├─ wwwroot/      lib/signalr, js/site.js, css/site.css
│  ├─ appsettings.json                  # CHANGE_ME (commit)
│  ├─ appsettings.Local.json            # git-ignored: SQL + sandbox creds
│  ├─ Properties/launchSettings.json    # :5110
│  └─ Program.cs
├─ tests/NhatTinMvc.Tests/              # xUnit + EF InMemory + SDK stub handler
└─ README.md
```

## 5. Tham chiếu SDK dạng gói

- `dotnet pack CodeSDK/src/NhatTinLogistics.Sdk/NhatTinLogistics.Sdk.csproj -c Release -o CodeMVC/local-feed`.
- `nuget.config`: thêm `<add key="local-feed" value="./local-feed" />` cạnh nuget.org.
- MVC csproj: `<PackageReference Include="NhatTinLogistics.Sdk" Version="0.3.0" />`.
- README ghi lệnh pack + `dotnet nuget locals` khi bump version (tránh cache gói cũ).

## 6. Mô hình dữ liệu (DB `NhatTinMvc`, `decimal(18,2)`)

**TrackedBill**: Id, BillCode(unique), RefCode, PartnerId, CreatedAt, SenderName/Phone/Address,
ReceiverName/Phone/Address, Weight, TotalFee, ServiceName, LastStatusId, LastStatusName, LastStatusAt,
RawCreateResponse.

**BillStatusEvent**: Id, TrackedBillId(FK, nullable — event có thể tới trước khi khớp bill), BillCode, StatusId,
StatusName, StatusTime, PushTime, Reason, Source(`Webhook`|`Tracking`), ReceivedAt, RawPayload,
**DedupeKey**=`{bill_no}|{status_id}|{status_time}` (nullable khi thiếu phần).

Filtered unique index `DedupeKey WHERE DedupeKey IS NOT NULL` → self-dedupe (webhook NhatTin không idempotency key).

## 7. Nghiệp vụ ↔ màn hình

| Màn hình | Action | SDK / helper |
|---|---|---|
| Login / Refresh | `Account/Login`, `Account/Refresh` | `Auth.SignInAsync` / `RefreshTokenAsync` → seed `Tokens` |
| Địa chỉ (AJAX) | `Location/Provinces\|Districts\|Wards` | `Location.*` |
| Tính phí | `Fee/Index` (GET form, POST calc) | `Bill.CalcFeeAsync` |
| Tạo đơn | `Bills/Create` | `Bill.CreateAsync` → lưu TrackedBill |
| Danh sách | `Bills/Index` | đọc TrackedBill |
| Chi tiết + live | `Bills/Details/{code}` | `Bill.TrackingAsync` + BillStatusEvent + SignalR |
| Cập nhật | `Bills/Update/{code}` | `Bill.UpdateAsync` |
| Hủy / Hoàn | `Bills/Cancel` / `Bills/Revert` | `Bill.CancelAsync` / `RevertAsync` |
| In | `Bills/Print/{code}` | `Bill.PrintAsync` / `GetPrintUrl` |
| Giả lập trạng thái (sandbox-only) | `Bills/Simulate` | `SandboxControlClient` → `/sandbox/bills/{code}/simulate-status` |

Mọi call SDK trả `NhatTinResponse<T>`: business-fail (`IsSuccess=false`) hiển thị `Message` dạng alert, không ném;
chỉ transport/JSON/auth ném `NhatTinApiException` → trang lỗi thân thiện.

## 8. Webhook + SignalR

- `WebhooksController`: `[HttpPost/Put/Get("/webhooks/nhattin/status")]` — nhận mọi method, đọc raw body, giao
  `WebhookIngestService`. Trả `{success:true,message:"ACK"}` (hoặc "ACK (duplicate ignored)").
- `WebhookIngestService`: lưu raw → `NhatTinWebhookParser.TryParse` → DedupeKey → check trùng (query + backstop
  `DbUpdateException` do unique index) → nếu mới lưu event, khớp `TrackedBill` theo BillCode, update Last* →
  `IHubContext<BillStatusHub>.Clients.All.SendAsync("BillStatusChanged", {billCode,statusId,statusName,statusTime})`.
- `BillStatusHub` tại `/hubs/bill-status`. `site.js`: connect, lắng `BillStatusChanged`, cập nhật badge/row theo
  `data-bill-code`.

## 9. Cấu hình, cổng, secret

- MVC `:5110` (launchSettings, không xung đột 5080/5090/5099).
- `appsettings.json` (commit, placeholder):
  - `ConnectionStrings:MvcDb = "CHANGE_ME"`
  - `NhatTin:BaseUrl = "http://localhost:5080"` (trỏ sandbox nội bộ, KHÔNG phải NTL thật)
  - `NhatTin:PartnerId = null` (tự bắt từ login)
  - `Sandbox:BaseUrl = "http://localhost:5080"` (cho SandboxControlClient)
  - `Sandbox:DemoUsername/DemoPassword` (điền form login sẵn, tuỳ chọn)
- `appsettings.Local.json` (git-ignored): conn-string thật (`Server=192.168.200.8;Database=NhatTinMvc;User Id=vipos;...;TrustServerCertificate=True;Encrypt=False`) + creds sandbox.

## 10. DI (Program.cs)

```csharp
builder.Services.AddNhatTinLogisticsClient(o => {
    o.BaseUrl = cfg["NhatTin:BaseUrl"];   // http://localhost:5080
    o.AutoAuthenticate = false;           // manual token mode
    o.Environment = NhatTinEnvironment.Sandbox;
});
builder.Services.AddDbContext<MvcDbContext>(opt => opt.UseSqlServer(cfg.GetConnectionString("MvcDb")));
builder.Services.AddScoped<IShippingService, ShippingService>();
builder.Services.AddScoped<IWebhookIngestService, WebhookIngestService>();
builder.Services.AddHttpClient<ISandboxControl, SandboxControlClient>(c => c.BaseAddress = new(cfg["Sandbox:BaseUrl"]));
builder.Services.AddSignalR();
builder.Services.AddControllersWithViews();
// ... app.MapControllerRoute + app.MapHub<BillStatusHub>("/hubs/bill-status");
```

`ITokenStore` là singleton (mặc định DI của SDK là singleton) → token giữ qua request cho demo một-người-vận-hành.
`NhatTinLogisticsClient` scoped; đăng nhập seed vào singleton token store.

## 11. Kiểm thử & nghiệm thu

- `tests/NhatTinMvc.Tests` (xUnit, EF InMemory, SDK stub `HttpMessageHandler`):
  1. `WebhookIngestService` dedupe: cùng DedupeKey → 1 row + ACK-duplicate.
  2. `WebhookIngestService` parse + update TrackedBill.Last*.
  3. `ShippingService` map `CreateAsync` thành công → TrackedBill đúng field.
- Nghiệm thu thủ công (README): chạy receiver(5099)→sandbox(5080)→MVC(5110); đăng ký subscription; login; calc-fee;
  create; simulate-status; xem badge đổi live; cancel dọn bill test.

## 12. Ngoài phạm vi (YAGNI)

Multi-tenant/đa người dùng, auth per-session tách biệt, phân trang/lọc nâng cao, i18n, dark-mode, retry webhook
phía MVC, thay thế CodeWebHooks. Có thể thêm sau nếu cần.

## 13. Rủi ro & lưu ý

- Sandbox không có API tạo subscription → **đăng ký tay 1 lần** qua AdminPortal (:5090/Subscriptions). Bước bắt buộc,
  ghi rõ ở README.
- Agent auto-mode bị chặn ghi/tạo DB trên SQL Server chung → **user tự tạo DB `NhatTinMvc`** (hoặc để `Migrate()` tạo
  nếu login `vipos` đủ quyền `dbcreator` — đã xác nhận ở migration trước).
- SDK target net6.0; MVC net8.0 tiêu thụ DLL net6 qua NuGet — tương thích, đã kiểm khi thiết kế.
- `simulate-status` là helper sandbox-only, không thuộc API NTL thật → tách riêng `SandboxControlClient`, đánh dấu rõ.
