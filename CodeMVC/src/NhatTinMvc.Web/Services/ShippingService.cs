using System.Globalization;
using Microsoft.EntityFrameworkCore;
using NhatTinLogistics.Sdk;
using NhatTinLogistics.Sdk.Http;
using NhatTinLogistics.Sdk.Types.Requests;
using NhatTinLogistics.Sdk.Types.Responses;
using NhatTinMvc.Web.Data;
using NhatTinMvc.Web.Data.Entities;
using NhatTinMvc.Web.Models;

namespace NhatTinMvc.Web.Services;

/// <summary>
/// Bọc SDK (Auth/Bill/Location) + lưu trữ. Manual token mode: đăng nhập seed token vào ITokenStore và
/// set options.PartnerId để calc-fee/update/print tự lấy partner_id.
/// </summary>
public sealed class ShippingService : IShippingService
{
    private readonly NhatTinLogisticsClient _client;
    private readonly NhatTinLogisticsClientOptions _options;
    private readonly MvcDbContext _db;
    private readonly DemoAuthState _auth;
    private readonly ILogger<ShippingService> _logger;

    public ShippingService(
        NhatTinLogisticsClient client,
        NhatTinLogisticsClientOptions options,
        MvcDbContext db,
        DemoAuthState auth,
        ILogger<ShippingService> logger)
    {
        _client = client;
        _options = options;
        _db = db;
        _auth = auth;
        _logger = logger;
    }

    // ---- Auth ----

    public async Task<AuthStatusViewModel> LoginAsync(string username, string password, CancellationToken ct = default)
    {
        var resp = await _client.Auth.SignInAsync(username, password, ct);
        if (!resp.IsSuccess || resp.Data is null)
            return new AuthStatusViewModel { IsAuthenticated = false, IsError = true, Message = resp.Message ?? "Đăng nhập thất bại." };

        SeedFromToken(resp.Data);
        _auth.Username = username;
        _auth.LoginAtUtc = DateTimeOffset.UtcNow;
        _logger.LogInformation("Đăng nhập sandbox thành công, partner_id={PartnerId}", resp.Data.PartnerId);

        var vm = GetAuthStatus();
        vm.Message = "Đăng nhập thành công.";
        return vm;
    }

    public async Task<AuthStatusViewModel> RefreshAsync(CancellationToken ct = default)
    {
        var refresh = _client.Tokens.RefreshToken;
        if (string.IsNullOrEmpty(refresh))
            return new AuthStatusViewModel { IsAuthenticated = false, IsError = true, Message = "Chưa đăng nhập — không có refresh token." };

        var resp = await _client.Auth.RefreshTokenAsync(refresh, ct);
        if (!resp.IsSuccess || resp.Data is null)
        {
            var err = GetAuthStatus();
            err.IsError = true;
            err.Message = resp.Message ?? "Làm mới token thất bại.";
            return err;
        }

        SeedFromToken(resp.Data);
        var vm = GetAuthStatus();
        vm.Message = "Đã làm mới token.";
        return vm;
    }

    public void Logout()
    {
        _client.Tokens.Clear();
        _options.PartnerId = null;
        _auth.Clear();
    }

    public AuthStatusViewModel GetAuthStatus()
    {
        var t = _client.Tokens;
        return new AuthStatusViewModel
        {
            IsAuthenticated = !string.IsNullOrEmpty(t.AccessToken),
            PartnerId = _options.PartnerId ?? _auth.PartnerId,
            Username = _auth.Username,
            AccessTokenMasked = Mask(t.AccessToken),
            RefreshTokenMasked = Mask(t.RefreshToken),
            AccessTokenExpiresAt = t.AccessTokenExpiresAt,
            RefreshTokenExpiresAt = t.RefreshTokenExpiresAt,
            LoginAtUtc = _auth.LoginAtUtc
        };
    }

    private void SeedFromToken(AuthToken token)
    {
        var now = DateTimeOffset.UtcNow;
        DateTimeOffset? accessExp = ParseTtl(token.TokenExpiresIn) is { } a ? now + a : null;
        DateTimeOffset? refreshExp = ParseTtl(token.RefreshExpiresIn) is { } r ? now + r : null;
        _client.Tokens.SetTokens(token.JwtToken, token.RefreshToken, accessExp, refreshExp);
        if (token.PartnerId is not null)
        {
            _options.PartnerId = token.PartnerId;
            _auth.PartnerId = token.PartnerId;
        }
    }

    // ---- Location ----

    public Task<NhatTinResponse<List<ProvinceDto>>> GetProvincesAsync(bool isNew, CancellationToken ct = default)
        => _client.Location.GetProvincesAsync(isNew, ct);

    public Task<NhatTinResponse<List<DistrictDto>>> GetDistrictsAsync(string provinceId, CancellationToken ct = default)
        => _client.Location.GetDistrictsAsync(provinceId, ct);

    public Task<NhatTinResponse<List<WardDto>>> GetWardsAsync(string? districtId, string? provinceId, bool isNew, CancellationToken ct = default)
        => _client.Location.GetWardsAsync(districtId, provinceId, isNew, ct);

    // ---- Fee ----

    public Task<NhatTinResponse<List<FeeOption>>> CalcFeeAsync(CalcFeeRequest request, CancellationToken ct = default)
        => _client.Bill.CalcFeeAsync(request, ct);

    // ---- Bills (ghi) ----

    public async Task<(NhatTinResponse<BillResult> Response, TrackedBill? Saved)> CreateBillAsync(CreateBillRequest request, CancellationToken ct = default)
    {
        var resp = await _client.Bill.CreateAsync(request, ct);
        if (!resp.IsSuccess || resp.Data is null || string.IsNullOrEmpty(resp.Data.BillCode))
            return (resp, null);

        var data = resp.Data;
        var existing = await _db.TrackedBills.FirstOrDefaultAsync(b => b.BillCode == data.BillCode, ct);
        var bill = existing ?? new TrackedBill();

        bill.BillCode = data.BillCode;
        bill.RefCode = data.RefCode ?? request.RefCode;
        bill.PartnerId = _options.PartnerId;
        bill.CreatedAt = existing?.CreatedAt ?? DateTimeOffset.UtcNow;
        bill.SenderName = request.SName;
        bill.SenderPhone = request.SPhone;
        bill.SenderAddress = request.SAddress;
        bill.ReceiverName = data.ReceiverName ?? request.RName;
        bill.ReceiverPhone = data.ReceiverPhone ?? request.RPhone;
        bill.ReceiverAddress = data.ReceiverAddress ?? request.RAddress;
        bill.Weight = data.Weight != 0 ? data.Weight : request.Weight;
        bill.TotalFee = data.TotalFee;
        bill.ServiceId = data.ServiceId != 0 ? data.ServiceId : request.ServiceId;
        bill.LastStatusId = data.StatusId;
        bill.LastStatusName = data.Status.ToString();
        bill.LastStatusAt = DateTimeOffset.UtcNow;
        bill.RawCreateResponse = resp.RawBody;

        if (existing is null) _db.TrackedBills.Add(bill);
        await _db.SaveChangesAsync(ct);
        return (resp, bill);
    }

    public async Task<NhatTinResponse<BillResult>> UpdateBillAsync(UpdateBillRequest request, CancellationToken ct = default)
    {
        var resp = await _client.Bill.UpdateAsync(request, ct);
        if (resp.IsSuccess)
        {
            var bill = await _db.TrackedBills.FirstOrDefaultAsync(b => b.BillCode == request.BillCode, ct);
            if (bill is not null)
            {
                if (request.Weight is { } w) bill.Weight = w;
                if (request.ReceiverName is { } rn) bill.ReceiverName = rn;
                if (request.ReceiverPhone is { } rp) bill.ReceiverPhone = rp;
                if (request.ReceiverAddress is { } ra) bill.ReceiverAddress = ra;
                if (resp.Data is not null && resp.Data.TotalFee != 0) bill.TotalFee = resp.Data.TotalFee;
                await _db.SaveChangesAsync(ct);
            }
        }
        return resp;
    }

    public Task<NhatTinResponse<CancelResponse>> CancelAsync(IEnumerable<string> billCodes, CancellationToken ct = default)
        => _client.Bill.CancelAsync(billCodes, ct);

    public Task<NhatTinResponse<RevertResult>> RevertAsync(IEnumerable<string> billCodes, CancellationToken ct = default)
        => _client.Bill.RevertAsync(billCodes, ct);

    // ---- Bills (đọc / in) ----

    public async Task<NhatTinResponse<List<TrackingResult>>> TrackingAsync(string billCode, CancellationToken ct = default)
    {
        var resp = await _client.Bill.TrackingAsync(billCode, ct);
        // Cập nhật Last* từ trạng thái hiện tại (không tạo event — event là feed webhook).
        if (resp.IsSuccess && resp.Data is { Count: > 0 })
        {
            var top = resp.Data[0];
            var bill = await _db.TrackedBills.FirstOrDefaultAsync(b => b.BillCode == billCode, ct);
            if (bill is not null && top.BillStatusId != 0)
            {
                bill.LastStatusId = top.BillStatusId;
                bill.LastStatusName = top.BillStatusDesc ?? bill.LastStatusName;
                bill.LastStatusAt = DateTimeOffset.UtcNow;
                await _db.SaveChangesAsync(ct);
            }
        }
        return resp;
    }

    public Task<PrintResult> PrintAsync(string billCode, CancellationToken ct = default)
        => _client.Bill.PrintAsync(billCode, ct: ct);

    public string GetPrintUrl(string billCode) => _client.Bill.GetPrintUrl(billCode);

    // ---- Lưu trữ ----

    public async Task<IReadOnlyList<TrackedBill>> GetTrackedBillsAsync(int take = 100, CancellationToken ct = default)
        => await _db.TrackedBills.OrderByDescending(b => b.CreatedAt).Take(take).ToListAsync(ct);

    public Task<TrackedBill?> GetTrackedBillAsync(string billCode, CancellationToken ct = default)
        => _db.TrackedBills.FirstOrDefaultAsync(b => b.BillCode == billCode, ct);

    public async Task<IReadOnlyList<BillStatusEvent>> GetEventsAsync(string billCode, CancellationToken ct = default)
        => await _db.BillStatusEvents
            .Where(e => e.BillCode == billCode)
            .OrderByDescending(e => e.ReceivedAt)
            .ToListAsync(ct);

    // ---- Helpers ----

    /// <summary>Parse TTL NhatTin ("24h"/"7d"/"3600s"/số=giây) → TimeSpan để hiển thị hạn token (TokenTtl của SDK là internal).</summary>
    private static TimeSpan? ParseTtl(string? ttl)
    {
        if (string.IsNullOrWhiteSpace(ttl)) return null;
        var s = ttl.Trim();
        if (long.TryParse(s, NumberStyles.None, CultureInfo.InvariantCulture, out var bare))
            return TimeSpan.FromSeconds(bare);
        if (s.Length < 2) return null;
        if (!long.TryParse(s[..^1], NumberStyles.None, CultureInfo.InvariantCulture, out var v)) return null;
        return char.ToLowerInvariant(s[^1]) switch
        {
            's' => TimeSpan.FromSeconds(v),
            'm' => TimeSpan.FromMinutes(v),
            'h' => TimeSpan.FromHours(v),
            'd' => TimeSpan.FromDays(v),
            _ => null
        };
    }

    private static string? Mask(string? token)
    {
        if (string.IsNullOrEmpty(token)) return null;
        return token.Length <= 12 ? "****" : $"{token[..6]}…{token[^4..]}";
    }
}
