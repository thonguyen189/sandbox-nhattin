<#
.SYNOPSIS
    Khởi chạy hệ demo NhatTin: Sandbox API (:5080), AdminPortal (:5090) và MVC demo (:5110),
    mỗi tiến trình trong một cửa sổ riêng. Kèm -WithReceiver để chạy thêm webhook receiver (:5099).

.DESCRIPTION
    Mỗi service mở trong một cửa sổ PowerShell mới (dùng -NoExit để giữ log). Đường dẫn project
    tính tương đối từ thư mục repo gốc (hai cấp trên script này: CodeMVC/scripts -> CodeMVC -> repo).

    CHÚ Ý: Đây chỉ là launcher tiện lợi. KHÔNG tự đăng ký subscription (sandbox không có API đó) —
    xem hướng dẫn in ra cuối script và README mục 8.

.EXAMPLE
    pwsh CodeMVC/scripts/run-all.ps1
    pwsh CodeMVC/scripts/run-all.ps1 -WithReceiver
#>

param(
    # Chạy thêm CodeWebHooks receiver (:5099) làm referee/evidence (tùy chọn).
    [switch]$WithReceiver
)

$ErrorActionPreference = 'Stop'

# --- Xác định thư mục repo gốc (script nằm ở CodeMVC/scripts) ---
$RepoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
Write-Host "Repo root: $RepoRoot" -ForegroundColor Cyan

# --- Chọn shell để spawn (ưu tiên pwsh 7, fallback Windows PowerShell) ---
$Shell = if (Get-Command pwsh -ErrorAction SilentlyContinue) { 'pwsh' } else { 'powershell' }

# --- Helper: mở một cửa sổ mới chạy `dotnet run` cho một project ---
function Start-Service([string]$Title, [string]$ProjectRelPath) {
    $projFull = Join-Path $RepoRoot $ProjectRelPath
    if (-not (Test-Path $projFull)) {
        Write-Warning "Bỏ qua '$Title' — không thấy project: $projFull"
        return
    }
    Write-Host "Đang mở cửa sổ: $Title  ($ProjectRelPath)" -ForegroundColor Green
    # -NoExit giữ cửa sổ mở để xem log; $host.UI.RawUI.WindowTitle đặt tên cho dễ nhận biết.
    $cmd = "`$host.UI.RawUI.WindowTitle='$Title'; Set-Location '$RepoRoot'; dotnet run --project '$ProjectRelPath'"
    Start-Process $Shell -ArgumentList '-NoExit', '-Command', $cmd | Out-Null
    Start-Sleep -Seconds 2   # cách quãng nhẹ để log các service không chồng nhau khi bung port
}

# --- Khởi chạy theo thứ tự: (receiver) -> sandbox -> adminportal -> MVC ---
if ($WithReceiver) {
    Start-Service 'CodeWebHooks Receiver :5099' 'CodeWebHooks/src/NhatTinWebhookReceiver.Api'
}
Start-Service 'Sandbox API :5080'  'CodeSandBox/src/NhatTinSandbox.Api'
Start-Service 'AdminPortal :5090'  'CodeSandBox/src/NhatTinSandbox.AdminPortal'
Start-Service 'MVC Demo :5110'     'CodeMVC/src/NhatTinMvc.Web'

# --- Hướng dẫn sau khi bung ---
Write-Host ''
Write-Host '=================================================================' -ForegroundColor Yellow
Write-Host ' Các service đang khởi động (mỗi cái một cửa sổ). URL:' -ForegroundColor Yellow
Write-Host '   - MVC demo      : http://localhost:5110'
Write-Host '   - AdminPortal   : http://localhost:5090'
Write-Host '   - Sandbox API   : http://localhost:5080/swagger'
if ($WithReceiver) {
    Write-Host '   - Receiver      : http://localhost:5099'
}
Write-Host ''
Write-Host ' BƯỚC BẮT BUỘC 1 LẦN — đăng ký subscription để webhook về được MVC:' -ForegroundColor Yellow
Write-Host '   1) Mở AdminPortal http://localhost:5090 -> mục Subscriptions'
Write-Host '   2) Tạo subscription với CallbackUrl:'
Write-Host '        http://localhost:5110/webhooks/nhattin/status' -ForegroundColor Cyan
Write-Host ''
Write-Host ' Nhắc: cần appsettings.Local.json (conn-string SQL thật) và gói SDK trong'
Write-Host " local-feed. Chi tiết xem CodeMVC/README.md." -ForegroundColor Yellow
Write-Host '=================================================================' -ForegroundColor Yellow
