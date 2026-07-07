# Endpoint Matrix - Nhất Tín Logistics

Ma trận này bám theo luồng TruePos/Sandbox, nguồn scrape tại [NhatTinAPIDocumentation/vi/](../NhatTinAPIDocumentation/vi/) và phản hồi Nhất Tín trong [05-CAU-HOI-GUI-NHATTIN.md](05-CAU-HOI-GUI-NHATTIN.md). Nếu tài liệu không có method/path cụ thể, ghi rõ `Chưa thấy trong tài liệu scrape`.

| Luồng TruePos/Sandbox | Endpoint Nhất Tín | Bắt buộc/Tùy chọn | Dùng cho Sandbox hay Client integration | Phụ thuộc auth | Ghi chú rủi ro |
| --- | --- | --- | --- | --- | --- |
| Authentication - đăng nhập | `POST /v1/auth/sign-in` | Bắt buộc | Cả Sandbox và Client integration | Không; endpoint cấp token | Nhất Tín xác nhận request authenticated gửi `Authorization` với access token. TTL thật còn chờ IT/live evidence. |
| Authentication - refresh token | `POST /v1/auth/refresh-token` | Bắt buộc | Cả Sandbox và Client integration | Không dùng access token; phụ thuộc `refresh_token` | Khi API trả `401`, client refresh token và retry một lần. Refresh rotation còn chờ IT Nhất Tín. |
| Location sync - tỉnh/thành | `GET /v3/loc/provinces` | Bắt buộc | Cả Sandbox và Client integration | Có, dùng Bearer token theo header chung | Tham số `is_new` docs ghi bắt buộc nhưng ví dụ không truyền; cần hỗ trợ mặc định và `is_new=1`. |
| Location sync - quận/huyện | `GET /v3/loc/districts` | Tùy chọn | Cả Sandbox và Client integration | Có | Sau chuyển đổi địa danh mới có thể không cần district cho create bill; vẫn cần nếu đồng bộ dữ liệu cũ. |
| Location sync - phường/xã | `GET /v3/loc/wards` | Bắt buộc | Cả Sandbox và Client integration | Có | Sau `01/07/2025` dùng địa danh mới cho tạo vận đơn; docs có hai kiểu query nên adapter vẫn cần hỗ trợ nhánh cũ/mới. |
| Create order/bill | `POST /v3/bill/create` | Bắt buộc | Cả Sandbox và Client integration | Có | Đã có payload tối thiểu từ Nhất Tín; field địa danh mới dùng `*_province_code`, `*_ward_code`. `return_ward_code` còn chờ IT. |
| Update bill | `POST /v3/bill/update-shipping` | Bắt buộc nếu TruePos cho sửa đơn; nếu không thì tùy chọn | Cả Sandbox và Client integration | Có | Nhất Tín chưa mô tả trạng thái được cập nhật; thiết kế guarded cho tới khi có ma trận transition. |
| Cancel bill | `POST /v3/bill/destroy` | Bắt buộc | Cả Sandbox và Client integration | Có | Nhất Tín xác nhận chỉ hủy trạng thái `1` Chưa thành công và `2` Chờ lấy hàng; request nhận mảng `bill_code`, cần xử lý partial success/failure. |
| Revert bill | `POST /v3/bill/revert-bill` | Tùy chọn | Cả Sandbox và Client integration | Có | Nhất Tín xác nhận áp dụng trạng thái `3` Đã lấy hàng, `15` đang vận chuyển, `7` không phát được; response tách `success[]`/`failed[]`. |
| Calc fee | `POST /v3/bill/calc-fee` | Bắt buộc | Cả Sandbox và Client integration | Có | Hỗ trợ cả địa danh cũ và mới; response trả danh sách service/fee nên UI phải chọn đúng service trước create bill. |
| Print waybill | `GET /v3/bill/print?do_code={bill_code}&partner_id={partner_id}` | Bắt buộc nếu TruePos in nhãn; tùy chọn nếu in trên portal Nhất Tín | Cả Sandbox và Client integration | Có theo header chung | Nhất Tín xác nhận query đúng là `do_code`; response format/content-type vẫn cần xác minh. |
| Tracking - polling | `GET /v3/bill/tracking` | Bắt buộc | Cả Sandbox và Client integration | Có | Nhất Tín phản hồi chỉ hỗ trợ 1 đơn/lần; vẫn cần xác minh dùng query `bill_code` hay body khi gọi API thật. |
| Webhook - nhận cập nhật trạng thái | Callback cấu hình qua Web portal | Bắt buộc cho đồng bộ tự động | Chủ yếu Client integration nhận callback; Sandbox cần module giả lập gửi webhook sang Webhook Receiver | Không có signature theo phản hồi Nhất Tín | POS chọn method, khuyến nghị `POST`; retry/timeout/ACK và idempotency key vẫn cần xác minh. |

## Nhìn theo luồng triển khai

| Thứ tự | Luồng | Endpoint/nguồn | Ghi chú thực thi Sandbox |
| --- | --- | --- | --- |
| 1 | Lấy token | `POST /v1/auth/sign-in`, sau đó refresh bằng `POST /v1/auth/refresh-token` | Seed tài khoản sandbox, phát Bearer token, mô phỏng lỗi `401` và `400` theo [authentication.md](../NhatTinAPIDocumentation/vi/authentication.md). |
| 2 | Đồng bộ địa danh | `GET /v3/loc/provinces`, `GET /v3/loc/districts`, `GET /v3/loc/wards` | Seed địa danh tối thiểu có mã mới `province_code/ward_code`; sau `01/07/2025` flow tạo vận đơn dùng địa danh mới. |
| 3 | Tính phí | `POST /v3/bill/calc-fee` | Trả nhiều lựa chọn dịch vụ/phí như docs; dùng cùng bảng `service_id`, `payment_method_id`. |
| 4 | Tạo vận đơn | `POST /v3/bill/create` | Sinh `bill_code`, trạng thái ban đầu `2` nếu không draft, lưu `ref_code` để đối soát với TruePos. |
| 5 | Cập nhật/hủy/chuyển hoàn | `POST /v3/bill/update-shipping`, `POST /v3/bill/destroy`, `POST /v3/bill/revert-bill` | Ràng buộc theo trạng thái đã xác nhận: cancel `1`/`2`; revert `3`/`15`/`7`; update guarded. |
| 6 | In nhãn | `GET /v3/bill/print?do_code=...&partner_id=...` | Request builder dùng `do_code`; response handler chờ xác minh PDF/link/HTML/JSON. |
| 7 | Theo dõi | `GET /v3/bill/tracking` | Polling từng bill, không batch nhiều đơn; parser timestamp tolerant và lưu raw payload. |
| 8 | Webhook | Callback cấu hình qua portal | Sandbox chủ động gửi payload webhook mẫu sang receiver của TruePos khi trạng thái đổi; không có signature, POS tự dedupe. |

## Quyết định đề xuất cho Phase 1

| Hạng mục | Đề xuất | Lý do |
| --- | --- | --- |
| Endpoint phải mô phỏng 100% | 13 dòng inventory trong [01-API-INVENTORY.md](01-API-INVENTORY.md) | Bao phủ toàn bộ endpoint/path/callback tìm thấy trong tài liệu scrape. |
| Endpoint cần crosscheck API thật sớm | Tracking, Print, Webhook | Tracking còn mâu thuẫn query/body; Print còn thiếu response type; Webhook còn thiếu ACK/retry/timeout. |
| Luồng tối thiểu happy path | Auth -> Location -> Calc fee -> Create bill -> Print -> Tracking -> Webhook | Đủ để TruePos tạo đơn, in, theo dõi và nhận trạng thái tự động. |
| Luồng thao tác sau tạo | Update -> Cancel hoặc Revert | Cần test theo trạng thái vì docs chỉ mô tả một phần điều kiện nghiệp vụ. |
