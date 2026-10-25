using System.ComponentModel.DataAnnotations;

namespace ThueVayDuTiec.Web.Models;

public class TheoDoiDonThue
{
    [Key]
    public int MaDonThue { get; set; }

    public DateTime? NgayNhanVay { get; set; }

    public DateTime? NgayKhongDenNhan { get; set; }

    [StringLength(500)]
    public string? GhiChuKhongDenNhan { get; set; }

    public decimal TienCocMat { get; set; }

    public decimal TienThueDaThu { get; set; }

    public DateTime? NgayThuTienThue { get; set; }

    public DateTime? NgayKiemTraTra { get; set; }

    [StringLength(30)]
    public string? TinhTrangVay { get; set; }

    [StringLength(1000)]
    public string? GhiChuKiemTra { get; set; }

    public decimal TienCoc { get; set; }

    public decimal TienKhauTru { get; set; }

    public decimal TienHoanCoc { get; set; }

    [StringLength(30)]
    public string? HinhThucHoanCoc { get; set; }

    public DateTime? NgayHoanCoc { get; set; }

    public string? NguoiThucHienId { get; set; }

    public DonThue? DonThue { get; set; }
}
