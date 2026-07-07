# Thiết kế: Chuẩn hóa kết nối SQL Server (bỏ hoàn toàn SQLite)

- **Ngày:** 2026-07-07
- **Trạng thái:** Đã duyệt, đang triển khai
- **Phạm vi:** `CodeSandBox`, `CodeWebHooks` (không đụng `CodeSDK`)

## Bối cảnh

Sandbox ban đầu dùng SQLite vì SQL Server đích `192.168.200.8` không ổn định (xem `2026-07-06-nhattin-sandbox-webhook-design.md`). Server nay đã hoạt động ổn định, nên chuẩn hóa cả hai solution sang SQL Server và **gỡ bỏ hoàn toàn** SQLite (package, `UseSqlite`, migration SQLite, và SQLite trong test).

Kiến trúc Clean Architecture đã tách provider: tầng Domain/Application không phụ thuộc provider, nên thay đổi gói gọn trong tầng Infrastructure + điểm khởi động (composition root) + migrations + test.

## Quyết định (đã chốt với người dùng)

1. **Bỏ hoàn toàn SQLite**, dùng SQL Server (không giữ chế độ provider theo cấu hình).
2. **DB riêng cho mỗi solution** trên `192.168.200.8`:
   - `CodeSandBox` → DB **`NhatTinSandbox`**
   - `CodeWebHooks` → DB **`NhatTinWebhooks`**
   - Không ghi vào DB nghiệp vụ thật (`VPOSDB_114`) để tránh xung đột schema.
3. **Chuyển cả hai** solution trong lần này.
4. **Sửa cột tiền tệ** từ `float` (do `HasConversion<double>()`) sang `decimal(18,2)`.

## Thay đổi chi tiết

### 1. Provider (runtime)
- `Api/Extensions/InfrastructureRegistration.cs`, `AdminPortal/Program.cs`, `WebHooks/Program.cs`: `UseSqlite` → `UseSqlServer`.
- Bỏ logic đường dẫn file SQLite (`SqliteConnectionStringBuilder`) và tạo thư mục `App_Data` — không còn file DB.
- `WebHooks/Program.cs`: bọc lời gọi `Database.Migrate()` bằng `if (db.Database.IsRelational()) Migrate(); else EnsureCreated();` để host test dùng InMemory không ném lỗi (InMemory không hỗ trợ Migrate).
- Hai `IDesignTimeDbContextFactory` → `UseSqlServer` với chuỗi kết nối placeholder (chỉ dùng để sinh migration, không mở kết nối).

### 2. Connection string & bí mật
Chuẩn dạng `Microsoft.Data.SqlClient`:
```
Server=192.168.200.8;Database=<DB>;User Id=vipos;Password=<pwd>;TrustServerCertificate=True;Encrypt=False
```
- `appsettings.json` (commit): giữ chuỗi với `Password=CHANGE_ME` làm khung tài liệu, không chứa bí mật thật.
- Mật khẩu thật: nạp qua `appsettings.Local.json` (thêm vào `.gitignore`) — mỗi điểm khởi động thêm `builder.Configuration.AddJsonFile("appsettings.Local.json", optional: true, reloadOnChange: true)`. Nguồn credential là file gitignored `sql-Server.txt`.

Ba điểm sai của `sql-Server.txt` được sửa khi chuẩn hóa: thiếu `Database=`, `User=` → `User Id=`, thiếu `TrustServerCertificate=True` (SqlClient EF Core 8 mặc định `Encrypt=True`, gây lỗi chứng chỉ với cert self-signed).

### 3. Kiểu tiền tệ
Trong `SandboxDbContext.OnModelCreating`: thay `.HasConversion<double>()` bằng `.HasPrecision(18, 2)` cho `CodAmount`, `MainFee`, `TotalFee`, `OtherFee` (Bill) và `ShippingFee` (BillStatusHistory). `CargoValue` là `double` thật → giữ nguyên.

### 4. Migrations
Xóa toàn bộ `Persistence/Migrations/` ở cả hai solution, tạo lại `InitialCreate` bằng provider SQL Server (`dotnet ef migrations add InitialCreate`). Kết quả: cột `nvarchar/int/bit/float/decimal(18,2)/datetimeoffset`, PK `IDENTITY`. Lệnh này không kết nối DB.

### 5. Test (bỏ SQLite)
- 6 test class của CodeSandBox: thay `SqliteConnection(:memory:)` + `UseSqlite` bằng `UseInMemoryDatabase`. Giữ nguyên ngữ nghĩa từng test (test nào mở context thứ hai để kiểm tra dữ liệu đã lưu phải dùng chung một tên InMemory DB).
- Integration test CodeWebHooks (`WebApplicationFactory<Program>`): tạo `WebhookApiFactory` override đăng ký `WebhookDbContext` sang InMemory để không nối SQL Server thật.

### 6. NuGet
- Thêm `Microsoft.EntityFrameworkCore.SqlServer` 8.0.10 (2 project runtime).
- Thêm `Microsoft.EntityFrameworkCore.InMemory` 8.0.10 (2 project test).
- Gỡ `Microsoft.EntityFrameworkCore.Sqlite` (4 project).

## Kiểm chứng
1. Build + test cả hai solution (26 + 3 test) — nền InMemory, không cần SQL Server.
2. Tạo `appsettings.Local.json` (gitignored) với credential thật, chạy API thật: xác nhận `Migrate()` tạo được schema trên `NhatTinSandbox`/`NhatTinWebhooks` và luồng tạo bill / nhận webhook end-to-end.

## Rủi ro
- Login `vipos` cần quyền `dbcreator` để `Migrate()` tự tạo database; nếu không, phải tạo sẵn `NhatTinSandbox`/`NhatTinWebhooks` thủ công.
- Collation SQL Server thường không phân biệt hoa/thường (khác SQLite `BINARY`) — kiểm thử lại tra cứu `BillCode`/`Username`. Test chạy trên InMemory nên không bắt được khác biệt này; cần smoke test trên SQL Server thật.
