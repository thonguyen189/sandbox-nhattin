# Đánh Giá Nâng Cấp Sandbox / Webhook / SDK Sau Phản Hồi NhatTin

Nguồn: phản hồi NhatTin trong [05-CAU-HOI-GUI-NHATTIN.md](05-CAU-HOI-GUI-NHATTIN.md) + **live verify thật** ngày 2026-07-07 (evidence: [../../Tests/Results/EVIDENCE-NHATTIN-LIVE-2026-07-07.md](../../Tests/Results/EVIDENCE-NHATTIN-LIVE-2026-07-07.md)).

Đối chiếu 3 hệ thống: SDK (`CodeSDK`), Sandbox (`CodeSandBox`), Webhook Receiver (`CodeWebHooks`).

## Kết luận tổng

Phần lớn phản hồi **hợp thức hóa thiết kế hiện tại** — đặc biệt gỡ rủi ro lớn nhất GAP-18 (location). Cần nâng cấp có mục tiêu:

- **2 mismatch phải sửa (P0):** Print response, Token TTL.
- **Vài bổ sung nên làm (P1):** webhook dedupe/ACK, sandbox auto-emit webhook, SDK auto partner_id.
- **Đã có credential thật** → live verify chạy được, đã gỡ nhiều gap "chờ evidence" ngay trong đợt này.
- **Một số phản hồi giấy bị live evidence lật lại** (refresh rotation, print HTML, partner_id create) — ghi rõ bên dưới.

## Đợt live verify đã chốt/lật những gì

| Chủ đề | Phản hồi giấy | Live thật (2026-07-07) | Hệ quả |
| --- | --- | --- | --- |
| Access TTL | 24h | `token_expires_in="24h"` ✅ | Sandbox chỉnh 24h |
| Refresh TTL | — | `refresh_expires_in="7d"` | Sandbox chỉnh 7d |
| Refresh rotation | token cũ vô hiệu | **token cũ VẪN dùng được** (JWT stateless 7d) | SDK tolerant (ok); sandbox đang strict hơn thật; hỏi lại NhatTin |
| partner_id trong create | bắt buộc | **create không cần** (lấy từ token); login trả sẵn `partner_id=124823` | SDK đúng; nên auto-capture partner_id từ login |
| CalcFee `*_id` vs create `*_code` | cùng hệ mã | **cùng short code** ("01"/"00004") ✅ | SDK model 1-`id` đúng, GAP-18 gỡ |
| Print format | HTML | **JSON envelope, 200 + `success:false`, có `[ERR-xxxxx]`** | SDK `byte[]` sai → content-type-aware |
| Error code | cần hỏi IT | **có mã `[ERR-00019]`** | Bắt đầu ErrorCodes catalog |

## Bảng ưu tiên nâng cấp

### P0 — Phải sửa (mismatch thật)

| ID | Vấn đề | Hệ thống | Bằng chứng | Hành động |
| --- | --- | --- | --- | --- |
| P0-1 | Print thực tế trả **JSON envelope** (200 + `success:false`, có mã lỗi), **không** phải PDF/binary; success có thể là HTML (chưa verify được) | SDK + Sandbox | Live §6; SDK `Client/BillApi.cs` `PrintAsync`→`byte[]`; Sandbox `BillController.Print`→JSON link | SDK: `PrintAsync` **content-type-aware** — parse envelope, phát hiện `success:false`, hỗ trợ HTML; giữ `GetPrintUrl`. Sandbox: trả envelope sát thật (JSON lỗi cho bill chưa in được; HTML/`data` khi in được) |
| P0-2 | Token TTL sandbox = 900s/3600s, emit `"900s"/"3600s"` | Sandbox | Live §1 (24h/7d); `Infrastructure/Auth/JwtOptions.cs` | Access 86400s, refresh 604800s; emit format `"24h"/"7d"`. Cập nhật [00-PHAN-TICH-XAC-THUC-NHATTIN.md](00-PHAN-TICH-XAC-THUC-NHATTIN.md) (bỏ "1-2 tháng"/"1m/2m") |

### P1 — Nên sửa (robustness / độ phủ test)

| ID | Vấn đề | Hệ thống | Hành động |
| --- | --- | --- | --- |
| P1-1 | Receiver **không dedupe**; `status_time`/`push_time` chỉ trong raw blob | Webhook | Persist `status_time`/`push_time` thành cột; dedupe theo (`bill_no`+`status_id`+`status_time`) hoặc hash; unique index |
| P1-2 | ACK sau khi ghi DB đồng bộ; DB lỗi → 500 (NhatTin retry) | Webhook | ACK 200 sớm rồi xử lý; tối thiểu try/catch quanh SaveChanges |
| P1-3 | Sandbox **không tự bắn webhook** ở luồng public (chỉ qua simulate-status) | Sandbox | Emit webhook khi cancel/revert đổi trạng thái |
| P1-4 | SDK bắt cấu hình `PartnerId` thủ công, dù login đã trả `partner_id` | SDK | Auto-capture `partner_id` từ login response → dùng cho calc-fee/print/update |

### P2 — Verify / hoàn thiện

| ID | Việc | Ghi chú |
| --- | --- | --- |
| P2-1 | Hỏi NhatTin: điều kiện Print thành công + content-type khi thành công (HTML?) | Bill mới tạo trả `[ERR-00019]` |
| P2-2 | Hỏi NhatTin: refresh token cũ có thật sự bị vô hiệu ở production không | Live sandbox cho thấy KHÔNG |
| P2-3 | Xin bảng master data đầy đủ (ô Q-MD trong file 05 bị cắt) + catalog `[ERR-xxxxx]` | Seed hiện thiếu status 5/8/14 |
| P2-4 | Làm giàu tracking payload sandbox (fees, service, status_desc, sender/receiver) | Sát response thật §8 |
| P2-5 | Sandbox: create response `status_id=1` nhưng tracking `=2` | Hiện sandbox set 2 |

## Điểm đã đúng — không đổi

- Location `id`/`code`/`*_id` cùng giá trị short → SDK đúng (GAP-18 gỡ).
- SDK 401→refresh→retry-once + khóa chống race.
- do_code cho print; tracking GET query-string 1 đơn; webhook không signature; ACK 200; method linh hoạt.
- Sandbox cancel {1,2}/revert {3,15,7}; SDK để server enforce.

## Còn treo (chờ NhatTin)

- Điều kiện & content-type Print thành công (P2-1).
- Hành vi refresh rotation ở production (P2-2).
- Master data đầy đủ + error code catalog (P2-3).
- Retry/timeout webhook cụ thể (số giây/số lần).

## Lưu ý bảo mật

[05-CAU-HOI-GUI-NHATTIN.md](05-CAU-HOI-GUI-NHATTIN.md) chứa mật khẩu sandbox + portal plaintext; `appsettings.Local.json` (sandbox + webhook) có mật khẩu DB thật trong cây source. Đề xuất đưa secret ra env var/secret store, mask trước khi commit, kiểm tra `.gitignore`.
