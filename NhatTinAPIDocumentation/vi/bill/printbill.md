---
title: "In vận đơn"
source: "https://docs.ntlogistics.vn/docs/vi/bill/printBill"
---
Vận đơn

# In vận đơn

API in vận đơn.

## Cấu hình API

**HTTP Request**

GET: `v3/bill/print?do_code={bill_code}&partner_id={partner_id}`

| No. | Tham số | Loại | Bắt buộc | Mô tả |
| --- | --- | --- | --- | --- |
| 1 | bill_code | String | True | Mã vận đơn |
| 2 | partner_id | Int | True | Partner ID, được tạo trên Web portal |

**JSON body respone**

| Tên | Loại | Mô tả |
| --- | --- | --- |
| success | boolean | Trả về true nếu việc in đơn thành công, tất cả các giá trị false khác đều có nghĩa là có lỗi. Xem message để biết thêm thông tin chi tiết. |
| message | string | Tin nhắn trả về từ API. |
| data | object | Dữ liệu đơn từ API. |

### Sample Request

```
 https://printdev.ntlogistics.vn/v1/bill/print?do_code=CP98783232&partner_id=88798
 https://printdigi.ntlogistics.vn/v1/bill/print?do_code=CP98783232&partner_id=88798
```

>

Cập nhật lần cuối vào ngày 7 tháng 7 năm 2023

Webhook

Mô tả: Khi có bất kỳ cập nhật nào cho đơn hàng, hệ thống của Nhất Tín Tín sẽ tự động gửi cập nhật đó đến hệ thống của đối tác thông qua một URL (liên kết callback) mà đối tác đã cung cấp cho Nhất Tín..

Địa điểm

API liên quan đến địa điểm quận huyện, phường xã, tỉnh thành
