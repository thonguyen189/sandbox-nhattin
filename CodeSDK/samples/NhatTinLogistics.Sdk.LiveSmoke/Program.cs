using System.Text;
using NhatTinLogistics.Sdk;
using NhatTinLogistics.Sdk.Client;
using NhatTinLogistics.Sdk.Types.Requests;

// ---------------------------------------------------------------------------
// Live smoke test: drive the NhatTinLogistics.Sdk against the REAL NhatTin
// sandbox (https://apisandbox.ntlogistics.vn) through the SDK's public surface
// (NhatTinLogisticsClient) — NOT raw HTTP. Exercises Auth, Location and Bill.
//
// Credentials come from env vars (never hardcode secrets):
//     NHATTIN_USERNAME, NHATTIN_PASSWORD
// Optional:
//     NHATTIN_BASE_URL  (default: SDK Sandbox host)
// ---------------------------------------------------------------------------

Console.OutputEncoding = Encoding.UTF8;

var username = Environment.GetEnvironmentVariable("NHATTIN_USERNAME");
var password = Environment.GetEnvironmentVariable("NHATTIN_PASSWORD");
var baseUrl = Environment.GetEnvironmentVariable("NHATTIN_BASE_URL");

if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
{
    Console.Error.WriteLine("Set NHATTIN_USERNAME and NHATTIN_PASSWORD env vars first.");
    return 2;
}

var options = new NhatTinLogisticsClientOptions
{
    Username = username,
    Password = password,
    Environment = NhatTinEnvironment.Sandbox,
    // BaseUrl left null → resolves to the SDK's Sandbox host, unless overridden.
    BaseUrl = string.IsNullOrWhiteSpace(baseUrl) ? null : baseUrl,
    AutoAuthenticate = true,
};

using var client = new NhatTinLogisticsClient(options);

Console.WriteLine($"== SDK LIVE SMOKE ==  host={options.ResolveBaseUrl()}  user={username}");
Console.WriteLine($"   SDK v{SdkVersion.Current}\n");

int passed = 0, failed = 0;
string? refreshToken = null;
string? billCode = null;

static string Mask(string? s)
    => string.IsNullOrEmpty(s) ? "<null>" : s.Length <= 12 ? "***" : s[..6] + "..." + s[^4..];

async Task Step(string name, Func<Task<bool>> body)
{
    Console.WriteLine($"[ .. ] {name}");
    try
    {
        var ok = await body();
        if (ok) { passed++; Console.WriteLine($"[PASS] {name}\n"); }
        else { failed++; Console.WriteLine($"[FAIL] {name}\n"); }
    }
    catch (NhatTinLogistics.Sdk.Http.NhatTinApiException ex)
    {
        failed++;
        Console.WriteLine($"[FAIL] {name} -> NhatTinApiException: {ex.Message}");
        if (ex.InnerException is not null)
            Console.WriteLine($"       inner: {ex.InnerException.GetType().Name}: {ex.InnerException.Message}");
        if (!string.IsNullOrEmpty(ex.RawBody))
            Console.WriteLine($"       RAW BODY: {ex.RawBody[..Math.Min(700, ex.RawBody.Length)]}");
        Console.WriteLine();
    }
    catch (Exception ex)
    {
        failed++;
        Console.WriteLine($"[FAIL] {name} -> {ex.GetType().Name}: {ex.Message}\n");
    }
}

// 1) AUTH — explicit sign-in, read token metadata + partner_id.
await Step("Auth.SignInAsync", async () =>
{
    var res = await client.Auth.SignInAsync(username, password);
    Console.WriteLine($"       success={res.Success} http={res.HttpStatusCode} message={res.Message}");
    if (res.Data is null) return false;
    var d = res.Data;
    refreshToken = d.RefreshToken;
    Console.WriteLine($"       jwt_token          = {Mask(d.JwtToken)}");
    Console.WriteLine($"       token_type         = {d.TokenType}");
    Console.WriteLine($"       token_expires_in   = {d.TokenExpiresIn}");
    Console.WriteLine($"       refresh_token      = {Mask(d.RefreshToken)}");
    Console.WriteLine($"       refresh_expires_in = {d.RefreshExpiresIn}");
    Console.WriteLine($"       partner_id         = {d.PartnerId}");
    return res.Success && !string.IsNullOrEmpty(d.JwtToken);
});

// 2) LOCATION — provinces (new units). First authed call → triggers the SDK's
//    internal auto sign-in and auto-captures partner_id into options.
await Step("Location.GetProvincesAsync(isNew:true)", async () =>
{
    var res = await client.Location.GetProvincesAsync(isNew: true);
    Console.WriteLine($"       success={res.Success} http={res.HttpStatusCode} count={res.Data?.Count}");
    var first = res.Data is { Count: > 0 } ? res.Data[0] : null;
    if (first is not null) Console.WriteLine($"       sample: id={first.Id} name={first.ProvinceName} is_new={first.IsNew}");
    Console.WriteLine($"       -> options.PartnerId auto-captured = {options.PartnerId}");
    return res.Success && res.Data is { Count: > 0 };
});

// 3) LOCATION — wards for province "01" (short code), new units.
await Step("Location.GetWardsAsync(province=01, isNew:true)", async () =>
{
    var res = await client.Location.GetWardsAsync(districtId: null, provinceId: "01", isNew: true);
    Console.WriteLine($"       success={res.Success} http={res.HttpStatusCode} count={res.Data?.Count}");
    var first = res.Data is { Count: > 0 } ? res.Data[0] : null;
    if (first is not null) Console.WriteLine($"       sample: id={first.Id} name={first.WardName}");
    return res.Success && res.Data is { Count: > 0 };
});

// 4) BILL — calc fee (short codes; partner_id auto-filled from options).
await Step("Bill.CalcFeeAsync", async () =>
{
    var req = new CalcFeeRequest
    {
        Weight = 2, Width = 0, Length = 0, Height = 0,
        ServiceId = 91, PaymentMethodId = 10, CodAmount = 0, CargoValue = 0,
        SProvinceId = "01", SWardId = "00004",
        RProvinceId = "79", RWardId = "25750",
    };
    var res = await client.Bill.CalcFeeAsync(req);
    Console.WriteLine($"       success={res.Success} http={res.HttpStatusCode} options={res.Data?.Count}");
    var opt = res.Data is { Count: > 0 } ? res.Data[0] : null;
    if (opt is not null)
        Console.WriteLine($"       sample: service={opt.ServiceName} total_fee={opt.TotalFee} main_fee={opt.MainFee} lead_time={opt.LeadTime}");
    return res.Success && res.Data is { Count: > 0 } && res.Data[0].TotalFee > 0;
});

// 5) BILL — create with minimum payload, NO partner_id in body (taken from token).
await Step("Bill.CreateAsync (min payload, no partner_id)", async () =>
{
    var req = new CreateBillRequest
    {
        RefCode = "TP-SDK-SMOKE-" + Guid.NewGuid().ToString("N")[..8],
        Weight = 2, Width = 0, Length = 0, Height = 0,
        ServiceId = 91, PaymentMethodId = 10, CargoTypeId = 2,
        CodAmount = 0, CargoValue = 0,
        SName = "TruePos SDK", SPhone = "0333333333", SAddress = "so 10",
        SProvinceCode = "01", SWardCode = "00004",
        RName = "Nguyen Van A", RPhone = "0911111111", RAddress = "123",
        RProvinceCode = "79", RWardCode = "25750",
    };
    var res = await client.Bill.CreateAsync(req);
    Console.WriteLine($"       success={res.Success} http={res.HttpStatusCode} message={res.Message}");
    if (res.Data is not null)
    {
        billCode = res.Data.BillCode;
        Console.WriteLine($"       bill_code={res.Data.BillCode} status_id={res.Data.StatusId} ({res.Data.Status}) total_fee={res.Data.TotalFee}");
        Console.WriteLine($"       created_at={res.Data.CreatedAt} expected_at={res.Data.ExpectedAt}");
    }
    return res.Success && !string.IsNullOrEmpty(billCode);
});

// 6) BILL — tracking on the just-created bill.
await Step("Bill.TrackingAsync", async () =>
{
    if (string.IsNullOrEmpty(billCode)) { Console.WriteLine("       skipped: no bill_code"); return false; }
    var res = await client.Bill.TrackingAsync(billCode);
    Console.WriteLine($"       success={res.Success} http={res.HttpStatusCode} count={res.Data?.Count}");
    var t = res.Data is { Count: > 0 } ? res.Data[0] : null;
    if (t is not null)
        Console.WriteLine($"       bill_status_id={t.BillStatusId} desc={t.BillStatusDesc} service={t.Service} total_fee={t.TotalFee} histories={t.Histories.Count}");
    return res.Success && res.Data is { Count: > 0 };
});

// 7) BILL — print (content-type-aware; NhatTin returns 200 for both HTML success
//    and JSON error envelope, so PASS = "SDK correctly classified the response").
await Step("Bill.PrintAsync (content-type-aware)", async () =>
{
    if (string.IsNullOrEmpty(billCode)) { Console.WriteLine("       skipped: no bill_code"); return false; }
    var res = await client.Bill.PrintAsync(billCode);
    var preview = res.IsJson || res.IsHtml
        ? res.AsText()[..Math.Min(160, res.AsText().Length)]
        : $"<binary {res.Content.Length} bytes>";
    Console.WriteLine($"       http={res.HttpStatusCode} content-type={res.ContentType} isJson={res.IsJson} isHtml={res.IsHtml}");
    Console.WriteLine($"       business success={res.Success} errorCode={res.ErrorCode} message={res.Message}");
    Console.WriteLine($"       preview: {preview}");
    // The SDK's job here is faithful classification, not a successful print.
    return res.HttpStatusCode == 200 && res.ContentType is not null;
});

// 8) BILL — cancel/destroy the smoke bill (cleanup + exercises CancelAsync).
await Step("Bill.CancelAsync (cleanup)", async () =>
{
    if (string.IsNullOrEmpty(billCode)) { Console.WriteLine("       skipped: no bill_code"); return false; }
    var res = await client.Bill.CancelAsync(new[] { billCode });
    Console.WriteLine($"       success={res.Success} http={res.HttpStatusCode} message={res.Message}");
    if (res.Data is not null)
    {
        Console.WriteLine($"       succeeded={res.Data.Succeeded.Count} failed={res.Data.Failed.Count}");
        foreach (var c in res.Data.Succeeded) Console.WriteLine($"       [ok]   doCode={c.DoCode} message={c.Message}");
        foreach (var c in res.Data.Failed) Console.WriteLine($"       [fail] doCode={c.DoCode} message={c.Message}");
    }
    // Report the outcome regardless; a business rejection still proves the call round-tripped.
    return res.HttpStatusCode == 200;
});

// 9) AUTH — refresh token round-trip through the SDK.
await Step("Auth.RefreshTokenAsync", async () =>
{
    if (string.IsNullOrEmpty(refreshToken)) { Console.WriteLine("       skipped: no refresh_token"); return false; }
    var res = await client.Auth.RefreshTokenAsync(refreshToken);
    Console.WriteLine($"       success={res.Success} http={res.HttpStatusCode} message={res.Message}");
    if (res.Data is not null)
    {
        Console.WriteLine($"       new jwt_token     = {Mask(res.Data.JwtToken)}");
        Console.WriteLine($"       new refresh_token = {Mask(res.Data.RefreshToken)} rotated={res.Data.RefreshToken != refreshToken}");
        Console.WriteLine($"       token_expires_in  = {res.Data.TokenExpiresIn}");
    }
    return res.Success && res.Data is not null && !string.IsNullOrEmpty(res.Data.JwtToken);
});

Console.WriteLine($"== SUMMARY ==  passed={passed}  failed={failed}");
return failed == 0 ? 0 : 1;
