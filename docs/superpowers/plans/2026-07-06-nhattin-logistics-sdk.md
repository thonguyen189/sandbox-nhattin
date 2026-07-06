# NhatTinLogistics.Sdk Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Build a standalone .NET 6 C# SDK (`NhatTinLogistics.Sdk`) that wraps the Nhat Tin Logistics HTTP API (auth, bill, location) with automatic JWT refresh, typed snake_case DTOs, master-data enums, and an unsigned-webhook payload parser — modeled on tingee-csharp.

**Architecture:** One class library under `CodeSDK/src/NhatTinLogistics.Sdk` with grouped API classes (`Auth`/`Bill`/`Location`) sitting on a single low-level `NhatTinHttpClient` that owns token management, snake_case JSON, and the `{success,message,data}` envelope. A separate xUnit project drives everything with a `StubHttpMessageHandler` (no network). Usable standalone (`new NhatTinLogisticsClient(options)`) or via DI (`services.AddNhatTinLogisticsClient(...)`).

**Tech Stack:** .NET 6 (`net6.0`), `System.Text.Json` (in-box), `Microsoft.Extensions.Http`, `Microsoft.Extensions.DependencyInjection.Abstractions`, xUnit.

## Global Constraints

- **Target framework:** `net6.0` for both projects. Do not use APIs newer than .NET 6.
- **Nullable:** `<Nullable>enable</Nullable>` and `<ImplicitUsings>enable</ImplicitUsings>` in both csproj files. Use file-scoped namespaces.
- **Root namespace:** `NhatTinLogistics.Sdk` (sub-namespaces `.Client`, `.Http`, `.Webhooks`, `.Types.Requests`, `.Types.Responses`, `.Types.Enums`, `.Extensions`).
- **Main entry class:** `NhatTinLogisticsClient`. Options class: `NhatTinLogisticsClientOptions`. Environment enum: `NhatTinEnvironment` (`Sandbox`, `Production`).
- **Hosts:** Sandbox `https://apisandbox.ntlogistics.vn`, Production `https://apiws.ntlogistics.vn`.
- **Envelope:** every API call returns `NhatTinResponse<T>` mapped from `{success,message,data}`. `success:false` returns a non-success envelope (never throws); transport/JSON/auth failures throw `NhatTinApiException`.
- **JSON:** all DTOs use explicit `[JsonPropertyName("snake_case")]`; shared options live in `NhatTinJson.Options`.
- **Webhooks are NOT signed** by Nhat Tin — no signature verification anywhere.
- **Default PartnerId:** `123736` is only an example; it is supplied by the consumer via `Options.PartnerId`. Do not hardcode it in the library.
- **TDD:** write the failing test first every task. Commit after each task with tests green.
- **Source of truth for wire format:** `NhatTinAPIDocumentation/vi/`.

---

## File Structure

```
CodeSDK/
  NhatTinLogisticsSdk.sln
  src/NhatTinLogistics.Sdk/
    NhatTinLogistics.Sdk.csproj
    NhatTinLogisticsClient.cs                 # entry point (Task 11)
    NhatTinLogisticsClientOptions.cs          # options + ResolveBaseUrl/Validate (Task 4)
    NhatTinEnvironment.cs                     # enum (Task 4)
    SdkVersion.cs                             # version constant (Task 12)
    Http/
      NhatTinJson.cs                          # shared JsonSerializerOptions (Task 2)
      NhatTinResponse.cs                      # envelope + RawEnvelope<T> (Task 2)
      NhatTinApiException.cs                  # exception (Task 2)
      ITokenStore.cs                          # token store contract (Task 3)
      InMemoryTokenStore.cs                   # default store (Task 3)
      NhatTinHttpClient.cs                    # send + envelope + auth (Task 5)
    Client/
      IAuthApi.cs / AuthApi.cs                # Task 6
      IBillApi.cs / BillApi.cs                # Tasks 7-9
      ILocationApi.cs / LocationApi.cs        # Task 9
    Types/
      Enums/BillStatus.cs                     # + ServiceType/PaymentMethod/CargoType (Task 2)
      Responses/AuthToken.cs                  # Task 5
      Requests/CreateBillRequest.cs           # Task 7
      Responses/BillResult.cs                 # Task 7
      Requests/UpdateBillRequest.cs           # Task 7
      Requests/CalcFeeRequest.cs              # Task 8
      Responses/FeeOption.cs                  # Task 8
      Responses/CancelResult.cs               # Task 8
      Responses/RevertResult.cs               # Task 8
      Responses/TrackingResult.cs             # Task 8
      Responses/LocationDtos.cs               # Province/District/Ward (Task 9)
    Webhooks/
      WebhookPayload.cs                       # Task 10
      NhatTinWebhookParser.cs                 # Task 10
    Extensions/
      ServiceCollectionExtensions.cs          # AddNhatTinLogisticsClient (Task 11)
    README.md                                 # Task 12
    CHANGELOG.md                              # Task 12
  tests/NhatTinLogistics.Sdk.Tests/
    NhatTinLogistics.Sdk.Tests.csproj
    Infrastructure/StubHttpMessageHandler.cs  # Task 1
    Infrastructure/TestResponses.cs           # helper (Task 1)
    *Tests.cs                                 # per task
```

---

### Task 1: Scaffold solution, projects, and test infrastructure

**Files:**
- Create: `CodeSDK/NhatTinLogisticsSdk.sln`
- Create: `CodeSDK/src/NhatTinLogistics.Sdk/NhatTinLogistics.Sdk.csproj`
- Create: `CodeSDK/tests/NhatTinLogistics.Sdk.Tests/NhatTinLogistics.Sdk.Tests.csproj`
- Create: `CodeSDK/tests/NhatTinLogistics.Sdk.Tests/Infrastructure/StubHttpMessageHandler.cs`
- Create: `CodeSDK/tests/NhatTinLogistics.Sdk.Tests/Infrastructure/TestResponses.cs`
- Create: `CodeSDK/tests/NhatTinLogistics.Sdk.Tests/ScaffoldSmokeTests.cs`

**Interfaces:**
- Produces: `StubHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> responder)` with `List<HttpRequestMessage> Requests` and `List<string> RequestBodies`; `TestResponses.Json(HttpStatusCode, string)` → `HttpResponseMessage`.

- [ ] **Step 1: Create solution and projects**

Run from repo root:

```bash
dotnet new sln -o CodeSDK -n NhatTinLogisticsSdk
dotnet new classlib -o CodeSDK/src/NhatTinLogistics.Sdk -f net6.0
dotnet new xunit -o CodeSDK/tests/NhatTinLogistics.Sdk.Tests -f net6.0
rm CodeSDK/src/NhatTinLogistics.Sdk/Class1.cs
rm CodeSDK/tests/NhatTinLogistics.Sdk.Tests/UnitTest1.cs
dotnet sln CodeSDK/NhatTinLogisticsSdk.sln add CodeSDK/src/NhatTinLogistics.Sdk/NhatTinLogistics.Sdk.csproj
dotnet sln CodeSDK/NhatTinLogisticsSdk.sln add CodeSDK/tests/NhatTinLogistics.Sdk.Tests/NhatTinLogistics.Sdk.Tests.csproj
dotnet add CodeSDK/tests/NhatTinLogistics.Sdk.Tests reference CodeSDK/src/NhatTinLogistics.Sdk
dotnet add CodeSDK/src/NhatTinLogistics.Sdk package Microsoft.Extensions.Http
dotnet add CodeSDK/src/NhatTinLogistics.Sdk package Microsoft.Extensions.DependencyInjection.Abstractions
```

- [ ] **Step 2: Overwrite the library csproj** (`CodeSDK/src/NhatTinLogistics.Sdk/NhatTinLogistics.Sdk.csproj`) — keep the `PackageReference` items the CLI added, but set properties. Full file:

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>net6.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <LangVersion>latest</LangVersion>
    <RootNamespace>NhatTinLogistics.Sdk</RootNamespace>
    <AssemblyName>NhatTinLogistics.Sdk</AssemblyName>
    <GenerateDocumentationFile>true</GenerateDocumentationFile>
    <NoWarn>$(NoWarn);CS1591</NoWarn>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.Extensions.Http" Version="6.0.0" />
    <PackageReference Include="Microsoft.Extensions.DependencyInjection.Abstractions" Version="6.0.0" />
  </ItemGroup>

</Project>
```

- [ ] **Step 3: Write `Infrastructure/StubHttpMessageHandler.cs`**

```csharp
using System.Net.Http;

namespace NhatTinLogistics.Sdk.Tests.Infrastructure;

/// <summary>Deterministic HttpMessageHandler for tests. Captures requests + bodies, returns a scripted response.</summary>
public sealed class StubHttpMessageHandler : HttpMessageHandler
{
    private readonly Func<HttpRequestMessage, HttpResponseMessage> _responder;
    private readonly object _gate = new();

    public List<HttpRequestMessage> Requests { get; } = new();
    public List<string> RequestBodies { get; } = new();
    public int CallCount { get { lock (_gate) { return Requests.Count; } } }

    public StubHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> responder)
        => _responder = responder;

    // Thread-safe: the concurrency test fires parallel requests through one handler.
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var body = request.Content is null ? "" : await request.Content.ReadAsStringAsync(cancellationToken);
        lock (_gate)
        {
            Requests.Add(request);
            RequestBodies.Add(body);
        }
        return _responder(request);
    }
}
```

- [ ] **Step 4: Write `Infrastructure/TestResponses.cs`**

```csharp
using System.Net;
using System.Net.Http;
using System.Text;

namespace NhatTinLogistics.Sdk.Tests.Infrastructure;

public static class TestResponses
{
    public static HttpResponseMessage Json(HttpStatusCode code, string body)
        => new(code) { Content = new StringContent(body, Encoding.UTF8, "application/json") };

    public static HttpResponseMessage Ok(string body) => Json(HttpStatusCode.OK, body);
}
```

- [ ] **Step 5: Write `ScaffoldSmokeTests.cs`** (proves the test project + stub compile and run)

```csharp
using System.Net;
using System.Net.Http;
using NhatTinLogistics.Sdk.Tests.Infrastructure;
using Xunit;

namespace NhatTinLogistics.Sdk.Tests;

public class ScaffoldSmokeTests
{
    [Fact]
    public async Task Stub_handler_returns_scripted_response_and_records_request()
    {
        var handler = new StubHttpMessageHandler(_ => TestResponses.Ok("{\"success\":true}"));
        using var http = new HttpClient(handler) { BaseAddress = new Uri("https://example.test") };

        var resp = await http.GetAsync("/ping");

        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        Assert.Single(handler.Requests);
    }
}
```

- [ ] **Step 6: Build and test**

Run: `dotnet test CodeSDK/NhatTinLogisticsSdk.sln`
Expected: build succeeds, 1 test passes. (If the .NET 6 targeting pack is missing, `dotnet build` restores `Microsoft.NETCore.App.Ref` automatically — no manual install needed.)

- [ ] **Step 7: Commit**

```bash
git add CodeSDK/NhatTinLogisticsSdk.sln CodeSDK/src CodeSDK/tests
git commit -m "chore(sdk): scaffold NhatTinLogistics.Sdk .NET 6 solution + test infra"
```

---

### Task 2: Foundations — JSON options, envelope, exception, enums

**Files:**
- Create: `CodeSDK/src/NhatTinLogistics.Sdk/Http/NhatTinJson.cs`
- Create: `CodeSDK/src/NhatTinLogistics.Sdk/Http/NhatTinApiException.cs`
- Create: `CodeSDK/src/NhatTinLogistics.Sdk/Http/NhatTinResponse.cs`
- Create: `CodeSDK/src/NhatTinLogistics.Sdk/Types/Enums/BillStatus.cs`
- Test: `CodeSDK/tests/NhatTinLogistics.Sdk.Tests/FoundationTests.cs`

**Interfaces:**
- Produces:
  - `NhatTinJson.Options` (`JsonSerializerOptions`).
  - `NhatTinApiException(string message, int httpStatusCode = 0, string? rawBody = null, Exception? inner = null)` with `int HttpStatusCode`, `string? RawBody`.
  - `NhatTinResponse<T>` with `bool Success`, `string? Message`, `T? Data`, `int HttpStatusCode`, `string RawBody`, `bool IsSuccess`, `NhatTinResponse<T> EnsureSuccess()`.
  - `RawEnvelope<T>` (internal) with `[JsonPropertyName]` `success`/`message`/`data`.
  - Enums `BillStatus`, `ServiceType`, `PaymentMethod`, `CargoType`; extension `int.ToBillStatus()`.

- [ ] **Step 1: Write the failing test** (`FoundationTests.cs`)

```csharp
using System.Text.Json;
using NhatTinLogistics.Sdk.Http;
using NhatTinLogistics.Sdk.Types.Enums;
using Xunit;

namespace NhatTinLogistics.Sdk.Tests;

public class FoundationTests
{
    [Fact]
    public void EnsureSuccess_throws_when_not_successful()
    {
        var resp = new NhatTinResponse<string> { Success = false, Message = "bad", HttpStatusCode = 200, RawBody = "{}" };
        var ex = Assert.Throws<NhatTinApiException>(() => resp.EnsureSuccess());
        Assert.Contains("bad", ex.Message);
    }

    [Fact]
    public void EnsureSuccess_returns_self_when_successful()
    {
        var resp = new NhatTinResponse<string> { Success = true, Data = "ok" };
        Assert.Same(resp, resp.EnsureSuccess());
        Assert.True(resp.IsSuccess);
    }

    [Fact]
    public void RawEnvelope_deserializes_success_message_data()
    {
        var env = JsonSerializer.Deserialize<RawEnvelope<int>>(
            "{\"success\":true,\"message\":\"m\",\"data\":42}", NhatTinJson.Options);
        Assert.NotNull(env);
        Assert.True(env!.Success);
        Assert.Equal("m", env.Message);
        Assert.Equal(42, env.Data);
    }

    [Theory]
    [InlineData(4, BillStatus.Delivered)]
    [InlineData(2, BillStatus.WaitingPickup)]
    [InlineData(99, BillStatus.Unknown)]
    public void ToBillStatus_maps_known_and_unknown(int id, BillStatus expected)
        => Assert.Equal(expected, id.ToBillStatus());
}
```

- [ ] **Step 2: Run to verify it fails**

Run: `dotnet test CodeSDK/NhatTinLogisticsSdk.sln --filter FullyQualifiedName~FoundationTests`
Expected: FAIL — types do not exist / do not compile.

- [ ] **Step 3: Write `Http/NhatTinJson.cs`**

```csharp
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace NhatTinLogistics.Sdk.Http;

/// <summary>Shared JSON options for all NhatTin serialization. snake_case comes from explicit [JsonPropertyName].</summary>
public static class NhatTinJson
{
    public static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };
}
```

- [ ] **Step 4: Write `Http/NhatTinApiException.cs`**

```csharp
namespace NhatTinLogistics.Sdk.Http;

/// <summary>Thrown for transport, JSON-parse, or authentication failures. Business failures (success:false) do NOT throw.</summary>
public sealed class NhatTinApiException : Exception
{
    public int HttpStatusCode { get; }
    public string? RawBody { get; }

    public NhatTinApiException(string message, int httpStatusCode = 0, string? rawBody = null, Exception? inner = null)
        : base(message, inner)
    {
        HttpStatusCode = httpStatusCode;
        RawBody = rawBody;
    }
}
```

- [ ] **Step 5: Write `Http/NhatTinResponse.cs`**

```csharp
using System.Text.Json.Serialization;

namespace NhatTinLogistics.Sdk.Http;

/// <summary>Public result wrapper mapped from the NhatTin { success, message, data } envelope.</summary>
public sealed class NhatTinResponse<T>
{
    public bool Success { get; init; }
    public string? Message { get; init; }
    public T? Data { get; init; }
    public int HttpStatusCode { get; init; }
    public string RawBody { get; init; } = "";

    public bool IsSuccess => Success;

    public NhatTinResponse<T> EnsureSuccess()
    {
        if (!Success)
            throw new NhatTinApiException($"NhatTin API returned failure: {Message}", HttpStatusCode, RawBody);
        return this;
    }
}

/// <summary>Wire shape of the envelope. Internal — consumers use NhatTinResponse&lt;T&gt;.</summary>
internal sealed class RawEnvelope<T>
{
    [JsonPropertyName("success")] public bool Success { get; set; }
    [JsonPropertyName("message")] public string? Message { get; set; }
    [JsonPropertyName("data")] public T? Data { get; set; }
}
```

- [ ] **Step 6: Write `Types/Enums/BillStatus.cs`** (all four enums + helper in one file)

```csharp
namespace NhatTinLogistics.Sdk.Types.Enums;

/// <summary>Bill status ids (Master Data §4). Unknown/unmapped ids collapse to Unknown; the raw int is kept on the DTO.</summary>
public enum BillStatus
{
    Unknown = 0,
    WaitingFail = 1,
    WaitingPickup = 2,
    PickedUp = 3,
    Delivered = 4,
    Cancelled = 6,
    FailedDelivery = 7,
    Returning = 9,
    Returned = 10,
    DeliveryIncident = 11,
    Draft = 12,
    Delivering = 13,
    InTransit = 15,
    ReturnDelivering = 16,
    PickupError = 17,
}

/// <summary>Service ids (Master Data §1).</summary>
public enum ServiceType
{
    GiaoHangNhanh = 90, // CPN
    HoaToc = 81,
    TietKiem = 91,
    HonHopMES = 21,
}

/// <summary>Payment method ids (Master Data §2).</summary>
public enum PaymentMethod
{
    SenderPayNow = 10,
    SenderPayLater = 11,
    ReceiverPayNow = 20,
}

/// <summary>Cargo type ids (Master Data §3).</summary>
public enum CargoType
{
    ChungTu = 1,
    HangHoa = 2,
    HangLanh = 3,
    SinhPham = 4,
    MauBenhPham = 5,
}

public static class BillStatusExtensions
{
    public static BillStatus ToBillStatus(this int statusId)
        => Enum.IsDefined(typeof(BillStatus), statusId) ? (BillStatus)statusId : BillStatus.Unknown;
}
```

- [ ] **Step 7: Run to verify pass**

Run: `dotnet test CodeSDK/NhatTinLogisticsSdk.sln --filter FullyQualifiedName~FoundationTests`
Expected: PASS (4 tests / theory cases).

- [ ] **Step 8: Commit**

```bash
git add CodeSDK/src CodeSDK/tests
git commit -m "feat(sdk): json options, response envelope, exception, master-data enums"
```

---

### Task 3: Token store

**Files:**
- Create: `CodeSDK/src/NhatTinLogistics.Sdk/Http/ITokenStore.cs`
- Create: `CodeSDK/src/NhatTinLogistics.Sdk/Http/InMemoryTokenStore.cs`
- Test: `CodeSDK/tests/NhatTinLogistics.Sdk.Tests/TokenStoreTests.cs`

**Interfaces:**
- Produces: `ITokenStore` { `string? AccessToken`, `string? RefreshToken`, `void SetTokens(string accessToken, string refreshToken)`, `void Clear()` }; `InMemoryTokenStore : ITokenStore` (thread-safe).

- [ ] **Step 1: Write the failing test** (`TokenStoreTests.cs`)

```csharp
using NhatTinLogistics.Sdk.Http;
using Xunit;

namespace NhatTinLogistics.Sdk.Tests;

public class TokenStoreTests
{
    [Fact]
    public void SetTokens_then_read_back()
    {
        ITokenStore store = new InMemoryTokenStore();
        Assert.Null(store.AccessToken);

        store.SetTokens("acc", "ref");
        Assert.Equal("acc", store.AccessToken);
        Assert.Equal("ref", store.RefreshToken);

        store.Clear();
        Assert.Null(store.AccessToken);
        Assert.Null(store.RefreshToken);
    }
}
```

- [ ] **Step 2: Run to verify it fails**

Run: `dotnet test CodeSDK/NhatTinLogisticsSdk.sln --filter FullyQualifiedName~TokenStoreTests`
Expected: FAIL — `ITokenStore`/`InMemoryTokenStore` not found.

- [ ] **Step 3: Write `Http/ITokenStore.cs`**

```csharp
namespace NhatTinLogistics.Sdk.Http;

/// <summary>Holds the current JWT access + refresh tokens. Default impl is in-memory; consumers may supply their own.</summary>
public interface ITokenStore
{
    string? AccessToken { get; }
    string? RefreshToken { get; }
    void SetTokens(string accessToken, string refreshToken);
    void Clear();
}
```

- [ ] **Step 4: Write `Http/InMemoryTokenStore.cs`**

```csharp
namespace NhatTinLogistics.Sdk.Http;

public sealed class InMemoryTokenStore : ITokenStore
{
    private readonly object _lock = new();
    private string? _access;
    private string? _refresh;

    public string? AccessToken { get { lock (_lock) { return _access; } } }
    public string? RefreshToken { get { lock (_lock) { return _refresh; } } }

    public void SetTokens(string accessToken, string refreshToken)
    {
        lock (_lock) { _access = accessToken; _refresh = refreshToken; }
    }

    public void Clear()
    {
        lock (_lock) { _access = null; _refresh = null; }
    }
}
```

- [ ] **Step 5: Run to verify pass**

Run: `dotnet test CodeSDK/NhatTinLogisticsSdk.sln --filter FullyQualifiedName~TokenStoreTests`
Expected: PASS.

- [ ] **Step 6: Commit**

```bash
git add CodeSDK/src CodeSDK/tests
git commit -m "feat(sdk): in-memory token store"
```

---

### Task 4: Options + Environment

**Files:**
- Create: `CodeSDK/src/NhatTinLogistics.Sdk/NhatTinEnvironment.cs`
- Create: `CodeSDK/src/NhatTinLogistics.Sdk/NhatTinLogisticsClientOptions.cs`
- Test: `CodeSDK/tests/NhatTinLogistics.Sdk.Tests/OptionsTests.cs`

**Interfaces:**
- Produces: `NhatTinEnvironment { Sandbox, Production }`; `NhatTinLogisticsClientOptions` with `Username`, `Password`, `Environment`, `BaseUrl`, `PartnerId`, `TimeoutMilliseconds` (default 90000), `AutoAuthenticate` (default true), `string ResolveBaseUrl()`, `void Validate()`.

- [ ] **Step 1: Write the failing test** (`OptionsTests.cs`)

```csharp
using NhatTinLogistics.Sdk;
using Xunit;

namespace NhatTinLogistics.Sdk.Tests;

public class OptionsTests
{
    [Fact]
    public void ResolveBaseUrl_uses_environment_host()
    {
        Assert.Equal("https://apisandbox.ntlogistics.vn",
            new NhatTinLogisticsClientOptions { Environment = NhatTinEnvironment.Sandbox }.ResolveBaseUrl());
        Assert.Equal("https://apiws.ntlogistics.vn",
            new NhatTinLogisticsClientOptions { Environment = NhatTinEnvironment.Production }.ResolveBaseUrl());
    }

    [Fact]
    public void ResolveBaseUrl_prefers_explicit_baseurl_trimmed()
    {
        var o = new NhatTinLogisticsClientOptions { BaseUrl = "https://local.test/" };
        Assert.Equal("https://local.test", o.ResolveBaseUrl());
    }

    [Fact]
    public void Validate_throws_when_autoauth_without_credentials()
    {
        var o = new NhatTinLogisticsClientOptions { AutoAuthenticate = true };
        Assert.Throws<ArgumentException>(() => o.Validate());
    }

    [Fact]
    public void Validate_ok_when_autoauth_disabled()
    {
        var o = new NhatTinLogisticsClientOptions { AutoAuthenticate = false };
        o.Validate(); // must not throw
    }
}
```

- [ ] **Step 2: Run to verify it fails**

Run: `dotnet test CodeSDK/NhatTinLogisticsSdk.sln --filter FullyQualifiedName~OptionsTests`
Expected: FAIL — types not found.

- [ ] **Step 3: Write `NhatTinEnvironment.cs`**

```csharp
namespace NhatTinLogistics.Sdk;

public enum NhatTinEnvironment
{
    Sandbox,
    Production,
}
```

- [ ] **Step 4: Write `NhatTinLogisticsClientOptions.cs`**

```csharp
namespace NhatTinLogistics.Sdk;

public sealed class NhatTinLogisticsClientOptions
{
    /// <summary>JWT login account. Required when AutoAuthenticate is true.</summary>
    public string? Username { get; set; }
    /// <summary>JWT login password. Required when AutoAuthenticate is true.</summary>
    public string? Password { get; set; }

    public NhatTinEnvironment Environment { get; set; } = NhatTinEnvironment.Sandbox;

    /// <summary>Overrides Environment host when set (e.g. for tests/self-host).</summary>
    public string? BaseUrl { get; set; }

    /// <summary>Default partner_id for calc-fee / update-shipping / print. Supplied by the consumer.</summary>
    public int? PartnerId { get; set; }

    public int TimeoutMilliseconds { get; set; } = 90_000;

    /// <summary>When true, the SDK signs in lazily and refreshes the token on 401.</summary>
    public bool AutoAuthenticate { get; set; } = true;

    public string ResolveBaseUrl()
        => !string.IsNullOrWhiteSpace(BaseUrl)
            ? BaseUrl!.TrimEnd('/')
            : Environment == NhatTinEnvironment.Production
                ? "https://apiws.ntlogistics.vn"
                : "https://apisandbox.ntlogistics.vn";

    public void Validate()
    {
        if (AutoAuthenticate && (string.IsNullOrWhiteSpace(Username) || string.IsNullOrWhiteSpace(Password)))
            throw new ArgumentException("Username and Password are required when AutoAuthenticate is enabled.");
    }
}
```

- [ ] **Step 5: Run to verify pass**

Run: `dotnet test CodeSDK/NhatTinLogisticsSdk.sln --filter FullyQualifiedName~OptionsTests`
Expected: PASS.

- [ ] **Step 6: Commit**

```bash
git add CodeSDK/src CodeSDK/tests
git commit -m "feat(sdk): client options + environment host resolution"
```

---

### Task 5: NhatTinHttpClient — send, envelope, and JWT auth flow

This is the core. One file, one responsibility (talk to NhatTin over HTTP with tokens). Several TDD cycles.

**Files:**
- Create: `CodeSDK/src/NhatTinLogistics.Sdk/Types/Responses/AuthToken.cs`
- Create: `CodeSDK/src/NhatTinLogistics.Sdk/Http/NhatTinHttpClient.cs`
- Test: `CodeSDK/tests/NhatTinLogistics.Sdk.Tests/NhatTinHttpClientTests.cs`

**Interfaces:**
- Consumes: `NhatTinJson`, `NhatTinResponse<T>`, `RawEnvelope<T>`, `NhatTinApiException`, `ITokenStore`, `NhatTinLogisticsClientOptions`.
- Produces:
  - `AuthToken` (`jwt_token`, `token_type`, `token_expires_in`, `refresh_token`, `refresh_expires_in`).
  - `NhatTinHttpClient(HttpClient http, NhatTinLogisticsClientOptions options, ITokenStore tokens)`.
  - `Task<NhatTinResponse<T>> SendAsync<T>(HttpMethod method, string path, object? body, bool authenticated, CancellationToken ct)`.
  - `Task<NhatTinResponse<T>> PostAsync<T>(string path, object body, CancellationToken ct)` (authenticated).
  - `Task<NhatTinResponse<T>> GetAsync<T>(string path, CancellationToken ct)` (authenticated).
  - `Task<byte[]> GetBytesAsync(string url, CancellationToken ct)` (authenticated).

- [ ] **Step 1: Write `Types/Responses/AuthToken.cs`** (needed to compile the tests + client)

```csharp
using System.Text.Json.Serialization;

namespace NhatTinLogistics.Sdk.Types.Responses;

public sealed class AuthToken
{
    [JsonPropertyName("jwt_token")] public string JwtToken { get; set; } = "";
    [JsonPropertyName("token_type")] public string? TokenType { get; set; }
    [JsonPropertyName("token_expires_in")] public string? TokenExpiresIn { get; set; }
    [JsonPropertyName("refresh_token")] public string RefreshToken { get; set; } = "";
    [JsonPropertyName("refresh_expires_in")] public string? RefreshExpiresIn { get; set; }
}
```

- [ ] **Step 2: Write the failing tests** (`NhatTinHttpClientTests.cs`) — covers envelope, lazy sign-in, retry-401, single-refresh under concurrency, and business-failure-no-throw.

```csharp
using System.Net;
using System.Net.Http;
using NhatTinLogistics.Sdk;
using NhatTinLogistics.Sdk.Http;
using NhatTinLogistics.Sdk.Tests.Infrastructure;
using Xunit;

namespace NhatTinLogistics.Sdk.Tests;

public class NhatTinHttpClientTests
{
    private static (NhatTinHttpClient client, StubHttpMessageHandler handler, InMemoryTokenStore store) Build(
        Func<HttpRequestMessage, HttpResponseMessage> responder,
        Action<NhatTinLogisticsClientOptions>? configure = null)
    {
        var options = new NhatTinLogisticsClientOptions { Username = "u", Password = "p", BaseUrl = "https://test.local" };
        configure?.Invoke(options);
        var handler = new StubHttpMessageHandler(responder);
        var http = new HttpClient(handler) { BaseAddress = new Uri(options.ResolveBaseUrl()) };
        var store = new InMemoryTokenStore();
        return (new NhatTinHttpClient(http, options, store), handler, store);
    }

    private const string SignInBody =
        "{\"success\":true,\"data\":{\"jwt_token\":\"ACCESS\",\"refresh_token\":\"REFRESH\",\"token_type\":\"Bearer\"}}";

    [Fact]
    public async Task Post_success_maps_envelope_and_signs_in_first()
    {
        var (client, handler, store) = Build(req =>
            req.RequestUri!.AbsolutePath.EndsWith("/sign-in")
                ? TestResponses.Ok(SignInBody)
                : TestResponses.Ok("{\"success\":true,\"message\":\"ok\",\"data\":{\"bill_code\":\"CP1\"}}"));

        var resp = await client.PostAsync<Dictionary<string, string>>("/v3/bill/create", new { weight = 2 }, default);

        Assert.True(resp.IsSuccess);
        Assert.Equal("CP1", resp.Data!["bill_code"]);
        Assert.Equal("ACCESS", store.AccessToken);
        // sign-in call then the real call
        Assert.Equal(2, handler.CallCount);
        Assert.Contains("Bearer ACCESS", handler.Requests[1].Headers.Authorization!.ToString());
    }

    [Fact]
    public async Task Business_failure_does_not_throw()
    {
        var (client, _, _) = Build(req =>
            req.RequestUri!.AbsolutePath.EndsWith("/sign-in")
                ? TestResponses.Ok(SignInBody)
                : TestResponses.Ok("{\"success\":false,\"message\":\"bad input\",\"data\":null}"));

        var resp = await client.PostAsync<object>("/v3/bill/create", new { }, default);

        Assert.False(resp.IsSuccess);
        Assert.Equal("bad input", resp.Message);
    }

    [Fact]
    public async Task Unauthorized_triggers_single_refresh_then_retry()
    {
        var phase = 0;
        var (client, handler, store) = Build(req =>
        {
            var path = req.RequestUri!.AbsolutePath;
            if (path.EndsWith("/sign-in")) return TestResponses.Ok(SignInBody);
            if (path.EndsWith("/refresh-token"))
                return TestResponses.Ok("{\"success\":true,\"data\":{\"jwt_token\":\"ACCESS2\",\"refresh_token\":\"REFRESH2\"}}");
            // first business call 401, second OK
            return phase++ == 0
                ? TestResponses.Json(HttpStatusCode.Unauthorized, "{\"success\":false,\"message\":\"expired\"}")
                : TestResponses.Ok("{\"success\":true,\"data\":1}");
        });

        var resp = await client.GetAsync<int>("/v3/bill/tracking?bill_code=CP1", default);

        Assert.True(resp.IsSuccess);
        Assert.Equal(1, resp.Data);
        Assert.Equal("ACCESS2", store.AccessToken);
        Assert.Contains(handler.Requests, r => r.RequestUri!.AbsolutePath.EndsWith("/refresh-token"));
    }

    [Fact]
    public async Task Concurrent_401s_refresh_only_once()
    {
        // Ensure the pool can hand out 5 threads at once so all callers can sit in the barrier
        // together without starving the pool.
        ThreadPool.GetMinThreads(out var w, out var c);
        ThreadPool.SetMinThreads(Math.Max(w, 8), c);

        var refreshCount = 0;
        // Barrier: hold every stale-token (ACCESS) 401 until all 5 callers have arrived, so they
        // all reach RefreshIfStaleAsync's single-flight guard simultaneously. If the guard were
        // removed this would yield refreshCount == 5 (or hang → timeout → fail) instead of 1.
        using var gate = new System.Threading.CountdownEvent(5);

        var (client, _, store) = Build(req =>
        {
            var path = req.RequestUri!.AbsolutePath;
            if (path.EndsWith("/sign-in")) return TestResponses.Ok(SignInBody);
            if (path.EndsWith("/refresh-token"))
            {
                Interlocked.Increment(ref refreshCount);
                return TestResponses.Ok("{\"success\":true,\"data\":{\"jwt_token\":\"ACCESS2\",\"refresh_token\":\"REFRESH2\"}}");
            }
            // A business call 401s only while the caller still holds the stale token. Block here
            // until all 5 stale callers have arrived, forcing concurrent contention on the refresh
            // guard. The refresh (/refresh-token) and the ACCESS2 retries below never touch the gate.
            if (req.Headers.Authorization?.Parameter == "ACCESS")
            {
                gate.Signal();
                gate.Wait(TimeSpan.FromSeconds(5));
                return TestResponses.Json(HttpStatusCode.Unauthorized, "{\"success\":false}");
            }
            // Once refreshed to ACCESS2 (a valid token) the same call succeeds — as a real server behaves.
            return TestResponses.Ok("{\"success\":true,\"data\":0}");
        });

        // Seed the store so all 5 concurrent callers start with the SAME stale access token.
        store.SetTokens("ACCESS", "REFRESH");

        // Fire on real threads so all 5 can sit in the barrier at once; the synchronous stub would
        // otherwise block the single enumerating thread and the barrier could never fill.
        var tasks = Enumerable.Range(0, 5)
            .Select(_ => Task.Run(async () => await client.GetAsync<int>("/v3/bill/tracking?bill_code=X", default)))
            .ToArray();
        var results = await Task.WhenAll(tasks);

        Assert.True(gate.IsSet);                // all 5 stale-token callers reached the barrier concurrently
        Assert.Equal(1, refreshCount);          // only one refresh despite 5 concurrent 401s
        Assert.All(results, r => Assert.True(r.IsSuccess));
    }
}
```

- [ ] **Step 3: Run to verify it fails**

Run: `dotnet test CodeSDK/NhatTinLogisticsSdk.sln --filter FullyQualifiedName~NhatTinHttpClientTests`
Expected: FAIL — `NhatTinHttpClient` not found.

- [ ] **Step 4: Write `Http/NhatTinHttpClient.cs`**

```csharp
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using NhatTinLogistics.Sdk.Types.Responses;

namespace NhatTinLogistics.Sdk.Http;

/// <summary>Low-level HTTP layer: token management, snake_case serialization, envelope parsing, 401 retry.</summary>
public sealed class NhatTinHttpClient
{
    private readonly HttpClient _http;
    private readonly NhatTinLogisticsClientOptions _options;
    private readonly ITokenStore _tokens;
    private readonly SemaphoreSlim _authLock = new(1, 1);

    public NhatTinHttpClient(HttpClient http, NhatTinLogisticsClientOptions options, ITokenStore tokens)
    {
        _http = http;
        _options = options;
        _tokens = tokens;
        if (_http.BaseAddress is null)
            _http.BaseAddress = new Uri(options.ResolveBaseUrl());
    }

    public Task<NhatTinResponse<T>> PostAsync<T>(string path, object body, CancellationToken ct)
        => SendAsync<T>(HttpMethod.Post, path, body, authenticated: true, ct);

    public Task<NhatTinResponse<T>> GetAsync<T>(string path, CancellationToken ct)
        => SendAsync<T>(HttpMethod.Get, path, null, authenticated: true, ct);

    public async Task<NhatTinResponse<T>> SendAsync<T>(
        HttpMethod method, string path, object? body, bool authenticated, CancellationToken ct)
    {
        string? tokenUsed = null;
        if (authenticated)
        {
            await EnsureAuthenticatedAsync(ct).ConfigureAwait(false);
            tokenUsed = _tokens.AccessToken;
        }

        var response = await SendOnceAsync(method, path, body, tokenUsed, ct).ConfigureAwait(false);

        if (authenticated && response.StatusCode == HttpStatusCode.Unauthorized)
        {
            response.Dispose();
            await RefreshIfStaleAsync(tokenUsed, ct).ConfigureAwait(false);
            tokenUsed = _tokens.AccessToken;
            response = await SendOnceAsync(method, path, body, tokenUsed, ct).ConfigureAwait(false);
        }

        return await ReadResponseAsync<T>(response, ct).ConfigureAwait(false);
    }

    public async Task<byte[]> GetBytesAsync(string url, CancellationToken ct)
    {
        await EnsureAuthenticatedAsync(ct).ConfigureAwait(false);
        var response = await SendOnceAsync(HttpMethod.Get, url, null, _tokens.AccessToken, ct).ConfigureAwait(false);
        using (response)
        {
            if (!response.IsSuccessStatusCode)
                throw new NhatTinApiException($"Print request failed. Status {(int)response.StatusCode}.", (int)response.StatusCode);
            return await response.Content.ReadAsByteArrayAsync(ct).ConfigureAwait(false);
        }
    }

    private async Task<HttpResponseMessage> SendOnceAsync(
        HttpMethod method, string path, object? body, string? bearerToken, CancellationToken ct)
    {
        var request = new HttpRequestMessage(method, path);
        try
        {
            if (!string.IsNullOrEmpty(bearerToken))
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", bearerToken);
            if (body is not null)
            {
                var json = JsonSerializer.Serialize(body, body.GetType(), NhatTinJson.Options);
                request.Content = new StringContent(json, Encoding.UTF8, "application/json");
            }
            return await _http.SendAsync(request, ct).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            throw new NhatTinApiException($"HTTP request to '{path}' failed: {ex.Message}", 0, null, ex);
        }
        finally
        {
            request.Dispose();
        }
    }

    private static async Task<NhatTinResponse<T>> ReadResponseAsync<T>(HttpResponseMessage response, CancellationToken ct)
    {
        using (response)
        {
            var raw = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
            var status = (int)response.StatusCode;
            RawEnvelope<T>? env;
            try
            {
                env = string.IsNullOrWhiteSpace(raw)
                    ? null
                    : JsonSerializer.Deserialize<RawEnvelope<T>>(raw, NhatTinJson.Options);
            }
            catch (JsonException ex)
            {
                throw new NhatTinApiException($"Failed to parse NhatTin response as JSON. Status {status}.", status, raw, ex);
            }

            if (env is null)
                throw new NhatTinApiException($"Empty NhatTin response. Status {status}.", status, raw);

            return new NhatTinResponse<T>
            {
                Success = env.Success,
                Message = env.Message,
                Data = env.Data,
                HttpStatusCode = status,
                RawBody = raw,
            };
        }
    }

    private async Task EnsureAuthenticatedAsync(CancellationToken ct)
    {
        if (!_options.AutoAuthenticate) return;
        if (!string.IsNullOrEmpty(_tokens.AccessToken)) return;

        await _authLock.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            if (string.IsNullOrEmpty(_tokens.AccessToken))
                await SignInAsync(ct).ConfigureAwait(false);
        }
        finally { _authLock.Release(); }
    }

    private async Task RefreshIfStaleAsync(string? staleToken, CancellationToken ct)
    {
        await _authLock.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            // Another concurrent caller already refreshed → nothing to do.
            if (!string.IsNullOrEmpty(_tokens.AccessToken) && _tokens.AccessToken != staleToken)
                return;

            var refresh = _tokens.RefreshToken;
            if (!string.IsNullOrEmpty(refresh))
            {
                var res = await SendAsync<AuthToken>(
                    HttpMethod.Post, "/v1/auth/refresh-token", new { refresh_token = refresh }, authenticated: false, ct)
                    .ConfigureAwait(false);
                if (res.IsSuccess && res.Data is not null && !string.IsNullOrEmpty(res.Data.JwtToken))
                {
                    _tokens.SetTokens(res.Data.JwtToken, res.Data.RefreshToken);
                    return;
                }
            }
            // No refresh token or refresh failed → full sign-in.
            _tokens.Clear();
            await SignInAsync(ct).ConfigureAwait(false);
        }
        finally { _authLock.Release(); }
    }

    private async Task SignInAsync(CancellationToken ct)
    {
        var res = await SendAsync<AuthToken>(
            HttpMethod.Post, "/v1/auth/sign-in",
            new { username = _options.Username, password = _options.Password },
            authenticated: false, ct).ConfigureAwait(false);

        if (!res.IsSuccess || res.Data is null || string.IsNullOrEmpty(res.Data.JwtToken))
            throw new NhatTinApiException($"Sign-in failed: {res.Message}", res.HttpStatusCode, res.RawBody);

        _tokens.SetTokens(res.Data.JwtToken, res.Data.RefreshToken);
    }
}
```

> Note on concurrency: `EnsureAuthenticatedAsync` and `RefreshIfStaleAsync` both take `_authLock`. `SignInAsync`/refresh call `SendAsync(..., authenticated:false, ...)`, which never re-enters the lock — so there is no deadlock. `RefreshIfStaleAsync` compares the caller's stale token against the current one, so only the first of N concurrent 401s performs the refresh.

- [ ] **Step 5: Run to verify pass**

Run: `dotnet test CodeSDK/NhatTinLogisticsSdk.sln --filter FullyQualifiedName~NhatTinHttpClientTests`
Expected: PASS (4 tests).

- [ ] **Step 6: Commit**

```bash
git add CodeSDK/src CodeSDK/tests
git commit -m "feat(sdk): http client with envelope parsing + jwt auto sign-in/refresh"
```

---

### Task 6: AuthApi

**Files:**
- Create: `CodeSDK/src/NhatTinLogistics.Sdk/Client/IAuthApi.cs`
- Create: `CodeSDK/src/NhatTinLogistics.Sdk/Client/AuthApi.cs`
- Test: `CodeSDK/tests/NhatTinLogistics.Sdk.Tests/AuthApiTests.cs`

**Interfaces:**
- Consumes: `NhatTinHttpClient.SendAsync<AuthToken>`, `AuthToken`, `NhatTinResponse<T>`.
- Produces: `IAuthApi` { `Task<NhatTinResponse<AuthToken>> SignInAsync(string username, string password, CancellationToken ct = default)`, `Task<NhatTinResponse<AuthToken>> RefreshTokenAsync(string refreshToken, CancellationToken ct = default)` }; `AuthApi : IAuthApi`.

- [ ] **Step 1: Write the failing test** (`AuthApiTests.cs`)

```csharp
using System.Net.Http;
using NhatTinLogistics.Sdk;
using NhatTinLogistics.Sdk.Client;
using NhatTinLogistics.Sdk.Http;
using NhatTinLogistics.Sdk.Tests.Infrastructure;
using Xunit;

namespace NhatTinLogistics.Sdk.Tests;

public class AuthApiTests
{
    [Fact]
    public async Task SignInAsync_posts_credentials_and_maps_token()
    {
        var handler = new StubHttpMessageHandler(_ =>
            TestResponses.Ok("{\"success\":true,\"data\":{\"jwt_token\":\"A\",\"refresh_token\":\"R\",\"token_type\":\"Bearer\"}}"));
        var options = new NhatTinLogisticsClientOptions { AutoAuthenticate = false, BaseUrl = "https://test.local" };
        var http = new HttpClient(handler) { BaseAddress = new Uri(options.ResolveBaseUrl()) };
        var api = new AuthApi(new NhatTinHttpClient(http, options, new InMemoryTokenStore()));

        var resp = await api.SignInAsync("john", "secret");

        Assert.True(resp.IsSuccess);
        Assert.Equal("A", resp.Data!.JwtToken);
        Assert.Equal("R", resp.Data.RefreshToken);
        Assert.EndsWith("/v1/auth/sign-in", handler.Requests[0].RequestUri!.AbsolutePath);
        Assert.Contains("\"username\":\"john\"", handler.RequestBodies[0]);
        Assert.Contains("\"password\":\"secret\"", handler.RequestBodies[0]);
    }
}
```

- [ ] **Step 2: Run to verify it fails**

Run: `dotnet test CodeSDK/NhatTinLogisticsSdk.sln --filter FullyQualifiedName~AuthApiTests`
Expected: FAIL — `AuthApi` not found.

- [ ] **Step 3: Write `Client/IAuthApi.cs`**

```csharp
using NhatTinLogistics.Sdk.Http;
using NhatTinLogistics.Sdk.Types.Responses;

namespace NhatTinLogistics.Sdk.Client;

public interface IAuthApi
{
    Task<NhatTinResponse<AuthToken>> SignInAsync(string username, string password, CancellationToken ct = default);
    Task<NhatTinResponse<AuthToken>> RefreshTokenAsync(string refreshToken, CancellationToken ct = default);
}
```

- [ ] **Step 4: Write `Client/AuthApi.cs`**

```csharp
using System.Net.Http;
using NhatTinLogistics.Sdk.Http;
using NhatTinLogistics.Sdk.Types.Responses;

namespace NhatTinLogistics.Sdk.Client;

public sealed class AuthApi : IAuthApi
{
    private readonly NhatTinHttpClient _http;
    public AuthApi(NhatTinHttpClient http) => _http = http;

    public Task<NhatTinResponse<AuthToken>> SignInAsync(string username, string password, CancellationToken ct = default)
        => _http.SendAsync<AuthToken>(HttpMethod.Post, "/v1/auth/sign-in",
            new { username, password }, authenticated: false, ct);

    public Task<NhatTinResponse<AuthToken>> RefreshTokenAsync(string refreshToken, CancellationToken ct = default)
        => _http.SendAsync<AuthToken>(HttpMethod.Post, "/v1/auth/refresh-token",
            new { refresh_token = refreshToken }, authenticated: false, ct);
}
```

- [ ] **Step 5: Run to verify pass**

Run: `dotnet test CodeSDK/NhatTinLogisticsSdk.sln --filter FullyQualifiedName~AuthApiTests`
Expected: PASS.

- [ ] **Step 6: Commit**

```bash
git add CodeSDK/src CodeSDK/tests
git commit -m "feat(sdk): AuthApi (sign-in, refresh-token)"
```

---

### Task 7: BillApi — create & update

**Files:**
- Create: `CodeSDK/src/NhatTinLogistics.Sdk/Types/Requests/CreateBillRequest.cs`
- Create: `CodeSDK/src/NhatTinLogistics.Sdk/Types/Requests/UpdateBillRequest.cs`
- Create: `CodeSDK/src/NhatTinLogistics.Sdk/Types/Responses/BillResult.cs`
- Create: `CodeSDK/src/NhatTinLogistics.Sdk/Client/IBillApi.cs`
- Create: `CodeSDK/src/NhatTinLogistics.Sdk/Client/BillApi.cs`
- Test: `CodeSDK/tests/NhatTinLogistics.Sdk.Tests/BillApiCreateUpdateTests.cs`

**Interfaces:**
- Consumes: `NhatTinHttpClient.PostAsync`, `NhatTinLogisticsClientOptions.PartnerId`.
- Produces:
  - `CreateBillRequest`, `UpdateBillRequest`, `BillResult` (`BillStatus Status` computed from `StatusId`).
  - `IBillApi` with `CreateAsync(CreateBillRequest, CancellationToken=default)` and `UpdateAsync(UpdateBillRequest, CancellationToken=default)` returning `Task<NhatTinResponse<BillResult>>`. (More methods added in Tasks 8-9.)
  - `BillApi(NhatTinHttpClient http, NhatTinLogisticsClientOptions options)`.

- [ ] **Step 1: Write the failing test** (`BillApiCreateUpdateTests.cs`)

```csharp
using System.Net.Http;
using NhatTinLogistics.Sdk;
using NhatTinLogistics.Sdk.Client;
using NhatTinLogistics.Sdk.Http;
using NhatTinLogistics.Sdk.Tests.Infrastructure;
using NhatTinLogistics.Sdk.Types.Enums;
using NhatTinLogistics.Sdk.Types.Requests;
using Xunit;

namespace NhatTinLogistics.Sdk.Tests;

public class BillApiCreateUpdateTests
{
    private static (BillApi api, StubHttpMessageHandler handler) Build(
        Func<HttpRequestMessage, HttpResponseMessage> responder, int? partnerId = null)
    {
        var options = new NhatTinLogisticsClientOptions
        {
            AutoAuthenticate = false, BaseUrl = "https://test.local", PartnerId = partnerId
        };
        var handler = new StubHttpMessageHandler(responder);
        var http = new HttpClient(handler) { BaseAddress = new Uri(options.ResolveBaseUrl()) };
        return (new BillApi(new NhatTinHttpClient(http, options, new InMemoryTokenStore()), options), handler);
    }

    [Fact]
    public async Task CreateAsync_serializes_snake_case_and_maps_result()
    {
        var (api, handler) = Build(_ =>
            TestResponses.Ok("{\"success\":true,\"message\":\"Create bill successfully\",\"data\":{\"bill_code\":\"CP1\",\"status_id\":2}}"));

        var resp = await api.CreateAsync(new CreateBillRequest
        {
            Weight = 2, ServiceId = 91, PaymentMethodId = 10, CargoTypeId = 2,
            SName = "S", SPhone = "0333", SAddress = "a", SProvinceCode = "01", SWardCode = "00004",
            RName = "R", RPhone = "0333", RAddress = "b", RProvinceCode = "79", RWardCode = "25750",
        });

        Assert.True(resp.IsSuccess);
        Assert.Equal("CP1", resp.Data!.BillCode);
        Assert.Equal(BillStatus.WaitingPickup, resp.Data.Status);

        var body = handler.RequestBodies[0];
        Assert.EndsWith("/v3/bill/create", handler.Requests[0].RequestUri!.AbsolutePath);
        Assert.Contains("\"s_province_code\":\"01\"", body);
        Assert.Contains("\"payment_method_id\":10", body);
        Assert.DoesNotContain("ref_code", body); // null fields omitted
    }

    [Fact]
    public async Task UpdateAsync_defaults_partner_id_from_options_and_targets_update_shipping()
    {
        var (api, handler) = Build(_ =>
            TestResponses.Ok("{\"success\":true,\"data\":{\"bill_code\":\"CP1\"}}"), partnerId: 123736);

        await api.UpdateAsync(new UpdateBillRequest { BillCode = "CP1", CodAmount = 200000 });

        Assert.EndsWith("/v3/bill/update-shipping", handler.Requests[0].RequestUri!.AbsolutePath);
        Assert.Contains("\"partner_id\":123736", handler.RequestBodies[0]);
        Assert.Contains("\"bill_code\":\"CP1\"", handler.RequestBodies[0]);
    }
}
```

- [ ] **Step 2: Run to verify it fails**

Run: `dotnet test CodeSDK/NhatTinLogisticsSdk.sln --filter FullyQualifiedName~BillApiCreateUpdateTests`
Expected: FAIL — types not found.

- [ ] **Step 3: Write `Types/Requests/CreateBillRequest.cs`**

```csharp
using System.Text.Json.Serialization;

namespace NhatTinLogistics.Sdk.Types.Requests;

/// <summary>POST /v3/bill/create — fields per NhatTinAPIDocumentation/vi/bill/createbill.md.</summary>
public sealed class CreateBillRequest
{
    [JsonPropertyName("ref_code")] public string? RefCode { get; set; }
    [JsonPropertyName("package_no")] public int? PackageNo { get; set; }
    [JsonPropertyName("weight")] public double Weight { get; set; }
    [JsonPropertyName("width")] public double Width { get; set; }
    [JsonPropertyName("length")] public double Length { get; set; }
    [JsonPropertyName("height")] public double Height { get; set; }
    [JsonPropertyName("cargo_content")] public string? CargoContent { get; set; }
    [JsonPropertyName("service_id")] public int ServiceId { get; set; }
    [JsonPropertyName("payment_method_id")] public int PaymentMethodId { get; set; }
    [JsonPropertyName("is_return_doc")] public int? IsReturnDoc { get; set; }
    [JsonPropertyName("cod_amount")] public decimal? CodAmount { get; set; }
    [JsonPropertyName("note")] public string? Note { get; set; }
    [JsonPropertyName("cargo_value")] public double? CargoValue { get; set; }
    [JsonPropertyName("cargo_type_id")] public int CargoTypeId { get; set; }
    [JsonPropertyName("s_name")] public string SName { get; set; } = "";
    [JsonPropertyName("s_phone")] public string SPhone { get; set; } = "";
    [JsonPropertyName("s_address")] public string SAddress { get; set; } = "";
    [JsonPropertyName("s_province_code")] public string SProvinceCode { get; set; } = "";
    [JsonPropertyName("s_ward_code")] public string SWardCode { get; set; } = "";
    [JsonPropertyName("is_return_org")] public int? IsReturnOrg { get; set; }
    [JsonPropertyName("return_name")] public string? ReturnName { get; set; }
    [JsonPropertyName("return_phone")] public string? ReturnPhone { get; set; }
    [JsonPropertyName("return_address")] public string? ReturnAddress { get; set; }
    [JsonPropertyName("return_province_code")] public string? ReturnProvinceCode { get; set; }
    [JsonPropertyName("return_ward_code")] public string? ReturnWardCode { get; set; }
    [JsonPropertyName("r_name")] public string RName { get; set; } = "";
    [JsonPropertyName("r_phone")] public string RPhone { get; set; } = "";
    [JsonPropertyName("r_address")] public string RAddress { get; set; } = "";
    [JsonPropertyName("r_province_code")] public string RProvinceCode { get; set; } = "";
    [JsonPropertyName("r_ward_code")] public string RWardCode { get; set; } = "";
    [JsonPropertyName("is_draft")] public int? IsDraft { get; set; }
    [JsonPropertyName("other_fee")] public decimal? OtherFee { get; set; }
    [JsonPropertyName("is_installation")] public int? IsInstallation { get; set; }
    [JsonPropertyName("bill_type")] public int? BillType { get; set; }
    [JsonPropertyName("bill_return")] public string? BillReturn { get; set; }
}
```

- [ ] **Step 4: Write `Types/Requests/UpdateBillRequest.cs`**

```csharp
using System.Text.Json.Serialization;

namespace NhatTinLogistics.Sdk.Types.Requests;

/// <summary>POST /v3/bill/update-shipping — per updatebill.md. PartnerId defaults from options when null.</summary>
public sealed class UpdateBillRequest
{
    [JsonPropertyName("partner_id")] public int? PartnerId { get; set; }
    [JsonPropertyName("bill_code")] public string BillCode { get; set; } = "";
    [JsonPropertyName("cod_amount")] public decimal? CodAmount { get; set; }
    [JsonPropertyName("cargo_value")] public double? CargoValue { get; set; }
    [JsonPropertyName("weight")] public double? Weight { get; set; }
    [JsonPropertyName("length")] public double? Length { get; set; }
    [JsonPropertyName("height")] public double? Height { get; set; }
    [JsonPropertyName("width")] public double? Width { get; set; }
    [JsonPropertyName("cargo_content")] public string? CargoContent { get; set; }
    [JsonPropertyName("receiver_phone")] public string? ReceiverPhone { get; set; }
    [JsonPropertyName("receiver_name")] public string? ReceiverName { get; set; }
    [JsonPropertyName("receiver_address")] public string? ReceiverAddress { get; set; }
    [JsonPropertyName("package_no")] public int? PackageNo { get; set; }
    [JsonPropertyName("is_return_doc")] public int? IsReturnDoc { get; set; }
    [JsonPropertyName("note")] public string? Note { get; set; }
    [JsonPropertyName("is_installation")] public int? IsInstallation { get; set; }
}
```

- [ ] **Step 5: Write `Types/Responses/BillResult.cs`**

```csharp
using System.Text.Json.Serialization;
using NhatTinLogistics.Sdk.Types.Enums;

namespace NhatTinLogistics.Sdk.Types.Responses;

/// <summary>data of /v3/bill/create and /v3/bill/update-shipping.</summary>
public sealed class BillResult
{
    [JsonPropertyName("bill_id")] public int BillId { get; set; }
    [JsonPropertyName("bill_code")] public string BillCode { get; set; } = "";
    [JsonPropertyName("ref_code")] public string? RefCode { get; set; }
    [JsonPropertyName("status_id")] public int StatusId { get; set; }
    [JsonPropertyName("cod_amount")] public decimal CodAmount { get; set; }
    [JsonPropertyName("service_id")] public int ServiceId { get; set; }
    [JsonPropertyName("payment_method")] public int PaymentMethod { get; set; }
    [JsonPropertyName("created_at")] public string? CreatedAt { get; set; }
    [JsonPropertyName("main_fee")] public decimal MainFee { get; set; }
    [JsonPropertyName("cod_fee")] public decimal CodFee { get; set; }
    [JsonPropertyName("insurr_fee")] public decimal InsurrFee { get; set; }
    [JsonPropertyName("lifting_fee")] public decimal LiftingFee { get; set; }
    [JsonPropertyName("remote_fee")] public decimal RemoteFee { get; set; }
    [JsonPropertyName("counting_fee")] public decimal CountingFee { get; set; }
    [JsonPropertyName("packing_fee")] public decimal PackingFee { get; set; }
    [JsonPropertyName("total_fee")] public decimal TotalFee { get; set; }
    [JsonPropertyName("expected_at")] public string? ExpectedAt { get; set; }
    [JsonPropertyName("partner_address_id")] public int PartnerAddressId { get; set; }
    [JsonPropertyName("receiver_name")] public string? ReceiverName { get; set; }
    [JsonPropertyName("receiver_phone")] public string? ReceiverPhone { get; set; }
    [JsonPropertyName("receiver_address")] public string? ReceiverAddress { get; set; }
    [JsonPropertyName("package_no")] public int PackageNo { get; set; }
    [JsonPropertyName("weight")] public double Weight { get; set; }
    [JsonPropertyName("cargo_content")] public string? CargoContent { get; set; }
    [JsonPropertyName("cargo_value")] public double CargoValue { get; set; }
    [JsonPropertyName("note")] public string? Note { get; set; }

    [JsonIgnore] public BillStatus Status => StatusId.ToBillStatus();
}
```

- [ ] **Step 6: Write `Client/IBillApi.cs`** (full contract — later tasks implement the rest; declaring now keeps the interface stable)

```csharp
using NhatTinLogistics.Sdk.Http;
using NhatTinLogistics.Sdk.Types.Requests;
using NhatTinLogistics.Sdk.Types.Responses;

namespace NhatTinLogistics.Sdk.Client;

public interface IBillApi
{
    Task<NhatTinResponse<BillResult>> CreateAsync(CreateBillRequest request, CancellationToken ct = default);
    Task<NhatTinResponse<BillResult>> UpdateAsync(UpdateBillRequest request, CancellationToken ct = default);
    // Tasks 8 and 9 extend this interface (calc-fee/cancel/revert/tracking, then print).
}
```

- [ ] **Step 7: Write `Client/BillApi.cs`**

```csharp
using NhatTinLogistics.Sdk.Http;
using NhatTinLogistics.Sdk.Types.Requests;
using NhatTinLogistics.Sdk.Types.Responses;

namespace NhatTinLogistics.Sdk.Client;

public sealed class BillApi : IBillApi
{
    private readonly NhatTinHttpClient _http;
    private readonly NhatTinLogisticsClientOptions _options;

    public BillApi(NhatTinHttpClient http, NhatTinLogisticsClientOptions options)
    {
        _http = http;
        _options = options;
    }

    public Task<NhatTinResponse<BillResult>> CreateAsync(CreateBillRequest request, CancellationToken ct = default)
        => _http.PostAsync<BillResult>("/v3/bill/create", request, ct);

    public Task<NhatTinResponse<BillResult>> UpdateAsync(UpdateBillRequest request, CancellationToken ct = default)
    {
        request.PartnerId ??= _options.PartnerId;
        return _http.PostAsync<BillResult>("/v3/bill/update-shipping", request, ct);
    }
}
```

> Tasks 8 and 9 grow `IBillApi` and `BillApi` incrementally, each adding its methods together with the DTOs and tests that cover them — so there are no `NotImplementedException` bodies or empty stub types in any intermediate commit.

- [ ] **Step 8: Run to verify pass**

Run: `dotnet test CodeSDK/NhatTinLogisticsSdk.sln --filter FullyQualifiedName~BillApiCreateUpdateTests`
Expected: PASS (2 tests).

- [ ] **Step 9: Commit**

```bash
git add CodeSDK/src CodeSDK/tests
git commit -m "feat(sdk): BillApi create + update-shipping with typed DTOs"
```

---

### Task 8: BillApi — calc-fee, cancel, revert, tracking

**Files:**
- Create: `CodeSDK/src/NhatTinLogistics.Sdk/Types/Requests/CalcFeeRequest.cs`, `Types/Responses/FeeOption.cs`, `Types/Responses/CancelResult.cs`, `Types/Responses/RevertResult.cs`, `Types/Responses/TrackingResult.cs`
- Modify: `CodeSDK/src/NhatTinLogistics.Sdk/Client/IBillApi.cs` (add four method signatures)
- Modify: `CodeSDK/src/NhatTinLogistics.Sdk/Client/BillApi.cs` (implement the four methods)
- Test: `CodeSDK/tests/NhatTinLogistics.Sdk.Tests/BillApiOpsTests.cs`

**Interfaces:**
- Consumes: `IBillApi` (Create/Update from Task 7).
- Produces: `CalcFeeRequest`, `FeeOption`, `CancelResult` (`DoCode`, `Message`), `RevertResult` (`Success`, `Failed` string lists), `TrackingResult` (+ `TrackingHistory`); `IBillApi`/`BillApi` gain `CalcFeeAsync`/`CancelAsync`/`RevertAsync`/`TrackingAsync`.

- [ ] **Step 1: Write the failing test** (`BillApiOpsTests.cs`)

```csharp
using System.Net.Http;
using NhatTinLogistics.Sdk;
using NhatTinLogistics.Sdk.Client;
using NhatTinLogistics.Sdk.Http;
using NhatTinLogistics.Sdk.Tests.Infrastructure;
using NhatTinLogistics.Sdk.Types.Requests;
using Xunit;

namespace NhatTinLogistics.Sdk.Tests;

public class BillApiOpsTests
{
    private static (BillApi api, StubHttpMessageHandler handler) Build(
        Func<HttpRequestMessage, HttpResponseMessage> responder, int? partnerId = null)
    {
        var options = new NhatTinLogisticsClientOptions { AutoAuthenticate = false, BaseUrl = "https://test.local", PartnerId = partnerId };
        var handler = new StubHttpMessageHandler(responder);
        var http = new HttpClient(handler) { BaseAddress = new Uri(options.ResolveBaseUrl()) };
        return (new BillApi(new NhatTinHttpClient(http, options, new InMemoryTokenStore()), options), handler);
    }

    [Fact]
    public async Task CancelAsync_posts_bill_code_array_and_maps_doCode()
    {
        var (api, handler) = Build(_ =>
            TestResponses.Ok("{\"success\":true,\"data\":[{\"doCode\":\"E9\",\"message\":\"ok\"}]}"));

        var resp = await api.CancelAsync(new[] { "CP1" });

        Assert.True(resp.IsSuccess);
        Assert.Equal("E9", resp.Data![0].DoCode);
        Assert.EndsWith("/v3/bill/destroy", handler.Requests[0].RequestUri!.AbsolutePath);
        Assert.Contains("\"bill_code\":[\"CP1\"]", handler.RequestBodies[0]);
    }

    [Fact]
    public async Task RevertAsync_maps_success_and_failed_lists()
    {
        var (api, handler) = Build(_ =>
            TestResponses.Ok("{\"success\":true,\"data\":{\"success\":[\"CP1\"],\"failed\":[]}}"));

        var resp = await api.RevertAsync(new[] { "CP1" });

        Assert.Single(resp.Data!.Success);
        Assert.Empty(resp.Data.Failed);
        Assert.EndsWith("/v3/bill/revert-bill", handler.Requests[0].RequestUri!.AbsolutePath);
    }

    [Fact]
    public async Task CalcFeeAsync_defaults_partner_id_and_maps_options()
    {
        var (api, handler) = Build(_ =>
            TestResponses.Ok("{\"success\":true,\"data\":[{\"service_id\":21,\"service_name\":\"MES\",\"total_fee\":109910}]}"),
            partnerId: 123736);

        var resp = await api.CalcFeeAsync(new CalcFeeRequest { Weight = 1.3, PaymentMethodId = 10, SProvinceId = "79", SWardId = "27007", RProvinceId = "01", RWardId = "00004" });

        Assert.Equal(21, resp.Data![0].ServiceId);
        Assert.Equal(109910m, resp.Data[0].TotalFee);
        Assert.EndsWith("/v3/bill/calc-fee", handler.Requests[0].RequestUri!.AbsolutePath);
        Assert.Contains("\"partner_id\":123736", handler.RequestBodies[0]);
    }

    [Fact]
    public async Task TrackingAsync_gets_with_query_and_tolerates_string_numbers()
    {
        var (api, handler) = Build(_ =>
            TestResponses.Ok("{\"success\":true,\"data\":[{\"bill_code\":\"CP1\",\"weight\":\"1.00\",\"total_fee\":\"20000\",\"bill_status_id\":4}]}"));

        var resp = await api.TrackingAsync("CP1");

        Assert.True(resp.IsSuccess);
        Assert.Equal("CP1", resp.Data![0].BillCode);
        Assert.Equal("20000", resp.Data[0].TotalFee); // numbers-as-strings preserved
        Assert.Equal(4, resp.Data[0].BillStatusId);
        Assert.Equal("/v3/bill/tracking", handler.Requests[0].RequestUri!.AbsolutePath);
        Assert.Contains("bill_code=CP1", handler.Requests[0].RequestUri!.Query);
    }
}
```

- [ ] **Step 2: Run to verify it fails**

Run: `dotnet test CodeSDK/NhatTinLogisticsSdk.sln --filter FullyQualifiedName~BillApiOpsTests`
Expected: FAIL — `CalcFeeRequest`/`FeeOption`/`CancelResult`/`RevertResult`/`TrackingResult` and the four `BillApi` methods do not exist.

- [ ] **Step 3: Write `Types/Requests/CalcFeeRequest.cs`**

```csharp
using System.Text.Json.Serialization;

namespace NhatTinLogistics.Sdk.Types.Requests;

/// <summary>POST /v3/bill/calc-fee — per calcfee.md. New admin uses *_id fields; legacy uses province/district names.</summary>
public sealed class CalcFeeRequest
{
    [JsonPropertyName("partner_id")] public int? PartnerId { get; set; }
    [JsonPropertyName("weight")] public double Weight { get; set; }
    [JsonPropertyName("width")] public double? Width { get; set; }
    [JsonPropertyName("length")] public double? Length { get; set; }
    [JsonPropertyName("height")] public double? Height { get; set; }
    [JsonPropertyName("service_id")] public int? ServiceId { get; set; }
    [JsonPropertyName("payment_method_id")] public int PaymentMethodId { get; set; }
    [JsonPropertyName("cod_amount")] public double? CodAmount { get; set; }
    [JsonPropertyName("cargo_value")] public double? CargoValue { get; set; }
    // New administrative units (after 2025-07-01)
    [JsonPropertyName("s_province_id")] public string? SProvinceId { get; set; }
    [JsonPropertyName("s_ward_id")] public string? SWardId { get; set; }
    [JsonPropertyName("r_province_id")] public string? RProvinceId { get; set; }
    [JsonPropertyName("r_ward_id")] public string? RWardId { get; set; }
    // Legacy administrative units
    [JsonPropertyName("s_province")] public string? SProvince { get; set; }
    [JsonPropertyName("s_district")] public string? SDistrict { get; set; }
    [JsonPropertyName("r_province")] public string? RProvince { get; set; }
    [JsonPropertyName("r_district")] public string? RDistrict { get; set; }
}
```

- [ ] **Step 4: Write `Types/Responses/FeeOption.cs`**

```csharp
using System.Text.Json.Serialization;

namespace NhatTinLogistics.Sdk.Types.Responses;

public sealed class FeeOption
{
    [JsonPropertyName("weight")] public double Weight { get; set; }
    [JsonPropertyName("total_fee")] public decimal TotalFee { get; set; }
    [JsonPropertyName("main_fee")] public decimal MainFee { get; set; }
    [JsonPropertyName("insur_fee")] public decimal InsurFee { get; set; }
    [JsonPropertyName("remote_fee")] public decimal RemoteFee { get; set; }
    [JsonPropertyName("cod_fee")] public decimal CodFee { get; set; }
    [JsonPropertyName("service_id")] public int ServiceId { get; set; }
    [JsonPropertyName("service_name")] public string? ServiceName { get; set; }
    [JsonPropertyName("lead_time")] public string? LeadTime { get; set; }
}
```

- [ ] **Step 5: Write `Types/Responses/CancelResult.cs`**

```csharp
using System.Text.Json.Serialization;

namespace NhatTinLogistics.Sdk.Types.Responses;

/// <summary>Element of /v3/bill/destroy data. Note the documented camelCase "doCode".</summary>
public sealed class CancelResult
{
    [JsonPropertyName("doCode")] public string DoCode { get; set; } = "";
    [JsonPropertyName("message")] public string? Message { get; set; }
}
```

- [ ] **Step 6: Write `Types/Responses/RevertResult.cs`**

```csharp
using System.Text.Json.Serialization;

namespace NhatTinLogistics.Sdk.Types.Responses;

public sealed class RevertResult
{
    [JsonPropertyName("success")] public List<string> Success { get; set; } = new();
    [JsonPropertyName("failed")] public List<string> Failed { get; set; } = new();
}
```

- [ ] **Step 7: Write `Types/Responses/TrackingResult.cs`** (numeric fields are strings on the wire — keep them as `string?`)

```csharp
using System.Text.Json.Serialization;

namespace NhatTinLogistics.Sdk.Types.Responses;

/// <summary>Element of /v3/bill/tracking data. Numeric fields arrive as strings; kept as string? to avoid parse errors.</summary>
public sealed class TrackingResult
{
    [JsonPropertyName("bill_code")] public string BillCode { get; set; } = "";
    [JsonPropertyName("ref_code")] public string? RefCode { get; set; }
    [JsonPropertyName("weight")] public string? Weight { get; set; }
    [JsonPropertyName("dimension_weight")] public string? DimensionWeight { get; set; }
    [JsonPropertyName("width")] public string? Width { get; set; }
    [JsonPropertyName("length")] public string? Length { get; set; }
    [JsonPropertyName("height")] public string? Height { get; set; }
    [JsonPropertyName("payment_status")] public string? PaymentStatus { get; set; }
    [JsonPropertyName("payment_at")] public string? PaymentAt { get; set; }
    [JsonPropertyName("bill_status_id")] public int BillStatusId { get; set; }
    [JsonPropertyName("bill_status_desc")] public string? BillStatusDesc { get; set; }
    [JsonPropertyName("date_pickup")] public string? DatePickup { get; set; }
    [JsonPropertyName("pay_method")] public string? PayMethod { get; set; }
    [JsonPropertyName("service")] public string? Service { get; set; }
    [JsonPropertyName("cod_amt")] public string? CodAmount { get; set; }
    [JsonPropertyName("cod_fee")] public string? CodFee { get; set; }
    [JsonPropertyName("date_expected")] public string? DateExpected { get; set; }
    [JsonPropertyName("description")] public string? Description { get; set; }
    [JsonPropertyName("cargo_content")] public string? CargoContent { get; set; }
    [JsonPropertyName("insurance_fee")] public string? InsuranceFee { get; set; }
    [JsonPropertyName("counting_fee")] public string? CountingFee { get; set; }
    [JsonPropertyName("lifting_fee")] public string? LiftingFee { get; set; }
    [JsonPropertyName("packing_fee")] public string? PackingFee { get; set; }
    [JsonPropertyName("delivery_fee")] public string? DeliveryFee { get; set; }
    [JsonPropertyName("other_fee")] public string? OtherFee { get; set; }
    [JsonPropertyName("remote_fee")] public string? RemoteFee { get; set; }
    [JsonPropertyName("main_fee")] public string? MainFee { get; set; }
    [JsonPropertyName("total_fee")] public string? TotalFee { get; set; }
    [JsonPropertyName("sender_name")] public string? SenderName { get; set; }
    [JsonPropertyName("sender_phone")] public string? SenderPhone { get; set; }
    [JsonPropertyName("sender_address")] public string? SenderAddress { get; set; }
    [JsonPropertyName("sender_ward")] public string? SenderWard { get; set; }
    [JsonPropertyName("sender_district")] public string? SenderDistrict { get; set; }
    [JsonPropertyName("sender_province")] public string? SenderProvince { get; set; }
    [JsonPropertyName("receiver_name")] public string? ReceiverName { get; set; }
    [JsonPropertyName("receiver_phone")] public string? ReceiverPhone { get; set; }
    [JsonPropertyName("receiver_address")] public string? ReceiverAddress { get; set; }
    [JsonPropertyName("receiver_ward")] public string? ReceiverWard { get; set; }
    [JsonPropertyName("receiver_district")] public string? ReceiverDistrict { get; set; }
    [JsonPropertyName("receiver_province")] public string? ReceiverProvince { get; set; }
    [JsonPropertyName("date_delivery")] public string? DateDelivery { get; set; }
    [JsonPropertyName("note")] public string? Note { get; set; }
    [JsonPropertyName("histories")] public List<TrackingHistory> Histories { get; set; } = new();
    [JsonPropertyName("p_link_image")] public string? PLinkImage { get; set; }
    [JsonPropertyName("bill_image_link")] public List<string> BillImageLink { get; set; } = new();
    [JsonPropertyName("document_image_link")] public List<string> DocumentImageLink { get; set; } = new();
}

public sealed class TrackingHistory
{
    [JsonPropertyName("sequence")] public int Sequence { get; set; }
    [JsonPropertyName("log_status")] public string? LogStatus { get; set; }
    [JsonPropertyName("city")] public string? City { get; set; }
    [JsonPropertyName("district")] public string? District { get; set; }
    [JsonPropertyName("operation_en")] public string? OperationEn { get; set; }
    [JsonPropertyName("operationID")] public long OperationId { get; set; }
    [JsonPropertyName("operationType")] public string? OperationType { get; set; }
    [JsonPropertyName("delayReason")] public string? DelayReason { get; set; }
    [JsonPropertyName("operation")] public string? Operation { get; set; }
    [JsonPropertyName("loc_time")] public string? LocTime { get; set; }
}
```

- [ ] **Step 8: Extend `IBillApi`, then implement in `BillApi`** — first add the four signatures to the interface, then the matching methods to the class. (`List<>` and LINQ are covered by ImplicitUsings.)

Add these four lines inside the `IBillApi` interface body in `Client/IBillApi.cs` (replacing the `// Tasks 8 and 9 extend this interface` comment):

```csharp
    Task<NhatTinResponse<List<CancelResult>>> CancelAsync(IEnumerable<string> billCodes, CancellationToken ct = default);
    Task<NhatTinResponse<List<FeeOption>>> CalcFeeAsync(CalcFeeRequest request, CancellationToken ct = default);
    Task<NhatTinResponse<RevertResult>> RevertAsync(IEnumerable<string> billCodes, CancellationToken ct = default);
    Task<NhatTinResponse<List<TrackingResult>>> TrackingAsync(string billCode, CancellationToken ct = default);
    // Task 9 adds GetPrintUrl + PrintAsync.
```

Then add the implementations inside the `BillApi` class body in `Client/BillApi.cs`:

```csharp
    public Task<NhatTinResponse<List<CancelResult>>> CancelAsync(IEnumerable<string> billCodes, CancellationToken ct = default)
        => _http.PostAsync<List<CancelResult>>("/v3/bill/destroy", new { bill_code = billCodes.ToArray() }, ct);

    public Task<NhatTinResponse<List<FeeOption>>> CalcFeeAsync(CalcFeeRequest request, CancellationToken ct = default)
    {
        request.PartnerId ??= _options.PartnerId;
        return _http.PostAsync<List<FeeOption>>("/v3/bill/calc-fee", request, ct);
    }

    public Task<NhatTinResponse<RevertResult>> RevertAsync(IEnumerable<string> billCodes, CancellationToken ct = default)
        => _http.PostAsync<RevertResult>("/v3/bill/revert-bill", new { bill_code = billCodes.ToArray() }, ct);

    public Task<NhatTinResponse<List<TrackingResult>>> TrackingAsync(string billCode, CancellationToken ct = default)
        => _http.GetAsync<List<TrackingResult>>($"/v3/bill/tracking?bill_code={Uri.EscapeDataString(billCode)}", ct);
```

- [ ] **Step 9: Run to verify pass**

Run: `dotnet test CodeSDK/NhatTinLogisticsSdk.sln --filter FullyQualifiedName~BillApiOpsTests`
Expected: PASS (4 tests).

- [ ] **Step 10: Commit**

```bash
git add CodeSDK/src CodeSDK/tests
git commit -m "feat(sdk): BillApi calc-fee, cancel, revert, tracking"
```

---

### Task 9: BillApi print + LocationApi

**Files:**
- Modify: `CodeSDK/src/NhatTinLogistics.Sdk/Client/IBillApi.cs` (add `GetPrintUrl` + `PrintAsync` signatures)
- Modify: `CodeSDK/src/NhatTinLogistics.Sdk/Client/BillApi.cs` (implement `GetPrintUrl` + `PrintAsync`)
- Create: `CodeSDK/src/NhatTinLogistics.Sdk/Types/Responses/LocationDtos.cs`
- Create: `CodeSDK/src/NhatTinLogistics.Sdk/Client/ILocationApi.cs`
- Create: `CodeSDK/src/NhatTinLogistics.Sdk/Client/LocationApi.cs`
- Test: `CodeSDK/tests/NhatTinLogistics.Sdk.Tests/PrintAndLocationTests.cs`

**Interfaces:**
- Produces:
  - `BillApi.GetPrintUrl(billCode, partnerId?)` → absolute URL; `PrintAsync(...)` → bytes (best-effort).
  - `ProvinceDto` (`Id`,`ProvinceName`,`IsNew`), `DistrictDto` (`Id`,`DistrictName`,`IsNew`), `WardDto` (`Id`,`WardName`,`IsNew`).
  - `ILocationApi` { `GetProvincesAsync(bool isNew, ct)`, `GetDistrictsAsync(string provinceId, ct)`, `GetWardsAsync(string? districtId, string? provinceId, bool isNew, ct)` }; `LocationApi(NhatTinHttpClient)`.

- [ ] **Step 1: Write the failing test** (`PrintAndLocationTests.cs`)

```csharp
using System.Net.Http;
using NhatTinLogistics.Sdk;
using NhatTinLogistics.Sdk.Client;
using NhatTinLogistics.Sdk.Http;
using NhatTinLogistics.Sdk.Tests.Infrastructure;
using Xunit;

namespace NhatTinLogistics.Sdk.Tests;

public class PrintAndLocationTests
{
    private static (NhatTinHttpClient http, StubHttpMessageHandler handler, NhatTinLogisticsClientOptions options) Build(
        Func<HttpRequestMessage, HttpResponseMessage> responder, int? partnerId = null)
    {
        var options = new NhatTinLogisticsClientOptions { AutoAuthenticate = false, BaseUrl = "https://test.local", PartnerId = partnerId };
        var handler = new StubHttpMessageHandler(responder);
        var http = new HttpClient(handler) { BaseAddress = new Uri(options.ResolveBaseUrl()) };
        return (new NhatTinHttpClient(http, options, new InMemoryTokenStore()), handler, options);
    }

    [Fact]
    public void GetPrintUrl_builds_expected_url()
    {
        var (http, _, options) = Build(_ => TestResponses.Ok("{}"), partnerId: 123736);
        var api = new BillApi(http, options);

        var url = api.GetPrintUrl("CP1");

        Assert.Equal("https://test.local/v3/bill/print?do_code=CP1&partner_id=123736", url);
    }

    [Fact]
    public void GetPrintUrl_throws_when_no_partner_id()
    {
        var (http, _, options) = Build(_ => TestResponses.Ok("{}"));
        var api = new BillApi(http, options);
        Assert.Throws<ArgumentException>(() => api.GetPrintUrl("CP1"));
    }

    [Fact]
    public async Task GetProvincesAsync_sends_is_new_and_maps()
    {
        var (http, handler, _) = Build(_ =>
            TestResponses.Ok("{\"success\":true,\"data\":[{\"id\":\"11\",\"province_name\":\"Cao Bằng\",\"is_new\":\"N\"}]}"));
        var api = new LocationApi(http);

        var resp = await api.GetProvincesAsync(isNew: true);

        Assert.Equal("11", resp.Data![0].Id);
        Assert.Equal("Cao Bằng", resp.Data[0].ProvinceName);
        Assert.Equal("/v3/loc/provinces", handler.Requests[0].RequestUri!.AbsolutePath);
        Assert.Contains("is_new=1", handler.Requests[0].RequestUri!.Query);
    }

    [Fact]
    public async Task GetWardsAsync_includes_optional_params()
    {
        var (http, handler, _) = Build(_ =>
            TestResponses.Ok("{\"success\":true,\"data\":[{\"id\":\"01106\",\"ward_name\":\"P.Hồng Gai\",\"is_new\":\"N\"}]}"));
        var api = new LocationApi(http);

        await api.GetWardsAsync(districtId: null, provinceId: "01", isNew: true);

        var q = handler.Requests[0].RequestUri!.Query;
        Assert.Contains("is_new=1", q);
        Assert.Contains("province_id=01", q);
        Assert.DoesNotContain("district_id", q);
    }
}
```

- [ ] **Step 2: Run to verify it fails**

Run: `dotnet test CodeSDK/NhatTinLogisticsSdk.sln --filter FullyQualifiedName~PrintAndLocationTests`
Expected: FAIL — `LocationApi`/DTOs missing; `GetPrintUrl`/`PrintAsync` not on `BillApi`.

- [ ] **Step 3: Add print to `IBillApi` + implement in `BillApi`** — add the two signatures to the interface (replacing the `// Task 9 adds GetPrintUrl + PrintAsync.` comment), then add the methods to the class.

Add to `Client/IBillApi.cs`:

```csharp
    string GetPrintUrl(string billCode, int? partnerId = null);
    Task<byte[]> PrintAsync(string billCode, int? partnerId = null, CancellationToken ct = default);
```

Add to `Client/BillApi.cs`:

```csharp
    public string GetPrintUrl(string billCode, int? partnerId = null)
    {
        var pid = partnerId ?? _options.PartnerId
            ?? throw new ArgumentException("PartnerId is required for printing. Set Options.PartnerId or pass partnerId.");
        var baseUrl = _options.ResolveBaseUrl();
        return $"{baseUrl}/v3/bill/print?do_code={Uri.EscapeDataString(billCode)}&partner_id={pid}";
    }

    public Task<byte[]> PrintAsync(string billCode, int? partnerId = null, CancellationToken ct = default)
        // Best-effort: NhatTin's print host/format is not fully confirmed (see spec §10/§14).
        => _http.GetBytesAsync(GetPrintUrl(billCode, partnerId), ct);
```

- [ ] **Step 4: Write `Types/Responses/LocationDtos.cs`**

```csharp
using System.Text.Json.Serialization;

namespace NhatTinLogistics.Sdk.Types.Responses;

public sealed class ProvinceDto
{
    [JsonPropertyName("id")] public string Id { get; set; } = "";
    [JsonPropertyName("province_name")] public string? ProvinceName { get; set; }
    [JsonPropertyName("is_new")] public string? IsNew { get; set; }
}

public sealed class DistrictDto
{
    [JsonPropertyName("id")] public string Id { get; set; } = "";
    [JsonPropertyName("district_name")] public string? DistrictName { get; set; }
    [JsonPropertyName("is_new")] public string? IsNew { get; set; }
}

public sealed class WardDto
{
    [JsonPropertyName("id")] public string Id { get; set; } = "";
    [JsonPropertyName("ward_name")] public string? WardName { get; set; }
    [JsonPropertyName("is_new")] public string? IsNew { get; set; }
}
```

- [ ] **Step 5: Write `Client/ILocationApi.cs`**

```csharp
using NhatTinLogistics.Sdk.Http;
using NhatTinLogistics.Sdk.Types.Responses;

namespace NhatTinLogistics.Sdk.Client;

public interface ILocationApi
{
    Task<NhatTinResponse<List<ProvinceDto>>> GetProvincesAsync(bool isNew, CancellationToken ct = default);
    Task<NhatTinResponse<List<DistrictDto>>> GetDistrictsAsync(string provinceId, CancellationToken ct = default);
    Task<NhatTinResponse<List<WardDto>>> GetWardsAsync(string? districtId, string? provinceId, bool isNew, CancellationToken ct = default);
}
```

- [ ] **Step 6: Write `Client/LocationApi.cs`**

```csharp
using NhatTinLogistics.Sdk.Http;
using NhatTinLogistics.Sdk.Types.Responses;

namespace NhatTinLogistics.Sdk.Client;

public sealed class LocationApi : ILocationApi
{
    private readonly NhatTinHttpClient _http;
    public LocationApi(NhatTinHttpClient http) => _http = http;

    public Task<NhatTinResponse<List<ProvinceDto>>> GetProvincesAsync(bool isNew, CancellationToken ct = default)
        => _http.GetAsync<List<ProvinceDto>>($"/v3/loc/provinces?is_new={(isNew ? 1 : 0)}", ct);

    public Task<NhatTinResponse<List<DistrictDto>>> GetDistrictsAsync(string provinceId, CancellationToken ct = default)
        => _http.GetAsync<List<DistrictDto>>($"/v3/loc/districts?province_id={Uri.EscapeDataString(provinceId)}", ct);

    public Task<NhatTinResponse<List<WardDto>>> GetWardsAsync(string? districtId, string? provinceId, bool isNew, CancellationToken ct = default)
    {
        var q = new List<string> { $"is_new={(isNew ? 1 : 0)}" };
        if (!string.IsNullOrEmpty(districtId)) q.Add($"district_id={Uri.EscapeDataString(districtId)}");
        if (!string.IsNullOrEmpty(provinceId)) q.Add($"province_id={Uri.EscapeDataString(provinceId)}");
        return _http.GetAsync<List<WardDto>>($"/v3/loc/wards?{string.Join("&", q)}", ct);
    }
}
```

- [ ] **Step 7: Run to verify pass**

Run: `dotnet test CodeSDK/NhatTinLogisticsSdk.sln --filter FullyQualifiedName~PrintAndLocationTests`
Expected: PASS (4 tests).

- [ ] **Step 8: Commit**

```bash
git add CodeSDK/src CodeSDK/tests
git commit -m "feat(sdk): bill print url + LocationApi (provinces/districts/wards)"
```

---

### Task 10: Webhook payload + parser

**Files:**
- Create: `CodeSDK/src/NhatTinLogistics.Sdk/Webhooks/WebhookPayload.cs`
- Create: `CodeSDK/src/NhatTinLogistics.Sdk/Webhooks/NhatTinWebhookParser.cs`
- Test: `CodeSDK/tests/NhatTinLogistics.Sdk.Tests/WebhookTests.cs`

**Interfaces:**
- Consumes: `NhatTinJson`, `NhatTinApiException`, `BillStatus`, `int.ToBillStatus()`.
- Produces:
  - `WebhookPayload` (`BillNo`, `RefCode`, `StatusId`, `StatusName`, `StatusTime`, `PushTime`, `ShippingFee`, `IsPartial`, `Reason`, `Weight`, `DimensionWeight`, `Length`, `Width`, `Height`, `ExpectedAt`; computed `Status`, `IsPartialReturn`, `StatusTimeUtc`, `PushTimeUtc`, `ExpectedAtUtc`).
  - `NhatTinWebhookParser.Parse(string) → WebhookPayload`, `TryParse(string, out WebhookPayload) → bool`.

- [ ] **Step 1: Write the failing test** (`WebhookTests.cs`)

```csharp
using NhatTinLogistics.Sdk.Types.Enums;
using NhatTinLogistics.Sdk.Webhooks;
using Xunit;

namespace NhatTinLogistics.Sdk.Tests;

public class WebhookTests
{
    private const string Sample =
        "{\"weight\":2,\"bill_no\":\"CP16658276R\",\"status_time\":1681382601,\"shipping_fee\":38610," +
        "\"is_partial\":1,\"status_name\":\"Đã lấy hàng\",\"status_id\":3,\"dimension_weight\":1," +
        "\"length\":1,\"width\":1,\"height\":1,\"push_time\":1681382738,\"ref_code\":\"40724974\"," +
        "\"expected_at\":\"2024-08-02 09:00:00\"}";

    [Fact]
    public void Parse_maps_fields_and_status_enum()
    {
        var p = NhatTinWebhookParser.Parse(Sample);

        Assert.Equal("CP16658276R", p.BillNo);
        Assert.Equal(3, p.StatusId);
        Assert.Equal(BillStatus.PickedUp, p.Status);
        Assert.Equal(38610m, p.ShippingFee);
        Assert.True(p.IsPartialReturn);
        Assert.Equal(1681382601, p.StatusTime);
        Assert.Equal(2024, p.ExpectedAtUtc!.Value.Year);
    }

    [Fact]
    public void TryParse_returns_false_on_malformed_json()
    {
        Assert.False(NhatTinWebhookParser.TryParse("{not json", out _));
    }

    [Fact]
    public void TryParse_returns_true_on_valid()
    {
        Assert.True(NhatTinWebhookParser.TryParse(Sample, out var p));
        Assert.Equal("40724974", p.RefCode);
    }
}
```

- [ ] **Step 2: Run to verify it fails**

Run: `dotnet test CodeSDK/NhatTinLogisticsSdk.sln --filter FullyQualifiedName~WebhookTests`
Expected: FAIL — types not found.

- [ ] **Step 3: Write `Webhooks/WebhookPayload.cs`**

```csharp
using System.Globalization;
using System.Text.Json.Serialization;
using NhatTinLogistics.Sdk.Types.Enums;

namespace NhatTinLogistics.Sdk.Webhooks;

/// <summary>Typed incoming webhook payload (bill/webhook.md). NhatTin does NOT sign webhooks — no signature to verify.</summary>
public sealed class WebhookPayload
{
    [JsonPropertyName("bill_no")] public string BillNo { get; set; } = "";
    [JsonPropertyName("ref_code")] public string? RefCode { get; set; }
    [JsonPropertyName("status_id")] public int StatusId { get; set; }
    [JsonPropertyName("status_name")] public string? StatusName { get; set; }
    [JsonPropertyName("status_time")] public long StatusTime { get; set; }
    [JsonPropertyName("push_time")] public long PushTime { get; set; }
    [JsonPropertyName("shipping_fee")] public decimal ShippingFee { get; set; }
    [JsonPropertyName("is_partial")] public int IsPartial { get; set; }
    [JsonPropertyName("reason")] public string? Reason { get; set; }
    [JsonPropertyName("weight")] public double Weight { get; set; }
    [JsonPropertyName("dimension_weight")] public double DimensionWeight { get; set; }
    [JsonPropertyName("length")] public double Length { get; set; }
    [JsonPropertyName("width")] public double Width { get; set; }
    [JsonPropertyName("height")] public double Height { get; set; }
    [JsonPropertyName("expected_at")] public string? ExpectedAt { get; set; }

    [JsonIgnore] public BillStatus Status => StatusId.ToBillStatus();
    [JsonIgnore] public bool IsPartialReturn => IsPartial == 1;
    [JsonIgnore] public DateTimeOffset StatusTimeUtc => DateTimeOffset.FromUnixTimeSeconds(StatusTime);
    [JsonIgnore] public DateTimeOffset PushTimeUtc => DateTimeOffset.FromUnixTimeSeconds(PushTime);

    [JsonIgnore]
    public DateTime? ExpectedAtUtc =>
        DateTime.TryParseExact(ExpectedAt, "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture, DateTimeStyles.None, out var d)
            ? d : null;
}
```

- [ ] **Step 4: Write `Webhooks/NhatTinWebhookParser.cs`**

```csharp
using System.Text.Json;
using NhatTinLogistics.Sdk.Http;

namespace NhatTinLogistics.Sdk.Webhooks;

/// <summary>Parses raw NhatTin webhook JSON into a typed payload. Stateless — no client needed.</summary>
public static class NhatTinWebhookParser
{
    public static WebhookPayload Parse(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
            throw new NhatTinApiException("Webhook payload is empty.");
        try
        {
            var payload = JsonSerializer.Deserialize<WebhookPayload>(json, NhatTinJson.Options);
            if (payload is null)
                throw new NhatTinApiException("Webhook payload deserialized to null.", 0, json);
            return payload;
        }
        catch (JsonException ex)
        {
            throw new NhatTinApiException("Failed to parse webhook payload as JSON.", 0, json, ex);
        }
    }

    public static bool TryParse(string json, out WebhookPayload payload)
    {
        try
        {
            payload = Parse(json);
            return true;
        }
        catch (NhatTinApiException)
        {
            payload = new WebhookPayload();
            return false;
        }
    }
}
```

- [ ] **Step 5: Run to verify pass**

Run: `dotnet test CodeSDK/NhatTinLogisticsSdk.sln --filter FullyQualifiedName~WebhookTests`
Expected: PASS (3 tests).

- [ ] **Step 6: Commit**

```bash
git add CodeSDK/src CodeSDK/tests
git commit -m "feat(sdk): typed webhook payload + parser (unsigned)"
```

---

### Task 11: NhatTinLogisticsClient + DI extension

**Files:**
- Create: `CodeSDK/src/NhatTinLogistics.Sdk/NhatTinLogisticsClient.cs`
- Create: `CodeSDK/src/NhatTinLogistics.Sdk/Extensions/ServiceCollectionExtensions.cs`
- Test: `CodeSDK/tests/NhatTinLogistics.Sdk.Tests/ClientAndDiTests.cs`

**Interfaces:**
- Consumes: `AuthApi`, `BillApi`, `LocationApi`, `NhatTinHttpClient`, `InMemoryTokenStore`, `NhatTinLogisticsClientOptions`.
- Produces:
  - `NhatTinLogisticsClient` with `IAuthApi Auth`, `IBillApi Bill`, `ILocationApi Location`; two ctors: `(NhatTinHttpClient, NhatTinLogisticsClientOptions)` (DI) and `(NhatTinLogisticsClientOptions, HttpMessageHandler? handler = null)` (standalone).
  - `ServiceCollectionExtensions.AddNhatTinLogisticsClient(this IServiceCollection, Action<NhatTinLogisticsClientOptions>)`.

- [ ] **Step 1: Write the failing test** (`ClientAndDiTests.cs`)

```csharp
using Microsoft.Extensions.DependencyInjection;
using NhatTinLogistics.Sdk;
using NhatTinLogistics.Sdk.Extensions;
using NhatTinLogistics.Sdk.Tests.Infrastructure;
using Xunit;

namespace NhatTinLogistics.Sdk.Tests;

public class ClientAndDiTests
{
    [Fact]
    public async Task Standalone_client_creates_bill_end_to_end_via_handler()
    {
        var handler = new StubHttpMessageHandler(req =>
            req.RequestUri!.AbsolutePath.EndsWith("/sign-in")
                ? TestResponses.Ok("{\"success\":true,\"data\":{\"jwt_token\":\"A\",\"refresh_token\":\"R\"}}")
                : TestResponses.Ok("{\"success\":true,\"data\":{\"bill_code\":\"CP1\"}}"));

        var client = new NhatTinLogisticsClient(
            new NhatTinLogisticsClientOptions { Username = "u", Password = "p", BaseUrl = "https://test.local" },
            handler);

        var resp = await client.Bill.CreateAsync(new Types.Requests.CreateBillRequest { Weight = 1 });

        Assert.True(resp.IsSuccess);
        Assert.Equal("CP1", resp.Data!.BillCode);
    }

    [Fact]
    public void Di_registration_resolves_client()
    {
        var services = new ServiceCollection();
        services.AddNhatTinLogisticsClient(o =>
        {
            o.Username = "u"; o.Password = "p"; o.BaseUrl = "https://test.local";
        });
        using var provider = services.BuildServiceProvider();

        var client = provider.GetRequiredService<NhatTinLogisticsClient>();
        Assert.NotNull(client.Auth);
        Assert.NotNull(client.Bill);
        Assert.NotNull(client.Location);
    }

    [Fact]
    public void Di_registration_validates_options()
    {
        var services = new ServiceCollection();
        Assert.Throws<ArgumentException>(() =>
            services.AddNhatTinLogisticsClient(o => { /* no username/password, AutoAuthenticate default true */ }));
    }
}
```

- [ ] **Step 2: Run to verify it fails**

Run: `dotnet test CodeSDK/NhatTinLogisticsSdk.sln --filter FullyQualifiedName~ClientAndDiTests`
Expected: FAIL — `NhatTinLogisticsClient` / extension not found.

- [ ] **Step 3: Write `NhatTinLogisticsClient.cs`**

```csharp
using System.Net.Http;
using NhatTinLogistics.Sdk.Client;
using NhatTinLogistics.Sdk.Http;

namespace NhatTinLogistics.Sdk;

/// <summary>Entry point. Exposes Auth / Bill / Location. Use standalone or resolve from DI.</summary>
public sealed class NhatTinLogisticsClient
{
    public IAuthApi Auth { get; }
    public IBillApi Bill { get; }
    public ILocationApi Location { get; }

    /// <summary>DI-friendly constructor. The typed NhatTinHttpClient is supplied by the container.</summary>
    public NhatTinLogisticsClient(NhatTinHttpClient http, NhatTinLogisticsClientOptions options)
    {
        Auth = new AuthApi(http);
        Bill = new BillApi(http, options);
        Location = new LocationApi(http);
    }

    /// <summary>Standalone constructor. Owns its HttpClient. Pass a handler for tests.</summary>
    public NhatTinLogisticsClient(NhatTinLogisticsClientOptions options, HttpMessageHandler? handler = null)
    {
        options.Validate();
        var httpClient = handler is null ? new HttpClient() : new HttpClient(handler);
        httpClient.BaseAddress = new Uri(options.ResolveBaseUrl());
        httpClient.Timeout = TimeSpan.FromMilliseconds(options.TimeoutMilliseconds);

        var nt = new NhatTinHttpClient(httpClient, options, new InMemoryTokenStore());
        Auth = new AuthApi(nt);
        Bill = new BillApi(nt, options);
        Location = new LocationApi(nt);
    }
}
```

- [ ] **Step 4: Write `Extensions/ServiceCollectionExtensions.cs`**

```csharp
using Microsoft.Extensions.DependencyInjection;
using NhatTinLogistics.Sdk.Http;

namespace NhatTinLogistics.Sdk.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddNhatTinLogisticsClient(
        this IServiceCollection services, Action<NhatTinLogisticsClientOptions> configure)
    {
        var options = new NhatTinLogisticsClientOptions();
        configure(options);
        options.Validate();

        services.AddSingleton(options);
        services.AddSingleton<ITokenStore, InMemoryTokenStore>();
        services.AddHttpClient<NhatTinHttpClient>((_, http) =>
        {
            http.BaseAddress = new Uri(options.ResolveBaseUrl());
            http.Timeout = TimeSpan.FromMilliseconds(options.TimeoutMilliseconds);
        });
        services.AddScoped(sp =>
            new NhatTinLogisticsClient(sp.GetRequiredService<NhatTinHttpClient>(), options));

        return services;
    }
}
```

- [ ] **Step 5: Run to verify pass**

Run: `dotnet test CodeSDK/NhatTinLogisticsSdk.sln --filter FullyQualifiedName~ClientAndDiTests`
Expected: PASS (3 tests).

- [ ] **Step 6: Full suite green + commit**

Run: `dotnet test CodeSDK/NhatTinLogisticsSdk.sln`
Expected: all tests pass.

```bash
git add CodeSDK/src CodeSDK/tests
git commit -m "feat(sdk): NhatTinLogisticsClient entry point + DI registration"
```

---

### Task 12: Packaging — SdkVersion, README, CHANGELOG, NuGet metadata

**Files:**
- Create: `CodeSDK/src/NhatTinLogistics.Sdk/SdkVersion.cs`
- Create: `CodeSDK/src/NhatTinLogistics.Sdk/README.md`
- Create: `CodeSDK/src/NhatTinLogistics.Sdk/CHANGELOG.md`
- Modify: `CodeSDK/src/NhatTinLogistics.Sdk/NhatTinLogistics.Sdk.csproj` (add NuGet metadata)
- Test: `CodeSDK/tests/NhatTinLogistics.Sdk.Tests/SdkVersionTests.cs`

**Interfaces:**
- Produces: `SdkVersion.Current` (string constant) + `User-Agent` usage note.

- [ ] **Step 1: Write the failing test** (`SdkVersionTests.cs`)

```csharp
using NhatTinLogistics.Sdk;
using Xunit;

namespace NhatTinLogistics.Sdk.Tests;

public class SdkVersionTests
{
    [Fact]
    public void Current_is_semver_like()
    {
        Assert.Matches(@"^\d+\.\d+\.\d+$", SdkVersion.Current);
    }
}
```

- [ ] **Step 2: Run to verify it fails**

Run: `dotnet test CodeSDK/NhatTinLogisticsSdk.sln --filter FullyQualifiedName~SdkVersionTests`
Expected: FAIL — `SdkVersion` not found.

- [ ] **Step 3: Write `SdkVersion.cs`**

```csharp
namespace NhatTinLogistics.Sdk;

/// <summary>SDK version, kept in sync with the csproj Version and CHANGELOG.</summary>
public static class SdkVersion
{
    public const string Current = "0.1.0";
}
```

- [ ] **Step 4: Add NuGet metadata to the library csproj** — add these to the existing `<PropertyGroup>` in `NhatTinLogistics.Sdk.csproj`:

```xml
    <Version>0.1.0</Version>
    <PackageId>NhatTinLogistics.Sdk</PackageId>
    <Authors>TruePos</Authors>
    <Description>Unofficial C# SDK for the Nhat Tin Logistics (NTL) Open API: auth, bill, location, and webhook parsing. .NET 6.</Description>
    <PackageTags>nhattin;ntlogistics;logistics;shipping;vietnam;sdk</PackageTags>
    <PackageReadmeFile>README.md</PackageReadmeFile>
    <PackageLicenseExpression>MIT</PackageLicenseExpression>
    <IncludeSymbols>true</IncludeSymbols>
    <SymbolPackageFormat>snupkg</SymbolPackageFormat>
```

And add this `<ItemGroup>` so the README is packed:

```xml
  <ItemGroup>
    <None Include="README.md" Pack="true" PackagePath="\" />
  </ItemGroup>
```

- [ ] **Step 5: Write `README.md`** (library-facing usage)

````markdown
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
- `PartnerId` defaults from options for calc-fee / update / print; override per call where supported.
- Print endpoint host/format is not fully confirmed upstream; `GetPrintUrl` builds the URL, `PrintAsync` is best-effort.
````

- [ ] **Step 6: Write `CHANGELOG.md`**

```markdown
# Changelog

## 0.1.0

- Initial release.
- JWT auth with lazy sign-in and refresh-on-401.
- Bill API: create, update-shipping, destroy (cancel), calc-fee, revert-bill, tracking, print URL.
- Location API: provinces, districts, wards.
- Typed webhook payload parser (`NhatTinWebhookParser`).
- Standalone and DI (`AddNhatTinLogisticsClient`) usage.
```

- [ ] **Step 7: Run to verify pass + pack**

Run: `dotnet test CodeSDK/NhatTinLogisticsSdk.sln --filter FullyQualifiedName~SdkVersionTests`
Expected: PASS.

Run: `dotnet pack CodeSDK/src/NhatTinLogistics.Sdk/NhatTinLogistics.Sdk.csproj -c Release`
Expected: produces `NhatTinLogistics.Sdk.0.1.0.nupkg` under `bin/Release`, no errors.

- [ ] **Step 8: Full suite green + commit**

Run: `dotnet test CodeSDK/NhatTinLogisticsSdk.sln`
Expected: all tests pass.

```bash
git add CodeSDK/src CodeSDK/tests
git commit -m "chore(sdk): version, README, CHANGELOG, NuGet metadata + pack"
```

---

## Self-Review

**Spec coverage** (spec §→task):
- §2 architecture / folders → Tasks 1-12 (matches File Structure). ✅
- §3.1 standalone, §3.2 DI, §3.3 webhook → Task 11 (client + DI), Task 10 (webhook). ✅
- §3.4 method table → AuthApi (T6), BillApi (T7-9), LocationApi (T9). All endpoints present. ✅
- §4 options → Task 4. ✅
- §5 token/auth flow (lazy sign-in, retry-401, single refresh) → Task 5 (tests cover all three). ✅
- §6 envelope + errors → Task 2 (envelope/exception) + Task 5 (business-fail-no-throw). ✅
- §7 serialization (snake_case, string-numbers, doCode) → Task 2 JSON + Task 7/8 DTO tests. ✅
- §8 enums → Task 2. ✅
- §9 webhook → Task 10. ✅
- §10 print → Task 9 (GetPrintUrl confirmed; PrintAsync best-effort). ✅
- §11 DI → Task 11. ✅
- §12 tests → each task is TDD; covers items 1-10 of the spec's test list. ✅
- §13 packaging → Task 12. ✅
- §14 open points → documented in code comments (print) + README notes. ✅
- §15 build sequence → task order matches. ✅

**Placeholder scan:** No "TBD"/"add error handling"/"similar to". `IBillApi`/`BillApi` grow incrementally (Task 7 create/update, Task 8 ops, Task 9 print) — no `NotImplementedException` bodies or empty stub types in any intermediate commit. ✅

**Type consistency:** `NhatTinResponse<T>`, `NhatTinHttpClient.SendAsync/PostAsync/GetAsync/GetBytesAsync`, `IBillApi` method names/signatures, `AuthToken.JwtToken/RefreshToken`, `BillResult.Status`, `WebhookPayload.Status`, `ITokenStore.SetTokens/Clear` are used identically across tasks. Each task that adds an `IBillApi` method also adds its `BillApi` implementation + covering test in the same task, so the interface never has an unimplemented member. ✅
