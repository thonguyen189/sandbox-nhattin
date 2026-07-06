---
title: "Webhook"
source: "https://docs.ntlogistics.vn/docs/vi/bill/webhook"
---
Vận đơn

# Webhook

Mô tả: Khi có bất kỳ cập nhật nào cho đơn hàng, hệ thống của Nhất Tín Tín sẽ tự động gửi cập nhật đó đến hệ thống của đối tác thông qua một URL (liên kết callback) mà đối tác đã cung cấp cho Nhất Tín..

## Cấu hình API

**HTTP Request**

GET/POST/PUT

| No. | Tham số | Loại | Bắt buộc | Mô tả |
| --- | --- | --- | --- | --- |
| 1 | bill_no | String | True | Mã vận đơn của Nhất Tín |
| 2 | ref_code | String | True | Mã tham chiếu của khách hàng - có thể là số hóa đơn hoặc số đặt hàng |
| 3 | status_id | Number | True | Tham khảo phần Master Data |
| 4 | status_name | String | True | Tham khảo phần Master Data |
| 5 | status_time | int | True | Thời điểm thay đổi trạng thái |
| 6 | push_time | int | True | Thời điểm đẩy thông tin sang đối tác |
| 7 | shipping_fee | Double | True | Phí vận chuyển |
| 8 | is_partial | int | True | Đơn hoàn về từ giao hàng 1 phần. 1: đúng, 0: không. |
| 9 | reason | String |  | Lý do nếu có từ Nhất Tín |
| 10 | weight | Double | True | Trọng lượng hàng hóa |
| 11 | dimension_weight | Double | True | Trọng lượng qui đổi hàng hóa |
| 12 | length | Double | True | Chiều dài hàng hóa (cm) |
| 13 | width | Double | True | Chiều rộng hàng hóa (cm) |
| 14 | height | Double | True | Chiều cao hàng hóa (cm) |
| 15 | expected_at | String | True | Ngày giao hàng dự kiến (format: "YYYY-MM-DD HH:mm:ss" - 2024-08-01 12:20:00) |

### Mẫ yêu cầu (Request example)

```
{
   "weight": 2,
   "bill_no": "CP16658276R",
   "status_time": 1681382601,
   "shipping_fee": 38610,
   "is_partial": 1,
   "status_name": "Đã lấy hàng",
   "status_id": 3,
   "dimension_weight": 1,
   "length": 1,
   "width": 1,
   "height": 1,
   "push_time": 1681382738,
   "ref_code": "40724974",
   "expected_at":"2024-08-02 09:00:00"
}
```

>

Cập nhật lần cuối vào ngày 30 tháng 9, 2024

Tra cứu vận đơn

API tra cứu vận đơn.

In vận đơn

API in vận đơn.
