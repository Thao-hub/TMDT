using System.ComponentModel.DataAnnotations;

namespace ThueVayDuTiec.Web.ViewModels;

public class HoanTraViewModel
{
    [Required]
    public string TinhTrangVay { get; set; } = "Tot";

    [StringLength(1000)]
    public string? GhiChuKiemTra { get; set; }

    [Range(typeof(decimal), "0", "1000000000")]
    public decimal TienKhauTru { get; set; }

    [Required]
    public string HinhThucHoanCoc { get; set; } = "ChuyenKhoan";

    public bool DaHoanCocNgay { get; set; }
}
