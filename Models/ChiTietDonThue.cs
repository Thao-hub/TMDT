using System.ComponentModel.DataAnnotations;

namespace ThueVayDuTiec.Web.Models;

public class ChiTietDonThue
{
    [Key]
    public int MaChiTiet { get; set; }

    public int MaDonThue { get; set; }

    public int MaVay { get; set; }

    public int SoNgayThue { get; set; }

    public decimal DonGia { get; set; }

    public DonThue? DonThue { get; set; }

    public VayDuTiec? Vay { get; set; }
}
