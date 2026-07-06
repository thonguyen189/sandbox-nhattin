---
title: "Tra cứu vận đơn"
source: "https://docs.ntlogistics.vn/docs/vi/bill/trackingBill"
---
Vận đơn

# Tra cứu vận đơn

API tra cứu vận đơn.

## Cấu hình API

**HTTP Request**

GET: `v3/bill/tracking`

| No. | Tham số | Loại | Bắt buộc | Mô tả |
| --- | --- | --- | --- | --- |
| 1 | bill_code | String | True | Mã vận đơn |

**JSON body respone**

| Tên | Loại | Mô tả |
| --- | --- | --- |
| success | boolean | Trả về true nếu gửi yêu cầu thành công, tất cả các giá trị false khác có nghĩa là lỗi. Xem message để biết thêm mô tả. |
| message | string | Trả về thông điệp của phản hồi API. |
| data | object | Dữ liệu của phản hồi API. |

### Mẫu yêu cầu (Request example)

```
{
  "bill_code": "CP17340160"
}
```

### Phản hồi (Response)

```
{
    "success": true,
    "data": [
        {
            "bill_code": "CP17412845",
            "ref_code": "",
            "weight": "1.00",
            "dimension_weight": "0.00",
            "width": "0",
            "length": "0",
            "height": "0",
            "payment_status": "",
            "payment_at": "",
            "bill_status_id": 4,
            "bill_status_desc": "Đã giao hàng",
            "date_pickup": "2023-08-17 09:26:44.296",
            "pay_method": "NGTTS",
            "service": "CPN",
            "cod_amt": "0.00",
            "cod_fee": "0.0",
            "date_expected": "2023-08-18 23:59:59.0",
            "description": "HÀNG DỄ VỠ 3",
            "cargo_content": "Hàng Hóa",
            "insurance_fee": "0.0",
            "counting_fee": "0",
            "lifting_fee": "0.0",
            "packing_fee": "0",
            "delivery_fee": "0.0",
            "other_fee": "0.0",
            "remote_fee": "0.0",
            "main_fee": "0.00",
            "total_fee": "20000",
            "sender_name": "Log 3",
            "sender_phone": "09xxxxxxx",
            "sender_address": "12B HTM",
            "sender_ward": "Phường Dịch Vọng Hậu",
            "sender_district": "Quận Cầu Giấy",
            "sender_province": "Hà Nội",
            "receiver_name": "Hà Nội",
            "receiver_phone": "09xxxxxxx",
            "receiver_address": "khL",
            "receiver_ward": "Phường Phú Thượng",
            "receiver_district": "Quận Tây Hồ",
            "receiver_province": "Hà Nội",
            "date_delivery": "2023-08-17 09:30:17.0",
            "note": "Không cho xem hàng. GIAO GIỜ HÀNH CHÍNH, Ở CÔNG TY",
            "histories": [
                {
                    "sequence": 0,
                    "log_status": "10",
                    "city": "Hà Nội",
                    "district": "Quận Cầu Giấy",
                    "operation_en": "Received from Log 3",
                    "operationID": 0,
                    "operation": "Nhận vận đơn từ Log 3",
                    "loc_time": "17/08/2023 09:26"
                },
                {
                    "sequence": 1,
                    "log_status": "20",
                    "city": "TP. Hồ Chí Minh",
                    "district": "Quận Tân Phú",
                    "operation_en": "Bill import to Kho Tây Thạnh",
                    "operationID": 4399184,
                    "operationType": "WIW",
                    "delayReason": "",
                    "operation": "Nhập vận đơn vào Kho Tây Thạnh",
                    "loc_time": "17/08/2023 09:27"
                },
                {
                    "sequence": 2,
                    "log_status": "20",
                    "city": "TP. Hồ Chí Minh",
                    "district": "Quận Tân Phú",
                    "operation_en": "Bill export from Kho Tây Thạnh",
                    "operationID": 4399185,
                    "operationType": "WEG",
                    "delayReason": "",
                    "operation": "Xuất vận đơn từ Kho Tây Thạnh",
                    "loc_time": "17/08/2023 09:28"
                },
                {
                    "sequence": 3,
                    "log_status": "30",
                    "city": "Hà Nội",
                    "district": "Quận Tây Hồ",
                    "operation_en": "Delivered to Abc",
                    "operationID": 0,
                    "operation": "Trả vận đơn cho Abc",
                    "loc_time": "17/08/2023 09:30"
                }
            ],
            "p_link_image": "",
            "bill_image_link": [
                "https://cdndev.ntlogistics.vn/uploads/lien-trang/2023/08/17/mobile-app/CP17412845/d83a5d8bd22b49b484b85035895.jpg",
                "https://cdndev.ntlogistics.vn/uploads/lien-hong/2023/08/17/mobile-app/CP17412845/60d2176396adfee7b20cfac08.jpg"
            ], //Image for Picking and Receiving
            "document_image_link": [
                "https://cdndev.ntlogistics.vn/uploads/chung-tu/2023/08/17/mobile-app/CP17412845/739b6b31542949dda8c47c70600.jpg",
                "https://cdndev.ntlogistics.vn/uploads/chung-tu/2023/08/17/mobile-app/CP17412845/1034fe7f142e4eed90f15639dc5.jpg"
            ]//Image for document
        }
    ],
    "message": "Tracking successfully"
}
```

>

Cập nhật lần cuối vào ngày 15 tháng 5, 2024

Tính giá

Webhook

Mô tả: Khi có bất kỳ cập nhật nào cho đơn hàng, hệ thống của Nhất Tín Tín sẽ tự động gửi cập nhật đó đến hệ thống của đối tác thông qua một URL (liên kết callback) mà đối tác đã cung cấp cho Nhất Tín..
