using System.ComponentModel.DataAnnotations;
using NhatTinLogistics.Sdk.Types.Responses;

namespace NhatTinMvc.Web.Models;

/// <summary>Form tính phí (calc-fee) + kết quả danh sách dịch vụ.</summary>
public class FeeViewModel
{
    [Range(0.01, double.MaxValue, ErrorMessage = "Khối lượng phải > 0")]
    public double Weight { get; set; } = 1;
    public double? Width { get; set; }
    public double? Length { get; set; }
    public double? Height { get; set; }

    /// <summary>service_id (Master Data §1); null = để NhatTin trả mọi dịch vụ khả dụng.</summary>
    public int? ServiceId { get; set; }

    /// <summary>payment_method_id (Master Data §2). Mặc định 11 = người gửi trả sau.</summary>
    public int PaymentMethodId { get; set; } = 11;

    public double? CodAmount { get; set; }
    public double? CargoValue { get; set; }

    // Địa chỉ mới (sau 2025-07-01): tỉnh/phường theo *_id
    public string? SProvinceId { get; set; }
    public string? SWardId { get; set; }
    public string? RProvinceId { get; set; }
    public string? RWardId { get; set; }

    public List<FeeOption>? Results { get; set; }
    public string? Message { get; set; }
    public bool IsError { get; set; }
    public bool Submitted { get; set; }
}
