using System.ComponentModel.DataAnnotations;

namespace ThueVayDuTiec.Web.Models;

public class DonThue
{
    [Key]
    public int MaDonThue { get; set; }

    [StringLength(50)]
    public string? NguoiDungId { get; set; }

    [Required, StringLength(100)]
    public string HoTenKhach { get; set; } = "";

    [Required, EmailAddress, StringLength(150)]
    public string Email { get; set; } = "";

    [Required, StringLength(30)]
    public string SoDienThoai { get; set; } = "";

    public DateTime NgayTao { get; set; } = DateTime.Now;

    public DateTime NgayBatDauThue { get; set; }

    public DateTime NgayKetThucThue { get; set; }

    public decimal TongTien { get; set; }

    [StringLength(30)]
    public string TrangThai { get; set; } = "ChoXacNhan";

    [StringLength(500)]
    public string? LyDoHuy { get; set; }

    public NguoiDung? NguoiDung { get; set; }

    public TheoDoiDonThue? TheoDoi { get; set; }

    public List<ChiTietDonThue> DanhSachChiTiet { get; set; } = new();
}
