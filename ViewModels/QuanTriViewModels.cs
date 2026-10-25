using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;
using ThueVayDuTiec.Web.Models;

namespace ThueVayDuTiec.Web.ViewModels;

public class VayQuanTriViewModel
{
    public int MaVay { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Vui lòng chọn danh mục.")]
    [Display(Name = "Danh mục")]
    public int MaDanhMuc { get; set; }

    [Required, StringLength(200)]
    [Display(Name = "Tên váy")]
    public string TenVay { get; set; } = "";

    [Required, StringLength(30)]
    [Display(Name = "Kích cỡ")]
    public string KichCo { get; set; } = "";

    [Required, StringLength(50)]
    [Display(Name = "Màu sắc")]
    public string MauSac { get; set; } = "";

    [Required, StringLength(100)]
    [Display(Name = "Kiểu dáng")]
    public string KieuDang { get; set; } = "";

    [Range(0, 100000000)]
    [Display(Name = "Giá thuê mỗi ngày")]
    public decimal GiaThueMoiNgay { get; set; }

    [Range(0, 100000000)]
    [Display(Name = "Tiền đặt cọc")]
    public decimal TienDatCoc { get; set; }

    [StringLength(500)]
    [Display(Name = "Ảnh đại diện cũ")]
    public string? HinhAnh { get; set; }

    [Display(Name = "Hình ảnh")]
    public List<IFormFile> AnhTaiLen { get; set; } = [];

    public IReadOnlyList<HinhAnhVay> DanhSachHinhAnh { get; set; } = [];
}

public class DanhMucQuanTriViewModel
{
    public int MaDanhMuc { get; set; }

    [Required, StringLength(100)]
    [Display(Name = "Tên danh mục")]
    public string TenDanhMuc { get; set; } = "";

    [StringLength(500)]
    [Display(Name = "Mô tả")]
    public string? MoTa { get; set; }
}

public class TrangQuanTriViewModel
{
    public int SoVayDangKinhDoanh { get; set; }
    public int SoDanhMuc { get; set; }
    public int SoDonCho { get; set; }
    public int SoDonDaXacNhan { get; set; }
    public IReadOnlyList<DonThue> DonGanDay { get; set; } = [];
}
