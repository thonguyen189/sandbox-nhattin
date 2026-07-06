# Thiết kế: Nhất Tín Logistics Sandbox Emulator + Webhook Receiver

- **Ngày:** 2026-07-06
- **Phạm vi:** `CodeSandBox/` (giả lập API Nhất Tín) và `CodeWebHooks/` (bên nhận callback / referee).
- **Ngoài phạm vi:** `CodeMVC/` (client/POS integration thật) — để trống, làm sau.

## 1. Bối cảnh

Thư mục này (`NhatTin-logistics-sandbox`) là nơi chứa **code thuần** cho việc tích hợp Nhất Tín Logistics, tách biệt khỏi thư mục song song `NhatTin-logistics` (workspace phân tích/kế hoạch: `CLAUDE.md`, `Promt/`, `PhanTich-ThaoTac-DongBo/`, `Handoff-TichHopDoiTac/`, `Tests/` quản lý dự án...). Hai thư mục không đồng bộ tự động; thư mục kia được dùng làm **tham khảo** khi cần.

Nguồn sự thật cho hành vi API: `NhatTinAPIDocumentation/vi/` (offline copy của `https://docs.ntlogistics.vn/docs/vi`).

### Đã tham khảo từ `NhatTin-logistics` (không copy nguyên, chỉ rút thông tin)

- Quy ước kiến trúc/port chuẩn công ty (từng dùng cho Tingee): Clean Architecture (Domain → Application → Infrastructure → Api/AdminPortal), port cố định `5080/7080` (Sandbox API), `5090/7090` (Sandbox AdminPortal), `5099` (Webhook Receiver).
- JWT Bearer đã xác nhận là cơ chế xác thực thật của Nhất Tín (không phải HMAC).
- Danh sách điểm **chưa xác nhận** cần giữ nguyên là điểm mở, không tự suy diễn thành đặc tả (xem mục 9).
- Có một plan trước đó (`docs/superpowers/plans/2026-07-06-nhattin-code-projects.md` trong `NhatTin-logistics`) đã phác thảo cấu trúc tương tự nhưng dùng lưu trữ in-memory và gắn với system configuration của POS. Thiết kế này **thay** lưu trữ bằng SQLite và **bỏ** phần gắn kết với POS config vì `CodeMVC` (nơi cần đọc cấu hình POS) không nằm trong phạm vi hôm nay.

## 2. Kiến trúc tổng thể

Hai solution .NET độc lập, chạy như hai "công ty" riêng biệt để mô phỏng đúng thực tế (Nhất Tín và đối tác là hai hệ thống khác nhau):

```
CodeSandBox/                        # Đóng vai Nhất Tín (emulator)
  NhatTinSandbox.sln
  src/
    NhatTinSandbox.Domain/          # Entities, enums, status rules
    NhatTinSandbox.Application/     # Use cases, DTOs (đúng field JSON theo docs), interfaces
    NhatTinSandbox.Infrastructure/  # EF Core (SQLite), JWT issuer, webhook dispatcher, seed data
    NhatTinSandbox.Api/             # Controllers: /v1/auth/*, /v3/loc/*, /v3/bill/*, Swagger
    NhatTinSandbox.AdminPortal/     # Razor/MVC: dashboard, đổi trạng thái, cấu hình webhook
  tests/
    NhatTinSandbox.Tests/           # xUnit

CodeWebHooks/                       # Đóng vai đối tác (referee nhận callback)
  NhatTinWebhookReceiver.sln
  src/
    NhatTinWebhookReceiver.Api/     # Nhận webhook, lưu raw evidence, Razor Pages xem log
  tests/
    NhatTinWebhookReceiver.Tests/   # xUnit

CodeMVC/                            # Để trống — ngoài phạm vi
```

**Tech stack:** .NET 8 (LTS, build bằng SDK 9.0.301 hiện có trên máy — tương thích ngược, không cần cài thêm), ASP.NET Core Web API + MVC/Razor Pages, EF Core, xUnit.

**Lưu trữ:** SQLite (file cục bộ), thay vì SQL Server — do server 192.168.200.8 hiện không ổn định. Mỗi solution có 1 file DB riêng:

- `CodeSandBox/src/NhatTinSandbox.Api/App_Data/nhattin-sandbox.db`
- `CodeWebHooks/src/NhatTinWebhookReceiver.Api/App_Data/nhattin-webhooks.db`

EF Core Migrations (Code-First) cho cả hai. Nếu sau này server SQL Server ổn định trở lại và muốn chuyển, chỉ cần đổi connection string + provider (`UseSqlite` → `UseSqlServer`) vì tầng Domain/Application không phụ thuộc provider.

**Ports (theo đúng quy ước công ty):**

| Project | HTTP | HTTPS |
| --- | --- | --- |
| NhatTinSandbox.Api | 5080 | 7080 |
| NhatTinSandbox.AdminPortal | 5090 | 7090 |
| NhatTinWebhookReceiver.Api | 5099 | — |

## 3. Data model

### DB `nhattin-sandbox.db` (CodeSandBox)

| Bảng | Mục đích |
| --- | --- |
| `PartnerAccounts` | Tài khoản đăng nhập giả lập (Username, PasswordHash, PartnerId, IsActive). Seed sẵn 1 tài khoản demo. |
| `RefreshTokens` | Vòng đời refresh token (AccountId, TokenHash, ExpiresAt, IsRevoked) để giả lập đúng luồng làm mới token. |
| `WebhookSubscriptions` | URL callback đã đăng ký (PartnerId, CallbackUrl, IsActive). Seed mặc định trỏ tới `http://localhost:5099/webhooks/nhattin/status` để 2 project chạy được với nhau ngay không cần cấu hình thêm. |
| `Bills` | Toàn bộ field vận đơn theo đúng tên trong `createbill.md`/`updatebill.md` (ref_code, package_no, weight/width/length/height, cargo_content, service_id, payment_method_id, cod_amount, cargo_value, cargo_type_id, sender/receiver/return fields, is_draft, is_return_doc, bill_type, các loại phí...). |
| `BillStatusHistories` | Lịch sử đổi trạng thái (BillId, StatusId, StatusName, ChangedAt, Reason) — phục vụ mảng `histories` khi tracking và là nguồn kích hoạt webhook. |
| `WebhookDeliveryLogs` | Log mỗi lần sandbox gửi webhook đi (BillId, SubscriptionId, PayloadJson, HttpStatusCode, Success, AttemptedAt, ResponseBody) để xem/gửi lại thủ công trên AdminPortal. |
| `Provinces` / `Districts` / `Wards` | Seed tập đại diện (không phải toàn bộ danh mục hành chính VN — không có nguồn dữ liệu đầy đủ). Bao gồm đúng các mã đã xuất hiện trong ví dụ tài liệu (tỉnh `11`/`01`/`79`, phường `00004`/`25750`/`27007`) cộng thêm vài mã mẫu khác, hỗ trợ cả đơn vị hành chính CŨ và MỚI (cờ `is_new`). |
| `MasterData` (Service/PaymentMethod/CargoType/BillStatus) | Seed theo danh mục đã biết trong `00-thong-tin-ket-noi.md`. Không khóa cứng validation — chấp nhận id lạ (log cảnh báo) vì danh mục biết là chưa đầy đủ. |

### DB `nhattin-webhooks.db` (CodeWebHooks)

| Bảng | Mục đích |
| --- | --- |
| `ReceivedWebhooks` | Lưu **nguyên văn** mọi request đến (ReceivedAt, HttpMethod, HeadersJson, RawBody), cộng các trường đã parse được nếu hợp lệ (BillNo, StatusId, StatusName, RefCode, IsValidPayload). Lưu cả khi parse thất bại — không tự "sửa" dữ liệu, chỉ ghi nhận. |

## 4. API giả lập (CodeSandBox.Api)

Bám sát đúng route/field từng file trong `NhatTinAPIDocumentation/vi/`:

| Route | Method | Nguồn doc |
| --- | --- | --- |
| `/v1/auth/sign-in` | POST | `authentication.md` |
| `/v1/auth/refresh-token` | POST | `authentication.md` |
| `/v3/loc/provinces` | GET | `location/provinces.md` |
| `/v3/loc/districts` | GET | `location/districts.md` |
| `/v3/loc/wards` | GET | `location/wards.md` |
| `/v3/bill/create` | POST | `bill/createbill.md` |
| `/v3/bill/update-shipping` | POST | `bill/updatebill.md` |
| `/v3/bill/destroy` | POST | `bill/cancelbill.md` |
| `/v3/bill/revert-bill` | POST | `bill/revertbill.md` |
| `/v3/bill/calc-fee` | POST | `bill/calcfee.md` |
| `/v3/bill/tracking` | GET | `bill/trackingbill.md` |
| `/v3/bill/print` | GET | `bill/printbill.md` |

Ghi chú triển khai:

- Toàn bộ response bọc trong `{ success, message, data }` đúng như docs.
- JWT thật (ký + verify), TTL mặc định **15 phút / 60 phút** (access/refresh) — cấu hình được qua `appsettings.json`. Không dùng nguyên "1m"/"2m" như ví dụ trong doc vì TTL thật của Nhất Tín chưa được xác nhận và 1 phút không thực tế để test.
- `calc-fee`: công thức tính phí giả định đơn giản, xác định (deterministic theo weight/cod/service), trả đúng shape mảng theo response mẫu — không phải bảng giá thật.
- `print`: trả JSON kèm `data.print_url` trỏ tới 1 trang label placeholder do sandbox tự sinh (định dạng response thật của Nhất Tín chưa xác nhận).
- `tracking`: implement theo đúng nghĩa đen của doc — GET với query string `bill_code` (không phải GET kèm JSON body).
- Có route nội bộ `/sandbox/bills/{code}/simulate-status` (POST) để test tự động hoá (script/CI) ép trạng thái mà không cần vào AdminPortal.

## 5. AdminPortal (CodeSandBox.AdminPortal)

Công cụ nội bộ chạy localhost, không cần đăng nhập (không internet-facing):

- **Bills**: danh sách + chi tiết từng bill, dropdown ép `status_id` bất kỳ trong Master Data (tự do, không ép state machine, để tối đa linh hoạt khi test). Lưu sẽ: ghi `BillStatusHistories` + tự động bắn webhook tới mọi `WebhookSubscriptions` đang active.
- **Webhook Subscriptions**: thêm/sửa/xoá URL callback.
- **Webhook Delivery Logs**: xem log gửi đi (thành công/thất bại, response), nút "Gửi lại" thủ công.
- **Reset**: nút xoá sạch dữ liệu Bills/Histories/Logs và seed lại từ đầu (giữ nguyên PartnerAccounts/MasterData/Location).

## 6. Luồng Webhook

1. Trigger: AdminPortal đổi status HOẶC gọi `/sandbox/bills/{code}/simulate-status`.
2. Dispatcher (`NhatTinSandbox.Infrastructure`) POST JSON tới từng `CallbackUrl` đang active, đúng field theo `bill/webhook.md`: `bill_no, ref_code, status_id, status_name, status_time, push_time, shipping_fee, is_partial, reason, weight, dimension_weight, length, width, height, expected_at`.
3. Không có chữ ký/HMAC (đã xác nhận thật sự không có) — gửi 1 lần, ghi log kết quả vào `WebhookDeliveryLogs`. Không tự động retry (chính sách retry thật chưa xác nhận) — thay vào đó có nút gửi lại thủ công.
4. `CodeWebHooks.Api` nhận tại `/webhooks/nhattin/status` (hỗ trợ POST/PUT/GET theo đúng "GET/POST/PUT" ghi trong doc), luôn lưu raw evidence trước, cố gắng parse sau, trả `{ success: true, message: "ACK", data: {} }`.

## 7. Testing

- xUnit theo từng layer: auth (issue/refresh token), bill (create/status transition/fee calc), webhook (payload shape đúng field docs), location catalog.
- 1 script PowerShell (`Tests/run-nhattin-cycle.ps1` bên trong `CodeSandBox` hoặc thư mục gốc) chạy full chu trình: đăng nhập → tạo bill → đổi trạng thái → xác nhận `CodeWebHooks` đã nhận → tracking lại bill.
- Toàn bộ test tự chứa (self-contained), không phụ thuộc credential/API thật của Nhất Tín.

## 8. Cấu hình & bí mật

- Không hardcode credential/token trong code hay test.
- `appsettings.json` chứa các giá trị non-secret (TTL, port, connection string SQLite - vốn không nhạy cảm vì là file local).
- Nếu sau này cần username/password thật của Nhất Tín (cho việc verify chéo với sandbox thật — ngoài phạm vi hôm nay), dùng `dotnet user-secrets`, không commit vào file.

## 9. Điểm chưa xác nhận — giữ nguyên là điểm mở (không tự suy diễn)

| Điểm | Cách sandbox xử lý hôm nay |
| --- | --- |
| TTL/refresh token thật | Dùng giá trị giả định, cấu hình được, ghi rõ đây là giả định |
| Chính sách retry/timeout/ACK/idempotency của webhook thật | Gửi 1 lần + log + nút gửi lại thủ công, không tự bịa chính sách retry |
| Định dạng response thật của `print` (PDF/HTML/link/JSON) | Trả JSON kèm link placeholder |
| `province_code`/`ward_code` (create/update bill) và `province_id`/`ward_id` (calc-fee) có cùng hệ mã không | Sandbox coi là cùng 1 bảng mã trong seed data, nhưng ghi chú đây là giả định cần xác minh với API thật |
| Toàn bộ danh mục hành chính VN | Chỉ seed tập đại diện, không đầy đủ toàn quốc |
| Bộ mã đầy đủ Master Data (service/payment/cargo/status) | Chỉ seed phần đã biết, cho phép id lạ đi qua thay vì hard-reject |

## 10. Ngoài phạm vi hôm nay

- `CodeMVC` (client/POS integration thật) và mọi kết nối tới cấu hình hệ thống POS thật (`SThietLapHeThongs`, `AbpSettings`...).
- Verify chéo với sandbox thật của Nhất Tín (`https://apisandbox.ntlogistics.vn`) bằng credential thật.
- SQL Server (192.168.200.8) — có thể chuyển sang sau nếu cần, kiến trúc đã tách provider để dễ đổi.
