# Evidence — SDK live smoke against NhatTin sandbox (2026-07-07)

Host: `https://apisandbox.ntlogistics.vn`. Account: `thaison` (creds qua env var, không lưu secret vào repo).
Harness: [CodeSDK/samples/NhatTinLogistics.Sdk.LiveSmoke](../../CodeSDK/samples/NhatTinLogistics.Sdk.LiveSmoke/Program.cs) — drive **chính SDK** (`NhatTinLogisticsClient`), KHÔNG phải HTTP thô như [verify-nhattin-live.ps1](../Scripts/verify-nhattin-live.ps1). Token đã mask.

Chạy:
```
$env:NHATTIN_USERNAME="thaison"; $env:NHATTIN_PASSWORD="***"
dotnet run --project CodeSDK/samples/NhatTinLogistics.Sdk.LiveSmoke
```

> Đây là bằng chứng thật của SDK end-to-end. Test project đã pass 40/40 với stub; nhưng stub dùng payload GIẢ ĐỊNH nên bỏ sót 3 lệch shape mà chỉ sandbox thật mới lộ ra.

## Kết quả

| # | SDK call | Endpoint | Lần 1 | Sau fix |
| --- | --- | --- | --- | --- |
| 1 | `Auth.SignInAsync` | `POST /v1/auth/sign-in` | PASS | PASS |
| 2 | `Location.GetProvincesAsync(isNew)` | `GET /v3/loc/provinces?is_new=1` | PASS (34 tỉnh) | PASS |
| 3 | `Location.GetWardsAsync(prov=01,isNew)` | `GET /v3/loc/wards?is_new=1&province_id=01` | PASS (126 phường) | PASS |
| 4 | `Bill.CalcFeeAsync` | `POST /v3/bill/calc-fee` | **FAIL** | PASS (total_fee=41936) |
| 5 | `Bill.CreateAsync` (no partner_id) | `POST /v3/bill/create` | PASS | PASS |
| 6 | `Bill.TrackingAsync` | `GET /v3/bill/tracking` | **FAIL** | PASS (status 2 "Chờ lấy hàng") |
| 7 | `Bill.PrintAsync` | `GET /v3/bill/print` | PASS* | PASS* |
| 8 | `Bill.CancelAsync` | `POST /v3/bill/destroy` | **FAIL** | PASS (succeeded=1) |
| 9 | `Auth.RefreshTokenAsync` | `POST /v1/auth/refresh-token` | PASS (rotated=True) | PASS |

Tổng: lần 1 **6/9**, sau fix **9/9**. (*Print: HTTP 200 + JSON `{success:false, [ERR-00019]}` với bill mới tạo — SDK phân loại content-type đúng; đây là hành vi đã biết của sandbox, không phải bug SDK.)

Bill dùng để test được **tạo rồi cancel sạch** trong cùng một lần chạy (không để rác).

## 3 bug SDK do live smoke phát hiện (stub bỏ sót) + fix

### Bug 1 — CalcFee: `service_id` có thể null
Raw thật:
```json
{"success":true,"data":[{"weight":2,"total_fee":41936,"main_fee":41936,...,"service_id":null,"lead_time":"2026-01-01 17:55:00"}],"message":"Calculate Successfull"}
```
`FeeOption.ServiceId` khai báo `int` → ném `JsonException: could not be converted to System.Int32. Path: $.data[0].service_id`.
**Fix:** `FeeOption.ServiceId` → `int?`.

### Bug 2 — Tracking: field số về dạng RAW NUMBER (không phải string), có field null
Raw thật (rút gọn):
```json
{"data":[{"weight":"2","cod_amt":0,"main_fee":41936,"total_fee":41936,"lifting_fee":null,"bill_status_id":2,...}]}
```
Sandbox KHÔNG nhất quán: `weight` là string `"2"` nhưng `cod_amt`/`main_fee` là số thô, `lifting_fee` là null. `TrackingResult.*` khai báo `string?` → ném `JsonException: could not be converted to System.String. Path: $.data[0].cod_amt`.
**Fix:** thêm `TolerantStringConverter` (đọc string / raw number / bool / null → `string?`, giữ nguyên text số) và đăng ký GLOBAL trong `NhatTinJson.Options`. Làm cứng mọi string field trước sự thất thường của sandbox.

### Bug 3 — Cancel/destroy: `data` là OBJECT `{success,failed}`, không phải mảng
Raw thật:
```json
{"success":true,"data":{"success":[{"doCode":"CP252694164","message":"Bill CP252694164 has canceled successful."}],"failed":[]},"message":"Bill canceled successfully"}
```
`CancelAsync` khai báo trả `List<CancelResult>` → ném `JsonException: could not be converted to List<CancelResult>. Path: $.data`. (Shape này GIỐNG revert `{success,failed}` — SDK đã đúng cho revert nhưng sai cho cancel.)
**Fix:** thêm `CancelResponse { List<CancelResult> Succeeded; List<CancelResult> Failed; }`; `CancelAsync` trả `NhatTinResponse<CancelResponse>` (breaking).

## Regression net
2 unit test mới mã hoá payload thật (`CalcFeeAsync_tolerates_null_service_id`, `TrackingAsync_tolerates_raw_numbers_and_null_fee_fields`) + rewrite `CancelAsync_maps_success_and_failed_object`. Toàn bộ suite: **42/42 pass**.
