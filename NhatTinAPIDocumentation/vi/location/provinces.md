---
title: "Tỉnh/Thành phố"
source: "https://docs.ntlogistics.vn/docs/vi/location/provinces"
---
Địa điểm

# Tỉnh/Thành phố

API danh sách Tỉnh/Thành phố.

## Cấu hình API

**HTTP Request**

GET: `v3/loc/provinces`

| No. | Tham số | Loại | Bắt buộc | Mô tả |
| --- | --- | --- | --- | --- |
| 1 | is_new | Int | True | 1: Đơn vị mới, 0: Đơn vị cũ (Mặc định: 0) |

**JSON body respone**

| Tên | Loại | Mô tả |
| --- | --- | --- |
| success | boolean | Trả về true nếu gửi yêu cầu thành công, tất cả các giá trị false khác có nghĩa là có lỗi. Xem message để biết thêm chi tiết. |
| message | string | Trả về thông điệp của phản hồi API. |
| data | object | Dữ liệu của phản hồi API. |

### Mẫu yêu cầu (Request example)

```
https://apisandbox.ntlogistics.vn/v3/loc/provinces
```

### Phản hồi (Response)

```
{
    "success": true,
    "data": [
        {
            "id": "11",
            "province_name": "Cao Bằng",
            "is_new": "N"
        }
    ]
}
```

>

Cập nhật lần cuối vào ngày 7 tháng 7, 2023

Quận/Huyện

API danh sách Quận/Huyện.
