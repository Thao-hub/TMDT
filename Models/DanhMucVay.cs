using System.ComponentModel.DataAnnotations;

namespace ThueVayDuTiec.Web.Models;

public class DanhMucVay
{
    [Key]
    public int MaDanhMuc { get; set; }

    [Required, StringLength(100)]
    public string TenDanhMuc { get; set; } = "";

    public string? MoTa { get; set; }

    public List<VayDuTiec> DanhSachVay { get; set; } = new();
}
