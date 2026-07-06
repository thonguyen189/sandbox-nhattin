using Microsoft.EntityFrameworkCore;
using NhatTinSandbox.Application.Bills;
using NhatTinSandbox.Domain.Entities;
using NhatTinSandbox.Domain.Enums;
using NhatTinSandbox.Infrastructure.Persistence;

namespace NhatTinSandbox.Infrastructure.Bills;

public sealed class BillService : IBillService
{
    private readonly SandboxDbContext _db;
    public BillService(SandboxDbContext db) => _db = db;

    // Deterministic, made-up sandbox fee formula (NOT the real Nhất Tín price table).
    internal static decimal CalcMainFee(double weight, decimal codAmount)
    {
        var weightUnits = Math.Ceiling(weight <= 0 ? 1 : weight);
        var codSurcharge = codAmount > 0 ? 5000m : 0m;
        return 18000m + (decimal)weightUnits * 3000m + codSurcharge;
    }

    private static string NewBillCode()
        => "CP" + DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString();

    private static BillSummary ToSummary(Bill b) => new(
        BillId: b.Id, BillCode: b.BillCode, RefCode: b.RefCode, StatusId: b.StatusId,
        CodAmount: b.CodAmount, ServiceId: b.ServiceId, PaymentMethod: b.PaymentMethodId,
        CreatedAt: b.CreatedAt, MainFee: b.MainFee, TotalFee: b.TotalFee,
        ReceiverName: b.ReceiverName, ReceiverPhone: b.ReceiverPhone, ReceiverAddress: b.ReceiverAddress,
        PackageNo: b.PackageNo, Weight: b.Weight, CargoContent: b.CargoContent,
        CargoValue: b.CargoValue, Note: b.Note);

    public async Task<BillSummary> CreateAsync(CreateBillInput input, CancellationToken ct)
    {
        var mainFee = CalcMainFee(input.Weight, input.CodAmount ?? 0);
        var otherFee = input.OtherFee ?? 0;
        var bill = new Bill
        {
            BillCode = NewBillCode(),
            RefCode = input.RefCode,
            PackageNo = input.PackageNo ?? 1,
            Weight = input.Weight,
            Width = input.Width,
            Length = input.Length,
            Height = input.Height,
            CargoContent = input.CargoContent,
            ServiceId = input.ServiceId,
            PaymentMethodId = input.PaymentMethodId,
            IsReturnDoc = input.IsReturnDoc ?? 0,
            CodAmount = input.CodAmount ?? 0,
            Note = input.Note,
            CargoValue = input.CargoValue ?? 0,
            CargoTypeId = input.CargoTypeId,
            SenderName = input.SName,
            SenderPhone = input.SPhone,
            SenderAddress = input.SAddress,
            SenderProvinceCode = input.SProvinceCode,
            SenderWardCode = input.SWardCode,
            ReceiverName = input.RName,
            ReceiverPhone = input.RPhone,
            ReceiverAddress = input.RAddress,
            ReceiverProvinceCode = input.RProvinceCode,
            ReceiverWardCode = input.RWardCode,
            IsDraft = input.IsDraft ?? 0,
            OtherFee = otherFee,
            IsInstallation = input.IsInstallation ?? 0,
            BillType = input.BillType ?? 1,
            BillReturn = input.BillReturn,
            StatusId = (input.IsDraft ?? 0) == 1 ? (int)BillStatusId.VanDonNhap : (int)BillStatusId.ChoLayHang,
            MainFee = mainFee,
            TotalFee = mainFee + otherFee,
            CreatedAt = DateTimeOffset.UtcNow,
            ExpectedAt = DateTimeOffset.UtcNow.AddDays(3)
        };

        _db.Bills.Add(bill);
        await _db.SaveChangesAsync(ct);

        _db.BillStatusHistories.Add(new BillStatusHistory
        {
            BillId = bill.Id,
            StatusId = bill.StatusId,
            StatusName = StatusName(bill.StatusId),
            ShippingFee = bill.TotalFee,
            ChangedAt = DateTimeOffset.UtcNow
        });
        await _db.SaveChangesAsync(ct);

        return ToSummary(bill);
    }

    public async Task<BillSummary?> GetByCodeAsync(string billCode, CancellationToken ct)
    {
        var bill = await _db.Bills.FirstOrDefaultAsync(b => b.BillCode == billCode, ct);
        return bill is null ? null : ToSummary(bill);
    }

    public async Task<BillSummary?> SetStatusAsync(string billCode, int statusId, string? reason, CancellationToken ct)
    {
        var bill = await _db.Bills.FirstOrDefaultAsync(b => b.BillCode == billCode, ct);
        if (bill is null) return null;

        bill.StatusId = statusId;
        _db.BillStatusHistories.Add(new BillStatusHistory
        {
            BillId = bill.Id,
            StatusId = statusId,
            StatusName = StatusName(statusId),
            Reason = reason,
            ShippingFee = bill.TotalFee,
            ChangedAt = DateTimeOffset.UtcNow
        });
        await _db.SaveChangesAsync(ct);
        return ToSummary(bill);
    }

    private string StatusName(int statusId)
        => _db.MasterData
            .Where(m => m.Kind == MasterDataKind.BillStatus && m.Code == statusId)
            .Select(m => m.Name)
            .FirstOrDefault() ?? $"Status {statusId}";

    // ---- Task 7 fills these ----
    public Task<BillSummary?> UpdateAsync(UpdateBillInput input, CancellationToken ct)
        => throw new NotImplementedException();
    public Task<IReadOnlyList<FeeOption>> CalcFeeAsync(CalcFeeInput input, CancellationToken ct)
        => throw new NotImplementedException();
    public Task<IReadOnlyList<CancelResult>> CancelAsync(IReadOnlyList<string> billCodes, CancellationToken ct)
        => throw new NotImplementedException();
    public Task<RevertResult> RevertAsync(IReadOnlyList<string> billCodes, CancellationToken ct)
        => throw new NotImplementedException();
}
