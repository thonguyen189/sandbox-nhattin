# Schema Mapping - Tích hợp Nhất Tín Logistics

## Phạm vi và nguyên tắc

Tài liệu này map sơ bộ schema Nhất Tín Logistics sang khái niệm TruePos/Sandbox để chuẩn bị design/code. Chỉ dùng tài liệu scrape trong `NhatTinAPIDocumentation/vi/**/*.md` và checklist khởi tạo đối tác mới.

Nguồn chính:

- `NhatTinAPIDocumentation/vi/authentication.md`
- `NhatTinAPIDocumentation/vi/location/provinces.md`
- `NhatTinAPIDocumentation/vi/location/districts.md`
- `NhatTinAPIDocumentation/vi/location/wards.md`
- `NhatTinAPIDocumentation/vi/bill/createbill.md`
- `NhatTinAPIDocumentation/vi/bill/calcfee.md`
- `NhatTinAPIDocumentation/vi/bill/trackingbill.md`
- `NhatTinAPIDocumentation/vi/bill/webhook.md`
- `NhatTinAPIDocumentation/vi/bill/printbill.md`
- `NhatTinAPIDocumentation/vi/bill/updatebill.md`
- `NhatTinAPIDocumentation/vi/bill/cancelbill.md`
- `NhatTinAPIDocumentation/vi/bill/revertbill.md`
- `Handoff-TichHopDoiTac/04-CHECKLIST-DOI-TAC-MOI.md`

Quy ước:

- `TruePos/Sandbox` là khái niệm nội bộ cần thiết kế sau, chưa giả định tên class/table cuối cùng.
- Field không thấy rõ trong docs hoặc cần xác nhận với Nhất Tín được ghi `Cần xác minh`.
- Response envelope phổ biến trong docs: `success`, `message`, `data`.

## Cập nhật theo phản hồi Nhất Tín

Nguồn bổ sung: cột `Phản hồi` trong [05-CAU-HOI-GUI-NHATTIN.md](05-CAU-HOI-GUI-NHATTIN.md).

| Nhóm | Điểm đã xác nhận | Ảnh hưởng mapping |
| --- | --- | --- |
| Environment | API host sandbox `https://apisandbox.ntlogistics.vn`, production `https://apiws.ntlogistics.vn`; portal sandbox `https://bodev.ntlogistics.vn`, production `https://khachhang.ntlogistics.vn`. | Tách API host và portal host trong cấu hình environment. |
| Auth | Gửi header `Authorization` với access token ở mọi request; lỗi token có thể trả `401`. | Auth client dùng Bearer token, refresh rồi retry một lần khi gặp `401`. |
| Location | Sau `01/07/2025` dùng địa danh mới. | CreateBill ưu tiên các field `*_province_code`, `*_ward_code`; CalcFee ưu tiên `*_province_id`, `*_ward_id`. |
| Bill | Có payload tối thiểu tạo vận đơn; `partner_id` là bắt buộc và được tạo trên Web portal. | DTO/validation có thể dựng trước theo payload tối thiểu và bắt buộc partner context. |
| Operation | Hủy đơn cho trạng thái `1`, `2`; hoàn trả cho trạng thái `3`, `15`, `7`. | State machine sandbox có thể encode tạm các rule này, còn update bill guarded. |
| Tracking | Chỉ hỗ trợ tra cứu 1 đơn/lần; timestamp không có format/timezone cố định. | Polling thiết kế theo từng `bill_code`, parser timestamp tolerant và lưu raw payload. |
| Webhook | Callback cấu hình qua portal; POS chọn method; không có signature; POS tự xử lý trùng lặp. | Receiver mặc định `POST`, không verify signature, lưu raw payload/header và dedupe nội bộ. |
| Print | Endpoint dùng `GET /v3/bill/print?do_code={bill_code}&partner_id={partner_id}`; query đúng là `do_code`. | Request builder dùng `do_code`; response type/content-type còn chờ xác minh. |

## Auth token

Nguồn: `NhatTinAPIDocumentation/vi/authentication.md`

| Nhất Tín | TruePos/Sandbox | Ghi chú |
| --- | --- | --- |
| `POST /v1/auth/sign-in` | Partner credential login flow | Body gồm `username`, `password`. Tài khoản theo từng môi trường. |
| `username` | Partner credential/account username | Lưu bảo mật trong cấu hình/secret store. |
| `password` | Partner credential/account password | Lưu bảo mật trong cấu hình/secret store. |
| `jwt_token` | Access token dùng gọi API Nhất Tín | Gửi header `Authorization: Bearer <access_token>`. |
| `token_type` | Auth scheme | Docs trả `Bearer`. |
| `token_expires_in` | Access token TTL | Ví dụ docs là `1m`; TTL thật cần đo/xác nhận. |
| `refresh_token` | Refresh token | Cần lưu kín; dùng cho refresh. |
| `refresh_expires_in` | Refresh token TTL | Ví dụ docs là `2m`; TTL thật cần đo/xác nhận. |
| `POST /v1/auth/refresh-token` | Token refresh lifecycle | Body gồm `refresh_token`; response trả token mới và refresh token mới. |
| Lỗi auth `401` | Auth failure handling | Thiếu/sai header, token hết hạn, token không hợp lệ. |
| Lỗi refresh `400` | Refresh validation error | Thiếu `refresh_token`. |
| Cơ chế ký request | Đã xác nhận theo phản hồi hiện tại | Nhất Tín yêu cầu gửi `Authorization` với access token ở mọi request; chưa thấy yêu cầu HMAC/timestamp cho API client. Cần live test khi có credential. |

## Location: province/district/ward

Nguồn: `NhatTinAPIDocumentation/vi/location/provinces.md`, `districts.md`, `wards.md`

### Province

| Nhất Tín | TruePos/Sandbox | Ghi chú |
| --- | --- | --- |
| `GET v3/loc/provinces` | Đồng bộ danh mục tỉnh/thành | Query `is_new` là bắt buộc theo bảng docs. |
| `is_new` request | Chọn đơn vị hành chính mới/cũ | `1`: đơn vị mới, `0`: đơn vị cũ, mặc định docs ghi `0`. |
| `data[].id` | Province code/id nội bộ mapping | Ví dụ `"11"`, kiểu trong response là string dù bảng request ghi Int. |
| `data[].province_name` | Tên tỉnh/thành | Dùng hiển thị và mapping địa chỉ. |
| `data[].is_new` | Cờ đơn vị hành chính mới/cũ | Response ví dụ `"N"`; ý nghĩa đầy đủ `N`/giá trị khác Cần xác minh. |

### District

| Nhất Tín | TruePos/Sandbox | Ghi chú |
| --- | --- | --- |
| `GET v3/loc/districts` | Đồng bộ danh mục quận/huyện | Query `province_id` bắt buộc. |
| `province_id` request | Khóa cha tỉnh/thành | Int theo docs, ví dụ query `province_id=99`. |
| `data[].id` | District code/id nội bộ mapping | Ví dụ `"1101"`. |
| `data[].district_name` | Tên quận/huyện | Dùng cho địa chỉ cũ và hiển thị. |
| `data[].is_new` | Cờ đơn vị hành chính mới/cũ | Response ví dụ `"N"`; Cần xác minh ý nghĩa đầy đủ. |

### Ward

| Nhất Tín | TruePos/Sandbox | Ghi chú |
| --- | --- | --- |
| `GET v3/loc/wards` | Đồng bộ danh mục phường/xã | Hỗ trợ query theo đơn vị cũ hoặc mới. |
| `district_id` request | Khóa cha quận/huyện cũ | Docs ghi bắt buộc, ví dụ `district_id=9904`; dùng cho đơn vị cũ. |
| `is_new` request | Chọn đơn vị mới/cũ | Docs ghi bắt buộc; ví dụ `is_new=1`. |
| `province_id` request | Khóa cha tỉnh mới | Docs ghi bắt buộc, dùng với `is_new=1`; ví dụ `province_id=01`. |
| `data[].id` | Ward code/id nội bộ mapping | Ví dụ `"01106"`. |
| `data[].ward_name` | Tên phường/xã | Dùng hiển thị và mapping địa chỉ. |
| `data[].is_new` | Cờ đơn vị hành chính mới/cũ | Response ví dụ `"N"`; Cần xác minh ý nghĩa đầy đủ. |

## Bill/CreateBill

Nguồn: `NhatTinAPIDocumentation/vi/bill/createbill.md`

Endpoint: `POST /v3/bill/create`

Payload tối thiểu đã được Nhất Tín phản hồi để dùng làm baseline thiết kế/test happy path:

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

### Request mapping

| Nhất Tín | Bắt buộc | TruePos/Sandbox | Ghi chú |
| --- | --- | --- | --- |
| `ref_code` | Không | External order/invoice reference | Số đơn hàng/hóa đơn của khách hàng. |
| `package_no` | Không | Parcel/package count | Mặc định 1 nếu không rõ số kiện. |
| `weight` | Có | Shipment weight | Trọng lượng ước lượng/đo đạc. |
| `width`, `length`, `height` | Không | Package dimensions | Cm; có thể mặc định 0. |
| `cargo_content` | Không | Goods description | Mô tả hàng hóa. |
| `service_id` | Có | Shipping service code | Tham chiếu Master Data; danh mục chưa nằm trong phạm vi docs đọc. |
| `payment_method_id` | Có | Shipping fee payment method | Tham chiếu Master Data. |
| `is_return_doc` | Không | Return document flag | `1`: có, `0`: không, mặc định 0. |
| `cod_amount` | Không | COD amount | Số tiền thu hộ. |
| `note` | Không | Shipping note | Lưu ý giao/nhận. |
| `cargo_value` | Không | Goods declared value | Giá trị hàng hóa. |
| `cargo_type_id` | Có | Goods type code | Tham chiếu Master Data. |
| `s_name`, `s_phone`, `s_address` | Có | Sender contact/address line | Người gửi. |
| `s_province_code`, `s_ward_code` | Có | Sender province/ward code | Code đơn vị hành chính mới. |
| `is_return_org` | Không | Return address mode | Nếu `1` thì docs nói phải truyền thêm thông tin hoàn; điều kiện bắt buộc chi tiết Cần xác minh. |
| `return_name`, `return_phone`, `return_address` | Không | Return contact/address line | Địa chỉ hoàn hàng. |
| `return_province_code` | Không | Return province code | Docs ghi mô tả là tên/code tỉnh hoàn; cần chuẩn hóa tên field. |
| `return_ward_code` | Có | Return ward code | Bảng docs đánh dấu True dù `is_return_org` có điều kiện; Cần xác minh. |
| `r_name`, `r_phone`, `r_address` | Có | Receiver contact/address line | Người nhận. |
| `r_province_code`, `r_ward_code` | Có | Receiver province/ward code | Code đơn vị hành chính mới. |
| `is_draft` | Không | Draft shipment flag | `1`: đơn nháp chờ đóng gói; `0`: sẵn sàng lấy hàng. |
| `other_fee` | Không | Other negotiated fee | Phí khác theo thỏa thuận. |
| `is_installation` | Không | Installation support flag | `1`: có, `0`: không, mặc định 0. |
| `bill_type` | Không | Shipment type | `1`: bill giao; `2`: bill thu hồi; `3`: bill trade-in giao; `4`: bill thu hồi trade-in. |
| `bill_return` | Không | Related return bill code | Mã bill thu hồi liên kết. |

### Response mapping

| Nhất Tín | TruePos/Sandbox | Ghi chú |
| --- | --- | --- |
| `success` | API success flag | Envelope chung. |
| `message` | API message | Ví dụ `Create bill successfully`. |
| `data.bill_id` | Partner bill internal id | Ví dụ có thể là `0`; Cần xác minh tính ổn định. |
| `data.bill_code` | Carrier tracking/shipment code | Mã vận đơn Nhất Tín, ví dụ `CP17364792`. |
| `data.ref_code` | External reference echo | Có thể rỗng. |
| `data.status_id` | Bill status id | Ví dụ tạo xong trả `2` tương ứng Chờ lấy hàng theo mô tả `is_draft`. |
| `data.cod_amount` | COD amount | Echo/tính toán từ request. |
| `data.service_id` | Applied service code | Có thể khác request trong sample (`91` request, `80` response); Cần xác minh. |
| `data.payment_method` | Applied payment method | Response dùng `payment_method`, request dùng `payment_method_id`. |
| `data.created_at`, `data.expected_at` | Created/expected timestamps | Format ví dụ `YYYY-MM-DD HH:mm:ss`. |
| Fee fields: `main_fee`, `cod_fee`, `insurr_fee`, `lifting_fee`, `remote_fee`, `counting_fee`, `packing_fee`, `total_fee` | Fee breakdown | Lưu vào bảng phí/response snapshot. |
| `receiver_name`, `receiver_phone`, `receiver_address` | Receiver snapshot | Phone có thể bị mask. |
| `package_no`, `weight`, `cargo_content`, `cargo_value`, `note` | Shipment snapshot | Echo/chuẩn hóa từ Nhất Tín. |

## CalcFee

Nguồn: `NhatTinAPIDocumentation/vi/bill/calcfee.md`

Endpoint: `POST /v3/bill/calc-fee`

| Nhất Tín | Bắt buộc | TruePos/Sandbox | Ghi chú |
| --- | --- | --- | --- |
| `partner_id` | Có theo phản hồi Nhất Tín | Partner/customer id at Nhất Tín | Trường bắt buộc, được tạo trên Web portal. |
| `weight` | Có | Shipment weight | Trọng lượng ước lượng/đo đạc. |
| `width`, `length`, `height` | Không | Package dimensions | Cm; có thể mặc định 0. |
| `service_id` | Không | Requested service code | Nếu không truyền, response có thể trả nhiều dịch vụ. |
| `payment_method_id` | Có | Fee payment method | Tham chiếu Master Data. |
| `cod_amount` | Không | COD amount | Dùng tính COD fee nếu có. |
| `cargo_value` | Không | Goods declared value | Dùng tính phí bảo hiểm nếu có. |
| `s_province`, `s_district` | Không | Sender old administrative address | Dùng cho đơn vị hành chính cũ. |
| `r_province`, `r_district` | Không | Receiver old administrative address | Dùng cho đơn vị hành chính cũ. |
| `s_province_id`, `s_ward_id` | Có | Sender new province/ward id | Đơn vị hành chính mới sau 01-07-2025. |
| `r_province_id`, `r_ward_id` | Có | Receiver new province/ward id | Đơn vị hành chính mới sau 01-07-2025. |
| `data[].service_id`, `service_name` | Shipping service option | Một request có thể trả nhiều option. |
| `data[].total_fee`, `main_fee`, `insur_fee`, `remote_fee`, `cod_fee` | Fee quote breakdown | Lưu quote snapshot. |
| `data[].lead_time` | Estimated lead time | Format ví dụ `dd/MM/yyyy HH:mm`; cần parse theo locale. |

Lưu ý mapping: CreateBill dùng hậu tố `*_province_code`/`*_ward_code`, còn CalcFee dùng `*_province_id`/`*_ward_id`. Cần xác minh đây là cùng mã hay hai loại định danh khác nhau.

## Tracking

Nguồn: `NhatTinAPIDocumentation/vi/bill/trackingbill.md`

Endpoint: `GET v3/bill/tracking`

| Nhất Tín | TruePos/Sandbox | Ghi chú |
| --- | --- | --- |
| `bill_code` request | Carrier tracking/shipment code | Nhất Tín phản hồi tài liệu chỉ hỗ trợ 1 đơn/lần. Bảng docs ghi GET nhưng sample request là JSON; cách truyền query/body vẫn Cần xác minh. |
| `data[].bill_code` | Carrier tracking/shipment code | Mã vận đơn. |
| `data[].ref_code` | External order reference | Mã tham chiếu khách hàng. |
| `data[].weight`, `dimension_weight`, dimensions | Shipment measured info | Nhiều field dạng string trong sample. |
| `data[].payment_status`, `payment_at` | Payment status/time | Có thể rỗng. |
| `data[].bill_status_id`, `bill_status_desc` | Current shipment status | Ví dụ `4` là `Đã giao hàng`; full status catalog Cần xác minh. |
| `date_pickup`, `date_expected`, `date_delivery` | Shipment timeline timestamps | Nhất Tín phản hồi timestamp không thuộc một format/timezone cố định; adapter cần parse tolerant theo từng field và lưu raw value. |
| `pay_method`, `service` | Payment/service display | Dạng text/code hiển thị. |
| Fee fields | Fee snapshot | `cod_amt`, `cod_fee`, `insurance_fee`, `counting_fee`, `lifting_fee`, `packing_fee`, `delivery_fee`, `other_fee`, `remote_fee`, `main_fee`, `total_fee`. |
| Sender/receiver fields | Address/contact snapshot | Có sender/receiver name, phone, address, ward, district, province. |
| `histories[]` | Tracking events | Nên map thành shipment event log. |
| `histories[].sequence` | Event order | Thứ tự sự kiện. |
| `histories[].log_status` | Event status code | Ví dụ `10`, `20`, `30`; full meaning Cần xác minh. |
| `histories[].operationID`, `operationType` | Partner event identifiers | Có thể thiếu ở một số event. |
| `histories[].operation`, `operation_en` | Event description | Tiếng Việt/Anh. |
| `histories[].loc_time` | Event timestamp | Format ví dụ `dd/MM/yyyy HH:mm`. |
| `p_link_image`, `bill_image_link[]`, `document_image_link[]` | Proof/asset URLs | Có link ảnh lấy/giao/chứng từ; quyền truy cập và TTL Cần xác minh. |

## Webhook

Nguồn: `NhatTinAPIDocumentation/vi/bill/webhook.md`, `Handoff-TichHopDoiTac/04-CHECKLIST-DOI-TAC-MOI.md`

Docs mô tả Nhất Tín sẽ gửi cập nhật đơn hàng đến URL callback do đối tác cung cấp. HTTP method ghi `GET/POST/PUT`.

| Nhất Tín | Bắt buộc | TruePos/Sandbox | Ghi chú |
| --- | --- | --- | --- |
| Callback URL | Có | Webhook receiver endpoint | Cấu hình qua Web portal. |
| HTTP method `GET/POST/PUT` | Do POS chọn | Accepted webhook method(s) | Khuyến nghị thiết kế receiver mặc định `POST`, chỉ mở method khác nếu cấu hình portal cần. |
| `bill_no` | Có | Carrier tracking/shipment code | Mã vận đơn Nhất Tín. |
| `ref_code` | Có | External order reference | Số hóa đơn/đơn đặt hàng khách hàng. |
| `status_id` | Có | Shipment status id | Tham khảo Master Data; catalog chưa có trong phạm vi đọc. |
| `status_name` | Có | Shipment status name | Tham khảo Master Data. |
| `status_time` | Có | Status changed timestamp | Int epoch trong sample; timezone Cần xác minh. |
| `push_time` | Có | Webhook pushed timestamp | Int epoch trong sample; dùng audit/replay/idempotency nếu có. |
| `shipping_fee` | Có | Shipping fee at update time | Double. |
| `is_partial` | Có | Partial delivery return flag | `1`: đúng, `0`: không. |
| `reason` | Không | Status reason/failure reason | Lý do nếu có. |
| `weight`, `dimension_weight` | Có | Shipment weight metrics | Double. |
| `length`, `width`, `height` | Có | Package dimensions | Cm. |
| `expected_at` | Có | Expected delivery timestamp | Format docs: `YYYY-MM-DD HH:mm:ss`. |
| Webhook signature/header | Không có | Webhook auth verification | Nhất Tín phản hồi webhook không có signature. Nếu cần tăng an toàn, dùng network allowlist/domain control sau khi IT xác nhận. |
| Retry policy/idempotency key | POS tự xử lý trùng lặp | WebhookHistory/retry/replay design | Chưa có request id/idempotency key từ Nhất Tín; dedupe tạm theo `bill_no`, `status_id`, `status_time`, `push_time` hoặc hash payload. ACK/retry/timeout vẫn cần xác minh. |

## Print/Label

Nguồn: `NhatTinAPIDocumentation/vi/bill/printbill.md`

Endpoint docs ghi `GET v3/bill/print?do_code={bill_code}&partner_id={partner_id}`, sample dùng host `https://printdev.ntlogistics.vn/v1/bill/print` hoặc `https://printdigi.ntlogistics.vn/v1/bill/print`.

| Nhất Tín | Bắt buộc | TruePos/Sandbox | Ghi chú |
| --- | --- | --- | --- |
| `do_code` query | Có | Carrier tracking/shipment code | Nhất Tín xác nhận query đúng là `do_code`. |
| `partner_id` query | Có | Partner/customer id at Nhất Tín | Bắt buộc, được tạo trên Web portal. |
| `success`, `message`, `data` | Cần xác minh | Print response envelope | Docs có bảng response nhưng không có sample response. |
| Label/PDF/HTML content | Cần xác minh | Printable label artifact | Chưa rõ API trả link, binary, HTML hay JSON. |
| Print host/environment | Xác nhận một phần | Environment-specific print base URL | Nhất Tín xác nhận path/query `GET /v3/bill/print?do_code=...&partner_id=...`; content-type/response format vẫn cần xác minh. |

## Bill operations phụ trợ

Nguồn: `NhatTinAPIDocumentation/vi/bill/updatebill.md`, `cancelbill.md`, `revertbill.md`

| Nhất Tín | TruePos/Sandbox | Ghi chú |
| --- | --- | --- |
| `POST /v3/bill/update-shipping` | Update shipment command | Bắt buộc `partner_id`, `bill_code`; các field còn lại chủ yếu là COD, hàng hóa, người nhận, note. |
| `POST v3/bill/destroy` | Cancel shipment command | Body `bill_code` là string array. Chỉ hủy đơn trạng thái `1` Chưa thành công và `2` Chờ lấy hàng theo phản hồi Nhất Tín. |
| `POST v3/bill/revert-bill` | Return shipment command | Body `bill_code` là string array. Áp dụng khi đã lấy hàng `3`, đang vận chuyển `15`, không phát được `7` theo phản hồi Nhất Tín. |
| Response success/failed của revert | Batch operation result | `data.success[]`, `data.failed[]`. |
