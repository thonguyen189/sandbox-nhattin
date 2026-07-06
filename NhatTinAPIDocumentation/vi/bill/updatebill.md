---
title: "Cập nhật vận đơn"
source: "https://docs.ntlogistics.vn/docs/vi/bill/updateBill"
---
Vận đơn

# Cập nhật vận đơn

API Cập nhật vận đơn

## Cấu hình API

**HTTP Request**

POST: `/v3/bill/update-shipping`

| No. | Tham số | Loại | Bắt buộc | Mô tả |
| --- | --- | --- | --- | --- |
| 1 | partner_id | Number | True | Partner ID, được tạo trên Web portal |
| 2 | bill_code | String | True | Mã Bill |
| 3 | cod_amount | Double |  | Số tiền COD thu khi giao hàng cho người nhận. |
| 4 | cargo_value | Double |  | Giá trị hàng hoá |
| 5 | weight | Double |  | Trọng lượng hàng hoá |
| 6 | length | Double |  | Dài |
| 7 | height | Double |  | Cao |
| 8 | width | Double |  | Rộng |
| 9 | cargo_content | String |  | Mô tả sơ về hàng hóa của đơn vận chuyển |
| 10 | receiver_phone | String |  | Số điện thoại người nhận |
| 11 | receiver_name | String |  | Tên người nhận |
| 12 | receiver_address | String |  | Địa chỉ đầy đủ của người nhận (phải đúng định dạng: 52A Nguyễn Thái Bình). |
| 13 | package_no | Number |  | Số kiện |
| 14 | is_return_doc | Number |  | Có chuyển hoàn chứng từ không (1: Có; 0: Không; Mặc định để 0) |
| 15 | note | String |  | Ghi chú đơn hàng, lưu ý khi vận chuyển hàng hóa/ giao hàng |
| 16 | is_installation | Number |  | Đơn có hỗ trợ lắp đặt khi giao hàng không (1: Có; 0: Không; Mặc định để 0) |

**JSON body respone**

| Tên | Loại | Mô tả |
| --- | --- | --- |
| success | boolean | Trả về true nếu cập nhật vận đơn thành công, tất cả các giá trị false khác có nghĩa là lỗi. Xem message để biết thêm mô tả. |
| message | string | Trả về thông điệp của phản hồi API. |
| data | object | Dữ liệu của phản hồi API. |

### Mẫu yêu cầu (Request example)

```
{
   "partner_id": 123736,
   "bill_code": "TEST-009",
   "cod_amount": 200000,
   "is_return_doc": 0
}
```

### Phản hồi (Response)

```
 {
    "success": true,
    "message": "Update successful",
    "data": {
        "bill_id": 159839,
        "bill_code": "CP15983901",
        "ref_code": "TEST-009",
        "status_id": 2,
        "cod_amount": 200000,
        "service_id": 10,
        "payment_method": 10,
        "created_at": "2022-12-01 15:31:30",
        "main_fee": 33462,
        "cod_fee": 0,
        "insurr_fee": 5500,
        "lifting_fee": 0,
        "remote_fee": 0,
        "counting_fee": 0,
        "packing_fee": 0,
        "total_fee": 38962,
        "partner_address_id": 24371,
        "receiver_name": "Nguyễn Văn A",
        "receiver_phone": "09xxxxxxxx",
        "receiver_address": "52A, Nguyễn Thái Bình, P4, Tân Bình, TP.HCM",
        "package_no": 1,
        "weight": 1.3,
        "cargo_content": "Quần áo",
        "cargo_value": 1000000,
        "note": "ghi chú đơn hàng"
    }
}
```

### mẫu phản hồi thất bại (Response Failed)

```
{
    "success": false,
    "message": "descriptions for detail error",
    "data": {}
}
```

>

Cập nhật lần cuối vào ngày 10 tháng 7, 2025

Tạo vận đơn

Tự động truyền thông tin đơn hàng sang Nhất Tín các thông tin như kích thước , cân nặng , số điện thoại và nhiều thông tin khác. Từ đó sẽ tạo đơn giao nhận.

Hủy vận đơn

Hủy vận đơn đối với đơn trong trạng thái Chưa thành công (1) và Chờ lấy hàng (2).
