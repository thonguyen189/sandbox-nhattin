param(
    [string]$SandboxBaseUrl = "http://localhost:5080",
    [string]$WebhookBaseUrl = "http://localhost:5099"
)
$ErrorActionPreference = "Stop"

Write-Host "1) Sign in..."
$login = Invoke-RestMethod -Method Post -Uri "$SandboxBaseUrl/v1/auth/sign-in" `
    -ContentType "application/json" `
    -Body (@{ username = "sandbox"; password = "sandbox123" } | ConvertTo-Json)
$token = $login.data.jwt_token
$headers = @{ Authorization = "Bearer $token" }
Write-Host "   login success = $($login.success)"

Write-Host "2) Get provinces..."
$provinces = Invoke-RestMethod -Method Get -Uri "$SandboxBaseUrl/v3/loc/provinces?is_new=1" -Headers $headers

Write-Host "3) Create bill..."
$billBody = @{
    ref_code = "TP-CYCLE-001"; weight = 2; service_id = 91; payment_method_id = 10; cargo_type_id = 2
    s_name = "TruePos"; s_phone = "0333333333"; s_address = "so 10"; s_province_code = "01"; s_ward_code = "00004"
    r_name = "Nguyen Van A"; r_phone = "0911111111"; r_address = "123"; r_province_code = "79"; r_ward_code = "27007"
    cod_amount = 120000
} | ConvertTo-Json
$bill = Invoke-RestMethod -Method Post -Uri "$SandboxBaseUrl/v3/bill/create" -Headers $headers -ContentType "application/json" -Body $billBody
$billCode = $bill.data.bill_code
Write-Host "   bill_code = $billCode (status $($bill.data.status_id))"

Write-Host "4) Simulate status change -> webhook..."
$sim = Invoke-RestMethod -Method Post -Uri "$SandboxBaseUrl/sandbox/bills/$billCode/simulate-status" `
    -ContentType "application/json" -Body (@{ status_id = 3; reason = "Da lay hang" } | ConvertTo-Json)

Write-Host "5) Tracking..."
$tracking = Invoke-RestMethod -Method Get -Uri "$SandboxBaseUrl/v3/bill/tracking?bill_code=$billCode" -Headers $headers

Write-Host "6) Verify webhook received by CodeWebHooks receiver..."
$webhookReceived = $false
try {
    # Use Invoke-WebRequest -UseBasicParsing (works on both pwsh and Windows PowerShell 5.1
    # without depending on an IE-based HTML DOM parser) to fetch the receiver's Razor log page.
    $receiverResponse = Invoke-WebRequest -Uri "$WebhookBaseUrl/" -UseBasicParsing
    $receiverHtml = $receiverResponse.Content
    $webhookReceived = $receiverHtml -match [regex]::Escape($billCode)
}
catch {
    Write-Warning "Could not reach webhook receiver at $WebhookBaseUrl/: $($_.Exception.Message)"
}
if (-not $webhookReceived) {
    Write-Warning "Webhook receiver did not report bill $billCode (spec section 7 requires delivery confirmation)."
}

$summary = [PSCustomObject]@{
    LoginSuccess    = $login.success
    ProvinceCount   = @($provinces.data).Count
    BillCode        = $billCode
    SimulateSuccess = $sim.success
    TrackingStatus  = $tracking.data[0].bill_status_id
    WebhookReceived = $webhookReceived
}
$summary | ConvertTo-Json

if (-not $webhookReceived) {
    exit 1
}
