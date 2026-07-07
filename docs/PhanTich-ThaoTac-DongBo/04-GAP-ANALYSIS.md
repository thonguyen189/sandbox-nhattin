# Gap Analysis - Tích hợp Nhất Tín Logistics

## Phạm vi

Tài liệu này liệt kê các khoảng trống cần xử lý trước khi design/code Sandbox tích hợp Nhất Tín Logistics. Mức độ:

- `Blocker`: chưa rõ sẽ chặn thiết kế hoặc gọi API thật.
- `High`: có thể thiết kế tạm nhưng rủi ro lỗi nghiệp vụ lớn.
- `Medium`: cần xác minh để hoàn thiện mô phỏng/test.
- `Low`: nên làm rõ để tài liệu/vận hành sạch hơn.
- `Đã xác nhận`: đã có phản hồi đủ để đưa vào design, nhưng vẫn có thể cần live evidence trước khi chốt production.

Nguồn đọc:

- `NhatTinAPIDocumentation/vi/authentication.md`
- `NhatTinAPIDocumentation/vi/location/*.md`
- `NhatTinAPIDocumentation/vi/bill/*.md`
- `Handoff-TichHopDoiTac/04-CHECKLIST-DOI-TAC-MOI.md`
- Phản hồi Nhất Tín trong [05-CAU-HOI-GUI-NHATTIN.md](05-CAU-HOI-GUI-NHATTIN.md)

## Tổng quan phát hiện

Docs hiện có đủ khung chính cho JWT auth, location, tạo vận đơn, tính phí, tracking, webhook và in vận đơn. Phản hồi Nhất Tín đã xác nhận thêm base URL, auth header, địa danh mới, payload CreateBill tối thiểu, `partner_id`, tracking 1 đơn/lần, webhook không signature và print dùng `do_code`. Các điểm còn thiếu lớn nằm ở credential/live evidence, vòng đời token thực tế, Master Data chi tiết, webhook ACK/retry, rate limit, tracking request shape và chi tiết print/asset.

## Phạm vi được mở khóa có điều kiện

Chi tiết nằm trong [07-PHAM-VI-DESIGN-CO-DIEU-KIEN.md](07-PHAM-VI-DESIGN-CO-DIEU-KIEN.md). Có thể bắt đầu Stage 2 Design cho các module đã xác nhận, nhưng chưa được giả định các phần còn chờ IT Nhất Tín là hành vi cuối cùng.

## Gap chi tiết

| ID | Gap | Mức độ | Bằng chứng từ docs | Rủi ro nếu chưa xử lý | Bước kiểm chứng |
| --- | --- | --- | --- | --- | --- |
| GAP-01 | Credential sandbox/UAT/prod chưa có trong workspace | Blocker | `authentication.md` nói Nhất Tín cung cấp tài khoản theo từng môi trường; checklist yêu cầu xin credential sandbox/UAT. | Không thể crosscheck API thật, không xác minh TTL token, endpoint host, response lỗi thực tế. | Xin `username`, `password`, `partner_id`, base URL API và print URL cho sandbox/UAT; gọi thử `POST /v1/auth/sign-in`. |
| GAP-02 | Cơ chế auth thật là JWT hay có thêm ký request | Đã xác nhận | Nhất Tín phản hồi gửi header `Authorization` với access token ở mọi request. | Rủi ro còn lại là chưa có live credential để chứng minh request thật pass chỉ với access token. | Khi có credential, gọi `POST /v1/auth/sign-in` rồi `GET /v3/loc/provinces` chỉ với header `Authorization`. |
| GAP-03 | Token refresh lifecycle chưa đủ để thiết kế sản xuất | High | Docs có `jwt_token`, `token_expires_in`, `refresh_token`, `refresh_expires_in`; Nhất Tín phản hồi TTL có thể 1-2 tháng nhưng cần hỏi IT. | Race condition khi nhiều request cùng refresh; refresh token rotation có thể làm mất phiên; retry 401 không nhất quán. | Dùng credential UAT đo TTL thực tế, xác nhận refresh token có rotation bắt buộc, refresh token cũ còn dùng được không, và policy khi refresh hết hạn. |
| GAP-04 | Required fields tạo vận đơn có mâu thuẫn/điều kiện chưa rõ | High | Nhất Tín đã cung cấp payload tối thiểu CreateBill; `return_ward_code` vẫn cần trao đổi IT. | Validate sai ở luồng return address, có thể reject đơn hoặc ép người dùng nhập field không cần thiết. | Gọi UAT tạo đơn tối thiểu theo sample; thử `is_return_org=0` không truyền return fields và `is_return_org=1` thiếu từng return field để lấy lỗi thật. |
| GAP-05 | Master Data cho `service_id`, `payment_method_id`, `cargo_type_id`, `status_id` chưa có giá trị đầy đủ | High | Nhất Tín xác nhận nhóm bảng mã cần có: dịch vụ, hình thức thanh toán, loại hàng hóa, trạng thái đơn; chưa cung cấp đầy đủ giá trị trong file hiện tại. | Chưa seed được danh mục production-grade, validate request và map trạng thái đầy đủ. | Thu thập bảng mã chính thức; tạm dùng các code đã xuất hiện trong payload/rule để dựng design có điều kiện. |
| GAP-06 | Mã trạng thái bill chưa đủ catalog | High | Docs chỉ thấy ví dụ/tình huống: tạo xong `status_id=2`, cancel cho trạng thái `1`/`2`, revert cho `3`/`15`/`7`, tracking ví dụ `bill_status_id=4`, webhook `status_id=3`. | State machine Sandbox thiếu trạng thái, webhook mô phỏng sai, không biết trạng thái terminal/thất bại/hoàn. | Xin bảng `status_id/status_name` từ Master Data; crosscheck với tracking nhiều đơn ở các trạng thái khác nhau. |
| GAP-07 | Mã lỗi và response lỗi nghiệp vụ chưa đầy đủ | High | Auth docs chỉ liệt kê lỗi 401/400 chung; updatebill có sample `success:false`, `message`, `data:{}`; các API khác chủ yếu có response thành công. | Không xây được `ErrorCodes.cs`, test BIZ/AUTH thiếu case, client không phân loại lỗi được. | Gọi UAT với payload thiếu/sai field, sai token, sai location, sai bill_code; lưu response code/body/message để lập catalog lỗi. |
| GAP-08 | Webhook không có signature/header ký | Đã xác nhận | Nhất Tín phản hồi webhook không có signature. | Receiver không xác thực được bằng chữ ký; cần bù bằng raw audit, dedupe, HTTPS và network control nếu IT xác nhận được. | Khi test UAT, lưu header/body webhook thật để chứng minh không có signature và cập nhật vận hành bảo mật. |
| GAP-09 | Webhook retry/timeout/idempotency chưa có | High | Nhất Tín phản hồi POS tự xử lý trùng lặp; ACK/retry/timeout vẫn cần hỏi IT. | Có thể xử lý trùng, mất sự kiện, không biết trả HTTP code/body nào để Nhất Tín xem là thành công. | Xin policy retry/timeout, HTTP response expected, có request id hay không; test endpoint trả 500/timeout trên UAT nếu được. |
| GAP-10 | Webhook method chưa chốt | Đã xác nhận | Nhất Tín phản hồi do bên POS chọn. | Rủi ro còn lại là cấu hình portal/UAT có thể giới hạn method cụ thể. | Thiết kế mặc định `POST`; khi cấu hình portal thì xác nhận method đã chọn được Nhất Tín gọi đúng. |
| GAP-11 | Môi trường sandbox/UAT/base URL chưa chuẩn hóa | Đã xác nhận | Nhất Tín cung cấp API host sandbox/production và portal sandbox/production. | Còn thiếu credential và có thể còn thiếu CDN/asset host riêng. | Cấu hình environment theo host đã xác nhận; khi có credential, gọi smoke test trên sandbox. |
| GAP-12 | Rate limit/quota chưa có | Medium | Không thấy tài liệu rate limit trong các file đọc. | Client có thể bị throttled khi đồng bộ location/tracking/webhook replay; test stress không có ngưỡng. | Hỏi Nhất Tín quota theo endpoint/token/IP; tự đo với UAT ở mức an toàn nếu được phép. |
| GAP-13 | Upload/asset/proof image chưa rõ quyền truy cập và vòng đời | Medium | `trackingbill.md` trả `bill_image_link[]`, `document_image_link[]`, `p_link_image`; không có upload endpoint trong phạm vi đọc. | Không biết có cần lưu proxy, tải ảnh, kiểm soát TTL/quyền truy cập, hoặc mô phỏng asset trong Sandbox. | Mở thử link UAT/prod được cấp, xác nhận cần auth hay public, TTL, kích thước, loại file, có API upload chứng từ/ảnh không. |
| GAP-14 | Print/Label response chưa rõ format | High | `printbill.md` có endpoint và bảng envelope nhưng không có sample response; sample URL ở host print riêng. | Không biết client nên nhận PDF, HTML, link, binary stream hay JSON; khó design label storage/preview. | Gọi print UAT với bill thật; ghi lại status code, content-type, body, redirect, auth requirement. |
| GAP-15 | Tên query print mâu thuẫn `do_code` và `bill_code` | Đã xác nhận | Nhất Tín phản hồi query đúng là `do_code`. | Rủi ro mapping giảm; vẫn chưa biết response format của print. | Request builder dùng `do_code`; khi có bill thật, gọi print để lưu status/content-type/body shape. |
| GAP-16 | Tracking request GET nhưng sample body JSON | Medium | `trackingbill.md` ghi `GET v3/bill/tracking`, bảng có `bill_code`, sample request là JSON object; Nhất Tín phản hồi chỉ hỗ trợ 1 đơn/lần. | Implement client sai truyền query/body; cache/proxy GET body không ổn định. | Test `GET /v3/bill/tracking?bill_code=...` và body JSON; chốt cách gọi chính thức. |
| GAP-17 | Kiểu dữ liệu timestamp/status/fee không đồng nhất | Medium | Nhất Tín phản hồi timestamp không thuộc format/timezone cụ thể; tracking, webhook, create, calcfee có nhiều kiểu timestamp khác nhau. | Parser dễ lỗi, DB schema chọn sai type, test snapshot không ổn định. | Adapter phải parse tolerant theo field và lưu raw payload; sau live test bổ sung mapping từng endpoint. |
| GAP-18 | Location đơn vị hành chính mới/cũ cần chốt chiến lược mapping | Medium | Nhất Tín phản hồi sau 01-07-2025 dùng địa danh mới; CreateBill dùng `s_province_code`, `s_ward_code`. Quan hệ với CalcFee `*_id` vẫn chưa kiểm chứng. | Tạo đơn và tính phí có thể lệch mã địa bàn nếu `*_code` và `*_id` không cùng hệ. | Đồng bộ provinces/districts/wards UAT; xác nhận `id`, `code`, `*_code`, `*_id` có cùng giá trị không; test một tuyến bằng CalcFee rồi CreateBill. |
| GAP-19 | Response envelope chưa đủ chuẩn cho mọi endpoint | Low | Hầu hết docs ghi `success/message/data`; cancel sample có typo `messsage`; print chưa có sample. | Code xử lý lỗi cần tolerate typo/khác biệt, docs generated có thể sai. | Gọi từng endpoint happy/fail và ghi envelope thật; quyết định parser strict hay tolerant. |
| GAP-20 | Quy định cập nhật/hủy/chuyển hoàn theo trạng thái cần kiểm chứng | Medium | Nhất Tín xác nhận hủy cho trạng thái `1`, `2`; hoàn trả cho `3`, `15`, `7`; update bill chưa mô tả trạng thái. | UI/API Sandbox có thể cho thao tác update sai trạng thái nếu mở quá rộng. | Encode rule cancel/revert đã xác nhận; giữ update bill ở chế độ guarded cho tới khi có ma trận allowed transitions. |

## Checklist còn chặn design đầy đủ

- Credential: có credential UAT/sandbox, `partner_id`, login và smoke test thật.
- Token lifecycle: đo TTL thật, xác nhận refresh token có rotation hay không.
- CreateBill: tạo được một vận đơn tối thiểu bằng mã tỉnh/phường mới.
- Master Data: có bảng mã service, payment method, cargo type, bill status.
- Webhook ACK/retry: biết status/body ACK thành công, retry/timeout.
- `High` Print: biết rõ print trả binary/link/HTML/JSON và content-type.
- `High` Location: xác nhận `*_code` và `*_id` có cùng hệ mã cho đơn vị hành chính mới không.
- `High` Error codes: có catalog lỗi từ docs hoặc từ test payload fail.

## Quyết định design có điều kiện

Được phép bắt đầu design các module trong [07-PHAM-VI-DESIGN-CO-DIEU-KIEN.md](07-PHAM-VI-DESIGN-CO-DIEU-KIEN.md). Khi design, mọi phần còn thiếu phải được đóng gói thành assumption/config/adapter có thể thay đổi sau khi người phụ trách cập nhật thêm phản hồi Nhất Tín.

## Khuyến nghị cho design Sandbox

1. Lưu raw request/response/webhook payload song song với field đã parse để chống lệch schema.
2. Thiết kế token manager có lock refresh theo partner account và retry một lần khi API trả 401 token hết hạn.
3. Tách danh mục Master Data thành seed có version/ngày cập nhật, không hardcode vài status ví dụ trong docs.
4. Webhook receiver nên có bảng `WebhookHistory` với hash payload hoặc key tổng hợp tạm (`bill_no`, `status_id`, `status_time`, `push_time`) cho đến khi Nhất Tín cung cấp idempotency key chính thức.
5. Location adapter nên hỗ trợ song song đơn vị cũ và mới, nhưng flow CreateBill ưu tiên mã hành chính mới theo docs 2025.
