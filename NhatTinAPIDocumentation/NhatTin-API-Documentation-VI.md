# Tài liệu API Nhất Tín Logistics (Tiếng Việt)

> Tài liệu tích hợp dành cho lập trình viên · Bản hợp nhất offline từ bộ tài liệu `NhatTinAPIDocumentation/vi`, bổ sung phân tích tích hợp & vận hành đồng bộ từ `docs/PhanTich-ThaoTac-DongBo` — dùng làm tài nguyên nguồn cho NotebookLM.
>
> Nguồn: https://docs.ntlogistics.vn/docs/vi (API gốc) + phân tích nội bộ TruePos ↔ Nhất Tín.

## Mục lục

1. [Thông tin kết nối & Dữ liệu chính](#1-thông-tin-kết-nối)
2. [Xác thực JWT](#2-xác-thực-jwt)
3. [Vận đơn (Bill)](#3-vận-đơn-bill)
   - [3.1. Tạo vận đơn](#31-tạo-vận-đơn)
   - [3.2. Cập nhật vận đơn](#32-cập-nhật-vận-đơn)
   - [3.3. Hủy vận đơn](#33-hủy-vận-đơn)
   - [3.4. Chuyển hoàn vận đơn](#34-chuyển-hoàn-vận-đơn)
   - [3.5. Tính giá](#35-tính-giá)
   - [3.6. Tra cứu vận đơn](#36-tra-cứu-vận-đơn)
   - [3.7. In vận đơn](#37-in-vận-đơn)
   - [3.8. Webhook](#38-webhook)
4. [Địa điểm (Location)](#4-địa-điểm-location)
   - [4.1. Tỉnh/Thành phố](#41-tỉnhthành-phố)
   - [4.2. Quận/Huyện](#42-quậnhuyện)
   - [4.3. Phường/Xã](#43-phườngxã)
5. [Phân tích tích hợp & Vận hành đồng bộ (TruePos ↔ Nhất Tín)](#5-phân-tích-tích-hợp--vận-hành-đồng-bộ-truepos--nhất-tín)
   - [5.1. Phản hồi chính thức từ Nhất Tín](#51-phản-hồi-chính-thức-từ-nhất-tín)
   - [5.2. Ma trận đồng bộ dữ liệu](#52-ma-trận-đồng-bộ-dữ-liệu)
   - [5.3. Luồng triển khai & vai trò endpoint](#53-luồng-triển-khai--vai-trò-endpoint)
   - [5.4. Dữ liệu cần lưu tối thiểu khi thiết kế](#54-dữ-liệu-cần-lưu-tối-thiểu-khi-thiết-kế)
   - [5.5. Trạng thái xác minh (đã xác nhận / còn chờ)](#55-trạng-thái-xác-minh-đã-xác-nhận--còn-chờ)

---

## 1. Thông tin kết nối

### Thông báo quan trọng!!!

- Hệ thống Nhất Tín sẽ đồng bộ với Hệ thống địa danh mới của Nhà nước. Chi tiết như sau:

1. Yêu cầu Khách hàng cần mapping theo mã địa danh của Nhà nước khi truyền qua Nhất Tín.
2. Từ 08/07/2025 đến 20/07/2025 Khách hàng API có thể UAT ở môi trường sandbox.
3. Từ 24/07/2025 sẽ go-live trên môi trường production. API địa danh mới ở mục `/location`. Khách hàng có thể tham khảo và mapping.

### Môi trường

Nhất Tín cung cấp hai môi trường riêng biệt cho việc tích hợp:

- **Sandbox:** Sử dụng cho việc xây dựng tính năng, kiểm thử, gỡ lỗi, v.v...
- **Production:** Sử dụng cho Người dùng cuối.

### Request header

**authorization:** `Bearer <access_token>` (vui lòng tham khảo phần Xác thực JWT).

> Sơ đồ quy trình xác thực và tạo vận đơn (Authentication and Bill Creation Process): https://docs.ntlogistics.vn/image/auth_flow.png

### Request body

The request body dưới định dạng JSON. Data sẽ thay đổi tùy thuộc vào yêu cầu. Để biết thêm thông tin, hãy xem cụ thể về các API.

### Cấu hình

| Môi trường | Host |
| --- | --- |
| Sandbox | `https://apisandbox.ntlogistics.vn` |
| Production | `https://apiws.ntlogistics.vn` |

| Môi trường | Portal Web |
| --- | --- |
| Sandbox | `https://bodev.ntlogistics.vn` |
| Production | `https://khachhang.ntlogistics.vn` |

### Dữ liệu chính (Master Data)

#### 1. Dịch vụ (service_id)

| ID | Tên dịch vụ |
| --- | --- |
| 90 | Giao hàng nhanh (CPN) |
| 81 | Hỏa tốc |
| 91 | Tiết kiệm |
| 21 | Hỗn hợp MES |

#### 2. Hình thức thanh toán (payment_method)

| ID | Hình thức thanh toán |
| --- | --- |
| 10 | Người gửi thanh toán ngay |
| 11 | Người gửi thanh toán sau |
| 20 | Người nhận thanh toán ngay |

#### 3. Loại hàng hóa (cargo_type_id)

| ID | Loại hàng hóa |
| --- | --- |
| 1 | Chứng từ |
| 2 | Hàng hóa |
| 3 | Hàng lạnh |
| 4 | Sinh phẩm |
| 5 | Mẫu bệnh phẩm |

#### 4. Trạng thái đơn (status_id)

| Status ID | Status Code | Mô tả |
| --- | --- | --- |
| 1 | Waiting | Chưa thành công |
| 2 | Waiting | Chờ lấy hàng |
| 3 | KCB | Đã lấy hàng |
| 4 | FBC | Đã giao hàng |
| 6 | GBV | Hủy |
| 7 | FUD | Không phát được |
| 9 | NRT | Đang chuyển hoàn |
| 10 | MRC | Đã chuyển hoàn |
| 11 | QIU | Sự cố giao hàng |
| 12 | DRF | Vận đơn nháp |
| 13 | DEL | Đang giao hàng |
| 15 |  | Đang vận chuyển |
| 16 |  | Đang giao hàng hoàn |
| 17 |  | Lỗi lấy hàng |

#### 5. Qui trình trạng thái vận đơn

![Sơ đồ qui trình trạng thái vận đơn Nhất Tín Logistics](vi/assets/image-status-flow.webp)

_Qui trình trạng thái vận đơn — sơ đồ luồng chuyển trạng thái (tham chiếu bảng `status_id` ở trên)._

### Lịch sử cập nhật

| Phiên bản | Tác giả | Mô tả | Ngày cập nhật |
| --- | --- | --- | --- |
| 1.0.4 | KhoaNT | Change fulladdress from sender | 10/07/2021 |
| 1.0.5 | KhoaNT | Add calculate fee api | 23/07/2021 |
| 1.0.6 | KhoaNT | Add email receiver | 28/05/2022 |
| 1.0.7 | KhoaNT | Edit request webhook | 26/08/2022 |
| 1.0.7 | KhoaNT | Add connect to NTL | 26/08/2022 |
| 1.0.7 | KhoaNT | Add params create bill : is_return_doc | 26/08/2022 |
| 1.0.8 | KhoaNT | Add params partner_id in Print Waybill | 01/12/2022 |
| 1.0.9 | KhoaNT | Add new api for update shipping info | 02/12/2022 |
| 1.0.10 | KhoaNT | Add new params response when tracking : p_link_image | 24/02/2023 |
| 1.0.11 | KhoaNT | Add new shipping version 2 | 29/03/2023 |
| 1.0.12 | KhoaNT | Update new service list | 01/07/2023 |
| 1.0.13 | KhoaNT | Add new status (15,16) | 28/07/2023 |
| 1.0.14 | KhoaNT | Add new status (17) && Add length, width, height Fields into webhook request | 30/12/2023 |

### Danh sách API

| API List | Ghi chú |
| --- | --- |
| 1. Create a Shipping v2 | 10/4/2023 sử dụng V2. |
| 2. Update Shipping |  |
| 3. Cancel Shipping |  |
| 4. Calculate Price |  |
| 5. Tracking Shipping |  |
| 6. Webhook |  |
| 7. Print waybill |  |
| 8. Login - connect to NTL | Use for ecommerce/POS platforms |
| 9. Create a Shipping By ID | Xóa api này, hổ trợ KH cũ |

_Cập nhật vào ngày 9 tháng 7 năm 2025._

---

## 2. Xác thực JWT

Cách đăng nhập, gọi API bằng JWT và làm mới token.

### Bạn cần làm gì

Thực hiện các bước sau để xác thực và gọi API.

#### 1) Đăng nhập để lấy token

Nhất Tín cung cấp bộ tài khoản tương ứng với từng môi trường.

Yêu cầu đăng nhập:

```
POST /v1/auth/sign-in
Content-Type: application/json

{
  "username": "your_account",
  "password": "your_password"
}
```

Phản hồi đăng nhập thành công:

```json
{
  "success": true,
  "data": {
    "jwt_token": "<access_token>",
    "token_type": "Bearer",
    "token_expires_in": "1m",
    "refresh_token": "<refresh_token>",
    "refresh_expires_in": "2m"
  }
}
```

Giữ kín `refresh_token`. Bạn sẽ dùng nó để gia hạn truy cập khi access token hết hạn.

#### 2) Gọi API bằng access token

Gửi header `Authorization` với access token ở mọi request:

```
GET /v3/your-endpoint
Headers:
{
  "Authorization": "Bearer <access_token>"
}
```

Nếu nhận `401` do token hết hạn, hãy làm mới token (bước tiếp theo) rồi gọi lại.

#### 3) Làm mới access token

```
POST /v1/auth/refresh-token
Content-Type: application/json

{
  "refresh_token": "<refresh_token>"
}
```

Phản hồi thành công:

```json
{
  "success": true,
  "data": {
    "jwt_token": "<new_access_token>",
    "token_type": "Bearer",
    "token_expires_in": "1m",
    "refresh_token": "<new_refresh_token>",
    "refresh_expires_in": "2m"
  }
}
```

### Lỗi thường gặp

| Lỗi | Trạng thái | Khi nào xảy ra | Cách khắc phục |
| --- | --- | --- | --- |
| Yêu cầu xác thực | 401 | Thiếu header Authorization | Thêm `Authorization: Bearer <access_token>` |
| Sai định dạng header | 401 | Header không đúng định dạng `Bearer <token>` | Sửa đúng định dạng |
| Token hết hạn | 401 | Access token hết hạn | Làm mới token, sau đó gọi lại |
| Xác thực thất bại | 401 | Token không hợp lệ | Đăng nhập lại để lấy token mới |
| Thiếu refresh token | 400 | Thiếu `refresh_token` trong yêu cầu làm mới | Bổ sung `refresh_token` trong body |

_Cập nhật vào 16/09/2025 bởi Nhất Tín Logistics._

---

## 3. Vận đơn (Bill)

API liên quan đến vận đơn: tạo, cập nhật, hủy, chuyển hoàn, tính giá, tra cứu, webhook và in vận đơn.

### 3.1. Tạo vận đơn

Tự động truyền thông tin đơn hàng sang Nhất Tín các thông tin như kích thước, cân nặng, số điện thoại và nhiều thông tin khác. Từ đó sẽ tạo đơn giao nhận.

#### Cấu hình API

**HTTP Request** — `POST /v3/bill/create`

| No. | Tham số | Loại | Bắt buộc | Mô tả |
| --- | --- | --- | --- | --- |
| 1 | ref_code | String |  | Số Đơn Hàng tham chiếu của khách hàng, ví dụ số Hóa Đơn, số đơn bán hàng... |
| 2 | package_no | Number |  | Số lượng kiện (để mặc định 1 nếu khách hàng rõ là bao nhiêu kiện hàng) |
| 3 | weight | Double | **True** | Trọng lượng đơn hàng do khách hàng ước lượng / đo dạc |
| 4 | width | Double |  | Chiều rộng kiện hàng, tính theo cm, có thể để mặc định là 0 nếu Khách hàng ko có thông số này |
| 5 | length | Double |  | Chiều dài kiện hàng, tính theo cm, có thể để mặc định là 0 nếu Khách hàng ko có thông số này |
| 6 | height | Double |  | Chiều cao kiện hàng, tính theo cm, có thể để mặc định là 0 nếu Khách hàng ko có thông số này |
| 7 | cargo_content | String |  | Mô tả sơ về hàng hóa của đơn vận chuyển |
| 8 | service_id | Number | **True** | Mã dịch vụ vận chuyển, tham chiếu vào trang Master Data để lấy mã |
| 9 | payment_method_id | Number | **True** | Mã hình thức thanh toán cước phí vận chuyển, tham chiếu vào trang Master Data để lấy mã |
| 10 | is_return_doc | int |  | Có chuyển hoàn chứng từ không (1: Có; 0: Không; Mặc định để 0) |
| 11 | cod_amount | Number |  | Số Tiền Thu Hộ nhà vận chuyển cần thu từ người nhận khi giao hàng |
| 12 | note | String |  | Ghi chú đơn hàng, lưu ý khi vận chuyển hàng hóa/ giao hàng |
| 13 | cargo_value | Double |  | Giá trị hàng hóa |
| 14 | cargo_type_id | Number | **True** | Mã Loại hàng hóa (chứng từ hay hàng hóa thông thường), tham chiếu trang Master Data để lấy ID |
| 15 | s_name | String | **True** | Tên người gửi |
| 16 | s_phone | String | **True** | Số điện thoại người gửi |
| 17 | s_address | String | **True** | Địa chỉ người gửi |
| 18 | s_province_code | String | **True** | Tỉnh/Thành phố người gửi (Code theo đơn vị hành chính quốc gia mới) |
| 19 | s_ward_code | String | **True** | Phường/Xã người gửi (Code theo đơn vị hành chính quốc gia mới) |
| 20 | is_return_org | Number |  | Đặc tả địa chỉ hoàn. (mặc định là 0) - Nếu thiết lập = 1 thì bắt buộc truyền thêm các hạng mục 23 đến 28 - Nếu mặc định = 0 thì lấy theo địa chỉ gốc (thông tin người gửi) |
| 21 | return_name | String |  | Tên người nhận thông tin hoàn hàng |
| 22 | return_phone | String |  | Số điện thoại nơi hoàn hàng |
| 23 | return_address | String |  | Địa chỉ hoàn hàng |
| 24 | return_province_code | String |  | Tên Tỉnh / Thành phổ hoàn hàng (Code theo đơn vị hành chính quốc gia mới) |
| 25 | return_ward_code | String | **True** | Tên Phường / Xã hoàn hàng (Code theo đơn vị hành chính quốc gia mới) |
| 26 | r_name | String | **True** | Tên người nhận |
| 27 | r_phone | String | **True** | Số điện thoại người nhận |
| 28 | r_address | String | **True** | Địa chỉ người nhận |
| 29 | r_province_code | String | **True** | Tỉnh/Thành phố người nhận (Code theo đơn vị hành chính quốc gia mới) |
| 30 | r_ward_code | String | **True** | Phường/Xã người nhận (Code theo đơn vị hành chính quốc gia mới) |
| 31 | is_draft | Number |  | Đặc tả tạo đơn nháp (mặc định là 0) - Nếu thiết lập = 1, đơn được hiểu là đơn nháp - chờ đóng gói từ Khách hàng. Nhất Tín chưa sẳn sàng lấy hàng. Khi đơn đã được đóng gói thì KH chuyển sang trạng thái chờ lấy hàng (status_id=2) - Nếu mặc định = 0 thì được hiểu đơn đã sẳn sàng lấy hàng (đơn ở trạng thái Chờ lấy hàng) |
| 32 | other_fee | Number |  | Phí khác theo thỏa thuận giữa Nhất Tín và khách hàng |
| 33 | is_installation | Number |  | Đơn có hỗ trợ lắp đặt khi giao hàng không (1: Có; 0: Không; Mặc định để 0) |
| 34 | bill_type | Number |  | Loại vận đơn: 1 — bill giao (mặc định); 2 — bill thu hồi; 3 — bill trade-in (bill giao); 4 — bill thu hồi trade-in. |
| 35 | bill_return | String |  | Mã bill thu hồi (liên kết / tham chiếu bill thu hồi khi có). |

**JSON body response**

| Name | Loại | Mô tả |
| --- | --- | --- |
| success | boolean | Trả về true nếu tạo hóa đơn thành công, tất cả các giá trị false khác nghĩa là lỗi. Xem thông báo để biết thêm mô tả. |
| message | string | Trả về thông điệp của phản hồi API. |
| data | object | Dữ liệu của phản hồi API. |

#### Yêu cầu (Request)

```json
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

#### Phản hồi (Response)

```json
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

_Cập nhật vào ngày 10 tháng 7 năm 2025._

### 3.2. Cập nhật vận đơn

API Cập nhật vận đơn.

#### Cấu hình API

**HTTP Request** — `POST /v3/bill/update-shipping`

| No. | Tham số | Loại | Bắt buộc | Mô tả |
| --- | --- | --- | --- | --- |
| 1 | partner_id | Number | **True** | Partner ID, được tạo trên Web portal |
| 2 | bill_code | String | **True** | Mã Bill |
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

**JSON body response**

| Tên | Loại | Mô tả |
| --- | --- | --- |
| success | boolean | Trả về true nếu cập nhật vận đơn thành công, tất cả các giá trị false khác có nghĩa là lỗi. Xem message để biết thêm mô tả. |
| message | string | Trả về thông điệp của phản hồi API. |
| data | object | Dữ liệu của phản hồi API. |

#### Mẫu yêu cầu (Request example)

```json
{
   "partner_id": 123736,
   "bill_code": "TEST-009",
   "cod_amount": 200000,
   "is_return_doc": 0
}
```

#### Phản hồi (Response)

```json
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

#### Mẫu phản hồi thất bại (Response Failed)

```json
{
    "success": false,
    "message": "descriptions for detail error",
    "data": {}
}
```

_Cập nhật lần cuối vào ngày 10 tháng 7, 2025._

### 3.3. Hủy vận đơn

Hủy vận đơn đối với đơn trong trạng thái **Chưa thành công (1)** và **Chờ lấy hàng (2)**.

#### Cấu hình API

**HTTP Request** — `POST v3/bill/destroy`

| No. | Tham số | Loại | Bắt buộc | Mô tả |
| --- | --- | --- | --- | --- |
| 1 | bill_code | String Array | **True** | Danh sách mã vận đơn |

**JSON body response**

| Name | Loại | Mô tả |
| --- | --- | --- |
| success | boolean | Trả về true nếu hủy đơn thành công, tất cả các trường hợp còn lại trả về false nghĩa là lỗi. Xem message để biết thêm mô tả. |
| message | string | Trả về thông điệp của phản hồi API. |
| data | object | Dữ liệu của phản hồi API. |

#### Yêu cầu (Request)

```json
{
"bill_code": ["CP12777690"]
}
```

#### Phản hồi (Response)

```json
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

_Cập nhật lần cuối vào ngày 10 tháng 7 năm 2025._

### 3.4. Chuyển hoàn vận đơn

Chuyển hoàn vận đơn đối với những đơn Nhất Tín **đã lấy hàng (3)**, **đang vận chuyển (15)**, **không phát được (7)**.

#### Cấu hình API

**HTTP Request** — `POST v3/bill/revert-bill`

| No. | Tham số | Loại | Bắt buộc | Mô tả |
| --- | --- | --- | --- | --- |
| 1 | bill_code | String Array | **True** | Danh sách mã vận đơn |

**JSON body response**

| Name | Loại | Mô tả |
| --- | --- | --- |
| success | boolean | Trả về true nếu hủy đơn thành công, tất cả các trường hợp còn lại trả về false nghĩa là lỗi. Xem message để biết thêm mô tả. |
| message | string | Trả về thông điệp của phản hồi API. |
| data | object | Dữ liệu của phản hồi API. |

#### Yêu cầu (Request)

```json
{
"bill_code": ["CP12777690"]
}
```

#### Phản hồi (Response)

```json
{
    "success": true,
    "data": {
        "success": [
            "CP10188714"
        ],
        "failed": []
    },
    "message": ""
}
```

_Cập nhật lần cuối vào ngày 10 tháng 7 năm 2025._

### 3.5. Tính giá

#### Cấu hình API

**HTTP Request** — `POST /v3/bill/calc-fee`

| No. | Tham số | Loại | Bắt buộc | Mô tả |
| --- | --- | --- | --- | --- |
| 1 | partner_id | Number |  | ID của khách hàng, xem trên cấu hình tài khoản khi đăng nhập vào https://khachhang.ntlogistics.vn/ |
| 2 | weight | Double | **True** | Trọng lượng đơn hàng do khách hàng ước lượng / đo đạc. |
| 3 | width | Double |  | Chiều rộng kiện hàng, tính theo cm, có thể để mặc định là 0 nếu Khách hàng ko có thông số này |
| 4 | length | Double |  | Chiều dài kiện hàng, tính theo cm, có thể để mặc định là 0 nếu Khách hàng ko có thông số này |
| 5 | height | Double |  | Chiều cao kiện hàng, tính theo cm, có thể để mặc định là 0 nếu Khách hàng ko có thông số này |
| 6 | service_id | Number |  | Mã dịch vụ vận chuyển. |
| 7 | payment_method_id | Number | **True** | Mã hình thức thanh toán cước phí vận chuyển, tham chiếu vào trang Master Data để lấy mã. |
| 8 | cod_amount | Double |  | Số Tiền Thu Hộ nhà vận chuyển cần thu từ người nhận khi giao hàng. |
| 9 | cargo_value | Double |  | Giá trị hàng hóa. |
| 10 | s_province | String |  | Tên Tỉnh/Thành phố người gửi. (Dùng cho đơn vị hành chĩnh CŨ) |
| 11 | s_district | String |  | Tên Quận/Huyện người gửi. (Dùng cho đơn vị hành chĩnh CŨ) |
| 12 | r_province | String |  | Tên Tỉnh/Thành phố người nhận. (Dùng cho đơn vị hành chĩnh CŨ) |
| 13 | r_district | String |  | Tên Quận/Huyện người nhận. (Dùng cho đơn vị hành chĩnh CŨ) |
| 14 | s_province_id | String | **True** | ID Tỉnh/Thành phố người gửi. (Đối với đơn vị hành chính MỚI sau 01-07-2025) |
| 15 | s_ward_id | String | **True** | ID Phường/Xã người gửi. (Đối với đơn vị hành chính MỚI sau 01-07-2025) |
| 16 | r_province_id | String | **True** | ID Tỉnh/Thành phố người nhận. (Đối với đơn vị hành chính MỚI sau 01-07-2025) |
| 17 | r_ward_id | String | **True** | ID Phường/Xã người nhận. (Đối với đơn vị hành chính MỚI sau 01-07-2025) |

**JSON body response**

| Name | Loại | Mô tả |
| --- | --- | --- |
| success | boolean | Trả về true nếu tính giá thành công, tất cả các giá trị false khác nghĩa là lỗi. Xem message để biết thêm mô tả. |
| message | string | Trả về thông điệp của phản hồi API. |
| data | object | Dữ liệu của phản hồi API. |

#### Mẫu yêu cầu (Request example)

```json
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

```json
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

#### Phản hồi (Response)

```json
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

_Cập nhật lần cuối vào ngày 10 tháng 7, 2025._

### 3.6. Tra cứu vận đơn

API tra cứu vận đơn.

#### Cấu hình API

**HTTP Request** — `GET v3/bill/tracking`

| No. | Tham số | Loại | Bắt buộc | Mô tả |
| --- | --- | --- | --- | --- |
| 1 | bill_code | String | **True** | Mã vận đơn |

**JSON body response**

| Tên | Loại | Mô tả |
| --- | --- | --- |
| success | boolean | Trả về true nếu gửi yêu cầu thành công, tất cả các giá trị false khác có nghĩa là lỗi. Xem message để biết thêm mô tả. |
| message | string | Trả về thông điệp của phản hồi API. |
| data | object | Dữ liệu của phản hồi API. |

#### Mẫu yêu cầu (Request example)

```json
{
  "bill_code": "CP17340160"
}
```

#### Phản hồi (Response)

```json
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

_Cập nhật lần cuối vào ngày 15 tháng 5, 2024._

### 3.7. In vận đơn

API in vận đơn.

#### Cấu hình API

**HTTP Request** — `GET v3/bill/print?do_code={bill_code}&partner_id={partner_id}`

| No. | Tham số | Loại | Bắt buộc | Mô tả |
| --- | --- | --- | --- | --- |
| 1 | bill_code | String | **True** | Mã vận đơn |
| 2 | partner_id | Int | **True** | Partner ID, được tạo trên Web portal |

**JSON body response**

| Tên | Loại | Mô tả |
| --- | --- | --- |
| success | boolean | Trả về true nếu việc in đơn thành công, tất cả các giá trị false khác đều có nghĩa là có lỗi. Xem message để biết thêm thông tin chi tiết. |
| message | string | Tin nhắn trả về từ API. |
| data | object | Dữ liệu đơn từ API. |

#### Sample Request

```
https://printdev.ntlogistics.vn/v1/bill/print?do_code=CP98783232&partner_id=88798
https://printdigi.ntlogistics.vn/v1/bill/print?do_code=CP98783232&partner_id=88798
```

_Cập nhật lần cuối vào ngày 7 tháng 7 năm 2023._

### 3.8. Webhook

**Mô tả:** Khi có bất kỳ cập nhật nào cho đơn hàng, hệ thống của Nhất Tín sẽ tự động gửi cập nhật đó đến hệ thống của đối tác thông qua một URL (liên kết callback) mà đối tác đã cung cấp cho Nhất Tín.

#### Cấu hình API

**HTTP Request** — `GET / POST / PUT`

| No. | Tham số | Loại | Bắt buộc | Mô tả |
| --- | --- | --- | --- | --- |
| 1 | bill_no | String | **True** | Mã vận đơn của Nhất Tín |
| 2 | ref_code | String | **True** | Mã tham chiếu của khách hàng - có thể là số hóa đơn hoặc số đặt hàng |
| 3 | status_id | Number | **True** | Tham khảo phần Master Data |
| 4 | status_name | String | **True** | Tham khảo phần Master Data |
| 5 | status_time | int | **True** | Thời điểm thay đổi trạng thái |
| 6 | push_time | int | **True** | Thời điểm đẩy thông tin sang đối tác |
| 7 | shipping_fee | Double | **True** | Phí vận chuyển |
| 8 | is_partial | int | **True** | Đơn hoàn về từ giao hàng 1 phần. 1: đúng, 0: không. |
| 9 | reason | String |  | Lý do nếu có từ Nhất Tín |
| 10 | weight | Double | **True** | Trọng lượng hàng hóa |
| 11 | dimension_weight | Double | **True** | Trọng lượng qui đổi hàng hóa |
| 12 | length | Double | **True** | Chiều dài hàng hóa (cm) |
| 13 | width | Double | **True** | Chiều rộng hàng hóa (cm) |
| 14 | height | Double | **True** | Chiều cao hàng hóa (cm) |
| 15 | expected_at | String | **True** | Ngày giao hàng dự kiến (format: "YYYY-MM-DD HH:mm:ss" - 2024-08-01 12:20:00) |

#### Mẫu yêu cầu (Request example)

```json
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

_Cập nhật lần cuối vào ngày 30 tháng 9, 2024._

---

## 4. Địa điểm (Location)

API liên quan đến địa điểm quận huyện, phường xã, tỉnh thành.

### 4.1. Tỉnh/Thành phố

API danh sách Tỉnh/Thành phố.

#### Cấu hình API

**HTTP Request** — `GET v3/loc/provinces`

| No. | Tham số | Loại | Bắt buộc | Mô tả |
| --- | --- | --- | --- | --- |
| 1 | is_new | Int | **True** | 1: Đơn vị mới, 0: Đơn vị cũ (Mặc định: 0) |

**JSON body response**

| Tên | Loại | Mô tả |
| --- | --- | --- |
| success | boolean | Trả về true nếu gửi yêu cầu thành công, tất cả các giá trị false khác có nghĩa là có lỗi. Xem message để biết thêm chi tiết. |
| message | string | Trả về thông điệp của phản hồi API. |
| data | object | Dữ liệu của phản hồi API. |

#### Mẫu yêu cầu (Request example)

```
https://apisandbox.ntlogistics.vn/v3/loc/provinces
```

#### Phản hồi (Response)

```json
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

_Cập nhật lần cuối vào ngày 7 tháng 7, 2023._

### 4.2. Quận/Huyện

API danh sách Quận/Huyện.

#### Cấu hình API

**HTTP Request** — `GET v3/loc/districts`

| No. | Tham số | Loại | Bắt buộc | Mô tả |
| --- | --- | --- | --- | --- |
| 1 | province_id | Int | **True** | ID tỉnh thành |

**JSON body response**

| Tên | Loại | Mô tả |
| --- | --- | --- |
| success | boolean | Trả về true nếu gửi yêu cầu thành công, tất cả các giá trị false khác có nghĩa là có lỗi. Xem message để biết thêm chi tiết. |
| message | string | Trả về thông điệp của phản hồi API. |
| data | object | Dữ liệu của phản hồi API. |

#### Mẫu yêu cầu (Request example)

```
https://apisandbox.ntlogistics.vn/v3/loc/districts?province_id=99
```

#### Phản hồi (Response)

```json
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

_Cập nhật lần cuối vào ngày 7 tháng 7, 2023._

### 4.3. Phường/Xã

API danh sách Phường/Xã.

#### Cấu hình API

**HTTP Request** — `GET v3/loc/wards`

| No. | Tham số | Loại | Bắt buộc | Mô tả |
| --- | --- | --- | --- | --- |
| 1 | district_id | String | **True** | Mã quận / huyện cũ |
| 2 | is_new | String | **True** | 1: Đơn vị mới, 0: Đơn vị cũ (Mặc định: 0) |
| 3 | province_id | String | **True** | Mã Tỉnh mới |

**JSON body response**

| Tên | Loại | Mô tả |
| --- | --- | --- |
| success | boolean | Trả về true nếu gửi yêu cầu thành công, tất cả các giá trị false khác có nghĩa là có lỗi. Xem message để biết thêm chi tiết. |
| message | string | Trả về thông điệp của phản hồi API. |
| data | object | Dữ liệu của phản hồi API. |

#### Mẫu yêu cầu (Request example)

```
https://apisandbox.ntlogistics.vn/v3/loc/wards?district_id=9904
https://apisandbox.ntlogistics.vn/v3/loc/wards?is_new=1&province_id=01
```

#### Phản hồi (Response)

```json
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

_Cập nhật lần cuối vào ngày 9 tháng 7, 2025._

---

## 5. Phân tích tích hợp & Vận hành đồng bộ (TruePos ↔ Nhất Tín)

Phần này tổng hợp kết quả phân tích Stage 0/1 và **phản hồi chính thức từ Nhất Tín**, dùng để bổ sung và đính chính cho phần tài liệu API gốc ở trên. Nguồn: thư mục `docs/PhanTich-ThaoTac-DongBo/`.

> **Lưu ý ưu tiên:** Một số giá trị mẫu trong tài liệu API gốc chỉ là ví dụ minh họa (ví dụ TTL token `1m`/`2m`). **Phản hồi Nhất Tín trong phần này là căn cứ ưu tiên khi thiết kế/triển khai.** Các mục ghi "(giả định)" hoặc "Chờ xác minh" chưa được chốt bằng bằng chứng live sandbox.

### 5.1. Phản hồi chính thức từ Nhất Tín

Bảng hỏi–đáp đã chốt với Nhất Tín (nguồn: `05-CAU-HOI-GUI-NHATTIN.md`). Đây là điểm bổ sung quan trọng nhất so với tài liệu API gốc vì nó gỡ các điểm mập mờ.

#### Môi trường & credential

| Chủ đề | Phản hồi Nhất Tín |
| --- | --- |
| API host | Sandbox `https://apisandbox.ntlogistics.vn`; Production `https://apiws.ntlogistics.vn` |
| Portal web | Sandbox `https://bodev.ntlogistics.vn`; Production `https://khachhang.ntlogistics.vn` |
| Dữ liệu mẫu sandbox (tạo đơn/tracking/print/webhook) | Có |
| IP whitelist / VPN / domain callback bắt buộc / giới hạn network | Không (giả định) |
| Tài khoản sandbox & portal | _Đã được Nhất Tín cấp — lưu qua biến môi trường (`NHATTIN_USERNAME`/`NHATTIN_PASSWORD`); đã ẩn khỏi tài liệu này vì lý do bảo mật._ |

#### Xác thực & token

| Câu hỏi | Phản hồi Nhất Tín |
| --- | --- |
| JWT Bearer có phải cơ chế duy nhất? Có cần HMAC/timestamp/header khác? | Chỉ cần gửi header `Authorization` với access token ở mọi request. Không HMAC/timestamp. |
| TTL access/refresh token thực tế ở sandbox/production? | **24 giờ** (khác ví dụ `1m`/`2m` trong docs — docs chỉ là mẫu). |
| Refresh token có rotate không? Token cũ còn dùng được sau refresh? | Có rotation — **sau khi refresh, token cũ không dùng được**. |
| Khi token hết hạn/sai/thiếu, HTTP status & body lỗi? | Có thể trả `401`; khi đó cần gọi làm mới (refresh) token rồi retry. |

#### Master Data

| Câu hỏi | Phản hồi Nhất Tín |
| --- | --- |
| Bảng `service_id`, `payment_method_id`, `cargo_type_id`, `status_id` | Xác nhận đúng 4 bảng mã ở [mục 1 (Master Data)](#dữ-liệu-chính-master-data). |
| Bảng `histories[].log_status` (mã event tracking) | **Không có bảng này.** |
| Bảng mã lỗi nghiệp vụ (create/calc/update/cancel/revert) | Chưa có — cần trao đổi thêm với IT của Nhất Tín. |

#### Location / địa danh mới

| Câu hỏi | Phản hồi Nhất Tín |
| --- | --- |
| `id`, `province_code`, `ward_code`, `s_province_id`, `s_ward_id` có cùng hệ mã? | **Có** (cùng hệ mã). CreateBill dùng hậu tố `*_code`, CalcFee dùng `*_id` nhưng cùng giá trị. |
| Sau 01/07/2025, tạo vận đơn có bắt buộc dùng địa danh mới? | Dùng địa danh mới. |
| `/v3/loc/wards` khi `is_new=1` cần `district_id` hay chỉ `province_id`? | Chỉ dùng `province_id`. |

#### Tạo vận đơn & nghiệp vụ bill

| Câu hỏi | Phản hồi Nhất Tín |
| --- | --- |
| Payload tối thiểu hợp lệ để tạo vận đơn sandbox? | Xem payload mẫu ở [mục 3.1 (Yêu cầu)](#31-tạo-vận-đơn). |
| `return_ward_code` có bắt buộc khi `is_return_org=0`? | **Không cần** truyền `return_ward_code` nếu `is_return_org=0`. |
| `partner_id` có bắt buộc trong CreateBill/CalcFee/Update/Print? Lấy từ đâu? | **Bắt buộc**; trường này được tạo trên Web portal. |
| Ma trận trạng thái được phép update/cancel/revert? | Cập nhật: không mô tả trạng thái. Hoàn trả (revert): đã lấy hàng (3), đang vận chuyển (15), không phát được (7). Hủy (cancel): chưa thành công (1) và chờ lấy hàng (2). |
| Bảng mã lỗi nghiệp vụ? | Cần trao đổi thêm với IT của Nhất Tín. |

#### Tracking

| Câu hỏi | Phản hồi Nhất Tín |
| --- | --- |
| `GET /v3/bill/tracking` truyền `bill_code` bằng query string hay JSON body? | **Query string** (dù docs ghi ví dụ dạng JSON body). |
| Có hỗ trợ tra nhiều bill một lần không? | Theo tài liệu chỉ hỗ trợ **1 đơn/lần**. |
| Timestamp trong tracking dùng timezone/format nào? | Không thuộc format hay timezone cụ thể — cần handle tolerant theo từng trường hợp. |

#### Webhook

| Câu hỏi | Phản hồi Nhất Tín |
| --- | --- |
| Cách đăng ký/cấu hình callback URL? | Cấu hình qua Web portal. |
| Method chính thức là POST/GET/PUT? | Do bên POS chọn (khuyến nghị `POST`). |
| Có header ký/signature không? | **Không.** |
| Retry policy khi receiver trả 4xx/5xx/timeout? | Khi nhận thông tin từ Nhất Tín, receiver phản hồi nhận thành công; sau đó POS tự động xử lý nội bộ trong hệ thống của mình. |
| Response ACK cần status/body nào để coi là thành công? | Trả về **HTTP status 200**. |
| Có request id/idempotency key không? | Không — **POS tự xử lý trùng lặp** (dedupe). Gợi ý key tạm: `bill_no` + `status_id` + `status_time` + `push_time` hoặc hash payload. |

#### Print / Label & Asset

| Câu hỏi | Phản hồi Nhất Tín |
| --- | --- |
| Endpoint print chính thức? | `GET v3/bill/print?do_code={bill_code}&partner_id={partner_id}` |
| Query đúng là `do_code` hay `bill_code`? | `do_code`. |
| Print trả PDF/binary, HTML, redirect, link, hay JSON? | **Trả về định dạng HTML.** |
| Link ảnh trong tracking (`p_link_image`, `bill_image_link`, `document_image_link`) có cần auth & TTL? | Không cần auth (giả định). |

#### Rate limit & vận hành

| Câu hỏi | Phản hồi Nhất Tín |
| --- | --- |
| Rate limit/quota theo endpoint/token/IP? | Không (giả định). |
| Khuyến nghị tần suất polling tracking? | Không có khuyến nghị cụ thể (giả định). |
| Có sandbox reset data / cách tạo đơn test ổn định? | Có (giả định). |

### 5.2. Ma trận đồng bộ dữ liệu

Nguồn: `MA-TRAN-DONG-BO-DU-LIEU-NHATTIN.md`. Nguyên tắc: polling tracking là nguồn đối soát chủ động; webhook là kênh cập nhật nhanh nhưng chưa là nguồn duy nhất cho tới khi có spec retry/ACK/idempotency; luôn lưu raw payload cho response/webhook quan trọng; không hardcode Master Data.

#### Ma trận tổng quan theo loại dữ liệu

| Dữ liệu | Nguồn Nhất Tín | Cách đồng bộ đề xuất | Tự động / Thủ công | Tần suất gợi ý | Evidence cần lưu |
| --- | --- | --- | --- | --- | --- |
| Token access/refresh | `POST /v1/auth/sign-in`, `POST /v1/auth/refresh-token` | Token manager refresh trước hạn (TTL 24h), hoặc retry một lần khi 401 | Tự động | Theo TTL 24h | Login/refresh response (masked) |
| Tỉnh/thành | `GET /v3/loc/provinces` | Đồng bộ danh mục theo `is_new` | Tự động + nút refresh thủ công | Hàng ngày hoặc khi Nhất Tín báo đổi | Count, sample item, timestamp |
| Quận/huyện | `GET /v3/loc/districts` | Đồng bộ legacy nếu còn dùng địa danh cũ | Tự động + thủ công | Hàng ngày/tuần | Count theo province |
| Phường/xã | `GET /v3/loc/wards` | Đồng bộ theo tỉnh mới hoặc district cũ | Tự động + thủ công | Hàng ngày hoặc theo tỉnh cần dùng | Count theo province/district |
| Bảng service/payment/cargo/status | Master Data Nhất Tín | Import từ bảng mã Nhất Tín cung cấp; tạm dùng code đã xác nhận | Thủ công trước, tự động nếu có API | Khi có version mới | File nguồn, ngày hiệu lực |
| Báo giá | `POST /v3/bill/calc-fee` | Gọi realtime khi tạo đơn/đổi địa chỉ/dịch vụ | Tự động theo thao tác người dùng | Theo nhu cầu | Request/response raw |
| Vận đơn | `POST /v3/bill/create` | Gửi realtime khi chốt đơn | Tự động | Theo đơn hàng | `bill_code`, `ref_code`, fee snapshot |
| Cập nhật vận đơn | `POST /v3/bill/update-shipping` | Chỉ cho phép khi trạng thái hợp lệ | Tự động theo thao tác người dùng | Theo nhu cầu | Request/response raw |
| Hủy vận đơn | `POST /v3/bill/destroy` | Gửi realtime, xử lý partial result theo từng `bill_code` | Tự động + thủ công | Theo nhu cầu | Result từng bill |
| Chuyển hoàn | `POST /v3/bill/revert-bill` | Gửi realtime khi trạng thái cho phép | Thủ công có kiểm soát | Theo nhu cầu | `success[]`, `failed[]` |
| Tracking | `GET /v3/bill/tracking` | Polling từng `bill_code`; không batch nhiều đơn | Tự động + nút refresh | Sau tạo đơn, rồi định kỳ theo trạng thái | Snapshot, `histories[]`, raw payload, raw timestamp |
| Print label | `GET /v3/bill/print?do_code=&partner_id=` | Gọi khi cần in/preview; response là HTML | Tự động theo thao tác người dùng | Theo nhu cầu | Status, Content-Type, body HTML |
| Webhook trạng thái | Callback cấu hình qua portal | Receiver mặc định `POST`, không signature, ACK 200, lưu raw payload/header, dedupe tạm | Tự động | Theo event Nhất Tín | Header, body, status ACK |

#### Ma trận thao tác tự động / thủ công

| Tình huống | Tự động | Thủ công | Ghi chú |
| --- | --- | --- | --- |
| Token hết hạn | Refresh token, retry request một lần | Re-login bằng credential nếu refresh fail | Cần lock refresh theo account để tránh race (token cũ vô hiệu sau rotation). |
| Địa danh đổi | Job sync định kỳ | Nút refresh danh mục theo tỉnh | Ưu tiên địa danh mới sau 01/07/2025. |
| Người dùng tạo đơn | CalcFee rồi CreateBill | Cho phép nhập lại địa chỉ/dịch vụ nếu lỗi validation | Lưu raw request/response cho audit. |
| Nhất Tín cập nhật trạng thái | Webhook receiver nhận và cập nhật trạng thái | Nút polling tracking theo bill | Webhook không có signature; polling vẫn là đường đối soát chắc. |
| Webhook lỗi xử lý | Lưu failed event, dedupe, retry nội bộ nếu cần | Replay webhook từ admin | POS tự dedupe; ACK 200. |
| Lệch trạng thái TruePos/Nhất Tín | Polling tracking phát hiện lệch | Nút đối soát lại từng bill | Ưu tiên trạng thái từ tracking nếu webhook chậm/mất. |
| In nhãn lỗi | Retry print theo thao tác người dùng | In từ portal Nhất Tín nếu cần | Print trả HTML. |

### 5.3. Luồng triển khai & vai trò endpoint

Nguồn: `01-API-INVENTORY.md`, `02-ENDPOINT-MATRIX.md`. Luồng happy path tối thiểu: **Auth → Location → CalcFee → CreateBill → Print → Tracking → Webhook**; luồng thao tác sau tạo: **Update → Cancel / Revert**.

| # | Nhóm | Endpoint | Bắt buộc / Tùy chọn | Ghi chú tích hợp |
| --- | --- | --- | --- | --- |
| 1 | Auth | `POST /v1/auth/sign-in` | Bắt buộc | Lấy `jwt_token` + `refresh_token`; TTL 24h. |
| 2 | Auth | `POST /v1/auth/refresh-token` | Bắt buộc | Refresh khi 401; token cũ vô hiệu sau rotation. |
| 3 | Location | `GET /v3/loc/provinces` | Bắt buộc | Endpoint nhẹ để verify auth; hỗ trợ `is_new=1`. |
| 4 | Location | `GET /v3/loc/districts` | Tùy chọn (legacy) | Chỉ cần cho địa danh cũ. |
| 5 | Location | `GET /v3/loc/wards` | Bắt buộc | `is_new=1` chỉ cần `province_id`. |
| 6 | Bill | `POST /v3/bill/calc-fee` | Bắt buộc | Trả nhiều option dịch vụ/phí; dùng địa danh mới `*_id`. |
| 7 | Bill | `POST /v3/bill/create` | Bắt buộc | Endpoint lõi; địa danh mới `*_code`; trạng thái ban đầu `2`. |
| 8 | Bill | `POST /v3/bill/update-shipping` | Bắt buộc nếu cho sửa đơn | Guarded — chưa có ma trận trạng thái update. |
| 9 | Bill | `POST /v3/bill/destroy` | Bắt buộc | Hủy trạng thái 1, 2; mảng `bill_code`, xử lý partial. |
| 10 | Bill | `POST /v3/bill/revert-bill` | Tùy chọn | Hoàn trả trạng thái 3, 15, 7; tách `success[]`/`failed[]`. |
| 11 | Print | `GET /v3/bill/print?do_code=&partner_id=` | Bắt buộc nếu in nhãn | Query `do_code`; response HTML. |
| 12 | Tracking | `GET /v3/bill/tracking` | Bắt buộc | Query string, 1 đơn/lần; parser timestamp tolerant. |
| 13 | Webhook | Callback cấu hình qua portal | Bắt buộc cho đồng bộ tự động | POST, không signature, ACK 200, POS tự dedupe. |

### 5.4. Dữ liệu cần lưu tối thiểu khi thiết kế

Nguồn: `MA-TRAN-DONG-BO-DU-LIEU-NHATTIN.md`. Nguyên tắc: lưu raw payload song song với field đã parse để chống lệch schema.

| Nhóm bảng / log | Nội dung |
| --- | --- |
| PartnerCredential / Settings | API host, portal host, username reference, `partner_id`, token metadata; **không lưu password plaintext**. |
| LocationCache | Province/district/ward, `is_new`, raw source item, sync timestamp. |
| MasterData | Service, payment method, cargo type, bill status, source version. |
| Shipment/Bill | `ref_code`, `bill_code`, status, fee snapshot, sender/receiver snapshot, raw create response. |
| TrackingHistory | Tracking snapshot, histories event, poll timestamp. |
| WebhookHistory | Headers, raw body, derived key/hash, processing status, ACK status. |
| AuditLog | User/manual action, API request intent, result, correlation id. |

### 5.5. Trạng thái xác minh (đã xác nhận / còn chờ)

Nguồn: `04-GAP-ANALYSIS.md`, `06-STAGE1-CHECKPOINT-DUYET.md`, `07-PHAM-VI-DESIGN-CO-DIEU-KIEN.md`. Quyết định hiện tại: **duyệt Stage 2 Design có điều kiện** cho các phần đã xác nhận.

**Đã xác nhận (dùng cho design):**

- API host & portal host (sandbox/production).
- Auth header Bearer; 401 → refresh + retry.
- TTL token 24h; refresh có rotation.
- Địa danh mới bắt buộc sau 01/07/2025; `*_code` = `*_id` cùng hệ mã.
- Payload CreateBill tối thiểu; `partner_id` bắt buộc (tạo trên portal).
- `return_ward_code` không cần khi `is_return_org=0`.
- Cancel: trạng thái 1, 2. Revert: 3, 15, 7.
- Tracking: query string, 1 đơn/lần.
- Webhook: portal config, POST, không signature, ACK 200, POS tự dedupe.
- Print: `do_code` + `partner_id`, response HTML.
- Asset links & rate limit: không giới hạn (giả định).

**Còn chờ IT / live evidence:**

- Chạy live smoke test bằng credential sandbox thật.
- Đo/khẳng định hành vi TTL & refresh rotation trên môi trường thật.
- Master Data đầy đủ (service/payment/cargo/status) có version.
- Bảng mã lỗi nghiệp vụ (create/calc/update/cancel/revert).
- Ma trận trạng thái được phép cho Update bill.
- Bảng ý nghĩa `histories[].log_status` (Nhất Tín nói không có).
- Webhook retry/timeout chi tiết (ngoài ACK 200).
- Xác nhận sample response Print (HTML shape thực tế).
- Xác nhận TTL/quyền truy cập link ảnh asset.

_Tổng hợp từ `docs/PhanTich-ThaoTac-DongBo/` (Stage 0/1) · cập nhật theo phản hồi Nhất Tín._

---

_Tài liệu hợp nhất offline phục vụ NotebookLM · Nguồn gốc: https://docs.ntlogistics.vn/docs/vi (API gốc) + phân tích nội bộ `docs/PhanTich-ThaoTac-DongBo` · Nhất Tín Logistics._
