# Phan tich xac thuc Nhat Tin Logistics

Tai lieu nay tong hop co che xac thuc Nhat Tin Logistics tu docs scrape va bien checklist khoi tao doi tac moi thanh bang verify rieng cho Nhat Tin.

## Ket luan nhanh

- Co che xac thuc trong docs scrape la **JWT Bearer**, khong phai HMAC Tingee.
- Moi request can xac thuc gui header `Authorization: Bearer <access_token>`.
- Login lay token: `POST /v1/auth/sign-in`.
- Refresh token: `POST /v1/auth/refresh-token`.
- Sandbox host: `https://apisandbox.ntlogistics.vn`.
- Endpoint verify nhe: `GET /v3/loc/provinces`.
- Webhook auth/signature: **khong co signature theo phan hoi Nhat Tin**; ACK/retry/timeout van can xac minh.

## Nguon doc scrape da doi chieu

| File | Noi dung dung de ket luan |
| --- | --- |
| `NhatTinAPIDocumentation/vi/00-thong-tin-ket-noi.md` | Host sandbox/production, header `authorization: Bearer <access_token>`, danh sach API co Login va Webhook. |
| `NhatTinAPIDocumentation/vi/authentication.md` | JWT sign-in, response token, Bearer header, refresh-token, loi auth thuong gap. |
| `NhatTinAPIDocumentation/vi/location/provinces.md` | Endpoint `GET v3/loc/provinces`, response envelope `success`, `message`, `data`, request mau sandbox. |
| `Handoff-TichHopDoiTac/04-CHECKLIST-DOI-TAC-MOI.md` | Mau checklist xac minh co che auth that cua doi tac. |
| `PhanTich-ThaoTac-DongBo/05-CAU-HOI-GUI-NHATTIN.md` | Phan hoi Nhat Tin ve auth header, webhook signature, base URL va cach xu ly `401`. |

## Moi truong va endpoint auth

| Muc | Gia tri |
| --- | --- |
| Sandbox API host | `https://apisandbox.ntlogistics.vn` |
| Production API host | `https://apiws.ntlogistics.vn` |
| Sandbox portal | `https://bodev.ntlogistics.vn` |
| Production portal | `https://khachhang.ntlogistics.vn` |
| Login endpoint | `POST /v1/auth/sign-in` |
| Refresh endpoint | `POST /v1/auth/refresh-token` |
| Endpoint verify tinh/thanh | `GET /v3/loc/provinces` |

### Login request

```http
POST /v1/auth/sign-in
Content-Type: application/json

{
  "username": "your_account",
  "password": "your_password"
}
```

### Login response envelope

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

### Refresh request

```http
POST /v1/auth/refresh-token
Content-Type: application/json

{
  "refresh_token": "<refresh_token>"
}
```

### Refresh response envelope

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

## Bang checklist xac thuc cho Nhat Tin

| Muc | Tingee (tham chieu) | Nhat Tin Logistics |
| --- | --- | --- |
| Header auth | `x-client-id`, `x-request-timestamp`, `x-signature` | `Authorization: Bearer <access_token>` theo docs JWT. Header trong trang ket noi ghi `authorization: Bearer <access_token>`. |
| Cong thuc ky | `HMAC_SHA512(ts + ":" + body, secret)`, hex lowercase | Khong dung HMAC cho API client theo docs scrape. Xac thuc bang JWT Bearer. |
| Format timestamp / timezone | `yyyyMMddHHmmssSSS`, UTC+7, +/-10 phut | Khong thay yeu cau timestamp auth trong docs JWT. |
| Response envelope | `{code, message, data}`, `"00"` = success | Envelope dang `{success, message, data}`. Login/refresh tra `success: true` va `data.jwt_token`; provinces tra `success: true` va `data` la mang tinh/thanh. |
| Ma loi auth | `90 / 91 / 97 / 1042` | Docs authentication neu HTTP `401` cho thieu Authorization, sai dinh dang Bearer, token het han, token khong hop le; HTTP `400` cho thieu `refresh_token`. Chua thay bang ma loi rieng nhu Tingee. |
| Webhook: header + cach ky | `x-request-id`, `x-request-timestamp`, `x-signature` cung cong thuc | Nhat Tin phan hoi webhook khong co signature/header ky. |
| Webhook: retry policy doi tac | - | Chua co policy retry/timeout/ACK; can trao doi them voi IT Nhat Tin. |

## Checklist verify sandbox that

| Hang muc | Cach verify |
| --- | --- |
| Credential sandbox/UAT | Lay tu Nhat Tin, dat vao env vars `NHATTIN_USERNAME`, `NHATTIN_PASSWORD`; khong ghi vao file. |
| Base URL sandbox | Mac dinh script dung `https://apisandbox.ntlogistics.vn`; co the override bang `NHATTIN_BASE_URL`. |
| Login JWT | Goi `POST /v1/auth/sign-in`, kiem tra HTTP 2xx, `success = true`, `data.jwt_token` co gia tri, `data.token_type = Bearer` neu API tra ve. |
| Goi API co Bearer | Goi `GET /v3/loc/provinces` voi `Authorization: Bearer <masked runtime token>`, kiem tra `success = true` va `data` co du lieu. |
| Refresh token | Neu login tra `data.refresh_token`, goi `POST /v1/auth/refresh-token`, kiem tra `data.jwt_token` moi. |
| Negative auth | Goi `GET /v3/loc/provinces` khong co Bearer hoac voi Bearer gia, ky vong HTTP `401` hoac envelope loi tu Nhat Tin. |
| Envelope response | Ghi lai shape thuc te: cac field `success`, `message`, `data`; neu production/sandbox khac docs thi cap nhat mapping sau. |

## Ghi chu tich hop

- Khong copy `HmacAuthMiddleware`/cong thuc Tingee cho client API Nhat Tin neu muc tieu la goi API Nhat Tin that; docs hien tai chi xac nhan JWT Bearer.
- Webhook Nhat Tin da phan hoi khong co signature, vi vay receiver can luu raw header/body, dedupe noi bo va co the bo sung network control neu IT xac nhan duoc.
- TTL token da duoc **live verify 2026-07-07** (evidence: [../../Tests/Results/EVIDENCE-NHATTIN-LIVE-2026-07-07.md](../../Tests/Results/EVIDENCE-NHATTIN-LIVE-2026-07-07.md)): `token_expires_in = "24h"`, `refresh_expires_in = "7d"` (khong phai "1m"/"2m" nhu docs mau, cung khong phai 1-2 thang). Login response con tra san `data.partner_id`.
- Luu y refresh rotation: phan hoi giay noi token cu bi vo hieu, nhung live sandbox cho thay **refresh token cu VAN dung duoc** (JWT stateless 7d). Client giu co che tolerant; can hoi lai NhatTin hanh vi production.
