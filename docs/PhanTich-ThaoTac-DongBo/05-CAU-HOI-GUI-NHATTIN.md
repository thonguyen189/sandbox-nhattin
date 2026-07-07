# Câu Hỏi Gửi Nhất Tín Logistics

Mục tiêu của tài liệu này là gom các câu hỏi cần gửi Nhất Tín để gỡ blocker trước khi thiết kế/code sandbox. Các câu hỏi bám theo [04-GAP-ANALYSIS.md](04-GAP-ANALYSIS.md).

## 1. Credential Và Môi Trường

| ID       | Câu hỏi                                                                                 | Lý do cần hỏi                                        | Gap liên quan  | Phản hồi                                                                                                                                                                                                                            |
| -------- | --------------------------------------------------------------------------------------- | ---------------------------------------------------- | -------------- | ----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| Q-ENV-01 | Nhất Tín vui lòng cấp `username`, `password`, `partner_id` cho môi trường sandbox/UAT.  | Cần login thật và gọi thử API trước khi design/code. | GAP-01         | Tài khoản sandbox:<br>Username: thaison<br>Password: `<sandbox_password — lưu ở secret store, không commit>`<br><br>Tài khoản portal:<br>Portal URL: https://bodev.ntlogistics.vn<br>Username: admin@thaison.vn<br>Password: `<portal_password — lưu ở secret store, không commit>` |
| Q-ENV-02 | Base URL chính thức cho sandbox/UAT/production gồm API, print, portal, CDN/asset là gì? | Docs có API host và print host khác nhau.            | GAP-11, GAP-14 | Host:<br>Sandbox	https://apisandbox.ntlogistics.vn<br>Production	https://apiws.ntlogistics.vn<br><br>Portal Web:<br>Sandbox	https://bodev.ntlogistics.vn<br>Production	https://khachhang.ntlogistics.vn<br><br>                     |
| Q-ENV-03 | Môi trường sandbox có dữ liệu mẫu để tạo vận đơn, tracking, print và webhook không?     | Cần test end-to-end, không chỉ login/location.       | GAP-01, GAP-14 | Có                                                                                                                                                                                                                                  |
| Q-ENV-04 | Có IP whitelist, VPN, domain callback bắt buộc, hoặc giới hạn network nào không?        | Tránh fail khi test webhook/API thật.                | GAP-01         | Không (Giả định)                                                                                                                                                                                                                    |

## 2. Xác Thực Và Token

| ID        | Câu hỏi                                                                                                                        | Lý do cần hỏi                                                    | Gap liên quan | Phản hồi                                                   |
| --------- | ------------------------------------------------------------------------------------------------------------------------------ | ---------------------------------------------------------------- | ------------- | ---------------------------------------------------------- |
| Q-AUTH-01 | Với API client, JWT Bearer có phải là cơ chế xác thực duy nhất không? Có cần thêm chữ ký/HMAC/timestamp/header nào khác không? | Checklist bắt buộc xác minh auth thật, docs scrape chỉ thấy JWT. | GAP-02        | Gửi header `Authorization` với access token ở mọi request: |
| Q-AUTH-02 | TTL access token và refresh token thực tế ở sandbox/production là bao lâu?                                                     | Docs ví dụ `1m`/`2m`, có thể chỉ là mẫu.                         | GAP-03        | 24 giờ                                                     |
| Q-AUTH-03 | Refresh token có rotate không? Sau khi refresh, refresh token cũ còn dùng được không?                                          | Cần thiết kế token manager tránh race condition.                 | GAP-03        | sau khi refresh, token cũ không dùng được                  |
| Q-AUTH-04 | Khi token hết hạn/sai/thiếu header, HTTP status và body lỗi chuẩn là gì?                                                       | Cần viết ErrorCodes/test AUTH.                                   | GAP-07        | Có thể trả về lỗi 401, khi đó cần gọi làm refresh token    |

## 3. Master Data Và Bảng Mã

| ID      | Câu hỏi                                                                                                | Lý do cần hỏi                                                 | Gap liên quan  | Phản hồi                                 |
| ------- | ------------------------------------------------------------------------------------------------------ | ------------------------------------------------------------- | -------------- | ---------------------------------------- |
| Q-MD-01 | Vui lòng cung cấp bảng `service_id/service_name` đầy đủ.                                               | CreateBill/CalcFee cần validate và seed sandbox.              | GAP-05         | 1. Dịch vụ (service_id)                  |
| Q-MD-02 | Vui lòng cung cấp bảng `payment_method_id` đầy đủ.                                                     | CreateBill/CalcFee dùng payment method.                       | GAP-05         | 2. Hình thức thanh toán (payment_method) |
| Q-MD-03 | Vui lòng cung cấp bảng `cargo_type_id` đầy đủ.                                                         | CreateBill yêu cầu loại hàng.                                 | GAP-05         | 3. Loại hàng hóa (cargo_type_id)         |
| Q-MD-04 | Vui lòng cung cấp bảng `status_id/status_name` đầy đủ, gồm trạng thái terminal và trạng thái lỗi/hoàn. | Cần state machine, tracking, webhook, cancel/revert.          | GAP-05, GAP-06 | 4. Trạng thái đơn (status_id)            |
| Q-MD-05 | Vui lòng cung cấp bảng `histories[].log_status` và ý nghĩa các mã event tracking.                      | Tracking trả lịch sử event nhưng docs chưa có catalog đầy đủ. | GAP-06         | Không có bảng này                        |

## 4. Location Và Địa Danh Mới

| ID       | Câu hỏi                                                                                                          | Lý do cần hỏi                                  | Gap liên quan | Phản hồi          |
| -------- | ---------------------------------------------------------------------------------------------------------------- | ---------------------------------------------- | ------------- | ----------------- |
| Q-LOC-01 | Với đơn vị hành chính mới, `id`, `province_code`, `ward_code`, `s_province_id`, `s_ward_id` có cùng hệ mã không? | CreateBill dùng `*_code`, CalcFee dùng `*_id`. | GAP-18        | Có                |
| Q-LOC-02 | Sau mốc 01/07/2025, tạo vận đơn có bắt buộc dùng địa danh mới không?                                             | Cần chốt flow nhập địa chỉ.                    | GAP-18        | Dùng địa danh mới |
| Q-LOC-03 | API `/v3/loc/wards` khi `is_new=1` có cần `district_id` không, hay chỉ cần `province_id`?                        | Docs mô tả hai nhánh query, dễ implement sai.  | GAP-18        | Dùng province_id  |

## 5. Tạo Vận Đơn Và Nghiệp Vụ Bill

| ID        | Câu hỏi                                                                                      | Lý do cần hỏi                                     | Gap liên quan  | Phản hồi                                                                                                                                                                                                                                                                                                                                                                                 |
| --------- | -------------------------------------------------------------------------------------------- | ------------------------------------------------- | -------------- | ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| Q-BILL-01 | Payload tối thiểu hợp lệ để tạo vận đơn sandbox là gì?                                       | Cần test create bill thật và thiết kế validation. | GAP-04         | { "weight": 2, "width": 0, "length": 0, "height": 0, "service_id": 91, "payment_method_id": 10, "cod_amount": 0, "cargo_value": 0, "cargo_type_id": 2, "s_name": "TEST", "s_phone": "0333333333", "s_address": "số 10", "r_name": "TEST", "r_phone": "0333333333", "r_address": "123", "s_ward_code": "00004", "s_province_code": "01", "r_ward_code": "25750", "r_province_code": "79"} |
| Q-BILL-02 | `return_ward_code` có thật sự bắt buộc khi `is_return_org=0` không?                          | Docs đánh dấu bắt buộc nhưng mô tả có điều kiện.  | GAP-04         | Nếu is_return_org = 0 thì không cần truyền return_ward_code                                                                                                                                                                                                                                                                                                                              |
| Q-BILL-03 | `partner_id` có bắt buộc trong CreateBill/CalcFee/Update/Print không? Nếu có thì lấy từ đâu? | Một số docs ghi optional, một số endpoint cần.    | GAP-01, GAP-14 | đây là trường bắt buộc, trường này được tạo trên Web portal                                                                                                                                                                                                                                                                                                                              |
| Q-BILL-04 | Ma trận trạng thái nào được phép update/cancel/revert?                                       | Docs chỉ nêu một phần điều kiện.                  | GAP-20         | Cập nhât: Không mô tả trạng thái<br><br>Hoàn trả: Đã lấy hàng (3), đang vận chuyển (15), không phát được (7)<br><br>Hủy: Chưa thành công (1) và Chờ lấy hàng (2)                                                                                                                                                                                                                         |
| Q-BILL-05 | Có bảng mã lỗi nghiệp vụ cho tạo đơn/tính phí/update/cancel/revert không?                    | Cần xây catalog lỗi và testcase BIZ.              | GAP-07         | Cần trao đổi thêm với IT của Nhất Tín                                                                                                                                                                                                                                                                                                                                                    |

## 6. Tracking Và Đối Soát

| ID       | Câu hỏi                                                                      | Lý do cần hỏi                           | Gap liên quan  | Phản hồi                                                                                 |
| -------- | ---------------------------------------------------------------------------- | --------------------------------------- | -------------- | ---------------------------------------------------------------------------------------- |
| Q-TRK-01 | `GET /v3/bill/tracking` truyền `bill_code` bằng query string hay JSON body?  | Docs ghi GET nhưng sample là JSON body. | GAP-16         | truyền theo query string                                                                 |
| Q-TRK-02 | Tracking có hỗ trợ tra nhiều bill một lần không? Có giới hạn số lượng không? | Cần thiết kế polling đối soát.          | GAP-12, GAP-16 | Theo tài liệu chỉ hỗ trợ 1 đơn                                                           |
| Q-TRK-03 | Các timestamp trong tracking dùng timezone nào và format chuẩn là gì?        | Tracking có nhiều format timestamp.     | GAP-17         | timestamp trả về không thuộc format hay timezone cụ thể, cần handle với tường trường hợp |

## 7. Webhook

| ID      | Câu hỏi                                                                                             | Lý do cần hỏi                                       | Gap liên quan  | Phản hồi                                                                                                               |
| ------- | --------------------------------------------------------------------------------------------------- | --------------------------------------------------- | -------------- | ---------------------------------------------------------------------------------------------------------------------- |
| Q-WH-01 | Cách đăng ký/cấu hình callback URL webhook là gì? Qua portal hay API?                               | Docs chỉ nói gửi tới URL callback.                  | GAP-08, GAP-09 | Cấu hình qua web portal                                                                                                |
| Q-WH-02 | Method webhook chính thức là `POST`, `GET`, hay `PUT`?                                              | Docs ghi cả `GET/POST/PUT`.                         | GAP-10         | Do bên POS chọn                                                                                                        |
| Q-WH-03 | Webhook có header ký/signature không? Nếu có, xin thuật toán, secret, chuỗi ký, timestamp window.   | Cần xác thực nguồn gửi.                             | GAP-08         | không                                                                                                                  |
| Q-WH-04 | Retry policy khi receiver trả 4xx/5xx/timeout là gì? Timeout bao nhiêu giây?                        | Cần mô phỏng retry và vận hành receiver.            | GAP-09         | khi nhận thông tin từ Nhất Tín, webhook sẽ phản hồi nhận thành công,sau đó tự động xử lý nội bộ trong hệ thống của POS |
| Q-WH-05 | Response ACK webhook cần status/body nào để Nhất Tín xem là thành công?                             | Tránh webhook retry không cần thiết hoặc mất event. | GAP-09         | trả về status 200                                                                                                      |
| Q-WH-06 | Webhook có request id/idempotency key không? Nếu không, Nhất Tín khuyến nghị dedupe theo field nào? | Cần thiết kế WebhookHistory.                        | GAP-09         | POS tự xử lý trùng lặp                                                                                                 |

## 8. Print/Label Và Asset

| ID       | Câu hỏi                                                                                                                  | Lý do cần hỏi                     | Gap liên quan | Phản hồi                                                         |
| -------- | ------------------------------------------------------------------------------------------------------------------------ | --------------------------------- | ------------- | ---------------------------------------------------------------- |
| Q-PRN-01 | Print endpoint chính thức là `/v3/bill/print` trên API host hay `/v1/bill/print` trên `printdev/printdigi`?              | Docs có mâu thuẫn host/version.   | GAP-14        | GET: `v3/bill/print?do_code={bill_code}&partner_id={partner_id}` |
| Q-PRN-02 | Query đúng là `do_code` hay `bill_code`?                                                                                 | Docs dùng cả hai cách gọi.        | GAP-15        | do_code                                                          |
| Q-PRN-03 | Print API trả PDF/binary, HTML, redirect, link, hay JSON envelope? Content-Type là gì?                                   | Cần thiết kế lưu/preview/in nhãn. | GAP-14        | Trả về định dạng HTML                                            |
| Q-AST-01 | Các link ảnh trong tracking (`p_link_image`, `bill_image_link`, `document_image_link`) có cần auth không và TTL bao lâu? | Cần thiết kế proxy/lưu raw asset. | GAP-13        | Không (giả định)                                                 |

## 9. Rate Limit Và Vận Hành

| ID       | Câu hỏi                                                     | Lý do cần hỏi                                         | Gap liên quan | Phản hồi         |
| -------- | ----------------------------------------------------------- | ----------------------------------------------------- | ------------- | ---------------- |
| Q-OPS-01 | Rate limit/quota theo endpoint/token/IP là gì?              | Cần thiết kế polling, sync location, stress test.     | GAP-12        | Không (giả định) |
| Q-OPS-02 | Có khuyến nghị tần suất polling tracking không?             | Polling là nguồn đối soát chính nếu webhook chậm/mất. | GAP-12        | Không (giả định) |
| Q-OPS-03 | Có sandbox reset data hoặc cách tạo đơn test ổn định không? | Cần chạy test lặp lại.                                | GAP-01        | Có (giả định)    |

## Thứ Tự Ưu Tiên Gửi Hỏi

1. Credential, base URL, `partner_id`.
2. Auth/token, Master Data, CreateBill tối thiểu.
3. Webhook signature/retry/ACK.
4. Print/tracking/location mâu thuẫn.
5. Rate limit, asset, vận hành dài hạn.

