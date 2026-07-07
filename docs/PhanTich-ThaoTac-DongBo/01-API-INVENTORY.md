# API Inventory - Nhất Tín Logistics

Nguồn chính: [NhatTinAPIDocumentation/vi/README.md](../NhatTinAPIDocumentation/vi/README.md) và toàn bộ markdown dưới [NhatTinAPIDocumentation/vi/](../NhatTinAPIDocumentation/vi/).
Nguồn bổ sung: phản hồi Nhất Tín trong [05-CAU-HOI-GUI-NHATTIN.md](05-CAU-HOI-GUI-NHATTIN.md).

Ghi chú đọc tài liệu:

- Host API: Sandbox `https://apisandbox.ntlogistics.vn`, Production `https://apiws.ntlogistics.vn`, theo [00-thong-tin-ket-noi.md](../NhatTinAPIDocumentation/vi/00-thong-tin-ket-noi.md).
- Host portal: Sandbox `https://bodev.ntlogistics.vn`, Production `https://khachhang.ntlogistics.vn`.
- Header chung cho API nghiệp vụ: `Authorization: Bearer <access_token>`.
- Response envelope phổ biến: `success`, `message`, `data`.
- Tài liệu có thông báo chuyển sang địa danh mới từ 2025; các API tạo/tính giá đang dùng mã tỉnh/phường mới.

## Tổng quan endpoint

| # | Nhóm chức năng | Method | Path | Nguồn file | Source URL | Mục đích | Payload/response chính nếu thấy | Trạng thái tích hợp đề xuất |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| 1 | Authentication | POST | `/v1/auth/sign-in` | [authentication.md](../NhatTinAPIDocumentation/vi/authentication.md) | `https://docs.ntlogistics.vn/docs/vi/authentication` | Đăng nhập lấy `jwt_token` và `refresh_token`. | Request JSON: `username`, `password`. Response: `success=true`, `data.jwt_token`, `data.token_type=Bearer`, `data.token_expires_in`, `data.refresh_token`, `data.refresh_expires_in`. | Bắt buộc cho Sandbox emulator và Client integration; cần mô phỏng token TTL ngắn như docs. |
| 2 | Authentication | POST | `/v1/auth/refresh-token` | [authentication.md](../NhatTinAPIDocumentation/vi/authentication.md) | `https://docs.ntlogistics.vn/docs/vi/authentication` | Làm mới access token khi hết hạn. | Request JSON: `refresh_token`. Response: `success=true`, token mới và refresh token mới. Lỗi thường gặp: thiếu refresh token `400`, token sai/hết hạn `401`. | Bắt buộc; cần test luồng 401 rồi refresh/retry. |
| 3 | Location | GET | `/v3/loc/provinces` | [location/provinces.md](../NhatTinAPIDocumentation/vi/location/provinces.md) | `https://docs.ntlogistics.vn/docs/vi/location/provinces` | Lấy danh sách Tỉnh/Thành phố. | Query: `is_new` Int, docs ghi bắt buộc, `1` đơn vị mới, `0` đơn vị cũ mặc định. Response `data[]`: `id`, `province_name`, `is_new`. | Bắt buộc cho đồng bộ địa danh và mapping đơn hàng; ưu tiên hỗ trợ `is_new=1` theo mốc 2025. |
| 4 | Location | GET | `/v3/loc/districts` | [location/districts.md](../NhatTinAPIDocumentation/vi/location/districts.md) | `https://docs.ntlogistics.vn/docs/vi/location/districts` | Lấy danh sách Quận/Huyện theo tỉnh. | Query: `province_id` Int bắt buộc. Response `data[]`: `id`, `district_name`, `is_new`. | Tùy chọn/legacy nếu hệ thống dùng địa danh mới sau 2025; vẫn nên mô phỏng vì docs có endpoint. |
| 5 | Location | GET | `/v3/loc/wards` | [location/wards.md](../NhatTinAPIDocumentation/vi/location/wards.md) | `https://docs.ntlogistics.vn/docs/vi/location/wards` | Lấy danh sách Phường/Xã. | Query docs ghi bắt buộc: `district_id` cho mã cũ, hoặc `is_new=1&province_id={code}` cho mã mới. Response `data[]`: `id`, `ward_name`, `is_new`. | Bắt buộc cho địa danh mới; cần chấp nhận cả kiểu query cũ và mới để sandbox sát docs. |
| 6 | Bill | POST | `/v3/bill/create` | [bill/createbill.md](../NhatTinAPIDocumentation/vi/bill/createbill.md) | `https://docs.ntlogistics.vn/docs/vi/bill/createBill` | Tạo vận đơn Nhất Tín từ đơn hàng TruePos/POS. | Request chính: `weight`, `service_id`, `payment_method_id`, `cargo_type_id`, thông tin người gửi `s_*`, người nhận `r_*`, mã địa danh mới `s_province_code`, `s_ward_code`, `r_province_code`, `r_ward_code`; tùy chọn `ref_code`, kích thước, COD, ghi chú, hoàn chứng từ, bill trade-in. Response `data`: `bill_id`, `bill_code`, `ref_code`, `status_id`, phí, `expected_at`, thông tin người nhận, kiện hàng. | Bắt buộc; là endpoint lõi create order/bill. Cần validate tối thiểu các field True trong docs và trạng thái ban đầu `2`/draft. |
| 7 | Bill | POST | `/v3/bill/update-shipping` | [bill/updatebill.md](../NhatTinAPIDocumentation/vi/bill/updatebill.md) | `https://docs.ntlogistics.vn/docs/vi/bill/updateBill` | Cập nhật thông tin vận đơn. | Request bắt buộc: `partner_id`, `bill_code`; tùy chọn COD, giá trị hàng, cân nặng/kích thước, nội dung hàng, thông tin người nhận, `package_no`, `is_return_doc`, `note`, `is_installation`. Response thành công trả lại thông tin bill và phí; thất bại `success=false`, `message`, `data={}`. | Bắt buộc nếu TruePos cho phép sửa đơn trước khi lấy hàng; cần ràng buộc trạng thái được sửa, docs chưa nêu chi tiết. |
| 8 | Bill | POST | `/v3/bill/destroy` | [bill/cancelbill.md](../NhatTinAPIDocumentation/vi/bill/cancelbill.md) | `https://docs.ntlogistics.vn/docs/vi/bill/cancelBill` | Hủy vận đơn. | Request: `bill_code` String Array bắt buộc. Response `data[]`: `doCode`, `message`; docs mô tả chỉ hủy đơn trạng thái `1` chưa thành công và `2` chờ lấy hàng. | Bắt buộc cho luồng cancel; cần mô phỏng hủy nhiều bill và lỗi theo từng bill. |
| 9 | Bill | POST | `/v3/bill/revert-bill` | [bill/revertbill.md](../NhatTinAPIDocumentation/vi/bill/revertbill.md) | `https://docs.ntlogistics.vn/docs/vi/bill/revertBill` | Yêu cầu chuyển hoàn vận đơn. | Request: `bill_code` String Array bắt buộc. Response `data.success[]`, `data.failed[]`; docs mô tả áp dụng cho trạng thái đã lấy hàng `3`, đang vận chuyển `15`, không phát được `7`. | Tùy chọn nhưng nên có trong Sandbox vì là luồng nghiệp vụ sau giao nhận. |
| 10 | Bill | POST | `/v3/bill/calc-fee` | [bill/calcfee.md](../NhatTinAPIDocumentation/vi/bill/calcfee.md) | `https://docs.ntlogistics.vn/docs/vi/bill/calcFee` | Tính giá vận chuyển trước khi tạo vận đơn. | Request chính: `partner_id`, `weight`, kích thước, `service_id`, `payment_method_id`, COD, giá trị hàng, địa danh cũ `s_province/s_district/r_province/r_district` hoặc địa danh mới `s_province_id`, `s_ward_id`, `r_province_id`, `r_ward_id`. Response `data[]`: `weight`, `total_fee`, `main_fee`, `insur_fee`, `remote_fee`, `cod_fee`, `service_id`, `service_name`, `lead_time`. | Bắt buộc cho UI báo giá và kiểm tra phí khi create bill; cần hỗ trợ địa danh mới sau 01/07/2025. |
| 11 | Bill | GET | `/v3/bill/tracking` | [bill/trackingbill.md](../NhatTinAPIDocumentation/vi/bill/trackingbill.md) | `https://docs.ntlogistics.vn/docs/vi/bill/trackingBill` | Tra cứu trạng thái và lịch sử vận đơn. | Tham số: `bill_code` String bắt buộc. Nhất Tín phản hồi chỉ hỗ trợ 1 đơn/lần. Docs ghi GET nhưng ví dụ request là JSON body; cần xác nhận query/body khi gọi thật. Response `data[]`: thông tin bill, phí, sender/receiver, `histories[]`, `p_link_image`, `bill_image_link[]`, `document_image_link[]`. | Bắt buộc cho polling/tracking; thiết kế không batch nhiều bill; request shape còn cần crosscheck. |
| 12 | Print | GET | `/v3/bill/print?do_code={bill_code}&partner_id={partner_id}` | [bill/printbill.md](../NhatTinAPIDocumentation/vi/bill/printbill.md) | `https://docs.ntlogistics.vn/docs/vi/bill/printBill` | In vận đơn/waybill. | Nhất Tín xác nhận query đúng là `do_code`, kèm `partner_id` bắt buộc. Response envelope `success`, `message`, `data` có trong docs nhưng chưa có sample content-type/body thật. | Bắt buộc nếu TruePos in nhãn từ hệ thống; request builder có thể dùng `do_code`, response format vẫn cần gọi thật. |
| 13 | Webhook | POS chọn, khuyến nghị POST | Callback cấu hình qua Web portal | [bill/webhook.md](../NhatTinAPIDocumentation/vi/bill/webhook.md) | `https://docs.ntlogistics.vn/docs/vi/bill/webhook` | Nhất Tín đẩy cập nhật trạng thái đơn hàng tới URL callback do đối tác cung cấp. | Payload: `bill_no`, `ref_code`, `status_id`, `status_name`, `status_time`, `push_time`, `shipping_fee`, `is_partial`, `reason`, `weight`, `dimension_weight`, `length`, `width`, `height`, `expected_at`. Nhất Tín phản hồi không có signature và POS tự xử lý trùng lặp. | Bắt buộc cho đồng bộ tự động; Sandbox cần receiver riêng, lưu raw payload/header, dedupe nội bộ; retry/timeout/ACK vẫn cần xác minh. |

## Dữ liệu tham chiếu từ tài liệu kết nối

Nguồn: [00-thong-tin-ket-noi.md](../NhatTinAPIDocumentation/vi/00-thong-tin-ket-noi.md).

| Loại dữ liệu | Giá trị chính | Ghi chú tích hợp |
| --- | --- | --- |
| `service_id` | `90` Giao hàng nhanh/CPN, `81` Hỏa tốc, `91` Tiết kiệm, `21` Hỗn hợp MES | Một số response mẫu cũ trả `service_id` khác như `10`, `20`, `11`; cần mapping mềm theo dữ liệu thật. |
| `payment_method_id` | `10` Người gửi thanh toán ngay, `11` Người gửi thanh toán sau, `20` Người nhận thanh toán ngay | Dùng trong create bill và calc fee. |
| `cargo_type_id` | `1` Chứng từ, `2` Hàng hóa, `3` Hàng lạnh, `4` Sinh phẩm, `5` Mẫu bệnh phẩm | Bắt buộc khi tạo vận đơn. |
| `status_id` | `1`, `2`, `3`, `4`, `6`, `7`, `9`, `10`, `11`, `12`, `13`, `15`, `16`, `17` | Cần dùng chung cho tracking, webhook, cancel/revert rule. |

## Payload CreateBill tối thiểu từ phản hồi Nhất Tín

```json
{
  "weight": 2,
  "width": 0,
  "length": 0,
  "height": 0,
  "service_id": 91,
  "payment_method_id": 10,
  "cod_amount": 0,
  "cargo_value": 0,
  "cargo_type_id": 2,
  "s_name": "TEST",
  "s_phone": "0333333333",
  "s_address": "số 10",
  "r_name": "TEST",
  "r_phone": "0333333333",
  "r_address": "123",
  "s_ward_code": "00004",
  "s_province_code": "01",
  "r_ward_code": "25750",
  "r_province_code": "79"
}
```

Lưu ý: Nhất Tín cũng phản hồi `partner_id` là trường bắt buộc và được tạo trên Web portal, nhưng payload tối thiểu được cung cấp không có `partner_id`. Khi có credential/live sandbox cần crosscheck CreateBill có lấy `partner_id` từ token/account hay cần truyền trong body/query.

## Khoảng trống cần xác minh trước khi code API thật

| Chủ đề | Tình trạng trong docs scrape | Ảnh hưởng |
| --- | --- | --- |
| Webhook callback path | Cấu hình qua Web portal | Sandbox tự định nghĩa URL receiver nội bộ; Client integration cần cấu hình URL với Nhất Tín trên portal. |
| Webhook auth/signature | Nhất Tín xác nhận không có signature | Receiver không verify chữ ký, nhưng phải lưu raw header/body và có dedupe/audit. |
| Webhook retry/timeout/ack | Chưa có phản hồi cuối | Khó mô phỏng retry chính xác; nên log idempotency theo `bill_no/status_id/status_time/push_time` hoặc hash payload. |
| Tracking GET tham số | Docs ghi GET `/v3/bill/tracking` nhưng ví dụ là JSON body; chỉ hỗ trợ 1 đơn/lần | Cần crosscheck API thật: query `?bill_code=` hay GET body. |
| Print response format | Query `do_code` đã xác nhận, response format chưa rõ | Cần gọi thật để biết PDF/binary, HTML, redirect, link hay JSON envelope. |
| Location bắt buộc | Nhất Tín xác nhận dùng địa danh mới sau `01/07/2025`; quan hệ `*_code` và `*_id` chưa kiểm chứng | Cần triển khai linh hoạt theo nhánh địa danh cũ/mới và crosscheck CalcFee/CreateBill. |
