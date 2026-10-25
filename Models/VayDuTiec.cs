using System.ComponentModel.DataAnnotations;

namespace ThueVayDuTiec.Web.Models;

public class VayDuTiec
{
    [Key]
    public int MaVay { get; set; }

    public int MaDanhMuc { get; set; }

    [Required, StringLength(200)]
    public string TenVay { get; set; } = "";

    [StringLength(30)]
    public string KichCo { get; set; } = "M";

    [StringLength(50)]
    public string MauSac { get; set; } = "";

    [StringLength(100)]
    public string KieuDang { get; set; } = "";

    [Range(0, 100000000)]
    public decimal GiaThueMoiNgay { get; set; }

    [Range(0, 100000000)]
    public decimal TienDatCoc { get; set; }

    [StringLength(500)]
    public string? HinhAnh { get; set; }

    public bool DangKinhDoanh { get; set; } = true;

    public DanhMucVay? DanhMuc { get; set; }
    public ICollection<HinhAnhVay> DanhSachHinhAnh { get; set; } = new List<HinhAnhVay>();
}
