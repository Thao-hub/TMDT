using System.ComponentModel.DataAnnotations;

namespace ThueVayDuTiec.Web.Models;

public class YeuCauDatThue
{
    public int MaVay { get; set; }

    [Required]
    public DateTime NgayBatDauThue { get; set; } = DateTime.Today.AddDays(1);

    [Required]
    public DateTime NgayKetThucThue { get; set; } = DateTime.Today.AddDays(2);
}
