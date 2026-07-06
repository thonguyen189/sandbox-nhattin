namespace NhatTinSandbox.Domain.Entities;

public enum LocationKind { Province = 1, District = 2, Ward = 3 }

public class Location
{
    public int Id { get; set; }
    public LocationKind Kind { get; set; }
    public string Code { get; set; } = string.Empty; // province/district/ward id used by API
    public string Name { get; set; } = string.Empty;
    public string? ParentCode { get; set; }          // province code for districts/wards
    public string? DistrictCode { get; set; }        // district code for old-unit wards
    public bool IsNew { get; set; }                  // true => "Y", false => "N"
}
