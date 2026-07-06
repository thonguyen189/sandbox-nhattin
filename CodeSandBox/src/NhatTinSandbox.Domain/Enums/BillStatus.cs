namespace NhatTinSandbox.Domain.Enums;

// status_id catalog from NhatTinAPIDocumentation/vi/00-thong-tin-ket-noi.md.
// Known-incomplete: only documented ids are listed.
public enum BillStatusId
{
    ChuaThanhCong = 1,     // Waiting
    ChoLayHang = 2,        // Waiting
    DaLayHang = 3,         // KCB
    DaGiaoHang = 4,        // FBC
    Huy = 6,               // GBV
    KhongPhatDuoc = 7,     // FUD
    DangChuyenHoan = 9,    // NRT
    DaChuyenHoan = 10,     // MRC
    SuCoGiaoHang = 11,     // QIU
    VanDonNhap = 12,       // DRF
    DangGiaoHang = 13,     // DEL
    DangVanChuyen = 15,
    DangGiaoHangHoan = 16,
    LoiLayHang = 17
}
