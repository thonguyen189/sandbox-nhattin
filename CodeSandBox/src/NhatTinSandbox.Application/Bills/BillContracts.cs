namespace NhatTinSandbox.Application.Bills;

public sealed record CreateBillInput(
    string? RefCode,
    int? PackageNo,
    double Weight,
    double Width,
    double Length,
    double Height,
    string? CargoContent,
    int ServiceId,
    int PaymentMethodId,
    int? IsReturnDoc,
    decimal? CodAmount,
    string? Note,
    double? CargoValue,
    int CargoTypeId,
    string SName,
    string SPhone,
    string SAddress,
    string SProvinceCode,
    string SWardCode,
    string RName,
    string RPhone,
    string RAddress,
    string RProvinceCode,
    string RWardCode,
    int? IsDraft,
    decimal? OtherFee,
    int? IsInstallation,
    int? BillType,
    string? BillReturn);

public sealed record UpdateBillInput(
    int PartnerId,
    string BillCode,
    decimal? CodAmount,
    double? CargoValue,
    double? Weight,
    double? Length,
    double? Height,
    double? Width,
    string? CargoContent,
    string? ReceiverPhone,
    string? ReceiverName,
    string? ReceiverAddress,
    int? PackageNo,
    int? IsReturnDoc,
    string? Note,
    int? IsInstallation);

public sealed record BillSummary(
    int BillId,
    string BillCode,
    string? RefCode,
    int StatusId,
    decimal CodAmount,
    int ServiceId,
    int PaymentMethod,
    DateTimeOffset CreatedAt,
    decimal MainFee,
    decimal TotalFee,
    string ReceiverName,
    string ReceiverPhone,
    string ReceiverAddress,
    int PackageNo,
    double Weight,
    string? CargoContent,
    double CargoValue,
    string? Note);

public sealed record CalcFeeInput(
    int? PartnerId,
    double Weight,
    double? Width,
    double? Length,
    double? Height,
    int? ServiceId,
    int PaymentMethodId,
    decimal? CodAmount,
    double? CargoValue,
    string? SProvinceId,
    string? SWardId,
    string? RProvinceId,
    string? RWardId);

public sealed record FeeOption(
    double Weight,
    decimal TotalFee,
    decimal MainFee,
    decimal InsurFee,
    decimal RemoteFee,
    decimal CodFee,
    int ServiceId,
    string ServiceName,
    string LeadTime);

public sealed record CancelResult(string DoCode, string Message);
public sealed record RevertResult(IReadOnlyList<string> Success, IReadOnlyList<string> Failed);

public interface IBillService
{
    Task<BillSummary> CreateAsync(CreateBillInput input, CancellationToken ct);
    Task<BillSummary?> UpdateAsync(UpdateBillInput input, CancellationToken ct);
    Task<BillSummary?> GetByCodeAsync(string billCode, CancellationToken ct);
    Task<IReadOnlyList<FeeOption>> CalcFeeAsync(CalcFeeInput input, CancellationToken ct);
    Task<IReadOnlyList<CancelResult>> CancelAsync(IReadOnlyList<string> billCodes, CancellationToken ct);
    Task<RevertResult> RevertAsync(IReadOnlyList<string> billCodes, CancellationToken ct);
    // Used by AdminPortal + /sandbox route; returns null if bill not found.
    Task<BillSummary?> SetStatusAsync(string billCode, int statusId, string? reason, CancellationToken ct);
}
