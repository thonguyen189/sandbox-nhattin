---
title: "Xác thực JWT"
source: "https://docs.ntlogistics.vn/docs/vi/authentication"
---
# Xác thực JWT

Cách đăng nhập, gọi API bằng JWT và làm mới token

## Bạn cần làm gì

Thực hiện các bước sau để xác thực và gọi API.

### 1) Đăng nhập để lấy token

Nhất Tín cung cấp bộ tài khoản tương ứng với từng môi trường.

Yêu cầu đăng nhập

```
POST /v1/auth/sign-in
Content-Type: application/json

{
  "username": "your_account",
  "password": "your_password"
}
```

Phản hồi thành công:

Phản hồi đăng nhập thành công

```
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

### 2) Gọi API bằng access token

Gửi header `Authorization` với access token ở mọi request:

Yêu cầu có xác thực

```
GET /v3/your-endpoint
Headers:
{
  "Authorization": "Bearer <access_token>"
}
```

Nếu nhận 401 do token hết hạn, hãy làm mới token (bước tiếp theo) rồi gọi lại.

### 3) Làm mới access token

Yêu cầu làm mới token

```
POST /v1/auth/refresh-token
Content-Type: application/json

{
  "refresh_token": "<refresh_token>"
}
```

Phản hồi thành công:

Phản hồi làm mới token

```
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

## Lỗi thường gặp

| Lỗi | Trạng thái | Khi nào xảy ra | Cách khắc phục |
| --- | --- | --- | --- |
| Yêu cầu xác thực | 401 | Thiếu header Authorization | Thêm Authorization: Bearer <access_token> |
| Sai định dạng header | 401 | Header không đúng định dạng Bearer <token> | Sửa đúng định dạng |
| Token hết hạn | 401 | Access token hết hạn | Làm mới token, sau đó gọi lại |
| Xác thực thất bại | 401 | Token không hợp lệ | Đăng nhập lại để lấy token mới |
| Thiếu refresh token | 400 | Thiếu refresh_token trong yêu cầu làm mới | Bổ sung refresh_token trong body |

>

Cập nhật vào 16/09/2025 bởi Nhất Tín Logistics.

Thông tin kết nối

Vận đơn

API liên quan đến vận đơn
