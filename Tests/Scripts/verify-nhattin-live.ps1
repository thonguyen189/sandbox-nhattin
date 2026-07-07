<#
    Live verification against the REAL NhatTin sandbox API.
    Reads credentials from env vars (never hardcode secrets):
        $env:NHATTIN_USERNAME, $env:NHATTIN_PASSWORD
    Optional:
        $env:NHATTIN_BASE_URL   (default https://apisandbox.ntlogistics.vn)
        $env:NHATTIN_PARTNER_ID (for calc-fee / print)

    Goal: capture real behavior for the open questions that block P0/P1:
      - token_expires_in / refresh_expires_in (real TTL)
      - refresh token rotation + old-token invalidation
      - CreateBill minimum payload WITHOUT partner_id (is partner_id really required?)
      - Print content-type / body shape (HTML vs PDF vs JSON)
      - Tracking request shape
      - Error envelope shape
#>
param(
    [string]$BaseUrl = $(if ($env:NHATTIN_BASE_URL) { $env:NHATTIN_BASE_URL } else { "https://apisandbox.ntlogistics.vn" })
)
$ErrorActionPreference = "Continue"
[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12 -bor [Net.SecurityProtocolType]::Tls11

function Mask([string]$s) {
    if ([string]::IsNullOrEmpty($s)) { return "<null>" }
    if ($s.Length -le 12) { return "***" }
    return $s.Substring(0,6) + "..." + $s.Substring($s.Length-4)
}

function Invoke-Nt {
    param([string]$Method, [string]$Url, $Headers, $Body, [string]$ContentType = "application/json")
    $result = [ordered]@{ ok = $false; status = $null; contentType = $null; length = $null; body = $null; error = $null }
    try {
        $params = @{ Method = $Method; Uri = $Url; UseBasicParsing = $true; TimeoutSec = 30 }
        if ($Headers) { $params.Headers = $Headers }
        if ($Body)    { $params.Body = $Body; $params.ContentType = $ContentType }
        $resp = Invoke-WebRequest @params
        $result.ok = $true
        $result.status = [int]$resp.StatusCode
        $result.contentType = $resp.Headers["Content-Type"]
        $result.length = $resp.RawContentLength
        $result.body = $resp.Content
    }
    catch {
        $result.error = $_.Exception.Message
        $r = $_.Exception.Response
        if ($r) {
            try { $result.status = [int]$r.StatusCode } catch {}
            try { $result.contentType = $r.ContentType } catch {}
            try {
                $sr = New-Object System.IO.StreamReader($r.GetResponseStream())
                $result.body = $sr.ReadToEnd(); $sr.Close()
            } catch {}
        }
    }
    return $result
}

if (-not $env:NHATTIN_USERNAME -or -not $env:NHATTIN_PASSWORD) {
    Write-Error "Set `$env:NHATTIN_USERNAME and `$env:NHATTIN_PASSWORD first."
    exit 2
}

Write-Host "== BASE: $BaseUrl ==" -ForegroundColor Cyan

# 1) SIGN-IN
Write-Host "`n[1] POST /v1/auth/sign-in" -ForegroundColor Yellow
$loginBody = @{ username = $env:NHATTIN_USERNAME; password = $env:NHATTIN_PASSWORD } | ConvertTo-Json
$login = Invoke-Nt -Method Post -Url "$BaseUrl/v1/auth/sign-in" -Body $loginBody
Write-Host "  status=$($login.status) contentType=$($login.contentType) error=$($login.error)"
$access = $null; $refresh = $null; $loginData = $null
if ($login.body) {
    try { $loginData = ($login.body | ConvertFrom-Json) } catch {}
    if ($loginData) {
        $d = $loginData.data
        $access = $d.jwt_token
        $refresh = $d.refresh_token
        Write-Host "  success       = $($loginData.success)"
        Write-Host "  jwt_token     = $(Mask $access)"
        Write-Host "  token_type    = $($d.token_type)"
        Write-Host "  token_expires_in   = $($d.token_expires_in)   <-- REAL ACCESS TTL"
        Write-Host "  refresh_token = $(Mask $refresh)"
        Write-Host "  refresh_expires_in = $($d.refresh_expires_in) <-- REAL REFRESH TTL"
        Write-Host "  data keys     = $(( $d | Get-Member -MemberType NoteProperty | Select-Object -ExpandProperty Name ) -join ', ')"
    }
}
if (-not $access) { Write-Host "  RAW: $($login.body)"; Write-Error "No access token; stopping."; exit 1 }
$auth = @{ Authorization = "Bearer $access" }

# 2) PROVINCES (auth smoke test)
Write-Host "`n[2] GET /v3/loc/provinces?is_new=1" -ForegroundColor Yellow
$prov = Invoke-Nt -Method Get -Url "$BaseUrl/v3/loc/provinces?is_new=1" -Headers $auth
Write-Host "  status=$($prov.status) len=$($prov.length) error=$($prov.error)"
if ($prov.body) { $pj = $prov.body | ConvertFrom-Json; Write-Host "  success=$($pj.success) provinceCount=$(@($pj.data).Count) sample=$(@($pj.data)[0] | ConvertTo-Json -Compress)" }

# 3) WARDS is_new=1 & province_id (Q-LOC-03)
Write-Host "`n[3] GET /v3/loc/wards?is_new=1&province_id=01" -ForegroundColor Yellow
$wards = Invoke-Nt -Method Get -Url "$BaseUrl/v3/loc/wards?is_new=1&province_id=01" -Headers $auth
Write-Host "  status=$($wards.status) len=$($wards.length) error=$($wards.error)"
if ($wards.body) { $wj = $wards.body | ConvertFrom-Json; Write-Host "  success=$($wj.success) wardCount=$(@($wj.data).Count) sample=$(@($wj.data)[0] | ConvertTo-Json -Compress)" }

# 4) REFRESH rotation (Q-AUTH-03): refresh once, then RE-USE the OLD refresh token -> expect failure if rotated
Write-Host "`n[4] POST /v1/auth/refresh-token (rotation test)" -ForegroundColor Yellow
$oldRefresh = $refresh
$ref1 = Invoke-Nt -Method Post -Url "$BaseUrl/v1/auth/refresh-token" -Body (@{ refresh_token = $oldRefresh } | ConvertTo-Json)
Write-Host "  [refresh#1] status=$($ref1.status) error=$($ref1.error)"
$newRefresh = $null
if ($ref1.body) {
    $rj = $ref1.body | ConvertFrom-Json
    $newRefresh = $rj.data.refresh_token
    Write-Host "    new jwt_token     = $(Mask $rj.data.jwt_token)"
    Write-Host "    new refresh_token = $(Mask $newRefresh)  rotated=$([bool]($newRefresh -and $newRefresh -ne $oldRefresh))"
    Write-Host "    token_expires_in  = $($rj.data.token_expires_in)"
}
Write-Host "  [refresh#2] re-using OLD refresh token (expect 401/400 if old is invalidated)"
$ref2 = Invoke-Nt -Method Post -Url "$BaseUrl/v1/auth/refresh-token" -Body (@{ refresh_token = $oldRefresh } | ConvertTo-Json)
Write-Host "    status=$($ref2.status) oldTokenRejected=$([bool]($ref2.status -ge 400)) error=$($ref2.error)"
if ($ref2.body) { Write-Host "    body=$($ref2.body)" }

# 5) CREATE BILL minimum payload WITHOUT partner_id (Q-BILL-01 / Q-BILL-03 conflict)
Write-Host "`n[5] POST /v3/bill/create (minimum payload, NO partner_id)" -ForegroundColor Yellow
$createBody = @{
    weight=2; width=0; length=0; height=0; service_id=91; payment_method_id=10
    cod_amount=0; cargo_value=0; cargo_type_id=2
    s_name="TEST"; s_phone="0333333333"; s_address="so 10"; s_ward_code="00004"; s_province_code="01"
    r_name="TEST"; r_phone="0333333333"; r_address="123"; r_ward_code="25750"; r_province_code="79"
} | ConvertTo-Json
$create = Invoke-Nt -Method Post -Url "$BaseUrl/v3/bill/create" -Headers $auth -Body $createBody
Write-Host "  status=$($create.status) error=$($create.error)"
$billCode = $null
if ($create.body) {
    Write-Host "  BODY: $($create.body)"
    try { $cj = $create.body | ConvertFrom-Json; $billCode = $cj.data.bill_code } catch {}
    Write-Host "  -> success=$($cj.success) bill_code=$billCode status_id=$($cj.data.status_id)"
}

# 6) PRINT content-type (Q-PRN-03) - needs a bill_code + partner_id
if ($billCode -and $env:NHATTIN_PARTNER_ID) {
    Write-Host "`n[6] GET /v3/bill/print?do_code=$billCode&partner_id=$($env:NHATTIN_PARTNER_ID)" -ForegroundColor Yellow
    $print = Invoke-Nt -Method Get -Url "$BaseUrl/v3/bill/print?do_code=$billCode&partner_id=$($env:NHATTIN_PARTNER_ID)" -Headers $auth
    Write-Host "  status=$($print.status) CONTENT-TYPE=$($print.contentType) len=$($print.length) error=$($print.error)"
    if ($print.body) { Write-Host "  first 300 chars: $($print.body.Substring(0,[Math]::Min(300,$print.body.Length)))" }
} else {
    Write-Host "`n[6] PRINT skipped (need bill_code and `$env:NHATTIN_PARTNER_ID)" -ForegroundColor DarkGray
}

# 7) TRACKING shape (Q-TRK-01)
if ($billCode) {
    Write-Host "`n[7] GET /v3/bill/tracking?bill_code=$billCode" -ForegroundColor Yellow
    $trk = Invoke-Nt -Method Get -Url "$BaseUrl/v3/bill/tracking?bill_code=$billCode" -Headers $auth
    Write-Host "  status=$($trk.status) contentType=$($trk.contentType) error=$($trk.error)"
    if ($trk.body) { Write-Host "  BODY(500): $($trk.body.Substring(0,[Math]::Min(500,$trk.body.Length)))" }
}

Write-Host "`n== DONE ==" -ForegroundColor Cyan
