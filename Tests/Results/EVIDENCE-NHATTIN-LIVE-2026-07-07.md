# Evidence — Live verify NhatTin sandbox (2026-07-07)

Host: `https://apisandbox.ntlogistics.vn`. Account: `thaison` (env var, không lưu secret vào repo).
Script: [Tests/Scripts/verify-nhattin-live.ps1](../Scripts/verify-nhattin-live.ps1). Token đã mask.

> Đây là bằng chứng thật, ưu tiên hơn phản hồi trên giấy khi có mâu thuẫn.

## 1. Auth / token — `POST /v1/auth/sign-in`

- HTTP 200, `content-type: application/json`.
- `data` keys: `jwt_token, partner_id, refresh_expires_in, refresh_token, token_expires_in, token_type, username`.
- **`token_expires_in = "24h"`** (chuỗi, không phải giây). Khớp Q-AUTH-02.
- **`refresh_expires_in = "7d"`** (7 ngày) — thông tin MỚI, trước đây chưa có.
- **`data.partner_id` có sẵn trong login response** — decode JWT: `partner_id = 124823`. ⇒ partner_id gắn với tài khoản, lấy khi login.
- `token_type = "Bearer"`, access field tên `jwt_token`.

## 2. Refresh rotation — `POST /v1/auth/refresh-token` (MÂU THUẪN với Q-AUTH-03)

- refresh#1 (token gốc): 200, trả `jwt_token` mới + `refresh_token` mới (`rotated = True`), `token_expires_in = "24h"`.
- refresh#2 **tái sử dụng refresh token CŨ**: **vẫn 200, `message: "Token refreshed successfully"`** → **token cũ KHÔNG bị vô hiệu**.
- Kết luận: refresh token là **JWT stateless (7d)**, cấp mới nhưng **không revoke token cũ**. Trái với phản hồi giấy Q-AUTH-03 ("token cũ không dùng được").

## 3. Location — dual identifier + xác nhận hệ mã

- `GET /v3/loc/provinces?is_new=1` → 200, **34 tỉnh** (đơn vị mới). Mỗi tỉnh có `id="01"` **và** `value=1059764` (2 định danh).
- `GET /v3/loc/wards?is_new=1&province_id=01` → 200, **126 phường**. Ward có `id="00004"`, `ward_code="00004"`, `value="1013922"`, `province_value="01"`, `province_id="1059764"`, kèm `district_old`/`city_old`.
- Wards `is_new=1` chỉ cần `province_id` (dùng "01" — dạng short code). Khớp Q-LOC-03.

## 4. CalcFee — chốt `*_id` dùng short code (xác nhận Q-LOC-01)

`POST /v3/bill/calc-fee` (kèm `partner_id`):
- `s_province_id="01"`, `s_ward_id="00004"` (SHORT) → **success, total_fee=41936** ✅ ĐÚNG.
- `s_province_id="1059764"`, `s_ward_id="1013922"` (VALUE) → success nhưng **total_fee=0, lead_time="NULL_DATE"** ❌ SAI.
- ⇒ CalcFee `*_province_id`/`*_ward_id` = **cùng giá trị short** với create `*_province_code`/`*_ward_code`. SDK model 1-`id` là ĐÚNG. Field `value` không dùng.
- `lead_time` format `"yyyy-MM-dd HH:mm:ss"` (docs ghi `dd/MM/yyyy HH:mm` — lệch).

## 5. CreateBill — `POST /v3/bill/create` (minimum payload, KHÔNG partner_id)

- **Thành công** → `bill_code=CP252690012`. ⇒ **create KHÔNG cần partner_id trong body** (lấy từ token). Gỡ mâu thuẫn Q-BILL-03. SDK bỏ partner_id ở create là đúng.
- Response `status_id = 1` ("Chưa thành công") — nhưng **tracking ngay sau đó trả `bill_status_id = 2`** ("Chờ lấy hàng"). Lệch giữa create-response và tracking.
- `bill_id = 0` (luôn 0 — không ổn định). `payment_method` (không phải `payment_method_id`) trong response.
- **Nhiều format timestamp trong 1 response**: `created_at="2026-07-07 10:54:53"` (yyyy-MM-dd HH:mm:ss) vs `expected_at="2026-07-07T03:54:00.000Z"` (ISO8601 UTC). ⇒ parser phải tolerant.
- `receiver_phone` bị mask `**********`.

## 6. Print — `GET /v3/bill/print?do_code=...&partner_id=124823` (LẬT LẠI Q-PRN-03)

- Với partner_id ĐÚNG, bill vừa tạo: **HTTP 200, `content-type: application/json`, body**:
  `{"success":false,"data":[],"message":"[ERR-00019]Unknow error. Please contact admin"}`
- ⇒ Print **KHÔNG trả PDF/binary** (SDK `byte[]` sai) và **chưa xác nhận được HTML**. Thực tế: **JSON envelope `{success,message,data}`**, dùng pattern **HTTP 200 + `success:false`** cho lỗi.
- Bill mới tạo (status 1/2) không in được → cần hỏi NhatTin: điều kiện để print thành công (bill phải đã lấy hàng?), và khi thành công content-type là gì (HTML?).

## 7. Error envelope

- **Có mã lỗi nghiệp vụ**: `[ERR-00019]Unknow error` → NhatTin CÓ hệ error code `[ERR-xxxxx]` (gỡ một phần Q-BILL-05).
- Bad Bearer → `GET /v3/loc/provinces` trả **HTTP 401, content-type json, body RỖNG** (không có envelope). SDK/sandbox xử lý 401 (refresh/retry) khớp.

## 8. Tracking — `GET /v3/bill/tracking?bill_code=...`

- 200, `data` là mảng 1 phần tử. Payload thật RẤT giàu: `bill_status_id`(int), `bill_status_desc`, `pay_method`, `service`, đầy đủ breakdown phí, sender/receiver. Nhiều field số ở dạng **string** ("2","0"). Sandbox hiện tại trả payload rất mỏng → nên làm giàu.

---

## Bảng chốt (evidence thắng giấy)

| Chủ đề | Giấy (file 05) | Live thật | Hành động |
| --- | --- | --- | --- |
| Access TTL | 24h | `"24h"` ✅ | Sandbox set 24h |
| Refresh TTL | (không nêu) | `"7d"` | Sandbox set 7d |
| Refresh rotation | token cũ vô hiệu | **token cũ vẫn dùng được** | SDK giữ tolerant; hỏi lại NhatTin; sandbox đang strict hơn thật |
| partner_id create | bắt buộc | **create không cần** (lấy từ token) | SDK đúng; auto-capture partner_id từ login |
| CalcFee `*_id` | cùng hệ code | **= short code** ✅ | SDK đúng, không đổi |
| Print format | HTML | **JSON envelope (lỗi), 200+success:false** | SDK content-type-aware; hỏi lại điều kiện HTML |
| Error code | cần hỏi IT | **có `[ERR-xxxxx]`** | Bắt đầu ErrorCodes catalog |
