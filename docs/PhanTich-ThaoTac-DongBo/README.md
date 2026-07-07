# Phân Tích - Thao Tác - Đồng Bộ Nhất Tín

Thư mục này gom toàn bộ deliverable Stage 0/1 trước khi sang Design/Implementation sandbox Nhất Tín Logistics.

## Bộ Tài Liệu Hiện Có

| Thứ tự | File | Vai trò | Trạng thái |
| --- | --- | --- | --- |
| 00 | [00-PHAN-TICH-XAC-THUC-NHATTIN.md](00-PHAN-TICH-XAC-THUC-NHATTIN.md) | Xác định auth thật, host, login/refresh, response envelope | Chờ credential để crosscheck live |
| 01 | [01-API-INVENTORY.md](01-API-INVENTORY.md) | Inventory 13 endpoint/nhóm callback tìm thấy trong docs scrape | Đã lập từ docs offline |
| 02 | [02-ENDPOINT-MATRIX.md](02-ENDPOINT-MATRIX.md) | Matrix endpoint theo luồng TruePos/Sandbox | Đã lập từ docs offline |
| 03 | [03-SCHEMA-MAPPING.md](03-SCHEMA-MAPPING.md) | Mapping schema Nhất Tín sang khái niệm TruePos/Sandbox | Đã lập, còn nhiều điểm cần xác minh |
| 04 | [04-GAP-ANALYSIS.md](04-GAP-ANALYSIS.md) | Blocker/rủi ro trước Design/Code | Đã lập, dùng làm danh sách chặn |
| 05 | [05-CAU-HOI-GUI-NHATTIN.md](05-CAU-HOI-GUI-NHATTIN.md) | Bộ câu hỏi gửi Nhất Tín để gỡ blocker | Đã có phản hồi từ Nhất Tín |
| 06 | [06-STAGE1-CHECKPOINT-DUYET.md](06-STAGE1-CHECKPOINT-DUYET.md) | Gói checkpoint duyệt Stage 1 | Duyệt Design có điều kiện |
| 07 | [07-PHAM-VI-DESIGN-CO-DIEU-KIEN.md](07-PHAM-VI-DESIGN-CO-DIEU-KIEN.md) | Phạm vi được phép xây dựng trước từ các điểm đã xác nhận | Đã lập từ phản hồi Nhất Tín |
| Ops | [MA-TRAN-DONG-BO-DU-LIEU-NHATTIN.md](MA-TRAN-DONG-BO-DU-LIEU-NHATTIN.md) | Ma trận đồng bộ tự động/thủ công ban đầu | Draft theo docs, cần cập nhật sau live test |

## Kết Luận Hiện Tại

- Có thể khẳng định từ docs scrape: Nhất Tín dùng JWT Bearer cho client API.
- Từ phản hồi Nhất Tín trong [05-CAU-HOI-GUI-NHATTIN.md](05-CAU-HOI-GUI-NHATTIN.md), có thể bắt đầu Stage 2 Design có điều kiện cho các phần đã xác nhận: base URL, auth header, địa danh mới, payload CreateBill tối thiểu, `partner_id`, tracking 1 đơn, webhook không signature, print dùng `do_code`.
- Chưa đủ căn cứ để code sandbox chuẩn sản xuất đầy đủ vì vẫn còn các điểm chờ IT/live evidence: credential, token lifecycle, Master Data chi tiết, tracking request shape, webhook ACK/retry, print response format, rate limit.
- Việc tiếp theo khi có credential là chạy [Tests/Scripts/verify-nhattin-auth.ps1](../Tests/Scripts/verify-nhattin-auth.ps1) và điền evidence theo [Tests/Results/EVIDENCE-NHATTIN-SANDBOX-TEMPLATE.md](../Tests/Results/EVIDENCE-NHATTIN-SANDBOX-TEMPLATE.md).

## Quy Tắc Cập Nhật

1. Mọi thông tin từ API thật phải ghi lại evidence trong `Tests/Results/`.
2. Nếu Nhất Tín cung cấp tài liệu mới, tải về `NhatTinAPIDocumentation/{version}/` rồi cập nhật changelog.
3. Không xóa các marker `Cần xác minh` cho tới khi có bằng chứng từ docs chính thức hoặc live sandbox.
4. Chỉ chuyển sang Stage 2 Design khi file checkpoint được duyệt.
