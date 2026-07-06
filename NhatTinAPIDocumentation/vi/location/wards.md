---
title: "Phường/Xã"
source: "https://docs.ntlogistics.vn/docs/vi/location/wards"
---
Địa điểm

# Phường/Xã

API danh sách Phường/Xã.

## Cấu hình API

**HTTP Request**

GET: `v3/loc/wards`

| No. | Tham số | Loại | Bắt buộc | Mô tả |
| --- | --- | --- | --- | --- |
| 1 | district_id | String | True | Mã quận / huyện cũ |
| 2 | is_new | String | True | 1: Đơn vị mới, 0: Đơn vị cũ (Mặc định: 0) |
| 3 | province_id | String | True | Mã Tỉnh mới |

**JSON body respone**

| Tên | Loại | Mô tả |
| --- | --- | --- |
| success | boolean | Trả về true nếu gửi yêu cầu thành công, tất cả các giá trị false khác có nghĩa là có lỗi. Xem message để biết thêm chi tiết. |
| message | string | Trả về thông điệp của phản hồi API. |
| data | object | Dữ liệu của phản hồi API. |

### Mẫu yêu cầu (Request example)

```
https://apisandbox.ntlogistics.vn/v3/loc/wards?district_id=9904
https://apisandbox.ntlogistics.vn/v3/loc/wards?is_new=1&province_id=01
```

### Response

```
{
    "success": true,
    "data": [
        {
            "id": "01106",
            "ward_name": "P.Hồng Gai",
            "is_new": "N"
        }
    ]
}
```

>

Cập nhật lần cuối vào ngày 9 tháng 7, 2025

Địa điểm

API liên quan đến địa điểm quận huyện, phường xã, tỉnh thành

Quận/Huyện

API danh sách Quận/Huyện.
