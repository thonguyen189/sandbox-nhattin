---
title: "Tạo vận đơn"
source: "https://docs.ntlogistics.vn/docs/vi/bill/createBill"
---
Vận đơn

# Tạo vận đơn

Tự động truyền thông tin đơn hàng sang Nhất Tín các thông tin như kích thước , cân nặng , số điện thoại và nhiều thông tin khác. Từ đó sẽ tạo đơn giao nhận.

## Cấu hình API

**HTTP Request**

POST: `/v3/bill/create`

| No. | Tham số | Loại | Bắt buộc | Mô tả |
| --- | --- | --- | --- | --- |
| 1 | ref_code | String |  | Số Đơn Hàng tham chiếu của khách hàng, ví dụ số Hóa Đơn, số đơn bán hàng... |
| 2 | package_no | Number |  | Số lượng kiện (để mặc định 1 nếu khách hàng rõ là bao nhiêu kiện hàng) |
| 3 | weight | Double | True | Trọng lượng đơn hàng do khách hàng ước lượng / đo dạc |
| 4 | width | Double |  | Chiều rộng kiện hàng, tính theo cm, có thể để mặc định là 0 nếu Khách hàng ko có thông số này |
| 5 | length | Double |  | Chiều dài kiện hàng, tính theo cm, có thể để mặc định là 0 nếu Khách hàng ko có thông số này |
| 6 | height | Double |  | Chiều cao kiện hàng, tính theo cm, có thể để mặc định là 0 nếu Khách hàng ko có thông số này |
| 7 | cargo_content | String |  | Mô tả sơ về hàng hóa của đơn vận chuyển |
| 8 | service_id | Number | True | Mã dịch vụ vận chuyển, tham chiếu vào trang Master Data để lấy mã |
| 9 | payment_method_id | Number | True | Mã hình thức thanh toán cước phí vận chuyển, tham chiếu vào trang Master Data để lấy mã |
| 10 | is_return_doc | int |  | Có chuyển hoàn chứng từ không (1: Có; 0: Không; Mặc định để 0) |
| 11 | cod_amount | Number |  | Số Tiền Thu Hộ nhà vận chuyển cần thu từ người nhận khi giao hàng |
| 12 | note | String |  | Ghi chú đơn hàng, lưu ý khi vận chuyển hàng hóa/ giao hàng |
| 13 | cargo_value | Double |  | Giá trị hàng hóa |
| 14 | cargo_type_id | Number | True | Mã Loại hàng hóa (chứng từ hay hàng hóa thông thường), tham chiếu trang Master Data để lấy ID |
| 15 | s_name | String | True | Tên người gửi |
| 16 | s_phone | String | True | Số điện thoại người gửi |
| 17 | s_address | String | True | Địa chỉ người gửi |
| 18 | s_province_code | String | True | Tỉnh/Thành phố người gửi (Code theo đơn vị hành chính quốc gia mới) |
| 19 | s_ward_code | String | True | Phường/Xã người gửi (Code theo đơn vị hành chính quốc gia mới) |
| 20 | is_return_org | Number |  | Đặc tả địa chỉ hoàn. (mặc định là 0) - Nếu thiết lập = 1 thì bắt buộc truyền thêm các hạng mục 23 đến 28 - Nếu mặc định = 0 thì lấy theo địa chỉ gốc (thông tin người gửi) |
| 21 | return_name | String |  | Tên người nhận thông tin hoàn hàng |
| 22 | return_phone | String |  | Số điện thoại nơi hoàn hàng |
| 23 | return_address | String |  | Địa chỉ hoàn hàng |
| 24 | return_province_code | String |  | Tên Tỉnh / Thành phổ hoàn hàng (Code theo đơn vị hành chính quốc gia mới) |
| 25 | return_ward_code | String | True | Tên Phường / Xã hoàn hàng (Code theo đơn vị hành chính quốc gia mới) |
| 26 | r_name | String | True | Tên người nhận |
| 27 | r_phone | String | True | Số điện thoại người nhận |
| 28 | r_address | String | True | Địa chỉ người nhận |
| 29 | r_province_code | String | True | Tỉnh/Thành phố người nhận (Code theo đơn vị hành chính quốc gia mới) |
| 30 | r_ward_code | String | True | Phường/Xã người nhận (Code theo đơn vị hành chính quốc gia mới) |
| 31 | is_draft | Number |  | Đặc tả tạo đơn nháp (mặc định là 0) - Nếu thiết lập = 1, đơn được hiểu là đơn nháp - chờ đóng gói từ Khách hàng. Nhất Tín chưa sẳn sàng lấy hàng. Khi đơn đã được đóng gói thì KH chuyển sang trạng thái chờ lấy hàng (status_id=2) - Nếu mặc định = 0 thì được hiểu đơn đã sẳn sàng lấy hàng (đơn ở trạng thái Chờ lấy hàng) |
| 32 | other_fee | Number |  | Phí khác theo thỏa thuận giữa Nhất Tín và khách hàng |
| 33 | is_installation | Number |  | Đơn có hỗ trợ lắp đặt khi giao hàng không (1: Có; 0: Không; Mặc định để 0) |
| 34 | bill_type | Number |  | Loại vận đơn: 1 — bill giao (mặc định); 2 — bill thu hồi; 3 — bill trade-in (bill giao); 4 — bill thu hồi trade-in. |
| 35 | bill_return | String |  | Mã bill thu hồi (liên kết / tham chiếu bill thu hồi khi có). |

**JSON body respone**

| Name | Loại | Mô tả |
| --- | --- | --- |
| success | boolean | Trả về true nếu tạo hóa đơn thành công, tất cả các giá trị false khác nghĩa là lỗi. Xem thông báo để biết thêm mô tả. |
| message | string | Trả về thông điệp của phản hồi API. |
| data | object | Dữ liệu của phản hồi API. |

### Yêu cầu (Request)

```
//Đơn vị hành chính MỚI sau 01-07-2025
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

### Phản hồi (Response)

```
{
    "success": true,
    "data": {
        "bill_id": 0,
        "bill_code": "CP17364792",
        "ref_code": "",
        "status_id": 2,
        "cod_amount": 0,
        "service_id": 80,
        "payment_method": 10,
        "created_at": "2023-08-09 08:27:59",
        "main_fee": 82000,
        "cod_fee": 0,
        "insurr_fee": 0,
        "lifting_fee": 0,
        "remote_fee": 0,
        "counting_fee": 0,
        "packing_fee": 0,
        "total_fee": 82000,
        "expected_at": "2023-08-12 09:26:00",
        "partner_address_id": 0,
        "receiver_name": "Đồng hồ ABC",
        "receiver_phone": "**********",
        "receiver_address": "123",
        "package_no": 1,
        "weight": 2,
        "cargo_content": " | HÀNG DỄ VỠ 3",
        "cargo_value": 0,
        "note": ""
    },
    "message": "Create bill successfully"
}
```

>

Cập nhật vào ngày 10 tháng 7 năm 2025

Vận đơn

API liên quan đến vận đơn

Cập nhật vận đơn

API Cập nhật vận đơn
