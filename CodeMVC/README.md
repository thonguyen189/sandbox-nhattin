# CodeMVC — Web demo POS tích hợp NhatTin Logistics

Web app demo (ASP.NET Core MVC, .NET 8) đóng vai **POS-client**: **dùng lại DLL build từ CodeSDK**
(gói NuGet qua local-feed), nối vào **sandbox nội bộ** (`CodeSandBox`, cổng `:5080`), **tự nhận webhook**
đổi trạng thái, và hiển thị **trọn vòng đời một vận đơn LIVE** bằng **SignalR**. Dữ liệu lưu ở **SQL Server**
với database riêng `NhatTinMvc` (EF Core).

> Đây là công cụ demo/tham chiếu nội bộ. Nó **không thay thế** `CodeWebHooks` (receiver vẫn chạy song song
> làm referee/evidence), và `simulate-status` chỉ là helper **sandbox-only**, không thuộc API NhatTin thật.

## Mục lục

1. [Mục đích](#1-mục-đích)
2. [Kiến trúc & luồng dữ liệu](#2-kiến-trúc--luồng-dữ-liệu)
3. [Nghiệp vụ / màn hình](#3-nghiệp-vụ--màn-hình)
4. [Yêu cầu](#4-yêu-cầu)
5. [Cách SDK được tiêu thụ (local-feed)](#5-cách-sdk-được-tiêu-thụ-local-feed)
6. [Cấu hình & secret](#6-cấu-hình--secret)
7. [Cách chạy](#7-cách-chạy)
8. [BƯỚC BẮT BUỘC 1 LẦN — đăng ký subscription](#8-bước-bắt-buộc-1-lần--đăng-ký-subscription)
9. [Kịch bản demo](#9-kịch-bản-demo)
10. [Kiểm thử](#10-kiểm-thử)
11. [Ghi chú](#11-ghi-chú)

---

## 1. Mục đích

Tái hiện trực quan **trọn luồng** một đơn vận chuyển đi qua hệ NhatTin, để đối tác/POS thấy được cách tích hợp:

- **Dùng lại DLL từ CodeSDK**: SDK được `dotnet pack` thành gói `NhatTinLogistics.Sdk` rồi tiêu thụ qua
  `PackageReference` (không tham chiếu project trực tiếp) — đúng cách một client thật sẽ dùng.
- **Nối thông sandbox + webhook**: mọi call nghiệp vụ đi qua SDK → sandbox `:5080`; sandbox đổi trạng thái
  sẽ **dispatch webhook** ngược về MVC.
- **Live**: webhook về → parse + dedupe → đẩy SignalR → badge/bảng sự kiện trên trình duyệt đổi ngay,
  không cần refresh.
- **Lưu trữ riêng**: DB SQL Server `NhatTinMvc` (2 bảng `TrackedBill`, `BillStatusEvent`), tách hẳn khỏi
  DB của sandbox/webhook receiver.

## 2. Kiến trúc & luồng dữ liệu

```
   Browser (Razor + Bootstrap)
        │  HTTP form / AJAX               ▲  SignalR push (BillStatusChanged)
        ▼                                 │
┌─────────────────────────────────────────────────────────────┐
│ NhatTinMvc.Web  (ASP.NET Core MVC, net8, cổng :5110)         │
│                                                              │
│  Controllers ──▶ Services ──▶ MvcDbContext (EF Core → SQL)   │
│   Account       ShippingService     ─ bọc SDK (Auth/Bill/Location)
│   Fee           WebhookIngestService ─ parse + dedupe + push SignalR
│   Bills         SandboxControlClient ─ gọi simulate-status (sandbox-only)
│   Location      Hubs/BillStatusHub  @ /hubs/bill-status
│   Webhooks  ◀── POST/PUT/GET /webhooks/nhattin/status  ◀──┐  │
└──────────────┬───────────────────────────────────────────┼──┘
               │ SDK.dll (net6, gói NuGet local-feed)       │ webhook dispatch
               ▼                                            │
        CodeSandBox API (:5080) ────────────────────────────┘
        AdminPortal (:5090)  ← đăng ký subscription 1 lần
        SQL Server → DB: NhatTinMvc
```

**Vòng chảy một vận đơn:** Đăng nhập (seed token vào SDK) → dựng địa chỉ (dropdown tỉnh→phường AJAX) →
tính phí → tạo đơn (lưu `TrackedBill`) → *(1 lần)* đăng ký subscription callback về MVC →
**giả lập trạng thái** trong MVC → sandbox bắn webhook → `WebhooksController` nhận →
`WebhookIngestService` lưu raw + `NhatTinWebhookParser.TryParse` + DedupeKey → nếu mới: lưu `BillStatusEvent`,
cập nhật `TrackedBill.Last*`, `Clients.All.BillStatusChanged(...)` → **UI đổi badge live**.

## 3. Nghiệp vụ / màn hình

| Màn hình / chức năng | Controller · Action | SDK / helper dùng |
|---|---|---|
| Đăng nhập / Làm mới token / Đăng xuất | `Account` · `Login`, `Refresh`, `Logout` | `Auth.SignInAsync` / `RefreshTokenAsync` → seed token store |
| Địa chỉ phụ thuộc (AJAX JSON) | `Location` · `Provinces`, `Districts`, `Wards` | `Location.GetProvinces/Districts/Wards` |
| Tính phí | `Fee` · `Index` (GET form, POST tính) | `Bill.CalcFeeAsync` |
| Tạo vận đơn | `Bills` · `Create` | `Bill.CreateAsync` → lưu `TrackedBill` |
| Danh sách vận đơn | `Bills` · `Index` | đọc `TrackedBill` từ DB |
| Chi tiết + trạng thái LIVE | `Bills` · `Details/{id}` | `Bill.TrackingAsync` + `BillStatusEvent` + SignalR |
| Cập nhật | `Bills` · `Update/{id}` | `Bill.UpdateAsync` |
| Hủy / Hoàn | `Bills` · `Cancel`, `Revert` | `Bill.CancelAsync` / `RevertAsync` |
| In vận đơn | `Bills` · `Print/{id}` | `Bill.PrintAsync` / `GetPrintUrl` |
| **Giả lập trạng thái** (sandbox-only) | `Bills` · `Simulate` | `SandboxControlClient` → `POST /sandbox/bills/{code}/simulate-status` |
| Nhận webhook | `Webhooks` · `Receive` (POST/PUT/GET `/webhooks/nhattin/status`) | `WebhookIngestService` + `NhatTinWebhookParser` |

Mọi call SDK trả `NhatTinResponse<T>`: lỗi nghiệp vụ (`IsSuccess=false`) hiển thị `Message` dạng alert,
không ném; chỉ lỗi transport/JSON/auth mới ném `NhatTinApiException` → trang lỗi thân thiện.

## 4. Yêu cầu

- **.NET SDK 8** (chạy tốt với SDK 9 đã cài). MVC target `net8.0`; SDK gói target `net6.0` — tương thích.
- **SQL Server** truy cập được (môi trường hiện tại: `192.168.200.8`). Login cần đủ quyền tạo/migrate DB
  (`vipos` đã xác nhận có `dbcreator`); nếu không, tạo sẵn DB `NhatTinMvc` bằng tay.
- **CodeSandBox API** (`:5080`) đang chạy — bắt buộc, MVC gọi mọi nghiệp vụ qua đó.
- **AdminPortal** (`:5090`) — cần để đăng ký subscription (bước 1 lần).
- **CodeWebHooks receiver** (`:5099`) — **tùy chọn**, chạy song song làm referee/evidence, không bắt buộc cho MVC.

## 5. Cách SDK được tiêu thụ (local-feed)

MVC **không** tham chiếu project SDK trực tiếp mà dùng **artifact đóng gói**:

```powershell
# Từ thư mục repo gốc — đóng gói SDK vào local-feed của CodeMVC
dotnet pack CodeSDK/src/NhatTinLogistics.Sdk/NhatTinLogistics.Sdk.csproj -c Release -o CodeMVC/local-feed
```

- `CodeMVC/nuget.config` khai báo source `local-feed` (`./local-feed`) cạnh `nuget.org`.
- `NhatTinMvc.Web.csproj`: `<PackageReference Include="NhatTinLogistics.Sdk" Version="0.3.0" />`.
- Gói hiện có sẵn trong repo: `CodeMVC/local-feed/NhatTinLogistics.Sdk.0.3.0.nupkg`.

**Khi bump version SDK:** sửa version trong `.csproj` cho khớp, `dotnet pack` lại. Nếu NuGet vẫn lấy gói cũ
từ cache thì xóa cache:

```powershell
dotnet nuget locals all --clear
```

## 6. Cấu hình & secret

`appsettings.json` (commit, dùng placeholder — **không chứa secret**):

| Key | Giá trị mặc định | Ý nghĩa |
|---|---|---|
| `ConnectionStrings:MvcDb` | `CHANGE_ME` | Conn-string SQL — đè ở `appsettings.Local.json` |
| `NhatTin:BaseUrl` | `http://localhost:5080` | SDK trỏ **sandbox nội bộ**, KHÔNG phải NTL thật |
| `Sandbox:BaseUrl` | `http://localhost:5080` | Đích cho `SandboxControlClient` (simulate-status) |
| `Sandbox:DemoUsername` / `DemoPassword` | `sandbox` / `sandbox123` | Điền sẵn form đăng nhập cho tiện demo |
| `WebhookCallbackUrl` | `http://localhost:5110/webhooks/nhattin/status` | URL để khai báo ở subscription |

**Tạo file secret cục bộ** `src/NhatTinMvc.Web/appsettings.Local.json` (đã git-ignored), điền conn-string thật:

```json
{
  "ConnectionStrings": {
    "MvcDb": "Server=192.168.200.8;Database=NhatTinMvc;User Id=vipos;Password=***;TrustServerCertificate=True;Encrypt=False"
  }
}
```

Khi khởi động, `Program.cs` nạp `appsettings.Local.json` (đè placeholder) rồi **tự `Migrate()` tạo DB
`NhatTinMvc`** nếu conn-string hợp lệ (khác `CHANGE_ME`). Nếu conn-string còn `CHANGE_ME` hoặc thiếu quyền,
app vẫn chạy nhưng log cảnh báo và các thao tác DB sẽ lỗi.

> Lưu ý: app **không** dùng HTTPS-redirect (webhook sandbox POST qua `http://localhost:5110`; redirect sẽ
> làm rớt request).

## 7. Cách chạy

Chạy theo thứ tự (mỗi tiến trình một cửa sổ). Lệnh chạy từ **thư mục repo gốc**:

```powershell
# (A) [tùy chọn] Webhook receiver — referee/evidence, cổng :5099
dotnet run --project CodeWebHooks/src/NhatTinWebhookReceiver.Api

# (B) Sandbox API — bắt buộc, cổng :5080  (Swagger: http://localhost:5080/swagger)
dotnet run --project CodeSandBox/src/NhatTinSandbox.Api

# (C) AdminPortal — cổng :5090  (dùng để đăng ký subscription)
dotnet run --project CodeSandBox/src/NhatTinSandbox.AdminPortal

# (D) MVC demo — cổng :5110
dotnet run --project CodeMVC/src/NhatTinMvc.Web
```

Tiện hơn: chạy `pwsh CodeMVC/scripts/run-all.ps1` để mở sẵn các cửa sổ (B), (C), (D) (thêm `-WithReceiver`
để kèm (A)). Xem [scripts/run-all.ps1](scripts/run-all.ps1).

- MVC: <http://localhost:5110>
- AdminPortal: <http://localhost:5090>
- Sandbox Swagger: <http://localhost:5080/swagger>

## 8. BƯỚC BẮT BUỘC 1 LẦN — đăng ký subscription

Sandbox **không có API tạo subscription**, nên webhook chỉ về được MVC sau khi bạn **thêm tay 1 lần**:

1. Mở AdminPortal: <http://localhost:5090> → mục **Subscriptions**.
2. Tạo subscription mới với **CallbackUrl** =

   ```
   http://localhost:5110/webhooks/nhattin/status
   ```

3. Lưu lại. Từ giờ mọi lần sandbox đổi trạng thái bill sẽ dispatch webhook về đúng endpoint MVC.

> Đây là **điều kiện tiên quyết** để phần "trạng thái LIVE" hoạt động. Bỏ qua bước này thì tạo/giả lập đơn
> vẫn chạy nhưng badge sẽ không tự cập nhật.

## 9. Kịch bản demo

1. **Đăng nhập** (`/Account/Login`) — form điền sẵn `sandbox` / `sandbox123`, bấm đăng nhập.
2. **Tính phí** (`/Fee`) — chọn tuyến (tỉnh/phường qua dropdown AJAX) + khối lượng → xem bảng dịch vụ & phí.
3. **Tạo vận đơn** (`/Bills/Create`) — điền người gửi/nhận, dịch vụ → tạo. App chuyển sang trang **Chi tiết**.
4. Ở trang **Chi tiết** (`/Bills/Details/{code}`), dùng **"Giả lập trạng thái"** — chọn trạng thái, ví dụ:
   - Đã lấy hàng = `3`
   - Đang giao = `13`
   - Đã giao = `4`
5. Sandbox đổi trạng thái và **bắn webhook** → xem **badge trạng thái + bảng sự kiện** cập nhật **LIVE**
   (nhờ webhook + SignalR), không cần refresh.
6. Có thể **Hủy** (`/Bills/Cancel`) để dọn các bill test.

## 10. Kiểm thử

```powershell
dotnet test CodeMVC/tests/NhatTinMvc.Tests
```

Test project nhẹ (xUnit + EF InMemory + SDK stub): kiểm dedupe webhook, parse + cập nhật `TrackedBill.Last*`,
và map `CreateAsync` → `TrackedBill`.

## 11. Ghi chú

- **MVC không thay thế CodeWebHooks.** Receiver `:5099` vẫn chạy song song làm evidence/referee; MVC chỉ là
  một subscriber độc lập có UI live.
- **`simulate-status` là helper sandbox-only**, không thuộc API NhatTin thật — vì vậy được tách riêng ở
  `SandboxControlClient`, đánh dấu rõ trong code và UI.
- **Dedupe:** webhook NhatTin không có idempotency key → MVC tự dedupe bằng `DedupeKey = {bill_no}|{status_id}|{status_time}`
  (filtered unique index), an toàn khi sandbox gửi lặp.
- **Token store là singleton** trong SDK → phù hợp demo một-người-vận-hành; đăng nhập một lần dùng chung qua
  các request.
