# Stage 1 Checkpoint Duyệt - Nhất Tín Logistics

## Mục Tiêu Checkpoint

Checkpoint này dùng để quyết định có được chuyển từ Discovery sang Design hay chưa. Sau phản hồi Nhất Tín trong [05-CAU-HOI-GUI-NHATTIN.md](05-CAU-HOI-GUI-NHATTIN.md), quyết định hiện tại là **duyệt Stage 2 Design có điều kiện** cho các phần đã được xác nhận, chưa duyệt code sandbox chuẩn sản xuất đầy đủ cho các phần còn chờ IT/live evidence.

## Deliverable Đã Chuẩn Bị

| Deliverable | File | Trạng thái | Ghi chú |
| --- | --- | --- | --- |
| Tài liệu API offline | [../NhatTinAPIDocumentation/vi/README.md](../NhatTinAPIDocumentation/vi/README.md) | Đã có | 15 trang source, 16 file Markdown gồm README. |
| Auth analysis | [00-PHAN-TICH-XAC-THUC-NHATTIN.md](00-PHAN-TICH-XAC-THUC-NHATTIN.md) | Đã lập | JWT Bearer theo docs, chờ live credential. |
| API Inventory | [01-API-INVENTORY.md](01-API-INVENTORY.md) | Đã lập | 13 dòng inventory gồm auth, location, bill, print, webhook. |
| Endpoint Matrix | [02-ENDPOINT-MATRIX.md](02-ENDPOINT-MATRIX.md) | Đã lập | Có luồng triển khai tối thiểu. |
| Schema Mapping | [03-SCHEMA-MAPPING.md](03-SCHEMA-MAPPING.md) | Đã cập nhật | Đã bổ sung các điểm Nhất Tín xác nhận; vẫn giữ marker `Cần xác minh` cho phần chưa rõ. |
| Gap Analysis | [04-GAP-ANALYSIS.md](04-GAP-ANALYSIS.md) | Đã cập nhật | Một số gap đã chuyển sang `Đã xác nhận`; các gap còn lại chặn design đầy đủ. |
| Câu hỏi gửi Nhất Tín | [05-CAU-HOI-GUI-NHATTIN.md](05-CAU-HOI-GUI-NHATTIN.md) | Đã có phản hồi | Dùng làm nguồn cập nhật Stage 2 Design có điều kiện. |
| Testcase auth/location | [../Tests/TestCases/Testcase-NhatTin-Sandbox.md](../Tests/TestCases/Testcase-NhatTin-Sandbox.md) | Đã chuẩn bị | Chờ credential để chạy. |
| Script verify auth | [../Tests/Scripts/verify-nhattin-auth.ps1](../Tests/Scripts/verify-nhattin-auth.ps1) | Đã chuẩn bị | Đọc credential từ env vars, không hardcode secret. |
| Phạm vi design có điều kiện | [07-PHAM-VI-DESIGN-CO-DIEU-KIEN.md](07-PHAM-VI-DESIGN-CO-DIEU-KIEN.md) | Đã lập | Liệt kê phần được phép xây dựng trước và phần còn chờ cập nhật. |

## Những Gì Có Thể Chốt Từ Docs Offline

| Chủ đề | Kết luận |
| --- | --- |
| Auth client API | JWT Bearer. |
| Login | `POST /v1/auth/sign-in`. |
| Refresh token | `POST /v1/auth/refresh-token`. |
| Header gọi API | `Authorization: Bearer <access_token>`. |
| Sandbox API host theo docs | `https://apisandbox.ntlogistics.vn`. |
| Production API host theo docs | `https://apiws.ntlogistics.vn`. |
| Endpoint location nhẹ để verify | `GET /v3/loc/provinces`. |
| Response envelope phổ biến | `success`, `message`, `data`, nhưng cần crosscheck từng endpoint. |
| Nhóm API phải mô phỏng | Auth, Location, CalcFee, CreateBill, Update, Cancel, Revert, Print, Tracking, Webhook. |

## Những Gì Đã Được Nhất Tín Xác Nhận

| Chủ đề | Kết luận dùng cho Stage 2 Design có điều kiện |
| --- | --- |
| API host | Sandbox `https://apisandbox.ntlogistics.vn`; Production `https://apiws.ntlogistics.vn`. |
| Portal web | Sandbox `https://bodev.ntlogistics.vn`; Production `https://khachhang.ntlogistics.vn`. |
| Auth header | Gửi `Authorization` với access token ở mọi request. |
| Auth lỗi | Token hết hạn/sai/thiếu có thể trả `401`, client refresh token rồi retry một lần. |
| Địa danh | Tạo vận đơn sau `01/07/2025` dùng địa danh mới. |
| CreateBill | Có payload tối thiểu để dựng DTO/validation/test happy path. |
| `partner_id` | Bắt buộc, được tạo trên Web portal. |
| Cancel/Revert | Hủy trạng thái `1`, `2`; hoàn trả trạng thái `3`, `15`, `7`. |
| Tracking | Chỉ hỗ trợ tra cứu 1 đơn/lần; timestamp không có format/timezone cố định. |
| Webhook | Cấu hình qua portal; POS chọn method; không có signature; POS tự xử lý trùng lặp. |
| Print | Dùng `GET /v3/bill/print?do_code={bill_code}&partner_id={partner_id}`; query đúng là `do_code`. |

## Điểm Còn Chặn Design Đầy Đủ

| Điểm còn mở | Vì sao chặn phần đầy đủ | Evidence cần có |
| --- | --- | --- |
| Credential sandbox/UAT | Không gọi được API thật, không xác minh response thực tế. | Login thành công, token masked, response saved. |
| Token TTL/refresh rotation | Chưa biết TTL thật và refresh token cũ còn dùng được không. | Đo TTL hoặc có phản hồi IT Nhất Tín. |
| Master Data service/payment/cargo/status | Chưa seed được sandbox/state machine đầy đủ. | Bảng mã chính thức hoặc response Master Data. |
| CreateBill conditional return fields | `return_ward_code` và return address còn chờ IT. | Tạo đơn UAT với `is_return_org=0/1` và lưu lỗi thật. |
| Webhook ACK/retry/timeout | Chưa biết status/body ACK thành công và retry policy. | Spec webhook hoặc captured webhook UAT. |
| Print response format | Chưa biết in nhãn trả binary/link/HTML/JSON. | Gọi print thành công và lưu status/content-type/body shape. |
| Tracking query/body | Docs mâu thuẫn GET với JSON body. | Chốt cách gọi thật với API sandbox. |
| Rate limit/asset | Chưa biết quota, TTL/quyền truy cập link ảnh. | Phản hồi IT Nhất Tín hoặc evidence UAT. |

## Quyết Định Đề Xuất

| Phương án | Khi nào chọn | Hệ quả |
| --- | --- | --- |
| Không chọn phương án tiếp tục chờ | Không có phản hồi đối tác hoặc không có đủ điểm xác nhận tối thiểu | Không còn là trạng thái hiện tại vì Nhất Tín đã phản hồi một phần. |
| Duyệt Design có điều kiện | Đã có phản hồi cho base URL, auth header, địa danh mới, CreateBill, `partner_id`, tracking, webhook, print query | **Phương án đang chọn.** Có thể thiết kế skeleton/adapter cho phần đã xác nhận, mọi phần còn thiếu phải để assumption/config rõ ràng. |
| Duyệt Design đầy đủ | Đã có live evidence và trả lời các blocker chính | Bắt đầu Phase 2 Design Clean Architecture/Data Model/Sequence Diagram. |

## Phạm Vi Có Thể Làm Ngay

1. Environment config theo API host/portal host đã xác nhận.
2. Auth client Bearer token, refresh và retry một lần khi gặp `401`.
3. Location adapter ưu tiên địa danh mới cho CreateBill/CalcFee.
4. CreateBill DTO và validation happy path theo payload tối thiểu.
5. Operation rule cho cancel/revert theo trạng thái đã xác nhận.
6. Tracking polling theo từng `bill_code`, parser timestamp tolerant.
7. Webhook receiver mặc định `POST`, không signature, lưu raw payload và dedupe nội bộ.
8. Print request builder dùng `do_code` và `partner_id`, chưa chốt storage/preview cho tới khi biết response type.

## Việc Bạn Cần Xử Lý Sau

1. Cập nhật thêm vào [05-CAU-HOI-GUI-NHATTIN.md](05-CAU-HOI-GUI-NHATTIN.md) khi Nhất Tín/IT phản hồi các mục còn mở.
2. Cấp credential qua biến môi trường khi tiện ngồi máy.
3. Chạy script verify auth:

```powershell
$env:NHATTIN_USERNAME = '<sandbox_username>'
$env:NHATTIN_PASSWORD = '<sandbox_password>'
powershell -NoProfile -ExecutionPolicy Bypass -File .\Tests\Scripts\verify-nhattin-auth.ps1
```

4. Lưu kết quả theo [../Tests/Results/EVIDENCE-NHATTIN-SANDBOX-TEMPLATE.md](../Tests/Results/EVIDENCE-NHATTIN-SANDBOX-TEMPLATE.md).
5. Dùng [07-PHAM-VI-DESIGN-CO-DIEU-KIEN.md](07-PHAM-VI-DESIGN-CO-DIEU-KIEN.md) làm phạm vi đầu vào cho Stage 2 Design.

## Tiêu Chí Để Chuyển Sang Design Đầy Đủ

- Login JWT pass trên sandbox/UAT thật.
- `GET /v3/loc/provinces` pass bằng Bearer token.
- Có `partner_id` và xác nhận cách truyền `partner_id` cho từng endpoint cần dùng.
- Có Master Data tối thiểu: service, payment method, cargo type, bill status.
- Có quyết định rõ về webhook retry/timeout/ACK và captured request thật xác nhận không có signature.
- Có quyết định rõ về tracking query/body.
- Có quyết định rõ về print response format/content-type.
