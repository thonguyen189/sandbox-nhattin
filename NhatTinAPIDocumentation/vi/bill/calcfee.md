---
title: "Tính giá"
source: "https://docs.ntlogistics.vn/docs/vi/bill/calcFee"
---
Vận đơn

# Tính giá

## Cấu hình API

**HTTP Request**

POST: `/v3/bill/calc-fee`

| No. | Tham số | Loại | Bắt buộc | Mô tả |
| --- | --- | --- | --- | --- |
| 1 | partner_id | Number |  | ID của khách hàng, xem trên cấu hình tài khoản khi đăng nhập vào https://khachhang.ntlogistics.vn/ |
| 2 | weight | Double | True | Trọng lượng đơn hàng do khách hàng ước lượng / đo đạc. |
| 3 | width | Double |  | Chiều rộng kiện hàng, tính theo cm, có thể để mặc định là 0 nếu Khách hàng ko có thông số này |
| 4 | length | Double |  | Chiều dài kiện hàng, tính theo cm, có thể để mặc định là 0 nếu Khách hàng ko có thông số này |
| 5 | height | Double |  | Chiều cao kiện hàng, tính theo cm, có thể để mặc định là 0 nếu Khách hàng ko có thông số này |
| 6 | service_id | Number |  | Mã dịch vụ vận chuyển. |
| 7 | payment_method_id | Number | True | Mã hình thức thanh toán cước phí vận chuyển, tham chiếu vào trang Master Data để lấy mã. |
| 8 | cod_amount | Double |  | Số Tiền Thu Hộ nhà vận chuyển cần thu từ người nhận khi giao hàng. |
| 9 | cargo_value | Double |  | Giá trị hàng hóa. |
| 10 | s_province | String |  | Tên Tỉnh/Thành phố người gửi. (Dùng cho đơn vị hành chĩnh CŨ) |
| 11 | s_district | String |  | Tên Quận/Huyện người gửi. (Dùng cho đơn vị hành chĩnh CŨ) |
| 12 | r_province | String |  | Tên Tỉnh/Thành phố người nhận. (Dùng cho đơn vị hành chĩnh CŨ) |
| 13 | r_district | String |  | Tên Quận/Huyện người nhận. (Dùng cho đơn vị hành chĩnh CŨ) |
| 14 | s_province_id | String | True | ID Tỉnh/Thành phố người gửi. (Đối với đơn vị hành chính MỚI sau 01-07-2025) |
| 15 | s_ward_id | String | True | ID Phường/Xã người gửi. (Đối với đơn vị hành chính MỚI sau 01-07-2025) |
| 16 | r_province_id | String | True | ID Tỉnh/Thành phố người nhận. (Đối với đơn vị hành chính MỚI sau 01-07-2025) |
| 17 | r_ward_id | String | True | ID Phường/Xã người nhận. (Đối với đơn vị hành chính MỚI sau 01-07-2025) |

**JSON body respone**

| Name | Loại | Mô tả |
| --- | --- | --- |
| success | boolean | Trả về true nếu tính giá thành công, tất cả các giá trị false khác nghĩa là lỗi. Xem message để biết thêm mô tả. |
| message | string | Trả về thông điệp của phản hồi API. |
| data | object | Dữ liệu của phản hồi API. |

### Mẫu yêu cầu (Request example)

```
//Đơn vị hành chính MỚI sau 01-07-2025
{
   "partner_id": 123736,
   "weight": 1.3,
   "payment_method_id": 10,
   "cod_amount": 120000,
   "cargo_value": 2000000,
   "s_province_id": "79",
   "s_ward_id": "27007",
   "r_province_id": "01",
   "r_ward_id": "00004",
}
```

```
//Đơn vị hành chính cũ
{
   "partner_id": 123736,
   "weight": 1.3,
   "payment_method_id": 10,
   "cod_amount": 120000,
   "cargo_value": 2000000,
   "s_province": "Hồ Chí Minh",
   "s_district": "Tân Bình",
   "r_province": "Hà Nội",
   "r_district": "Thanh Xuân",
}
```

### Phản hồi (Response)

```
{
   "success": true,
   "message": "",
   "data": [
      {
         "weight": 3,
         "total_fee": 109910,
         "main_fee": 109910,
         "insur_fee": 0,
         "remote_fee": 0,
         "cod_fee": 0,
         "service_id": 21,
         "service_name": "MES",
         "lead_time": "31/08/2021 23:10"
      },
      {
         "weight": 3,
         "total_fee": 66152,
         "main_fee": 66152,
         "insur_fee": 0,
         "remote_fee": 0,
         "cod_fee": 0,
         "service_id": 20,
         "service_name": "Đường bộ",
         "lead_time": "02/09/2021 11:10"
      },
      {
         "weight": 3,
         "total_fee": 301158,
         "main_fee": 301158,
         "insur_fee": 0,
         "remote_fee": 0,
         "cod_fee": 0,
         "service_id": 11,
         "service_name": "Hỏa tốc",
         "lead_time": "29/08/2021 09:10"
      },
      {
         "weight": 3,
         "total_fee": 146589,
         "main_fee": 146589,
         "insur_fee": 0,
         "remote_fee": 0,
         "cod_fee": 0,
         "service_id": 10,
         "service_name": "CPN",
         "lead_time": "29/08/2021 11:10"
      }
   ]
}
```

>

Last updated on 10 tháng 7, 2025

Chuyển hoàn vận đơn

Chuyển hoàn vận đơn đối với những đơn Nhất Tín đã lấy hàng (3), đang vận chuyển (15), không phát được (7).

Tra cứu vận đơn

API tra cứu vận đơn.
