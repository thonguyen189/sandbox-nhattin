# NhatTinLogistics.Sdk

Unofficial C# SDK for the **Nhat Tin Logistics (NTL) Open API** — modeled on the tingee-csharp SDK. Targets **.NET 6**.

Covers: JWT auth (auto sign-in + refresh on 401), bills (create / update / cancel / calc-fee / revert / tracking / print), locations (provinces / districts / wards), and typed webhook parsing.

> **Webhooks are not signed by Nhat Tin.** This SDK parses the payload into a typed object; there is no signature to verify (this is the key difference from Tingee's SDK).

## Install

```
dotnet add package NhatTinLogistics.Sdk
```

## Standalone

```csharp
using NhatTinLogistics.Sdk;
using NhatTinLogistics.Sdk.Types.Requests;

var client = new NhatTinLogisticsClient(new NhatTinLogisticsClientOptions
{
    Username    = "your_account",
    Password    = "your_password",
    Environment = NhatTinEnvironment.Sandbox, // or Production
    PartnerId   = 123736,                      // your partner id
});

var res = await client.Bill.CreateAsync(new CreateBillRequest
{
    Weight = 2, ServiceId = 91, PaymentMethodId = 10, CargoTypeId = 2,
    SName = "TEST", SPhone = "0333333333", SAddress = "số 10",
    SProvinceCode = "01", SWardCode = "00004",
    RName = "TEST", RPhone = "0333333333", RAddress = "123",
    RProvinceCode = "79", RWardCode = "25750",
});

if (res.IsSuccess) Console.WriteLine(res.Data!.BillCode);
else               Console.WriteLine(res.Message);
```

## ASP.NET Core (DI)

```csharp
builder.Services.AddNhatTinLogisticsClient(o =>
{
    o.Username = builder.Configuration["Ntl:Username"]!;
    o.Password = builder.Configuration["Ntl:Password"]!;
    o.Environment = NhatTinEnvironment.Sandbox;
    o.PartnerId = 123736;
});
// inject NhatTinLogisticsClient anywhere
```

## Manual token mode (`AutoAuthenticate = false`)

By default the SDK signs in lazily and refreshes the token on a 401. Set `AutoAuthenticate = false`
to fully manage auth yourself — the SDK will never sign in or refresh; it just attaches whatever
token you seed and returns the raw response.

```csharp
var client = new NhatTinLogisticsClient(new NhatTinLogisticsClientOptions
{
    BaseUrl = "https://apisandbox.ntlogistics.vn",
    AutoAuthenticate = false,   // Username/Password not required in this mode
});

// Seed the token you obtained elsewhere:
client.Tokens.SetTokens(accessToken, refreshToken);

var res = await client.Bill.CreateAsync(request);

if (!res.IsSuccess && res.HttpStatusCode == 401)
{
    // The SDK does NOT auto-refresh here — do it yourself, then re-seed:
    var refreshed = await client.Auth.RefreshTokenAsync(refreshToken);
    if (refreshed.IsSuccess)
    {
        client.Tokens.SetTokens(refreshed.Data!.JwtToken, refreshed.Data.RefreshToken);
        res = await client.Bill.CreateAsync(request); // retry
    }
}
```

## Handling webhooks

```csharp
using NhatTinLogistics.Sdk.Webhooks;

var raw = await new StreamReader(Request.Body).ReadToEndAsync();
if (NhatTinWebhookParser.TryParse(raw, out var payload))
{
    // payload.BillNo, payload.Status (enum), payload.ShippingFee, payload.StatusTimeUtc ...
}
```

## Notes

- All calls return `NhatTinResponse<T>` (`IsSuccess`, `Message`, `Data`). Business failures (`success:false`) do **not** throw; transport / JSON / auth failures throw `NhatTinApiException`. Call `.EnsureSuccess()` to convert a business failure into a throw.
- `tracking` returns some numeric fields as strings; those are kept as `string?`.
- `PartnerId` defaults from options for calc-fee / update / print; override per call where supported. It is also **auto-captured from the sign-in response** (`data.partner_id`) when you don't set it explicitly.
- Print: verified live to return a `{success,message,data}` JSON envelope (HTTP 200 + `success:false` on error, e.g. `[ERR-00019]`) — a successful label may be HTML. `GetPrintUrl` builds the URL; `PrintAsync` returns a **`PrintResult`** (`Success`, `IsJson`/`IsHtml`, `ContentType`, `Content` bytes, `AsText()`, `Message`, `ErrorCode`) so you can branch on the real content type instead of assuming a PDF/binary.
