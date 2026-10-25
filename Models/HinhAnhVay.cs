using System.ComponentModel.DataAnnotations;

namespace ThueVayDuTiec.Web.Models;

public class HinhAnhVay
{
    [Key]
    public int MaAnh { get; set; }

    public int MaVay { get; set; }

    [Required, StringLength(500)]
    public string DuongDan { get; set; } = "";

    public bool AnhChinh { get; set; }
    public int ThuTu { get; set; }

    public VayDuTiec? Vay { get; set; }
}
