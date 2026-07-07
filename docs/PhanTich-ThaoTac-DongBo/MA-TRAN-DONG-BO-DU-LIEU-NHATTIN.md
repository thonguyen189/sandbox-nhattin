# Ma Trận Đồng Bộ Dữ Liệu Nhất Tín

Tài liệu này là bản draft vận hành sớm, lập từ docs offline, gap analysis và phản hồi Nhất Tín trong [05-CAU-HOI-GUI-NHATTIN.md](05-CAU-HOI-GUI-NHATTIN.md). Sau khi có credential/live evidence, cần cập nhật bằng hành vi thật.

## Nguyên Tắc Đồng Bộ

1. Polling tracking là nguồn đối soát chủ động.
2. Webhook là kênh cập nhật nhanh, nhưng chưa thể xem là nguồn duy nhất cho tới khi có retry/ACK/idempotency spec.
3. Lưu raw payload cho mọi response/webhook quan trọng để xử lý lệch schema.
4. Không hardcode Master Data nếu Nhất Tín cung cấp bảng mã có version/ngày cập nhật.
5. Các phần đã được xác nhận được phép đưa vào Stage 2 Design có điều kiện; các phần còn chờ IT Nhất Tín phải để dạng assumption/config.

## Cập Nhật Theo Phản Hồi Nhất Tín

| Hạng mục | Cập nhật vận hành |
| --- | --- |
| Environment | API host sandbox `https://apisandbox.ntlogistics.vn`, production `https://apiws.ntlogistics.vn`; portal sandbox `https://bodev.ntlogistics.vn`, production `https://khachhang.ntlogistics.vn`. |
| Auth | Gửi `Authorization` với access token ở mọi request; khi gặp `401` thì refresh token và retry một lần. |
| Location | Sau `01/07/2025`, flow tạo vận đơn dùng địa danh mới. |
| Partner | `partner_id` bắt buộc và được tạo trên Web portal. |
| Tracking | Chỉ tra cứu 1 đơn/lần; timestamp không có format/timezone cố định. |
| Webhook | Callback cấu hình qua portal; POS chọn method; không có signature; POS tự dedupe. |
| Print | Request dùng `GET /v3/bill/print?do_code={bill_code}&partner_id={partner_id}`; query đúng là `do_code`. |

## Ma Trận Tổng Quan

| Dữ liệu | Nguồn Nhất Tín | Cách đồng bộ đề xuất | Tự động/Thủ công | Tần suất gợi ý | Evidence cần lưu |
| --- | --- | --- | --- | --- | --- |
| Token access/refresh | `POST /v1/auth/sign-in`, `POST /v1/auth/refresh-token` | Token manager refresh trước hạn nếu biết TTL, hoặc retry một lần khi 401 | Tự động | Theo TTL thật; tạm thiết kế tolerant vì TTL/rotation còn chờ IT | Login/refresh response masked |
| Tỉnh/thành | `GET /v3/loc/provinces` | Đồng bộ danh mục theo `is_new` | Tự động + nút refresh thủ công | Hàng ngày hoặc khi Nhất Tín thông báo đổi | Count, sample item, timestamp |
| Quận/huyện | `GET /v3/loc/districts` | Đồng bộ legacy nếu còn dùng địa danh cũ | Tự động + thủ công | Hàng ngày/tuần | Count theo province |
| Phường/xã | `GET /v3/loc/wards` | Đồng bộ theo tỉnh mới hoặc district cũ | Tự động + thủ công | Hàng ngày hoặc theo tỉnh cần dùng | Count theo province/district |
| Bảng service/payment/cargo/status | Nhất Tín xác nhận nhóm bảng mã nhưng chưa cung cấp giá trị đầy đủ | Import từ tài liệu/bảng mã Nhất Tín cung cấp; tạm dùng code đã xác nhận cho design có điều kiện | Thủ công trước, tự động nếu có API | Khi có version mới | File nguồn, ngày hiệu lực |
| Báo giá | `POST /v3/bill/calc-fee` | Gọi realtime khi tạo đơn/đổi địa chỉ/dịch vụ | Tự động theo thao tác người dùng | Theo nhu cầu | Request/response raw |
| Vận đơn | `POST /v3/bill/create` | Gửi realtime khi chốt đơn | Tự động | Theo đơn hàng | `bill_code`, `ref_code`, fee snapshot |
| Cập nhật vận đơn | `POST /v3/bill/update-shipping` | Chỉ cho phép khi trạng thái hợp lệ | Tự động theo thao tác người dùng | Theo nhu cầu | Request/response raw |
| Hủy vận đơn | `POST /v3/bill/destroy` | Gửi realtime, xử lý partial result theo từng `bill_code` | Tự động + thao tác thủ công | Theo nhu cầu | Result từng bill |
| Chuyển hoàn | `POST /v3/bill/revert-bill` | Gửi realtime khi trạng thái cho phép | Thủ công có kiểm soát | Theo nhu cầu | `success[]`, `failed[]` |
| Tracking | `GET /v3/bill/tracking` | Polling từng `bill_code`; không batch nhiều đơn | Tự động + nút refresh | Sau tạo đơn, rồi định kỳ theo trạng thái | Snapshot, `histories[]`, raw payload, raw timestamp |
| Print label | `GET /v3/bill/print?do_code={bill_code}&partner_id={partner_id}` | Gọi khi cần in/preview; chỉ chốt request builder trước, response handler chờ content-type thật | Tự động theo thao tác người dùng | Theo nhu cầu | Status, Content-Type, link/file id hoặc raw body shape |
| Webhook trạng thái | Callback cấu hình qua portal | Receiver mặc định `POST`, không signature, lưu raw payload/header, dedupe tạm theo bill/status/time/hash | Tự động | Theo event Nhất Tín | Header, body, status ACK |

## Ma Trận Thao Tác Tự Động/Thủ Công

| Tình huống | Tự động | Thủ công | Ghi chú |
| --- | --- | --- | --- |
| Token hết hạn | Refresh token, retry request một lần | Re-login bằng credential nếu refresh fail | Cần lock refresh theo account để tránh race. |
| Địa danh đổi | Job sync định kỳ | Nút refresh danh mục theo tỉnh | Chờ xác minh địa danh mới/cũ. |
| Người dùng tạo đơn | CalcFee rồi CreateBill | Cho phép nhập lại địa chỉ/dịch vụ nếu lỗi validation | Lưu raw request/response cho audit. |
| Nhất Tín cập nhật trạng thái | Webhook receiver nhận và cập nhật trạng thái | Nút polling tracking theo bill | Webhook đã xác nhận không có signature; polling vẫn là đường đối soát chắc vì ACK/retry còn thiếu. |
| Webhook lỗi xử lý | Lưu failed event, dedupe, retry nội bộ nếu cần | Replay webhook từ admin | Cần ACK/retry policy từ Nhất Tín. |
| Lệch trạng thái TruePos/Nhất Tín | Polling tracking phát hiện lệch | Nút đối soát lại từng bill | Ưu tiên trạng thái từ tracking nếu webhook chậm/mất. |
| In nhãn lỗi | Retry print theo thao tác người dùng | In từ portal Nhất Tín nếu cần | Chờ xác minh print response format. |
| Reset sandbox nội bộ | Reset DB sandbox/client/webhook receiver | Nút reset admin | Chỉ áp dụng khi đã có emulator. |

## Dữ Liệu Cần Lưu Tối Thiểu Khi Thiết Kế

| Nhóm bảng/log | Nội dung |
| --- | --- |
| PartnerCredential/Settings | API host, portal host, username reference, partner_id, token metadata, không lưu password plaintext. |
| LocationCache | Province/district/ward, `is_new`, raw source item, sync timestamp. |
| MasterData | Service, payment method, cargo type, bill status, source version. |
| Shipment/Bill | `ref_code`, `bill_code`, status, fee snapshot, sender/receiver snapshot, raw create response. |
| TrackingHistory | Tracking snapshot, histories event, poll timestamp. |
| WebhookHistory | Headers, raw body, derived key/hash, processing status, ACK status. |
| AuditLog | User/manual action, API request intent, result, correlation id. |

## Các Điểm Chờ Xác Minh

- `GET /v3/bill/tracking` truyền `bill_code` bằng query hay body.
- Print response format/content-type và asset handling.
- Webhook retry, timeout, ACK, request id/idempotency key.
- Master Data chính thức cho service/payment/cargo/status.
- Quan hệ mã địa danh `*_code` và `*_id` sau chuyển đổi 2025.
- Rate limit/quota theo endpoint/token/IP.
