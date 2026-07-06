---
title: "Hủy vận đơn"
source: "https://docs.ntlogistics.vn/docs/vi/bill/cancelBill"
---
Vận đơn

# Hủy vận đơn

Hủy vận đơn đối với đơn trong trạng thái Chưa thành công (1) và Chờ lấy hàng (2).

## Cấu hình API

**HTTP Request**

POST: `v3/bill/destroy`

| No. | Tham số | Loại | Bắt buộc | Mô tả |
| --- | --- | --- | --- | --- |
| 1 | bill_code | String Array | True | Danh sách mã vận đơn |

**JSON body respone**

| Name | Loại | Mô tả |
| --- | --- | --- |
| success | boolean | Trả về true nếu hủy đơn thành công, tất cả các trường hợp còn lại trả về false nghĩa là lỗi. Xem message để biết thêm mô tả. |
| message | string | Trả về thông điệp của phản hồi API. |
| data | object | Dữ liệu của phản hồi API. |

### Yêu cầu (Request)

```
{
"bill_code": ["CP12777690"]
}
```

### Phản hồi (Response)

```
{
  "success": true,
  "messsage": "",
  "data": [
    {
      "doCode": "E9999999",
      "message": "Bill E9999999 has canceled successful"
    },
    {
      "doCode": "E8888888",
      "message": "Bill E8888888 has canceled successful"
    }
  ]
}
```

>

Cập nhật lần cuối vào ngày 10 tháng 7 năm 2025

Cập nhật vận đơn

API Cập nhật vận đơn

Chuyển hoàn vận đơn

Chuyển hoàn vận đơn đối với những đơn Nhất Tín đã lấy hàng (3), đang vận chuyển (15), không phát được (7).
