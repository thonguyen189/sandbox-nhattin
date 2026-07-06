---
title: "Quận/Huyện"
source: "https://docs.ntlogistics.vn/docs/vi/location/districts"
---
Địa điểm

# Quận/Huyện

API danh sách Quận/Huyện.

## Cấu hình API

**HTTP Request**

GET: `v3/loc/districts`

| No. | Tham số | Loại | Bắt buộc | Mô tả |
| --- | --- | --- | --- | --- |
| 1 | province_id | Int | True | ID tỉnh thành |

**JSON body respone**

| Tên | Loại | Mô tả |
| --- | --- | --- |
| success | boolean | Trả về true nếu gửi yêu cầu thành công, tất cả các giá trị false khác có nghĩa là có lỗi. Xem message để biết thêm chi tiết. |
| message | string | Trả về thông điệp của phản hồi API. |
| data | object | Dữ liệu của phản hồi API. |

### Mẫu yêu cầu (Request example)

```
https://apisandbox.ntlogistics.vn/v3/loc/districts?province_id=99
```

### Phản hồi (Response)

```
{
    "success": true,
    "data": [
        {
            "id": "1101",
            "district_name": "TP.Cao Bằng",
            "is_new": "N"
        }
    ]
}
```

>

Cập nhật lần cuối vào ngày 7 tháng 7, 2023

Phường/Xã

API danh sách Phường/Xã.

Tỉnh/Thành phố

API danh sách Tỉnh/Thành phố.
