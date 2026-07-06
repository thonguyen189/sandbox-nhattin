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
        var codSurcharge = codAmount > 0 ? 5000m : 0m;
        return CalcBaseMainFee(weight) + codSurcharge;
    }

    // Base main fee WITHOUT the COD surcharge. Used by the calc-fee path so that cod_fee can be
    // reported as its own breakdown line without being double-counted inside main_fee (per
    // calcfee.md, where total_fee == main_fee + insur_fee + remote_fee + cod_fee).
    private static decimal CalcBaseMainFee(double weight)
    {
        var weightUnits = Math.Ceiling(weight <= 0 ? 1 : weight);
        return 18000m + (decimal)weightUnits * 3000m;
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

    public async Task<BillSummary?> UpdateAsync(UpdateBillInput input, CancellationToken ct)
    {
        var bill = await _db.Bills.FirstOrDefaultAsync(b => b.BillCode == input.BillCode, ct);
        if (bill is null) return null;

        if (input.CodAmount.HasValue) bill.CodAmount = input.CodAmount.Value;
        if (input.CargoValue.HasValue) bill.CargoValue = input.CargoValue.Value;
        if (input.Weight.HasValue) bill.Weight = input.Weight.Value;
        if (input.Length.HasValue) bill.Length = input.Length.Value;
        if (input.Height.HasValue) bill.Height = input.Height.Value;
        if (input.Width.HasValue) bill.Width = input.Width.Value;
        if (input.CargoContent is not null) bill.CargoContent = input.CargoContent;
        if (input.ReceiverPhone is not null) bill.ReceiverPhone = input.ReceiverPhone;
        if (input.ReceiverName is not null) bill.ReceiverName = input.ReceiverName;
        if (input.ReceiverAddress is not null) bill.ReceiverAddress = input.ReceiverAddress;
        if (input.PackageNo.HasValue) bill.PackageNo = input.PackageNo.Value;
        if (input.IsReturnDoc.HasValue) bill.IsReturnDoc = input.IsReturnDoc.Value;
        if (input.Note is not null) bill.Note = input.Note;
        if (input.IsInstallation.HasValue) bill.IsInstallation = input.IsInstallation.Value;

        bill.MainFee = CalcMainFee(bill.Weight, bill.CodAmount);
        bill.TotalFee = bill.MainFee + bill.OtherFee;
        await _db.SaveChangesAsync(ct);
        return ToSummary(bill);
    }

    public async Task<IReadOnlyList<FeeOption>> CalcFeeAsync(CalcFeeInput input, CancellationToken ct)
    {
        // Sandbox approximation: build one option per known service. Not the real price table.
        var services = await _db.MasterData
            .Where(m => m.Kind == MasterDataKind.Service)
            .OrderBy(m => m.Code)
            .ToListAsync(ct);

        var cod = input.CodAmount ?? 0;
        var lead = DateTimeOffset.UtcNow.AddDays(2).ToString("dd/MM/yyyy HH:mm");
        var options = new List<FeeOption>();
        foreach (var s in services)
        {
            if (input.ServiceId.HasValue && input.ServiceId.Value != s.Code) continue;
            var main = CalcBaseMainFee(input.Weight);
            var codFee = cod > 0 ? 5000m : 0m;
            options.Add(new FeeOption(
                Weight: input.Weight,
                TotalFee: main + codFee,
                MainFee: main,
                InsurFee: 0,
                RemoteFee: 0,
                CodFee: codFee,
                ServiceId: s.Code,
                ServiceName: s.Name,
                LeadTime: lead));
        }
        return options;
    }

    public async Task<IReadOnlyList<CancelResult>> CancelAsync(IReadOnlyList<string> billCodes, CancellationToken ct)
    {
        var results = new List<CancelResult>();
        foreach (var code in billCodes)
        {
            var bill = await _db.Bills.FirstOrDefaultAsync(b => b.BillCode == code, ct);
            if (bill is null)
            {
                results.Add(new CancelResult(code, $"Bill {code} not found"));
                continue;
            }
            // Doc: cancel allowed for status 1 (Chưa thành công) and 2 (Chờ lấy hàng).
            if (bill.StatusId is 1 or 2)
            {
                bill.StatusId = (int)Domain.Enums.BillStatusId.Huy;
                _db.BillStatusHistories.Add(new BillStatusHistory
                {
                    BillId = bill.Id, StatusId = bill.StatusId, StatusName = StatusName(bill.StatusId),
                    ShippingFee = bill.TotalFee, ChangedAt = DateTimeOffset.UtcNow
                });
                results.Add(new CancelResult(code, $"Bill {code} has canceled successful"));
            }
            else
            {
                results.Add(new CancelResult(code, $"Bill {code} cannot be canceled at status {bill.StatusId}"));
            }
        }
        await _db.SaveChangesAsync(ct);
        return results;
    }

    public async Task<RevertResult> RevertAsync(IReadOnlyList<string> billCodes, CancellationToken ct)
    {
        var success = new List<string>();
        var failed = new List<string>();
        foreach (var code in billCodes)
        {
            var bill = await _db.Bills.FirstOrDefaultAsync(b => b.BillCode == code, ct);
            // Doc: revert allowed for status 3 (Đã lấy hàng), 15 (Đang vận chuyển), 7 (Không phát được).
            if (bill is not null && bill.StatusId is 3 or 15 or 7)
            {
                bill.StatusId = (int)Domain.Enums.BillStatusId.DangChuyenHoan;
                _db.BillStatusHistories.Add(new BillStatusHistory
                {
                    BillId = bill.Id, StatusId = bill.StatusId, StatusName = StatusName(bill.StatusId),
                    ShippingFee = bill.TotalFee, ChangedAt = DateTimeOffset.UtcNow
                });
                success.Add(code);
            }
            else
            {
                failed.Add(code);
            }
        }
        await _db.SaveChangesAsync(ct);
        return new RevertResult(success, failed);
    }
}
